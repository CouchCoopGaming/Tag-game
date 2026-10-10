"""Oak. Tapered trunk, forked limbs, overlapping leaf clusters. No white cap."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from _trees import broadleaf


OAK = {
    "trunk": (2.15, 0.20, 0.08),
    "limbs": [
        ((0.05, 1.55, 0.0), (1.15, 2.55, 0.55), 0.045, 0.62, "Lib_Foliage"),
        ((-0.04, 1.70, 0.05), (-1.05, 2.45, -0.35), 0.04, 0.58, "Lib_FoliageDark"),
        ((0.0, 1.85, -0.04), (0.35, 2.85, -1.05), 0.038, 0.55, "Lib_Foliage"),
        ((0.02, 2.05, 0.0), (0.15, 3.35, 0.25), 0.032, 0.48, "Lib_FoliageDark"),
        ((-0.02, 1.95, 0.02), (-0.45, 3.15, 0.85), 0.03, 0.42, "Lib_FoliageLite"),
        ((0.08, 1.65, -0.02), (0.95, 2.15, -0.15), 0.028, 0.36, "Lib_Foliage"),
    ],
}


@register
def create():
    a = Asset("Tree", "Park", "Oak. Tapered trunk to about 2.2 m, forked limbs, and overlapping green clusters. Canopy about 4.0 m tall.")
    a.climb_note = "Trunk is round, about 0.28 m at the flare. Not a flat cling wall."
    a.vault_note = "No rail. The canopy is visual; the trunk is the blocker."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        broadleaf(g, lod, OAK)
        a.end()
    a.box("Col_Flare", (0, 0.06, 0), (0.36, 0.12, 0.36))
    a.capsule("Col_Trunk", (0, 1.1, 0), 0.09, 1.9, 1)
    return a
