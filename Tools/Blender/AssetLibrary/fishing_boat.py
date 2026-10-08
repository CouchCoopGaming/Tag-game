"""Small fishing boat. Curved bow, sheer, keel, transom, wheelhouse. Bow is -Z."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from _hull import beam_at, solid_hull

Z0 = -2.70
Z1 = 2.20
WL = 0.34


def profile(t):
    bow = math.sin(min(t / 0.22, 1.0) * math.pi * 0.5)
    aft = 0.76 + 0.24 * math.sin(min((1.0 - t) / 0.16, 1.0) * math.pi * 0.5)
    half = 0.78 * max(bow, 0.08) * aft
    keel = 0.0 + 0.18 * (1.0 - bow) ** 2 + 0.04 * (1.0 - aft) ** 2
    sheer = 0.68 + 0.18 * (1.0 - t) ** 1.45 + 0.05 * max(t - 0.78, 0.0) / 0.22
    return half, keel, sheer


def _top(t, half, sheer):
    gy = sheer
    xg = half * 0.96
    if 0.60 <= t <= 0.90:
        inner = max(0.12, xg - 0.07)
        floor = 0.42
        return [
            (xg, gy),
            (inner, gy - 0.015),
            (inner, floor),
            (0.0, floor + 0.008),
            (-inner, floor),
            (-inner, gy - 0.015),
            (-xg, gy),
        ]
    pts = []
    for i in range(7):
        u = i / 6.0
        x = xg * (1.0 - 2.0 * u)
        crown = 0.014 * (1.0 - (2.0 * u - 1.0) ** 2)
        pts.append((x, gy - 0.012 + crown))
    pts[0] = (xg, gy)
    pts[-1] = (-xg, gy)
    return pts


def _rails(g, segs, steps):
    for side in (1.0, -1.0):
        pts = []
        for i in range(steps + 1):
            t = 0.08 + 0.84 * i / steps
            half, _keel, sheer = profile(t)
            z = Z0 + (Z1 - Z0) * t
            x = side * (beam_at(profile, t, sheer - 0.02) + 0.028)
            pts.append((x, sheer + 0.045, z))
        for i in range(steps):
            g.pipe(pts[i], pts[i + 1], 0.012, "Lib_Steel", segs)


def _stripe(g, segs, steps):
    for side in (1.0, -1.0):
        pts = []
        for i in range(steps + 1):
            t = 0.06 + 0.88 * i / steps
            z = Z0 + (Z1 - Z0) * t
            x = side * (beam_at(profile, t, WL) + 0.03)
            pts.append((x, WL, z))
        for i in range(steps):
            g.pipe(pts[i], pts[i + 1], 0.012, "Lib_PaintRed", segs)


def _cleat(g, x, y, z):
    g.box((x, y, z), (0.18, 0.028, 0.055), "Lib_SteelDark")
    g.box((x, y + 0.028, z), (0.045, 0.04, 0.045), "Lib_Steel")
    g.box((x, y + 0.05, z), (0.15, 0.02, 0.04), "Lib_Steel")


def _house(g, lod, seg):
    # Sits on the foredeck, a few millimetres above the highest sheer under it.
    cz = -0.55
    bottom = 0.80
    height = 0.56
    depth = 0.70
    width = 0.88
    cy = bottom + height * 0.5
    g.box((0, cy, cz), (width, height, depth), "Lib_PaintWhite")
    roof_bottom = bottom + height + 0.006
    g.box((0, roof_bottom + 0.025, cz - 0.02), (width + 0.08, 0.05, depth + 0.10), "Lib_SteelDark")
    face = cz - depth * 0.5
    g.box((0, cy + 0.04, face - 0.012), (0.40, 0.22, 0.012), "Lib_ShopGlass")
    if lod == 0:
        for side in (-1.0, 1.0):
            g.box((side * (width * 0.5 + 0.012), cy + 0.02, cz - 0.06), (0.012, 0.20, 0.28), "Lib_ShopGlass")
        g.box((0.16, cy - 0.06, cz + depth * 0.5 + 0.012), (0.28, 0.42, 0.016), "Lib_WoodDark")
        g.pipe((-0.36, roof_bottom + 0.07, cz - 0.28), (0.36, roof_bottom + 0.07, cz - 0.28), 0.01, "Lib_Steel", seg)


def _motor(g, seg):
    z = Z1
    g.box((0, 0.50, z + 0.07), (0.14, 0.16, 0.08), "Lib_SteelDark")
    g.box((0, 0.64, z + 0.18), (0.24, 0.28, 0.24), "Lib_Black")
    g.cylinder((0, 0.36, z + 0.18), 0.032, 0.28, "Lib_SteelDark", seg)
    g.cylinder((0, 0.20, z + 0.28), 0.05, 0.028, "Lib_Steel", seg, axis="Z")
    g.box((0, 0.20, z + 0.33), (0.12, 0.08, 0.016), "Lib_Steel")


def _planks(g):
    # Wood deck sits a few millimetres above the white hull top.
    for i, t in enumerate((0.16, 0.26, 0.36, 0.46)):
        _half, _keel, sheer = profile(t)
        z = Z0 + (Z1 - Z0) * t
        y = sheer + 0.02
        g.box((0, y, z), (0.62 - i * 0.04, 0.016, 0.22), "Lib_Wood", uv_scale=1.3)
    for t in (0.68, 0.76, 0.84):
        z = Z0 + (Z1 - Z0) * t
        g.box((0, 0.445, z), (0.85, 0.016, 0.22), "Lib_WoodDark", uv_scale=1.2)


@register
def create():
    a = Asset(
        "FishingBoat",
        "Harbor",
        "Fishing boat about 5.1 m, bow to -Z. Curved stem, sheer, keel and transom. "
        "Wheelhouse with windows, side rails, cleats, and an outboard. Waterline stripe at 0.34 m.",
    )
    a.loose_pivot = True
    a.climb_note = "The hull is a solid prop. Not a cling wall."
    a.vault_note = "Gunwale is about 0.70 m. Under the vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        n = lod_pick(lod, 16, 9)
        solid_hull(g, Z0, Z1, n, profile, _top, "Lib_PaintWhite", bow_extra=0.16)
        _house(g, lod, seg)
        steps = lod_pick(lod, 8, 4)
        _rails(g, seg, steps)
        _stripe(g, seg, steps)
        if lod == 0:
            _planks(g)
            _motor(g, seg)
            _cleat(g, 0.0, 0.88, -2.05)
            _cleat(g, -0.28, 0.455, 1.35)
            _cleat(g, 0.28, 0.455, 1.35)
        a.end()
    a.box("Col_Bilge", (0, 0.16, 0.15), (0.36, 0.08, 1.4))
    a.box("Col_Bow", (0, 0.52, -1.85), (0.14, 0.08, 0.22))
    a.box("Col_Transom", (0, 0.46, 1.90), (0.40, 0.14, 0.10))
    a.box("Col_Cabin", (0, 1.08, -0.55), (0.64, 0.36, 0.48))
    a.box("Col_Motor", (0, 0.64, 2.40), (0.14, 0.14, 0.10))
    return a
