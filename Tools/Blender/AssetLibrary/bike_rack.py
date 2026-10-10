"""Inverted-U bike rack. Three hoops, 0.85 m tall, 1.90 m long."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("BikeRack", "StreetFurniture", "Three inverted-U hoops. 0.85 m tall, 1.90 m overall, tube 4 cm.")
    a.climb_note = "Tubes are too thin to cling."
    a.vault_note = "Hoop tops are 0.85 m, just under the vault band, and round."
    xs = (-0.7, 0.0, 0.7)
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        for x in xs:
            g.pipe((x, 0.02, -0.28), (x, 0.78, -0.28), 0.02, "Lib_Steel", seg)
            g.pipe((x, 0.02, 0.28), (x, 0.78, 0.28), 0.02, "Lib_Steel", seg)
            g.pipe((x, 0.78, -0.28), (x, 0.78, 0.28), 0.02, "Lib_Steel", seg)
            g.sphere((x, 0.78, -0.28), 0.02, "Lib_Steel", 6)
            g.sphere((x, 0.78, 0.28), 0.02, "Lib_Steel", 6)
        g.box((0, 0.015, 0), (1.70, 0.02, 0.08), "Lib_SteelDark")
        a.end()
    for i, x in enumerate(xs):
        a.capsule("Col_LegA_%d" % i, (x, 0.40, -0.28), 0.02, 0.80, 1)
        a.capsule("Col_LegB_%d" % i, (x, 0.40, 0.28), 0.02, 0.80, 1)
        a.capsule("Col_Top_%d" % i, (x, 0.78, 0.0), 0.02, 0.56, 2)
    return a
