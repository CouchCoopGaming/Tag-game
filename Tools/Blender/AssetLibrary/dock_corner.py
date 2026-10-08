"""L-shaped dock corner. Each leg is 2.0 m wide and runs 4.0 m from the outer corner."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Dock_Corner",
        "Harbor",
        "L dock corner inside a 4 m square. Legs run toward +X and +Z from the inner corner. Deck top is 0.62 m, matching Dock_Straight.",
    )
    a.loose_pivot = True
    a.climb_note = "Walk the deck. The inner corner is open water, not a collider. Pivot is the center of the 4 m square, not the pile centroid."
    a.vault_note = "No rail."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        seg = lod_pick(lod, 8, 6)
        _leg(g, lod, bev, seg, along="Z")
        _leg(g, lod, bev, seg, along="X")
        # Corner pile under the inner joint, and three outer piles.
        for x, z in ((-1.0, -1.0), (1.55, -1.0), (-1.0, 1.55), (0.55, -1.0), (-1.0, 0.55)):
            g.cylinder((x, 0.28, z), 0.11, 0.56, "Lib_WoodDark", seg)
        a.end()
    for i, (x, z) in enumerate(((-1.0, -1.0), (1.55, -1.0), (-1.0, 1.55), (0.55, -1.0), (-1.0, 0.55))):
        a.capsule("Col_Pile_%d" % i, (x, 0.28, z), 0.11, 0.56, 1)
    # Planks already authored; colliders follow the same grid, stopping before the missing quadrant.
    _plank_colliders(a, "Z")
    _plank_colliders(a, "X")
    return a


def _leg(g, lod, bev, seg, along):
    count = lod_pick(lod, 14, 8)
    # Leg occupies [-2, 0] in the cross axis and [-2, 2] in the along axis, but the
    # shared corner square [-2, 0] x [-2, 0] is only built by the Z leg.
    if along == "Z":
        z0, z1 = -2.0, 2.0
        pitch = (z1 - z0) / count
        board = pitch * 0.88
        for i in range(count):
            z = z0 + pitch * (i + 0.5)
            g.box((-1.0, 0.602, z), (2.00, 0.032, board), "Lib_WoodWeather", bevel=bev, segs=1 if lod == 0 else 0, uv_scale=1.2)
        g.box((-1.0, 0.42, 0), (0.12, 0.16, 3.7), "Lib_WoodDark", uv_scale=1.0)
        g.box((-1.95, 0.50, 0), (0.05, 0.18, 3.9), "Lib_WoodDark")
        g.box((-0.05, 0.50, 1.0), (0.05, 0.18, 1.9), "Lib_WoodDark")
    else:
        x0, x1 = -0.02, 2.0
        pitch = (x1 - x0) / count
        board = pitch * 0.88
        for i in range(count):
            x = x0 + pitch * (i + 0.5)
            g.box((x, 0.602, -1.0), (board, 0.032, 1.92), "Lib_WoodWeather", bevel=bev, segs=1 if lod == 0 else 0, uv_scale=1.2)
        g.box((1.0, 0.42, -1.0), (1.9, 0.16, 0.12), "Lib_WoodDark", uv_scale=1.0)
        g.box((1.0, 0.50, -1.95), (1.9, 0.18, 0.05), "Lib_WoodDark")
        g.box((1.0, 0.50, -0.05), (1.9, 0.18, 0.05), "Lib_WoodDark")


def _plank_colliders(a, along):
    count = 14
    if along == "Z":
        z0, z1 = -2.0, 2.0
        pitch = (z1 - z0) / count
        for i in range(count):
            z = z0 + pitch * (i + 0.5)
            a.box("Col_PlankZ_%d" % i, (-1.0, 0.602, z), (1.96, 0.030, pitch * 0.84))
    else:
        x0, x1 = 0.0, 2.0
        pitch = (x1 - x0) / count
        for i in range(count):
            x = x0 + pitch * (i + 0.5)
            a.box("Col_PlankX_%d" % i, (x, 0.602, -1.0), (pitch * 0.84, 0.030, 1.88))
