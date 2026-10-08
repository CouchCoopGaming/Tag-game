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


# (radius, height). Closed loop. Drain is 8 cm under the crown.
# The floor stays nearly flat, then the wall rises into the rolled lip.
BOWL = (
    (0.017, 0.898),
    (0.017, 0.904),
    (0.055, 0.907),
    (0.105, 0.912),
    (0.140, 0.916),
    (0.156, 0.948),
    (0.168, 0.970),
    (0.178, 0.978),
    (0.184, 0.984),
    (0.192, 0.978),
    (0.190, 0.970),
    (0.190, 0.920),
    (0.178, 0.902),
    (0.160, 0.896),
    (0.090, 0.894),
    (0.017, 0.896),
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

# Short guarded arc. The first point sits on the boss cap.
BUBBLER = (
    (0.0, 0.931, 0.000),
    (0.0, 0.948, 0.000),
    (0.0, 0.954, 0.014),
    (0.0, 0.944, 0.030),
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


def _face_ring(g, y, z, major, minor, seg, mat):
    """Chrome ring in the Unity XY plane, facing +Z."""
    verts = []
    minor_seg = 6
    for i in range(seg):
        a = 2.0 * math.pi * i / float(seg)
        ca, sa = math.cos(a), math.sin(a)
        for j in range(minor_seg):
            b = 2.0 * math.pi * j / float(minor_seg)
            rad = major + minor * math.cos(b)
            verts.append((rad * sa, y + rad * ca, z + minor * math.sin(b)))
    faces = []
    n = minor_seg
    for i in range(seg):
        i2 = (i + 1) % seg
        for j in range(n):
            j2 = (j + 1) % n
            faces.append((i * n + j, i2 * n + j, i2 * n + j2, i * n + j2))
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
    # Chrome boss plugs the drain. The arc starts on its cap.
    g.cylinder((0, 0.914, 0), 0.015, 0.032, "Lib_Steel", lod_pick(lod, 12, 8))
    sweep_tube(g, BUBBLER, 0.0065, "Lib_Steel", segments=lod_pick(lod, 8, 6))
    if lod == 0:
        _face_ring(g, 0.950, 0.020, 0.012, 0.0025, 12, "Lib_Steel")
        g.box((0, 0.966, 0.018), (0.020, 0.004, 0.014), "Lib_Steel")
        g.box((-0.011, 0.952, 0.018), (0.003, 0.014, 0.012), "Lib_Steel")
        g.box((0.011, 0.952, 0.018), (0.003, 0.014, 0.012), "Lib_Steel")
    # Round push-plate, 4.4 cm across, on the vertical face of the rim.
    # Back is 1.5 mm clear of the wall. Front is 6 mm proud of that face.
    g.cylinder((0, 0.945, 0.194), 0.022, 0.005, "Lib_SteelDark", lod_pick(lod, 20, 12), axis="Z")
    _face_ring(g, 0.945, 0.194, 0.027, 0.0025, lod_pick(lod, 20, 12), "Lib_Steel")
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
    a.box("Col_FloorF", (0, 0.904, 0.055), (0.024, 0.005, 0.024))
    a.box("Col_FloorB", (0, 0.904, -0.055), (0.024, 0.005, 0.024))
    a.box("Col_FloorL", (-0.055, 0.904, 0), (0.024, 0.005, 0.024))
    a.box("Col_FloorR", (0.055, 0.904, 0), (0.024, 0.005, 0.024))
    a.box("Col_RimF", (0, 0.972, 0.180), (0.016, 0.010, 0.008))
    a.box("Col_RimB", (0, 0.972, -0.180), (0.016, 0.010, 0.008))
    a.box("Col_RimL", (-0.180, 0.972, 0), (0.008, 0.010, 0.016))
    a.box("Col_RimR", (0.180, 0.972, 0), (0.008, 0.010, 0.016))
    return a
