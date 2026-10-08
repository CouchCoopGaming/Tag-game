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
        "Dock module 6.0 m long and 3.0 m wide. Deck top is 0.62 m. Cleats on both sides, a rope coil, and a line between the near cleats. Butt the next module on the Z ends (center to center 6.0 m).",
    )
    a.climb_note = "Deck is a walk surface. Pilings are round, not cling panels."
    a.vault_note = "No rail on this module. Deck height is 0.62 m, under a vault."
    piles = ((-1.15, -2.40), (1.15, -2.40), (-1.15, 2.40), (1.15, 2.40))
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        seg = lod_pick(lod, 10, 6)
        for x, z in piles:
            g.cylinder((x, 0.28, z), 0.11, 0.56, "Lib_WoodDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
            if lod == 0:
                g.cylinder((x, 0.50, z), 0.118, 0.03, "Lib_SteelDark", seg)
        for x in (-0.95, 0.95):
            g.box((x, 0.42, 0), (0.10, 0.16, 5.70), "Lib_WoodDark", uv_scale=1.0)
        joists = lod_pick(lod, 7, 4)
        for i in range(joists):
            z = -2.4 + i * (4.8 / (joists - 1))
            g.box((0, 0.535, z), (2.55, 0.07, 0.08), "Lib_Wood", uv_scale=1.0)
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
            _rope(g)
        a.end()
    for i, (x, z) in enumerate(piles):
        a.capsule("Col_Pile_%d" % i, (x, 0.28, z), 0.11, 0.56, 1)
    for i, x in enumerate((-0.95, 0.95)):
        a.box("Col_Stringer_%d" % i, (x, 0.42, 0), (0.10, 0.16, 5.70))
    # Plank gaps are about 2 cm and are not a passage. One box per plank matches the board.
    pitch = LENGTH / PLANKS
    for i in range(PLANKS):
        z = -LENGTH * 0.5 + pitch * (i + 0.5)
        a.box("Col_Plank_%d" % i, (0, 0.602, z), (2.88, 0.032, pitch * 0.88))
    # Cleat base sits a few millimetres above the planks so the two skins do not share a face.
    for i, (x, z) in enumerate(((-1.20, -1.55), (-1.20, 1.55), (1.20, -1.55), (1.20, 1.55))):
        a.box("Col_Cleat_%d" % i, (x, 0.70, z), (0.20, 0.06, 0.07))
    return a


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
