"""One inverted-U bike hoop. Same tube as the three-hoop run.

0.60 m between leg centres, 48 mm tube, top at 0.86 m. Not the three-hoop rack.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import sweep_tube

RADIUS = 0.30
TUBE = 0.024
CROWN = 0.86 - TUBE
YB = CROWN - RADIUS


def _pts(steps):
    pts = [(0.0, 0.020, -RADIUS)]
    for i in range(steps + 1):
        ang = math.pi * (1.0 - i / float(steps))
        pts.append((0.0, YB + RADIUS * math.sin(ang), RADIUS * math.cos(ang)))
    pts.append((0.0, 0.020, RADIUS))
    return pts


@register
def create():
    a = Asset(
        "BikeRack_Hoop1",
        "StreetFurniture",
        "One inverted-U hoop. 0.60 m between leg centres, tube top 0.86 m.",
    )
    a.climb_note = "Tube is too thin to cling."
    a.vault_note = "Crown is 0.86 m and round. Under the vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        steps = lod_pick(lod, 40, 14)
        sweep_tube(g, _pts(steps), TUBE, "Lib_Steel", seg)
        bev = lod_pick(lod, 0.0015, 0.0)
        for z in (-RADIUS, RADIUS):
            g.box((0, 0.012, z), (0.11, 0.024, 0.11), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
            if lod == 0:
                for sx in (-0.032, 0.032):
                    for sz in (-0.032, 0.032):
                        g.cylinder((sx, 0.026, z + sz), 0.006, 0.008, "Lib_Steel", 6)
        a.end()
    steps = 40
    for j, z in enumerate((-RADIUS, RADIUS)):
        a.capsule("Col_Leg_%d" % j, (0, 0.26, z), 0.016, 0.46, direction=1)
        a.box("Col_Foot_%d" % j, (0, 0.010, z), (0.07, 0.016, 0.07))
    for k, pt in enumerate(_pts(steps)):
        if k < 3 or k > steps - 1 or k % 2:
            continue
        a.box("Col_Arc_%d" % k, pt, (0.018, 0.018, 0.018))
    return a
