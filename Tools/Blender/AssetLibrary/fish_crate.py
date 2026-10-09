"""Plastic fish tote. Pivot is the base."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register


@register
def create():
    a = Asset(
        "FishCrate",
        "Harbor",
        "Fish tote, 0.72 x 0.46 m, 0.32 m tall. White body, blue rim. Sits on a deck.",
    )
    a.climb_note = "Too small to cling."
    a.vault_note = "Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.125, 0), (0.66, 0.25, 0.40), "Lib_PaintWhite")
        g.box((0, 0.275, 0), (0.72, 0.04, 0.46), "Lib_PaintBlue")
        if lod == 0:
            g.box((0, 0.16, 0.214), (0.40, 0.07, 0.012), "Lib_PaintBlue")
            g.box((0, 0.02, 0), (0.60, 0.02, 0.34), "Lib_PaintWhite")
        a.end()
    a.box("Col_Body", (0, 0.13, 0), (0.58, 0.22, 0.34))
    a.box("Col_Rim", (0, 0.275, 0), (0.64, 0.03, 0.40))
    return a
