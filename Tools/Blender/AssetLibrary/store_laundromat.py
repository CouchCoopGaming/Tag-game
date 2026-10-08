"""Laundromat. White walls, a teal awning, and a row of lit windows."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import register
from _volume import build_store

PROFILE = {
    "name": "Store_Laundromat",
    "blurb": "Laundromat, 9.2 x 6.8 m, walls 4.8 m under a parapet. White body, teal awning and WASH sign. Alley face is -Z.",
    "sx": 9.2,
    "sz": 6.8,
    "wall_h": 4.8,
    "body": "Lib_PaintWhite",
    "trim": "Lib_PaintTeal",
    "awning": "Lib_PaintTeal",
    "accent": "Lib_PaintTeal",
    "sign": "WASH",
    "front": "shop",
    "right": "plain",
    "left": "shop",
    "back": "alley",
}


@register
def create():
    return build_store(PROFILE)()
