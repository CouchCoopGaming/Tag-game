"""Park restroom. Concrete block, two doors on +Z, flat roof."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from _nudge import shift_z


W = 5.20
D = 3.40
WALL_H = 2.52
WALL_Y0 = 0.10
T = 0.20


@register
def create():
    a = Asset(
        "Restroom",
        "Park",
        "Park restroom, 5.2 m by 3.4 m, wall top 2.52 m, flat roof. Two doors face +Z. "
        "A short screen stands in front of the doors.",
    )
    a.climbable = True
    a.climb_note = "The long walls are cling faces. Doors face +Z."
    a.vault_note = "No rail. The roof edge is at about 2.7 m."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        if lod == 2:
            # Four walls and the roof. 60 tris, under 0.6× the 144-triangle LOD1.
            _wall_box(g, -W * 0.5 + T * 0.5, 0.0, T, D)
            _wall_box(g, W * 0.5 - T * 0.5, 0.0, T, D)
            _wall_box(g, 0.0, -D * 0.5 + T * 0.5, W - 2.0 * T - 0.016, T)
            _wall_box(g, 0.0, D * 0.5 - T * 0.5, W - 2.0 * T - 0.016, T)
            g.box((0.0, WALL_H + 0.066, 0.0), (W + 0.24, 0.12, D + 0.24), "Lib_Concrete", uv_scale=0.4)
            a.end()
            continue
        _shell(g, lod)
        if lod < 2:
            _doors(g, lod)
            _screen(g)
        if lod == 0:
            _vent(g)
        a.end()
    wall_y = (WALL_Y0 + WALL_H) * 0.5
    wall_h = WALL_H - WALL_Y0
    a.box("Climb_Left", (-W * 0.5 + T * 0.5, wall_y, 0.0), (T - 0.04, wall_h - 0.08, D - 0.16))
    a.box("Climb_Right", (W * 0.5 - T * 0.5, wall_y, 0.0), (T - 0.04, wall_h - 0.08, D - 0.16))
    a.box("Col_Back", (0.0, wall_y, -D * 0.5 + T * 0.5), (W - 2.0 * T - 0.12, wall_h - 0.08, T - 0.04))
    a.box("Col_Roof", (0.0, 2.586, 0.0), (4.60, 0.06, 2.80))
    a.box("Col_Slab", (0.0, 0.04, 0.0), (W - 0.10, 0.06, D - 0.10))
    a.box("Col_DoorL", (-1.02, 1.02, D * 0.5 + 0.028), (0.64, 1.80, 0.02))
    a.box("Col_DoorR", (1.02, 1.02, D * 0.5 + 0.028), (0.64, 1.80, 0.02))
    shift_z(a, -0.645)
    return a


def _wall_box(g, center_x, center_z, sx, sz):
    h = WALL_H - WALL_Y0
    y = (WALL_H + WALL_Y0) * 0.5
    g.box((center_x, y, center_z), (sx, h, sz), "Lib_Concrete", uv_scale=0.55)


def _shell(g, lod):
    # Side walls run the full depth. Front and back sit between the inner faces.
    inner = W * 0.5 - T
    _wall_box(g, -W * 0.5 + T * 0.5, 0.0, T, D)
    _wall_box(g, W * 0.5 - T * 0.5, 0.0, T, D)
    _wall_box(g, 0.0, -D * 0.5 + T * 0.5, inner * 2.0 - 0.016, T)
    _front(g)
    roof_y = WALL_H + 0.006 + 0.06
    g.box((0.0, roof_y, 0.0), (W + 0.24, 0.12, D + 0.24), "Lib_Concrete", uv_scale=0.4)
    g.box((0.0, 0.045, 0.0), (W + 0.08, 0.09, D + 0.08), "Lib_Concrete", uv_scale=0.45)
    if lod == 0:
        g.box((0.0, roof_y + 0.07, 0.0), (W + 0.08, 0.02, 0.08), "Lib_SteelDark")


def _front(g):
    """Piers stop under a header so the door openings stay clear."""
    z = D * 0.5 - T * 0.5
    pier_top = 2.06
    pier_h = pier_top - WALL_Y0
    pier_y = (pier_top + WALL_Y0) * 0.5
    g.box((-1.95, pier_y, z), (0.86, pier_h, T), "Lib_Concrete", uv_scale=0.55)
    g.box((0.0, pier_y, z), (1.04, pier_h, T), "Lib_Concrete", uv_scale=0.55)
    g.box((1.95, pier_y, z), (0.86, pier_h, T), "Lib_Concrete", uv_scale=0.55)
    g.box((0.0, 2.30, z), (4.76, 0.40, T), "Lib_Concrete", uv_scale=0.55)


def _doors(g, lod):
    z = D * 0.5 + 0.028
    for x in (-1.02, 1.02):
        g.box((x, 1.02, z), (0.78, 1.96, 0.04), "Lib_PaintGreen", uv_scale=0.6)
        if lod == 0:
            g.box((x, 1.35, z + 0.028), (0.22, 0.08, 0.012), "Lib_Steel")


def _screen(g):
    # Privacy wing in front of the doors, clear of the door slabs.
    z = D * 0.5 + 0.78
    g.box((0.0, 1.20, z), (0.12, 2.08, 1.10), "Lib_Concrete", uv_scale=0.6)


def _vent(g):
    g.cylinder((1.4, 2.85, -0.4), 0.08, 0.28, "Lib_SteelDark", 8)
