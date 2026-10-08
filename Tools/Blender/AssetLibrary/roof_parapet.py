"""Corniced parapet. 4 m module. Stack it on a bay at y = 3.2, or at 6.4 for two stories."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Roof_Parapet",
        "Buildings",
        "4.00 m parapet with a corbelled cornice and a concrete coping. 0.72 m tall. Stack on a wall.",
    )
    a.climbable = True
    a.climb_note = "Short cling face. A roof edge, not a full wall."
    a.vault_note = "Coping is 0.72 m above its own base."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.005, 0.0, 0.0)
        g.box((0, 0.28, 0), (4.0, 0.52, 0.30), "Lib_Brick", bevel=bev, segs=lod_pick(lod, 1, 1, 0))
        g.box((0, 0.52, 0.04), (4.08, 0.08, 0.40), "Lib_Brick")
        g.box((0, 0.66, 0.06), (4.14, 0.08, 0.48), "Lib_Concrete", bevel=bev, segs=1)
        if lod == 0:
            g.box((0, 0.18, 0.16), (3.9, 0.06, 0.04), "Lib_Concrete")
        a.end()
    a.box("Climb_Parapet", (0, 0.28, 0), (4.0, 0.52, 0.30))
    a.box("Col_Cornice", (0, 0.58, 0.04), (4.08, 0.16, 0.42))
    a.box("Col_Coping", (0, 0.66, 0.06), (4.14, 0.08, 0.46))
    return a
