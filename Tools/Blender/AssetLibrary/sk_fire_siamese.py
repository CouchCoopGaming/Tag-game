"""Freestanding fire-department connection. Two brass outlets on a short body."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "FireSiamese_Post",
        "StreetFurniture",
        "Siamese connection, 0.46 m tall. Two brass outlets face +Z.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.box((0, 0.02, 0), (0.32, 0.04, 0.24), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0, 0.24, 0), (0.22, 0.44, 0.16), "Lib_Hydrant", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0, 0.46, 0), (0.26, 0.06, 0.18), "Lib_SteelDark")
        for x in (-0.06, 0.06):
            g.cylinder((x, 0.28, 0.10), 0.028, 0.06, "Lib_Brass", seg, axis="Z")
            g.cylinder((x, 0.28, 0.132), 0.034, 0.012, "Lib_Brass", seg, axis="Z")
        a.end()
    a.box("Col_Foot", (0, 0.012, 0), (0.20, 0.016, 0.14))
    a.box("Col_Body", (0, 0.24, 0), (0.14, 0.28, 0.10))
    return a
