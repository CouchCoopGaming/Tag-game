"""Turbine roof vent on a curb. About 0.55 m across and 0.70 m tall."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _annulus(g, y, r_in, r_out, height, seg, mat):
    """Closed drum wall. The hole through the middle is outside the mesh."""
    y0 = y - height * 0.5
    y1 = y + height * 0.5
    verts = []

    def ring(radius, yy):
        base = len(verts)
        for i in range(seg):
            ang = 2.0 * math.pi * i / seg
            verts.append((radius * math.sin(ang), yy, radius * math.cos(ang)))
        return base

    ob = ring(r_out, y0)
    ot = ring(r_out, y1)
    ib = ring(r_in, y0)
    it = ring(r_in, y1)
    faces = []
    for i in range(seg):
        j = (i + 1) % seg
        faces.append((ob + i, ob + j, ot + j, ot + i))
        faces.append((ib + i, it + i, it + j, ib + j))
        faces.append((ob + i, ib + i, ib + j, ob + j))
        faces.append((ot + i, ot + j, it + j, it + i))
    g.mesh(verts, faces, mat)


def _slats(g, y, count, r_in, mat):
    """Radial vanes across the hollow, stopping short of the drum wall."""
    length = r_in * 0.82
    for i in range(count):
        ang = 2.0 * math.pi * i / count
        rad = r_in * 0.50
        g.box(
            (math.sin(ang) * rad, y, math.cos(ang) * rad),
            (0.012, 0.15, length),
            mat,
            euler=(0.0, math.degrees(ang), 0.0),
        )


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
        "Turbine vent, 0.46 m curb, throat 0.32 m across, cap at 0.70 m. Hollow drum with radial slats.",
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
        _annulus(g, 0.52, 0.125, 0.155, 0.22, seg, "Lib_Steel")
        _slats(g, 0.52, 8 if lod == 0 else 6, 0.125, "Lib_SteelDark")
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
    for name, center in (
        ("Col_HeadE", (0.140, 0.52, 0.0)),
        ("Col_HeadW", (-0.140, 0.52, 0.0)),
        ("Col_HeadN", (0.0, 0.52, 0.140)),
        ("Col_HeadS", (0.0, 0.52, -0.140)),
    ):
        a.box(name, center, (0.016, 0.10, 0.030) if abs(center[0]) > 0.05 else (0.030, 0.10, 0.016))
    a.box("Col_Cap", (0, 0.64, 0), (0.08, 0.03, 0.08))
    return a
