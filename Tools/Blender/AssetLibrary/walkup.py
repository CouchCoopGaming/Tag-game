"""Three-storey brick walk-up. Door and fire escapes face +Z.

The baked brick tile is 8 courses per UV metre. uv 1.625 puts 13 courses
on each world metre, about 38 courses on a 2.9 m floor.
"""

import math
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
    """A thin coping on the parapet, and a corona that mitres around every corner."""
    # 4 mm above the brick top so the two shells do not weld. 5.6 cm tall, not a lid.
    y, h = 9.332, 0.056
    g.box((0, y, 3.14), (8.24, h, 0.18), "Lib_Concrete", uv_scale=0.5)
    g.box((0, y, -3.14), (8.24, h, 0.18), "Lib_Concrete", uv_scale=0.5)
    g.box((3.94, y, 0.0), (0.18, h, 6.08), "Lib_Concrete", uv_scale=0.5)
    g.box((-3.94, y, 0.0), (0.18, h, 6.08), "Lib_Concrete", uv_scale=0.5)
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


# One landing per floor, each spanning two windows. Glass is ±0.35 m around
# x = -3, -1.5, 1.5, 3. Sills end at z=3.39, so the decks start at z=3.42.
# The stair occupies the solid pier between the pairs (x -1.16 to 1.16).
# Rise 2.90 m over a 2.30 m run is about 51°, the slope that pier allows.
LO_X0, LO_X1 = -3.90, -1.16
HI_X0, HI_X1 = 1.16, 3.90
_Z0, _Z1 = 3.42, 4.22
LO_Y, HI_Y = 4.02, 6.92
_RAIL_Z = 4.175


def _yz_plate(g, x, y0, z0, y1, z1, width, thick, mat):
    """Flat bar in a YZ plane. Width is across the bar, thickness is along X."""
    dy, dz = y1 - y0, z1 - z0
    length = math.hypot(dy, dz)
    if length < 0.04:
        return
    uy, uz = dy / length, dz / length
    # Perpendicular in YZ, pointing below the bar.
    py, pz = uz, -uy
    hw, ht = width * 0.5, thick * 0.5
    ring = (
        (y0 + py * hw, z0 + pz * hw),
        (y1 + py * hw, z1 + pz * hw),
        (y1 - py * hw, z1 - pz * hw),
        (y0 - py * hw, z0 - pz * hw),
    )
    verts = [(x - ht, y, z) for y, z in ring] + [(x + ht, y, z) for y, z in ring]
    g.mesh(verts, (
        (0, 3, 2, 1), (4, 5, 6, 7),
        (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7),
    ), mat)


def _slope_bar(g, x0, y0, x1, y1, z, depth, thick, mat):
    """Stringer. The top edge is the given line; the plate hangs below it."""
    dx, dy = x1 - x0, y1 - y0
    length = math.hypot(dx, dy)
    if length < 0.04:
        return
    ux, uy = dx / length, dy / length
    px, py = uy, -ux
    ht = thick * 0.5
    ring = (
        (x0, y0),
        (x1, y1),
        (x1 + px * depth, y1 + py * depth),
        (x0 + px * depth, y0 + py * depth),
    )
    verts = [(x, y, z - ht) for x, y in ring] + [(x, y, z + ht) for x, y in ring]
    g.mesh(verts, (
        (0, 3, 2, 1), (4, 5, 6, 7),
        (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7),
    ), mat)


def _landing(g, x0, x1, y_top):
    g.box(((x0 + x1) * 0.5, y_top - 0.020, (_Z0 + _Z1) * 0.5), (x1 - x0, 0.040, _Z1 - _Z0), "Lib_SteelDark")


def _brackets(g, x0, x1, y_top):
    """A bearer under the deck and a flat bracket bolted into the brick below the sill."""
    for x in (x0 + 0.22, (x0 + x1) * 0.5, x1 - 0.22):
        g.box((x, y_top - 0.055, 3.82), (0.032, 0.014, 0.72), "Lib_SteelDark")
        g.box((x, y_top - 0.78, 3.208), (0.064, 0.080, 0.008), "Lib_SteelDark")
        _yz_plate(g, x, y_top - 0.78, 3.240, y_top - 0.14, 3.55, 0.036, 0.010, "Lib_SteelDark")


