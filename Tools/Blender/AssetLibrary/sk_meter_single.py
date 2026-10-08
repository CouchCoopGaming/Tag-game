"""Single-space digital parking meter. Generic housing, no brand."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

_FONT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "..", "StrafeJumpSim", "Fonts", "Overpass-Bold.ttf",
))


def meter_head(g, lod, x, font):
    """One digital head. x is the housing center. Front is +Z.

    The plate is not part of the head. The post must sleeve the housing.
    """
    seg = lod_pick(lod, 16, 8)
    bev = lod_pick(lod, 0.008, 0.0)
    bs = 1 if lod == 0 else 0
    # Bevelled housing. Front face lands at z = 0.055.
    g.box((x, 1.15, 0.0), (0.16, 0.28, 0.11), "Lib_Black", bevel=bev, segs=bs)
    # Dome sits on the roof. The sphere center is below the roof so only the cap shows.
    g.sphere((x, 1.255, -0.006), 0.080, "Lib_Black", seg)
    # Brow over the screen.
    g.box((x, 1.255, 0.028), (0.150, 0.014, 0.070), "Lib_Black", euler=(16, 0, 0))
    # Housing front is z=0.055. The screen has to sit in front of that face
    # or the solid housing hides it. The bezel stands proud of the screen.
    g.box((x, 1.198, 0.062), (0.100, 0.072, 0.010), "Lib_Black")
    g.box((x, 1.240, 0.072), (0.124, 0.010, 0.014), "Lib_Steel")
    g.box((x, 1.156, 0.072), (0.124, 0.010, 0.014), "Lib_Steel")
    g.box((x - 0.057, 1.198, 0.072), (0.010, 0.074, 0.014), "Lib_Steel")
    g.box((x + 0.057, 1.198, 0.072), (0.010, 0.074, 0.014), "Lib_Steel")
    # Card slot, proud surround with a black recess.
    g.box((x, 1.108, 0.066), (0.092, 0.026, 0.016), "Lib_Steel")
    g.box((x, 1.108, 0.076), (0.064, 0.008, 0.008), "Lib_Black")
    for row, y in enumerate((1.052, 1.074)):
        for col, dx in enumerate((-0.030, 0.0, 0.030)):
            mat = "Lib_SignalGreen" if row == 1 and col == 2 else "Lib_Steel"
            g.cylinder((x + dx, y, 0.070), 0.009, 0.022, mat, 8, axis="Z")
    if lod == 0:
        # Angled solar panel on a riser. The blue face tips toward the front.
        g.box((x, 1.328, 0.0), (0.024, 0.040, 0.024), "Lib_SteelDark")
        # Negative pitch tips the cell face up and toward the front.
        g.box((x, 1.350, 0.012), (0.116, 0.012, 0.078), "Lib_PaintBlue", euler=(-42, 0, 0))
        g.box((x, 1.356, 0.016), (0.008, 0.006, 0.054), "Lib_PaintWhite", euler=(-42, 0, 0))
        g.text("2:00", (x, 1.200, 0.072), 0.046, "Lib_SignalGreen", extrude=0.003, font=font)


def meter_foot(g, lod, width, depth):
    """Plate on the ground. The post is added by the caller and sleeves this plate."""
    g.box((0.0, 0.014, 0.0), (width, 0.028, depth), "Lib_SteelDark")
    if lod != 0:
        return
    for i in range(4):
        ang = (i + 0.5) * math.pi * 0.5
        g.cylinder(
            (math.sin(ang) * (width * 0.32), 0.030, math.cos(ang) * (depth * 0.32)),
            0.006, 0.010, "Lib_Steel", 6,
        )


@register
def create():
    a = Asset(
        "ParkingMeter_Single",
        "StreetFurniture",
        "Digital single-space meter. Plate on the ground, screen center 1.21 m, head 0.15 m wide. No brand.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        meter_foot(g, lod, 0.20, 0.18)
        # Bottom is inside the plate. Top sleeves the head and stops below the screen.
        g.cylinder((0.0, 0.545, 0.0), 0.030, 1.07, "Lib_Steel", seg)
        g.cylinder((0.0, 0.034, 0.0), 0.044, 0.016, "Lib_SteelDark", seg)
        meter_head(g, lod, 0.0, _FONT)
        a.end()
    a.box("Col_Base", (0.0, 0.014, 0.0), (0.12, 0.018, 0.10))
    a.capsule("Col_Post", (0.0, 0.50, 0.0), 0.020, 0.80, 1)
    # Above the post and below the dome so the sample stays in one solid.
    a.box("Col_Head", (0.0, 1.125, 0.0), (0.06, 0.06, 0.04))
    return a
