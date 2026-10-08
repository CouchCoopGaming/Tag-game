"""Small steel jib crane. Tubular mast, cab, and a level jib over +Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "HarborCrane",
        "Harbor",
        "Steel jib crane. 1.6 m base, tubular mast to 4.7 m, cab, and a level jib reaching about 4 m toward +Z. Counterweight on -Z.",
    )
    a.climb_note = "The mast is a 0.28 m tube, not a cling wall."
    a.vault_note = "No rail. The jib is overhead."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8, 6)
        bev = 0.004 if lod == 0 else 0
        g.box((0, 0.10, 0), (1.60, 0.20, 1.60), "Lib_Concrete", bevel=bev, segs=1, uv_scale=1.0)
        g.cylinder((0, 0.33, 0), 0.36, 0.22, "Lib_SteelDark", seg)
        g.cylinder((0, 2.56, 0), 0.14, 4.22, "Lib_Steel", seg)
        g.box((0, 4.715, 0), (0.40, 0.08, 0.40), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0, 4.15, 0.58), (0.62, 0.55, 0.56), "Lib_CraneYellow", bevel=bev, segs=1)
        if lod == 0:
            g.box((0, 4.18, 0.875), (0.36, 0.22, 0.016), "Lib_ShopGlass")
        g.box((0, 4.84, 0.0), (0.12, 0.14, 0.12), "Lib_Steel")
        g.box((0, 4.94, 2.10), (0.14, 0.16, 3.70), "Lib_CraneYellow", bevel=bev, segs=1)
        g.box((0, 4.70, 2.15), (0.08, 0.08, 3.40), "Lib_Steel")
        if lod < 2:
            for z in (1.15, 2.15, 3.15):
                g.box((0, 4.80, z), (0.04, 0.05, 0.04), "Lib_SteelDark")
        g.box((0, 4.55, -0.88), (0.55, 0.40, 0.62), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0, 4.70, -0.42), (0.10, 0.08, 0.28), "Lib_Steel")
        if lod < 2:
            g.pipe((0, 5.10, -0.15), (0, 5.16, 3.70), 0.016, "Lib_SteelDark", 6)
        if lod == 0:
            g.pipe((0, 4.82, 3.72), (0, 2.15, 3.72), 0.012, "Lib_Steel", 5)
            g.box((0, 2.08, 3.72), (0.16, 0.12, 0.12), "Lib_SteelDark", bevel=0.003, segs=1)
        a.end()
    a.box("Col_Base", (0, 0.10, 0), (1.52, 0.16, 1.52))
    a.capsule("Col_Plinth", (0, 0.33, 0), 0.32, 0.16, 1)
    a.capsule("Col_Mast", (0, 2.56, 0), 0.11, 4.00, 1)
    a.box("Col_Head", (0, 4.715, 0), (0.32, 0.05, 0.32))
    a.box("Col_Cab", (0, 4.15, 0.58), (0.54, 0.46, 0.46))
    a.box("Col_Saddle", (0, 4.84, 0.0), (0.08, 0.10, 0.08))
    a.box("Col_Jib", (0, 4.94, 2.10), (0.10, 0.10, 3.40))
    a.box("Col_Chord", (0, 4.70, 2.15), (0.05, 0.05, 3.10))
    a.box("Col_Counter", (0, 4.55, -0.88), (0.46, 0.32, 0.50))
    a.box("Col_Tail", (0, 4.70, -0.42), (0.06, 0.05, 0.20))
    return a
