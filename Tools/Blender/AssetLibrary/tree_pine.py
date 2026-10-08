"""Pine. Tall taper, whorled branches, dark needle clusters, a small dark tip."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from _trees import pine_tree


@register
def create():
    a = Asset("Tree_Pine", "Park", "Pine. Trunk to about 3.4 m, branch whorls, and a narrow crown of needle clusters.")
    a.climb_note = "Trunk is round. Not a flat cling wall."
    a.vault_note = "No rail. The crown is visual; the trunk is the blocker."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        pine_tree(g, lod)
        a.end()
    a.box("Col_Flare", (0, 0.05, 0), (0.28, 0.10, 0.28))
    a.capsule("Col_Trunk", (0, 1.7, 0), 0.07, 3.2, 1)
    return a
