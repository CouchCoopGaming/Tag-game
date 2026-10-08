"""Wooden dock straight module. 6.0 m along Z, 3.0 m across, deck at 0.62 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

LENGTH = 6.0
WIDTH = 3.0
PLANKS = 36


@register
def create():
    a = Asset(
        "Dock_Straight",
        "Harbor",
        "Dock module 6.0 m long and 3.0 m wide. Deck top is 0.62 m. Pilings run from about 1.15 m below the pivot up to the joists. Cleats on both sides, two bollards, a ladder on -X, a rope coil, and a line between the near cleats. Butt the next module on the Z ends (center to center 6.0 m).",
    )
    a.allow_below = True
    a.loose_pivot = True
    a.climb_note = "Deck is a walk surface. Pilings are round, not cling panels."
    a.vault_note = "No rail on this module. Deck height is 0.62 m, under a vault."
    piles = ((-1.15, -2.40), (1.15, -2.40), (-1.15, 2.40), (1.15, 2.40))
    pile_bot, pile_top = -1.15, 0.56
    pile_h = pile_top - pile_bot
    pile_y = (pile_top + pile_bot) * 0.5
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        seg = lod_pick(lod, 10, 6)
        for x, z in piles:
            g.cylinder((x, pile_y, z), 0.11, pile_h, "Lib_WoodDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
            if lod == 0:
                g.cylinder((x, 0.50, z), 0.118, 0.03, "Lib_SteelDark", seg)
        for x in (-0.95, 0.95):
            g.box((x, 0.42, 0), (0.10, 0.16, 5.70), "Lib_WoodDark", uv_scale=1.0)
        joists = lod_pick(lod, 7, 4)
        for i in range(joists):
            z = -2.4 + i * (4.8 / (joists - 1))
            # Joist top bites the plank soffit so the boards are not floating.
            g.box((0, 0.555, z), (2.55, 0.09, 0.08), "Lib_Wood", uv_scale=1.0)
        _planks(g, lod, bev)
        g.box((0, 0.50, -2.94), (2.84, 0.22, 0.06), "Lib_WoodDark", uv_scale=1.0)
        g.box((0, 0.50, 2.94), (2.84, 0.22, 0.06), "Lib_WoodDark", uv_scale=1.0)
        for x in (-1.46, 1.46):
            g.box((x, 0.50, 0), (0.05, 0.20, 5.88), "Lib_WoodDark", uv_scale=1.0)
        if lod == 0:
            _cleat(g, -1.20, -1.55)
            _cleat(g, -1.20, 1.55)
            _cleat(g, 1.20, -1.55)
            _cleat(g, 1.20, 1.55)
            for x, z in ((-1.35, -2.15), (1.35, 2.15)):
                _bollard(g, x, z, seg)
            _ladder(g)
            _rope(g)
        a.end()
    for i, (x, z) in enumerate(piles):
        a.capsule("Col_Pile_%d" % i, (x, -0.30, z), 0.09, 1.48, 1)
    for i, x in enumerate((-0.95, 0.95)):
        a.box("Col_Stringer_%d" % i, (x, 0.42, 0), (0.10, 0.16, 5.70))
    # Plank gaps are about 2 cm and are not a passage. One box per plank matches the board.
    pitch = LENGTH / PLANKS
    for i in range(PLANKS):
        z = -LENGTH * 0.5 + pitch * (i + 0.5)
        a.box("Col_Plank_%d" % i, (0, 0.602, z), (2.88, 0.032, pitch * 0.88))
    # Cleat base sits a few millimetres above the planks so the two skins do not share a face.
    for i, (x, z) in enumerate(((-1.20, -1.55), (-1.20, 1.55), (1.20, -1.55), (1.20, 1.55))):
        a.box("Col_Cleat_%d" % i, (x, 0.706, z), (0.18, 0.020, 0.036))
    for i, (x, z) in enumerate(((-1.35, -2.15), (1.35, 2.15))):
        a.capsule("Col_Bollard_%d" % i, (x, 0.82, z), 0.05, 0.26, 1)
    for i, dz in enumerate((-0.145, 0.145)):
        a.capsule("Col_Ladder_%d" % i, (-1.62, 0.14, dz), 0.012, 1.05, 1)
    return a


def _bollard(g, x, z, seg):
    """Short steel bollard on the deck, clear of the cleats."""
    g.cylinder((x, 0.640, z), 0.10, 0.028, "Lib_SteelDark", seg)
    g.cylinder((x, 0.81, z), 0.06, 0.30, "Lib_SteelDark", seg)
    g.cylinder((x, 0.99, z), 0.074, 0.036, "Lib_Steel", seg)


def _ladder(g):
    """Side ladder on -X, from below the waterline up over the deck edge."""
    x = -1.62
    z = 0.0
    y0, y1 = -0.45, 0.74
    for dz in (-0.145, 0.145):
        g.cylinder((x, (y0 + y1) * 0.5, z + dz), 0.015, y1 - y0, "Lib_Steel", 6)
    for y in (-0.28, -0.02, 0.24, 0.50):
        g.cylinder((x, y, z), 0.011, 0.248, "Lib_Steel", 6, axis="Z")
    for dz in (-0.145, 0.145):
        g.box((-1.51, 0.756, z + dz), (0.22, 0.016, 0.026), "Lib_Steel")


def _planks(g, lod, bev):
    count = lod_pick(lod, PLANKS, PLANKS // 2)
    pitch = LENGTH / count
    board = pitch * 0.88
    for i in range(count):
        z = -LENGTH * 0.5 + pitch * (i + 0.5)
        g.box((0, 0.602, z), (WIDTH - 0.08, 0.032, board), "Lib_WoodWeather", bevel=bev, segs=1 if lod == 0 else 0, uv_scale=1.2)
        if lod == 0 and i % 3 == 0:
            for x in (-1.15, 1.15):
                g.cylinder((x, 0.624, z), 0.012, 0.01, "Lib_SteelDark", 6)


def _cleat(g, x, z):
    y = 0.634
    g.box((x, y, z), (0.22, 0.024, 0.09), "Lib_SteelDark")
    g.box((x, y + 0.04, z), (0.07, 0.05, 0.045), "Lib_Steel")
    g.box((x, y + 0.072, z), (0.24, 0.026, 0.05), "Lib_Steel")


def _rope(g):
    g.torus((0.55, 0.70, 0.15), 0.16, 0.028, "Lib_Rust", 12, 6)
    g.torus((0.55, 0.74, 0.15), 0.145, 0.026, "Lib_Rust", 12, 6)
    # A line between the two -X cleats, sagging in the middle.
    g.pipe((-1.20, 0.72, -1.40), (-1.05, 0.66, -0.4), 0.012, "Lib_Rust", 6)
    g.pipe((-1.05, 0.66, -0.4), (-1.05, 0.66, 0.4), 0.012, "Lib_Rust", 6)
    g.pipe((-1.05, 0.66, 0.4), (-1.20, 0.72, 1.40), 0.012, "Lib_Rust", 6)
