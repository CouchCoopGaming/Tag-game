"""Bus stop shelter. Open to the street (-Z). Roof at 2.45 m you can land on."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "BusShelter",
        "StreetFurniture",
        "3.4 m shelter, open on -Z. Roof at 2.45 m, glass back and one side, bench inside.",
    )
    a.climb_note = "Glass and posts. The roof is a landing. Posts are 8 cm, not a cling wall."
    a.vault_note = "Roof edge is 2.45 m. Too high to vault from the ground; it is a landing."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.005 if lod == 0 else 0
        # Posts. Street side is -Z, only the ends have posts; back has a full frame.
        for x in (-1.6, 1.6):
            for z in (-0.65, 0.65):
                g.box((x, 1.20, z), (0.08, 2.40, 0.08), "Lib_Steel", bevel=bev, segs=1)
        g.box((0, 2.48, 0.05), (3.6, 0.08, 1.7), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0, 2.54, -0.7), (3.6, 0.04, 0.18), "Lib_Steel")
        # Back glass and a side panel on +X. -Z stays open.
        g.box((0, 1.25, 0.66), (3.05, 1.7, 0.015), "Lib_Glass")
        g.box((1.58, 1.25, 0.0), (0.015, 1.7, 1.15), "Lib_Glass")
        # Bench
        g.box((0, 0.45, 0.28), (2.2, 0.04, 0.40), "Lib_Wood", bevel=bev, segs=1, uv_scale=1.2)
        for x in (-0.9, 0.9):
            g.box((x, 0.22, 0.28), (0.05, 0.44, 0.36), "Lib_SteelDark")
        if lod == 0:
            g.box((-1.2, 1.35, 0.64), (0.7, 0.9, 0.02), "Lib_PaintBlue", bevel=0.004, segs=1)
        a.end()
    for i, x in enumerate((-1.6, 1.6)):
        for j, z in enumerate((-0.65, 0.65)):
            a.box("Col_Post_%d%d" % (i, j), (x, 1.20, z), (0.08, 2.40, 0.08))
    a.box("Col_Roof", (0, 2.48, 0.05), (3.6, 0.08, 1.7))
    a.box("Col_GlassBack", (0, 1.25, 0.66), (3.05, 1.7, 0.015))
    a.box("Col_GlassSide", (1.58, 1.25, 0.0), (0.015, 1.7, 1.15))
    a.box("Col_Bench", (0, 0.45, 0.28), (2.2, 0.04, 0.40))
    for i, x in enumerate((-0.9, 0.9)):
        a.box("Col_BenchLeg_%d" % i, (x, 0.22, 0.28), (0.05, 0.44, 0.36))
    return a
