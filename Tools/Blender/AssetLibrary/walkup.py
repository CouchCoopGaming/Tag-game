"""Three-storey brick walk-up. Door and fire escapes face +Z.

The baked brick tile is 8 courses per UV metre. uv 1.625 puts 13 courses
on each world metre, about 38 courses on a 2.9 m floor.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from _volume import _extrude

W = 8.0
D = 6.4
BODY_H = 9.30
# Main volume stops behind the front skin. Glass sits in the 13 cm reveal.
FRONT = 3.04
SKIN0 = 3.08
SKIN1 = 3.20
BRICK_UV = 1.625
X_ESC = -2.35


def _span(g, x0, x1, y0, y1, z0, z1, mat, uv, shrink=0.003):
    x0 += shrink
    x1 -= shrink
    y0 += shrink
    y1 -= shrink
    if x1 - x0 < 0.03 or y1 - y0 < 0.03 or z1 - z0 < 0.01:
        return
    g.box(
        ((x0 + x1) * 0.5, (y0 + y1) * 0.5, (z0 + z1) * 0.5),
        (x1 - x0, y1 - y0, z1 - z0),
        mat,
        uv_scale=uv,
    )


def _facade(g, lod):
    """Brick skin with window and door holes. Neighbors stay 6 mm apart."""
    piers_low = ((-4.0, -3.42), (-2.58, -1.92), (-1.08, -0.56), (0.56, 1.08), (1.92, 2.58), (3.42, 4.0))
    piers_up = ((-4.0, -3.42), (-2.58, -1.92), (-1.08, 1.08), (1.92, 2.58), (3.42, 4.0))
    bands = (
        (0.00, 1.12, "door"),
        (1.12, 2.28, "low"),
        (2.28, 4.02, "solid"),
        (4.02, 5.18, "up"),
        (5.18, 6.92, "solid"),
        (6.92, 8.08, "up"),
        (8.08, 9.24, "solid"),
    )
    for y0, y1, kind in bands:
        if kind == "solid":
            _span(g, -4.0, 4.0, y0, y1, SKIN0, SKIN1, "Lib_Brick", BRICK_UV)
        elif kind == "door":
            _span(g, -4.0, -0.56, y0, y1, SKIN0, SKIN1, "Lib_Brick", BRICK_UV)
            _span(g, 0.56, 4.0, y0, y1, SKIN0, SKIN1, "Lib_Brick", BRICK_UV)
        else:
            piers = piers_low if kind == "low" else piers_up
            for x0, x1 in piers:
                _span(g, x0, x1, y0, y1, SKIN0, SKIN1, "Lib_Brick", BRICK_UV)
    openings = ((-3.0, 1.70), (-1.5, 1.70), (1.5, 1.70), (3.0, 1.70),
                (-3.0, 4.60), (-1.5, 4.60), (1.5, 4.60), (3.0, 4.60))
    if lod < 2:
        openings = openings + ((-3.0, 7.50), (-1.5, 7.50), (1.5, 7.50), (3.0, 7.50))
    else:
        openings = ((-1.5, 4.60), (1.5, 4.60))
    for x, y in openings:
        # Skin outer face is z=3.20. Jambs sit in the pier gap, sash 13 cm back.
        g.box((x - 0.38, y, 3.12), (0.036, 0.92, 0.12), "Lib_Brick", uv_scale=BRICK_UV)
        g.box((x + 0.38, y, 3.12), (0.036, 0.92, 0.12), "Lib_Brick", uv_scale=BRICK_UV)
        g.box((x, y + 0.48, 3.12), (0.68, 0.036, 0.12), "Lib_Brick", uv_scale=BRICK_UV)
        g.box((x, y - 0.48, 3.12), (0.68, 0.036, 0.12), "Lib_Brick", uv_scale=BRICK_UV)
        g.box((x, y, 3.04), (0.70, 0.92, 0.016), "Lib_ShopGlass")
        g.box((x, y - 0.58, 3.32), (0.92, 0.06, 0.14), "Lib_Concrete")
        g.box((x, y + 0.58, 3.30), (0.96, 0.07, 0.10), "Lib_Concrete")
        if lod == 0:
            g.box((x, y, 3.02), (0.62, 0.02, 0.012), "Lib_PaintCream")
            g.box((x, y, 3.02), (0.02, 0.84, 0.012), "Lib_PaintCream")
    g.box((0, 1.16, 3.10), (0.92, 2.00, 0.03), "Lib_WoodDark")
    if lod == 0:
        g.box((0, 2.22, 3.055), (0.70, 0.10, 0.016), "Lib_ShopGlass")
        g.box((0, 1.16, 3.078), (0.86, 0.018, 0.012), "Lib_PaintCream")


def _cornice(g):
    """Parapet cap, and a corona that mitres around every corner."""
    g.box((0, 9.48, -0.06), (8.46, 0.18, 6.78), "Lib_PaintCream", uv_scale=0.6)
    g.box((0, 9.70, -0.06), (8.22, 0.12, 6.54), "Lib_Concrete", uv_scale=0.5)
    # Straight runs stop 2 cm short of the mitre so the corner prisms do not share a volume.
    g.box((0, 9.28, 3.39), (8.16, 0.14, 0.22), "Lib_Concrete", uv_scale=0.5)
    g.box((0, 9.28, -3.39), (8.16, 0.14, 0.22), "Lib_Concrete", uv_scale=0.5)
    g.box((4.21, 9.28, 0.0), (0.22, 0.14, 6.52), "Lib_Concrete", uv_scale=0.5)
    g.box((-4.21, 9.28, 0.0), (0.22, 0.14, 6.52), "Lib_Concrete", uv_scale=0.5)
    for sx, sz in ((1.0, 1.0), (-1.0, 1.0), (1.0, -1.0), (-1.0, -1.0)):
        _corona_miter(g, sx, sz)


def _corona_miter(g, sx, sz):
    ax0, ax1 = 4.10, 4.32
    az0, az1 = 3.28, 3.50
    poly_f = (
        (ax0 + 0.010, az1 - 0.004),
        (ax1 - 0.014, az1 - 0.004),
        (ax0 + 0.010, az0 + 0.014),
    )
    poly_s = (
        (ax1 - 0.004, az0 + 0.010),
        (ax1 - 0.004, az1 - 0.014),
        (ax0 + 0.014, az0 + 0.010),
    )
    _extrude(g, [(sx * x, sz * z) for x, z in poly_f], 9.21, 9.35, "Lib_Concrete")
    _extrude(g, [(sx * x, sz * z) for x, z in poly_s], 9.21, 9.35, "Lib_Concrete")


def _entry(g, lod):
    g.box((0, 0.09, 3.62), (1.70, 0.16, 0.72), "Lib_Concrete", uv_scale=0.8)
    g.box((0, 0.045, 4.16), (1.95, 0.07, 0.32), "Lib_Concrete", uv_scale=0.8)
    g.box((0, 2.52, 3.66), (1.85, 0.08, 0.78), "Lib_PaintCream", uv_scale=0.7)
    if lod == 0:
        for x in (-0.72, 0.72):
            g.pipe((x, 2.16, 3.28), (x, 2.42, 3.70), 0.016, "Lib_SteelDark", 6)


# Landings on the street face. Switchback stairs sit between them, clear of the brick.
_LX0, _LX1 = -3.22, -1.82
_MZ0, _MZ1 = 3.44, 4.24
_LY, _MY, _UY = 3.95, 5.40, 6.85
_MX0, _MX1 = -0.08, 1.18


def _landing(g, x0, x1, y_top, z0, z1):
    g.box(((x0 + x1) * 0.5, y_top - 0.018, (z0 + z1) * 0.5), (x1 - x0, 0.032, z1 - z0), "Lib_SteelDark")


def _brackets(g, x0, x1, y_top):
    """Arms and a diagonal, 1 cm clear of the brick skin at z=3.20."""
    for x in (x0 + 0.14, x1 - 0.14):
        g.box((x, y_top - 0.05, 3.312), (0.032, 0.032, 0.176), "Lib_SteelDark")
        g.pipe((x, y_top - 0.58, 3.24), (x, y_top - 0.08, 3.50), 0.014, "Lib_SteelDark", 5)


def _rails(g, x0, x1, z0, z1, y_top):
    """Posts, a mid rail, and a top rail about 1 m above the deck."""
    for x in (x0 + 0.045, x1 - 0.045):
        for z in (z0 + 0.045, z1 - 0.045):
            g.box((x, y_top + 0.50, z), (0.028, 0.96, 0.028), "Lib_SteelDark")
    y_top_r = y_top + 1.005
    y_mid = y_top + 0.50
    for z in (z0 + 0.045, z1 - 0.045):
        g.pipe((x0 + 0.09, y_top_r, z), (x1 - 0.09, y_top_r, z), 0.015, "Lib_Steel", 5)
        g.pipe((x0 + 0.09, y_mid, z), (x1 - 0.09, y_mid, z), 0.013, "Lib_Steel", 5)
    for x in (x0 + 0.045, x1 - 0.045):
        g.pipe((x, y_top_r, z0 + 0.09), (x, y_top_r, z1 - 0.09), 0.015, "Lib_Steel", 5)
        g.pipe((x, y_mid, z0 + 0.09), (x, y_mid, z1 - 0.09), 0.013, "Lib_Steel", 5)


def _flight(g, lod, x0, x1, zc, y0, y1):
    """Open-riser stair with a rail 1 m above the stringer."""
    n = lod_pick(lod, 7, 4)
    half = 0.15
    for s in (-1.0, 1.0):
        z = zc + s * (half + 0.04)
        g.pipe((x0, y0 + 0.05, z), (x1, y1 + 0.05, z), 0.014, "Lib_SteelDark", 5)
        g.pipe((x0, y0 + 1.00, z), (x1, y1 + 1.00, z), 0.013, "Lib_Steel", 5)
    span = abs(x1 - x0)
    for i in range(n):
        t = (i + 0.5) / float(n)
        x = x0 + (x1 - x0) * t
        y = y0 + (y1 - y0) * t + 0.02
        g.box((x, y, zc), (span / n * 0.62, 0.026, half * 1.7), "Lib_Steel")


def _drop_ladder(g, lod):
    """Hangs from the lower landing, short of the sidewalk."""
    xs = (_LX0 - 0.18, _LX0 - 0.52)
    zs = 3.86
    for x in xs:
        g.pipe((x, 0.42, zs), (x, _LY - 0.09, zs), 0.015, "Lib_SteelDark", 6)
    g.pipe((_LX0 - 0.012, _LY - 0.05, zs), (xs[1] + 0.04, _LY - 0.05, zs), 0.014, "Lib_SteelDark", 5)
    rungs = lod_pick(lod, 6, 3)
    for i in range(rungs):
        y = 0.72 + i * (2.70 / rungs)
        g.pipe((xs[0] - 0.04, y, zs), (xs[1] + 0.04, y, zs), 0.011, "Lib_Steel", 5)


def _escape(g, lod):
    """Landings, 1 m rails, a switchback, and a drop ladder, bracketed to the brick."""
    _landing(g, _LX0, _LX1, _LY, _MZ0, _MZ1)
    _brackets(g, _LX0, _LX1, _LY)
    _rails(g, _LX0, _LX1, _MZ0, _MZ1, _LY)
    _drop_ladder(g, lod)
    if lod < 2:
        _landing(g, _MX0, _MX1, _MY, _MZ0, _MZ1)
        _brackets(g, _MX0, _MX1, _MY)
        _rails(g, _MX0, _MX1, _MZ0, _MZ1, _MY)
        _flight(g, lod, -1.72, -0.18, 4.02, _LY, _MY)
        _flight(g, lod, -0.18, -1.72, 3.66, _MY, _UY)
        _landing(g, _LX0, _LX1, _UY, _MZ0, _MZ1)
        _brackets(g, _LX0, _LX1, _UY)
        _rails(g, _LX0, _LX1, _MZ0, _MZ1, _UY)


@register
def create():
    a = Asset(
        "WalkUp",
        "Buildings",
        "Brick walk-up, 8.0 x 6.4 m, three stories, parapet at 9.5 m. Brick is 13 courses per metre. "
        "Windows are recessed 13 cm with brick returns, a stone sill, and a lintel. "
        "A concrete corona projects past each wall under the parapet cap. Stoop and canopy on +Z. "
        "Fire-escape landings at 3.95 m and 6.85 m with a mid landing, switchback stairs, and a drop ladder. "
        "Rails are 1.02 m above each deck. Brackets tie the landings back to the brick.",
    )
    a.loose_pivot = True
    a.climbable = True
    a.vaultable = True
    a.vault_height = 1.02
    a.climb_note = "Brick walls are cling. Glass is solid. The escapes are the steel landings on +Z."
    a.vault_note = "Escape rail is 1.02 m above the lower landing (deck at 3.95 m, rail top at 4.97 m)."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        g.box((0, BODY_H * 0.5, -0.08), (W, BODY_H, 6.24), "Lib_Brick", bevel=bev, segs=1, uv_scale=BRICK_UV)
        _facade(g, lod)
        _cornice(g)
        _entry(g, lod)
        _escape(g, lod)
        if lod == 0:
            g.box((1.2, BODY_H + 0.55, -0.6), (0.80, 0.46, 0.60), "Lib_Steel")
        a.end()
    a.box("Col_Body", (0, 4.55, -0.08), (7.70, 8.80, 5.90))
    a.box("Climb_Front", (0, 4.4, 2.96), (7.2, 8.0, 0.08))
    a.box("Climb_Back", (0, 4.4, -3.12), (7.2, 8.0, 0.08))
    a.box("Climb_SideL", (-3.88, 4.4, -0.08), (0.08, 8.0, 5.6))
    a.box("Climb_SideR", (3.88, 4.4, -0.08), (0.08, 8.0, 5.6))
    a.box("Col_Stoop", (0, 0.09, 3.62), (1.55, 0.12, 0.60))
    a.box("Col_Canopy", (0, 2.52, 3.66), (1.60, 0.05, 0.62))
    a.box("Col_Cornice", (0, 9.48, -0.06), (8.10, 0.12, 6.40))
    a.box("Col_CoronaF", (0, 9.28, 3.39), (7.70, 0.08, 0.14))
    a.box("Col_CoronaB", (0, 9.28, -3.39), (7.70, 0.08, 0.14))
    a.box("Col_CoronaR", (4.21, 9.28, 0.0), (0.12, 0.08, 6.10))
    a.box("Col_CoronaL", (-4.21, 9.28, 0.0), (0.12, 0.08, 6.10))
    a.box("Col_Deck_0", ((_LX0 + _LX1) * 0.5, _LY - 0.018, (_MZ0 + _MZ1) * 0.5), (1.22, 0.018, 0.68))
    a.capsule("Vault_Rail_0", ((_LX0 + _LX1) * 0.5, _LY + 1.005, _MZ1 - 0.045), 0.012, 1.05, 0)
    a.box("Col_Deck_M", ((_MX0 + _MX1) * 0.5, _MY - 0.018, (_MZ0 + _MZ1) * 0.5), (1.10, 0.018, 0.68))
    a.box("Col_Deck_1", ((_LX0 + _LX1) * 0.5, _UY - 0.018, (_MZ0 + _MZ1) * 0.5), (1.22, 0.018, 0.68))
    a.capsule("Vault_Rail_1", ((_LX0 + _LX1) * 0.5, _UY + 1.005, _MZ1 - 0.045), 0.012, 1.05, 0)
    return a
