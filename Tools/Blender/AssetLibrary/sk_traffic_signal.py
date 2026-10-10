"""Tapered mast arm and one three-lamp head. No legend and no ped head.

Pole is 300 mm at the base and 200 mm at the top. The arm rises toward the
tip. The head hangs from a clamp under the arm, not off the end. Lenses are
full 12 inch circles inside tunnel visors.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import sweep_tube

POLE_A = (0.0, 0.04, 0.0)
POLE_B = (0.0, 6.42, 0.0)
POLE_R0 = 0.150
POLE_R1 = 0.100
ARM_A = (0.02, 5.66, 0.0)
ARM_B = (5.80, 5.96, 0.0)
ARM_R0 = 0.078
ARM_R1 = 0.042
HEAD_X = 4.35
HEAD_BOTTOM = 4.60
HEAD_H = 1.16
HEAD_Y = HEAD_BOTTOM + HEAD_H * 0.5


def _pole_radius(y):
    t = (y - POLE_A[1]) / (POLE_B[1] - POLE_A[1])
    return POLE_R0 + (POLE_R1 - POLE_R0) * t


def _arm_at(x):
    t = (x - ARM_A[0]) / (ARM_B[0] - ARM_A[0])
    y = ARM_A[1] + (ARM_B[1] - ARM_A[1]) * t
    r = ARM_R0 + (ARM_R1 - ARM_R0) * t
    return y, r


def _tunnel(g, center, z0, z1, r_in, r_out, seg):
    """Closed tube wall along +Z. The opening is larger than the lens."""
    x, y = center[0], center[1]
    verts = []

    def ring(z, radius):
        idxs = []
        for i in range(seg):
            a = 2.0 * math.pi * i / float(seg)
            verts.append((x + radius * math.cos(a), y + radius * math.sin(a), z))
            idxs.append(len(verts) - 1)
        return idxs

    back_in = ring(z0, r_in)
    back_out = ring(z0, r_out)
    front_in = ring(z1, r_in)
    front_out = ring(z1, r_out)
    faces = []
    for i in range(seg):
        j = (i + 1) % seg
        faces.append((back_out[i], back_out[j], front_out[j], front_out[i]))
        faces.append((back_in[i], front_in[i], front_in[j], back_in[j]))
        faces.append((back_in[i], back_in[j], back_out[j], back_out[i]))
        faces.append((front_out[i], front_out[j], front_in[j], front_in[i]))
    g.mesh(verts, faces, "Lib_Black")


def _head(g, lod):
    seg = lod_pick(lod, 24, 12)
    bev = lod_pick(lod, 0.003, 0.0)
    bs = 1 if lod == 0 else 0
    hx, hy = HEAD_X, HEAD_Y
    g.box((hx, hy, -0.02), (0.52, 1.24, 0.02), "Lib_Black")
    g.box((hx, hy, 0.06), (0.44, HEAD_H, 0.22), "Lib_Black", bevel=bev, segs=bs)
    lamps = (
        (hy + 0.38, "Lib_LensRed"),
        (hy, "Lib_LensAmber"),
        (hy - 0.38, "Lib_LensGreen"),
    )
    for y, mat in lamps:
        # Disc sits fully in front of the housing, inside the tunnel, not cut by it.
        g.cylinder((hx, y, 0.200), 0.152, 0.036, mat, seg, axis="Z")
        g.cylinder((hx, y, 0.162), 0.170, 0.010, "Lib_Black", seg, axis="Z")
        _tunnel(g, (hx, y, 0.0), 0.168, 0.38, 0.190, 0.212, seg)


def _arm_box(name, x0, x1):
    y0, _r0 = _arm_at(x0)
    y1, _r1 = _arm_at(x1)
    ang = math.degrees(math.atan2(y1 - y0, x1 - x0))
    return name, ((x0 + x1) * 0.5, (y0 + y1) * 0.5, 0.0), (abs(x1 - x0), 0.050, 0.050), ang


@register
def create():
    a = Asset(
        "TrafficSignal_Mast",
        "StreetFurniture",
        "Tapered mast. Pole 300 mm to 200 mm, rising arm, three 12 inch lenses in tunnel visors. Head bottom 4.60 m.",
    )
    a.climb_note = "Tapered pole, about 250 mm. Not a cling."
    a.vault_note = "The arm is about 5.8 m up."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 20, 10)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.025, 0), (0.56, 0.05, 0.56), "Lib_SteelDark", bevel=bev, segs=bs)
        if lod == 0:
            for x in (-0.20, 0.20):
                for z in (-0.20, 0.20):
                    g.cylinder((x, 0.055, z), 0.016, 0.018, "Lib_Steel", 6)
        sweep_tube(g, [POLE_A, POLE_B], POLE_R0, "Lib_Steel", seg, POLE_R1)
        # Handhole cover on the sidewalk side, low. Not a cabinet on the tip.
        hy = 0.52
        hr = _pole_radius(hy)
        g.box((hr + 0.006, hy, 0.0), (0.016, 0.24, 0.12), "Lib_SteelDark", bevel=bev, segs=bs)
        if lod == 0:
            g.cylinder((hr + 0.016, hy + 0.07, 0.0), 0.008, 0.008, "Lib_Steel", 6, axis="X")
        sweep_tube(g, [ARM_A, ARM_B], ARM_R0, "Lib_Steel", seg, ARM_R1)
        # Flange at the pole, and a gusset plate under the root.
        g.cylinder((0.12, ARM_A[1], 0.0), 0.135, 0.028, "Lib_SteelDark", seg, axis="X")
        g.box((0.34, ARM_A[1] - 0.10, 0.0), (0.42, 0.22, 0.016), "Lib_SteelDark")
        ay, ar = _arm_at(HEAD_X)
        g.cylinder((HEAD_X, ay, 0.0), ar + 0.012, 0.10, "Lib_SteelDark", seg, axis="X")
        g.cylinder((HEAD_X, (ay - ar + HEAD_Y + HEAD_H * 0.5) * 0.5, 0.0), 0.028, ay - ar - (HEAD_Y + HEAD_H * 0.5) + 0.04, "Lib_Steel", 8)
        _head(g, lod)
        a.end()
    a.box("Col_Base", (0, 0.020, 0), (0.40, 0.028, 0.40))
    # Capsules stay inside the taper and stop under the arm joint.
    a.capsule("Col_Pole_A", (0, 1.15, 0), 0.118, 2.00, direction=1)
    a.capsule("Col_Pole_B", (0, 3.05, 0), 0.100, 1.60, direction=1)
    a.capsule("Col_Pole_C", (0, 4.55, 0), 0.086, 1.20, direction=1)
    for name, center, size, ang in (
        _arm_box("Col_Arm_A", 0.62, 3.70),
        _arm_box("Col_Arm_B", 4.85, 5.60),
    ):
        a.box(name, center, size, euler=(0, 0, ang))
    a.box("Col_Head", (HEAD_X, HEAD_Y, 0.04), (0.30, 0.90, 0.10))
    return a
