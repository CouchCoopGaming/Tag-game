"""Outdoor cafe table and two chairs.

Table top is 0.72 m square at 0.76 m. Chair seats are at 0.45 m.
The chairs sit clear of the top so each piece is its own solid on the ground.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _chair(g, z, lod):
    bev = 0.003 if lod == 0 else 0.0
    seat_y = 0.45
    for x in (-0.16, 0.16):
        for dz in (-0.16, 0.16):
            g.cylinder((x, 0.22, z + dz), 0.014, 0.44, "Lib_SteelDark", 6)
    g.box((0, seat_y, z), (0.42, 0.028, 0.40), "Lib_Wood", bevel=bev, segs=1 if lod == 0 else 0)
    for x in (-0.16, 0.16):
        g.cylinder((x, seat_y + 0.22, z - 0.16), 0.014, 0.48, "Lib_SteelDark", 6)
    g.box((0, seat_y + 0.40, z - 0.16), (0.36, 0.03, 0.02), "Lib_Wood", bevel=bev, segs=1 if lod == 0 else 0)
    if lod == 0:
        g.box((0, seat_y + 0.22, z - 0.16), (0.30, 0.025, 0.016), "Lib_Wood")


@register
def create():
    a = Asset(
        "CafeSet_Bistro",
        "StreetFurniture",
        "Cafe table 0.72 m square, top at 0.76 m, plus two chairs with seats at 0.45 m.",
    )
    a.climb_note = "Table edge is a small lip."
    a.vault_note = "Top is 0.76 m. A low step, not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.025, 0), (0.36, 0.05, 0.36), "Lib_SteelDark", bevel=bev, segs=bs)
        g.cylinder((0, 0.385, 0), 0.040, 0.71, "Lib_Steel", lod_pick(lod, 10, 6))
        g.box((0, 0.74, 0), (0.72, 0.04, 0.72), "Lib_Wood", bevel=bev, segs=bs, uv_scale=0.6)
        if lod == 0:
            g.box((0.22, 0.748, 0.22), (0.12, 0.006, 0.08), "Lib_WoodWeather")
        _chair(g, 0.62, lod)
        _chair(g, -0.62, lod)
        a.end()
    # Pedestal bites only the underside of the top. Collider stays in the wood above it.
    a.box("Col_Top", (0, 0.748, 0), (0.56, 0.016, 0.56))
    a.capsule("Col_Post", (0, 0.40, 0), 0.024, 0.50)
    a.box("Col_SeatN", (0, 0.456, 0.62), (0.30, 0.012, 0.26))
    a.box("Col_SeatS", (0, 0.456, -0.62), (0.30, 0.012, 0.26))
    a.box("Col_BackN", (0, 0.85, 0.46), (0.24, 0.016, 0.012))
    a.box("Col_BackS", (0, 0.85, -0.78), (0.24, 0.016, 0.012))
    return a
