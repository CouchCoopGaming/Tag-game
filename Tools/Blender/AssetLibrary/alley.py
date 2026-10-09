"""City service alley. Brick walls, a back door, asphalt floor. The opening faces +Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


# Inner clear width 2.40 m. Side walls sit outside that, back wall between them.
WALL_X = 1.35
WALL_T = 0.28
WALL_H = 3.36
SIDE_Z = 0.15
SIDE_L = 7.70
BACK_W = 2.32
BACK_T = 0.26
BACK_Z = -3.78


@register
def create():
    a = Asset(
        "Alley",
        "Buildings",
        "Narrow service alley, 2.4 m between the brick faces and about 7.7 m deep. "
        "The open end faces +Z. A back door sits in the far wall, with a downspout on the right face.",
    )
    a.climbable = True
    a.climb_note = "Both side walls and the back piers are cling faces. The floor is the ground."
    a.vault_note = "No rail. The coping is at 3.4 m."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        if lod == 2:
            # Four boxes. 48 tris is under 0.6× the 132-triangle LOD1.
            g.box((-WALL_X, WALL_H * 0.5, SIDE_Z), (WALL_T, WALL_H, SIDE_L), "Lib_Brick", uv_scale=0.8)
            g.box((WALL_X, WALL_H * 0.5, SIDE_Z), (WALL_T, WALL_H, SIDE_L), "Lib_Brick", uv_scale=0.8)
            g.box((0.0, WALL_H * 0.5, BACK_Z), (BACK_W, WALL_H, BACK_T), "Lib_Brick", uv_scale=0.8)
            g.box((0.0, 0.025, 0.24), (2.24, 0.05, 7.04), "Lib_Asphalt", uv_scale=0.45)
            a.end()
            continue
        bev = lod_pick(lod, 0.004, 0.0, 0.0)
        _shell(g, bev)
        if lod < 2:
            _door(g)
            if lod == 0:
                _window(g)
                _pipe(g)
        a.end()
    # Inset so the 8 mm collider step stays inside the brick.
    a.box("Climb_Left", (-WALL_X, WALL_H * 0.5, SIDE_Z), (WALL_T - 0.06, WALL_H - 0.08, SIDE_L - 0.12))
    a.box("Climb_Right", (WALL_X, WALL_H * 0.5, SIDE_Z), (WALL_T - 0.06, WALL_H - 0.08, SIDE_L - 0.12))
    a.box("Climb_BackL", (-0.86, WALL_H * 0.5, BACK_Z), (0.56, WALL_H - 0.08, BACK_T - 0.06))
    a.box("Climb_BackR", (0.86, WALL_H * 0.5, BACK_Z), (0.56, WALL_H - 0.08, BACK_T - 0.06))
    a.box("Col_Header", (0.0, 2.74, BACK_Z), (0.88, 1.16, BACK_T - 0.06))
    a.box("Col_Door", (0.0, 1.02, -3.60), (0.76, 1.92, 0.03))
    a.box("Col_Floor", (0.0, 0.02, 0.24), (2.08, 0.03, 6.80))
    return a


def _shell(g, bev):
    g.box((-WALL_X, WALL_H * 0.5, SIDE_Z), (WALL_T, WALL_H, SIDE_L), "Lib_Brick", bevel=bev, uv_scale=0.8)
    g.box((WALL_X, WALL_H * 0.5, SIDE_Z), (WALL_T, WALL_H, SIDE_L), "Lib_Brick", bevel=bev, uv_scale=0.8)
    # Piers and header leave the door opening. 4 mm of air at the joints.
    g.box((-0.86, WALL_H * 0.5, BACK_Z), (0.60, WALL_H, BACK_T), "Lib_Brick", uv_scale=0.8)
    g.box((0.86, WALL_H * 0.5, BACK_Z), (0.60, WALL_H, BACK_T), "Lib_Brick", uv_scale=0.8)
    g.box((0.0, 2.74, BACK_Z), (1.11, 1.20, BACK_T), "Lib_Brick", uv_scale=0.8)
    cap_y = WALL_H + 0.006 + 0.035
    g.box((-WALL_X, cap_y, SIDE_Z), (WALL_T + 0.04, 0.07, SIDE_L + 0.04), "Lib_Concrete", uv_scale=0.6)
    g.box((WALL_X, cap_y, SIDE_Z), (WALL_T + 0.04, 0.07, SIDE_L + 0.04), "Lib_Concrete", uv_scale=0.6)
    g.box((0.0, cap_y, BACK_Z), (BACK_W, 0.07, BACK_T + 0.04), "Lib_Concrete", uv_scale=0.6)
    g.box((0.0, 0.025, 0.24), (2.24, 0.05, 7.04), "Lib_Asphalt", uv_scale=0.45)


def _door(g):
    g.box((0.0, 1.02, -3.60), (0.82, 1.98, 0.04), "Lib_WoodDark", uv_scale=0.8)
    g.box((0.0, 0.08, -3.44), (1.10, 0.06, 0.20), "Lib_Concrete", uv_scale=0.7)


def _window(g):
    # Proud of the left inner face by 4 mm. Inner face is at x = -1.21.
    g.box((-1.16, 1.85, 1.15), (0.04, 0.72, 0.86), "Lib_Wood", uv_scale=0.6)
    g.box((-1.11, 1.85, 1.15), (0.02, 0.52, 0.62), "Lib_Glass", uv_scale=0.5)


def _pipe(g):
    g.cylinder((1.12, 1.70, -1.4), 0.035, 3.05, "Lib_SteelDark", 8)
    g.box((1.12, 3.30, -1.4), (0.10, 0.06, 0.10), "Lib_Steel", uv_scale=0.5)
