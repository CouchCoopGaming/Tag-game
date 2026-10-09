"""Diner. Cream walls, a red awning, and a parapet roof over one tall story."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import register
from _volume import build_store

PROFILE = {
    "name": "Store_Diner",
    "blurb": "Diner, 10.0 x 7.0 m, walls 4.6 m under a parapet. Cream body, a long mullioned DINER front with a red rail, recessed door, sign band, brick string course, and a sloped red awning. Alley face is -Z.",
    "sx": 10.0,
    "sz": 7.0,
    "wall_h": 4.6,
    "body": "Lib_PaintCream",
    "trim": "Lib_PaintRed",
    "awning": "Lib_PaintRed",
    "accent": "Lib_PaintRed",
    "sign": "DINER",
    "front": "shop_diner",
    "right": "plain",
    "left": "plain",
    "back": "alley",
}


@register
def create():
    return build_store(PROFILE)()
