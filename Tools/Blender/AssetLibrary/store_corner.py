"""Corner shop. Two glazed street faces, a parapet roof, and an alley behind."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import register
from _volume import build_store

PROFILE = {
    "name": "Store_Corner",
    "blurb": "Corner shop, 8.0 x 7.2 m, two stories to a 6.4 m parapet. Glazing on +Z and +X. Alley face is -Z, with a fire escape and downspouts. Awning and MARKET sign on the front.",
    "sx": 8.0,
    "sz": 7.2,
    "wall_h": 6.4,
    "body": "Lib_Brick",
    "trim": "Lib_PaintCream",
    "awning": "Lib_Awning",
    "accent": "Lib_PaintRed",
    "sign": "MARKET",
    "front": "shop",
    "right": "shop",
    "left": "plain",
    "back": "alley",
}


@register
def create():
    return build_store(PROFILE)()
