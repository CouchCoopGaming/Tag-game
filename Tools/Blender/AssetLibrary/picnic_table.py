"""Park picnic table. 1.80 m, attached benches, top at 0.76 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("PicnicTable", "Park", "Picnic table 1.80 m long. Top at 0.76 m, benches at 0.45 m. Distinct from the toy picnic prop.")
    a.climb_note = "Not a wall."
    a.vault_note = "Top is 0.76 m, under the vault band. Benches are 0.45 m."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0
        planks = lod_pick(lod, 5, 3)
        for i in range(planks):
            z = -0.32 + i * (0.64 / max(1, planks - 1))
            g.box((0, 0.76, z), (1.80, 0.035, 0.12), "Lib_Wood", bevel=bev, segs=1, uv_scale=1.2)
        for z in (-0.55, 0.55):
            n = lod_pick(lod, 3, 2)
            for i in range(n):
                x = -0.55 + i * (1.1 / max(1, n - 1))
                g.box((x, 0.45, z), (0.28, 0.03, 0.22), "Lib_Wood", uv_scale=1.2)
        for x in (-0.7, 0.7):
            g.box((x, 0.38, 0), (0.06, 0.76, 0.08), "Lib_SteelDark", euler=(0, 0, 18 if x < 0 else -18))
            g.box((x, 0.38, -0.55), (0.05, 0.45, 0.05), "Lib_SteelDark")
            g.box((x, 0.38, 0.55), (0.05, 0.45, 0.05), "Lib_SteelDark")
        a.end()
    a.box("Col_Top", (0, 0.76, 0), (1.80, 0.04, 0.70))
    a.box("Col_BenchN", (0, 0.45, 0.55), (1.40, 0.04, 0.24))
    a.box("Col_BenchS", (0, 0.45, -0.55), (1.40, 0.04, 0.24))
    for i, x in enumerate((-0.7, 0.7)):
        roll = 18 if x < 0 else -18
        a.box("Col_Leg_%d" % i, (x, 0.38, 0), (0.05, 0.70, 0.07), euler=(0, 0, roll))
        a.box("Col_BenchLegN_%d" % i, (x, 0.38, 0.55), (0.04, 0.40, 0.04))
        a.box("Col_BenchLegS_%d" % i, (x, 0.38, -0.55), (0.04, 0.40, 0.04))
    return a
