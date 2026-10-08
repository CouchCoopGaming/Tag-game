"""Sidewalk parking pay station. Screen, keypad, card and coin slots. No brand.

About 1.60 m tall, 0.40 m wide, 0.32 m deep. The hood slopes toward the street.
Not the older lot cabinet.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

W = 0.40
D = 0.32
FRONT = D * 0.5


def _face(g, lod):
    seg = lod_pick(lod, 8, 6)
    z = FRONT + 0.006
    g.box((0, 1.22, z), (0.24, 0.16, 0.010), "Lib_Black")
    g.box((0, 1.22, z + 0.004), (0.22, 0.14, 0.006), "Lib_Glass")
    # Keypad. Twelve keys, no legends.
    if lod == 0:
        for row in range(4):
            for col in range(3):
                g.box((
                    -0.04 + col * 0.04,
                    0.92 + row * 0.036,
                    z + 0.004,
                ), (0.026, 0.022, 0.008), "Lib_Steel")
    else:
        g.box((0, 0.98, z), (0.14, 0.14, 0.010), "Lib_Steel")
    g.box((0, 0.78, z), (0.16, 0.012, 0.010), "Lib_Black")
    g.cylinder((0.08, 0.70, z + 0.004), 0.012, 0.010, "Lib_Black", seg, axis="Z")
    g.box((0, 0.46, z), (0.18, 0.018, 0.012), "Lib_Black")
    g.box((0, 0.43, z + 0.012), (0.16, 0.012, 0.020), "Lib_Steel")


@register
def create():
    a = Asset(
        "PayStation_Street",
        "StreetFurniture",
        "Pay station, 0.40 m wide, 1.60 m tall. Screen, keypad, card slot, coin, ticket mouth. No brand.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.025, 0), (0.48, 0.05, 0.40), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((0, 0.78, 0), (W, 1.50, D), "Lib_Steel", bevel=bev, segs=bs)
        # Hood slopes down toward the front. A plate, then a dark blank panel.
        g.box((0, 1.54, -0.01), (W + 0.04, 0.04, D + 0.06), "Lib_SteelDark", euler=(8, 0, 0))
        g.box((0, 1.56, -0.02), (0.30, 0.008, 0.22), "Lib_Black", euler=(8, 0, 0))
        _face(g, lod)
        a.end()
    a.box("Col_Base", (0, 0.020, 0), (0.34, 0.028, 0.26))
    # Core of the cabinet, clear of the face hardware and the hood plate.
    a.box("Col_Body", (0, 0.78, -0.02), (0.28, 1.20, 0.18))
    return a
