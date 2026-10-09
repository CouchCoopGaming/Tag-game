"""Sunken park pond. Banks slope from grade down to a water surface.

The basin extends below the pivot. Place the surrounding ground at y = 0
and keep that ground sheet off the ellipse, or it will lid the water.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

N = 18


def _ring(rx, rz, y):
    pts = []
    for i in range(N):
        a = 2.0 * math.pi * i / N
        pts.append((math.sin(a) * rx, y, math.cos(a) * rz))
    return pts


def _join(faces, a0, b0):
    for i in range(N):
        j = (i + 1) % N
        faces.append((a0 + i, b0 + i, b0 + j, a0 + j))


def _bank(g, mat):
    rings = (
        (5.35, 3.85, -0.52),
        (5.35, 3.85, 0.05),
        (4.55, 3.25, 0.02),
        (3.85, 2.65, -0.16),
        (3.40, 2.28, -0.38),
        (3.15, 2.08, -0.52),
    )
    verts = []
    for rx, rz, y in rings:
        verts.extend(_ring(rx, rz, y))
    faces = []
    for i in range(len(rings) - 1):
        _join(faces, i * N, (i + 1) * N)
    _join(faces, (len(rings) - 1) * N, 0)
    g.mesh(verts, faces, mat, uv_scale=0.35)


def _disc(g, y0, y1, rx, rz, mat):
    verts = [(0.0, y0, 0.0), (0.0, y1, 0.0)]
    for i in range(N):
        a = 2.0 * math.pi * i / N
        verts.append((math.sin(a) * rx, y0, math.cos(a) * rz))
    for i in range(N):
        a = 2.0 * math.pi * i / N
        verts.append((math.sin(a) * rx, y1, math.cos(a) * rz))
    faces = []
    for i in range(N):
        j = (i + 1) % N
        faces.append((0, 2 + j, 2 + i))
        faces.append((1, 2 + N + i, 2 + N + j))
        faces.append((2 + i, 2 + j, 2 + N + j, 2 + N + i))
    g.mesh(verts, faces, mat, uv_scale=0.25)


@register
def create():
    a = Asset(
        "Pond",
        "Park",
        "Sunken pond about 10.7 x 7.7 m over the banks. Water sits 0.32 m below grade. "
        "Grass crest, dirt slope, stone lip, and reeds. The mesh continues 0.52 m below the pivot.",
    )
    a.allow_below = True
    a.loose_pivot = True
    a.climb_note = "The bank is a slope, not a cling wall."
    a.vault_note = "No rail. Water has no collider."
    for lod in (0, 1):
        g = a.begin(lod)
        _bank(g, "Lib_FoliageLite")
        _disc(g, -0.36, -0.32, 2.85, 1.82, "Lib_Water")
        stones = lod_pick(lod, 8, 4)
        for i in range(stones):
            a_ang = 2.0 * math.pi * (i + 0.5) / stones
            x = math.sin(a_ang) * 4.70
            z = math.cos(a_ang) * 3.35
            g.box((x, 0.14, z), (0.32, 0.10, 0.22), "Lib_Concrete", uv_scale=0.8)
        if lod == 0:
            for i in range(5):
                ang = 2.0 * math.pi * (i + 0.2) / 5.0
                g.box((math.sin(ang) * 4.85, 0.12, math.cos(ang) * 3.48), (0.36, 0.04, 0.20), "Lib_Soil", uv_scale=0.6)
        if lod == 0:
            for i, (rx, rz, mat) in enumerate((
                (1.1, 0.4, "Lib_FoliageDark"),
                (-0.8, -0.5, "Lib_PaintTeal"),
                (0.2, 0.7, "Lib_FoliageDark"),
                (-1.3, 0.3, "Lib_PaintTeal"),
            )):
                g.box((rx, -0.30, rz), (0.55, 0.012, 0.32), mat)
            reeds = 8
            for i in range(reeds):
                ang = 2.0 * math.pi * i / reeds + 0.2
                x = math.sin(ang) * 3.02
                z = math.cos(ang) * 1.95
                g.cylinder((x, -0.02, z), 0.008, 0.48, "Lib_FoliageDark", 5)
                g.cone((x, 0.28, z), 0.016, 0.004, 0.12, "Lib_Foliage", 5)
        a.end()
    a.box("Col_BankN", (0.0, -0.22, 3.55), (1.0, 0.16, 0.28))
    a.box("Col_BankS", (0.0, -0.22, -3.55), (1.0, 0.16, 0.28))
    a.box("Col_BankE", (4.55, -0.22, 0.0), (0.28, 0.16, 0.8))
    a.box("Col_BankW", (-4.55, -0.22, 0.0), (0.28, 0.16, 0.8))
    return a
