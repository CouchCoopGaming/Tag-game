"""Shade tree. Flared trunk, a solid canopy core, and crossed leaf cards."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _card(g, center, w, h, yaw, tilt, mat):
    hw, hh = w * 0.5, h * 0.5
    cy, sy = math.cos(yaw), math.sin(yaw)
    ct, st = math.cos(tilt), math.sin(tilt)
    local = [(-hw, -hh), (hw, -hh), (hw, hh), (-hw, hh)]
    verts = []
    for lx, ly in local:
        x = lx * cy + ly * st * sy
        y = ly * ct
        z = -lx * sy + ly * st * cy
        verts.append((center[0] + x, center[1] + y, center[2] + z))
    g.mesh(verts, [(0, 1, 2, 3), (3, 2, 1, 0)], mat)


def _cards(g, lod):
    clusters = lod_pick(lod, 16, 8)
    for i in range(clusters):
        ang = (i * 2.399) % 6.28318
        rad = 0.85 + (i % 5) * 0.18
        y = 2.7 + (i % 4) * 0.42
        center = (math.sin(ang) * rad, y, math.cos(ang) * rad * 0.85)
        mat = "Lib_FoliageDark" if i % 3 == 0 else "Lib_Foliage"
        yaw = ang
        tilt = 0.35 + (i % 3) * 0.25
        _card(g, center, 0.95, 0.72, yaw, tilt, mat)
        _card(g, center, 0.85, 0.66, yaw + 1.2, tilt * 0.6, mat)


@register
def create():
    a = Asset("Tree", "Park", "Shade tree, trunk to 1.8 m, canopy about 5.2 m tall and 3.4 m across.")
    a.climb_note = "Trunk is round, 0.28 m at the flare. Not a flat cling wall."
    a.vault_note = "No rail. Canopy is visual."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        g.cylinder((0, 0.07, 0), 0.22, 0.14, "Lib_WoodDark", seg)
        g.cone((0, 0.98, 0), 0.14, 0.08, 1.68, "Lib_WoodDark", seg)
        voxel = 0.16 if lod == 0 else 0.28
        g.blob(
            [
                ((0.0, 3.3, 0.0), 1.15),
                ((0.55, 3.45, 0.25), 0.78),
                ((-0.5, 3.35, -0.15), 0.74),
                ((0.05, 4.0, -0.25), 0.62),
            ],
            "Lib_Foliage",
            voxel=voxel,
        )
        _cards(g, lod)
        a.end()
    a.box("Col_Flare", (0, 0.07, 0), (0.28, 0.12, 0.28))
    a.capsule("Col_Trunk", (0, 1.05, 0), 0.07, 1.20, 1)
    # Leaf cards are not a closed volume, so the crown stays visual.
    a.vault_note = "No rail. The canopy is visual; the trunk is the blocker."
    return a
