"""Suburb house. Porch on +Z and a gable roof."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import register
from _volume import build_house

PROFILE = {
    "name": "House_Gable",
    "blurb": "Gable house. Body 7.6 x 6.8 m, walls 2.8 m, ridge about 4.6 m. Porch, steps, paneled door, trimmed windows, eave gutters, and a chimney. Porch faces +Z.",
    "sx": 7.6,
    "sz": 6.8,
    "porch": 1.8,
    "wall_h": 2.8,
    "rise": 1.7,
    "roof": "gable",
    "body": "Lib_Siding",
    "trim": "Lib_PaintWhite",
}


@register
def create():
    return build_house(PROFILE)()
