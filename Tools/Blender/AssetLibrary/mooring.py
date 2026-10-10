"""Short mooring finger. Deck 0.55 m, two pilings, a cleat, and a rope. Place it beside HarborWater."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Mooring",
        "Harbor",
        "Mooring finger 3.2 x 1.4 m, deck at 0.55 m, cleat and rope on the +X edge. No water collider.",
    )
    a.loose_pivot = True
    a.climb_note = "Walk the deck. Pilings are round and sit on -X, so the pivot stays the deck center."
    a.vault_note = "Deck is 0.55 m. Under the vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        planks = lod_pick(lod, 10, 6)
        for i in range(planks):
            z = -1.6 + (i + 0.5) * (3.2 / planks)
            g.box((0, 0.535, z), (1.32, 0.03, 3.2 / planks * 0.86), "Lib_WoodWeather", uv_scale=1.2)
        for x in (-0.45, 0.45):
            g.box((x, 0.40, 0), (0.08, 0.12, 2.9), "Lib_WoodDark")
        for z in (-1.2, 1.2):
            g.cylinder((-0.55, 0.26, z), 0.09, 0.52, "Lib_WoodDark", seg)
        # Cleat on the outer edge.
        g.box((0.55, 0.60, 0.2), (0.16, 0.04, 0.06), "Lib_SteelDark")
        g.box((0.48, 0.64, 0.2), (0.05, 0.05, 0.14), "Lib_Steel")
        g.box((0.62, 0.64, 0.2), (0.05, 0.05, 0.14), "Lib_Steel")
        if lod == 0:
            pts = []
            for i in range(8):
                ang = math.radians(i * 40)
                pts.append((0.15 + math.cos(ang) * 0.16, 0.58 + 0.02 * math.sin(ang), -0.35 + math.sin(ang) * 0.16))
            for i in range(len(pts) - 1):
                g.pipe(pts[i], pts[i + 1], 0.012, "Lib_Rust", 5)
        a.end()
    for i in range(10):
        z = -1.6 + (i + 0.5) * 0.32
        a.box("Col_Plank_%d" % i, (0, 0.535, z), (1.20, 0.024, 0.24))
    a.capsule("Col_PileA", (-0.55, 0.26, -1.2), 0.08, 0.48, 1)
    a.capsule("Col_PileB", (-0.55, 0.26, 1.2), 0.08, 0.48, 1)
    a.box("Col_Cleat", (0.55, 0.60, 0.2), (0.12, 0.035, 0.05))
    return a
