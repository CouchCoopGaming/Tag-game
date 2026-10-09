"""Sidewalk pipe rail. Top grip at 0.91 m, returns at both ends.

Posts are 1.80 m apart. No legend.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import sweep_tube


POSTS = (-0.90, 0.90)


def _top_rail(g, segments):
    # Vertical first and last segments so the end caps stay horizontal.
    sweep_tube(
        g,
        [
            (-1.12, 0.64, 0.0),
            (-1.12, 0.84, 0.0),
            (-1.08, 0.88, 0.0),
            (-1.02, 0.895, 0.0),
            (1.02, 0.895, 0.0),
            (1.08, 0.88, 0.0),
            (1.12, 0.84, 0.0),
            (1.12, 0.64, 0.0),
        ],
        0.019,
        "Lib_Steel",
        segments,
    )


@register
def create():
    a = Asset(
        "Rail_Sidewalk",
        "StreetFurniture",
        "Pipe rail, grip top at 0.91 m, posts 1.80 m apart, returned ends.",
    )
    a.climb_note = "38 mm grip. A handhold, not a flat cling."
    a.vault_note = "Top is 0.91 m. The rail is round."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        _top_rail(g, seg)
        g.cylinder((0, 0.46, 0.055), 0.013, 1.72, "Lib_Steel", seg, axis="X")
        for x in POSTS:
            g.box((x, 0.010, 0), (0.16, 0.020, 0.16), "Lib_SteelDark")
            g.box((x, 0.445, 0), (0.048, 0.830, 0.048), "Lib_Steel")
            # Collar and clamp stop short of the tubes so the solids stay apart.
            g.cylinder((x, 0.868, 0), 0.016, 0.012, "Lib_SteelDark", seg)
            g.box((x, 0.46, 0.033), (0.028, 0.028, 0.010), "Lib_SteelDark")
            if lod == 0:
                for sx in (-0.05, 0.05):
                    for sz in (-0.05, 0.05):
                        g.cylinder((x + sx, 0.024, sz), 0.007, 0.006, "Lib_Steel", 6)
        a.end()
    for i, x in enumerate(POSTS):
        a.box("Col_Flange_%d" % i, (x, 0.008, 0), (0.12, 0.014, 0.12))
        a.box("Col_Post_%d" % i, (x, 0.445, 0), (0.036, 0.78, 0.036))
    a.box("Col_Top", (0, 0.895, 0), (1.96, 0.026, 0.026))
    a.box("Col_ReturnL", (-1.12, 0.74, 0), (0.026, 0.16, 0.026))
    a.box("Col_ReturnR", (1.12, 0.74, 0), (0.026, 0.16, 0.026))
    a.box("Col_Low", (0, 0.46, 0.055), (1.64, 0.018, 0.018))
    return a
