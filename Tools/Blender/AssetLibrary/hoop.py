"""Basketball hoop. Rim at 3.05 m, pole at the origin, rim toward +Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Hoop",
        "Park",
        "Regulation rim height 3.05 m. Pole at the origin, backboard and rim toward +Z. Set the pole just outside the court baseline.",
    )
    a.climb_note = "Pole is 12 cm. Not a cling wall."
    a.vault_note = "No rail. The rim is 3.05 m."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        g.box((0, 0.02, 0), (0.45, 0.04, 0.45), "Lib_SteelDark", bevel=0.004 if lod == 0 else 0, segs=1)
        g.cylinder((0, 1.6, 0), 0.06, 3.15, "Lib_Steel", seg)
        g.box((0, 3.35, 0.55), (1.40, 0.90, 0.04), "Lib_PaintWhite", bevel=0.006 if lod == 0 else 0, segs=1)
        g.box((0, 3.15, 0.575), (0.45, 0.35, 0.012), "Lib_PaintRed")
        g.box((0, 3.20, 0.35), (0.08, 0.08, 0.40), "Lib_SteelDark")
        g.torus((0, 3.05, 0.85), 0.225, 0.012, "Lib_Orange", 16 if lod == 0 else 10, 6)
        if lod == 0:
            for i in range(8):
                ang = math_ang(i)
                x = math_sin(ang) * 0.225
                z = 0.85 + math_cos(ang) * 0.225
                g.pipe((x, 3.05, z), (x * 0.55, 2.72, 0.85 + (z - 0.85) * 0.55), 0.004, "Lib_PaintWhite", 4)
        a.end()
    a.box("Col_Base", (0, 0.02, 0), (0.45, 0.04, 0.45))
    a.capsule("Col_Pole", (0, 1.6, 0), 0.06, 3.15, 1)
    a.box("Col_Board", (0, 3.35, 0.55), (1.40, 0.90, 0.05))
    a.box("Col_Arm", (0, 3.20, 0.35), (0.08, 0.08, 0.40))
    return a


def math_ang(i):
    import math
    return math.radians(i * 45)


def math_sin(a):
    import math
    return math.sin(a)


def math_cos(a):
    import math
    return math.cos(a)
