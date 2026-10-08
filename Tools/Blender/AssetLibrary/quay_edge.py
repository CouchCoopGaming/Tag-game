"""Concrete quay. 18 m along X, an 8.2 m apron inland of the water face.

Bullnose, edge stone, bollards, a ladder, and five fenders are on -Z.
The face runs below the pivot. Deck top is 0.90 m.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

# Walking surface runs from the water edge inland. The old 3 m apron could not
# hold a 20 ft container without filling the frame.
APRON_Z0 = -1.05
APRON_Z1 = 7.15


@register
def create():
    a = Asset(
        "Quay_Edge",
        "Harbor",
        "Concrete quay, 18 m long, apron 8.2 m deep, deck at 0.90 m. Bullnose, edge stone, four bollards, a ladder, and five fenders on -Z. The face runs into the water.",
    )
    a.climb_note = "The face is a wall. Too low and too thick to treat as a cling panel from the water."
    a.vault_note = "Deck is 0.90 m. The bullnose is rounded, not a rail."
    a.allow_below = True
    a.loose_pivot = True
    apron = APRON_Z1 - APRON_Z0
    cz = (APRON_Z0 + APRON_Z1) * 0.5
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = 0.01 if lod == 0 else 0
        g.box((0, 0.72, cz), (18.0, 0.36, apron), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.4)
        g.box((0, 0.16, -1.22), (18.0, 0.72, 0.28), "Lib_Concrete", uv_scale=0.4)
        g.cylinder((0, 0.84, -1.02), 0.10, 17.6, "Lib_Concrete", seg, axis="X")
        # Coping sits a centimetre above the deck so the two skins do not share a face.
        g.box((0, 0.965, -0.86), (17.4, 0.09, 0.26), "Lib_Concrete", uv_scale=0.5)
        if lod == 0:
            g.box((0, 1.025, -0.86), (17.1, 0.025, 0.16), "Lib_Concrete")
        for x in (-7.0, -3.5, 0.0, 3.5, 7.0):
            g.cylinder((x, 0.32, -1.58), 0.14, 0.62, "Lib_Rubber", seg)
            if lod == 0:
                g.box((x, 0.62, -1.58), (0.16, 0.03, 0.16), "Lib_SteelDark")
        for x in (-6.2, -2.1, 2.4, 6.4):
            g.cylinder((x, 0.96, -0.42), 0.16, 0.08, "Lib_SteelDark", seg)
            g.cylinder((x, 1.22, -0.42), 0.09, 0.42, "Lib_Steel", seg)
            g.sphere((x, 1.46, -0.42), 0.12, "Lib_Steel", max(8, seg - 2))
        # Ladder just off the face, clear of the fenders, down into the water.
        for x in (8.42, 8.78):
            g.pipe((x, -0.45, -1.50), (x, 1.02, -1.50), 0.028, "Lib_Steel", 6)
        rungs = lod_pick(lod, 7, 4)
        for i in range(rungs):
            y = -0.28 + i * (1.15 / max(1, rungs - 1))
            g.pipe((8.42, y, -1.50), (8.78, y, -1.50), 0.016, "Lib_Steel", 5)
        if lod == 0:
            g.box((0, 0.915, 1.4), (16.8, 0.012, 0.08), "Lib_Lane")
        a.end()
    a.box("Col_Deck", (0, 0.72, cz), (17.5, 0.30, apron - 0.24))
    a.box("Col_Face", (0, 0.18, -1.22), (17.6, 0.64, 0.22))
    a.box("Col_Coping", (0, 0.965, -0.86), (17.0, 0.06, 0.18))
    for i, x in enumerate((-7.0, -3.5, 0.0, 3.5, 7.0)):
        a.capsule("Col_Fender_%d" % i, (x, 0.32, -1.58), 0.11, 0.48, 1)
    for i, x in enumerate((-6.2, -2.1, 2.4, 6.4)):
        a.capsule("Col_Bollard_%d" % i, (x, 1.18, -0.42), 0.07, 0.36, 1)
    a.capsule("Col_LadderL", (8.42, 0.30, -1.50), 0.02, 1.15, 1)
    a.capsule("Col_LadderR", (8.78, 0.30, -1.50), 0.02, 1.15, 1)
    return a
