"""Lidded public trash can. Hemispherical lid, hinged deposit flap, 1.02 m to the crown."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _dome(g, cy, radius, mat, seg, rings):
    """Hemisphere, open at the equator so it can sit in the rim. +Y up."""
    verts = []
    rings_i = []
    for i in range(rings + 1):
        theta = (i / float(rings)) * math.pi * 0.5
        y = cy + radius * math.cos(theta)
        r = radius * math.sin(theta)
        ring = []
        steps = 1 if i == 0 else seg
        for k in range(steps):
            ang = 2.0 * math.pi * k / steps
            ring.append(len(verts))
            verts.append((r * math.cos(ang), y, r * math.sin(ang)))
        rings_i.append(ring)
    faces = []
    for i in range(rings):
        a = rings_i[i]
        b = rings_i[i + 1]
        if len(a) == 1:
            for k in range(len(b)):
                faces.append((a[0], b[k], b[(k + 1) % len(b)]))
            continue
        for k in range(len(a)):
            k2 = (k + 1) % len(a)
            faces.append((a[k], a[k2], b[k2], b[k]))
    g.mesh(verts, faces, mat)


@register
def create():
    a = Asset(
        "TrashCan_Lidded",
        "StreetFurniture",
        "Lidded park can, about 1.02 m to the crown. Hemispherical steel lid and a hinged deposit flap.",
    )
    a.climb_note = "Not a cling surface."
    a.vault_note = "Too narrow to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 20, 10)
        g.cylinder((0, 0.40, 0), 0.22, 0.76, "Lib_SteelDark", seg)
        g.cylinder((0, 0.78, 0), 0.25, 0.035, "Lib_Steel", seg)
        g.cylinder((0, 0.04, 0), 0.25, 0.06, "Lib_Steel", seg)
        _dome(g, 0.76, 0.24, "Lib_Steel", seg, lod_pick(lod, 6, 3))
        g.sphere((0, 1.01, 0), 0.028, "Lib_SteelDark", 8)
        # Deposit opening: a frame in the wall and a flap hinged at the top.
        g.box((0, 0.48, 0.205), (0.26, 0.20, 0.028), "Lib_Steel")
        g.box((0, 0.47, 0.222), (0.18, 0.12, 0.010), "Lib_Rubber", euler=(16, 0, 0))
        if lod == 0:
            g.cylinder((0, 0.56, 0.218), 0.010, 0.20, "Lib_SteelDark", 8, axis="X")
        a.end()
    a.capsule("Col_Body", (0, 0.48, 0), 0.20, 0.92, 1)
    a.box("Col_Door", (0, 0.48, 0.205), (0.22, 0.16, 0.020))
    return a
