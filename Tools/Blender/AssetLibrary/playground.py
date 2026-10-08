"""Playground set. A-frame swings, a railed slide, a spring rider, and a climbing dome."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _ramp(g, x0, y0, x1, y1, z0, z1, thick, mat):
    dx, dy = x1 - x0, y1 - y0
    length = math.hypot(dx, dy) or 1.0
    nx, ny = -dy / length, dx / length
    if ny < 0.0:
        nx, ny = -nx, -ny
    o0 = (x0 - nx * thick, y0 - ny * thick)
    o1 = (x1 - nx * thick, y1 - ny * thick)
    verts = [
        (x0, y0, z0), (x1, y1, z0), (x1, y1, z1), (x0, y0, z1),
        (o0[0], o0[1], z0), (o1[0], o1[1], z0), (o1[0], o1[1], z1), (o0[0], o0[1], z1),
    ]
    g.mesh(verts, [
        (0, 1, 2, 3), (4, 7, 6, 5),
        (0, 3, 7, 4), (1, 5, 6, 2),
        (0, 4, 5, 1), (3, 2, 6, 7),
    ], mat, uv_scale=1.0)


def _ramp_col(a, name, x0, y0, x1, y1, z0, z1, thick):
    dx, dy = x1 - x0, y1 - y0
    length = math.hypot(dx, dy) or 1.0
    nx, ny = -dy / length, dx / length
    if ny < 0.0:
        nx, ny = -nx, -ny
    mx = (x0 + x1) * 0.5 - nx * thick * 0.5
    my = (y0 + y1) * 0.5 - ny * thick * 0.5
    roll = math.degrees(math.atan2(dy, dx))
    a.box(name, (mx, my, (z0 + z1) * 0.5), (length * 0.82, thick * 0.5, (z1 - z0) * 0.72), euler=(0.0, 0.0, roll))


def _leg_col(a, name, p0, p1, radius):
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    length = math.hypot(dx, dy) or 1.0
    center = ((p0[0] + p1[0]) * 0.5, (p0[1] + p1[1]) * 0.5, p0[2])
    roll = math.degrees(math.atan2(dy, dx))
    cross = radius * 1.15
    a.box(name, center, (length * 0.84, cross, cross), euler=(0.0, 0.0, roll))


def _arch(g, origin, radius, axis, tube, mat, steps):
    """Two sides of a semicircle. The crown is left open for a shared hub."""
    ox, oy, oz = origin
    for start, end in ((0.22, 1.22), (1.92, 2.92)):
        pts = []
        for i in range(steps + 1):
            ang = start + (end - start) * i / steps
            c, s = math.cos(ang), math.sin(ang)
            if axis == "x":
                pts.append((ox + radius * c, oy + radius * s, oz))
            else:
                pts.append((ox, oy + radius * s, oz + radius * c))
        for i in range(steps):
            g.pipe(pts[i], pts[i + 1], tube, mat, 5)


@register
def create():
    a = Asset(
        "Playground",
        "Park",
        "Play set about 7.4 x 4.8 m. A-frame swings with belt seats, a slide with rails and a curled lip, a guarded deck and ladder, a spring rider, and a climbing dome. Wood-chip patch inside a rubber border. Pivot is the pad center.",
    )
    a.climb_note = "Bars and posts are too small to cling. Not a wall."
    a.vault_note = "The deck rail is a guard, not a 0.90 m vault. Seats are at 0.50 m."
    # Slide bed, top surface, running +X and curling up at the lip.
    slide = ((1.78, 1.16), (2.16, 0.74), (2.52, 0.36), (2.86, 0.16), (3.16, 0.22))
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0
        g.box((0, 0.02, 0), (7.4, 0.04, 4.8), "Lib_Rubber", bevel=bev, segs=1, uv_scale=0.5)
        g.box((0, 0.055, 0), (6.2, 0.026, 3.6), "Lib_Mulch", uv_scale=0.7)
        _swings(g, lod, bev)
        _slide(g, lod, bev, slide)
        if lod < 2:
            _rider(g, lod, -0.15, -1.55)
            _dome(g, lod, 0.25, 1.48)
        else:
            g.sphere((-0.15, 0.55, -1.55), 0.16, "Lib_PaintYellow", 6)
            g.sphere((0.25, 0.32, 1.48), 0.28, "Lib_PaintBlue", 6)
        a.end()
    a.box("Col_Pad", (0, 0.02, 0), (7.2, 0.03, 4.6))
    a.box("Col_Mulch", (0, 0.055, 0), (6.0, 0.02, 3.4))
    _swing_cols(a)
    _slide_cols(a, slide)
    a.box("Col_RiderBase", (-0.15, 0.098, -1.55), (0.28, 0.024, 0.20))
    a.box("Col_RiderBody", (-0.15, 0.62, -1.53), (0.14, 0.10, 0.22))
    return a


def _swings(g, lod, bev):
    apex_x = -1.95
    # Two A-frames. Each leg stays in its own Z plane so the pair does not cross.
    for z_end, sign in ((-1.28, -1.0), (1.28, 1.0)):
        z_back = z_end + 0.07 * sign
        z_front = z_end - 0.07 * sign
        g.pipe((-2.62, 0.16, z_back), (apex_x, 2.34, z_back), 0.034, "Lib_PaintBlue", 6)
        g.pipe((-1.28, 0.16, z_front), (apex_x, 2.34, z_front), 0.034, "Lib_PaintBlue", 6)
        g.box((-2.62, 0.09, z_back), (0.16, 0.04, 0.12), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((-1.28, 0.09, z_front), (0.16, 0.04, 0.12), "Lib_SteelDark", bevel=bev, segs=1)
    g.pipe((apex_x, 2.44, -1.46), (apex_x, 2.44, 1.46), 0.04, "Lib_PaintBlue", 6)
    if lod == 2:
        return
    for z in (-0.46, 0.46):
        for dz in (-0.07, 0.07):
            g.pipe((apex_x, 2.38, z + dz), (apex_x - 0.06, 0.56, z + dz), 0.008, "Lib_Chain", 5)
        g.box((apex_x - 0.04, 0.50, z), (0.34, 0.035, 0.14), "Lib_Rubber", bevel=bev, segs=1)
        g.box((apex_x - 0.04, 0.455, z), (0.30, 0.02, 0.16), "Lib_Rubber")
        if lod == 0:
            g.box((apex_x + 0.08, 0.56, z), (0.03, 0.10, 0.12), "Lib_Rubber")


def _swing_cols(a):
    apex_x = -1.95
    for i, (z_end, sign) in enumerate(((-1.28, -1.0), (1.28, 1.0))):
        z_back = z_end + 0.07 * sign
        z_front = z_end - 0.07 * sign
        _leg_col(a, "Col_LegA_%d" % i, (-2.62, 0.16, z_back), (apex_x, 2.34, z_back), 0.034)
        _leg_col(a, "Col_LegB_%d" % i, (-1.28, 0.16, z_front), (apex_x, 2.34, z_front), 0.034)
    a.box("Col_Beam", (apex_x, 2.44, 0), (0.055, 0.055, 2.70))
    for i, z in enumerate((-0.46, 0.46)):
        a.box("Col_Seat_%d" % i, (apex_x - 0.04, 0.50, z), (0.26, 0.02, 0.10))


def _slide(g, lod, bev, slide):
    # Deck top 1.24. Ladder on -X, chute on +X, guards on the other three sides.
    g.box((1.20, 1.20, 0.05), (1.10, 0.08, 1.00), "Lib_PaintBlue", bevel=bev, segs=1)
    posts = ((0.78, -0.32), (0.78, 0.42), (1.62, -0.32), (1.62, 0.42))
    for x, z in posts:
        # Stop 2 cm under the deck so the post and the slab do not share a face.
        g.box((x, 0.62, z), (0.06, 1.00, 0.06), "Lib_Steel", bevel=bev, segs=1)
    if lod < 2:
        # Guard posts stand on the deck and stop short of the top rail.
        for x, z in ((0.78, -0.32), (0.78, 0.42), (1.62, -0.32), (1.62, 0.42)):
            g.box((x, 1.54, z), (0.045, 0.52, 0.045), "Lib_PaintBlue")
        g.pipe((0.78, 1.86, -0.32), (1.62, 1.86, -0.32), 0.02, "Lib_PaintBlue", 5)
        g.pipe((0.78, 1.86, 0.42), (1.62, 1.86, 0.42), 0.02, "Lib_PaintBlue", 5)
        g.pipe((0.78, 1.86, -0.32), (0.78, 1.86, 0.42), 0.02, "Lib_PaintBlue", 5)
        if lod == 0:
            for x, z0, z1 in ((0.78, -0.32, 0.42), (1.62, -0.32, 0.42)):
                g.pipe((x, 1.55, z0), (x, 1.55, z1), 0.014, "Lib_Steel", 5)
            g.pipe((0.78, 1.55, -0.32), (1.62, 1.55, -0.32), 0.014, "Lib_Steel", 5)
        # Ladder rails tie into the deck edge.
        for z in (-0.18, 0.22):
            g.pipe((0.42, 0.10, z), (0.70, 1.22, z), 0.022, "Lib_Steel", 6)
        for i in range(4):
            t = (i + 1) / 5.0
            y = 0.10 + (1.22 - 0.10) * t
            x = 0.42 + (0.70 - 0.42) * t
            g.box((x, y, 0.02), (0.025, 0.02, 0.32), "Lib_Steel")
    z0, z1 = -0.20, 0.30
    for i in range(len(slide) - 1):
        x0, y0 = slide[i]
        x1, y1 = slide[i + 1]
        _ramp(g, x0, y0, x1, y1, z0, z1, 0.04, "Lib_Orange")
    if lod < 2:
        for z in (-0.26, 0.36):
            for i in range(len(slide) - 1):
                x0, y0 = slide[i]
                x1, y1 = slide[i + 1]
                g.pipe((x0, y0 + 0.06, z), (x1, y1 + 0.06, z), 0.016, "Lib_PaintRed", 5)


def _slide_cols(a, slide):
    a.box("Col_Deck", (1.20, 1.20, 0.05), (1.00, 0.05, 0.90))
    for i, (x, z) in enumerate(((0.78, -0.32), (0.78, 0.42), (1.62, -0.32), (1.62, 0.42))):
        a.box("Col_DeckPost_%d" % i, (x, 0.62, z), (0.04, 0.90, 0.04))
    z0, z1 = -0.20, 0.30
    for i in range(len(slide) - 1):
        x0, y0 = slide[i]
        x1, y1 = slide[i + 1]
        _ramp_col(a, "Col_Slide_%d" % i, x0, y0, x1, y1, z0, z1, 0.04)
    for i, z in enumerate((-0.18, 0.22)):
        _leg_col(a, "Col_Ladder_%d" % i, (0.42, 0.10, z), (0.70, 1.22, z), 0.022)


def _rider(g, lod, sx, sz):
    g.box((sx, 0.098, sz), (0.36, 0.036, 0.26), "Lib_SteelDark")
    turns = 5 if lod == 0 else 3
    radius = 0.065
    for i in range(turns):
        a0 = math.radians(i * (300.0 / turns))
        a1 = math.radians((i + 1) * (300.0 / turns))
        y0 = 0.14 + i * 0.055
        y1 = 0.14 + (i + 1) * 0.055
        p0 = (sx + radius * math.sin(a0), y0, sz + radius * math.cos(a0))
        p1 = (sx + radius * math.sin(a1), y1, sz + radius * math.cos(a1))
        g.pipe(p0, p1, 0.012, "Lib_Steel", 5)
    g.box((sx, 0.50, sz), (0.28, 0.04, 0.20), "Lib_PaintRed")
    g.box((sx, 0.62, sz + 0.02), (0.18, 0.14, 0.30), "Lib_PaintYellow")
    g.sphere((sx, 0.78, sz + 0.20), 0.07, "Lib_PaintYellow", 8)
    if lod == 0:
        g.pipe((sx - 0.07, 0.66, sz + 0.06), (sx - 0.07, 0.82, sz + 0.14), 0.01, "Lib_Steel", 4)
        g.pipe((sx + 0.07, 0.66, sz + 0.06), (sx + 0.07, 0.82, sz + 0.14), 0.01, "Lib_Steel", 4)
        g.pipe((sx - 0.07, 0.82, sz + 0.14), (sx + 0.07, 0.82, sz + 0.14), 0.01, "Lib_Steel", 4)


def _dome(g, lod, ox, oz):
    steps = lod_pick(lod, 4, 3, 3)
    tube = 0.02
    origin_y = 0.12
    radius = 0.44
    _arch(g, (ox, origin_y, oz), radius, "x", tube, "Lib_PaintGreen", steps)
    _arch(g, (ox, origin_y, oz), radius, "z", tube, "Lib_PaintGreen", steps)
    g.sphere((ox, origin_y + radius, oz), 0.045, "Lib_Steel", 6)
    if lod > 0:
        return
    diag = []
    for start, end in ((0.28, 1.15), (1.99, 2.86)):
        pts = []
        for i in range(steps + 1):
            ang = start + (end - start) * i / max(1, steps)
            c, s = math.cos(ang), math.sin(ang)
            pts.append((ox + radius * c * 0.72, origin_y + radius * s, oz + radius * c * 0.72))
        diag.append(pts)
    for pts in diag:
        for i in range(len(pts) - 1):
            g.pipe(pts[i], pts[i + 1], tube, "Lib_PaintBlue", 5)
