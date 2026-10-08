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
        "The side is open: gapped deck planks, one 5 by 20 cm stringer, then cross-braces bolted pile to pile. "
        "Piles are 30 cm rounds from below the water (darker under the line) up to the stringer. "
        "A round ladder on -X curves over the deck as grab handles and runs into the water. "
        "Cleats, two bollards, and a rope that lies on the deck. "
        "Butt the next module on the Z ends (center to center 6.0 m).",
    )
    a.allow_below = True
    a.loose_pivot = True
    a.climb_note = "Deck is a walk surface. Pilings are round, not cling panels."
    a.vault_note = "No rail on this module. Deck height is 0.62 m, under a vault."
    piles = [(x, z) for x in PILE_XS for z in PILE_ZS]
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        seg = lod_pick(lod, 10, 6)
        for x, z in piles:
            _pile(g, x, z, seg if lod == 0 else 6)
        _frame(g, lod)
        _planks(g, lod, bev)
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
        a.box("Col_Plank_%d" % i, (0, PLANK_TOP - PLANK_T * 0.5, z), (2.40, PLANK_T * 0.7, board * 0.86))
    for i, (x, z) in enumerate(((-1.20, -1.55), (-1.20, 1.55), (1.20, -1.55), (1.20, 1.55))):
        a.box("Col_Cleat_%d" % i, (x, 0.706, z), (0.18, 0.020, 0.036))
    for i, (x, z) in enumerate(((-0.55, -1.85), (0.85, 1.70))):
        a.capsule("Col_Bollard_%d" % i, (x, 0.82, z), 0.05, 0.26, 1)
    for i, dz in enumerate((-0.17, 0.17)):
        a.capsule("Col_Ladder_%d" % i, (-1.52, -0.27, dz), 0.014, 1.50, 1)
        a.capsule("Col_Grab_%d" % i, (-1.09, 0.84, dz), 0.010, 0.12, 0)
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
    """X-braces above the water, bolted to the pile faces."""
    for x in (-PILE_XS[0], PILE_XS[0]):
        for z0, z1 in ((-2.40, 0.0), (0.0, 2.40)):
            _x_brace(g, (x, z0), (x, z1))
    edge = PILE_XS[2]
    for z in (-2.40, 2.40):
        for x0, x1 in ((-edge, 0.0), (0.0, edge)):
            _x_brace(g, (x0, z), (x1, z))


def _x_brace(g, p0, p1):
    dx, dz = p1[0] - p0[0], p1[1] - p0[1]
    length = math.hypot(dx, dz)
    ux, uz = dx / length, dz / length
    lx, lz = -uz, ux
    inset = PILE_R + 0.030
    for sign, y0, y1 in ((1.0, 0.07, 0.30), (-1.0, 0.30, 0.07)):
        ox, oz = lx * 0.055 * sign, lz * 0.055 * sign
        a = (p0[0] + ux * inset + ox, y0, p0[1] + uz * inset + oz)
        b = (p1[0] - ux * inset + ox, y1, p1[1] - uz * inset + oz)
        g.pipe(a, b, 0.016, "Lib_WoodDark", 6)
        _bolt(g, p0, p1, y0, sign)
        _bolt(g, p1, p0, y1, sign)


def _bolt(g, pile, other, y, sign):
    dx, dz = other[0] - pile[0], other[1] - pile[1]
    length = math.hypot(dx, dz)
    ux, uz = dx / length, dz / length
    lx, lz = -uz * 0.055 * sign, ux * 0.055 * sign
    cx = pile[0] + ux * (PILE_R + 0.014) + lx
    cz = pile[1] + uz * (PILE_R + 0.014) + lz
    if abs(ux) >= abs(uz):
        size = (0.012, 0.055, 0.055)
    else:
        size = (0.055, 0.055, 0.012)
    g.box((cx, y, cz), size, "Lib_SteelDark")


def _planks(g, lod, bev):
    count = lod_pick(lod, PLANKS, PLANKS // 2)
    pitch = LENGTH / count
    board = pitch - PLANK_GAP
    y = PLANK_TOP - PLANK_T * 0.5
    for i in range(count):
        z = -LENGTH * 0.5 + pitch * (i + 0.5)
        g.box((0, y, z), (2.64, PLANK_T, board), "Lib_WoodWeather", bevel=bev, segs=1 if lod == 0 else 0, uv_scale=1.2, grain=1.0)
        if lod == 0 and i % 4 == 0:
            for x in (-1.05, 1.05):
                g.cylinder((x, PLANK_TOP + 0.008, z), 0.010, 0.008, "Lib_SteelDark", 6)


def _bollard(g, x, z, seg):
    g.cylinder((x, 0.640, z), 0.10, 0.028, "Lib_SteelDark", seg)
    g.cylinder((x, 0.81, z), 0.06, 0.30, "Lib_SteelDark", seg)
    g.cylinder((x, 0.99, z), 0.074, 0.036, "Lib_Steel", seg)


def _ladder(g):
    """Dark round rails, 4 cm across, with round rungs. The top bends over the deck."""
    rail_r = 0.020
    for dz in (-0.17, 0.17):
        g.tube([
            (-1.52, -1.05, dz),
            (-1.52, 0.50, dz),
            (-1.38, 0.70, dz),
            (-1.18, 0.82, dz),
            (-1.00, 0.86, dz),
        ], rail_r, "Lib_SteelDark", 8)
    rung = -0.88
    while rung < 0.42:
        g.cylinder((-1.52, rung, 0.0), 0.011, 0.292, "Lib_SteelDark", 8, axis="Z")
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
