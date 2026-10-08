"""Park lamp. 3.2 m post, lantern head."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("ParkLamp", "Park", "Path lamp, 3.20 m to the lantern cap. Square glass lantern.")
    a.climb_note = "Post is 9 cm. Not a cling wall."
    a.vault_note = "No rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        bev = 0.004 if lod == 0 else 0
        g.box((0, 0.02, 0), (0.32, 0.04, 0.32), "Lib_SteelDark", bevel=bev, segs=1)
        g.cylinder((0, 1.55, 0), 0.045, 3.0, "Lib_Black", seg)
        g.box((0, 3.05, 0), (0.32, 0.36, 0.32), "Lib_Glass")
        g.box((0, 3.24, 0), (0.40, 0.06, 0.40), "Lib_SteelDark", bevel=bev, segs=1)
        g.cone((0, 3.34, 0), 0.22, 0.02, 0.12, "Lib_Steel", seg)
        a.end()
    a.box("Col_Base", (0, 0.02, 0), (0.32, 0.04, 0.32))
    a.capsule("Col_Post", (0, 1.55, 0), 0.045, 3.0, 1)
    a.box("Col_Lantern", (0, 3.05, 0), (0.28, 0.32, 0.28))
    a.box("Col_Cap", (0, 3.24, 0), (0.36, 0.05, 0.36))
    return a
