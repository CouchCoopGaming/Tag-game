"""Harbor work boat. Cabin amidships, bow to -Z, gunwale near 1.0 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Boat",
        "Harbor",
        "Work boat, 6.4 m long, 2.1 m across the stern, gunwale at 1.02 m, cabin roof at 2.15 m. Bow is -Z. Cleats on the gunwales.",
    )
    a.climb_note = "The hull is a solid prop. Not a cling wall."
    a.vault_note = "Gunwale is about 1.0 m. Not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0
        seg = lod_pick(lod, 10, 8)
        g.box((0, 0.14, 0.05), (1.15, 0.22, 4.85), "Lib_PaintBlue", bevel=bev, segs=1, uv_scale=1.0)
        g.box((-0.78, 0.58, 0.12), (0.07, 0.74, 4.90), "Lib_PaintWhite", euler=(0, -3.5, 0), uv_scale=1.0)
        g.box((0.78, 0.58, 0.12), (0.07, 0.74, 4.90), "Lib_PaintWhite", euler=(0, 3.5, 0), uv_scale=1.0)
        g.box((-0.92, 1.04, 0.12), (0.055, 0.06, 4.90), "Lib_WoodDark", euler=(0, -3.5, 0))
        g.box((0.92, 1.04, 0.12), (0.055, 0.06, 4.90), "Lib_WoodDark", euler=(0, 3.5, 0))
        g.box((0, 0.55, 3.02), (1.62, 0.88, 0.07), "Lib_PaintWhite")
        g.box((0, 0.48, -2.78), (0.55, 0.72, 0.48), "Lib_PaintWhite")
        g.box((0, 0.84, 0.15), (1.28, 0.055, 4.35), "Lib_Wood", uv_scale=1.1)
        # Cabin sits above the deck with a gap so the two skins do not weld.
        g.box((0, 1.50, 0.45), (1.22, 1.05, 1.70), "Lib_PaintWhite")
        g.box((0, 2.12, 0.45), (1.40, 0.07, 1.90), "Lib_PaintCream")
        g.box((0.628, 1.58, 0.45), (0.014, 0.28, 0.72), "Lib_Window")
        g.box((-0.628, 1.58, 0.45), (0.014, 0.28, 0.72), "Lib_Window")
        g.box((0, 1.58, -0.415), (0.52, 0.28, 0.014), "Lib_Window")
        if lod == 0:
            g.box((0, 1.22, 1.36), (0.42, 0.55, 0.028), "Lib_WoodDark")
            g.cylinder((0.32, 2.85, -0.7), 0.028, 0.95, "Lib_Steel", seg)
            g.box((0, 1.12, 2.05), (0.72, 0.42, 0.55), "Lib_Wood")
            for x, z in ((-0.52, -1.45), (0.52, -1.45), (-0.52, 1.70), (0.52, 1.70)):
                g.box((x, 0.92, z), (0.12, 0.045, 0.06), "Lib_Steel")
                g.box((x, 0.97, z), (0.045, 0.045, 0.035), "Lib_SteelDark")
        a.end()
    a.box("Col_Keel", (0, 0.14, 0.05), (1.00, 0.16, 4.60))
    a.box("Col_SideL", (-0.78, 0.58, 0.12), (0.045, 0.60, 4.55), euler=(0, -3.5, 0))
    a.box("Col_SideR", (0.78, 0.58, 0.12), (0.045, 0.60, 4.55), euler=(0, 3.5, 0))
    a.box("Col_Transom", (0, 0.55, 3.02), (1.46, 0.74, 0.045))
    a.box("Col_Bow", (0, 0.48, -2.78), (0.42, 0.58, 0.36))
    a.box("Col_Deck", (0, 0.84, 0.15), (1.12, 0.035, 4.05))
    a.box("Col_Cabin", (0, 1.50, 0.45), (1.08, 0.90, 1.50))
    a.box("Col_CabinRoof", (0, 2.12, 0.45), (1.22, 0.045, 1.70))
    return a
