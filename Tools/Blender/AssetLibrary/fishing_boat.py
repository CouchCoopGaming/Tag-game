"""Fishing skiff. Flared bow, deadrise V, wheelhouse, outboard. Bow is -Z."""

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
    half = 0.84 * max(bow, 0.20) * aft
    keel = 0.0 + 0.18 * (1.0 - bow) ** 2 + 0.04 * (1.0 - aft) ** 2
    sheer = 0.68 + 0.18 * (1.0 - t) ** 1.45 + 0.05 * max(t - 0.78, 0.0) / 0.22
    return half, keel, sheer


def section_skiff(half, keel, sheer, t):
    """Hard chine. Bow pulls the chine in so the topsides flare."""
    rise = max(0.05, sheer - keel)
    chine_frac = 0.44 + 0.18 * min(t / 0.35, 1.0)
    chine_x = half * chine_frac
    chine_y = keel + rise * 0.36
    sheer_x = half * 0.98
    return [
        (0.0, keel),
        (chine_x * 0.45, keel + rise * 0.16),
        (chine_x, chine_y),
        (sheer_x * 0.92, keel + rise * 0.70),
        (sheer_x, sheer),
    ]


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
            _half, _keel, sheer = profile(t)
            z = Z0 + (Z1 - Z0) * t
            x = side * (beam_at(profile, t, sheer - 0.02, section_skiff) + 0.028)
            pts.append((x, sheer + 0.045, z))
        for i in range(steps):
            g.pipe(pts[i], pts[i + 1], 0.012, "Lib_Steel", segs)


def _stripe(g, segs, steps):
    for side in (1.0, -1.0):
        pts = []
        for i in range(steps + 1):
            t = 0.06 + 0.88 * i / steps
            z = Z0 + (Z1 - Z0) * t
            x = side * (beam_at(profile, t, WL, section_skiff) + 0.020)
            pts.append((x, WL, z))
        for i in range(steps):
            g.pipe(pts[i], pts[i + 1], 0.010, "Lib_PaintRed", segs)


def _chine(g, segs, steps):
    for side in (1.0, -1.0):
        pts = []
        for i in range(steps + 1):
            t = 0.05 + 0.90 * i / steps
            half, keel, sheer = profile(t)
            rise = max(0.05, sheer - keel)
            y = keel + rise * 0.36
            z = Z0 + (Z1 - Z0) * t
            x = side * (beam_at(profile, t, y, section_skiff) + 0.016)
            pts.append((x, y, z))
        for i in range(steps):
            g.pipe(pts[i], pts[i + 1], 0.006, "Lib_SteelDark", segs)


def _cleat(g, x, y, z):
    g.box((x, y, z), (0.18, 0.028, 0.055), "Lib_SteelDark")
    g.box((x, y + 0.028, z), (0.045, 0.04, 0.045), "Lib_Steel")
    g.box((x, y + 0.05, z), (0.15, 0.02, 0.04), "Lib_Steel")


def _house(g, lod, seg):
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
    for i, t in enumerate((0.16, 0.26, 0.36, 0.46)):
        _half, _keel, sheer = profile(t)
        z = Z0 + (Z1 - Z0) * t
        g.box((0, sheer + 0.02, z), (0.58 - i * 0.03, 0.016, 0.20), "Lib_Varnish")
    for t in (0.68, 0.76, 0.84):
        z = Z0 + (Z1 - Z0) * t
        g.box((0, 0.445, z), (0.78, 0.016, 0.20), "Lib_Varnish")


@register
def create():
    a = Asset(
        "FishingBoat",
        "Harbor",
        "Fishing skiff about 5.1 m, bow to -Z. Hard-chine hull with a deadrise V and a flared bow. "
        "Wheelhouse, side rails, cleats, and an outboard. Waterline stripe at 0.34 m.",
    )
    a.loose_pivot = True
    a.climb_note = "The hull is a solid prop. Not a cling wall."
    a.vault_note = "Gunwale is about 0.70 m. Under the vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        n = lod_pick(lod, 28, 16)
        solid_hull(g, Z0, Z1, n, profile, _top, "Lib_PaintWhite", bow_extra=0.18, section_fn=section_skiff)
        _house(g, lod, seg)
        steps = lod_pick(lod, 12, 6)
        _rails(g, seg, steps)
        _stripe(g, seg, steps)
        _chine(g, seg, steps)
        if lod == 0:
            _planks(g)
            _motor(g, seg)
            _cleat(g, 0.0, 0.88, -2.05)
            _cleat(g, -0.28, 0.455, 1.35)
            _cleat(g, 0.28, 0.455, 1.35)
        a.end()
    a.box("Col_Bilge", (0, 0.11, 0.20), (0.16, 0.04, 1.0))
    a.box("Col_Bow", (0, 0.52, -1.85), (0.12, 0.06, 0.18))
    a.box("Col_Transom", (0, 0.46, 1.90), (0.28, 0.10, 0.08))
    a.box("Col_Cabin", (0, 1.08, -0.55), (0.64, 0.36, 0.48))
    a.box("Col_Motor", (0, 0.64, 2.40), (0.14, 0.14, 0.10))
    return a
