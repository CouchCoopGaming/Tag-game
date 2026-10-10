"""Concrete Jersey barrier, 10 ft section.

Real size: 32 in tall, 24 in at the base, 6 in at the top, 10 ft long
(0.81 x 0.61 x 3.05 m). New Jersey profile.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _hull(g, bevel, segs):
    # Half-width at each height. Toe, then the 55-degree face, then the steep face.
    profile = (
        (0.000, 0.305),
        (0.076, 0.305),
        (0.330, 0.130),
        (0.813, 0.076),
    )
    n = len(profile)
    xs = (-1.525, 1.525)
    verts = []
    for x in xs:
        for y, hz in profile:
            verts.append((x, y, hz))
            verts.append((x, y, -hz))

    def loop(xi):
        base = xi * n * 2
        order = [base + i * 2 for i in range(n)]
        order += [base + i * 2 + 1 for i in range(n - 1, -1, -1)]
        return order

    ring0 = loop(0)
    ring1 = loop(1)
    count = len(ring0)
    faces = []
    for i in range(count):
        j = (i + 1) % count
        faces.append((ring0[i], ring0[j], ring1[j], ring1[i]))
    faces.append(tuple(reversed(ring0)))
    faces.append(tuple(ring1))
    g.mesh(verts, faces, "Lib_Concrete", bevel=bevel, segs=segs, uv_scale=0.55)


@register
def create():
    a = Asset(
        "Barrier_Jersey",
        "StreetFurniture",
        "Jersey barrier, 3.05 m long, 0.61 m at the base, 0.81 m tall. Concrete.",
    )
    a.climb_note = "Sloped face. Not a cling wall."
    a.vault_note = "Top is 0.81 m, under the vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.008, 0.0)
        bs = 1 if lod == 0 else 0
        _hull(g, bev, bs)
        # Lifting slots, set into the toe.
        for x in (-0.70, 0.70):
            g.box((x, 0.11, 0.27), (0.28, 0.07, 0.04), "Lib_Asphalt")
            g.box((x, 0.11, -0.27), (0.28, 0.07, 0.04), "Lib_Asphalt")
        # Tongue and groove overlap the ends.
        g.box((-1.56, 0.20, 0), (0.08, 0.22, 0.16), "Lib_Concrete")
        g.box((1.50, 0.22, 0), (0.06, 0.16, 0.10), "Lib_SteelDark")
        if lod == 0:
            g.box((0, 0.55, 0.105), (1.40, 0.08, 0.012), "Lib_PaintYellow")
            g.box((0, 0.55, -0.105), (1.40, 0.08, 0.012), "Lib_PaintYellow")
            g.box((-1.10, 0.08, 0.28), (0.35, 0.05, 0.016), "Lib_Rust")
            for x in (-0.9, 0.9):
                g.box((x, 0.02, 0.22), (0.12, 0.02, 0.08), "Lib_AsphaltWear")
        a.end()
    # Crown is 0.813. Collider top is 0.7 cm under it. Half-width stays inside the top.
    a.box("Col_Top", (0, 0.778, 0), (2.80, 0.056, 0.10))
    a.box("Col_Mid", (0, 0.40, 0), (2.80, 0.28, 0.18))
    a.box("Col_Foot", (0, 0.10, 0), (2.80, 0.14, 0.48))
    return a
