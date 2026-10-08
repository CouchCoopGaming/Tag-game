"""Open fishing skiff. Bow to -Z, gunwale near 0.72 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "FishingBoat",
        "Harbor",
        "Open fishing skiff, 4.2 m long and 1.5 m across, gunwale at 0.72 m. Bow is -Z. Outboard on the stern.",
    )
    a.loose_pivot = True
    a.climb_note = "The hull is a solid prop. Not a cling wall."
    a.vault_note = "Gunwale is 0.72 m. Under the vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        bev = 0.003 if lod == 0 else 0
        g.box((0, 0.12, 0.05), (0.85, 0.18, 3.15), "Lib_PaintWhite", bevel=bev, segs=1)
        g.box((-0.58, 0.42, 0.10), (0.05, 0.48, 3.20), "Lib_PaintWhite", euler=(0, -4.0, 0))
        g.box((0.58, 0.42, 0.10), (0.05, 0.48, 3.20), "Lib_PaintWhite", euler=(0, 4.0, 0))
        g.box((-0.66, 0.70, 0.10), (0.04, 0.045, 3.15), "Lib_Wood", euler=(0, -4.0, 0), uv_scale=1.1)
        g.box((0.66, 0.70, 0.10), (0.04, 0.045, 3.15), "Lib_Wood", euler=(0, 4.0, 0), uv_scale=1.1)
        g.box((0, 0.40, 1.85), (1.15, 0.55, 0.05), "Lib_PaintWhite")
        g.box((0, 0.36, -1.85), (0.42, 0.48, 0.38), "Lib_PaintWhite")
        g.box((0, 0.55, 0.15), (0.95, 0.04, 2.70), "Lib_Wood", uv_scale=1.0)
        g.box((0, 0.64, 0.55), (0.72, 0.04, 0.28), "Lib_WoodDark")
        g.box((0, 0.78, 1.15), (0.34, 0.28, 0.28), "Lib_PaintWhite")
        if lod == 0:
            g.box((0.42, 0.48, 1.72), (0.16, 0.32, 0.22), "Lib_SteelDark")
            g.cylinder((0.42, 0.78, 1.72), 0.06, 0.22, "Lib_Steel", seg)
            g.box((0, 0.86, 1.15), (0.22, 0.08, 0.16), "Lib_Black")
        a.end()
    a.box("Col_Keel", (0, 0.12, 0.05), (0.72, 0.12, 2.90))
    a.box("Col_SideL", (-0.58, 0.42, 0.10), (0.035, 0.38, 2.95), euler=(0, -4.0, 0))
    a.box("Col_SideR", (0.58, 0.42, 0.10), (0.035, 0.38, 2.95), euler=(0, 4.0, 0))
    a.box("Col_Transom", (0, 0.40, 1.85), (1.00, 0.42, 0.035))
    a.box("Col_Bow", (0, 0.36, -1.85), (0.32, 0.36, 0.28))
    a.box("Col_Deck", (0, 0.55, 0.15), (0.82, 0.025, 2.45))
    return a
