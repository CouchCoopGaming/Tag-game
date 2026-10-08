"""1.8 m scale figure for the showcase. Not a gameplay character."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Mannequin",
        "Showcase",
        "Scale figure, 1.80 m tall. Stands in the showcase so props can be judged against a player.",
    )
    a.climb_note = "Reference only."
    a.vault_note = "Reference only."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        g.cylinder((-0.10, 0.42, 0), 0.07, 0.84, "Lib_Rubber", seg)
        g.cylinder((0.10, 0.42, 0), 0.07, 0.84, "Lib_Rubber", seg)
        g.cylinder((0, 1.05, 0), 0.16, 0.62, "Lib_PaintBlue", seg)
        g.cylinder((0, 1.40, 0), 0.07, 0.10, "Lib_PaintBlue", seg)
        g.sphere((0, 1.66, 0), 0.14, "Lib_PaintWhite", seg)
        g.cylinder((-0.26, 1.00, 0), 0.045, 0.48, "Lib_PaintBlue", seg)
        g.cylinder((0.26, 1.00, 0), 0.045, 0.48, "Lib_PaintBlue", seg)
        g.sphere((-0.26, 1.26, 0), 0.055, "Lib_PaintBlue", seg)
        g.sphere((0.26, 1.26, 0), 0.055, "Lib_PaintBlue", seg)
        a.end()
    a.capsule("Col_Body", (0, 1.05, 0), 0.16, 0.62, 1)
    a.capsule("Col_LegL", (-0.10, 0.42, 0), 0.07, 0.84, 1)
    a.capsule("Col_LegR", (0.10, 0.42, 0), 0.07, 0.84, 1)
    a.sphere("Col_Head", (0, 1.66, 0), 0.13)
    a.capsule("Col_ArmL", (-0.26, 1.00, 0), 0.045, 0.48, 1)
    a.capsule("Col_ArmR", (0.26, 1.00, 0), 0.045, 0.48, 1)
    return a