def _street_rail(g, x0, x1, y_top, outer_x):
    """Guard on the street edge and the outer end. Nothing on the wall side."""
    y_r = y_top + 1.00
    y_m = y_top + 0.50
    z_post = 4.145
    for x in (x0 + 0.08, (x0 + x1) * 0.5, x1 - 0.08):
        g.box((x, y_top + 0.49, z_post), (0.024, 0.94, 0.022), "Lib_SteelDark")
    g.pipe((x0 + 0.10, y_r, _RAIL_Z), (x1 - 0.10, y_r, _RAIL_Z), 0.015, "Lib_Steel", 5)
    g.pipe((x0 + 0.10, y_m, _RAIL_Z), (x1 - 0.10, y_m, _RAIL_Z), 0.012, "Lib_Steel", 5)
    # Return at the end away from the stair, over the corner pier rather than a window.
    z_a, z_b = 3.56, 4.12
    g.box((outer_x, y_top + 0.49, (z_a + z_b) * 0.5), (0.022, 0.94, 0.024), "Lib_SteelDark")
    g.pipe((outer_x, y_r, z_a), (outer_x, y_r, z_b), 0.015, "Lib_Steel", 5)
    g.pipe((outer_x, y_m, z_a), (outer_x, y_m, z_b), 0.012, "Lib_Steel", 5)


def _flight(g, lod):
    """Open stair along the wall. A stringer on each side, treads with real depth."""
    x0, x1 = LO_X1 + 0.016, HI_X0 - 0.016
    y0, y1 = LO_Y, HI_Y
    run = x1 - x0
    rise = y1 - y0
    drop = 0.042
    z_wall, z_street = 3.52, 4.08
    for z in (z_wall, z_street):
        _slope_bar(g, x0, y0 - drop, x1, y1 - drop, z, 0.075, 0.016, "Lib_SteelDark")
    n = lod_pick(lod, 12, 7)
    going = run / float(n)
    riser = rise / float(n)
    tz0, tz1 = z_wall + 0.022, z_street - 0.022
    for i in range(n):
        x_front = x0 + (i + 1) * going
        x_back = x_front - going * 0.84
        y_top = y0 + (i + 1) * riser
        g.box(
            ((x_back + x_front) * 0.5, y_top - 0.016, (tz0 + tz1) * 0.5),
            (x_front - x_back, 0.032, tz1 - tz0),
            "Lib_Steel",
        )
    g.pipe((x0, y0 + 1.00, _RAIL_Z), (x1, y1 + 1.00, _RAIL_Z), 0.015, "Lib_Steel", 5)
    g.pipe((x0, y0 + 0.50, _RAIL_Z), (x1, y1 + 0.50, _RAIL_Z), 0.012, "Lib_Steel", 5)


def _drop_ladder(g, lod):
    """Hangs from the outer end of the lower landing, over the pier, not a window."""
    xs = (-3.78, -3.48)
    zs = 3.78
    for x in xs:
        g.pipe((x, 0.36, zs), (x, LO_Y - 0.088, zs), 0.016, "Lib_SteelDark", 6)
    g.pipe((xs[0] + 0.026, LO_Y - 0.072, zs), (xs[1] - 0.026, LO_Y - 0.072, zs), 0.011, "Lib_SteelDark", 5)
    g.box((-3.63, LO_Y - 0.050, zs), (0.22, 0.008, 0.032), "Lib_SteelDark")
    rungs = lod_pick(lod, 8, 4)
    span = (LO_Y - 0.20) - 0.55
    for i in range(rungs):
        y = 0.55 + span * (i + 0.5) / float(rungs)
        g.pipe((xs[0] + 0.028, y, zs), (xs[1] - 0.028, y, zs), 0.011, "Lib_Steel", 5)


def _escape(g, lod):
    """A landing outside each upper floor, a stair along the wall, and a drop ladder."""
    _landing(g, LO_X0, LO_X1, LO_Y)
    _brackets(g, LO_X0, LO_X1, LO_Y)
    _street_rail(g, LO_X0, LO_X1, LO_Y, LO_X0 + 0.08)
    _drop_ladder(g, lod)
    if lod < 2:
        _landing(g, HI_X0, HI_X1, HI_Y)
        _brackets(g, HI_X0, HI_X1, HI_Y)
        _street_rail(g, HI_X0, HI_X1, HI_Y, HI_X1 - 0.08)
        _flight(g, lod)


