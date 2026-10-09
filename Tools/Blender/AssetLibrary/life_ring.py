"""Life ring on a stand. The ring faces +Z."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _vtorus(g, center, major, minor, mat, major_seg, minor_seg):
    """Ring in the Unity XY plane, so it faces +Z."""
    verts = []
    rings = []
    for i in range(major_seg):
        ang = 2.0 * math.pi * i / major_seg
        ca, sa = math.cos(ang), math.sin(ang)
        # Radial in XY, tube binormal along Z.
        idx = []
        for j in range(minor_seg):
            b = 2.0 * math.pi * j / minor_seg
            rad = major + minor * math.cos(b)
            x = center[0] + rad * ca
            y = center[1] + rad * sa
            z = center[2] + minor * math.sin(b)
            idx.append(len(verts))
            verts.append((x, y, z))
        rings.append(idx)
    faces = []
    for i in range(major_seg):
        ni = (i + 1) % major_seg
        for j in range(minor_seg):
            nj = (j + 1) % minor_seg
            faces.append((rings[i][j], rings[ni][j], rings[ni][nj], rings[i][nj]))
    g.mesh(verts, faces, mat, uv_scale=1.0)


@register
def create():
    a = Asset(
        "LifeRing",
        "Harbor",
        "Life ring on a stand, 1.25 m tall. Orange ring with white bands faces +Z. Place the base on a deck.",
    )
    a.climb_note = "Post is 4 cm. Not a cling."
    a.vault_note = "No rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        g.box((0, 0.015, 0), (0.28, 0.03, 0.22), "Lib_SteelDark")
        g.cylinder((0, 0.62, 0), 0.02, 1.18, "Lib_Steel", seg)
        g.box((0, 1.05, 0.04), (0.10, 0.16, 0.02), "Lib_SteelDark")
        maj = lod_pick(lod, 18, 10)
        minu = lod_pick(lod, 8, 6)
        _vtorus(g, (0, 1.02, 0.10), 0.28, 0.04, "Lib_Orange", maj, minu)
        if lod == 0:
            for ang in (0.3, 1.9, 3.4, 5.0):
                x = 0.40 * math.cos(ang)
                y = 1.02 + 0.40 * math.sin(ang)
                g.box((x, y, 0.10), (0.05, 0.035, 0.04), "Lib_PaintWhite")
        a.end()
    a.capsule("Col_Post", (0, 0.62, 0), 0.018, 1.10, 1)
    a.box("Col_Base", (0, 0.015, 0), (0.24, 0.024, 0.18))
    return a
