"""Small harbor skiff. 3.2 m long, planked hull, gunwales at 0.46 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Boat",
        "Harbor",
        "Skiff, 3.20 m long, 1.05 m across the stern, gunwale at 0.46 m. Bow is -Z.",
    )
    a.climb_note = "Hull is a solid prop. Not a cling wall."
    a.vault_note = "Gunwale is 0.46 m. Not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        # Floor boards, narrower toward the bow.
        for z, length, beam in ((-1.15, 0.70, 0.42), (-0.40, 0.80, 0.62), (0.40, 0.80, 0.72), (1.15, 0.65, 0.58)):
            g.box((0, 0.02, z), (beam, 0.04, length), "Lib_Wood", bevel=bev, segs=1, uv_scale=1.2)
        # Sides toe in toward the bow. Yaw sign brings the -Z end inward.
        g.box((-0.34, 0.26, 0.05), (0.035, 0.36, 2.85), "Lib_PaintWhite", bevel=bev, segs=1, euler=(0, -7, 0), uv_scale=1.0)
        g.box((0.34, 0.26, 0.05), (0.035, 0.36, 2.85), "Lib_PaintWhite", bevel=bev, segs=1, euler=(0, 7, 0), uv_scale=1.0)
        g.box((-0.36, 0.42, 0.05), (0.02, 0.04, 2.85), "Lib_PaintBlue", euler=(0, -7, 0))
        g.box((0.36, 0.42, 0.05), (0.02, 0.04, 2.85), "Lib_PaintBlue", euler=(0, 7, 0))
        g.box((0, 0.24, 1.48), (0.62, 0.36, 0.04), "Lib_PaintWhite", bevel=bev, segs=1)
        g.box((0, 0.22, -1.42), (0.16, 0.28, 0.18), "Lib_PaintWhite", bevel=bev, segs=1)
        g.box((0, 0.50, -0.35), (0.36, 0.04, 0.28), "Lib_Wood", uv_scale=1.2)
        g.box((0, 0.50, 0.75), (0.42, 0.04, 0.28), "Lib_Wood", uv_scale=1.2)
        if lod == 0:
            g.cylinder((0.40, 0.46, -0.1), 0.015, 0.03, "Lib_Brass", 8)
            g.cylinder((-0.40, 0.46, -0.1), 0.015, 0.03, "Lib_Brass", 8)
        a.end()
    for i, (z, length, beam) in enumerate(((-1.15, 0.66, 0.38), (-0.40, 0.76, 0.58), (0.40, 0.76, 0.68), (1.15, 0.60, 0.54))):
        a.box("Col_Floor_%d" % i, (0, 0.02, z), (beam, 0.035, length))
    a.box("Col_SideL", (-0.34, 0.26, 0.05), (0.03, 0.32, 2.70), euler=(0, -7, 0))
    a.box("Col_SideR", (0.34, 0.26, 0.05), (0.03, 0.32, 2.70), euler=(0, 7, 0))
    a.box("Col_Transom", (0, 0.24, 1.48), (0.58, 0.32, 0.035))
    a.box("Col_Bow", (0, 0.22, -1.42), (0.14, 0.24, 0.16))
    a.box("Col_SeatA", (0, 0.50, -0.35), (0.32, 0.035, 0.24))
    a.box("Col_SeatB", (0, 0.50, 0.75), (0.38, 0.035, 0.24))
    return a
