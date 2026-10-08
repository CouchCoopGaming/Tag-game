"""Chain-link fence panel. 2.0 m wide, 1.8 m tall. Posts included."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("ChainFence", "Buildings", "Chain-link panel 2.0 m wide, 1.8 m tall, posts 6 cm. Fabric is a welded diamond grid.")
    a.climbable = True
    a.climb_note = "The mesh is climbable in the loose sense, but the collider is the posts, rails, and a fabric slab the thickness of the wire."
    a.vault_note = "Top rail is 1.80 m. Too high to vault from flat ground."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        for x in (-0.97, 0.97):
            g.cylinder((x, 0.92, 0), 0.03, 1.84, "Lib_SteelDark", seg)
            g.sphere((x, 1.84, 0), 0.035, "Lib_Steel", 6)
        g.pipe((-0.97, 1.72, 0), (0.97, 1.72, 0), 0.018, "Lib_Steel", seg)
        g.pipe((-0.97, 0.12, 0), (0.97, 0.12, 0), 0.015, "Lib_SteelDark", seg)
        step = lod_pick(lod, 0.22, 0.40)
        _diamonds(g, step, lod)
        a.end()
    a.capsule("Col_PostL", (-0.97, 0.92, 0), 0.03, 1.84, 1)
    a.capsule("Col_PostR", (0.97, 0.92, 0), 0.03, 1.84, 1)
    a.capsule("Col_TopRail", (0, 1.72, 0), 0.018, 1.94, 0)
    a.capsule("Col_BotRail", (0, 0.12, 0), 0.015, 1.94, 0)
    # Wire slab. Thickness equals the wire. It blocks the panel; it does not extend past the posts.
    a.box("Col_Fabric", (0, 0.95, 0), (1.86, 1.55, 0.012), approx=True)
    return a


def _diamonds(g, step, lod):
    x0, x1 = -0.88, 0.88
    y0, y1 = 0.22, 1.62
    tube = 0.006
    seg = 4
    x = x0
    while x < x1 - 0.01:
        y = y0
        while y < y1 - 0.01:
            g.pipe((x, y, 0), (min(x + step, x1), min(y + step, y1), 0), tube, "Lib_Chain", seg)
            g.pipe((x, min(y + step, y1), 0), (min(x + step, x1), y, 0), tube, "Lib_Chain", seg)
            y += step
        x += step
