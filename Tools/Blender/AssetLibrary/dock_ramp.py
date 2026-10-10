"""Dock ramp. 4 m long, 2 m wide. High end (-Z) is 0.62 m, low end (+Z) meets the ground.

Butt the high end against Dock_Straight. Center-to-center from a dock end is 2 m
plus half the dock, so a dock centered at the origin meets a ramp centered at z = 4.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

LENGTH = 4.0
HIGH = 0.62
LOW = 0.05
PITCH = math.degrees(math.atan2(HIGH - LOW, LENGTH))


@register
def create():
    a = Asset(
        "DockRamp",
        "Harbor",
        "Ramp 4.0 x 2.0 m. Deck falls from 0.62 m at -Z to 0.05 m at +Z. Matches Dock_Straight.",
    )
    a.loose_pivot = True
    a.climb_note = "Walk the planks. Not a cling wall. Pivot is the center of the ramp, not the pile centroid."
    a.vault_note = "The high end is 0.62 m. Under the vault band."
    count = 14
    for lod in (0, 1):
        g = a.begin(lod)
        n = lod_pick(lod, count, 8)
        bev = 0.003 if lod == 0 else 0
        for i in range(n):
            t = (i + 0.5) / n
            z = -LENGTH * 0.5 + t * LENGTH
            y = HIGH + (LOW - HIGH) * t
            pitch = LENGTH / n
            g.box((0, y, z), (1.88, 0.03, pitch * 0.86), "Lib_WoodWeather", bevel=bev, segs=1 if lod == 0 else 0, euler=(PITCH, 0, 0), uv_scale=1.2)
        g.box(( -0.72, 0.34, -0.4), (0.10, 0.16, 2.6), "Lib_WoodDark", euler=(PITCH * 0.5, 0, 0))
        g.box((0.72, 0.34, -0.4), (0.10, 0.16, 2.6), "Lib_WoodDark", euler=(PITCH * 0.5, 0, 0))
        for x in (-0.72, 0.72):
            g.cylinder((x, 0.28, -1.55), 0.11, 0.56, "Lib_WoodDark", lod_pick(lod, 8, 6))
        a.end()
    for i in range(count):
        t = (i + 0.5) / count
        z = -LENGTH * 0.5 + t * LENGTH
        y = HIGH + (LOW - HIGH) * t
        pitch = LENGTH / count
        a.box("Col_Plank_%d" % i, (0, y, z), (1.70, 0.022, pitch * 0.72), euler=(PITCH, 0, 0))
    a.capsule("Col_PileL", (-0.72, 0.28, -1.55), 0.10, 0.52, 1)
    a.capsule("Col_PileR", (0.72, 0.28, -1.55), 0.10, 0.52, 1)
    return a
