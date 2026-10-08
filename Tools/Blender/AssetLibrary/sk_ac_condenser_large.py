"""Larger ground condenser. About a 5-ton cabinet: 0.96 m square, 0.92 m tall, two fans."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import register
from sk_ac_condenser import _asset


@register
def create():
    return _asset(
        "AC_Condenser_Large",
        "Ground condenser, 0.96 m square, 0.92 m tall, two top fans, on a 40 mm pad.",
        0.96, 0.92, 2,
    )
