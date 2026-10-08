"""Street palm. Leaning ringed trunk and a crown of separate fronds."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from _trees import palm_tree


@register
def create():
    a = Asset(
        "Tree_Palm",
        "Park",
        "Street palm. Ringed trunk with a slight lean, about 4.2 m to the crown, arching fronds.",
    )
    a.climb_note = "Trunk is round. Not a flat cling wall."
    a.vault_note = "No rail. Fronds are visual; the trunk is the blocker."
    a.loose_pivot = True
    for lod in (0, 1, 2):
        g = a.begin(lod)
        palm_tree(g, lod)
        a.end()
    a.capsule("Col_Trunk", (0.04, 1.15, 0.02), 0.055, 1.5, 1)
    return a
