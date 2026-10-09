"""Concrete jersey barrier. 3.0 m long, 0.82 m tall."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Barrier", "Utility", "Jersey barrier 3.0 m long, 0.60 m at the base, 0.82 m tall, tapered.")
    a.climb_note = "Too low and sloped to cling."
    a.vault_note = "Top is 0.82 m, under the 0.90 m vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.008 if lod == 0 else 0
        # Stepped taper reads as a jersey profile without a boolean.
        g.box((0, 0.12, 0), (3.0, 0.24, 0.60), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.5)
        g.box((0, 0.36, 0), (3.0, 0.24, 0.42), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.5)
        g.box((0, 0.62, 0), (3.0, 0.28, 0.22), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.5)
        if lod == 0:
            g.box((0, 0.55, 0.12), (1.4, 0.16, 0.01), "Lib_PaintYellow")
        a.end()
    a.box("Col_Base", (0, 0.12, 0), (3.0, 0.24, 0.60))
    a.box("Col_Mid", (0, 0.36, 0), (3.0, 0.24, 0.42))
    a.box("Col_Top", (0, 0.62, 0), (3.0, 0.28, 0.22))
    return a
