"""Can buoy. Ballast below, cylindrical body, cage and light above."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _lathe(profile, segs):
    verts = []
    rings = []
    for radius, y in profile:
        if radius < 1e-4:
            rings.append([len(verts)])
            verts.append((0.0, y, 0.0))
            continue
        idxs = []
        for i in range(segs):
            ang = 2.0 * math.pi * i / segs
            idxs.append(len(verts))
            verts.append((radius * math.cos(ang), y, radius * math.sin(ang)))
        rings.append(idxs)
    faces = []
    for a, b in zip(rings, rings[1:]):
        if len(a) == 1 and len(b) > 1:
            c = a[0]
            for i in range(len(b)):
                j = (i + 1) % len(b)
                faces.append((c, b[i], b[j]))
        elif len(b) == 1 and len(a) > 1:
            c = b[0]
            for i in range(len(a)):
                j = (i + 1) % len(a)
                faces.append((c, a[j], a[i]))
        else:
            m = len(a)
            for i in range(m):
                j = (i + 1) % m
                faces.append((a[i], b[i], b[j], a[j]))
    return verts, faces


def _band(g, y0, y1, r_in, r_out, segs, mat):
    verts = []
    rings = []
    for y, radius in ((y0, r_in), (y0, r_out), (y1, r_out), (y1, r_in)):
        idxs = []
        for i in range(segs):
            ang = 2.0 * math.pi * i / segs
            idxs.append(len(verts))
            verts.append((radius * math.cos(ang), y, radius * math.sin(ang)))
        rings.append(idxs)
    faces = []
    order = (0, 1, 2, 3)
    for a, b in ((0, 1), (1, 2), (2, 3)):
        ra, rb = rings[a], rings[b]
        for i in range(segs):
            j = (i + 1) % segs
            faces.append((ra[i], ra[j], rb[j], rb[i]))
    ra, rb = rings[3], rings[0]
    for i in range(segs):
        j = (i + 1) % segs
        faces.append((ra[i], ra[j], rb[j], rb[i]))
    g.mesh(verts, faces, mat, uv_scale=1.0)
    return order


def _cage(g, seg):
    radial = 0.17
    for i in range(4):
        ang = math.pi * 0.25 + i * math.pi * 0.5
        x = math.cos(ang) * radial
        z = math.sin(ang) * radial
        g.cylinder((x, 1.45, z), 0.008, 0.34, "Lib_Steel", seg)
    g.torus((0, 1.66, 0), radial, 0.007, "Lib_Steel", 16, 6)
    g.torus((0, 1.24, 0), radial, 0.007, "Lib_SteelDark", 16, 6)
    g.pipe((-0.06, 1.34, -0.06), (0.06, 1.56, 0.06), 0.005, "Lib_Steel", 6)
    g.pipe((0.06, 1.34, -0.06), (-0.06, 1.56, 0.06), 0.005, "Lib_Steel", 6)
    g.cylinder((0, 1.74, 0), 0.012, 0.10, "Lib_Steel", seg)
    g.sphere((0, 1.84, 0), 0.04, "Lib_PaintYellow", 8)


@register
def create():
    a = Asset(
        "Buoy",
        "Harbor",
        "Can buoy, 1.85 m tall. Ballast bulb, red cylindrical body with a white band, and a cage with a light. Pivot is the base.",
    )
    a.climb_note = "Round. Not a cling."
    a.vault_note = "No rail."
    profile = [
        (0.0, 0.0),
        (0.12, 0.02),
        (0.24, 0.12),
        (0.26, 0.24),
        (0.16, 0.34),
        (0.30, 0.44),
        (0.32, 0.78),
        (0.32, 1.02),
        (0.18, 1.14),
        (0.14, 1.18),
        (0.0, 1.20),
    ]
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 10)
        verts, faces = _lathe(profile, seg)
        g.mesh(verts, faces, "Lib_PaintRed", uv_scale=1.0)
        _band(g, 0.62, 0.78, 0.335, 0.352, seg, "Lib_PaintWhite")
        if lod == 0:
            _cage(g, 8)
        else:
            g.cylinder((0, 1.50, 0), 0.04, 0.50, "Lib_Steel", 6)
            g.sphere((0, 1.78, 0), 0.04, "Lib_PaintYellow", 6)
        a.end()
    a.capsule("Col_Body", (0, 0.74, 0), 0.24, 0.50, 1)
    a.sphere("Col_Ballast", (0, 0.16, 0), 0.12)
    return a
