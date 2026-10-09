"""Blue 40 ft low-floor city bus with a white skirt. No badges."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from _common import register
import bus


@register
def create():
    return bus.create_city40(
        "Bus_City40_Blue",
        "Lib_PaintBlue",
        "Lib_PaintWhite",
        "Low-floor city bus, 12.50 m over bumpers, 2.59 m wide, roof unit 3.20 m. Blue body, white skirt, curb-side doors.",
    )
