"""Street tree in a square steel grate. Trunk through the opening, bars clear of the bark."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from _trees import broadleaf

STREET = {
    "trunk": (2.35, 0.16, 0.07),
    "limbs": [
        ((0.04, 1.45, 0.0), (0.85, 2.35, 0.40), 0.04, 0.55, "Lib_Foliage"),
        ((-0.03, 1.55, 0.04), (-0.78, 2.25, -0.28), 0.036, 0.50, "Lib_FoliageDark"),
        ((0.0, 1.70, -0.03), (0.22, 2.70, -0.72), 0.032, 0.46, "Lib_Foliage"),
        ((-0.02, 1.80, 0.02), (-0.28, 2.95, 0.55), 0.028, 0.42, "Lib_FoliageLite"),
    ],
}


def _grate(g):
    # Square frame. End bars stop short of the long bars so the corners do not share a volume.
    g.box((0, 0.03, 0.56), (1.20, 0.04, 0.08), "Lib_SteelDark")
    g.box((0, 0.03, -0.56), (1.20, 0.04, 0.08), "Lib_SteelDark")
    g.box((0.56, 0.03, 0), (0.08, 0.04, 1.032), "Lib_SteelDark")
    g.box((-0.56, 0.03, 0), (0.08, 0.04, 1.032), "Lib_SteelDark")
    # Slats above the frame, clear of the trunk flare (radius about 0.20).
    for zz in (-0.40, -0.26, 0.26, 0.40):
        g.box((0, 0.062, zz), (1.00, 0.016, 0.028), "Lib_Steel")


@register
def create():
    a = Asset(
        "Tree_Grate",
        "Park",
        "Street tree in a 1.20 m steel grate. Trunk about 2.4 m to the forks, canopy about 3.4 m. The grate opening clears the flare.",
    )
    a.climb_note = "Trunk is round, about 0.22 m at the flare. Not a flat cling wall."
    a.vault_note = "No rail. The grate is a sidewalk skin. The trunk is the blocker."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        _grate(g)
        broadleaf(g, lod, STREET)
        a.end()
    a.box("Col_GrateN", (0, 0.03, 0.56), (1.12, 0.03, 0.06))
    a.box("Col_GrateS", (0, 0.03, -0.56), (1.12, 0.03, 0.06))
    a.box("Col_GrateE", (0.56, 0.03, 0), (0.06, 0.03, 0.96))
    a.box("Col_GrateW", (-0.56, 0.03, 0), (0.06, 0.03, 0.96))
    a.box("Col_Flare", (0, 0.06, 0), (0.28, 0.08, 0.28))
    a.capsule("Col_Trunk", (0, 1.15, 0), 0.07, 2.0, 1)
    return a
