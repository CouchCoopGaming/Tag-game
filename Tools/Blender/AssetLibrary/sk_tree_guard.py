"""Square tree-pit guard. Four posts and two rails. Not the tree grate."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "TreeGuard_Square",
        "StreetFurniture",
        "Tree-pit guard, 0.90 m square, rails at 0.18 m and 0.42 m.",
    )
    a.climb_note = "Posts are 4 cm. Not a cling."
    a.vault_note = "Top rail is 0.42 m."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.002, 0.0)
        bs = 1 if lod == 0 else 0
        for x in (-0.42, 0.42):
            for z in (-0.42, 0.42):
                g.box((x, 0.012, z), (0.10, 0.024, 0.10), "Lib_SteelDark", bevel=bev, segs=bs)
                g.box((x, 0.24, z), (0.04, 0.44, 0.04), "Lib_Steel", bevel=bev, segs=bs)
        for y in (0.18, 0.40):
            g.box((0, y, 0.42), (0.80, 0.025, 0.025), "Lib_Steel")
            g.box((0, y, -0.42), (0.80, 0.025, 0.025), "Lib_Steel")
            g.box((0.42, y, 0), (0.025, 0.025, 0.80), "Lib_Steel")
            g.box((-0.42, y, 0), (0.025, 0.025, 0.80), "Lib_Steel")
        a.end()
    a.box("Col_Post00", (-0.42, 0.29, -0.42), (0.024, 0.12, 0.024))
    a.box("Col_Post01", (0.42, 0.29, -0.42), (0.024, 0.12, 0.024))
    a.box("Col_Post10", (-0.42, 0.29, 0.42), (0.024, 0.12, 0.024))
    a.box("Col_Post11", (0.42, 0.29, 0.42), (0.024, 0.12, 0.024))
    return a
