"""Utility pole with a crossarm and a 6 m wire span to a stub anchor."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "UtilityPole",
        "Utility",
        "8.6 m pole, crossarm at 7.4 m, three wires sagging 6 m to a stub anchor. Wires have no collider.",
    )
    a.climb_note = "Pole is a 28 cm timber at the base, tapering. Not a flat cling wall."
    a.vault_note = "No rail. Wires are visual only."
    a.loose_pivot = True
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        g.cone((0, 4.3, 0), 0.14, 0.08, 8.6, "Lib_WoodDark", seg)
        g.box((0, 7.45, 0), (1.8, 0.10, 0.10), "Lib_Wood", uv_scale=1.0)
        for x in (-0.7, 0.0, 0.7):
            g.cylinder((x, 7.55, 0.08), 0.04, 0.08, "Lib_Steel", 8)
            _catenary(g, (x, 7.55, 0.08), (x, 6.6, 6.0), lod)
        g.cylinder((0, 3.2, 6.0), 0.06, 6.4, "Lib_WoodDark", seg)
        a.end()
    a.capsule("Col_Pole", (0, 4.3, 0), 0.08, 8.6, 1)
    a.box("Col_Arm", (0, 7.45, 0), (1.8, 0.10, 0.10))
    a.capsule("Col_Anchor", (0, 3.2, 6.0), 0.06, 6.4, 1)
    return a


def _catenary(g, a, b, lod):
    steps = 6 if lod == 0 else 3
    pts = []
    for i in range(steps + 1):
        t = i / steps
        x = a[0] + (b[0] - a[0]) * t
        z = a[2] + (b[2] - a[2]) * t
        y = a[1] + (b[1] - a[1]) * t - 0.45 * math.sin(t * math.pi)
        pts.append((x, y, z))
    for i in range(steps):
        g.pipe(pts[i], pts[i + 1], 0.008, "Lib_Black", 4)
