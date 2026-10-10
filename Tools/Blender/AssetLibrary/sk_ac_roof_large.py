"""Larger rooftop condenser. 1.50 x 1.05 m, 0.78 m tall, two fans, on rails."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import register
from sk_ac_roof import _asset


@register
def create():
    return _asset(
        "AC_Roof_Large",
        "Rooftop condenser 1.50 x 1.05 m, 0.78 m tall on 80 mm rails, two fans.",
        1.50, 1.05, 0.78, 2,
    )
