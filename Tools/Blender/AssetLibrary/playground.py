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


@register
def create():
    a = Asset(
        "Playground",
        "Park",
        "Play set about 7.4 x 4.8 m. A-frame swings with two chains per belt seat, a slide with rails and a curled lip, square posts to the mulch, guard panels, and stairs. Spring rider seat is about 0.6 m. Climbing dome is a geodesic of bars. Wood-chip patch inside a rubber border. Pivot is the pad center.",
    )
    a.climb_note = "Bars and posts are too small to cling. Not a wall."
    a.vault_note = "The deck panels are a guard, not a 0.90 m vault. The rider seat is about 0.6 m."
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
    a.box("Col_RiderBase", (-0.15, 0.11, -1.55), (0.40, 0.028, 0.28))
    a.box("Col_RiderBody", (-0.15, 0.86, -1.50), (0.26, 0.28, 0.42))
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
        # Two chains, spread to the seat edges. Nothing hangs in the middle.
        for dz in (-0.18, 0.18):
            g.pipe((apex_x, 2.38, z + dz), (apex_x - 0.02, 0.56, z + dz), 0.008, "Lib_Chain", 5)
        g.box((apex_x - 0.02, 0.52, z), (0.38, 0.04, 0.46), "Lib_Rubber", bevel=bev, segs=1)
        g.box((apex_x - 0.02, 0.47, z), (0.32, 0.02, 0.40), "Lib_Rubber")


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
    # Deck top 1.24. Stairs on -X, chute on +X, guard panels on the long sides.
    g.box((1.20, 1.20, 0.05), (1.10, 0.08, 1.00), "Lib_PaintBlue", bevel=bev, segs=1)
    posts = ((0.78, -0.32), (0.78, 0.42), (1.62, -0.32), (1.62, 0.42))
    for x, z in posts:
        # Square posts from just above the mulch to just under the deck.
        g.box((x, 0.612, z), (0.12, 1.084, 0.12), "Lib_Steel", bevel=bev, segs=1)
    if lod < 2:
        for x, z in posts:
            g.box((x, 1.58, z), (0.10, 0.66, 0.10), "Lib_PaintBlue")
        # Slat panels outside the posts, clear of the stair (-X) and the chute (+X).
        for z_out in (-0.404, 0.504):
            for y in (1.40, 1.56, 1.72):
                g.box((1.20, y, z_out), (0.64, 0.09, 0.028), "Lib_PaintBlue")
            g.box((1.20, 1.90, z_out), (0.68, 0.05, 0.036), "Lib_Wood")
        _stairs(g, lod)
    z0, z1 = -0.20, 0.30
    _slide_bed(g, slide, z0, z1, 0.04, "Lib_Orange")
    if lod < 2:
        for z in (-0.26, 0.36):
            for i in range(len(slide) - 1):
                x0, y0 = slide[i]
                x1, y1 = slide[i + 1]
                _gap_pipe(g, (x0, y0 + 0.06, z), (x1, y1 + 0.06, z), 0.016, "Lib_PaintRed", 0.018)


def _gap_pipe(g, a, b, radius, mat, gap):
    dx, dy, dz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    length = math.sqrt(dx * dx + dy * dy + dz * dz) or 1.0
    t = gap / length
    if t >= 0.45:
        return
    p0 = (a[0] + dx * t, a[1] + dy * t, a[2] + dz * t)
    p1 = (b[0] - dx * t, b[1] - dy * t, b[2] - dz * t)
    g.pipe(p0, p1, radius, mat, 5)


