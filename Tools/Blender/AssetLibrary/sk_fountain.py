"""Outdoor pedestal fountain, Most Dependable / Haws proportions.

Shallow round bowl, rolled lip, rim at 0.98 m. The chrome bubbler rises from
the bowl floor inside the rim and points at the user. Push button on the
front of the bowl. Fluted cast pedestal, flared foot, anchor plate.
A small pet bowl sits on the lit side. No legend.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import sweep_tube


# (radius, height). Closed loop. Floor is shallow; the lip rolls over.
BOWL = (
    (0.018, 0.898),
    (0.105, 0.898),
    (0.145, 0.910),
    (0.168, 0.938),
    (0.180, 0.958),
    (0.184, 0.970),
    (0.176, 0.984),
    (0.162, 0.974),
    (0.146, 0.952),
    (0.122, 0.936),
    (0.096, 0.930),
    (0.018, 0.928),
)

# Valley radius of the casting. Flutes add the ridges.
PED = (
    (0.118, 0.028),
    (0.102, 0.060),
    (0.064, 0.140),
    (0.050, 0.260),
    (0.045, 0.520),
    (0.046, 0.740),
    (0.058, 0.840),
    (0.074, 0.890),
)

# Inside the bowl. Base just above the floor, nozzle down toward +Z.
BUBBLER = (
    (0.0, 0.941, -0.036),
    (0.0, 0.968, -0.036),
    (0.0, 0.974, -0.010),
    (0.0, 0.958, 0.014),
)

PET = (
    (0.012, 0.000),
    (0.046, 0.000),
    (0.054, 0.014),
    (0.056, 0.030),
    (0.048, 0.036),
    (0.034, 0.024),
    (0.012, 0.016),
)


def _lathe(g, profile, segments, mat, origin=(0.0, 0.0, 0.0)):
    ox, oy, oz = origin
    n = len(profile)
    verts = []
    for i in range(segments):
        ang = 2.0 * math.pi * i / segments
        s, c = math.sin(ang), math.cos(ang)
        for radius, y in profile:
            verts.append((ox + radius * s, oy + y, oz + radius * c))
    faces = []
    for i in range(segments):
        i2 = (i + 1) % segments
        for k in range(n):
            k2 = (k + 1) % n
            faces.append((i * n + k, i * n + k2, i2 * n + k2, i2 * n + k))
    g.mesh(verts, faces, mat)


def _fluted(g, profile, flutes, amp, seg, mat):
    n = len(profile)
    verts = []
    for i in range(seg):
        ang = 2.0 * math.pi * i / float(seg)
        wave = amp * (0.5 + 0.5 * math.cos(flutes * ang))
        s, c = math.sin(ang), math.cos(ang)
        for radius, y in profile:
            rr = radius + wave
            verts.append((rr * s, y, rr * c))
    faces = []
    for i in range(seg):
        i2 = (i + 1) % seg
        for k in range(n - 1):
            a = i * n + k
            faces.append((a, a + 1, i2 * n + k + 1, i2 * n + k))
    bi = len(verts)
    verts.append((0.0, profile[0][1], 0.0))
    ti = bi + 1
    verts.append((0.0, profile[-1][1], 0.0))
    for i in range(seg):
        i2 = (i + 1) % seg
        faces.append((bi, i2 * n, i * n))
        faces.append((ti, i * n + n - 1, i2 * n + n - 1))
    g.mesh(verts, faces, mat)


def _body(g, lod):
    seg = lod_pick(lod, 32, 16)
    bev = lod_pick(lod, 0.0015, 0.0)
    bs = 1 if lod == 0 else 0
    g.box((0, 0.012, 0), (0.36, 0.024, 0.36), "Lib_SteelDark", bevel=bev, segs=bs)
    if lod == 0:
        for i in range(4):
            ang = math.pi * 0.25 + i * math.pi * 0.5
            g.cylinder((math.sin(ang) * 0.148, 0.030, math.cos(ang) * 0.148), 0.008, 0.012, "Lib_Steel", 6)
    _fluted(g, PED, 8, 0.008, seg, "Lib_PaintGreen")
    _lathe(g, BOWL, lod_pick(lod, 24, 14), "Lib_PaintGreen")
    # Drain, clear of the floor and of the bubbler.
    g.cylinder((0.028, 0.936, 0.020), 0.014, 0.004, "Lib_SteelDark", lod_pick(lod, 10, 6))
    sweep_tube(g, BUBBLER, 0.008, "Lib_Steel", segments=lod_pick(lod, 8, 6))
    # Bezel and button on the front of the bowl, clear of the casting.
    g.cylinder((0, 0.952, 0.192), 0.022, 0.008, "Lib_Steel", lod_pick(lod, 10, 6), axis="Z")
    g.cylinder((0, 0.952, 0.206), 0.014, 0.012, "Lib_Steel", lod_pick(lod, 10, 6), axis="Z")
    if lod == 0:
        _lathe(g, PET, 12, "Lib_PaintGreen", origin=(0.148, 0.200, 0.0))
        g.cylinder((0.080, 0.218, 0.0), 0.008, 0.012, "Lib_PaintGreen", 6, axis="X")


@register
def create():
    a = Asset(
        "Fountain_Walk",
        "StreetFurniture",
        "Pedestal fountain. Rim 0.98 m, shallow bowl, chrome bubbler inside the rim, push button, fluted base.",
    )
    a.climb_note = "The pedestal is about 11 cm across. Not a cling."
    a.vault_note = "Rim is 0.98 m and too small to stand on."
    for lod in (0, 1):
        g = a.begin(lod)
        _body(g, lod)
        a.end()
    a.box("Col_Plate", (0, 0.010, 0), (0.26, 0.016, 0.26))
    a.capsule("Col_Post", (0, 0.48, 0), 0.028, 0.62, direction=1)
    a.box("Col_Floor", (0, 0.914, 0), (0.08, 0.020, 0.08))
    a.box("Col_RimF", (0, 0.958, 0.170), (0.024, 0.012, 0.012))
    a.box("Col_RimB", (0, 0.958, -0.170), (0.024, 0.012, 0.012))
    a.box("Col_RimL", (-0.170, 0.958, 0), (0.012, 0.012, 0.024))
    a.box("Col_RimR", (0.170, 0.958, 0), (0.012, 0.012, 0.024))
    return a
