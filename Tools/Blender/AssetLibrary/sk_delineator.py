"""Surface-mount channelizer. White post, two orange bands, rubber base.

42 in to the cap. 76 mm post. No legend.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Delineator_Post",
        "StreetFurniture",
        "Channelizer post, cap at 1.07 m, 76 mm tube, 0.40 m rubber base.",
    )
    a.climb_note = "76 mm post. Not a cling."
    a.vault_note = "Cap is 1.07 m and rounded. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        g.cylinder((0, 0.023, 0), 0.200, 0.046, "Lib_Rubber", seg)
        if lod == 0:
            for i in range(4):
                ang = math.pi * 0.25 + i * math.pi * 0.5
                g.cylinder((math.sin(ang) * 0.13, 0.052, math.cos(ang) * 0.13), 0.008, 0.008, "Lib_SteelDark", 6)
        g.cylinder((0, 0.060, 0), 0.028, 0.020, "Lib_SteelDark", seg)
        # Post is split so the orange sleeves do not intersect the white tube.
        g.cylinder((0, 0.302, 0), 0.038, 0.452, "Lib_PaintWhite", seg)
        g.cylinder((0, 0.562, 0), 0.044, 0.056, "Lib_Orange", seg)
        g.cylinder((0, 0.698, 0), 0.038, 0.204, "Lib_PaintWhite", seg)
        g.cylinder((0, 0.834, 0), 0.044, 0.056, "Lib_Orange", seg)
        g.cylinder((0, 0.928, 0), 0.038, 0.120, "Lib_PaintWhite", seg)
        g.sphere((0, 1.032, 0), 0.038, "Lib_PaintWhite", seg)
        a.end()
    for i in range(8):
        ang = i * math.pi * 0.25
        yaw = math.degrees(ang)
        a.box(
            "Col_Base_%d" % i,
            (math.sin(ang) * 0.110, 0.020, math.cos(ang) * 0.110),
            (0.050, 0.032, 0.160),
            euler=(0.0, yaw, 0.0),
        )
    a.capsule("Col_PostA", (0, 0.30, 0), 0.030, 0.40, 1)
    a.capsule("Col_PostB", (0, 0.70, 0), 0.030, 0.16, 1)
    a.capsule("Col_PostC", (0, 0.928, 0), 0.030, 0.10, 1)
    return a
