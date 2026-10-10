"""Sidewalk collection box. No legend, no eagle, no brand.

1.27 m to the crown. Footprint 0.53 m wide and 0.50 m deep, on four short
legs. The top is a half-cylinder whose axis runs front to back, so the
front reads as an arch. Hopper and carrier door are both on the front.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

WIDTH = 0.53
DEPTH = 0.50
CROWN = 1.27
LEG = 0.10
RADIUS = WIDTH * 0.5
SPRING = CROWN - RADIUS
FRONT = DEPTH * 0.5


def _profile(steps):
    """Front outline, bottom-left, up the side, over the arch, down the other side."""
    pts = [(-RADIUS, LEG), (-RADIUS, SPRING)]
    for i in range(1, steps):
        ang = math.pi * (1.0 - i / float(steps))
        pts.append((RADIUS * math.cos(ang), SPRING + RADIUS * math.sin(ang)))
    pts.append((RADIUS, SPRING))
    pts.append((RADIUS, LEG))
    return pts


def _carcass(g, lod):
    steps = lod_pick(lod, 32, 12)
    outline = _profile(steps)
    z0, z1 = -FRONT, FRONT
    verts = [(x, y, z0) for x, y in outline] + [(x, y, z1) for x, y in outline]
    n = len(outline)
    faces = []
    for i in range(n - 1):
        faces.append((i, i + 1, n + i + 1, n + i))
    faces.append((n - 1, 0, n, n + n - 1))
    # Convex cap. Fan from the first corner.
    for i in range(1, n - 1):
        faces.append((0, i, i + 1))
        faces.append((n, n + i + 1, n + i))
    g.mesh(verts, faces, "Lib_BoxBlue", bevel=lod_pick(lod, 0.004, 0.0), segs=1 if lod == 0 else 0)


def _hopper(g, lod):
    """Pull-down hopper on the upper front. Hinged at the top, handle at the lip."""
    seg = lod_pick(lod, 10, 6)
    hinge_y = 0.96
    hinge_z = FRONT + 0.004
    length = 0.22
    ang = math.radians(16.0)
    # Bottom of the flap swings out and down.
    lip_y = hinge_y - length * math.cos(ang)
    lip_z = hinge_z + length * math.sin(ang)
    half_w = 0.17
    thick = 0.012
    # A thin plate. Local thickness stays along the flap normal.
    ny = math.sin(ang)
    nz = math.cos(ang)
    verts = []
    for y, z in ((hinge_y, hinge_z), (lip_y, lip_z)):
        for x in (-half_w, half_w):
            verts.append((x, y, z))
            verts.append((x, y + ny * thick, z + nz * thick))
    # 0,1 hinge-left back/front; 2,3 hinge-right; 4,5 lip-left; 6,7 lip-right
    g.mesh(verts, [
        (0, 2, 6, 4),
        (1, 5, 7, 3),
        (0, 4, 5, 1),
        (2, 3, 7, 6),
        (0, 1, 3, 2),
        (4, 6, 7, 5),
    ], "Lib_BoxBlue")
    # Dark mouth behind the flap.
    g.box((0, hinge_y - 0.08, FRONT - 0.012), (0.30, 0.14, 0.016), "Lib_Black")
    # Pull bar on the lip, held off the plate by two posts.
    bar_y = lip_y + 0.012
    bar_z = lip_z + 0.028
    g.cylinder((0, bar_y, bar_z), 0.011, 0.26, "Lib_Steel", seg, axis="X")
    for x in (-0.09, 0.09):
        g.cylinder((x, lip_y + 0.006, lip_z + 0.012), 0.008, 0.028, "Lib_Steel", seg, axis="Z")


def _carrier(g, lod):
    """Small collection door, low on the front. No legend."""
    seg = lod_pick(lod, 10, 6)
    bev = lod_pick(lod, 0.002, 0.0)
    bs = 1 if lod == 0 else 0
    z = FRONT + 0.010
    g.box((0, 0.36, z), (0.30, 0.34, 0.012), "Lib_SteelDark", bevel=bev, segs=bs)
    g.box((0, 0.36, z + 0.008), (0.26, 0.30, 0.010), "Lib_BoxBlue", bevel=bev, segs=bs)
    g.cylinder((0.0, 0.26, z + 0.028), 0.010, 0.12, "Lib_Steel", seg, axis="X")
    g.cylinder((-0.05, 0.26, z + 0.018), 0.007, 0.016, "Lib_Steel", seg, axis="Z")
    g.cylinder((0.05, 0.26, z + 0.018), 0.007, 0.016, "Lib_Steel", seg, axis="Z")
    g.cylinder((0.08, 0.46, z + 0.020), 0.014, 0.016, "Lib_Brass", seg, axis="Z")
    if lod == 0:
        g.cylinder((0.08, 0.46, z + 0.030), 0.005, 0.006, "Lib_Black", 8, axis="Z")


@register
def create():
    a = Asset(
        "MailDrop_Corner",
        "StreetFurniture",
        "Collection box, 1.27 m, footprint 0.53 by 0.50 m, four legs. Half-cylinder top, front hopper, low carrier door. No legend.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Rounded top, under a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        _carcass(g, lod)
        for x in (-0.185, 0.185):
            for z in (-0.16, 0.16):
                # Leg sits on the foot. The two solids do not share a volume.
                g.box((x, 0.014, z), (0.07, 0.028, 0.07), "Lib_SteelDark")
                g.cylinder((x, 0.098, z), 0.022, 0.12, "Lib_SteelDark", seg)
        _hopper(g, lod)
        _carrier(g, lod)
        if lod == 0:
            g.box((0.16, 0.22, FRONT + 0.004), (0.06, 0.05, 0.004), "Lib_MetalWorn")
        a.end()
    # Interior of the one carcass solid. Crown box stays under the arch.
    a.box("Col_Body", (0, 0.54, 0), (0.40, 0.68, 0.36))
    a.box("Col_Crown", (0, 1.20, 0), (0.06, 0.10, 0.28))
    for i, x in enumerate((-0.185, 0.185)):
        for j, z in enumerate((-0.16, 0.16)):
            a.box("Col_Leg_%d%d" % (i, j), (x, 0.070, z), (0.024, 0.040, 0.024))
            a.box("Col_Foot_%d%d" % (i, j), (x, 0.014, z), (0.040, 0.016, 0.040))
    return a
