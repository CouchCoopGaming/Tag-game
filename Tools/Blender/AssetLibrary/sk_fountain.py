"""Sidewalk drinking fountain. Green cast bowl, brass gooseneck, push button.

The rim crown is 1.00 m. The basin is a hollow you can see into. No legend.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import sweep_tube


# Closed bowl profile, (radius, height). The last point joins the first,
# so the core under the basin floor stays solid and the cavity stays open.
BOWL = (
    (0.025, 0.800),
    (0.130, 0.800),
    (0.155, 0.840),
    (0.210, 0.920),
    (0.235, 0.980),
    (0.210, 0.998),
    (0.180, 0.980),
    (0.155, 0.920),
    (0.110, 0.870),
    (0.025, 0.870),
)

# Gooseneck on the lit side, clear of the pedestal and the bowl, tip over the basin.
SPOUT = (
    (0.17, 0.58, 0.0),
    (0.30, 0.88, 0.02),
    (0.18, 1.14, 0.05),
    (0.02, 1.06, 0.06),
    (0.0, 0.99, 0.04),
)

# Dark liner inside the green bowl. Inset so it never touches the casting.
LINER = (
    (0.158, 0.950),
    (0.140, 0.924),
    (0.098, 0.900),
    (0.086, 0.900),
    (0.126, 0.924),
    (0.144, 0.950),
)


def _lathe(g, profile, segments, mat):
    n = len(profile)
    verts = []
    for i in range(segments):
        ang = 2.0 * math.pi * i / segments
        s, c = math.sin(ang), math.cos(ang)
        for radius, y in profile:
            verts.append((radius * s, y, radius * c))
    faces = []
    for i in range(segments):
        i2 = (i + 1) % segments
        for k in range(n):
            k2 = (k + 1) % n
            faces.append((
                i * n + k,
                i * n + k2,
                i2 * n + k2,
                i2 * n + k,
            ))
    g.mesh(verts, faces, mat)


def _body(g, lod):
    seg = lod_pick(lod, 16, 10)
    bev = lod_pick(lod, 0.002, 0.0)
    bs = 1 if lod == 0 else 0
    g.box((0, 0.015, 0), (0.40, 0.030, 0.40), "Lib_SteelDark", bevel=bev, segs=bs)
    if lod == 0:
        for x in (-0.14, 0.14):
            for z in (-0.14, 0.14):
                g.cylinder((x, 0.040, z), 0.011, 0.012, "Lib_Steel", 6)
    g.cylinder((0, 0.385, 0), 0.082, 0.700, "Lib_PaintGreen", seg)
    g.cylinder((0, 0.762, 0), 0.115, 0.040, "Lib_PaintGreen", seg)
    _lathe(g, BOWL, lod_pick(lod, 20, 12), "Lib_PaintGreen")
    _lathe(g, LINER, lod_pick(lod, 16, 10), "Lib_SteelDark")
    # Basin floor, gapped under the liner and above the green casting.
    g.cylinder((0, 0.882, 0), 0.072, 0.006, "Lib_SteelDark", lod_pick(lod, 12, 8))
    if lod == 0:
        g.box((0, 0.890, 0), (0.046, 0.004, 0.008), "Lib_Steel")
        g.box((0, 0.890, 0), (0.008, 0.004, 0.046), "Lib_Steel")
    # Mount on the lit side, gapped off the pedestal. The tube starts clear of the mount.
    g.cylinder((0.118, 0.58, 0), 0.022, 0.046, "Lib_Steel", lod_pick(lod, 8, 6), axis="X")
    sweep_tube(g, SPOUT, 0.013, "Lib_Brass", segments=lod_pick(lod, 8, 6))
    # Push plate and button on the street face, clear of the pedestal.
    g.box((0, 0.50, 0.100), (0.064, 0.084, 0.010), "Lib_Steel", bevel=bev, segs=bs)
    g.cylinder((0, 0.50, 0.124), 0.022, 0.020, "Lib_PaintRed", lod_pick(lod, 10, 6), axis="Z")


@register
def create():
    a = Asset(
        "Fountain_Walk",
        "StreetFurniture",
        "Sidewalk fountain. Rim at 1.00 m, hollow basin, brass gooseneck, push button. No legend.",
    )
    a.climb_note = "The pedestal is 16 cm across. Not a cling."
    a.vault_note = "Rim is 1.00 m and too small to stand on."
    for lod in (0, 1):
        g = a.begin(lod)
        _body(g, lod)
        a.end()
    a.box("Col_Base", (0, 0.012, 0), (0.28, 0.018, 0.28))
    a.capsule("Col_Post", (0, 0.385, 0), 0.050, 0.64, direction=1)
    a.box("Col_Under", (0, 0.834, 0), (0.14, 0.040, 0.14))
    # Short boxes in the rim wall, inset from the crown and the basin.
    a.box("Col_RimF", (0, 0.946, 0.204), (0.060, 0.028, 0.018))
    a.box("Col_RimB", (0, 0.946, -0.204), (0.060, 0.028, 0.018))
    a.box("Col_RimL", (-0.204, 0.946, 0), (0.018, 0.028, 0.060))
    a.box("Col_RimR", (0.204, 0.946, 0), (0.018, 0.028, 0.060))
    return a
