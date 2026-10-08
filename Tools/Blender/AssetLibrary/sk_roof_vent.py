"""Turbine roof vent on a curb. About 0.55 m across and 0.70 m tall."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _vane(g, index, count, mat, steps=5):
    """Helical strip outside the head drum. Visual only."""
    sweep = math.radians(32.0)
    a0 = 2.0 * math.pi * index / count
    radius = 0.172
    y0, y1 = 0.43, 0.61
    thick = 0.010
    chord = 0.038
    verts = []
    rings = []
    for step in range(steps + 1):
        t = step / float(steps)
        ang = a0 + sweep * t
        y = y0 + (y1 - y0) * t
        rx, rz = math.sin(ang), math.cos(ang)
        tx, tz = math.cos(ang), -math.sin(ang)
        base = len(verts)
        for su in (-0.5, 0.5):
            for sr in (-0.5, 0.5):
                rad = radius + sr * thick
                verts.append((
                    rx * rad + tx * su * chord,
                    y,
                    rz * rad + tz * su * chord,
                ))
        rings.append(base)
    faces = []
    for step in range(steps):
        a = rings[step]
        b = rings[step + 1]
        for k, k2 in ((0, 1), (1, 3), (3, 2), (2, 0)):
            faces.append((a + k, a + k2, b + k2, b + k))
    faces.append((rings[0], rings[0] + 1, rings[0] + 3, rings[0] + 2))
    last = rings[-1]
    faces.append((last + 2, last + 3, last + 1, last))
    g.mesh(verts, faces, mat)


@register
def create():
    a = Asset(
        "RoofVent_Turbine",
        "StreetFurniture",
        "Turbine vent, 0.46 m curb, throat 0.32 m across, cap at 0.70 m. Helical vane cage, visual only.",
    )
    a.climb_note = "Curb is under 0.2 m. Not a wall."
    a.vault_note = "Too small to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 14, 8)
        bev = lod_pick(lod, 0.004, 0.0)
        # Square curb, open in the middle.
        g.box((0, 0.08, -0.20), (0.46, 0.16, 0.06), "Lib_Steel", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0, 0.08, 0.20), (0.46, 0.16, 0.06), "Lib_Steel", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((-0.20, 0.08, 0), (0.06, 0.16, 0.34), "Lib_Steel", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0.20, 0.08, 0), (0.06, 0.16, 0.34), "Lib_Steel", bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.26, 0), 0.13, 0.16, "Lib_SteelDark", seg)
        g.cylinder((0, 0.38, 0), 0.16, 0.04, "Lib_Steel", seg)
        g.cylinder((0, 0.52, 0), 0.15, 0.22, "Lib_Steel", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.torus((0, 0.44, 0), 0.168, 0.008, "Lib_SteelDark", lod_pick(lod, 16, 8), 6)
        g.torus((0, 0.60, 0), 0.168, 0.008, "Lib_SteelDark", lod_pick(lod, 16, 8), 6)
        vanes = 12 if lod == 0 else 8
        steps = 5 if lod == 0 else 2
        for i in range(vanes):
            _vane(g, i, vanes, "Lib_SteelDark", steps)
        g.cone((0, 0.66, 0), 0.18, 0.04, 0.08, "Lib_SteelDark", seg)
        if lod == 0:
            g.cylinder((0, 0.71, 0), 0.02, 0.03, "Lib_Steel", 8)
        a.end()
    for name, center, size in (
        ("Col_CurbN", (0, 0.08, 0.20), (0.40, 0.12, 0.04)),
        ("Col_CurbS", (0, 0.08, -0.20), (0.40, 0.12, 0.04)),
        ("Col_CurbW", (-0.20, 0.08, 0), (0.04, 0.12, 0.28)),
        ("Col_CurbE", (0.20, 0.08, 0), (0.04, 0.12, 0.28)),
    ):
        a.box(name, center, size)
    a.box("Col_Throat", (0, 0.26, 0), (0.16, 0.10, 0.16))
    a.capsule("Col_Head", (0, 0.52, 0), 0.12, 0.20, 1)
    a.capsule("Col_Cap", (0, 0.66, 0), 0.06, 0.08, 1)
    return a
