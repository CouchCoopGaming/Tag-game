"""Street litter can. Slatted shell, liner, and a dome lid.

The liner shows between the slats. The lid is one closed dome that sleeves the rim.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _lathe(g, profile, segments, mat):
    verts = []
    rings = []
    for radius, y in profile:
        if radius < 1e-5:
            rings.append([len(verts)])
            verts.append((0.0, y, 0.0))
            continue
        ring = []
        for i in range(segments):
            ang = 2.0 * math.pi * i / segments
            ring.append(len(verts))
            verts.append((radius * math.sin(ang), y, radius * math.cos(ang)))
        rings.append(ring)
    faces = []
    for a, b in zip(rings, rings[1:]):
        if len(a) == 1:
            c = a[0]
            n = len(b)
            for i in range(n):
                faces.append((c, b[i], b[(i + 1) % n]))
        elif len(b) == 1:
            c = b[0]
            n = len(a)
            for i in range(n):
                faces.append((c, a[(i + 1) % n], a[i]))
        else:
            n = len(a)
            for i in range(n):
                j = (i + 1) % n
                faces.append((a[i], b[i], b[j], a[j]))
    g.mesh(verts, faces, mat)


@register
def create():
    a = Asset(
        "LitterCan_Street",
        "StreetFurniture",
        "Slatted litter can, 0.60 m across and 1.06 m to the dome. Liner shows between the slats.",
    )
    a.climb_note = "Smooth slats. Not a cling."
    a.vault_note = "Dome is 1.06 m. Not a vault."
    dome = (
        (0.0, 1.055),
        (0.07, 1.046),
        (0.15, 1.022),
        (0.23, 0.980),
        (0.29, 0.930),
        (0.325, 0.885),
        (0.335, 0.855),
        (0.300, 0.832),
        (0.230, 0.848),
        (0.0, 0.862),
    )
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 10)
        slats = lod_pick(lod, 16, 10)
        bev = lod_pick(lod, 0.002, 0.0)
        g.cylinder((0, 0.035, 0), 0.30, 0.07, "Lib_SteelDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.46, 0), 0.235, 0.70, "Lib_Steel", seg)
        g.cylinder((0, 0.10, 0), 0.275, 0.06, "Lib_Steel", seg)
        for i in range(slats):
            ang = 2.0 * math.pi * i / slats
            g.box(
                (math.sin(ang) * 0.242, 0.47, math.cos(ang) * 0.242),
                (0.040, 0.74, 0.028),
                "Lib_PaintGreen",
                euler=(0.0, -math.degrees(ang), 0.0),
            )
        g.cylinder((0, 0.845, 0), 0.292, 0.07, "Lib_Steel", seg)
        _lathe(g, dome, seg, "Lib_SteelDark")
        if lod == 0:
            g.cylinder((0.29, 0.97, 0), 0.012, 0.07, "Lib_Steel", 6, axis="X")
        a.end()
    # Foot only, under the belly band.
    a.box("Col_Base", (0, 0.022, 0), (0.22, 0.028, 0.22))
    # Liner core, inside the slats and under the dome.
    # Inset well clear of the slat corners. A corner on a slat edge flips the parity test.
    a.box("Col_Liner", (0, 0.46, 0), (0.18, 0.40, 0.18))
    # Dome crown, within 1 cm of the top.
    a.box("Col_Dome", (0, 1.030, 0), (0.06, 0.030, 0.06))
    return a
