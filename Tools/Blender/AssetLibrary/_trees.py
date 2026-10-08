"""Shared canopy builders. Leaf masses are overlapping clusters, never one smooth cap."""

import math

from _common import lod_pick


# Scattered so the crown has holes. Outer clumps sit a little lower than the joint.
_SEEDS = (
    (0.12, 0.16, 0.04),
    (0.82, -0.10, 0.30),
    (-0.76, -0.18, 0.24),
    (0.28, 0.02, -0.86),
    (-0.36, 0.08, -0.74),
    (0.04, -0.28, 0.78),
    (-0.90, -0.34, -0.16),
    (0.64, -0.30, -0.28),
)


def _cluster(g, center, radius, mat, seg, count=6, droop=0.12):
    for i in range(count):
        ox, oy, oz = _SEEDS[i % len(_SEEDS)]
        rad = radius * (0.42 if i % 3 == 0 else 0.32)
        g.sphere(
            (
                center[0] + ox * radius,
                center[1] + oy * radius * 0.55 - droop * (0.4 + (i % 4) * 0.15),
                center[2] + oz * radius,
            ),
            rad,
            mat,
            seg,
        )


def _fork(g, a, b, radius, mat, seg, children=()):
    g.pipe(a, b, radius, mat, seg)
    for end, rad in children:
        mid = (
            (a[0] + b[0]) * 0.5,
            (a[1] + b[1]) * 0.5,
            (a[2] + b[2]) * 0.5,
        )
        g.pipe(mid, end, rad, mat, max(4, seg - 2))


def shade_trunk(g, lod, height, base_r, top_r):
    seg = lod_pick(lod, 10, 7)
    g.cylinder((0, 0.06, 0), base_r * 1.25, 0.12, "Lib_Bark", seg)
    g.cone((0, height * 0.5, 0), base_r, top_r, height, "Lib_Bark", seg)
    return seg


def broadleaf(g, lod, spec):
    """spec: list of (start, end, radius, cluster_r, mat)."""
    seg = shade_trunk(g, lod, spec["trunk"][0], spec["trunk"][1], spec["trunk"][2])
    cseg = lod_pick(lod, 6, 5, 4)
    count = lod_pick(lod, 6, 3, 2)
    for start, end, radius, cr, mat in spec["limbs"]:
        if lod > 0 and cr < 0.34:
            continue
        if lod > 1 and cr < 0.46:
            continue
        _fork(g, start, end, radius, "Lib_Bark", seg)
        _cluster(g, end, cr, mat, cseg, count, droop=0.08 + cr * 0.12)


def pine_tree(g, lod):
    seg = shade_trunk(g, lod, 3.4, 0.16, 0.05)
    cseg = lod_pick(lod, 5, 4)
    whorls = (
        (1.55, 1.15, 0.42, "Lib_Needle"),
        (2.25, 0.95, 0.36, "Lib_FoliageDark"),
        (2.95, 0.72, 0.30, "Lib_Needle"),
        (3.55, 0.48, 0.24, "Lib_FoliageDark"),
    )
    for y, reach, cr, mat in whorls:
        n = 5 if lod == 0 else 3
        for i in range(n):
            ang = (i / float(n)) * math.tau + y
            tip = (math.cos(ang) * reach, y + 0.15, math.sin(ang) * reach)
            g.pipe((0, y - 0.15, 0), tip, 0.025, "Lib_Bark", max(4, seg - 4))
            _cluster(
                g, (tip[0], tip[1] - 0.02, tip[2]), cr * 0.85, mat, cseg,
                lod_pick(lod, 4, 2, 1), droop=0.06 + cr * 0.08,
            )
    # Small dark tip. Not a pale cap.
    _cluster(g, (0, 4.15, 0), 0.22, "Lib_FoliageDark", cseg, 2)


def palm_tree(g, lod):
    """Leaning trunk and a crown of arching fronds.

    Map PR 9 does not ship a palm mesh. This is the street-palm read: a slender
    trunk and separate fronds, not a round canopy. The lower trunk is one cone
    so the collider sits in a single closed volume.
    """
    seg = lod_pick(lod, 8, 6, 5)
    g.cone((0.04, 1.20, 0.02), 0.16, 0.09, 2.4, "Lib_Bark", seg)
    crown_base = (0.18, 2.45, 0.04)
    crown = (0.48, 4.2, 0.1)
    g.pipe(crown_base, crown, 0.065, "Lib_Bark", seg)
    fronds = lod_pick(lod, 8, 5, 4)
    blades = lod_pick(lod, 4, 2, 2)
    for i in range(fronds):
        ang = math.tau * i / fronds
        mat = "Lib_PalmDry" if i % 4 == 0 else "Lib_Palm"
        prev = crown
        for k in range(1, blades + 1):
            t = k / float(blades)
            reach = t * 1.55
            droop = t * t * 0.85
            nxt = (
                crown[0] + math.cos(ang) * reach,
                crown[1] + 0.28 * math.sin(t * math.pi) - droop,
                crown[2] + math.sin(ang) * reach,
            )
            width = 0.16 * (1.15 - t)
            _frond_blade(g, prev, nxt, width, mat)
            prev = nxt


def _frond_blade(g, a, b, width, mat):
    dx, dy, dz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    length = math.sqrt(dx * dx + dy * dy + dz * dz) or 1.0
    sx, sy, sz = -dz / length, 0.0, dx / length
    hw = width * 0.5
    verts = [
        (a[0] + sx * hw, a[1], a[2] + sz * hw),
        (a[0] - sx * hw, a[1], a[2] - sz * hw),
        (b[0] - sx * hw * 0.55, b[1], b[2] - sz * hw * 0.55),
        (b[0] + sx * hw * 0.55, b[1], b[2] + sz * hw * 0.55),
    ]
    g.mesh(verts, [(0, 1, 2, 3), (3, 2, 1, 0)], mat)
