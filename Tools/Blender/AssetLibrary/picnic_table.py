"""Park picnic table. 1.80 m, attached benches, tapered A-frame trestles. Top at 0.76 m."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _tapered_leg(g, x, bottom, top, bot_w, top_w):
    """Leg from a foot point to the top, wider at the foot. bottom/top are (y, z)."""
    y0, z0 = bottom
    y1, z1 = top
    dy, dz = y1 - y0, z1 - z0
    length = math.hypot(dy, dz) or 1.0
    py, pz = -dz / length, dy / length
    hb, ht = bot_w * 0.5, top_w * 0.5
    verts = []
    for y, z, h in ((y0, z0, hb), (y1, z1, ht)):
        for sx in (-1.0, 1.0):
            for sp in (-1.0, 1.0):
                verts.append((x + sx * h, y + sp * h * py, z + sp * h * pz))
    faces = [
        (0, 2, 3, 1),
        (4, 5, 7, 6),
        (0, 1, 5, 4),
        (1, 3, 7, 5),
        (3, 2, 6, 7),
        (2, 0, 4, 6),
    ]
    g.mesh(verts, faces, "Lib_Batten")


@register
def create():
    a = Asset(
        "PicnicTable",
        "Park",
        "Picnic table 1.80 m long. Board top on two tapered A-frame trestles with a cross brace. Benches tie into the legs. Top at 0.76 m.",
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
        for z in (-0.20, 0.20):
            g.box((0, 0.724, z), (1.64, 0.028, 0.06), "Lib_Batten")
        for x in (-0.62, 0.62):
            # Wider at the foot, buried in the foot block and in the top.
            _tapered_leg(g, x, (0.04, -0.46), (0.74, -0.05), 0.090, 0.046)
            _tapered_leg(g, x, (0.04, 0.46), (0.74, 0.05), 0.090, 0.046)
            # Cross brace buried in both legs of the trestle.
            g.box((x, 0.36, 0.0), (0.044, 0.044, 0.70), "Lib_Batten")
            g.box((x, 0.04, -0.46), (0.12, 0.04, 0.16), "Lib_Batten")
            g.box((x, 0.04, 0.46), (0.12, 0.04, 0.16), "Lib_Batten")
            g.box((x, 0.40, -0.48), (0.06, 0.05, 0.42), "Lib_Batten")
            g.box((x, 0.40, 0.48), (0.06, 0.05, 0.42), "Lib_Batten")
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
        a.box("Col_LegN_%d" % i, (x, 0.39, -0.255), (0.024, 0.04, 0.024))
        a.box("Col_LegS_%d" % i, (x, 0.39, 0.255), (0.024, 0.04, 0.024))
        a.box("Col_BenchLegN_%d" % i, (x, 0.40, 0.48), (0.04, 0.04, 0.28))
        a.box("Col_BenchLegS_%d" % i, (x, 0.40, -0.48), (0.04, 0.04, 0.28))
    return a
