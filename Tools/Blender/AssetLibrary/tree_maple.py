"""Maple. Upright trunk, denser crown, olive and deep green clusters."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from _trees import broadleaf


MAPLE = {
    "trunk": (2.4, 0.16, 0.06),
    "limbs": [
        ((0.04, 1.7, 0.0), (0.85, 2.9, 0.45), 0.036, 0.52, "Lib_FoliageLite"),
        ((-0.03, 1.85, 0.02), (-0.8, 2.85, -0.3), 0.034, 0.5, "Lib_Foliage"),
        ((0.0, 2.0, -0.02), (0.25, 3.25, -0.7), 0.03, 0.46, "Lib_FoliageDark"),
        ((0.0, 2.15, 0.0), (-0.2, 3.55, 0.35), 0.026, 0.4, "Lib_Foliage"),
        ((0.02, 1.6, 0.04), (0.55, 2.35, -0.55), 0.028, 0.38, "Lib_FoliageLite"),
        ((-0.02, 1.75, -0.02), (-0.4, 2.4, 0.7), 0.026, 0.36, "Lib_FoliageDark"),
    ],
}


@register
def create():
    a = Asset("Tree_Maple", "Park", "Maple. Slimmer trunk than the oak, upright forks, olive and deep-green clusters.")
    a.climb_note = "Trunk is round. Not a flat cling wall."
    a.vault_note = "No rail. The canopy is visual; the trunk is the blocker."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        broadleaf(g, lod, MAPLE)
        a.end()
    a.box("Col_Flare", (0, 0.05, 0), (0.28, 0.10, 0.28))
    # Trunk capsule starts 2 cm above the flare box. The cone mesh covers that band.
    a.capsule("Col_Trunk", (0, 1.185, 0), 0.07, 2.13, 1)
    return a
