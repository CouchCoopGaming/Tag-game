"""Wrought-iron fence bay. Posts 1.20 m, pickets at 110 mm.

Pickets stop inside the bottom of the top rail so the rail crown can hold a
collider. Finials sit on the rail.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Fence_Iron",
        "StreetFurniture",
        "Iron fence 2.10 m between post centers. Posts 1.20 m. Pickets 16 mm at 110 mm. Rail crown 1.08 m.",
    )
    a.climb_note = "Pickets are a tight grille."
    a.vault_note = "Rail is 1.08 m. A high step, not a vault."
    xs = []
    x = -0.88
    while x < 0.90:
        xs.append(round(x, 3))
        x += 0.11
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.002 if lod == 0 else 0.0
        seg = lod_pick(lod, 8, 6)
        for px in (-1.05, 1.05):
            g.cylinder((px, 0.60, 0), 0.032, 1.20, "Lib_Iron", seg)
            g.cylinder((px, 0.03, 0), 0.07, 0.06, "Lib_Concrete", 8)
            g.sphere((px, 1.22, 0), 0.045, "Lib_Iron", seg)
        g.box((0, 1.05, 0), (2.16, 0.06, 0.04), "Lib_Iron", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0, 0.14, 0), (2.16, 0.04, 0.03), "Lib_Iron")
        use = xs if lod == 0 else xs[::2]
        for px in use:
            g.box((px, 0.56, 0), (0.016, 0.96, 0.016), "Lib_Iron")
            if lod == 0:
                g.cone((px, 1.11, 0), 0.014, 0.004, 0.06, "Lib_Iron", 6)
        a.end()
    a.capsule("Col_PostL", (-1.05, 0.58, 0), 0.018, 0.70)
    a.capsule("Col_PostR", (1.05, 0.58, 0), 0.018, 0.70)
    a.box("Col_Rail", (0, 1.062, 0), (1.60, 0.024, 0.018))
    for i, px in enumerate(xs):
        a.box("Col_Picket_%d" % i, (px, 0.55, 0), (0.008, 0.70, 0.008))
    return a
