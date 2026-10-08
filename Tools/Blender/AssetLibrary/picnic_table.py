"""Park picnic table. 1.80 m, attached benches, A-frame trestles. Top at 0.76 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "PicnicTable",
        "Park",
        "Picnic table 1.80 m long. Board top on two A-frame trestles, benches tied into the legs. Top at 0.76 m.",
    )
    a.climb_note = "Not a wall."
    a.vault_note = "Top is 0.76 m, under the vault band. Benches are 0.45 m."
    for lod in (0, 1):
        g = a.begin(lod)
        planks = lod_pick(lod, 5, 3)
        span = 0.64
        for i in range(planks):
            z = -0.32 + i * (span / max(1, planks - 1))
            g.box((0, 0.76, z), (1.80, 0.038, 0.13), "Lib_Board")
        # Stringers under the top, so the planks are one top.
        for z in (-0.20, 0.20):
            g.box((0, 0.724, z), (1.64, 0.028, 0.06), "Lib_Batten")
        for x in (-0.62, 0.62):
            # Splayed legs meet under the stringers and land on feet.
            g.box((x, 0.38, -0.18), (0.07, 0.76, 0.07), "Lib_Batten", euler=(22, 0, 0))
            g.box((x, 0.38, 0.18), (0.07, 0.76, 0.07), "Lib_Batten", euler=(-22, 0, 0))
            g.box((x, 0.04, -0.46), (0.12, 0.04, 0.16), "Lib_Batten")
            g.box((x, 0.04, 0.46), (0.12, 0.04, 0.16), "Lib_Batten")
            # Seat rails from the trestle out to each bench.
            g.box((x, 0.40, -0.48), (0.06, 0.05, 0.42), "Lib_Batten")
            g.box((x, 0.40, 0.48), (0.06, 0.05, 0.42), "Lib_Batten")
        # Lower stretcher tying the two trestles.
        g.box((0, 0.16, 0), (1.10, 0.05, 0.06), "Lib_Batten")
        for z in (-0.55, 0.55):
            g.box((0, 0.45, z), (1.56, 0.036, 0.22), "Lib_Board")
        if lod == 0:
            g.cylinder((0, 0.782, 0), 0.018, 0.012, "Lib_SteelDark", 8)
        a.end()
    a.box("Col_Top", (0, 0.76, 0), (1.70, 0.030, 0.64))
    a.box("Col_BenchN", (0, 0.45, 0.55), (1.46, 0.028, 0.18))
    a.box("Col_BenchS", (0, 0.45, -0.55), (1.46, 0.028, 0.18))
    for i, x in enumerate((-0.62, 0.62)):
        a.box("Col_LegN_%d" % i, (x, 0.38, -0.18), (0.03, 0.08, 0.03))
        a.box("Col_LegS_%d" % i, (x, 0.38, 0.18), (0.03, 0.08, 0.03))
        a.box("Col_BenchLegN_%d" % i, (x, 0.40, 0.48), (0.04, 0.04, 0.28))
        a.box("Col_BenchLegS_%d" % i, (x, 0.40, -0.48), (0.04, 0.04, 0.28))
    return a
