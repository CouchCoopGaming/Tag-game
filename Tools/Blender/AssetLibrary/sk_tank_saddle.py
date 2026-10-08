"""Horizontal rooftop tank on saddles. 1.80 m long, 0.80 m diameter."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "WaterTank_Saddle",
        "StreetFurniture",
        "Horizontal tank, 1.80 m long, 0.80 m diameter, on 0.35 m saddles. Overall height 1.15 m.",
    )
    a.climb_note = "Round tank. Not a cling wall."
    a.vault_note = "Top of the tank is 1.15 m and curved."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        bev = lod_pick(lod, 0.004, 0.0)
        for x in (-0.55, 0.55):
            g.box((x, 0.16, -0.22), (0.08, 0.32, 0.06), "Lib_SteelDark", bevel=bev, segs=1)
            g.box((x, 0.16, 0.22), (0.08, 0.32, 0.06), "Lib_SteelDark", bevel=bev, segs=1)
            g.box((x, 0.36, 0), (0.10, 0.06, 0.36), "Lib_Steel", bevel=bev, segs=1)
            g.box((x, 0.06, 0), (0.16, 0.04, 0.50), "Lib_SteelDark")
        g.cylinder((0, 0.75, 0), 0.40, 1.70, "Lib_Steel", seg, axis="X", bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((-0.86, 0.75, 0), 0.42, 0.04, "Lib_SteelDark", seg, axis="X")
        g.cylinder((0.86, 0.75, 0), 0.42, 0.04, "Lib_SteelDark", seg, axis="X")
        if lod == 0:
            g.cylinder((0.70, 0.75, 0.42), 0.04, 0.10, "Lib_Brass", 8, axis="Z")
            g.cylinder((0.70, 0.75, 0.48), 0.06, 0.02, "Lib_Brass", 8, axis="Z")
            g.cylinder((-0.40, 1.16, 0), 0.025, 0.08, "Lib_SteelDark", 8)
            g.sphere((-0.40, 1.21, 0), 0.04, "Lib_Steel", 8)
        a.end()
    # Legs stop below the tank. The capsule stays inside the cylinder.
    for i, x in enumerate((-0.55, 0.55)):
        a.box("Col_LegN_%d" % i, (x, 0.15, -0.22), (0.06, 0.26, 0.04))
        a.box("Col_LegS_%d" % i, (x, 0.15, 0.22), (0.06, 0.26, 0.04))
        a.box("Col_Saddle_%d" % i, (x, 0.34, 0), (0.07, 0.03, 0.28))
    a.capsule("Col_Tank", (0, 0.75, 0), 0.34, 1.55, 0)
    return a
