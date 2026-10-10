"""Timber pier piling. 0.32 m across, 3.2 m above the pivot."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Piling",
        "Harbor",
        "Fender pile, 3.20 m tall, 32 cm at the base tapering to 24 cm. Steel band and a rope wrap.",
    )
    a.allow_below = True
    a.climb_note = "Round timber. Not a cling wall. About 1.4 m of the shaft is below the pivot, in the water."
    a.vault_note = "No rail. The head is 3.2 m, too high to vault from the dock."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        # Shaft continues below the pivot so a piling placed at the waterline stands in the water.
        g.cylinder((0, -0.7, 0), 0.17, 1.5, "Lib_WoodDark", seg, uv_scale=1.0)
        g.cylinder((0, 0.08, 0), 0.20, 0.16, "Lib_Concrete", seg, uv_scale=1.0)
        g.cone((0, 1.68, 0), 0.15, 0.11, 3.04, "Lib_WoodDark", seg)
        g.cylinder((0, 2.55, 0), 0.132, 0.06, "Lib_SteelDark", seg)
        g.cylinder((0, 1.15, 0), 0.155, 0.05, "Lib_Steel", seg)
        if lod == 0:
            g.torus((0, 1.70, 0), 0.15, 0.012, "Lib_Rust", 12, 6)
            g.torus((0, 1.78, 0), 0.15, 0.012, "Lib_Rust", 12, 6)
        a.end()
    a.box("Col_Collar", (0, 0.08, 0), (0.26, 0.14, 0.26))
    a.capsule("Col_Pile", (0, 1.70, 0), 0.09, 2.40, 1)
    return a
