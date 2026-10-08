"""Serpentine bike rack. One square-wave tube, 2.10 m long, 0.84 m tall."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import polyline


@register
def create():
    a = Asset(
        "BikeRack_Wave",
        "StreetFurniture",
        "Serpentine rack, 2.10 m long, 0.84 m to the top tube, 4.2 cm pipe.",
    )
    a.climb_note = "Tube is too thin to cling."
    a.vault_note = "Top is 0.84 m and round. Under the vault band."
    xs = [-0.90, -0.30, 0.30, 0.90]
    r = 0.021
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        z0, z1 = -0.26, 0.26
        pts = [(xs[0], 0.02, z0)]
        z = z0
        for i, x in enumerate(xs):
            other = z1 if z < 0 else z0
            pts.append((x, 0.82, z))
            pts.append((x, 0.82, other))
            pts.append((x, 0.02, other))
            z = other
            if i + 1 < len(xs):
                pts.append((xs[i + 1], 0.02, z))
        polyline(g, pts, r, "Lib_Steel", seg)
        for x in xs:
            g.box((x, 0.012, -0.26), (0.08, 0.024, 0.08), "Lib_SteelDark", bevel=0.002 if lod == 0 else 0, segs=1)
            g.box((x, 0.012, 0.26), (0.08, 0.024, 0.08), "Lib_SteelDark", bevel=0.002 if lod == 0 else 0, segs=1)
        a.end()
    for i, x in enumerate(xs):
        a.capsule("Col_Up_%d" % i, (x, 0.42, -0.26 if i % 2 == 0 else 0.26), 0.016, 0.76, 1)
        a.capsule("Col_Down_%d" % i, (x, 0.42, 0.26 if i % 2 == 0 else -0.26), 0.016, 0.76, 1)
        a.capsule("Col_Top_%d" % i, (x, 0.82, 0.0), 0.016, 0.48, 2)
        a.box("Col_FootA_%d" % i, (x, 0.012, -0.26), (0.06, 0.02, 0.06))
        a.box("Col_FootB_%d" % i, (x, 0.012, 0.26), (0.06, 0.02, 0.06))
    for i in range(3):
        x = (xs[i] + xs[i + 1]) * 0.5
        z = 0.26 if i % 2 == 0 else -0.26
        a.capsule("Col_Run_%d" % i, (x, 0.02, z), 0.016, 0.52, 0)
    return a