def _slide_bed(g, pts, z0, z1, thick, mat):
    """One closed chute. Separate slabs at the bends were flipping collider tests."""
    seg_n = []
    for i in range(len(pts) - 1):
        dx = pts[i + 1][0] - pts[i][0]
        dy = pts[i + 1][1] - pts[i][1]
        length = math.hypot(dx, dy) or 1.0
        nx, ny = -dy / length, dx / length
        if ny < 0.0:
            nx, ny = -nx, -ny
        seg_n.append((nx, ny))
    offs = []
    for i in range(len(pts)):
        if i == 0:
            nx, ny = seg_n[0]
        elif i == len(pts) - 1:
            nx, ny = seg_n[-1]
        else:
            ax, ay = seg_n[i - 1]
            bx, by = seg_n[i]
            mx, my = ax + bx, ay + by
            length = math.hypot(mx, my) or 1.0
            mx, my = mx / length, my / length
            dot = mx * bx + my * by
            if dot > 0.25:
                mx, my = mx / dot, my / dot
            nx, ny = mx, my
        offs.append((pts[i][0] - nx * thick, pts[i][1] - ny * thick))
    verts = []
    for i, (x, y) in enumerate(pts):
        ox, oy = offs[i]
        verts.extend(((x, y, z0), (x, y, z1), (ox, oy, z0), (ox, oy, z1)))
    faces = []
    for i in range(len(pts) - 1):
        a = i * 4
        b = a + 4
        faces.append((a, b, b + 1, a + 1))
        faces.append((a + 2, a + 3, b + 3, b + 2))
        faces.append((a, a + 2, b + 2, b))
        faces.append((a + 1, b + 1, b + 3, a + 3))
    faces.append((0, 1, 3, 2))
    last = (len(pts) - 1) * 4
    faces.append((last, last + 2, last + 3, last + 1))
    g.mesh(verts, faces, mat, uv_scale=1.0)


def _stairs(g, lod):
    # Five treads up to the deck. Each tread stops short of the one above it.
    for i in range(5):
        depth = 0.20
        x0 = i * 0.15
        top = 0.269 + i * 0.194
        bottom = 0.078 if i == 0 else top - 0.16
        height = top - bottom
        g.box((x0 + depth * 0.5, bottom + height * 0.5, 0.05), (depth, height, 0.50), "Lib_Steel")
    if lod == 2:
        return
    g.pipe((0.02, 0.10, -0.46), (0.72, 1.08, -0.46), 0.018, "Lib_Steel", 5)
    g.pipe((0.02, 0.10, 0.56), (0.72, 1.08, 0.56), 0.018, "Lib_Steel", 5)


def _slide_cols(a, slide):
    a.box("Col_Deck", (1.20, 1.20, 0.05), (1.00, 0.05, 0.90))
    for i, (x, z) in enumerate(((0.78, -0.32), (0.78, 0.42), (1.62, -0.32), (1.62, 0.42))):
        a.box("Col_DeckPost_%d" % i, (x, 0.612, z), (0.08, 0.96, 0.08))
        a.box("Col_Guard_%d" % i, (x, 1.58, z), (0.06, 0.52, 0.06))
    z0, z1 = -0.20, 0.30
    for i in range(len(slide) - 1):
        x0, y0 = slide[i]
        x1, y1 = slide[i + 1]
        _ramp_col(a, "Col_Slide_%d" % i, x0, y0, x1, y1, z0, z1, 0.04)
    for i in range(5):
        depth = 0.20
        x0 = i * 0.15
        top = 0.269 + i * 0.194
        bottom = 0.078 if i == 0 else top - 0.16
        height = top - bottom
        a.box(
            "Col_Stair_%d" % i,
            (x0 + depth * 0.5, bottom + height * 0.5, 0.05),
            (depth * 0.72, height * 0.72, 0.36),
        )


def _rider(g, lod, sx, sz):
    g.box((sx, 0.11, sz), (0.52, 0.04, 0.38), "Lib_SteelDark")
    turns = 6 if lod == 0 else 4
    radius = 0.10
    for i in range(turns):
        a0 = math.radians(i * (320.0 / turns))
        a1 = math.radians((i + 1) * (320.0 / turns))
        y0 = 0.16 + i * 0.068
        y1 = 0.16 + (i + 1) * 0.068
        p0 = (sx + radius * math.sin(a0), y0, sz + radius * math.cos(a0))
        p1 = (sx + radius * math.sin(a1), y1, sz + radius * math.cos(a1))
        g.pipe(p0, p1, 0.016, "Lib_Steel", 5)
    # Seat about 0.6 m long, surface near 0.65 m.
    g.box((sx, 0.62, sz), (0.60, 0.06, 0.36), "Lib_PaintRed")
    g.box((sx, 0.86, sz + 0.05), (0.36, 0.40, 0.58), "Lib_PaintYellow")
    g.sphere((sx, 1.20, sz + 0.42), 0.12, "Lib_PaintYellow", 8)
    if lod == 0:
        g.pipe((sx - 0.24, 0.78, sz + 0.06), (sx - 0.24, 1.00, sz + 0.20), 0.014, "Lib_Steel", 4)
        g.pipe((sx + 0.24, 0.78, sz + 0.06), (sx + 0.24, 1.00, sz + 0.20), 0.014, "Lib_Steel", 4)
        g.pipe((sx - 0.24, 1.00, sz + 0.20), (sx + 0.24, 1.00, sz + 0.20), 0.014, "Lib_Steel", 4)


