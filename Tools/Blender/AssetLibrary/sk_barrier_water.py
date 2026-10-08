"""Plastic water-filled barrier. 1.82 m long, 0.82 m tall."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Barrier_Water",
        "StreetFurniture",
        "Water-filled plastic barrier, 1.82 m long, 0.48 m wide, 0.81 m tall. Fill cap and end knuckles.",
    )
    a.climb_note = "Sloped plastic. Not a cling wall."
    a.vault_note = "Top is 0.82 m, under the vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.012, 0.004)
        bs = lod_pick(lod, 2, 1)
        g.box((0, 0.22, 0), (1.78, 0.44, 0.48), "Lib_Orange", bevel=bev, segs=bs)
        g.box((0, 0.52, 0), (1.70, 0.20, 0.36), "Lib_Orange", bevel=bev, segs=bs)
        g.box((0, 0.68, 0), (1.62, 0.16, 0.22), "Lib_PaintYellow", bevel=bev, segs=bs)
        g.cylinder((0.35, 0.78, 0), 0.06, 0.04, "Lib_Black", 10)
        g.cylinder((0.35, 0.80, 0), 0.035, 0.02, "Lib_SteelDark", 8)
        for x in (-0.92, 0.92):
            g.cylinder((x, 0.40, 0), 0.06, 0.08, "Lib_Orange", 8, axis="X")
            g.box((x * 0.86, 0.18, 0), (0.08, 0.10, 0.28), "Lib_Orange")
        if lod == 0:
            for x in (-0.45, 0.0, 0.45):
                g.box((x, 0.55, 0.185), (0.28, 0.08, 0.012), "Lib_PaintWhite")
        a.end()
    a.box("Col_Low", (0, 0.22, 0), (1.70, 0.38, 0.42))
    a.box("Col_Mid", (0, 0.52, 0), (1.62, 0.16, 0.30))
    a.box("Col_Top", (0, 0.68, 0), (1.54, 0.12, 0.16))
    return a
