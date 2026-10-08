"""Suburb house. Porch on +Z and a hip roof."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import register
from _volume import build_house

PROFILE = {
    "name": "House_Hip",
    "blurb": "Hip-roof house. Body 8.4 x 7.2 m, walls 2.9 m, hip about 4.5 m, porch on +Z. Cream walls.",
    "sx": 8.4,
    "sz": 7.2,
    "porch": 1.7,
    "wall_h": 2.9,
    "rise": 1.55,
    "roof": "hip",
    "body": "Lib_PaintCream",
    "trim": "Lib_PaintWhite",
}


@register
def create():
    return build_house(PROFILE)()
