"""ISO 40-foot shipping container. 12.19 x 2.44 x 2.59 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from container_20 import build_container
from _common import register


@register
def create():
    return build_container(
        "Container_40",
        12.19,
        "Lib_BoxBlue",
        "40-foot container, blue enamel. 12.19 x 2.44 x 2.59 m. Same corrugation, castings, and door bars as the 20-foot box.",
    )