def _unit(p):
    length = math.sqrt(p[0] * p[0] + p[1] * p[1] + p[2] * p[2]) or 1.0
    return (p[0] / length, p[1] / length, p[2] / length)


def _ico():
    t = (1.0 + math.sqrt(5.0)) * 0.5
    verts = [_unit(p) for p in (
        (-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0),
        (0, -1, t), (0, 1, t), (0, -1, -t), (0, 1, -t),
        (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1),
    )]
    best = 99.0
    for i in range(12):
        for j in range(i + 1, 12):
            best = min(best, math.dist(verts[i], verts[j]))
    edges = []
    linked = set()
    for i in range(12):
        for j in range(i + 1, 12):
            if abs(math.dist(verts[i], verts[j]) - best) < 0.03:
                edges.append((i, j))
                linked.add((i, j))
    tris = []
    for i in range(12):
        for j in range(i + 1, 12):
            if (i, j) not in linked:
                continue
            for k in range(j + 1, 12):
                if (i, k) in linked and (j, k) in linked:
                    tris.append((i, j, k))
    return verts, edges, tris


def _dome(g, lod, ox, oz):
    """Frequency-2 geodesic hemisphere. Bars stop short of shared hubs."""
    if lod >= 2:
        return
    verts, edges, tris = _ico()
    segs = []
    if lod == 0:
        cache = {}

        def mid(i, j):
            key = (i, j) if i < j else (j, i)
            if key not in cache:
                cache[key] = _unit(tuple((verts[i][k] + verts[j][k]) * 0.5 for k in range(3)))
            return cache[key]

        seen = set()

        def add(a, b):
            ka = tuple(round(c, 4) for c in a)
            kb = tuple(round(c, 4) for c in b)
            key = (ka, kb) if ka < kb else (kb, ka)
            if key in seen:
                return
            seen.add(key)
            segs.append((a, b))

        for i, j, k in tris:
            if verts[i][1] < -0.05 and verts[j][1] < -0.05 and verts[k][1] < -0.05:
                continue
            vi, vj, vk = verts[i], verts[j], verts[k]
            mij, mjk, mki = mid(i, j), mid(j, k), mid(k, i)
            for a, b in (
                (vi, mij), (mij, vj), (vj, mjk), (mjk, vk), (vk, mki), (mki, vi),
                (mij, mjk), (mjk, mki), (mki, mij),
            ):
                if a[1] < -0.02 and b[1] < -0.02:
                    continue
                add(a, b)
    else:
        for i, j in edges:
            if verts[i][1] < -0.02 and verts[j][1] < -0.02:
                continue
            segs.append((verts[i], verts[j]))
    radius = 0.78
    base = 0.12
    tube = 0.016

    def place(p):
        return (ox + p[0] * radius, base + max(p[1], 0.0) * radius, oz + p[2] * radius)

    if lod == 0:
        hubs = {}
        for a, b in segs:
            hubs[tuple(round(c, 4) for c in a)] = a
            hubs[tuple(round(c, 4) for c in b)] = b
        for p in hubs.values():
            g.sphere(place(p), 0.022, "Lib_Steel", 5)
    gap = 0.036
    for a, b in segs:
        wa, wb = place(a), place(b)
        dist = math.dist(wa, wb)
        if dist < gap * 2.4:
            continue
        t0 = gap / dist
        pa = tuple(wa[k] + (wb[k] - wa[k]) * t0 for k in range(3))
        pb = tuple(wa[k] + (wb[k] - wa[k]) * (1.0 - t0) for k in range(3))
        g.pipe(pa, pb, tube, "Lib_PaintGreen", 4)