@register
def create():
    a = Asset(
        "WalkUp",
        "Buildings",
        "Brick walk-up, 8.0 x 6.4 m, three stories, parapet at 9.5 m. Brick is 13 courses per metre. "
        "Windows are recessed 13 cm with brick returns, a stone sill, and a lintel. "
        "A thin concrete coping sits on the brick parapet, and a corona projects under it. Stoop and canopy on +Z. "
        "One fire-escape landing per upper floor, each spanning two windows, at 4.02 m and 6.92 m. "
        "A stair with a stringer on both sides runs along the wall between them, and a drop ladder hangs from the lower landing. "
        "Rails are 1.02 m above each deck, on the street edge. Brackets bolt into the brick under each landing.",
    )
    a.loose_pivot = True
    a.climbable = True
    a.vaultable = True
    a.vault_height = 1.02
    a.climb_note = "Brick walls are cling. Glass is solid. The escapes are the steel landings on +Z."
    a.vault_note = "Escape rail is 1.02 m above the lower landing (deck at 4.02 m, rail top at 5.04 m)."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        g.box((0, BODY_H * 0.5, -0.08), (W, BODY_H, 6.24), "Lib_Brick", bevel=bev, segs=1, uv_scale=BRICK_UV)
        _facade(g, lod)
        _cornice(g)
        _entry(g, lod)
        _escape(g, lod)
        if lod == 0:
            g.box((1.2, 9.54, -0.6), (0.80, 0.46, 0.60), "Lib_Steel")
        a.end()
    a.box("Col_Body", (0, 4.55, -0.08), (7.70, 8.80, 5.90))
    a.box("Climb_Front", (0, 4.4, 2.96), (7.2, 8.0, 0.08))
    a.box("Climb_Back", (0, 4.4, -3.12), (7.2, 8.0, 0.08))
    a.box("Climb_SideL", (-3.88, 4.4, -0.08), (0.08, 8.0, 5.6))
    a.box("Climb_SideR", (3.88, 4.4, -0.08), (0.08, 8.0, 5.6))
    a.box("Col_Stoop", (0, 0.09, 3.62), (1.55, 0.12, 0.60))
    a.box("Col_Canopy", (0, 2.52, 3.66), (1.60, 0.05, 0.62))
    a.box("Col_Roof", (0, 9.22, -0.04), (7.50, 0.10, 5.70))
    a.box("Col_CopeF", (0, 9.332, 3.14), (8.08, 0.036, 0.12))
    a.box("Col_CopeB", (0, 9.332, -3.14), (8.08, 0.036, 0.12))
    a.box("Col_CopeR", (3.94, 9.332, 0.0), (0.12, 0.036, 5.92))
    a.box("Col_CopeL", (-3.94, 9.332, 0.0), (0.12, 0.036, 5.92))
    a.box("Col_CoronaF", (0, 9.28, 3.39), (7.70, 0.08, 0.14))
    a.box("Col_CoronaB", (0, 9.28, -3.39), (7.70, 0.08, 0.14))
    a.box("Col_CoronaR", (4.21, 9.28, 0.0), (0.12, 0.08, 6.10))
    a.box("Col_CoronaL", (-4.21, 9.28, 0.0), (0.12, 0.08, 6.10))
    a.box("Col_Deck_0", ((LO_X0 + LO_X1) * 0.5, LO_Y - 0.020, (_Z0 + _Z1) * 0.5), (LO_X1 - LO_X0 - 0.10, 0.022, _Z1 - _Z0 - 0.10))
    a.capsule("Vault_Rail_0", ((LO_X0 + LO_X1) * 0.5, LO_Y + 1.00, _RAIL_Z), 0.008, (LO_X1 - LO_X0) - 0.36, 0)
    a.box("Col_Deck_1", ((HI_X0 + HI_X1) * 0.5, HI_Y - 0.020, (_Z0 + _Z1) * 0.5), (HI_X1 - HI_X0 - 0.10, 0.022, _Z1 - _Z0 - 0.10))
    a.capsule("Vault_Rail_1", ((HI_X0 + HI_X1) * 0.5, HI_Y + 1.00, _RAIL_Z), 0.008, (HI_X1 - HI_X0) - 0.36, 0)
    return a
