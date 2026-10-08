"""Wood rowboat. Bow to -Z. Two seats and a pair of oars."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Rowboat",
        "Harbor",
        "Rowboat, 2.6 m long and 0.95 m across, gunwale at 0.42 m. Bow is -Z. Two seats. Oars rest across the gunwales.",
    )
    a.loose_pivot = True
    a.climb_note = "Too small to cling."
    a.vault_note = "Gunwale is 0.42 m. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        g.box((0, 0.08, 0.0), (0.55, 0.12, 1.85), "Lib_Wood", bevel=bev, segs=1, uv_scale=1.2)
        g.box((-0.36, 0.24, 0.02), (0.035, 0.26, 1.95), "Lib_Wood", euler=(0, -6.0, 0), uv_scale=1.2)
        g.box((0.36, 0.24, 0.02), (0.035, 0.26, 1.95), "Lib_Wood", euler=(0, 6.0, 0), uv_scale=1.2)
        g.box((0, 0.22, 1.05), (0.62, 0.28, 0.04), "Lib_WoodDark")
        g.box((0, 0.20, -1.05), (0.28, 0.24, 0.28), "Lib_WoodDark")
        g.box((-0.40, 0.40, 0.02), (0.025, 0.03, 1.90), "Lib_WoodDark", euler=(0, -6.0, 0))
        g.box((0.40, 0.40, 0.02), (0.025, 0.03, 1.90), "Lib_WoodDark", euler=(0, 6.0, 0))
        g.box((0, 0.28, -0.35), (0.55, 0.025, 0.22), "Lib_WoodDark")
        g.box((0, 0.28, 0.45), (0.55, 0.025, 0.22), "Lib_WoodDark")
        if lod == 0:
            g.box((0, 0.46, 0.05), (1.35, 0.025, 0.06), "Lib_Wood", uv_scale=1.4)
            g.box((0.15, 0.50, 0.05), (1.15, 0.02, 0.045), "Lib_WoodDark", uv_scale=1.4)
        a.end()
    a.box("Col_Keel", (0, 0.08, 0), (0.46, 0.08, 1.65))
    a.box("Col_SideL", (-0.36, 0.24, 0.02), (0.025, 0.18, 1.75), euler=(0, -6.0, 0))
    a.box("Col_SideR", (0.36, 0.24, 0.02), (0.025, 0.18, 1.75), euler=(0, 6.0, 0))
    a.box("Col_Transom", (0, 0.22, 1.05), (0.50, 0.20, 0.028))
    a.box("Col_Bow", (0, 0.20, -1.05), (0.20, 0.16, 0.20))
    return a
