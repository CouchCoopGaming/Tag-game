"""Pad-mount electrical box. 0.90 x 1.15 x 0.40 m on a concrete pad."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register


@register
def create():
    a = Asset("ElectricalBox", "Utility", "Pad-mount cabinet 0.90 m wide, 1.15 m tall, 0.40 m deep, on a 1.15 m pad.")
    a.climb_note = "Cabinet face is flat but only 1.15 m and 0.40 m deep. Not a cling wall."
    a.vault_note = "Too shallow to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.005 if lod == 0 else 0
        g.box((0, 0.04, 0), (1.15, 0.08, 0.60), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.8)
        g.box((0, 0.62, 0), (0.90, 1.05, 0.40), "Lib_PaintGreen", bevel=bev, segs=1)
        g.box((0, 0.70, 0.205), (0.55, 0.70, 0.015), "Lib_PaintGreen")
        if lod == 0:
            g.box((0.28, 0.85, 0.22), (0.06, 0.08, 0.03), "Lib_Steel")
            g.box((0, 0.25, 0.22), (0.20, 0.06, 0.02), "Lib_PaintYellow")
        a.end()
    a.box("Col_Pad", (0, 0.04, 0), (1.15, 0.08, 0.60))
    a.box("Col_Cab", (0, 0.62, 0), (0.90, 1.05, 0.40))
    return a
