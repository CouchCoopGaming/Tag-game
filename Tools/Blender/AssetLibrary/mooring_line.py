"""Sagged mooring line along Z. Place the ends on a cleat and a boat cleat."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

LENGTH = 3.4
END_Y = 0.55
SAG = 0.28


@register
def create():
    a = Asset(
        "MooringLine",
        "Harbor",
        "Sagged rope, 3.4 m along Z. Ends at 0.55 m, belly about 0.28 m lower. No collider. Place an end on a cleat.",
    )
    a.allow_float = True
    a.loose_pivot = True
    a.climb_note = "A rope. Not a surface."
    a.vault_note = "No rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 6, 5)
        steps = lod_pick(lod, 6, 4)
        pts = []
        for i in range(steps + 1):
            t = i / steps
            z = -LENGTH * 0.5 + LENGTH * t
            y = END_Y - SAG * math.sin(math.pi * t)
            pts.append((0.0, y, z))
        for i in range(steps):
            g.pipe(pts[i], pts[i + 1], 0.012, "Lib_Rust", seg)
        a.end()
    return a
