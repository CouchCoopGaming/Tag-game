"""Three inverted-U bike hoops. Each hoop is one smooth tube.

BikeRack_Wave was renamed to BikeRack_Hoop3. The old prefab is a copy of this
mesh so the previous guid still resolves. The crown is a 40-step semicircle
with shared rings, so it shades smooth at a 1 m look.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import sweep_tube

HOOPS = (-0.90, 0.0, 0.90)
# 0.60 m between leg centres. The bend is a semicircle of that half-width.
RADIUS = 0.30
TUBE = 0.024
# Tube centerline. The top of the 48 mm tube sits at 0.86 m.
CROWN = 0.86 - TUBE
YB = CROWN - RADIUS


def _hoop_pts(x, steps):
    pts = [(x, 0.020, -RADIUS)]
    for i in range(steps + 1):
        ang = math.pi * (1.0 - i / float(steps))
        pts.append((x, YB + RADIUS * math.sin(ang), RADIUS * math.cos(ang)))
    pts.append((x, 0.020, RADIUS))
    return pts


def _hoop(g, x, lod):
    seg = lod_pick(lod, 16, 8)
    steps = lod_pick(lod, 40, 14)
    sweep_tube(g, _hoop_pts(x, steps), TUBE, "Lib_Steel", seg)
    bev = lod_pick(lod, 0.0015, 0.0)
    for z in (-RADIUS, RADIUS):
        g.box((x, 0.012, z), (0.11, 0.024, 0.11), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        if lod == 0:
            for sx in (-0.032, 0.032):
                for sz in (-0.032, 0.032):
                    g.cylinder((x + sx, 0.026, z + sz), 0.006, 0.008, "Lib_Steel", 6)


@register
def create():
    a = Asset(
        "BikeRack_Hoop3",
        "StreetFurniture",
        "Three inverted-U hoops. 0.60 m between leg centres, tube top 0.86 m. The crown is one smooth 40-step bend.",
    )
    a.climb_note = "Tube is too thin to cling."
    a.vault_note = "Crown is 0.86 m and round. Under the vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        for x in HOOPS:
            _hoop(g, x, lod)
        a.end()
    steps = 40
    for i, x in enumerate(HOOPS):
        for j, z in enumerate((-RADIUS, RADIUS)):
            a.capsule("Col_Leg_%d_%d" % (i, j), (x, 0.26, z), 0.016, 0.46, direction=1)
            a.box("Col_Foot_%d_%d" % (i, j), (x, 0.010, z), (0.07, 0.016, 0.07))
        for k, pt in enumerate(_hoop_pts(x, steps)):
            # Straight leg ends and the first bend step sit in the leg capsules.
            if k < 3 or k > steps - 1:
                continue
            if k % 2:
                continue
            a.box("Col_Arc_%d_%d" % (i, k), pt, (0.018, 0.018, 0.018))
    return a
