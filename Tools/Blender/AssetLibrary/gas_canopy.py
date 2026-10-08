"""Gas station. Canopy, pumps, MART shop, price sign, bollards, and an ICE box."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "GasCanopy",
        "Buildings",
        "Fuel canopy 9.0 x 6.8 m, roof at 3.50 m with a fascia, two pumps and bollards. "
        "MART shop on -Z, price sign on +X, ICE machine beside the shop. FUEL sign faces +Z.",
    )
    a.loose_pivot = True
    a.climbable = True
    a.climb_note = "Columns are 22 cm. The shop walls are cling."
    a.vault_note = "No rail. The roof is at 3.50 m. Shop wall top is 3.15 m."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        bev = 0.004 if lod == 0 else 0
        g.box((0, 0.03, 0), (8.4, 0.06, 6.2), "Lib_Asphalt", uv_scale=0.5)
        for x in (-1.05, 1.05):
            g.box((x, 0.070, 0), (0.08, 0.008, 4.4), "Lib_PaintWhite")
        for x in (-3.4, 3.4):
            for z in (-2.35, 2.35):
                g.box((x, 1.72, z), (0.22, 3.28, 0.22), "Lib_PaintWhite", bevel=bev, segs=1)
                g.box((x + 0.126, 2.20, z), (0.016, 0.22, 0.16), "Lib_PaintRed")
                g.box((x - 0.126, 2.20, z), (0.016, 0.22, 0.16), "Lib_PaintRed")
                g.box((x, 2.20, z + 0.126), (0.16, 0.22, 0.016), "Lib_PaintRed")
                g.box((x, 2.20, z - 0.126), (0.16, 0.22, 0.016), "Lib_PaintRed")
        g.box((0, 3.46, 0), (9.0, 0.16, 6.8), "Lib_Steel", bevel=bev, segs=1, uv_scale=0.6)
        g.box((0, 3.58, 0), (8.55, 0.04, 6.35), "Lib_PaintWhite")
        g.box((0, 3.22, 3.32), (8.55, 0.20, 0.05), "Lib_PaintWhite")
        g.box((0, 3.22, -3.32), (8.55, 0.20, 0.05), "Lib_PaintWhite")
        g.box((-4.42, 3.22, 0), (0.05, 0.20, 6.40), "Lib_PaintWhite")
        g.box((4.42, 3.22, 0), (0.05, 0.20, 6.40), "Lib_PaintWhite")
        g.box((0, 3.22, 3.37), (8.40, 0.06, 0.02), "Lib_PaintRed")
        g.box((0, 3.55, 3.48), (2.5, 0.52, 0.04), "Lib_PaintRed")
        if lod == 0:
            g.text("FUEL", (0, 3.55, 3.52), 0.32, "Lib_PaintWhite", extrude=0.008, yaw=0.0)
        for x in (-1.6, 1.6):
            g.box((x, 0.70, 0.10), (0.56, 1.24, 0.36), "Lib_PaintWhite", bevel=bev, segs=1)
            g.box((x, 1.02, 0.31), (0.50, 0.16, 0.02), "Lib_PaintRed")
            g.box((x, 0.72, 0.31), (0.24, 0.18, 0.016), "Lib_Black")
            if lod == 0:
                g.box((x + 0.34, 0.90, 0.10), (0.08, 0.24, 0.08), "Lib_SteelDark")
                g.cylinder((x, 1.355, 0.10), 0.07, 0.05, "Lib_Steel", seg)
        for x in (-1.6, 1.6):
            for z in (0.95, -0.75):
                g.cylinder((x, 0.42, z), 0.055, 0.68, "Lib_Steel", seg)
                g.sphere((x, 0.84, z), 0.055, "Lib_Steel", max(6, seg))
                g.torus((x, 0.55, z), 0.072, 0.008, "Lib_PaintYellow", 8, 4)
        _shop(g, lod, bev, seg)
        a.end()
    a.box("Col_Pad", (0, 0.03, 0), (8.2, 0.04, 6.0))
    for i, x in enumerate((-3.4, 3.4)):
        for j, z in enumerate((-2.35, 2.35)):
            a.box("Col_Post_%d%d" % (i, j), (x, 1.72, z), (0.16, 3.12, 0.16))
    a.box("Col_Roof", (0, 3.46, 0), (8.7, 0.12, 6.5))
    for i, x in enumerate((-1.6, 1.6)):
        a.box("Col_Pump_%d" % i, (x, 0.70, 0.10), (0.46, 1.12, 0.28))
    for i, (x, z) in enumerate(((-1.6, 0.95), (-1.6, -0.75), (1.6, 0.95), (1.6, -0.75))):
        a.capsule("Col_Bollard_%d" % i, (x, 0.42, z), 0.045, 0.58, 1)
    a.box("Climb_ShopBack", (0, 1.60, -8.15), (5.4, 2.7, 0.06))
    a.box("Climb_ShopSideL", (-2.95, 1.60, -6.35), (0.06, 2.7, 3.2))
    a.box("Climb_ShopSideR", (2.95, 1.60, -6.35), (0.06, 2.7, 3.2))
    a.box("Col_ShopFront", (0, 1.60, -4.72), (5.4, 2.7, 0.08))
    a.box("Col_Ice", (3.55, 0.70, -5.6), (0.55, 1.15, 0.40))
    a.capsule("Col_Price", (5.15, 1.45, 1.6), 0.05, 2.5, 1)
    return a


def _shop(g, lod, bev, seg):
    """Convenience store behind the canopy. Front faces +Z, toward the pumps."""
    g.box((0, 0.04, -6.4), (7.2, 0.08, 4.6), "Lib_Concrete", uv_scale=0.5)
    g.box((0, 1.62, -6.40), (6.2, 3.05, 3.6), "Lib_Siding", bevel=bev, segs=1, uv_scale=0.8)
    g.box((0, 3.22, -6.40), (6.5, 0.10, 3.9), "Lib_Roof", uv_scale=0.7)
    g.box((0.4, 1.15, -4.52), (0.96, 2.05, 0.04), "Lib_WoodDark")
    g.box((-1.5, 1.55, -4.52), (1.15, 0.85, 0.02), "Lib_ShopGlass")
    g.box((0, 2.85, -4.48), (2.4, 0.42, 0.04), "Lib_PaintRed")
    if lod == 0:
        g.text("MART", (0, 2.85, -4.42), 0.28, "Lib_PaintWhite")
    g.box((3.55, 0.72, -5.6), (0.70, 1.28, 0.52), "Lib_PaintWhite", bevel=bev, segs=1)
    g.box((3.55, 1.05, -5.28), (0.55, 0.28, 0.02), "Lib_PaintRed")
    if lod == 0:
        g.text("ICE", (3.55, 1.05, -5.24), 0.16, "Lib_PaintWhite")
    g.cylinder((5.15, 1.45, 1.60), 0.06, 2.70, "Lib_Steel", seg)
    g.box((5.15, 2.85, 1.74), (0.72, 0.85, 0.05), "Lib_PaintWhite")
    g.box((5.15, 2.85, 1.80), (0.60, 0.68, 0.02), "Lib_PaintRed")
    if lod == 0:
        g.text("3.19", (5.15, 2.95, 1.84), 0.14, "Lib_PaintWhite")
