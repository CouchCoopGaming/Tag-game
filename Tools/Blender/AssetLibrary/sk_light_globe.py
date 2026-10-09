"""Ornamental acorn street lamp. Globe center at 3.30 m."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import bolt_ring, polyline


def _cage(g, center, radius, tube, steps, ribs):
    cy = center[1]
    for k in range(ribs):
        ang = 2.0 * math.pi * k / ribs
        pts = []
        for i in range(steps + 1):
            t = math.pi * i / steps
            rr = radius * math.sin(t)
            pts.append((
                center[0] + rr * math.cos(ang),
                cy + radius * math.cos(t),
                center[2] + rr * math.sin(ang),
            ))
        polyline(g, pts, tube, "Lib_Black", 5)
    g.torus(center, radius * 0.72, tube, "Lib_Black", 16, 5)


@register
def create():
    a = Asset(
        "LightPost_Globe",
        "StreetFurniture",
        "Ornamental acorn lamp. Globe center 3.30 m, finial 3.68 m. Painted pole, caged glass.",
    )
    a.climb_note = "Round pole, about 11 cm at the urn. Not a cling wall."
    a.vault_note = "No rail. The cage is overhead."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        if lod >= 2:
            g.box((0, 0.05, 0), (0.36, 0.10, 0.36), "Lib_Black")
            g.cylinder((0, 1.70, 0), 0.036, 3.10, "Lib_Black", 6)
            g.sphere((0, 3.30, 0), 0.18, "Lib_Glass", 6)
            g.cone((0, 3.56, 0), 0.04, 0.012, 0.14, "Lib_Black", 6)
            a.end()
            continue
        seg = lod_pick(lod, 16, 8)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = lod_pick(lod, 1, 0)
        g.box((0, 0.025, 0), (0.42, 0.05, 0.42), "Lib_Black", bevel=bev, segs=bs)
        g.box((0, 0.085, 0), (0.30, 0.05, 0.30), "Lib_Black", bevel=bev, segs=bs)
        if lod == 0:
            bolt_ring(g, (0, 0.066, 0), 0.16, 4, 0.014, 0.016, "Lib_Steel")
        g.cylinder((0, 0.28, 0), 0.115, 0.28, "Lib_Black", seg, bevel=bev, segs=bs)
        g.torus((0, 0.43, 0), 0.09, 0.012, "Lib_SteelDark", seg, 6)
        g.cone((0, 1.68, 0), 0.052, 0.032, 2.46, "Lib_Black", seg)
        for y in (0.62, 1.55, 2.55):
            g.torus((0, y, 0), 0.046, 0.008, "Lib_SteelDark", max(8, seg - 2), 5)
        if lod == 0:
            g.box((0.055, 1.15, 0), (0.012, 0.16, 0.08), "Lib_SteelDark", bevel=0.002, segs=1)
            g.box((0.062, 1.15, 0), (0.006, 0.10, 0.05), "Lib_Black")
        g.cylinder((0, 3.00, 0), 0.07, 0.14, "Lib_Black", seg, bevel=bev, segs=bs)
        g.torus((0, 3.08, 0), 0.078, 0.012, "Lib_SteelDark", seg, 6)
        globe = (0, 3.30, 0)
        if lod == 0:
            for k in range(4):
                ang = math.radians(45 + k * 90)
                polyline(g, [
                    (0.05 * math.cos(ang), 3.08, 0.05 * math.sin(ang)),
                    (0.16 * math.cos(ang), 3.18, 0.16 * math.sin(ang)),
                ], 0.01, "Lib_Black", 5)
            _cage(g, globe, 0.23, 0.008, 6, 4)
        g.sphere(globe, 0.18, "Lib_Glass", seg)
        g.sphere((0, 3.30, 0), 0.10, "Lib_Lamp", max(8, seg // 2))
        g.cone((0, 3.58, 0), 0.045, 0.012, 0.16, "Lib_Black", seg)
        g.sphere((0, 3.66, 0), 0.018, "Lib_Brass", 8)
        a.end()
    a.box("Col_Plinth", (0, 0.025, 0), (0.38, 0.036, 0.38))
    a.box("Col_Step", (0, 0.085, 0), (0.26, 0.036, 0.26))
    a.capsule("Col_Urn", (0, 0.28, 0), 0.10, 0.26, 1)
    a.capsule("Col_Shaft", (0, 1.68, 0), 0.028, 2.40, 1)
    a.sphere("Col_Globe", (0, 3.30, 0), 0.16)
    a.capsule("Col_Finial", (0, 3.58, 0), 0.012, 0.10, 1)
    return a
