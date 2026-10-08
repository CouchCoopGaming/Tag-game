"""Pedestrian signal. Hand above, walking figure below, push button at 1.07 m.

No words. The head bottom is 2.36 m, about 7.7 ft. The button is 42 inches up.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _person(g, y, z, lod):
    """Walking figure. Blocks only, no type."""
    seg = lod_pick(lod, 8, 6)
    g.sphere((0.02, y + 0.10, z), 0.028, "Lib_SignalWhite", seg)
    g.box((0.0, y + 0.02, z), (0.045, 0.10, 0.012), "Lib_SignalWhite", euler=(0, 0, -8))
    g.box((-0.03, y - 0.06, z), (0.022, 0.09, 0.012), "Lib_SignalWhite", euler=(0, 0, 24))
    g.box((0.035, y - 0.07, z), (0.022, 0.10, 0.012), "Lib_SignalWhite", euler=(0, 0, -28))
    g.box((-0.04, y + 0.04, z), (0.07, 0.016, 0.010), "Lib_SignalWhite", euler=(0, 0, 20))


def _hand(g, y, z, lod):
    """Raised hand. Palm and four fingers, no legend."""
    g.box((0.0, y - 0.01, z), (0.09, 0.10, 0.012), "Lib_SignalAmber")
    for i, x in enumerate((-0.036, -0.012, 0.012, 0.036)):
        g.box((x, y + 0.09, z), (0.016, 0.07, 0.010), "Lib_SignalAmber")
    g.box((-0.055, y + 0.03, z), (0.04, 0.016, 0.010), "Lib_SignalAmber", euler=(0, 0, 40))


def _button(g, lod):
    seg = lod_pick(lod, 10, 6)
    bev = lod_pick(lod, 0.002, 0.0)
    bs = 1 if lod == 0 else 0
    g.box((0.0, 1.07, 0.12), (0.12, 0.28, 0.10), "Lib_Black", bevel=bev, segs=bs)
    g.cylinder((0.0, 1.10, 0.178), 0.032, 0.012, "Lib_PaintYellow", 12, axis="Z")
    g.cylinder((0.0, 1.10, 0.186), 0.022, 0.010, "Lib_Black", 12, axis="Z")
    g.box((0.10, 1.12, 0.14), (0.06, 0.12, 0.04), "Lib_Black")
    g.box((0.13, 1.12, 0.11), (0.055, 0.12, 0.012), "Lib_PaintYellow", bevel=bev, segs=bs)
    if lod == 0:
        g.box((0.125, 1.12, 0.120), (0.028, 0.008, 0.006), "Lib_Black")
        g.box((0.138, 1.132, 0.120), (0.018, 0.008, 0.006), "Lib_Black", euler=(0, 0, 36))
        g.box((0.138, 1.108, 0.120), (0.018, 0.008, 0.006), "Lib_Black", euler=(0, 0, -36))


@register
def create():
    a = Asset(
        "PedSignal_Crosswalk",
        "StreetFurniture",
        "Crosswalk signal. Hand and walking figure, head bottom 2.36 m, push button at 1.07 m. No words.",
    )
    a.climb_note = "100 mm pole. Not a cling."
    a.vault_note = "Too thin to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.018, 0), (0.28, 0.036, 0.28), "Lib_SteelDark", bevel=bev, segs=bs)
        g.cylinder((0, 1.65, 0), 0.050, 3.22, "Lib_Steel", seg)
        g.sphere((0, 3.28, 0), 0.055, "Lib_Steel", seg)
        # Bracket and the two-section head. Face is +Z.
        g.box((0, 2.75, 0.08), (0.08, 0.16, 0.08), "Lib_SteelDark")
        g.box((0, 2.75, 0.16), (0.42, 0.78, 0.16), "Lib_Black", bevel=bev, segs=bs)
        g.box((0, 2.75, 0.245), (0.34, 0.04, 0.012), "Lib_SteelDark")
        _hand(g, 2.98, 0.252, lod)
        _person(g, 2.52, 0.252, lod)
        _button(g, lod)
        a.end()
    a.box("Col_Base", (0, 0.014, 0), (0.20, 0.020, 0.20))
    a.capsule("Col_Pole", (0, 1.15, 0), 0.034, 2.10, direction=1)
    a.box("Col_Head", (0, 2.75, 0.15), (0.28, 0.58, 0.08))
    # Core of the housing, clear of the pole, the neck, and the button discs.
    a.box("Col_Button", (0, 1.07, 0.11), (0.06, 0.16, 0.04))
    return a
