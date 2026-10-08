"""Shade tree. Flared trunk and a remeshed canopy. About 5.2 m tall."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Tree", "Park", "Shade tree, trunk to 1.8 m, canopy about 5.2 m tall and 3.4 m across.")
    a.climb_note = "Trunk is round, 0.28 m at the flare. Not a flat cling wall."
    a.vault_note = "No rail. Canopy is visual."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        g.cylinder((0, 0.07, 0), 0.22, 0.14, "Lib_WoodDark", seg)
        g.cone((0, 0.98, 0), 0.14, 0.08, 1.68, "Lib_WoodDark", seg)
        voxel = 0.16 if lod == 0 else 0.28
        g.blob(
            [
                ((0.0, 3.3, 0.0), 1.15),
                ((0.7, 3.5, 0.3), 0.85),
                ((-0.6, 3.4, -0.2), 0.8),
                ((0.1, 4.1, -0.4), 0.7),
                ((-0.2, 2.8, 0.6), 0.65),
            ],
            "Lib_Foliage",
            voxel=voxel,
        )
        a.end()
    a.box("Col_Flare", (0, 0.07, 0), (0.28, 0.12, 0.28))
    a.capsule("Col_Trunk", (0, 1.05, 0), 0.07, 1.20, 1)
    a.sphere("Col_Canopy", (0, 3.35, 0), 0.72)
    return a
