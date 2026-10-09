"""Three-storey brick walk-up. Door and fire escapes face +Z.

The baked brick tile is 8 courses per UV metre. uv 1.625 puts 13 courses
on each world metre, about 38 courses on a 2.9 m floor.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

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
        g.box((x, y, 3.055), (0.70, 1.00, 0.016), "Lib_ShopGlass")
        g.box((x, y - 0.54, 3.18), (0.74, 0.05, 0.16), "Lib_PaintCream")
        g.box((x, y + 0.54, 3.16), (0.74, 0.045, 0.10), "Lib_Concrete")
        if lod == 0:
            g.box((x, y, 3.078), (0.70, 0.02, 0.012), "Lib_PaintCream")
            g.box((x, y, 3.078), (0.02, 0.96, 0.012), "Lib_PaintCream")
    g.box((0, 1.16, 3.10), (0.92, 2.00, 0.03), "Lib_WoodDark")
    if lod == 0:
        g.box((0, 2.22, 3.055), (0.70, 0.10, 0.016), "Lib_ShopGlass")
        g.box((0, 1.16, 3.078), (0.86, 0.018, 0.012), "Lib_PaintCream")


def _cornice(g):
    g.box((0, 9.48, -0.06), (8.46, 0.18, 6.78), "Lib_PaintCream", uv_scale=0.6)
    g.box((0, 9.70, -0.06), (8.22, 0.12, 6.54), "Lib_Concrete", uv_scale=0.5)


def _entry(g, lod):
    g.box((0, 0.09, 3.62), (1.70, 0.16, 0.72), "Lib_Concrete", uv_scale=0.8)
    g.box((0, 0.045, 4.16), (1.95, 0.07, 0.32), "Lib_Concrete", uv_scale=0.8)
    g.box((0, 2.52, 3.66), (1.85, 0.08, 0.78), "Lib_PaintCream", uv_scale=0.7)
    if lod == 0:
        for x in (-0.72, 0.72):
            g.pipe((x, 2.16, 3.28), (x, 2.42, 3.70), 0.016, "Lib_SteelDark", 6)


def _escape(g, lod, deck_y):
    """Landing, rail, ladder, and two brackets clear of the brick skin."""
    z0 = 3.36
    slats = lod_pick(lod, 6, 3)
    for i in range(slats):
        z = z0 + i * 0.13
        g.box((X_ESC, deck_y, z), (1.20, 0.035, 0.07), "Lib_Steel")
    g.box((X_ESC, deck_y - 0.05, z0 + 0.34), (1.28, 0.03, 0.08), "Lib_SteelDark")
    for x in (X_ESC - 0.56, X_ESC + 0.56):
        g.box((x, deck_y + 0.52, z0 + 0.70), (0.035, 0.90, 0.035), "Lib_SteelDark")
        g.box((x, deck_y - 0.12, 3.25), (0.16, 0.05, 0.04), "Lib_SteelDark")
        g.pipe((x, deck_y - 0.08, 3.30), (x, deck_y - 0.08, z0 + 0.62), 0.016, "Lib_Steel", 5)
        g.pipe((x, deck_y - 0.48, 3.28), (x, deck_y - 0.10, z0 + 0.55), 0.016, "Lib_SteelDark", 5)
    g.box((X_ESC, deck_y + 1.02, z0 + 0.70), (1.16, 0.028, 0.028), "Lib_Steel")
    lz = z0 + 0.13 * (slats - 1) + 0.14
    top = deck_y - 0.06
    bot = 0.36 if deck_y < 5.0 else deck_y - 2.70
    for x in (X_ESC - 0.18, X_ESC + 0.18):
        g.pipe((x, bot, lz), (x, top, lz), 0.015, "Lib_SteelDark", 6)
    rungs = lod_pick(lod, 5, 3)
    for i in range(rungs):
        y = bot + (i + 1) * ((top - bot) / (rungs + 1))
        g.pipe((X_ESC - 0.18, y, lz), (X_ESC + 0.18, y, lz), 0.01, "Lib_Steel", 5)
    if deck_y < 5.0:
        g.box((X_ESC, 0.14, lz), (0.48, 0.26, 0.18), "Lib_Concrete")


@register
def create():
    a = Asset(
        "WalkUp",
        "Buildings",
        "Brick walk-up, 8.0 x 6.4 m, three stories, parapet at 9.5 m. Brick is 13 courses per metre. "
        "Windows are recessed 13 cm with sills and lintels. Stoop and canopy on +Z. "
        "Fire-escape landings at 3.95 m and 6.85 m, brackets back to the wall, rail 1.05 m above each deck.",
    )
    a.loose_pivot = True
    a.climbable = True
    a.vaultable = True
    a.vault_height = 1.05
    a.climb_note = "Brick walls are cling. Glass is solid. The escapes are the steel landings on +Z."
    a.vault_note = "Escape rail is 1.05 m above the lower landing (deck at 3.95 m, rail top at 5.00 m)."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        g.box((0, BODY_H * 0.5, -0.08), (W, BODY_H, 6.24), "Lib_Brick", bevel=bev, segs=1, uv_scale=BRICK_UV)
        _facade(g, lod)
        _cornice(g)
        _entry(g, lod)
        _escape(g, lod, 3.95)
        if lod < 2:
            _escape(g, lod, 6.85)
        if lod == 0:
            # Above the parapet slab (top 9.76) so the two shells do not overlap.
            g.box((1.2, 9.93, -0.6), (0.72, 0.30, 0.52), "Lib_Steel")
        a.end()
    a.box("Col_Body", (0, 4.55, -0.08), (7.70, 8.80, 5.90))
    a.box("Climb_Front", (0, 4.4, 2.96), (7.2, 8.0, 0.08))
    a.box("Climb_Back", (0, 4.4, -3.12), (7.2, 8.0, 0.08))
    a.box("Climb_SideL", (-3.88, 4.4, -0.08), (0.08, 8.0, 5.6))
    a.box("Climb_SideR", (3.88, 4.4, -0.08), (0.08, 8.0, 5.6))
    a.box("Col_Stoop", (0, 0.09, 3.62), (1.55, 0.12, 0.60))
    a.box("Col_Canopy", (0, 2.52, 3.66), (1.60, 0.05, 0.62))
    a.box("Col_Cornice", (0, 9.48, -0.06), (8.10, 0.12, 6.40))
    # Roof bulkhead. Mesh crown is 10.08 m. This box stays 7 cm under that crown.
    a.box("Col_Bulkhead", (1.2, 9.93, -0.6), (0.48, 0.16, 0.32))
    a.box("Col_Deck_0", (X_ESC, 3.90, 3.70), (1.05, 0.02, 0.08))
    a.capsule("Vault_Rail_0", (X_ESC, 4.97, 4.06), 0.012, 0.95, 0)
    a.box("Col_Deck_1", (X_ESC, 6.80, 3.70), (1.05, 0.02, 0.08))
    a.capsule("Vault_Rail_1", (X_ESC, 7.87, 4.06), 0.012, 0.95, 0)
    a.box("Col_Pier", (X_ESC, 0.14, 4.16), (0.38, 0.20, 0.14))
    return a
