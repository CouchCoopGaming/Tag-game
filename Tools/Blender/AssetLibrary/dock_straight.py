"""Wooden dock straight module. 6.0 m along Z, 3.0 m across, deck at 0.62 m.

The side is open: gapped planks, one 5 by 20 cm stringer, cross-braces, then water.
Piles are 30 cm rounds from below the water up to the stringer. The ladder is round tube.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

LENGTH = 6.0
WIDTH = 3.0
PLANKS = 24
PLANK_GAP = 0.050
PILE_R = 0.15
PILE_XS = (-1.08, 0.0, 1.08)
PILE_ZS = (-2.40, 0.0, 2.40)
PILE_BOT = -1.22
# 6 mm under the stringer so the two closed shells do not weld.
PILE_TOP = 0.370
PLANK_TOP = 0.618
PLANK_T = 0.036
# Stringer 5 cm thick and 20 cm deep, sitting directly under the planks.
STRINGER_X = 1.16
STRINGER_T = 0.05
STRINGER_D = 0.20
STRINGER_Y = 0.476


@register
def create():
    a = Asset(
        "Dock_Straight",
        "Harbor",
        "Dock module 6.0 m long and 3.0 m wide. Deck top is 0.62 m. "
        "The side is open: flat deck planks within 3 mm of one height, one 5 by 20 cm stringer, then flat steel straps bolted pile to pile. "
        "Piles are 30 cm rounds from below the water (darker under the line) up to the stringer. "
        "A round ladder on -X is bolted to the stringer, curves over the deck as grab handles, and runs down into the water. "
        "Cleats, two bollards, and a rope that lies on the deck. "
        "Butt the next module on the Z ends (center to center 6.0 m).",
    )
    a.allow_below = True
    a.loose_pivot = True
    a.climb_note = "Deck is a walk surface. Pilings are round, not cling panels."
    a.vault_note = "No rail on this module. Deck height is 0.62 m, under a vault."
    piles = [(x, z) for x in PILE_XS for z in PILE_ZS]
    for lod in (0, 1, 2):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        for x, z in piles:
            _pile(g, x, z, seg if lod == 0 else 6)
        _frame(g, lod)
        _planks(g, lod)
        if lod == 0:
            _braces(g)
            for x, z in ((-1.20, -1.55), (-1.20, 1.55), (1.20, -1.55), (1.20, 1.55)):
                _cleat(g, x, z)
            _bollard(g, -0.55, -1.85, seg)
            _bollard(g, 0.85, 1.70, seg)
            _ladder(g)
            _rope(g)
        a.end()
    for i, (x, z) in enumerate(piles):
        a.capsule("Col_Pile_%d" % i, (x, -0.425, z), 0.12, 1.48, 1)
    for i, x in enumerate((-STRINGER_X, 0.0, STRINGER_X)):
        a.box("Col_Stringer_%d" % i, (x, STRINGER_Y, 0.0), (0.036, 0.16, 5.20))
    pitch = LENGTH / PLANKS
    board = pitch - PLANK_GAP
    for i in range(PLANKS):
        z = -LENGTH * 0.5 + pitch * (i + 0.5)
        y = PLANK_TOP - PLANK_T * 0.5 + _plank_lift(i)
        a.box("Col_Plank_%d" % i, (0, y, z), (2.40, PLANK_T * 0.7, board * 0.86))
    for i, (x, z) in enumerate(((-1.20, -1.55), (-1.20, 1.55), (1.20, -1.55), (1.20, 1.55))):
        a.box("Col_Cleat_%d" % i, (x, 0.706, z), (0.18, 0.020, 0.036))
    for i, (x, z) in enumerate(((-0.55, -1.85), (0.85, 1.70))):
        a.capsule("Col_Bollard_%d" % i, (x, 0.82, z), 0.05, 0.26, 1)
    for i, dz in enumerate((-0.17, 0.17)):
        a.capsule("Col_Ladder_%d" % i, (-1.50, -0.32, dz), 0.012, 1.58, 1)
        a.capsule("Col_Grab_%d" % i, (-1.08, 0.862, dz), 0.008, 0.10, 0)
    return a


def _pile(g, x, z, seg):
    """30 cm round from below the water to the stringer. Darker under the line, dry above it."""
    g.cylinder_bands(x, z, PILE_R, [
        (PILE_BOT, -0.05, "Lib_PileWet"),
        (-0.05, 0.015, "Lib_PileAlgae"),
        (0.015, PILE_TOP, "Lib_Pile"),
    ], seg)


def _frame(g, lod):
    """One timber per line, directly under the planks. Joists stay behind the stringer faces."""
    for x in (-STRINGER_X, 0.0, STRINGER_X):
        _one_timber(g, (x, STRINGER_Y, 0.0), (STRINGER_T, STRINGER_D, 5.50))
    count = lod_pick(lod, 7, 4)
    # Two bays. Each joist stops short of the stringers so the shells do not overlap,
    # and the ends stay behind the outer stringer face.
    x_outer = STRINGER_X - STRINGER_T * 0.5 - 0.012
    x_inner = STRINGER_T * 0.5 + 0.012
    span = x_outer - x_inner
    for i in range(count):
        z = -2.40 + (4.80 * i / (count - 1))
        for sign in (-1.0, 1.0):
            cx = sign * (x_inner + span * 0.5)
            g.box((cx, 0.548, z), (span, 0.050, 0.06), "Lib_Wood", uv_scale=1.0, grain=1.0)


def _one_timber(g, center, size):
    """A box whose UVs stay inside one board, so the face is a single timber."""
    cx, cy, cz = center
    hx, hy, hz = size[0] * 0.5, size[1] * 0.5, size[2] * 0.5
    corners = (
        (cx - hx, cy - hy, cz - hz),
        (cx + hx, cy - hy, cz - hz),
        (cx + hx, cy + hy, cz - hz),
        (cx - hx, cy + hy, cz - hz),
        (cx - hx, cy - hy, cz + hz),
        (cx + hx, cy - hy, cz + hz),
        (cx + hx, cy + hy, cz + hz),
        (cx - hx, cy + hy, cz + hz),
    )
    quads = (
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (3, 2, 6, 7),
        (0, 3, 7, 4),
        (1, 5, 6, 2),
    )
    verts = []
    uvs = []
    faces = []
    for f in quads:
        base = len(verts)
        for i in f:
            verts.append(corners[i])
            # U along the length, V parked in the middle of one board (no seam).
            uvs.append((corners[i][2] * 0.15 + 0.5, 0.08))
        faces.append((base, base + 1, base + 2, base + 3))
    g.quad(verts, faces, uvs, "Lib_WoodDark")


def _braces(g):
    """Flat steel straps above the water, with a bolt plate at each pile."""
    for x in (-PILE_XS[0], PILE_XS[0]):
        for z0, z1 in ((-2.40, 0.0), (0.0, 2.40)):
            _x_strap(g, (x, z0), (x, z1))
    edge = PILE_XS[2]
    for z in (-2.40, 2.40):
        for x0, x1 in ((-edge, 0.0), (0.0, edge)):
            _x_strap(g, (x0, z), (x1, z))


def _flat_bar(g, a, b, width, thick, normal, mat):
    """Closed flat bar. `normal` is the thickness axis."""
    d = (b[0] - a[0], b[1] - a[1], b[2] - a[2])
    length = math.sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2])
    if length < 0.04:
        return
    u = (d[0] / length, d[1] / length, d[2] / length)
    sx = u[1] * normal[2] - u[2] * normal[1]
    sy = u[2] * normal[0] - u[0] * normal[2]
    sz = u[0] * normal[1] - u[1] * normal[0]
    sl = math.sqrt(sx * sx + sy * sy + sz * sz)
    if sl < 1e-6:
        return
    sx, sy, sz = sx / sl, sy / sl, sz / sl
    hw, ht = width * 0.5, thick * 0.5
    nx, ny, nz = normal

    def corner(p, wu, tu):
        return (
            p[0] + sx * hw * wu + nx * ht * tu,
            p[1] + sy * hw * wu + ny * ht * tu,
            p[2] + sz * hw * wu + nz * ht * tu,
        )

    ring = ((-1.0, -1.0), (1.0, -1.0), (1.0, 1.0), (-1.0, 1.0))
    verts = [corner(a, wu, tu) for wu, tu in ring] + [corner(b, wu, tu) for wu, tu in ring]
    g.mesh(verts, (
        (0, 3, 2, 1), (4, 5, 6, 7),
        (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7),
    ), mat)


def _x_strap(g, p0, p1):
    dx, dz = p1[0] - p0[0], p1[1] - p0[1]
    length = math.hypot(dx, dz)
    ux, uz = dx / length, dz / length
    # Thickness faces out of the pile row so the wide face reads from the side.
    normal = (1.0, 0.0, 0.0) if abs(dz) >= abs(dx) else (0.0, 0.0, 1.0)
    inset = PILE_R + 0.022
    for sign, y0, y1 in ((1.0, 0.08, 0.28), (-1.0, 0.28, 0.08)):
        # Offset each strap out of the other so the X does not share a volume.
        ox, oz = -uz * 0.016 * sign, ux * 0.016 * sign
        a = (p0[0] + ux * inset + ox, y0, p0[1] + uz * inset + oz)
        b = (p1[0] - ux * inset + ox, y1, p1[1] - uz * inset + oz)
        _flat_bar(g, a, b, 0.042, 0.010, normal, "Lib_SteelDark")
        _bolt(g, p0, (ux, uz), y0, sign)
        _bolt(g, p1, (-ux, -uz), y1, sign)


def _bolt(g, pile, direction, y, sign):
    ux, uz = direction
    lx, lz = -uz * 0.016 * sign, ux * 0.016 * sign
    dist = PILE_R + 0.012
    cx = pile[0] + ux * dist + lx
    cz = pile[1] + uz * dist + lz
    if abs(ux) >= abs(uz):
        size = (0.008, 0.046, 0.046)
        axis = "X"
    else:
        size = (0.046, 0.046, 0.008)
        axis = "Z"
    g.box((cx, y, cz), size, "Lib_SteelDark")
    hx = cx + ux * 0.012
    hz = cz + uz * 0.012
    g.cylinder((hx, y, hz), 0.009, 0.008, "Lib_Steel", 6, axis=axis)


def _plank_lift(i):
    """Boards stay flat. A few sit 0.5–1.5 mm lower. None crown up at the ends."""
    return -((i * 17 + 5) % 4) * 0.0005


def _planks(g, lod):
    count = lod_pick(lod, PLANKS, PLANKS // 2)
    pitch = LENGTH / count
    board = pitch - PLANK_GAP
    for i in range(count):
        z = -LENGTH * 0.5 + pitch * (i + 0.5)
        y = PLANK_TOP - PLANK_T * 0.5 + _plank_lift(i)
        g.box((0, y, z), (2.64, PLANK_T, board), "Lib_WoodWeather", uv_scale=1.2, grain=1.0)
        if lod == 0 and i % 4 == 0:
            for x in (-1.05, 1.05):
                g.cylinder((x, PLANK_TOP + 0.008, z), 0.010, 0.008, "Lib_SteelDark", 6)


def _bollard(g, x, z, seg):
    g.cylinder((x, 0.640, z), 0.10, 0.028, "Lib_SteelDark", seg)
    g.cylinder((x, 0.81, z), 0.06, 0.30, "Lib_SteelDark", seg)
    g.cylinder((x, 0.99, z), 0.074, 0.036, "Lib_Steel", seg)


def _ladder(g):
    """Round rails into the water, bolted to the -X stringer, then over the deck."""
    rail_r = 0.020
    x_rail = -1.50
    for dz in (-0.17, 0.17):
        g.tube([
            (x_rail, -1.18, dz),
            (x_rail, 0.55, dz),
            (-1.36, 0.74, dz),
            (-1.16, 0.84, dz),
            (-1.00, 0.88, dz),
        ], rail_r, "Lib_SteelDark", 8)
    rung = -0.95
    while rung < 0.48:
        g.pipe(
            (x_rail, rung, -0.17 + rail_r + 0.008),
            (x_rail, rung, 0.17 - rail_r - 0.008),
            0.011, "Lib_SteelDark", 6,
        )
        rung += 0.26
    # Stringer outer face is x=-1.185. A strap and a bolt plate tie each rail to it.
    for dz in (-0.17, 0.17):
        _flat_bar(g, (-1.470, 0.48, dz), (-1.206, 0.48, dz), 0.040, 0.010, (0.0, 1.0, 0.0), "Lib_SteelDark")
        g.box((-1.196, 0.48, dz), (0.008, 0.050, 0.050), "Lib_SteelDark")
        g.cylinder((-1.208, 0.48, dz), 0.009, 0.008, "Lib_Steel", 6, axis="X")
        rung += 0.28


def _cleat(g, x, z):
    y = 0.634
    g.box((x, y, z), (0.22, 0.024, 0.09), "Lib_SteelDark")
    g.box((x, y + 0.04, z), (0.07, 0.05, 0.045), "Lib_Steel")
    g.box((x, y + 0.072, z), (0.24, 0.026, 0.05), "Lib_Steel")


def _rope(g):
    """A coil on the planks, and a line that leaves a cleat and lies on the deck."""
    g.torus((0.72, 0.650, 0.95), 0.12, 0.020, "Lib_Rust", 12, 6)
    g.torus((0.72, 0.694, 0.95), 0.105, 0.018, "Lib_Rust", 12, 6)
    y = PLANK_TOP + 0.020
    g.pipe((-1.15, 0.74, -1.40), (-0.55, y, -0.70), 0.014, "Lib_Rust", 6)
    g.pipe((-0.55, y, -0.70), (0.10, y, 0.05), 0.014, "Lib_Rust", 6)
    g.pipe((0.10, y, 0.05), (0.55, y, 0.62), 0.014, "Lib_Rust", 6)
    g.pipe((0.55, y, 0.62), (0.70, 0.70, 0.90), 0.014, "Lib_Rust", 6)
