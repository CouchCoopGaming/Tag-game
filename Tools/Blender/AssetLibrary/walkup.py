"""Three-storey brick walk-up. Door and fire escapes face +Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

W = 8.0
D = 6.4
BODY_H = 9.30
FACE = D * 0.5


def _front_windows(g, lod, xs, ys):
    for y in ys:
        for x in xs:
            g.box((x, y, FACE + 0.020), (0.70, 1.00, 0.022), "Lib_ShopGlass")
            g.box((x, y - 0.58, FACE + 0.024), (0.84, 0.05, 0.028), "Lib_PaintCream")
            if lod == 0:
                g.box((x, y, FACE + 0.040), (0.70, 0.028, 0.012), "Lib_PaintCream")
                g.box((x, y, FACE + 0.040), (0.028, 1.00, 0.012), "Lib_PaintCream")


def _escape(g, lod, deck_y, x_mid):
    """Slats, a rail, and a ladder just past the deck. Nothing shares a volume."""
    z0 = FACE + 0.12
    slats = lod_pick(lod, 5, 3)
    for i in range(slats):
        z = z0 + i * 0.16
        g.box((x_mid, deck_y, z), (1.10, 0.04, 0.09), "Lib_Steel")
    g.box((x_mid, deck_y - 0.045, z0 + 0.32), (1.20, 0.028, 0.07), "Lib_SteelDark")
    for x in (x_mid - 0.52, x_mid + 0.52):
        g.box((x, deck_y + 0.55, z0 + 0.64), (0.04, 0.94, 0.04), "Lib_SteelDark")
    g.box((x_mid, deck_y + 1.05, z0 + 0.64), (1.08, 0.032, 0.032), "Lib_Steel")
    lz = z0 + 0.16 * (slats - 1) + 0.16
    top = deck_y - 0.05
    bot = 0.34 if deck_y < 4.0 else deck_y - 2.70
    for x in (x_mid - 0.20, x_mid + 0.20):
        g.pipe((x, bot, lz), (x, top, lz), 0.016, "Lib_SteelDark", 6)
    rungs = lod_pick(lod, 5, 3)
    for i in range(rungs):
        y = bot + (i + 1) * ((top - bot) / (rungs + 1))
        g.pipe((x_mid - 0.20, y, lz), (x_mid + 0.20, y, lz), 0.01, "Lib_Steel", 5)
    if deck_y < 4.0:
        g.box((x_mid, 0.14, lz), (0.52, 0.28, 0.22), "Lib_Concrete")


@register
def create():
    a = Asset(
        "WalkUp",
        "Buildings",
        "Brick walk-up, 8.0 x 6.4 m, three stories, parapet at 9.5 m. Door and two fire-escape landings face +Z. Landings at 3.15 m and 6.20 m, rail 1.05 m above each deck.",
    )
    a.loose_pivot = True
    a.climbable = True
    a.vaultable = True
    a.vault_height = 1.05
    a.climb_note = "Brick walls are cling. Glass is solid. The escapes are the steel landings on +Z."
    a.vault_note = "Escape rail is 1.05 m above the lower landing (deck at 3.15 m, rail top at 4.20 m)."
    x_mid = -2.35
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0
        g.box((0, BODY_H * 0.5, 0), (W, BODY_H, D), "Lib_Brick", bevel=bev, segs=1, uv_scale=0.7)
        g.box((0, BODY_H + 0.12, 0), (W + 0.24, 0.16, D + 0.18), "Lib_PaintCream", uv_scale=0.8)
        g.box((0, 1.22, FACE + 0.040), (0.96, 2.04, 0.04), "Lib_WoodDark")
        g.box((0, 0.08, FACE + 0.41), (1.46, 0.14, 0.78), "Lib_Concrete")
        xs = lod_pick(lod, (-3.0, -1.5, 1.5, 3.0), (-2.3, 2.3), (0.0,))
        floors = (1.70, 4.60, 7.50) if lod < 2 else (4.60,)
        _front_windows(g, lod, xs, floors)
        if lod == 0:
            for y, z in ((4.60, 0.6), (7.50, -0.7)):
                g.box((W * 0.5 + 0.020, y, z), (0.022, 1.00, 0.70), "Lib_ShopGlass")
                g.box((-(W * 0.5 + 0.020), y, -z), (0.022, 1.00, 0.70), "Lib_ShopGlass")
        _escape(g, lod, 3.15, x_mid)
        _escape(g, lod, 6.20, x_mid)
        if lod == 0:
            g.box((1.1, BODY_H + 0.52, -0.5), (0.85, 0.52, 0.65), "Lib_Steel")
        a.end()
    a.box("Col_Body", (0, BODY_H * 0.5, 0), (W - 0.16, BODY_H - 0.16, D - 0.16))
    a.box("Climb_Front", (0, 4.4, FACE - 0.08), (W - 0.6, 8.0, 0.08))
    a.box("Climb_Back", (0, 4.4, -(FACE - 0.08)), (W - 0.6, 8.0, 0.08))
    a.box("Climb_SideL", (-(W * 0.5 - 0.08), 4.4, 0), (0.08, 8.0, D - 0.6))
    a.box("Climb_SideR", (W * 0.5 - 0.08, 4.4, 0), (0.08, 8.0, D - 0.6))
    a.box("Col_Stoop", (0, 0.08, FACE + 0.41), (1.30, 0.10, 0.64))
    a.box("Col_Cornice", (0, BODY_H + 0.12, 0), (W + 0.08, 0.10, D + 0.04))
    for i, deck_y in enumerate((3.15, 6.20)):
        a.box("Col_Deck_%d" % i, (x_mid, deck_y - 0.045, FACE + 0.44), (1.05, 0.02, 0.05))
        a.capsule("Vault_Rail_%d" % i, (x_mid, deck_y + 1.05, FACE + 0.76), 0.012, 0.90, 0)
    a.box("Col_Pier", (x_mid, 0.14, FACE + 0.92), (0.40, 0.22, 0.16))
    return a
