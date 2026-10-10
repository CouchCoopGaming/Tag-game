"""One scaffold bay. 1.8 m wide, 1.2 m deep, deck at 1.70 m.

Posts are 48 mm tube. The deck is one plank with score lines. A brace sits
on the outside of the posts so it does not enter the post colliders.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Scaffold_Bay",
        "StreetFurniture",
        "Scaffold bay 1.80 m wide, 1.20 m deep. Deck at 1.70 m. Posts 48 mm tube, 2.05 m tall.",
    )
    a.climb_note = "Tubes are round. The deck is the stand."
    a.vault_note = "Deck is 1.70 m."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        bev = 0.002 if lod == 0 else 0.0
        for x in (-0.90, 0.90):
            for z in (-0.60, 0.60):
                g.cylinder((x, 1.02, z), 0.024, 2.04, "Lib_Steel", seg)
                g.cylinder((x, 0.03, z), 0.05, 0.06, "Lib_SteelDark", 6)
        g.cylinder((0, 1.78, -0.60), 0.018, 1.84, "Lib_Steel", 6, axis="X")
        g.cylinder((0, 1.78, 0.60), 0.018, 1.84, "Lib_Steel", 6, axis="X")
        g.cylinder((-0.90, 1.78, 0), 0.018, 1.28, "Lib_Steel", 6, axis="Z")
        g.cylinder((0.90, 1.78, 0), 0.018, 1.28, "Lib_Steel", 6, axis="Z")
        g.box((0, 1.68, 0), (1.88, 0.04, 1.28), "Lib_Wood", bevel=bev, segs=1 if lod == 0 else 0, uv_scale=0.5)
        if lod == 0:
            for x in (-0.4, 0.4):
                g.box((x, 1.704, 0), (0.012, 0.006, 0.90), "Lib_WoodDark")
            g.pipe((-0.86, 0.40, 0.64), (0.86, 1.50, 0.64), 0.016, "Lib_Steel", 6)
            g.box((0, 1.74, 0.62), (1.55, 0.12, 0.06), "Lib_Wood")
        a.end()
    # Tube is 48 mm and runs from the ground to above the deck. The capsule
    # stays inside it: bottom 1.5 cm off the pivot, top 2.5 cm under the deck.
    for name, x, z in (
        ("Col_PostA", -0.90, -0.60),
        ("Col_PostB", 0.90, -0.60),
        ("Col_PostC", -0.90, 0.60),
        ("Col_PostD", 0.90, 0.60),
    ):
        a.capsule(name, (x, 0.835, z), 0.016, 1.640)
    a.box("Col_Deck", (0, 1.688, 0), (1.40, 0.016, 0.80))
    return a
