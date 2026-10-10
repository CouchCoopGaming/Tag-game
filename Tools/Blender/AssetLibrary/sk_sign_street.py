"""Street-name blades. One 3 mm sheet each, not a pair of thick plates.

MAIN is 36 x 9 in and faces +Z. 5TH AVE is 30 x 9 in and faces +X.
Letters are about two thirds of the blade height, Overpass Bold (OFL).
A cast hub and two clamp straps hold each blade off the pole.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import block_word


@register
def create():
    a = Asset(
        "Sign_StreetName",
        "StreetFurniture",
        "Street blades 0.91 x 0.23 m and 0.76 x 0.23 m, 3 mm thick. Bottoms at 2.74 m and 3.01 m. Slip base. MAIN ST and 5TH AVE in Overpass.",
    )
    a.climb_note = "60 mm pole. Not a cling."
    a.vault_note = "Blades are thin and high."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        bev = lod_pick(lod, 0.002, 0.0)
        bs = 1 if lod == 0 else 0
        # Slip base: anchor plate, four bolts, and a two-flange coupling.
        g.box((0, 0.006, 0), (0.22, 0.012, 0.22), "Lib_SteelDark", bevel=bev, segs=bs)
        for x in (-0.07, 0.07):
            for z in (-0.07, 0.07):
                g.cylinder((x, 0.016, z), 0.008, 0.012, "Lib_Steel", 6)
        g.cylinder((0, 0.028, 0), 0.055, 0.016, "Lib_Steel", seg)
        g.cylinder((0, 0.052, 0), 0.048, 0.020, "Lib_SteelDark", seg)
        g.cylinder((0, 1.66, 0), 0.030, 3.24, "Lib_Steel", seg)
        g.sphere((0, 3.30, 0), 0.036, "Lib_Steel", seg)
        # One 3 mm blade per street, clear of the pole. White core, green both faces.
        _blade(g, "z", 2.855, 0.914, bev, bs)
        _blade(g, "x", 3.125, 0.762, bev, bs)
        # Cast hubs. Clamp straps bite the pole and the blade. Bolt heads sit on the face.
        g.cylinder((0, 2.855, 0), 0.044, 0.055, "Lib_SteelDark", seg)
        g.cylinder((0, 3.125, 0), 0.044, 0.055, "Lib_SteelDark", seg)
        for y in (2.775, 2.935):
            g.box((0, y, 0.024), (0.040, 0.014, 0.032), "Lib_Steel", bevel=bev, segs=bs)
            g.cylinder((0.0, y, 0.044), 0.007, 0.005, "Lib_Steel", 6, axis="Z")
        for y in (3.045, 3.205):
            g.box((0.024, y, 0), (0.032, 0.014, 0.040), "Lib_Steel", bevel=bev, segs=bs)
            g.cylinder((0.044, y, 0.0), 0.007, 0.005, "Lib_Steel", 6, axis="X")
        if lod == 0:
            # Block letters sit just off each face of the 3 mm blade.
            block_word(g, "MAIN", (-0.12, 2.855, 0.045), 0.16, "Lib_PaintWhite", depth=0.002)
            block_word(g, "ST", (0.34, 2.840, 0.045), 0.09, "Lib_PaintWhite", depth=0.002)
            block_word(g, "MAIN", (-0.12, 2.855, 0.037), 0.16, "Lib_PaintWhite", depth=0.002)
            block_word(g, "ST", (0.34, 2.840, 0.037), 0.09, "Lib_PaintWhite", depth=0.002)
            block_word(g, "5TH", (0.045, 3.125, -0.10), 0.15, "Lib_PaintWhite", axis="z", depth=0.002)
            block_word(g, "AVE", (0.045, 3.100, 0.22), 0.08, "Lib_PaintWhite", axis="z", depth=0.002)
            block_word(g, "5TH", (0.037, 3.125, -0.10), 0.15, "Lib_PaintWhite", axis="z", depth=0.002)
            block_word(g, "AVE", (0.037, 3.100, 0.22), 0.08, "Lib_PaintWhite", axis="z", depth=0.002)
        a.end()
    a.box("Col_Base", (0, 0.005, 0), (0.12, 0.006, 0.12))
    a.capsule("Col_Pole", (0, 1.30, 0), 0.018, 2.30)
    # Tips of each sheet, past the legend and clear of the clamps.
    a.box("Col_Main", (-0.42, 2.855, 0.041), (0.04, 0.08, 0.0015))
    a.box("Col_Fifth", (0.041, 3.125, -0.34), (0.0015, 0.08, 0.04))
    return a


def _blade(g, axis, y, length, bev, bs):
    """One flanged sheet. axis 'z' faces the street. axis 'x' faces the cross street."""
    thick = 0.003
    tall = 0.229
    if axis == "z":
        g.box((0, y, 0.041), (length, tall, thick), "Lib_PaintWhite", bevel=bev, segs=bs)
        g.box((0, y, 0.0432), (length - 0.028, tall - 0.028, 0.0012), "Lib_SignGreen")
        g.box((0, y, 0.0388), (length - 0.028, tall - 0.028, 0.0012), "Lib_SignGreen")
        lip = 0.008
        g.box((0, y + tall * 0.5, 0.041), (length, 0.006, lip), "Lib_PaintWhite")
        g.box((0, y - tall * 0.5, 0.041), (length, 0.006, lip), "Lib_PaintWhite")
        g.box((length * 0.5, y, 0.041), (0.006, tall, lip), "Lib_PaintWhite")
        g.box((-length * 0.5, y, 0.041), (0.006, tall, lip), "Lib_PaintWhite")
    else:
        g.box((0.041, y, 0), (thick, tall, length), "Lib_PaintWhite", bevel=bev, segs=bs)
        g.box((0.0432, y, 0), (0.0012, tall - 0.028, length - 0.028), "Lib_SignGreen")
        g.box((0.0388, y, 0), (0.0012, tall - 0.028, length - 0.028), "Lib_SignGreen")
        lip = 0.008
        g.box((0.041, y + tall * 0.5, 0), (lip, 0.006, length), "Lib_PaintWhite")
        g.box((0.041, y - tall * 0.5, 0), (lip, 0.006, length), "Lib_PaintWhite")
        g.box((0.041, y, length * 0.5), (lip, tall, 0.006), "Lib_PaintWhite")
        g.box((0.041, y, -length * 0.5), (lip, tall, 0.006), "Lib_PaintWhite")
