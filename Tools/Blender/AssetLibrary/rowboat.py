"""Wood rowboat. Smooth hull, overlapping lapstrake, two thwarts. Bow is -Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from _hull import beam_at, solid_hull

Z0 = -1.55
Z1 = 1.20


def profile(t):
    import math
    bow = math.sin(min(t / 0.24, 1.0) * math.pi * 0.5)
    aft = 0.70 + 0.30 * math.sin(min((1.0 - t) / 0.14, 1.0) * math.pi * 0.5)
    half = 0.50 * max(bow, 0.10) * aft
    keel = 0.0 + 0.14 * (1.0 - bow) ** 2 + 0.03 * (1.0 - aft) ** 2
    sheer = 0.50 + 0.16 * (1.0 - t) ** 1.5 + 0.04 * max(t - 0.8, 0.0) / 0.2
    return half, keel, sheer


# Beam and height fractions. The last knot tucks in for a little tumblehome.
_SECTION_KNOTS = (
    (0.00, 0.00),
    (0.10, 0.08),
    (0.28, 0.18),
    (0.52, 0.32),
    (0.78, 0.48),
    (0.96, 0.66),
    (1.02, 0.84),
    (0.98, 1.00),
)


def section_smooth(half, keel, sheer, t=0.5):
    """Round bilge. Twenty-one segments around, cosine-smoothed, monotonic in height."""
    import math
    del t
    rise = max(0.05, sheer - keel)
    parts = 3
    pts = []
    last = len(_SECTION_KNOTS) - 1
    for i in range(last):
        x0, y0 = _SECTION_KNOTS[i]
        x1, y1 = _SECTION_KNOTS[i + 1]
        steps = parts + 1 if i == last - 1 else parts
        for s in range(steps):
            u = s / float(parts)
            w = 0.5 - 0.5 * math.cos(u * math.pi)
            pts.append((half * (x0 + (x1 - x0) * w), keel + rise * (y0 + (y1 - y0) * w)))
    pts[0] = (0.0, keel)
    pts[-1] = (half * 0.98, sheer)
    return pts


def _top(t, half, sheer):
    xg = half * 0.96
    gy = sheer
    if t < 0.14:
        u = t / 0.14
        floor = gy - 0.02 - (gy - 0.20) * u
    elif t > 0.88:
        u = (t - 0.88) / 0.12
        floor = 0.20 + (gy - 0.22) * u
    else:
        floor = 0.18
    inner = max(0.06, xg - 0.05)
    return [
        (xg, gy),
        (inner, gy - 0.008),
        (inner, floor),
        (0.0, floor + 0.004),
        (-inner, floor),
        (-inner, gy - 0.008),
        (-xg, gy),
    ]


def _band(g, f0, f1, offset, thick, steps):
    """One lapstrake plank per side. Overlaps the next plank in height, not in radius."""
    verts = []
    rings = []
    for side in (1.0, -1.0):
        for i in range(steps + 1):
            t = 0.06 + 0.88 * i / steps
            half, keel, sheer = profile(t)
            rise = max(0.05, sheer - keel)
            z = Z0 + (Z1 - Z0) * t
            ring = []
            for frac, off in ((f0, offset), (f0, offset + thick), (f1, offset + thick), (f1, offset)):
                y = keel + rise * frac
                x = beam_at(profile, t, y, section_smooth) + off
                ring.append(len(verts))
                verts.append((side * x, y, z))
            rings.append(ring)
    # Each side is its own chain. steps+1 rings, then the other side.
    nring = steps + 1
    faces = []
    for base in (0, nring):
        chain = rings[base:base + nring]
        for s in range(nring - 1):
            a = chain[s]
            b = chain[s + 1]
            for i in range(4):
                j = (i + 1) % 4
                faces.append((a[i], b[i], b[j], a[j]))
        faces.append(tuple(chain[0]))
        faces.append(tuple(reversed(chain[-1])))
    g.mesh(verts, faces, "Lib_Varnish", uv_scale=1.0)


def _strakes(g, steps):
    # Radial step is about 12 mm, so the shells do not intersect.
    bands = (
        (0.08, 0.32, 0.008),
        (0.26, 0.50, 0.020),
        (0.44, 0.70, 0.032),
        (0.62, 0.90, 0.044),
    )
    for f0, f1, offset in bands:
        _band(g, f0, f1, offset, 0.006, steps)


def _gunwale(g, seg, steps):
    for side in (1.0, -1.0):
        pts = []
        for i in range(steps + 1):
            t = 0.04 + 0.92 * i / steps
            _half, _keel, sheer = profile(t)
            z = Z0 + (Z1 - Z0) * t
            x = side * (beam_at(profile, t, sheer, section_smooth) + 0.030)
            pts.append((x, sheer + 0.036, z))
        for i in range(steps):
            g.pipe(pts[i], pts[i + 1], 0.013, "Lib_Varnish", seg)


def _thwarts(g):
    for t in (0.38, 0.66):
        z = Z0 + (Z1 - Z0) * t
        g.box((0, 0.30, z), (0.52, 0.032, 0.11), "Lib_Varnish")
        g.box((0, 0.318, z), (0.46, 0.008, 0.09), "Lib_PaintCream")


def _oarlock(g, x, z, seg):
    t = (z - Z0) / (Z1 - Z0)
    _half, _keel, sheer = profile(t)
    y = sheer + 0.058
    g.cylinder((x, y, z - 0.028), 0.009, 0.05, "Lib_SteelDark", seg)
    g.cylinder((x, y, z + 0.028), 0.009, 0.05, "Lib_SteelDark", seg)
    g.pipe((x, y + 0.022, z - 0.028), (x, y + 0.022, z + 0.028), 0.007, "Lib_Steel", seg)


def _oar(g, side, x_lock, y_pin, z_lock):
    """Shaft centered on the oarlock pin so the loom sits between the horns."""
    g.box((x_lock, y_pin, z_lock), (1.16, 0.014, 0.022), "Lib_Varnish")
    g.box((x_lock + side * 0.70, y_pin, z_lock), (0.30, 0.008, 0.078), "Lib_PaintCream")


def _cleat(g, side, t):
    _half, _keel, sheer = profile(t)
    z = Z0 + (Z1 - Z0) * t
    x = side * (beam_at(profile, t, sheer, section_smooth) + 0.030)
    y = sheer + 0.062
    g.box((x, y, z), (0.07, 0.012, 0.04), "Lib_Steel")
    g.box((x, y + 0.014, z), (0.09, 0.010, 0.024), "Lib_SteelDark")


@register
def create():
    a = Asset(
        "Rowboat",
        "Harbor",
        "Rowboat about 2.9 m, bow to -Z. Smooth varnish hull with overlapping lapstrake planks, "
        "two thwarts, and oarlocks. Sit it so the midship topsides meet the water.",
    )
    a.loose_pivot = True
    a.climb_note = "Too small to cling."
    a.vault_note = "Gunwale is about 0.55 m. Not a vault."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6, 5)
        n = lod_pick(lod, 21, 12, 6)
        solid_hull(
            g, Z0, Z1, n, profile, _top, "Lib_Varnish",
            bow_extra=0.12, section_fn=section_smooth, bevel_stern=0.05,
        )
        steps = lod_pick(lod, 12, 6, 3)
        if lod < 2:
            _strakes(g, steps)
            _gunwale(g, seg, steps)
        else:
            _gunwale(g, seg, 4)
        _thwarts(g)
        g.box((0, 0.192, 0.02), (0.36, 0.008, 0.70), "Lib_PaintCream")
        if lod == 0:
            t_lock = 0.52
            z_lock = Z0 + (Z1 - Z0) * t_lock
            _half, _keel, sheer = profile(t_lock)
            x_lock = beam_at(profile, t_lock, sheer, section_smooth) + 0.030
            y_pin = sheer + 0.080
            _oarlock(g, x_lock, z_lock, seg)
            _oarlock(g, -x_lock, z_lock, seg)
            _oar(g, 1.0, x_lock, y_pin, z_lock)
            _oar(g, -1.0, -x_lock, y_pin, z_lock)
            _cleat(g, 1.0, 0.40)
            _cleat(g, -1.0, 0.40)
        a.end()
    a.box("Col_Bilge", (0, 0.10, 0.0), (0.14, 0.035, 0.50))
    a.box("Col_ThwartF", (0, 0.30, -0.505), (0.18, 0.012, 0.04))
    a.box("Col_ThwartA", (0, 0.30, 0.265), (0.18, 0.012, 0.04))
    a.box("Col_Transom", (0, 0.13, 0.72), (0.08, 0.03, 0.10))
    return a
