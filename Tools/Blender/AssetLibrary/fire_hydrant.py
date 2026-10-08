"""Dry-barrel fire hydrant. 0.78 m to the operating nut. Real nozzle layout."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "FireHydrant",
        "StreetFurniture",
        "Dry-barrel hydrant, 0.78 m to the operating nut, two hose nozzles and one pumper nozzle.",
    )
    a.climb_note = "Round barrel under 0.8 m. Not a cling wall."
    a.vault_note = "Too short and too narrow to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = lod_pick(lod, 1, 0)
        g.cylinder((0, 0.02, 0), 0.155, 0.04, "Lib_SteelDark", seg, bevel=bev, segs=bs)
        g.cylinder((0, 0.16, 0), 0.105, 0.24, "Lib_PaintRed", seg, bevel=bev, segs=bs)
        g.cylinder((0, 0.30, 0), 0.122, 0.045, "Lib_PaintRed", seg)
        g.cylinder((0, 0.46, 0), 0.092, 0.27, "Lib_PaintRed", seg, bevel=bev, segs=bs)
        g.cone((0, 0.64, 0), 0.108, 0.07, 0.10, "Lib_PaintRed", seg)
        g.cylinder((0, 0.72, 0), 0.048, 0.045, "Lib_Brass", max(6, seg // 2))
        g.cylinder((0, 0.755, 0), 0.028, 0.03, "Lib_Brass", 6)
        # Axis-aligned nozzles so the caps sit flush and the colliders match.
        g.cylinder((0.145, 0.40, 0), 0.036, 0.12, "Lib_Brass", max(8, seg // 2), axis="X", bevel=bev, segs=bs)
        g.cylinder((-0.145, 0.40, 0), 0.036, 0.12, "Lib_Brass", max(8, seg // 2), axis="X", bevel=bev, segs=bs)
        g.cylinder((0, 0.40, 0.155), 0.050, 0.14, "Lib_Brass", max(8, seg // 2), axis="Z", bevel=bev, segs=bs)
        g.cylinder((0.20, 0.40, 0), 0.044, 0.028, "Lib_SteelDark", max(8, seg // 2), axis="X")
        g.cylinder((-0.20, 0.40, 0), 0.044, 0.028, "Lib_SteelDark", max(8, seg // 2), axis="X")
        g.cylinder((0, 0.40, 0.22), 0.060, 0.03, "Lib_SteelDark", max(8, seg // 2), axis="Z")
        if lod == 0:
            for i in range(4):
                ang = math.radians(45 + i * 90)
                g.cylinder((math.sin(ang) * 0.11, 0.045, math.cos(ang) * 0.11), 0.012, 0.012, "Lib_Steel", 6)
            g.cylinder((0, 0.40, 0), 0.02, 0.02, "Lib_SteelDark", 6)  # chain eye, flush boss
        a.end()
    a.capsule("Col_Barrel", (0, 0.40, 0), 0.092, 0.70, 1)
    a.box("Col_Flange", (0, 0.02, 0), (0.22, 0.04, 0.22))
    a.capsule("Col_Nozzle_L", (-0.145, 0.40, 0), 0.036, 0.12, 0)
    a.capsule("Col_Nozzle_R", (0.145, 0.40, 0), 0.036, 0.12, 0)
    a.capsule("Col_Nozzle_Pumper", (0, 0.40, 0.155), 0.050, 0.14, 2)
    return a
