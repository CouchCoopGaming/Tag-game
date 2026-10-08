"""Cream articulated low-floor city bus. No badges."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from _common import register
import bus


@register
def create():
    return bus.create_city60(
        "Bus_City60",
        "Lib_PaintCream",
        "Lib_PaintBlue",
        "Articulated low-floor city bus, 18.54 m over bumpers, 2.59 m wide, roof unit 3.20 m. Three axles, turntable cover, curb-side doors.",
    )
