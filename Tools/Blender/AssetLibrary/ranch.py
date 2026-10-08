"""Single-story ranch. Hip roof, attached garage, porch on +Z.

Footprint 10.8 x 7.2 m, plate at 2.50 m, ridge about 3.70 m.
The garage is the +X 3.6 m. The porch is in front of the living wing only.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from _volume import _apply_cols, _hip, _wall

SX = 10.8
SZ = 7.2
PLATE = 2.50
RISE = 1.20
ROOF_X = SX * 0.5 + 0.38
ROOF_Z = SZ * 0.5 + 0.32


def _front_holes():
    """Along X. Garage door on +X, entry and two windows on the living wing."""
    return [
        (-3.55, 1.58, 1.10, 1.22, False, False),
        (-1.85, 1.58, 1.10, 1.22, False, False),
        (-0.15, 1.06, 0.90, 2.02, False, "solid"),
        (3.55, 1.115, 2.44, 2.13, False, "opening"),
    ]


def _side_holes(right):
    if right:
        return [(-0.7, 1.62, 0.86, 0.52, False, False)]
    return [
        (-1.55, 1.55, 0.96, 1.16, False, False),
        (1.55, 1.55, 0.96, 1.16, False, False),
    ]


def _back_holes():
    return [
        (-2.6, 1.55, 1.05, 1.16, False, False),
        (2.4, 1.55, 1.05, 1.16, True, False),
    ]


def _garage_door(g, lod):
    """Closed overhead door, inset in the opening so it does not meet the jambs."""
    panels = lod_pick(lod, 4, 2, 1)
    top, bot = 2.10, 0.10
    span = top - bot
    for i in range(panels):
        y = bot + (i + 0.5) * span / panels
        h = span / panels * 0.86
        g.box((3.55, y, 3.52), (2.28, h, 0.040), "Lib_PaintWhite")
    if lod == 0:
        g.box((4.35, 1.05, 3.55), (0.04, 0.10, 0.018), "Lib_Black")
        g.box((3.55, 2.26, 3.66), (2.70, 0.07, 0.035), "Lib_PaintWhite")


def _porch(g, cols, lod):
    """Deck, roof, posts, and a rail with a gap at the steps. Living wing only."""
    # Wall outer face is z=3.60. 8 mm of air, then the deck.
    deck_z0, deck_z1 = 3.608, 5.05
    deck_x0, deck_x1 = -4.55, 0.85
    cx = (deck_x0 + deck_x1) * 0.5
    cz = (deck_z0 + deck_z1) * 0.5
    g.box((cx, 0.14, cz), (deck_x1 - deck_x0, 0.10, deck_z1 - deck_z0), "Lib_Wood", uv_scale=1.1)
    cols.append(("box", "Col_Porch", (cx, 0.14, cz), (deck_x1 - deck_x0 - 0.08, 0.06, deck_z1 - deck_z0 - 0.08)))
    # Wall outer face is z=3.60. The roof starts 2 cm past it so the two shells do not meet.
    # Overhang is on the street side only.
    roof_z0, roof_z1 = 3.62, deck_z1 + 0.08
    roof_x0, roof_x1 = deck_x0 - 0.10, deck_x1 + 0.10
    rx = (roof_x0 + roof_x1) * 0.5
    rz = (roof_z0 + roof_z1) * 0.5
    roof_y = 2.36
    g.box((rx, roof_y, rz), (roof_x1 - roof_x0, 0.07, roof_z1 - roof_z0), "Lib_Roof")
    cols.append((
        "box", "Col_PorchRoof", (rx, roof_y, rz),
        (roof_x1 - roof_x0 - 0.16, 0.04, roof_z1 - roof_z0 - 0.16),
    ))
    posts = ((deck_x0 + 0.10, deck_z1 - 0.10), (deck_x1 - 0.10, deck_z1 - 0.10), (-0.85, deck_z1 - 0.10))
    for x, z in posts:
        g.box((x, 1.26, z), (0.12, 2.08, 0.12), "Lib_PaintWhite")
    if lod < 2:
        # Rail top is 0.95 m above the deck top (deck top 0.19, rail top 1.14).
        y = 1.115
        z = deck_z1 - 0.08
        left0, left1 = deck_x0 + 0.20, -0.95
        right0, right1 = -0.15, deck_x1 - 0.20
        g.pipe((left0, y, z), (left1, y, z), 0.028, "Lib_PaintWhite", 6)
        g.pipe((right0, y, z), (right1, y, z), 0.028, "Lib_PaintWhite", 6)
        g.pipe((deck_x1 - 0.08, y, 3.75), (deck_x1 - 0.08, y, deck_z1 - 0.18), 0.028, "Lib_PaintWhite", 6)
        if lod == 0:
            # One capsule per rail, short of the open step gap.
            cols.append(("cap", "Vault_PorchRail", ((left0 + left1) * 0.5, y, z), 0.016, (left1 - left0) - 0.20, 0))
            cols.append(("cap", "Vault_PorchRailR", ((right0 + right1) * 0.5, y, z), 0.016, (right1 - right0) - 0.20, 0))


def _steps(g, cols):
    g.box((-0.15, 0.10, 5.28), (1.15, 0.08, 0.32), "Lib_Concrete", uv_scale=0.7)
    g.box((-0.15, 0.035, 5.62), (1.35, 0.05, 0.32), "Lib_Concrete", uv_scale=0.7)
    cols.append(("box", "Col_StepHigh", (-0.15, 0.10, 5.28), (1.05, 0.05, 0.24)))
    cols.append(("box", "Col_StepLow", (-0.15, 0.035, 5.62), (1.25, 0.03, 0.24)))
    g.box((3.55, 0.02, 4.55), (3.10, 0.04, 1.70), "Lib_Concrete", uv_scale=0.6)
    cols.append(("box", "Col_Apron", (3.55, 0.02, 4.55), (2.90, 0.028, 1.50)))


def _roof_landing(asset, name, z0, y0, z1, y1, width):
    """A box under the shingle, inside the hip, on the long slope."""
    dz, dy = (z1 - z0), (y1 - y0)
    length = math.hypot(dz, dy) or 1.0
    nz, ny = -dy / length, dz / length
    if ny < 0:
        nz, ny = -nz, -ny
    thick = 0.24
    cz = (z0 + z1) * 0.5 - nz * thick * 0.42
    cy = (y0 + y1) * 0.5 - ny * thick * 0.42
    angle = math.degrees(math.atan2(abs(dy), abs(dz)))
    pitch = -angle if dz * dy > 0 else angle
    asset.box(name, (0.0, cy, cz), (width * 0.62, thick * 0.36, length * 0.50), euler=(pitch, 0.0, 0.0))


@register
def create():
    a = Asset(
        "Ranch_House",
        "Buildings",
        "Single-story ranch, 10.8 x 7.2 m, plate 2.50 m, hip ridge about 3.70 m. "
        "Attached single garage on +X with a closed overhead door. "
        "Porch and steps on the living wing, +Z. Siding walls are cling. "
        "The long hip slopes are landings. The porch rail is 0.95 m above the deck.",
    )
    a.loose_pivot = True
    a.climbable = True
    a.vaultable = True
    a.vault_height = 0.95
    a.climb_note = "Siding walls are cling. Glass and the garage door are solid. The porch is open on +Z."
    a.vault_note = "Porch rail is 0.95 m above the porch deck (deck top 0.19 m, rail top 1.14 m)."
    hx, hz = SX * 0.5, SZ * 0.5
    span_x = SX - 0.44 - 0.012
    span_z = SZ - 0.44 - 0.012
    body, trim = "Lib_Siding", "Lib_PaintWhite"
    for lod in (0, 1, 2):
        g = a.begin(lod)
        cols = []
        _wall(g, cols, "z", hz - 0.11, -1.0, span_x, 0.0, PLATE, _front_holes(), body, trim, lod, "Climb_Front")
        _wall(g, cols, "z", -hz + 0.11, 1.0, span_x, 0.0, PLATE, _back_holes(), body, trim, lod, "Climb_Back")
        _wall(g, cols, "x", hx - 0.11, -1.0, span_z, 0.0, PLATE, _side_holes(True), body, trim, lod, "Climb_Right")
        _wall(g, cols, "x", -(hx - 0.11), 1.0, span_z, 0.0, PLATE, _side_holes(False), body, trim, lod, "Climb_Left")
        _garage_door(g, lod)
        _porch(g, cols, lod)
        _steps(g, cols)
        _hip(g, ROOF_X, ROOF_Z, PLATE, RISE)
        if lod == 0:
            kept = []
            for item in cols:
                if item[0] == "cap":
                    a.capsule(item[1], item[2], item[3], item[4], item[5])
                else:
                    kept.append(item)
            a._cols = kept
        a.end()
    _apply_cols(a)
    y0 = PLATE + 0.01
    y1 = PLATE + RISE
    _roof_landing(a, "Col_RoofS", ROOF_Z, y0, 0.0, y1, ROOF_X * 0.84)
    _roof_landing(a, "Col_RoofN", -ROOF_Z, y0, 0.0, y1, ROOF_X * 0.84)
    return a
