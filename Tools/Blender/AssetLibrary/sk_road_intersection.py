"""2-lane crossing. 6 m square so a two-lane tile butts each face."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_street_road import LANE, asphalt, paint, slab_collider, zebra

SPAN = LANE * 2


@register
def create():
    a = Asset(
        "StreetRoad_Intersection",
        "StreetFurniture",
        "6 m square crossing. Surface at 0.12 m. Continental bars and a 0.40 m stop bar on each approach.",
    )
    a.climb_note = "Flat asphalt."
    a.vault_note = "No curb on this tile."
    for lod in (0, 1):
        g = a.begin(lod)
        asphalt(g, lod, SPAN, SPAN)
        bars = lod_pick(lod, 6, 4)
        for sign, along in ((1, "z"), (-1, "z"), (1, "x"), (-1, "x")):
            zebra(g, along, sign * 1.55, bars, 1.50, 4.2)
            if along == "z":
                paint(g, 0, sign * 2.62, 4.4, 0.40)
            else:
                paint(g, sign * 2.62, 0, 0.40, 4.4)
        a.end()
    slab_collider(a, SPAN, SPAN)
    return a
