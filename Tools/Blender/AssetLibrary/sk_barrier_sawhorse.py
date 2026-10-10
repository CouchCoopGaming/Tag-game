"""A-frame barricade. Three striped rails, 1.80 m wide, 1.05 m tall."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import polyline, span_collider


@register
def create():
    a = Asset(
        "Barrier_Sawhorse",
        "StreetFurniture",
        "A-frame barricade, 1.80 m wide, 1.05 m tall, three rails with dark stripes.",
    )
    a.climb_note = "Open frame. Not a cling wall."
    a.vault_note = "Top rail is 0.98 m, thin, and not a vault lip."
    legs = (-0.78, 0.78)
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        bev = lod_pick(lod, 0.003, 0.0)
        for x in legs:
            polyline(g, [(x, 0.02, -0.28), (x, 1.00, 0.0)], 0.018, "Lib_Steel", seg)
            polyline(g, [(x, 0.02, 0.28), (x, 1.00, 0.0)], 0.018, "Lib_Steel", seg)
            g.box((x, 0.015, -0.28), (0.10, 0.03, 0.12), "Lib_SteelDark", bevel=bev, segs=1)
            g.box((x, 0.015, 0.28), (0.10, 0.03, 0.12), "Lib_SteelDark", bevel=bev, segs=1)
            g.box((x, 0.55, 0.0), (0.04, 0.04, 0.36), "Lib_SteelDark")
        for y, depth in ((0.38, 0.08), (0.66, 0.07), (0.94, 0.06)):
            g.box((0, y, 0), (1.70, depth, 0.035), "Lib_PaintYellow", bevel=bev, segs=1)
            if lod == 0:
                for x in (-0.55, -0.15, 0.25, 0.65):
                    g.box((x, y, 0.0), (0.16, depth * 0.9, 0.04), "Lib_Black")
        a.end()
    for i, x in enumerate(legs):
        c0, s0, e0 = span_collider((x, 0.06, -0.26), (x, 0.96, -0.02), 0.014)
        c1, s1, e1 = span_collider((x, 0.06, 0.26), (x, 0.96, 0.02), 0.014)
        a.box("Col_LegA_%d" % i, c0, s0, euler=e0)
        a.box("Col_LegB_%d" % i, c1, s1, euler=e1)
        a.box("Col_FootA_%d" % i, (x, 0.015, -0.28), (0.08, 0.024, 0.10))
        a.box("Col_FootB_%d" % i, (x, 0.015, 0.28), (0.08, 0.024, 0.10))
    for i, (y, depth) in enumerate(((0.38, 0.08), (0.66, 0.07), (0.94, 0.06))):
        a.box("Col_Rail_%d" % i, (0, y, 0), (1.64, depth * 0.8, 0.028))
    return a
