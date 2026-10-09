"""Dressed four-way intersection. Asphalt is the inner 6 x 6 m.

Road_Straight still butts the asphalt edge (3 m from the center). The sidewalk
corners sit outside that edge, so they do not cover the socket.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import ROAD_TOP, ROAD_W, WALK_TOP, Asset, register

HALF = ROAD_W * 0.5
WALK = 2.0


@register
def create():
    a = Asset(
        "Road_Junction",
        "Roads",
        "10 x 10 m intersection. Inner asphalt is 6 x 6 m at 0.12 m, so Road_Straight butts each edge. "
        "Four concrete corner returns carry a 15 cm curb, and each arm has a zebra crosswalk.",
    )
    a.climb_note = "Flat asphalt and sidewalk. The curb face is 0.27 m from the pivot."
    a.vault_note = "Curb is 0.15 m above the road. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, ROAD_TOP * 0.5, 0), (ROAD_W, ROAD_TOP, ROAD_W), "Lib_Asphalt", uv_scale=0.4)
        for sx in (-1, 1):
            for sz in (-1, 1):
                # Inner face laps the asphalt by 1 cm so the curb joint does not crack.
                g.box(
                    (sx * (HALF + WALK * 0.5 - 0.01), WALK_TOP * 0.5, sz * (HALF + WALK * 0.5 - 0.01)),
                    (WALK, WALK_TOP, WALK),
                    "Lib_Concrete",
                    uv_scale=0.7,
                )
        _crosswalks(g)
        a.end()
    # Inset so the curb overlap is not a second shell around these samples.
    a.box("Col_Asphalt", (0, ROAD_TOP * 0.5, 0), (ROAD_W - 0.12, ROAD_TOP, ROAD_W - 0.12))
    for i, (sx, sz) in enumerate(((-1, -1), (-1, 1), (1, -1), (1, 1))):
        a.box(
            "Col_Walk_%d" % i,
            (sx * (HALF + WALK * 0.5 - 0.01), WALK_TOP * 0.5, sz * (HALF + WALK * 0.5 - 0.01)),
            (WALK - 0.04, WALK_TOP - 0.02, WALK - 0.04),
        )
    return a


def _crosswalks(g):
    """Bars run with the pedestrians. Paint only, just above the asphalt."""
    y = ROAD_TOP + 0.005
    bars = 7
    span = 4.4
    width = span / bars * 0.55
    length = 1.35
    inset = 1.85
    for i in range(bars):
        x = -span * 0.5 + (i + 0.5) * span / bars
        g.box((x, y, -inset), (width, 0.006, length), "Lib_PaintWhite")
        g.box((x, y, inset), (width, 0.006, length), "Lib_PaintWhite")
        z = -span * 0.5 + (i + 0.5) * span / bars
        g.box((-inset, y, z), (length, 0.006, width), "Lib_PaintWhite")
        g.box((inset, y, z), (length, 0.006, width), "Lib_PaintWhite")
