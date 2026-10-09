"""Cream 40 ft low-floor city bus with a blue skirt. No badges."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from _common import register
import bus


@register
def create():
    return bus.create_city40(
        "Bus_City40",
        "Lib_PaintCream",
        "Lib_PaintBlue",
        "Low-floor city bus, 12.50 m over bumpers, 2.59 m wide, roof unit 3.20 m. Cream body, blue skirt, curb-side doors.",
    )
