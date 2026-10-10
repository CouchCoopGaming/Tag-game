"""Type III barricade. Three striped rails on folding A-frames.

Top rail crown at 1.52 m. Rails are 8 in boards, 8 ft long. No legend.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import span_collider


# Board centres. Each board is 0.20 m tall, so the top crown is 1.52 m.
# Z is shifted so the foot footprint, not the rail face, sits on the pivot.
Z = 0.25
RAILS = (0.58, 1.00, 1.42)
POSTS = (-0.90, 0.90)


def _stripes(g, y):
    # 6 in pitch. The board is the orange stripe. White plates sit on the face.
    pitch = 0.1524
    for i in range(8):
        x = -1.067 + i * pitch * 2.0
        g.box((x, y, 0.020 + Z), (0.140, 0.168, 0.004), "Lib_PaintWhite")


@register
def create():
    a = Asset(
        "Barricade_Type3",
        "StreetFurniture",
        "Type III barricade, 2.44 m rails, top at 1.52 m, folding A-frames.",
    )
    a.climb_note = "Open frame. The rails are boards, not a cling wall."
    a.vault_note = "Top rail is 1.52 m and 32 mm thick. Not a vault lip."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        for y in RAILS:
            g.box((0, y, Z), (2.44, 0.200, 0.032), "Lib_Orange")
            if lod == 0:
                _stripes(g, y)
        for x in POSTS:
            g.box((x, 0.76, -0.060 + Z), (0.044, 1.46, 0.044), "Lib_Steel")
            for y in RAILS:
                g.box((x, y, -0.027 + Z), (0.030, 0.050, 0.014), "Lib_SteelDark")
            g.pipe((x, 0.050, -0.42 + Z), (x, 1.36, -0.16 + Z), 0.016, "Lib_Steel", seg)
            g.pipe((x, 0.050, -0.14 + Z), (x, 1.36, -0.10 + Z), 0.016, "Lib_Steel", seg)
            g.pipe((x, 1.40, -0.16 + Z), (x, 1.40, -0.10 + Z), 0.010, "Lib_Steel", seg)
            g.pipe((x, 0.22, -0.36 + Z), (x, 0.22, -0.18 + Z), 0.010, "Lib_Steel", seg)
            g.box((x, 0.012, -0.42 + Z), (0.12, 0.024, 0.10), "Lib_SteelDark")
            g.box((x, 0.012, -0.14 + Z), (0.12, 0.024, 0.10), "Lib_SteelDark")
        a.end()
    for i, y in enumerate(RAILS):
        a.box("Col_Rail_%d" % i, (0, y, Z), (2.40, 0.180, 0.024))
    for i, x in enumerate(POSTS):
        a.box("Col_Post_%d" % i, (x, 0.76, -0.060 + Z), (0.034, 1.38, 0.034))
        a.box("Col_FootA_%d" % i, (x, 0.010, -0.42 + Z), (0.08, 0.016, 0.07))
        a.box("Col_FootB_%d" % i, (x, 0.010, -0.14 + Z), (0.08, 0.016, 0.07))
        c0, s0, e0 = span_collider((x, 0.14, -0.402 + Z), (x, 1.22, -0.188 + Z), 0.008)
        c1, s1, e1 = span_collider((x, 0.14, -0.136 + Z), (x, 1.22, -0.104 + Z), 0.008)
        a.box("Col_LegA_%d" % i, c0, s0, euler=e0)
        a.box("Col_LegB_%d" % i, c1, s1, euler=e1)
    return a
