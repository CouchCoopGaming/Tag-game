"""Sidewalk chalkboard A-frame.

Wood stiles are the legs. Each leg ends in a rubber cap.
A barrel hinge joins the top rails. A chain on each side keeps the stance.
OPEN and a short generic menu are Overpass Bold (OFL) on both faces.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import chain

_FONT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "..", "StrafeJumpSim", "Fonts", "Overpass-Bold.ttf",
))

ANG = 21.0
_C = math.cos(math.radians(ANG))
_S = math.sin(math.radians(ANG))
# Front board centerline. The back board is the mirror through Z.
_CY = 0.430
_CZ = 0.176
# Front euler is -ANG: local +Y climbs toward the hinge, local +Z faces the street.
_UY = (0.0, _C, -_S)
_UZ = (0.0, _S, _C)


def _front(along, out=0.0, x=0.0):
    return (
        x + _UY[0] * along + _UZ[0] * out,
        _CY + _UY[1] * along + _UZ[1] * out,
        _CZ + _UY[2] * along + _UZ[2] * out,
    )


def _back(along, out=0.0, x=0.0):
    p = _front(along, out, x)
    return (p[0], p[1], -p[2])


@register
def create():
    a = Asset(
        "Sign_AFrame",
        "StreetFurniture",
        "Chalkboard A-frame 0.58 m wide and 0.88 m tall. Hinge, spreader chains, rubber caps on the legs. OPEN on both faces.",
    )
    a.climb_note = "Smooth board. Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.002, 0.0)
        bs = 1 if lod == 0 else 0
        for x in (-0.26, 0.26):
            g.box(_front(0.0, 0.0, x), (0.040, 0.90, 0.036), "Lib_Wood", bevel=bev, segs=bs, euler=(-ANG, 0, 0))
            g.box(_back(0.0, 0.0, x), (0.040, 0.90, 0.036), "Lib_Wood", bevel=bev, segs=bs, euler=(ANG, 0, 0))
            # Rubber cap sleeved on the leg end, not a separate shoe.
            g.box(_front(-0.43, 0.0, x), (0.050, 0.055, 0.046), "Lib_Black", euler=(-ANG, 0, 0))
            g.box(_back(-0.43, 0.0, x), (0.050, 0.055, 0.046), "Lib_Black", euler=(ANG, 0, 0))
        g.box(_front(0.40, 0.006), (0.58, 0.048, 0.064), "Lib_Wood", bevel=bev, segs=bs, euler=(-ANG, 0, 0))
        g.box(_back(0.40, 0.006), (0.58, 0.048, 0.064), "Lib_Wood", bevel=bev, segs=bs, euler=(ANG, 0, 0))
        g.box(_front(-0.32, 0.004), (0.58, 0.042, 0.040), "Lib_Wood", bevel=bev, segs=bs, euler=(-ANG, 0, 0))
        g.box(_back(-0.32, 0.004), (0.58, 0.042, 0.040), "Lib_Wood", bevel=bev, segs=bs, euler=(ANG, 0, 0))
        g.box(_front(0.04, 0.020), (0.48, 0.70, 0.016), "Lib_Black", euler=(-ANG, 0, 0))
        g.box(_back(0.04, 0.020), (0.48, 0.70, 0.016), "Lib_Black", euler=(ANG, 0, 0))
        hinge = _front(0.40, 0.0)
        g.cylinder((0.0, hinge[1], 0.0), 0.015, 0.52, "Lib_SteelDark", 8, axis="X")
        if lod == 0:
            for x in (-0.16, 0.0, 0.16):
                g.cylinder((x, hinge[1], 0.0), 0.022, 0.030, "Lib_Steel", 8, axis="X")
            chain(g, (0.26, 0.28, 0.234), (0.26, 0.28, -0.234), n=6, sag=0.045, radius=0.004)
            chain(g, (-0.26, 0.28, 0.234), (-0.26, 0.28, -0.234), n=6, sag=0.045, radius=0.004)
            for along, body, size in (
                (0.20, "OPEN", 0.078),
                (0.05, "Coffee", 0.040),
                (-0.07, "Tea", 0.040),
                (-0.19, "Pastry", 0.040),
            ):
                g.text(body, _front(along, 0.031), size, "Lib_PaintCream", extrude=0.003, pitch=-ANG, font=_FONT)
                g.text(
                    body, _back(along, 0.031), size, "Lib_PaintCream",
                    extrude=0.003, yaw=180.0, pitch=-ANG, font=_FONT,
                )
            g.box(_front(0.12, 0.031), (0.30, 0.006, 0.004), "Lib_PaintCream", euler=(-ANG, 0, 0))
            g.box(_back(0.12, 0.031), (0.30, 0.006, 0.004), "Lib_PaintCream", euler=(ANG, 0, 0))
        a.end()
    # Inner half of each chalkboard, clear of the stiles, the legend, and the chains.
    a.box("Col_Front", _front(0.02, 0.020), (0.24, 0.32, 0.010), euler=(-ANG, 0, 0))
    a.box("Col_Back", _back(0.02, 0.020), (0.24, 0.32, 0.010), euler=(ANG, 0, 0))
    return a
