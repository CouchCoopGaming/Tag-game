"""Slatted shipping crate. 0.80 m cube. Corner posts carry the faces."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Crate", "Harbor", "Wood crate, 0.80 m cube. Slats sit on corner posts. Gaps are about 2 cm and are not a way through.")
    a.climb_note = "A crate this size is a blocker, not a cling wall."
    a.vault_note = "Top is 0.80 m. Under vault height, and the lid is the whole top."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        slats = lod_pick(lod, 5, 3)
        for x in (-0.36, 0.36):
            for z in (-0.36, 0.36):
                g.box((x, 0.40, z), (0.06, 0.80, 0.06), "Lib_WoodDark", bevel=bev, segs=1)
        _face_slats(g, slats, "Z", 0.37, bev, lod)
        _face_slats(g, slats, "Z", -0.37, bev, lod)
        _face_slats(g, slats, "X", 0.37, bev, lod)
        _face_slats(g, slats, "X", -0.37, bev, lod)
        _face_slats(g, slats, "Y", 0.37, bev, lod)
        g.box((0, 0.03, 0), (0.68, 0.04, 0.68), "Lib_Wood", uv_scale=1.0)
        g.box((0, 0.785, 0), (0.66, 0.03, 0.66), "Lib_Wood", uv_scale=1.0)
        if lod == 0:
            for x in (-0.36, 0.36):
                for z in (-0.36, 0.36):
                    g.box((x, 0.02, z), (0.08, 0.04, 0.08), "Lib_SteelDark")
            g.box((0, 0.45, 0.40), (0.28, 0.10, 0.008), "Lib_Black")
        a.end()
    # Inside the posts and the lid. Slat gaps are ~2 cm and are not a passage.
    a.box("Col_Crate", (0, 0.40, 0), (0.70, 0.74, 0.70))
    return a


def _face_slats(g, count, axis, pos, bev, lod):
    span = 0.64
    pitch = span / count
    board = pitch * 0.82
    for i in range(count):
        t = -span * 0.5 + pitch * (i + 0.5)
        if axis == "Z":
            g.box((0, 0.40 + t, pos), (0.70, board, 0.018), "Lib_Wood", bevel=bev, segs=1 if lod == 0 else 0, uv_scale=1.2)
        elif axis == "X":
            g.box((pos, 0.40 + t, 0), (0.018, board, 0.70), "Lib_Wood", bevel=bev, segs=1 if lod == 0 else 0, uv_scale=1.2)
        else:
            g.box((t, 0.74, 0), (board, 0.018, 0.70), "Lib_Wood", bevel=bev, segs=1 if lod == 0 else 0, uv_scale=1.2)
