"""Wooden dock straight module. 4.0 m along Z, 2.0 m across, deck at 0.62 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Dock_Straight",
        "Harbor",
        "Dock module 4.0 m long and 2.0 m wide. Deck top is 0.62 m. Butt the next module on the Z ends (center to center 4.0 m).",
    )
    a.climb_note = "Deck is a walk surface. Pilings are round, not cling panels."
    a.vault_note = "No rail on this module. Deck height is 0.62 m, under a vault."
    length, width = 4.0, 2.0
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        seg = lod_pick(lod, 10, 6)
        for x in (-0.72, 0.72):
            for z in (-1.55, 1.55):
                g.cylinder((x, 0.28, z), 0.11, 0.56, "Lib_WoodDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
                if lod == 0:
                    g.cylinder((x, 0.50, z), 0.118, 0.03, "Lib_SteelDark", seg)
        for x in (-0.55, 0.55):
            g.box((x, 0.42, 0), (0.10, 0.16, 3.72), "Lib_WoodDark", uv_scale=1.0)
        joists = lod_pick(lod, 5, 3)
        for i in range(joists):
            z = -1.6 + i * (3.2 / (joists - 1))
            g.box((0, 0.535, z), (1.72, 0.07, 0.08), "Lib_Wood", uv_scale=1.0)
        _planks(g, length, width, lod, bev)
        g.box((0, 0.50, -1.96), (1.84, 0.22, 0.06), "Lib_WoodDark", uv_scale=1.0)
        g.box((0, 0.50, 1.96), (1.84, 0.22, 0.06), "Lib_WoodDark", uv_scale=1.0)
        for x in (-0.98, 0.98):
            g.box((x, 0.50, 0), (0.05, 0.20, 3.84), "Lib_WoodDark", uv_scale=1.0)
        if lod == 0:
            g.box((0.82, 0.66, 0.4), (0.16, 0.04, 0.28), "Lib_SteelDark", bevel=0.002, segs=1)
        a.end()
    for i, (x, z) in enumerate(((-0.72, -1.55), (0.72, -1.55), (-0.72, 1.55), (0.72, 1.55))):
        a.capsule("Col_Pile_%d" % i, (x, 0.28, z), 0.11, 0.56, 1)
    for i, x in enumerate((-0.55, 0.55)):
        a.box("Col_Stringer_%d" % i, (x, 0.42, 0), (0.10, 0.16, 3.72))
    # Plank gaps are 1.2 cm and are not a passage. One box per plank matches the board.
    count = 26
    pitch = length / count
    for i in range(count):
        z = -length * 0.5 + pitch * (i + 0.5)
        a.box("Col_Plank_%d" % i, (0, 0.602, z), (1.92, 0.032, pitch * 0.88))
    return a


def _planks(g, length, width, lod, bev):
    count = lod_pick(lod, 26, 13)
    pitch = length / count
    board = pitch * 0.88
    for i in range(count):
        z = -length * 0.5 + pitch * (i + 0.5)
        g.box((0, 0.602, z), (width - 0.08, 0.032, board), "Lib_WoodWeather", bevel=bev, segs=1 if lod == 0 else 0, uv_scale=1.2)
        if lod == 0 and i % 2 == 0:
            for x in (-0.72, 0.72):
                g.cylinder((x, 0.624, z), 0.012, 0.01, "Lib_SteelDark", 6)
