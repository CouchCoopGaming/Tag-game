"""Rooftop water tank on a steel frame. Tank center at 3.3 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("WaterTank", "Buildings", "1.4 m diameter tank on a 2.4 m frame. Overall height 4.15 m. Legs are the footprint.")
    a.climb_note = "Legs are 8 cm tubes, not a cling wall. The tank is round."
    a.vault_note = "No rail at vault height."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 10, 4)
        leg = max(6, seg // 2) if lod < 2 else seg
        for x in (-0.55, 0.55):
            for z in (-0.55, 0.55):
                g.cylinder((x, 1.20, z), 0.04, 2.40, "Lib_SteelDark", leg)
        g.pipe((-0.55, 0.80, -0.55), (0.55, 0.80, 0.55), 0.025, "Lib_Steel", 6)
        g.pipe((-0.55, 0.80, 0.55), (0.55, 0.80, -0.55), 0.025, "Lib_Steel", 6)
        g.cylinder((0, 2.45, 0), 0.78, 0.08, "Lib_Steel", seg)
        g.cylinder((0, 3.25, 0), 0.70, 1.45, "Lib_Steel", seg, bevel=0.01 if lod == 0 else 0, segs=1)
        g.cone((0, 4.05, 0), 0.70, 0.15, 0.22, "Lib_SteelDark", seg)
        if lod == 0:
            g.torus((0, 3.15, 0), 0.70, 0.02, "Lib_Rust", 16, 6)
            g.cylinder((0.72, 2.7, 0), 0.04, 0.5, "Lib_SteelDark", 6, axis="X")
        a.end()
    for i, (x, z) in enumerate(((-0.55, -0.55), (0.55, -0.55), (-0.55, 0.55), (0.55, 0.55))):
        a.capsule("Col_Leg_%d" % i, (x, 1.20, z), 0.04, 2.40, 1)
    a.capsule("Col_Tank", (0, 3.25, 0), 0.68, 1.50, 1)
    return a
