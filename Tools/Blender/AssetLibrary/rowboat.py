"""Wood rowboat. Curved bow, sheer, keel, transom, two thwarts, oars. Bow is -Z."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from _hull import beam_at, solid_hull

Z0 = -1.55
Z1 = 1.20
WL = 0.26


def profile(t):
    bow = math.sin(min(t / 0.24, 1.0) * math.pi * 0.5)
    aft = 0.70 + 0.30 * math.sin(min((1.0 - t) / 0.14, 1.0) * math.pi * 0.5)
    half = 0.50 * max(bow, 0.10) * aft
    keel = 0.0 + 0.14 * (1.0 - bow) ** 2 + 0.03 * (1.0 - aft) ** 2
    sheer = 0.50 + 0.16 * (1.0 - t) ** 1.5 + 0.04 * max(t - 0.8, 0.0) / 0.2
    return half, keel, sheer


def _top(t, half, sheer):
    xg = half * 0.96
    gy = sheer
    seat = 0.30
    floor = 0.18
    inner = max(0.08, xg - 0.045)
    if 0.32 <= t <= 0.42 or 0.60 <= t <= 0.70:
        return [
            (xg, gy),
            (inner, gy - 0.012),
            (inner * 0.55, seat),
            (0.0, seat + 0.006),
            (-inner * 0.55, seat),
            (-inner, gy - 0.012),
            (-xg, gy),
        ]
    if 0.16 <= t <= 0.88:
        return [
            (xg, gy),
            (inner, gy - 0.012),
            (inner, floor),
            (0.0, floor + 0.006),
            (-inner, floor),
            (-inner, gy - 0.012),
            (-xg, gy),
        ]
    pts = []
    for i in range(7):
        u = i / 6.0
        x = xg * (1.0 - 2.0 * u)
        pts.append((x, gy - 0.01))
    pts[0] = (xg, gy)
    pts[-1] = (-xg, gy)
    return pts


def _oarlock(g, x, z, seg):
    _half, _keel, sheer = profile((z - Z0) / (Z1 - Z0))
    y = sheer + 0.04
    g.cylinder((x, y, z - 0.025), 0.008, 0.045, "Lib_SteelDark", seg)
    g.cylinder((x, y, z + 0.025), 0.008, 0.045, "Lib_SteelDark", seg)
    g.pipe((x, y + 0.02, z - 0.025), (x, y + 0.02, z + 0.025), 0.006, "Lib_Steel", seg)


def _strakes(g, seg, steps):
    """Lapstrake seams, proud of the skin. Kept off the white waterline."""
    for frac in (0.20, 0.40, 0.62, 0.80):
        for side in (1.0, -1.0):
            pts = []
            count = steps if frac > 0.3 else max(3, steps - 1)
            for i in range(count + 1):
                t = 0.07 + 0.86 * i / count
                half, keel, sheer = profile(t)
                rise = max(0.05, sheer - keel)
                y = keel + rise * frac
                z = Z0 + (Z1 - Z0) * t
                x = side * (beam_at(profile, t, y) + 0.014)
                pts.append((x, y, z))
            for i in range(count):
                g.pipe(pts[i], pts[i + 1], 0.006, "Lib_Black", seg)


def _gunwale(g, seg, steps):
    for side in (1.0, -1.0):
        pts = []
        for i in range(steps + 1):
            t = 0.03 + 0.94 * i / steps
            half, _keel, sheer = profile(t)
            z = Z0 + (Z1 - Z0) * t
            x = side * (half * 0.96 + 0.038)
            pts.append((x, sheer + 0.020, z))
        for i in range(steps):
            g.pipe(pts[i], pts[i + 1], 0.016, "Lib_Varnish", seg)


def _interior(g):
    # Painted cockpit, a few millimetres above the hull skin.
    g.box((0, 0.196, 0.02), (0.46, 0.008, 0.82), "Lib_PaintCream")
    g.box((0, 0.322, -0.53), (0.48, 0.010, 0.14), "Lib_PaintCream")
    g.box((0, 0.322, 0.24), (0.48, 0.010, 0.14), "Lib_PaintCream")


def _cleat(g, side, t):
    half, _keel, sheer = profile(t)
    z = Z0 + (Z1 - Z0) * t
    x = side * (half * 0.96 + 0.038)
    y = sheer + 0.052
    g.box((x, y, z), (0.08, 0.014, 0.045), "Lib_Steel")
    g.box((x, y + 0.018, z), (0.10, 0.012, 0.028), "Lib_SteelDark")


def _oar(g, side, z):
    # Shaft and blade sit above the gunwale so they rest in the lock.
    _half, _keel, sheer = profile(0.5)
    y = sheer + 0.085
    reach = side * 0.48
    g.box((reach, y, z), (1.00, 0.014, 0.028), "Lib_Varnish")
    blade = side * 1.16
    g.box((blade, y, z), (0.26, 0.012, 0.08), "Lib_PaintCream")


@register
def create():
    a = Asset(
        "Rowboat",
        "Harbor",
        "Rowboat about 2.9 m, bow to -Z. Uniform varnish hull with lapstrake seams, "
        "a painted cockpit, and a gunwale rail. Two thwarts, oarlocks, oars, and cleats. "
        "Waterline is 0.26 m; place it so that meets the water.",
    )
    a.loose_pivot = True
    a.climb_note = "Too small to cling."
    a.vault_note = "Gunwale is about 0.50 m. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        n = lod_pick(lod, 14, 8)
        solid_hull(g, Z0, Z1, n, profile, _top, "Lib_Varnish", bow_extra=0.12)
        steps = lod_pick(lod, 8, 4)
        _strakes(g, seg, steps)
        _gunwale(g, seg, steps)
        _interior(g)
        for side in (1.0, -1.0):
            pts = []
            for i in range(steps + 1):
                t = 0.08 + 0.84 * i / steps
                z = Z0 + (Z1 - Z0) * t
                x = side * (beam_at(profile, t, WL) + 0.028)
                pts.append((x, WL, z))
            for i in range(steps):
                g.pipe(pts[i], pts[i + 1], 0.008, "Lib_PaintWhite", seg)
        if lod == 0:
            z_lock = Z0 + (Z1 - Z0) * 0.52
            x_lock = beam_at(profile, 0.52, profile(0.52)[2]) + 0.03
            _oarlock(g, x_lock, z_lock, seg)
            _oarlock(g, -x_lock, z_lock, seg)
            _oar(g, 1.0, z_lock + 0.08)
            _oar(g, -1.0, z_lock - 0.10)
            _cleat(g, 1.0, 0.40)
            _cleat(g, -1.0, 0.40)
        a.end()
    a.box("Col_Bilge", (0, 0.10, 0.0), (0.20, 0.05, 0.70))
    a.box("Col_ThwartF", (0, 0.285, -0.53), (0.22, 0.014, 0.05))
    a.box("Col_ThwartA", (0, 0.285, 0.24), (0.22, 0.014, 0.05))
    a.box("Col_Transom", (0, 0.30, 1.05), (0.28, 0.10, 0.06))
    return a
