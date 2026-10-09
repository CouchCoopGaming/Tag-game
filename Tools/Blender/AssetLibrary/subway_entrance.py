"""Subway stair entrance. The stair run extends below the pivot. Street face is +Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from _nudge import shift_z


WALL_X = 1.42
WALL_T = 0.18
WALL_Z = 0.35
WALL_L = 6.40
WALL_BOT = -1.55
WALL_TOP = 1.08


@register
def create():
    a = Asset(
        "Subway_Entrance",
        "Buildings",
        "Street stair down to a platform run. Side walls are 1.08 m above the sidewalk and the "
        "stair run extends below the pivot. The street opening faces +Z. A canopy covers the head.",
    )
    a.allow_below = True
    a.climbable = True
    a.climb_note = "The concrete side walls are the cling faces. The steps are the way down."
    a.vault_note = "The parapet is about 1.08 m above the sidewalk."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        if lod == 2:
            # Walls, four treads, and the canopy roof. 96 tris, under 0.6× the 180-triangle LOD1.
            _walls(g)
            for i in range(4):
                src = int(round(i * 7 / 3.0))
                y_top = -0.04 - src * 0.17
                z_front = 1.85 - src * 0.36
                g.box((0.0, y_top - 0.075, z_front - 0.16), (2.48, 0.14, 0.30), "Lib_Concrete", uv_scale=0.6)
            g.box((0.0, 2.42, 2.35), (2.90, 0.10, 1.50), "Lib_Steel", uv_scale=0.4)
            a.end()
            continue
        _walls(g)
        _steps(g, lod)
        _canopy(g, lod)
        a.end()
    a.box("Climb_Left", (-WALL_X, (WALL_BOT + WALL_TOP) * 0.5, WALL_Z), (WALL_T - 0.04, (WALL_TOP - WALL_BOT) - 0.08, WALL_L - 0.10))
    a.box("Climb_Right", (WALL_X, (WALL_BOT + WALL_TOP) * 0.5, WALL_Z), (WALL_T - 0.04, (WALL_TOP - WALL_BOT) - 0.08, WALL_L - 0.10))
    _step_colliders(a)
    a.box("Col_Pad", (0.0, 0.04, 2.55), (2.40, 0.06, 1.05))
    a.box("Col_Roof", (0.0, 2.42, 2.35), (2.70, 0.08, 1.35))
    shift_z(a, -0.35)
    return a


def _walls(g):
    h = WALL_TOP - WALL_BOT
    y = (WALL_TOP + WALL_BOT) * 0.5
    g.box((-WALL_X, y, WALL_Z), (WALL_T, h, WALL_L), "Lib_Concrete", uv_scale=0.55)
    g.box((WALL_X, y, WALL_Z), (WALL_T, h, WALL_L), "Lib_Concrete", uv_scale=0.55)
    # Floor of the run, clear of the lowest step.
    g.box((0.0, -1.48, 0.15), (2.48, 0.10, 4.60), "Lib_Concrete", uv_scale=0.5)


def _steps(g, lod):
    # Eight risers. Each tread is a closed box with air between it and the next.
    n = lod_pick(lod, 8, 8, 4)
    span = 8
    for i in range(n):
        src = int(round(i * (span - 1) / max(1, n - 1))) if n > 1 else 0
        y_top = -0.04 - src * 0.17
        z_front = 1.85 - src * 0.36
        g.box((0.0, y_top - 0.075, z_front - 0.16), (2.48, 0.14, 0.30), "Lib_Concrete", uv_scale=0.6)
        if lod == 0:
            g.box((0.0, y_top + 0.008, z_front - 0.02), (2.44, 0.012, 0.04), "Lib_SteelDark")
    g.box((0.0, 0.045, 2.55), (2.52, 0.09, 1.15), "Lib_Concrete", uv_scale=0.55)


def _step_colliders(a):
    for i in range(8):
        y_top = -0.04 - i * 0.17
        z_front = 1.85 - i * 0.36
        a.box(
            "Col_Step_%d" % i,
            (0.0, y_top - 0.08, z_front - 0.16),
            (2.30, 0.08, 0.22),
        )


def _canopy(g, lod):
    for sign in (-1, 1):
        g.box((sign * 1.15, 1.72, 2.55), (0.16, 1.20, 0.16), "Lib_SteelDark")
    g.box((0.0, 2.42, 2.35), (2.90, 0.10, 1.50), "Lib_Steel", uv_scale=0.4)
    if lod == 0:
        g.box((0.0, 2.28, 2.95), (1.60, 0.08, 0.06), "Lib_PaintYellow")
        g.box((0.0, 1.15, 2.55), (0.06, 0.90, 1.10), "Lib_Steel", uv_scale=0.4)
