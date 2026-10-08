"""Traffic cone, 0.72 m, with a reflective band."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("TrafficCone", "Utility", "Traffic cone 0.72 m tall, 0.36 m base.")
    a.climb_note = "Not a cling."
    a.vault_note = "Too light and short. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        g.box((0, 0.015, 0), (0.36, 0.03, 0.36), "Lib_Black", bevel=0.004 if lod == 0 else 0, segs=1)
        g.cone((0, 0.38, 0), 0.14, 0.025, 0.66, "Lib_Orange", seg)
        g.cylinder((0, 0.42, 0), 0.09, 0.06, "Lib_PaintWhite", seg)
        a.end()
    a.box("Col_Base", (0, 0.015, 0), (0.36, 0.03, 0.36))
    a.capsule("Col_Cone", (0, 0.38, 0), 0.03, 0.62, 1)
    return a
