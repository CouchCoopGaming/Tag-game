"""Three inverted-U bike hoops. Each tube is bent, with a flange under each leg.

BikeRack_Wave was renamed to BikeRack_Hoop3. The old prefab is a copy of this
mesh so the previous guid still resolves.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import polyline

HOOPS = (-0.90, 0.0, 0.90)
# 0.60 m between leg centres. The bend is a semicircle of that half-width.
RADIUS = 0.30
TUBE = 0.024
# Tube centerline. The top of the 48 mm tube sits at 0.86 m.
CROWN = 0.86 - TUBE
YB = CROWN - RADIUS


def _arc_points(x, steps):
    pts = []
    for i in range(steps + 1):
        ang = math.pi * (1.0 - i / float(steps))
        pts.append((x, YB + RADIUS * math.sin(ang), RADIUS * math.cos(ang)))
    return pts


def _hoop(g, x, lod):
    seg = lod_pick(lod, 12, 8)
    steps = lod_pick(lod, 24, 10)
    for z in (-RADIUS, RADIUS):
        g.pipe((x, 0.016, z), (x, YB + 0.02, z), TUBE, "Lib_Steel", seg)
    polyline(g, _arc_points(x, steps), TUBE, "Lib_Steel", seg)
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
        "Three inverted-U hoops. 0.60 m between leg centres, 0.86 m to the top of the tube, 24-step crown. Flange feet, no shared rail.",
    )
    a.climb_note = "Tube is too thin to cling."
    a.vault_note = "Crown is 0.86 m and round. Under the vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        for x in HOOPS:
            _hoop(g, x, lod)
        a.end()
    steps = 24
    for i, x in enumerate(HOOPS):
        for j, z in enumerate((-RADIUS, RADIUS)):
            a.capsule("Col_Leg_%d_%d" % (i, j), (x, 0.28, z), 0.016, 0.50, direction=1)
            a.box("Col_Foot_%d_%d" % (i, j), (x, 0.010, z), (0.07, 0.016, 0.07))
        for k, pt in enumerate(_arc_points(x, steps)):
            # Endpoints sit inside the leg tubes. The next step still overlaps them.
            if k < 2 or k > steps - 2:
                continue
            a.box("Col_Arc_%d_%d" % (i, k), pt, (0.016, 0.016, 0.016))
    return a
