"""Dry-barrel fire hydrant. Worn red, bonnet, capped nozzles, chains, flange bolts."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _chain(g, a, b, n=5):
    pts = []
    for i in range(n + 1):
        t = i / float(n)
        sag = math.sin(t * math.pi) * 0.055
        pts.append((
            a[0] + (b[0] - a[0]) * t,
            a[1] + (b[1] - a[1]) * t - sag,
            a[2] + (b[2] - a[2]) * t,
        ))
    for i in range(n):
        g.pipe(pts[i], pts[i + 1], 0.0045, "Lib_Chain", 4)


@register
def create():
    a = Asset(
        "FireHydrant",
        "StreetFurniture",
        "Dry-barrel hydrant, 0.76 m to the operating nut. Hose nozzles, pumper cap, chains, and flange bolts.",
    )
    a.climb_note = "Round barrel under 0.8 m. Not a cling wall."
    a.vault_note = "Too short and too narrow to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = lod_pick(lod, 1, 0)
        g.cylinder((0, 0.028, 0), 0.165, 0.056, "Lib_SteelDark", seg, bevel=bev, segs=bs)
        if lod == 0:
            for i in range(6):
                ang = math.radians(15 + i * 60)
                g.cylinder((math.sin(ang) * 0.125, 0.062, math.cos(ang) * 0.125), 0.011, 0.016, "Lib_Steel", 6)
        g.cylinder((0, 0.34, 0), 0.090, 0.52, "Lib_Hydrant", seg, bevel=bev, segs=bs)
        g.cylinder((0, 0.42, 0), 0.108, 0.10, "Lib_Hydrant", seg)
        # 2.5 inch hose nozzles on the sides, 4.5 inch pumper toward +Z.
        for sign in (-1, 1):
            g.cylinder((sign * 0.125, 0.42, 0), 0.032, 0.09, "Lib_Brass", max(8, seg // 2), axis="X", bevel=bev, segs=bs)
            g.cylinder((sign * 0.178, 0.42, 0), 0.040, 0.026, "Lib_SteelDark", max(8, seg // 2), axis="X")
            g.cylinder((sign * 0.194, 0.42, 0), 0.014, 0.012, "Lib_Brass", 6, axis="X")
            if lod == 0:
                _chain(g, (sign * 0.19, 0.40, 0.02), (sign * 0.07, 0.28, 0.06))
        g.cylinder((0, 0.40, 0.145), 0.046, 0.11, "Lib_Brass", max(8, seg // 2), axis="Z", bevel=bev, segs=bs)
        g.cylinder((0, 0.40, 0.208), 0.054, 0.028, "Lib_SteelDark", max(8, seg // 2), axis="Z")
        g.cylinder((0, 0.40, 0.224), 0.016, 0.012, "Lib_Brass", 6, axis="Z")
        if lod == 0:
            _chain(g, (0.02, 0.38, 0.22), (0.08, 0.26, 0.10))
        g.cone((0, 0.66, 0), 0.108, 0.048, 0.14, "Lib_Hydrant", seg)
        g.cylinder((0, 0.745, 0), 0.026, 0.032, "Lib_Brass", 5)
        a.end()
    a.box("Col_Flange", (0, 0.028, 0), (0.22, 0.05, 0.22))
    a.capsule("Col_Barrel", (0, 0.36, 0), 0.084, 0.50, 1)
    a.capsule("Col_Nozzle_L", (-0.13, 0.42, 0), 0.032, 0.10, 0)
    a.capsule("Col_Nozzle_R", (0.13, 0.42, 0), 0.032, 0.10, 0)
    a.capsule("Col_Nozzle_Pumper", (0, 0.40, 0.15), 0.046, 0.12, 2)
    return a
