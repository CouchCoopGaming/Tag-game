"""Sidewalk newsstand. Green booth, service hatch, side racks. No masthead.

About 1.83 m wide, 1.22 m deep, roof at 2.17 m. The hatch and the counter
face +Z. Magazine covers are blank color blocks.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

W = 1.80
D = 1.20
WALL = 2.10
FRONT = D * 0.5
SIDE = W * 0.5

COVERS = (
    "Lib_PaintCream",
    "Lib_PaintWhite",
    "Lib_PaintRed",
    "Lib_BoxBlue",
    "Lib_PaintTeal",
    "Lib_PaintCream",
)


def _shell(g, lod):
    bev = lod_pick(lod, 0.004, 0.0)
    bs = 1 if lod == 0 else 0
    g.box((0, WALL * 0.5, 0), (W, WALL, D), "Lib_PaintGreen", bevel=bev, segs=bs)
    # Roof sits on a gap above the walls and overhangs on every side.
    g.box((0, 2.140, 0), (W + 0.18, 0.064, D + 0.20), "Lib_BoxGreen", bevel=bev, segs=bs)
    # Fascia hangs in the overhang, clear of the wall and of the roof slab.
    g.box((0, 2.04, FRONT + 0.06), (W + 0.10, 0.10, 0.018), "Lib_BoxGreen")
    g.box((0, 2.04, -(FRONT + 0.06)), (W + 0.10, 0.10, 0.018), "Lib_BoxGreen")
    g.box((SIDE + 0.045, 2.04, 0), (0.018, 0.10, D + 0.08), "Lib_BoxGreen")
    g.box((-(SIDE + 0.045), 2.04, 0), (0.018, 0.10, D + 0.08), "Lib_BoxGreen")


def _hatch(g, lod):
    """Dark service opening, steel frame, counter, and a flat visor."""
    z = FRONT + 0.012
    g.box((0, 1.46, z), (1.16, 0.58, 0.008), "Lib_Black")
    bar_z = z + 0.016
    g.box((0, 1.772, bar_z), (1.26, 0.022, 0.014), "Lib_Steel")
    g.box((0, 1.148, bar_z), (1.26, 0.022, 0.014), "Lib_Steel")
    g.box((-0.604, 1.46, bar_z), (0.022, 0.56, 0.014), "Lib_Steel")
    g.box((0.604, 1.46, bar_z), (0.022, 0.56, 0.014), "Lib_Steel")
    # Counter starts just past the frame. Top is 1.12 m.
    g.box((0, 1.102, FRONT + 0.18), (1.32, 0.036, 0.30), "Lib_Steel")
    if lod == 0:
        for x in (-0.46, 0.46):
            g.box((x, 0.96, FRONT + 0.16), (0.014, 0.22, 0.16), "Lib_SteelDark")
        # Stacks sit on the outer counter. Sheets stand at the hatch, facing the street.
        # Outer counter is past the visor, so the stacks sit in the light.
        g.box((0.22, 1.146, FRONT + 0.26), (0.20, 0.040, 0.10), "Lib_PaintCream")
        g.box((0.46, 1.140, FRONT + 0.25), (0.16, 0.028, 0.09), "Lib_PaintWhite")
        g.box((-0.28, 1.270, FRONT + 0.27), (0.14, 0.28, 0.008), "Lib_PaintCream")
        g.box((-0.08, 1.250, FRONT + 0.28), (0.12, 0.24, 0.008), "Lib_PaintWhite")
        g.box((0.04, 1.230, FRONT + 0.27), (0.08, 0.20, 0.008), "Lib_PaintRed")
    # Blank covers flank the hatch on the front face.
    g.box((-0.76, 1.52, FRONT + 0.012), (0.20, 0.36, 0.010), "Lib_PaintRed")
    g.box((-0.76, 1.16, FRONT + 0.014), (0.20, 0.14, 0.008), "Lib_BoxBlue")
    g.box((0.76, 1.52, FRONT + 0.012), (0.20, 0.36, 0.010), "Lib_PaintCream")
    g.box((0.76, 1.16, FRONT + 0.014), (0.20, 0.14, 0.008), "Lib_PaintTeal")
    # Visor shades only the top of the hatch. The counter lip stays in the sun.
    g.box((0, 1.88, FRONT + 0.14), (1.36, 0.016, 0.16), "Lib_PaintGreen")


def _door(g, lod):
    z = -(FRONT + 0.014)
    g.box((0.10, 1.00, z), (0.64, 1.72, 0.012), "Lib_BoxGreen")
    if lod == 0:
        g.cylinder((-0.10, 1.02, z - 0.016), 0.008, 0.18, "Lib_Steel", 8, axis="Y")
        g.cylinder((-0.10, 0.86, z - 0.014), 0.012, 0.010, "Lib_Brass", 8, axis="Z")
        for y in (0.40, 1.60):
            g.cylinder((0.44, y, z - 0.010), 0.010, 0.05, "Lib_SteelDark", 8, axis="Y")


def _racks(g, lod):
    rows = (0.50, 1.04, 1.58) if lod == 0 else (1.05,)
    cols = (-0.22, 0.22) if lod == 0 else (0.0,)
    cover_h = 0.34 if lod == 0 else 1.10
    cover_d = 0.28 if lod == 0 else 0.72
    n = 0
    for sign in (1.0, -1.0):
        x = sign * (SIDE + 0.012)
        for row in rows:
            if lod == 0:
                g.box((sign * (SIDE + 0.030), row - 0.20, 0), (0.036, 0.014, 0.86), "Lib_SteelDark")
            for col in cols:
                mat = COVERS[n % len(COVERS)]
                n += 1
                g.box((x, row, col), (0.010, cover_h, cover_d), mat)
                if lod == 0:
                    g.box((sign * (SIDE + 0.022), row + 0.13, col), (0.006, 0.036, cover_d - 0.04), "Lib_SteelDark")


def _kick(g):
    g.box((0, 0.07, FRONT + 0.010), (W - 0.08, 0.14, 0.010), "Lib_SteelDark")


@register
def create():
    a = Asset(
        "Newsstand_Corner",
        "StreetFurniture",
        "Newsstand, 1.80 m wide, roof 2.17 m. Service hatch, counter, blank side racks. No masthead.",
    )
    a.climb_note = "Roof is the stand."
    a.vault_note = "Counter lip is 1.12 m."
    for lod in (0, 1):
        g = a.begin(lod)
        _shell(g, lod)
        _hatch(g, lod)
        _door(g, lod)
        _racks(g, lod)
        _kick(g)
        a.end()
    # Wall collider stays inside the green box. Roof and counter are the stands.
    a.box("Col_Body", (0, 1.05, 0), (1.72, 2.02, 1.12))
    a.box("Col_Roof", (0, 2.140, 0), (1.84, 0.048, 1.24))
    a.box("Col_Counter", (0, 1.100, FRONT + 0.17), (1.16, 0.024, 0.20))
    return a
