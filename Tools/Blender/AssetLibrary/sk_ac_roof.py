"""Rooftop condenser on rails. One fan. 0.90 x 0.70 x 0.62 m above the rails."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def build_roof(g, lod, sx, sz, height, fans):
    seg = lod_pick(lod, 14, 8)
    bev = lod_pick(lod, 0.004, 0.0)
    bs = 1 if lod == 0 else 0
    for x in (-sx * 0.38, sx * 0.38):
        g.box((x, 0.04, 0), (0.06, 0.08, sz), "Lib_SteelDark", bevel=bev, segs=bs)
    g.box((0, 0.08 + height * 0.5, 0), (sx, height, sz), "Lib_Steel", bevel=bev, segs=bs)
    top = 0.08 + height
    step = sx / (fans + 1)
    for i in range(fans):
        x = -sx * 0.5 + step * (i + 1)
        g.cylinder((x, top + 0.015, 0), min(0.24, sz * 0.32), 0.025, "Lib_SteelDark", seg)
        g.cylinder((x, top + 0.032, 0), min(0.08, sz * 0.12), 0.016, "Lib_Black", 8)
        if lod == 0:
            for k in range(4):
                g.box((x, top + 0.03, -0.10 + k * 0.07), (min(0.36, sz * 0.5), 0.008, 0.012), "Lib_Steel")
    if lod == 0:
        for i in range(4):
            y = 0.22 + i * (height / 5.0)
            g.box((sx * 0.5, y, 0), (0.014, 0.016, sz * 0.62), "Lib_SteelDark")
        g.pipe((sx * 0.42, 0.40, sz * 0.15), (sx * 0.58, 0.40, sz * 0.28), 0.018, "Lib_Steel", 6)
        g.box((-sx * 0.15, 0.16, sz * 0.42), (0.16, 0.03, 0.03), "Lib_Rust")


def _asset(name, blurb, sx, sz, height, fans):
    a = Asset(name, "StreetFurniture", blurb)
    a.climb_note = "Not a wall."
    a.vault_note = "Fan top. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        build_roof(g, lod, sx, sz, height, fans)
        a.end()
    top = 0.08 + height
    a.box("Col_RailL", (-sx * 0.38, 0.04, 0), (0.02, 0.03, sz - 0.16))
    a.box("Col_RailR", (sx * 0.38, 0.04, 0), (0.02, 0.03, sz - 0.16))
    a.box("Col_Box", (0, 0.08 + height * 0.5, 0), (sx - 0.08, height - 0.06, sz - 0.08))
    a.box("Col_Top", (0, top - 0.01, 0), (sx - 0.16, 0.02, sz - 0.16))
    return a


@register
def create():
    return _asset(
        "AC_Roof_Small",
        "Rooftop condenser 0.90 x 0.70 m, 0.62 m tall on 80 mm rails, one fan.",
        0.90, 0.70, 0.62, 1,
    )
