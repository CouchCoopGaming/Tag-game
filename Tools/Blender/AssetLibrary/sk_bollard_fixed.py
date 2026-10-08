"""Fixed steel bollard, embedded in a concrete collar.

Real size: 152 mm OD, 0.91 m above the collar (36 in). Dome cap, two bands.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Bollard_Fixed",
        "StreetFurniture",
        "Fixed bollard, 152 mm OD, 0.91 m above a concrete collar.",
    )
    a.climb_note = "152 mm round post. Not a cling."
    a.vault_note = "Top is 0.96 m and domed. Not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 14, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.cylinder((0, 0.03, 0), 0.16, 0.06, "Lib_Concrete", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.48, 0), 0.076, 0.86, "Lib_Steel", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.sphere((0, 0.91, 0), 0.076, "Lib_Steel", seg)
        g.cylinder((0, 0.22, 0), 0.080, 0.045, "Lib_PaintYellow", seg)
        g.cylinder((0, 0.62, 0), 0.080, 0.045, "Lib_PaintWhite", seg)
        if lod == 0:
            g.torus((0, 0.08, 0), 0.084, 0.008, "Lib_Rust", 12, 5)
            g.cylinder((0, 0.94, 0), 0.012, 0.02, "Lib_SteelDark", 6)
        a.end()
    a.box("Col_Collar", (0, 0.03, 0), (0.24, 0.04, 0.24))
    a.capsule("Col_Post", (0, 0.44, 0), 0.055, 0.70, 1)
    return a
