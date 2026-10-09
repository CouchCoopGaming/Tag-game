"""One overpass span. Deck matches the 6 m road width. Piers sit on the ground."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import ROAD_W, Asset, register, lod_pick


DECK_L = 14.0
DECK_TOP = 5.02
DECK_BOT = 4.58


@register
def create():
    a = Asset(
        "Overpass",
        "Roads",
        "Single span, 14 m along Z and 6 m wide so it continues a Road_Straight. "
        "Asphalt deck top is 5.02 m. Two piers carry it. Clearance under the girders is about 4.2 m.",
    )
    a.climb_note = "The piers are the cling faces. The deck is the road."
    a.vault_note = "No rail at street level. The parapet is on the deck."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        _deck(g)
        _piers(g)
        _rails(g, lod)
        a.end()
    a.box("Col_Deck", (0.0, (DECK_BOT + 4.96) * 0.5, 0.0), (ROAD_W - 0.20, 4.96 - DECK_BOT - 0.06, DECK_L - 0.20))
    a.box("Col_Wearing", (0.0, 4.985, 0.0), (ROAD_W - 0.30, 0.04, DECK_L - 0.30))
    for i, z in enumerate((-3.4, 3.4)):
        a.box("Col_Pier_%d" % i, (0.0, 2.20, z), (0.56, 4.20, 0.70))
    a.box("Col_RailL", (-2.82, 5.55, 0.0), (0.08, 0.90, DECK_L - 0.40), approx=True)
    a.box("Col_RailR", (2.82, 5.55, 0.0), (0.08, 0.90, DECK_L - 0.40), approx=True)
    return a


def _deck(g):
    g.box((0.0, (DECK_BOT + 4.96) * 0.5, 0.0), (ROAD_W, 4.96 - DECK_BOT, DECK_L), "Lib_Concrete", uv_scale=0.35)
    g.box((0.0, 4.992, 0.0), (ROAD_W - 0.08, 0.048, DECK_L - 0.08), "Lib_Asphalt", uv_scale=0.3)
    # Girders under the slab, clear of the piers in X.
    for sign in (-1, 1):
        g.box((sign * 2.15, 4.28, 0.0), (0.28, 0.52, DECK_L - 0.40), "Lib_Concrete", uv_scale=0.4)


def _piers(g):
    for z in (-3.4, 3.4):
        g.box((0.0, 2.20, z), (0.72, 4.40, 0.92), "Lib_Concrete", uv_scale=0.45)
        g.box((0.0, 4.50, z), (1.15, 0.12, 1.20), "Lib_Concrete", uv_scale=0.5)


def _rails(g, lod):
    step = lod_pick(lod, 1.55, 2.8, 0.0)
    for sign in (-1, 1):
        x = sign * 2.82
        g.box((x, 6.02, 0.0), (0.08, 0.08, DECK_L - 0.30), "Lib_Steel")
        g.box((sign * 2.72, 5.35, 0.0), (0.04, 0.04, DECK_L - 0.40), "Lib_SteelDark")
        if step <= 0:
            continue
        z = -6.2
        while z <= 6.21:
            g.box((x, 5.50, z), (0.07, 0.92, 0.07), "Lib_SteelDark")
            z += step
