"""90 cm offset dish on a 1.20 m mast."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import polyline


# Apex sits on the mast cap. The bowl opens along a 22 deg up-tilt toward +Z.
_APEX = (0.0, 1.36, 0.08)
_TILT = 22.0
_RADIUS = 0.45
_DEPTH = 0.12
_THICK = 0.028


def _axis(dist):
    tilt = math.radians(_TILT)
    return (
        _APEX[0],
        _APEX[1] + math.sin(tilt) * dist,
        _APEX[2] + math.cos(tilt) * dist,
    )


def _dish(g, seg, rings, mat):
    """Closed paraboloid shell. Local +Z is the dish axis before the up-tilt."""
    tilt = math.radians(_TILT)
    ct, st = math.cos(tilt), math.sin(tilt)
    ox, oy, oz = _APEX

    def place(x, y, z):
        y2 = y * ct - z * st
        z2 = y * st + z * ct
        return (ox + x, oy + y2, oz + z2)

    verts = [place(0.0, 0.0, 0.0), place(0.0, 0.0, -_THICK)]
    for i in range(1, rings + 1):
        r = _RADIUS * i / float(rings)
        z = _DEPTH * (r / _RADIUS) ** 2
        for j in range(seg):
            ang = 2.0 * math.pi * j / seg
            verts.append(place(r * math.cos(ang), r * math.sin(ang), z))
        for j in range(seg):
            ang = 2.0 * math.pi * j / seg
            verts.append(place(r * math.cos(ang), r * math.sin(ang), z - _THICK))

    def vid(layer, i, j):
        # layer 0 outer, 1 inner. Ring i starts at 1.
        return 2 + (i - 1) * seg * 2 + layer * seg + (j % seg)

    faces = []
    for j in range(seg):
        faces.append((0, vid(0, 1, j), vid(0, 1, j + 1)))
        faces.append((1, vid(1, 1, j + 1), vid(1, 1, j)))
    for i in range(1, rings):
        for j in range(seg):
            faces.append((vid(0, i, j), vid(0, i + 1, j), vid(0, i + 1, j + 1), vid(0, i, j + 1)))
            faces.append((vid(1, i, j), vid(1, i, j + 1), vid(1, i + 1, j + 1), vid(1, i + 1, j)))
    for j in range(seg):
        faces.append((vid(0, rings, j), vid(1, rings, j), vid(1, rings, j + 1), vid(0, rings, j + 1)))
    g.mesh(verts, faces, mat)


@register
def create():
    a = Asset(
        "SatelliteDish",
        "StreetFurniture",
        "Offset paraboloid, 0.90 m across and 0.12 m deep, mast 1.20 m. The dish tilts 22 deg up toward +Z.",
    )
    a.climb_note = "3 cm mast. Not a cling."
    a.vault_note = "Dish is a thin shell."
    focus = _axis(_RADIUS * _RADIUS / (4.0 * _DEPTH))
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 18, 10)
        rings = lod_pick(lod, 8, 4)
        bev = lod_pick(lod, 0.003, 0.0)
        g.box((0, 0.02, 0), (0.28, 0.04, 0.28), "Lib_SteelDark", bevel=bev, segs=1)
        g.cylinder((0, 0.62, 0), 0.028, 1.16, "Lib_Steel", seg)
        g.cylinder((0, 1.18, 0), 0.05, 0.06, "Lib_SteelDark", seg)
        _dish(g, seg, rings, "Lib_PaintWhite")
        bracket = _axis(0.04)
        g.cylinder(bracket, 0.035, 0.08, "Lib_SteelDark", seg)
        if lod == 0:
            feed = _axis(0.22)
            polyline(g, [bracket, (0.0, focus[1] - 0.04, focus[2] - 0.06), focus], 0.008, "Lib_Steel", 5)
            g.box(focus, (0.07, 0.045, 0.09), "Lib_Black", bevel=bev, segs=1)
            g.cylinder(feed, 0.012, 0.08, "Lib_Steel", 6)
        a.end()
    a.box("Col_Base", (0, 0.02, 0), (0.24, 0.03, 0.24))
    a.capsule("Col_Mast", (0, 0.62, 0), 0.022, 1.08, 1)
    # Inside the thick apex, clear of the tilted skins.
    hub = _axis(0.012)
    a.box("Col_Dish", (hub[0], hub[1], hub[2]), (0.04, 0.028, 0.008))
    return a
