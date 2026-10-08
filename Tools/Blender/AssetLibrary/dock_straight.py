"""Wooden dock straight module. 6.0 m along Z, 3.0 m across, deck at 0.62 m.

Piles are 30 cm across on a 2.4 m grid, with stringers, joists, and X-bracing
under the deck. Planks are separate boards. The ladder runs into the water.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

LENGTH = 6.0
WIDTH = 3.0
PLANKS = 30
PLANK_GAP = 0.028
PILE_R = 0.15
PILE_XS = (-1.15, 0.0, 1.15)
PILE_ZS = (-2.40, 0.0, 2.40)
PILE_BOT = -1.20
PILE_TOP = 0.268
PLANK_TOP = 0.618
PLANK_T = 0.032


@register
def create():
    a = Asset(
        "Dock_Straight",
        "Harbor",
        "Dock module 6.0 m long and 3.0 m wide. Deck top is 0.62 m. "
        "30 cm piles on a 2.4 m grid along both edges and mid-span, with stringers, joists, and cross-bracing. "
        "Planks are separate boards. A ladder on -X runs into the water. Cleats, two bollards, and a rope that lies on the deck. "
        "Butt the next module on the Z ends (center to center 6.0 m).",
    )
    a.allow_below = True
    a.loose_pivot = True
    a.climb_note = "Deck is a walk surface. Pilings are round, not cling panels."
    a.vault_note = "No rail on this module. Deck height is 0.62 m, under a vault."
    pile_h = PILE_TOP - PILE_BOT
    pile_y = (PILE_TOP + PILE_BOT) * 0.5
    piles = [(x, z) for x in PILE_XS for z in PILE_ZS]
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        seg = lod_pick(lod, 8, 6)
        for x, z in piles:
            g.cylinder((x, pile_y, z), PILE_R, pile_h, "Lib_WoodDark", seg)
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
        a.capsule("Col_Pile_%d" % i, (x, -0.48, z), 0.12, 1.28, 1)
    for i, x in enumerate(PILE_XS):
        a.box("Col_Stringer_%d" % i, (x, 0.374, 0.0), (0.12, 0.18, 5.50))
    pitch = LENGTH / PLANKS
    board = pitch - PLANK_GAP
    for i in range(PLANKS):
        z = -LENGTH * 0.5 + pitch * (i + 0.5)
        a.box("Col_Plank_%d" % i, (0, PLANK_TOP - PLANK_T * 0.5, z), (2.70, PLANK_T * 0.8, board * 0.90))
    for i, (x, z) in enumerate(((-1.20, -1.55), (-1.20, 1.55), (1.20, -1.55), (1.20, 1.55))):
        a.box("Col_Cleat_%d" % i, (x, 0.706, z), (0.18, 0.020, 0.036))
    for i, (x, z) in enumerate(((-0.55, -1.85), (0.85, 1.70))):
        a.capsule("Col_Bollard_%d" % i, (x, 0.82, z), 0.05, 0.26, 1)
    for i, dz in enumerate((-0.16, 0.16)):
        a.capsule("Col_Ladder_%d" % i, (-1.74, -0.10, dz), 0.012, 1.70, 1)
    return a


def _frame(g, lod):
    """Stringers on the piles, joists across them, rim below the plank ends."""
    for x in PILE_XS:
        g.box((x, 0.374, 0.0), (0.14, 0.20, 5.55), "Lib_WoodDark", uv_scale=1.0)
    count = lod_pick(lod, 9, 5)
    for i in range(count):
        z = -2.40 + (4.80 * i / (count - 1))
        g.box((0.0, 0.530, z), (2.55, 0.10, 0.08), "Lib_Wood", uv_scale=1.0)
    g.box((0.0, 0.42, -2.94), (2.70, 0.20, 0.05), "Lib_WoodDark", uv_scale=1.0)
    g.box((0.0, 0.42, 2.94), (2.70, 0.20, 0.05), "Lib_WoodDark", uv_scale=1.0)
    for x in (-1.48, 1.48):
        g.box((x, 0.42, 0.0), (0.04, 0.20, 5.80), "Lib_WoodDark", uv_scale=1.0)


def _braces(g):
    """X-bracing clear of the piles, along both edges and the near end."""
    gap = PILE_R + 0.012
    for x in (-1.15, 1.15):
        for z0, z1 in ((-2.40, 0.0), (0.0, 2.40)):
            _diag(g, (x - 0.04, -0.72, z0 + gap), (x - 0.04, 0.08, z1 - gap))
            _diag(g, (x + 0.04, 0.08, z0 + gap), (x + 0.04, -0.72, z1 - gap))
    z = 2.40
    for x0, x1 in ((-1.15, 0.0), (0.0, 1.15)):
        _diag(g, (x0 + gap, -0.72, z - 0.04), (x1 - gap, 0.08, z - 0.04))
        _diag(g, (x0 + gap, 0.08, z + 0.04), (x1 - gap, -0.72, z + 0.04))


def _diag(g, a, b):
    g.pipe(a, b, 0.018, "Lib_WoodDark", 6)


def _planks(g, lod, bev):
    count = lod_pick(lod, PLANKS, PLANKS // 2)
    pitch = LENGTH / count
    board = pitch - PLANK_GAP
    y = PLANK_TOP - PLANK_T * 0.5
    for i in range(count):
        z = -LENGTH * 0.5 + pitch * (i + 0.5)
        g.box((0, y, z), (WIDTH - 0.16, PLANK_T, board), "Lib_WoodWeather", bevel=bev, segs=1 if lod == 0 else 0, uv_scale=1.2)
        if lod == 0 and i % 4 == 0:
            for x in (-1.05, 1.05):
                g.cylinder((x, PLANK_TOP + 0.008, z), 0.010, 0.008, "Lib_SteelDark", 6)


def _bollard(g, x, z, seg):
    g.cylinder((x, 0.640, z), 0.10, 0.028, "Lib_SteelDark", seg)
    g.cylinder((x, 0.81, z), 0.06, 0.30, "Lib_SteelDark", seg)
    g.cylinder((x, 0.99, z), 0.074, 0.036, "Lib_Steel", seg)


def _ladder(g):
    """Side ladder on -X. Rails and rungs continue well below the waterline."""
    x = -1.74
    y0, y1 = -1.05, 0.86
    for dz in (-0.16, 0.16):
        g.cylinder((x, (y0 + y1) * 0.5, dz), 0.016, y1 - y0, "Lib_Steel", 6)
    rung = -0.88
    while rung < 0.62:
        g.cylinder((x, rung, 0.0), 0.012, 0.276, "Lib_Steel", 6, axis="Z")
        rung += 0.28
    for dz in (-0.16, 0.16):
        g.box((-1.55, 0.80, dz), (0.30, 0.016, 0.026), "Lib_Steel")


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
