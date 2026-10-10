"""Pedestrian signal. Open-palm hand above a walking figure, push button at 1.07 m.

The symbols are flat lit stencils. The pole ends just above the head. No words.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

HEAD_Y = 2.75
HEAD_TOP = HEAD_Y + 0.39
# Pole stops a few centimetres above the head. No finial past that.
POLE_TOP = HEAD_TOP + 0.06


def _stencil(g, parts, z, mat):
    """Thin plates on the lamp face. parts are (center xy, size xy, z-rot deg)."""
    for (x, y), (sx, sy), rot in parts:
        g.box((x, y, z), (sx, sy, 0.006), mat, euler=(0, 0, rot))


def _hand(g, z):
    """Open palm, fingers up, thumb out to the left. One flat amber stencil."""
    y = 2.98
    parts = [
        ((0.012, y - 0.02), (0.100, 0.105), 0.0),
    ]
    # Four separated fingers. Gaps stay wide enough to read at the still size.
    span = (-0.042, -0.014, 0.014, 0.042)
    for x in span:
        parts.append(((x, y + 0.095), (0.016, 0.100), 0.0))
    parts.append(((-0.072, y + 0.012), (0.020, 0.078), 58.0))
    _stencil(g, parts, z, "Lib_SignalAmber")


def _person(g, z, lod):
    """Walking figure. Disc head, stride, one arm forward."""
    seg = lod_pick(lod, 12, 8)
    y = 2.50
    g.cylinder((0.01, y + 0.115, z), 0.026, 0.006, "Lib_SignalWhite", seg, axis="Z")
    _stencil(g, [
        ((0.0, y + 0.035), (0.046, 0.100), -6.0),
        ((-0.028, y - 0.055), (0.018, 0.105), 32.0),
        ((0.034, y - 0.060), (0.018, 0.115), -36.0),
        ((-0.045, y + 0.045), (0.016, 0.085), 48.0),
        ((0.040, y + 0.020), (0.014, 0.070), -40.0),
    ], z, "Lib_SignalWhite")


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
        "Crosswalk signal. Open-palm hand and a walking figure, pole ends just above the head, push button at 1.07 m.",
    )
    a.climb_note = "100 mm pole. Not a cling."
    a.vault_note = "Too thin to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.018, 0), (0.28, 0.036, 0.28), "Lib_SteelDark", bevel=bev, segs=bs)
        g.cylinder((0, POLE_TOP * 0.5, 0), 0.050, POLE_TOP, "Lib_Steel", seg)
        g.cylinder((0, POLE_TOP + 0.012, 0), 0.055, 0.024, "Lib_SteelDark", seg)
        g.box((0, HEAD_Y, 0.08), (0.08, 0.16, 0.08), "Lib_SteelDark")
        g.box((0, HEAD_Y, 0.16), (0.42, 0.78, 0.16), "Lib_Black", bevel=bev, segs=bs)
        g.box((0, HEAD_Y, 0.245), (0.34, 0.018, 0.008), "Lib_SteelDark")
        _hand(g, 0.246)
        _person(g, 0.246, lod)
        _button(g, lod)
        a.end()
    a.box("Col_Base", (0, 0.014, 0), (0.20, 0.020, 0.20))
    a.capsule("Col_Pole", (0, 1.10, 0), 0.034, 2.00, direction=1)
    a.box("Col_Head", (0, HEAD_Y, 0.15), (0.28, 0.58, 0.08))
    a.box("Col_Button", (0, 1.07, 0.11), (0.06, 0.16, 0.04))
    return a
