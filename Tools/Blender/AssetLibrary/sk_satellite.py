"""0.80 m offset dish on a roof shoe. Bowl, feed, and bracket are one assembly."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import look_euler


# Apex on the knuckle. The bowl opens 36 deg up toward +Z.
_APEX = (0.0, 0.52, 0.04)
_TILT = 36.0
_RADIUS = 0.40
_DEPTH = 0.16
_THICK = 0.032


def _axis(dist):
    tilt = math.radians(_TILT)
    return (
        _APEX[0],
        _APEX[1] + math.sin(tilt) * dist,
        _APEX[2] + math.cos(tilt) * dist,
    )


def _place(x, y, z):
    """Local +Z is the dish axis, tilted up toward world +Z."""
    tilt = math.radians(_TILT)
    ct, st = math.cos(tilt), math.sin(tilt)
    y2 = y * ct + z * st
    z2 = -y * st + z * ct
    return (_APEX[0] + x, _APEX[1] + y2, _APEX[2] + z2)


def _dish(g, seg, rings, mat):
    """Closed paraboloid. Positive local Z is the concave face."""
    verts = [_place(0.0, 0.0, 0.0), _place(0.0, 0.0, -_THICK)]
    for i in range(1, rings + 1):
        r = _RADIUS * i / float(rings)
        z = _DEPTH * (r / _RADIUS) ** 2
        for j in range(seg):
            ang = 2.0 * math.pi * j / seg
            verts.append(_place(r * math.cos(ang), r * math.sin(ang), z))
        for j in range(seg):
            ang = 2.0 * math.pi * j / seg
            verts.append(_place(r * math.cos(ang), r * math.sin(ang), z - _THICK))

    def vid(layer, i, j):
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


def _past(a, b, before, after):
    dx, dy, dz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    length = math.sqrt(dx * dx + dy * dy + dz * dz) or 1.0
    ux, uy, uz = dx / length, dy / length, dz / length
    return (
        (a[0] - ux * before, a[1] - uy * before, a[2] - uz * before),
        (b[0] + ux * after, b[1] + uy * after, b[2] + uz * after),
    )


def _strut(g, a, b, bevel):
    dx, dy, dz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    length = math.sqrt(dx * dx + dy * dy + dz * dz)
    mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5, (a[2] + b[2]) * 0.5)
    g.box(mid, (0.028, 0.016, length), "Lib_Steel", euler=look_euler(a, b), bevel=bevel, segs=1)


@register
def create():
    a = Asset(
        "SatelliteDish",
        "StreetFurniture",
        "Offset dish, 0.80 m across and 0.16 m deep, on a roof shoe. Feed arm and LNB sit at the focus. The bowl tilts 36 deg up toward +Z.",
    )
    a.climb_note = "Roof bracket. Not a cling."
    a.vault_note = "The bowl is a thin shell."
    focus_d = _RADIUS * _RADIUS / (4.0 * _DEPTH)
    focus = _axis(focus_d)
    knuckle = _axis(-0.055)
    euler = look_euler(_axis(0.0), _axis(1.0))
    rim = _place(0.0, -_RADIUS, _DEPTH)
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 20, 12)
        rings = lod_pick(lod, 8, 4)
        bev = lod_pick(lod, 0.002, 0.0)
        g.box((0, 0.014, 0), (0.36, 0.028, 0.26), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0, 0.055, -0.02), (0.16, 0.06, 0.08), "Lib_Steel", bevel=bev, segs=1)
        if lod == 0:
            for sx in (-0.14, 0.14):
                for sz in (-0.09, 0.09):
                    g.cylinder((sx, 0.032, sz), 0.008, 0.012, "Lib_Steel", 6)
        _strut(g, (-0.07, 0.07, -0.02), (knuckle[0] - 0.04, knuckle[1] - 0.02, knuckle[2]), bev)
        _strut(g, (0.07, 0.07, -0.02), (knuckle[0] + 0.04, knuckle[1] - 0.02, knuckle[2]), bev)
        g.box(knuckle, (0.11, 0.09, 0.08), "Lib_SteelDark", euler=euler, bevel=bev, segs=1)
        _dish(g, seg, rings, "Lib_PaintWhite")
        if lod == 0:
            arm_a, arm_b = _past(rim, focus, 0.02, 0.03)
            _strut(g, arm_a, arm_b, bev)
            g.box(focus, (0.055, 0.04, 0.07), "Lib_Black", euler=euler, bevel=bev, segs=1)
            horn = _axis(focus_d - 0.04)
            g.cylinder(horn, 0.016, 0.035, "Lib_SteelDark", 8)
        a.end()
    a.box("Col_Shoe", (0, 0.014, 0), (0.30, 0.02, 0.20))
    a.box("Col_Riser", (0, 0.05, -0.02), (0.12, 0.04, 0.05))
    # Rear of the knuckle, clear of the dish skin.
    a.box("Col_Knuckle", _axis(-0.078), (0.055, 0.04, 0.028), euler=euler)
    # Convex boxes in the bowl meat. sz is along the dish axis.
    a.box("Col_BowlA", _place(0.0, 0.0, -_THICK * 0.50), (0.05, 0.05, 0.012), euler=euler)
    for i in range(4):
        ang = math.pi * 0.5 * i + 0.4
        rad = 0.12
        surf = _DEPTH * (rad / _RADIUS) ** 2
        a.box(
            "Col_Bowl%d" % i,
            _place(rad * math.cos(ang), rad * math.sin(ang), surf - _THICK * 0.50),
            (0.032, 0.032, 0.012),
            euler=euler,
        )
    return a
