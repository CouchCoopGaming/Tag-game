"""Street half-court. 14 x 10 m slab, key, free-throw line, and a 6 m arc."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Court",
        "Park",
        "Street half-court 14 m long (Z) by 10 m wide. Baseline is -Z. Paint has no collider.",
    )
    a.climb_note = "Flat slab, 0.12 m thick."
    a.vault_note = "No rail. Place Hoop just outside the -Z baseline."
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.06, 0), (10.0, 0.12, 14.0), "Lib_Court", bevel=0.01 if lod == 0 else 0, segs=1, uv_scale=0.25)
        y = 0.124
        # Boundary
        g.box((0, y, 0), (9.7, 0.006, 0.08), "Lib_PaintWhite")
        g.box((0, y, 0), (0.08, 0.006, 13.7), "Lib_PaintWhite")
        g.box((0, y, -6.85), (9.7, 0.006, 0.08), "Lib_PaintWhite")
        g.box((0, y, 6.85), (9.7, 0.006, 0.08), "Lib_PaintWhite")
        g.box((-4.85, y, 0), (0.08, 0.006, 13.7), "Lib_PaintWhite")
        g.box((4.85, y, 0), (0.08, 0.006, 13.7), "Lib_PaintWhite")
        # Key, from the -Z baseline.
        g.box((0, y, -4.7), (3.6, 0.006, 0.08), "Lib_PaintWhite")
        g.box((-1.8, y, -5.6), (0.08, 0.006, 2.4), "Lib_PaintWhite")
        g.box((1.8, y, -5.6), (0.08, 0.006, 2.4), "Lib_PaintWhite")
        steps = lod_pick(lod, 16, 8)
        for i in range(steps):
            ang = math.radians(-70 + 140 * (i + 0.5) / steps)
            r = 6.0
            x = math.sin(ang) * r
            z = -5.6 + math.cos(ang) * r
            g.box((x, y, z), (0.08, 0.006, 0.42), "Lib_PaintWhite", euler=(0, math.degrees(ang) + 90, 0))
        a.end()
    a.box("Col_Slab", (0, 0.06, 0), (10.0, 0.12, 14.0))
    return a
