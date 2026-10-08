"""Brick harbor warehouse. Two roll-up doors face +Z. Roof is corrugated steel."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

W = 10.0
D = 6.2
WALL_TOP = 4.40
T = 0.16
PLATE_BOTTOM = WALL_TOP + 0.004
PLATE_TOP = PLATE_BOTTOM + 0.056


def _walls(g):
    y = (0.148 + WALL_TOP) * 0.5
    h = WALL_TOP - 0.148
    # Sides run the depth. Front and back sit between them.
    g.box((-W * 0.5 + T * 0.5, y, 0), (T, h, D - 0.04), "Lib_Brick", uv_scale=0.55)
    g.box((W * 0.5 - T * 0.5, y, 0), (T, h, D - 0.04), "Lib_Brick", uv_scale=0.55)
    z_b = -D * 0.5 + T * 0.5
    g.box((0, y, z_b), (W - T * 2 - 0.02, h, T), "Lib_Brick", uv_scale=0.55)
    z_f = D * 0.5 - T * 0.5
    # Piers. Door openings are x = ±2.15, 2.50 m wide, from y = 0.18 to 3.52.
    pier_h = h
    g.box((-4.12, y, z_f), (1.40, pier_h, T), "Lib_Brick", uv_scale=0.55)
    g.box((0.0, y, z_f), (1.64, pier_h, T), "Lib_Brick", uv_scale=0.55)
    g.box((4.12, y, z_f), (1.40, pier_h, T), "Lib_Brick", uv_scale=0.55)
    head_y = (3.528 + WALL_TOP) * 0.5
    head_h = WALL_TOP - 3.528
    g.box((-2.15, head_y, z_f), (2.42, head_h, T), "Lib_Brick", uv_scale=0.55)
    g.box((2.15, head_y, z_f), (2.42, head_h, T), "Lib_Brick", uv_scale=0.55)


def _plate(g):
    cy = (PLATE_BOTTOM + PLATE_TOP) * 0.5
    h = PLATE_TOP - PLATE_BOTTOM
    g.box((0, cy, -D * 0.5 + T * 0.5), (W - T * 2 - 0.04, h, T - 0.02), "Lib_Concrete")
    g.box((0, cy, D * 0.5 - T * 0.5), (W - T * 2 - 0.04, h, T - 0.02), "Lib_Concrete")
    g.box((-W * 0.5 + T * 0.5, cy, 0), (T - 0.02, h, D - T * 2 - 0.08), "Lib_Concrete")
    g.box((W * 0.5 - T * 0.5, cy, 0), (T - 0.02, h, D - T * 2 - 0.08), "Lib_Concrete")


def _roof(g, lod):
    """Slab 4 mm above the plate, ribs proud of the slab, fascia outside the edge."""
    thick = 0.10
    cy = PLATE_TOP + 0.004 + thick * 0.5
    sx = W + 0.70
    sz = D + 0.70
    g.box((0, cy, 0), (sx, thick, sz), "Lib_MetalWorn", uv_scale=0.45)
    ribs = lod_pick(lod, 15, 8)
    y = PLATE_TOP + 0.004 + thick + 0.004 + 0.012
    x0 = -4.55
    x1 = 4.55
    for i in range(ribs):
        x = x0 + (x1 - x0) * (i / max(1, ribs - 1))
        g.box((x, y, 0), (0.045, 0.024, sz - 0.20), "Lib_Steel")
    fascia_y = cy - 0.02
    g.box((0, fascia_y, sz * 0.5 + 0.022), (sx, 0.16, 0.028), "Lib_SteelDark")
    g.box((0, fascia_y, -(sz * 0.5 + 0.022)), (sx, 0.16, 0.028), "Lib_SteelDark")
    g.box((sx * 0.5 + 0.022, fascia_y, 0), (0.028, 0.16, sz), "Lib_SteelDark")
    g.box((-(sx * 0.5 + 0.022), fascia_y, 0), (0.028, 0.16, sz), "Lib_SteelDark")


def _door(g, lod, x):
    z = D * 0.5 - T * 0.5
    # Closed curtain behind the slats, inset from the jambs and the header.
    g.box((x, 1.86, z), (2.36, 3.20, 0.04), "Lib_SteelDark")
    slats = lod_pick(lod, 8, 4)
    step = 3.04 / slats
    for i in range(slats):
        y = 0.38 + (i + 0.5) * step
        g.box((x, y, z + 0.032), (2.28, step - 0.012, 0.014), "Lib_Steel")
    # Drum housing clear of the header face (outer z is D/2).
    g.box((x, 3.70, D * 0.5 + 0.07), (2.55, 0.26, 0.10), "Lib_SteelDark")
    if lod == 0:
        g.box((x - 0.95, 1.90, z + 0.072), (0.035, 3.05, 0.018), "Lib_Steel")
        g.box((x + 0.95, 1.90, z + 0.072), (0.035, 3.05, 0.018), "Lib_Steel")
        g.box((x + 0.72, 1.15, z + 0.078), (0.06, 0.16, 0.016), "Lib_Brass")


def _windows(g, lod):
    if lod != 0:
        return
    for x in (-2.2, 0.0, 2.2):
        for side in (-1.0, 1.0):
            face = side * (W * 0.5 + 0.018)
            g.box((face, 3.55, x), (0.02, 0.48, 0.70), "Lib_Varnish")
            g.box((side * (W * 0.5 + 0.038), 3.55, x), (0.012, 0.30, 0.46), "Lib_ShopGlass")


@register
def create():
    a = Asset(
        "HarborWarehouse",
        "Harbor",
        "Brick warehouse, 10 x 6.2 m, walls at 4.40 m. Two roll-up doors face +Z. Corrugated steel roof sits on a top plate.",
    )
    a.climbable = True
    a.climb_note = "Brick walls are cling. The roll-up doors are closed."
    a.vault_note = "No rail. Wall top is 4.40 m."
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.07, 0), (W + 0.16, 0.14, D + 0.16), "Lib_Concrete", uv_scale=0.4)
        _walls(g)
        _plate(g)
        _roof(g, lod)
        _door(g, lod, -2.15)
        _door(g, lod, 2.15)
        _windows(g, lod)
        if lod == 0:
            g.text("WHARF", (0, 4.12, D * 0.5 + 0.06), 0.36, "Lib_PaintWhite", extrude=0.012, yaw=0)
        a.end()
    a.box("Col_Floor", (0, 0.06, 0), (W - 0.40, 0.06, D - 0.40))
    a.box("Climb_Back", (0, 2.20, -D * 0.5 + 0.08), (W - 0.60, 3.80, 0.08))
    a.box("Climb_SideL", (-W * 0.5 + 0.08, 2.20, 0), (0.08, 3.80, D - 0.50))
    a.box("Climb_SideR", (W * 0.5 - 0.08, 2.20, 0), (0.08, 3.80, D - 0.50))
    a.box("Climb_PierL", (-4.12, 2.20, D * 0.5 - 0.08), (1.10, 3.80, 0.08))
    a.box("Climb_PierC", (0, 2.20, D * 0.5 - 0.08), (1.30, 3.80, 0.08))
    a.box("Climb_PierR", (4.12, 2.20, D * 0.5 - 0.08), (1.10, 3.80, 0.08))
    a.box("Col_DoorL", (-2.15, 1.86, D * 0.5 - 0.08), (2.10, 2.90, 0.03))
    a.box("Col_DoorR", (2.15, 1.86, D * 0.5 - 0.08), (2.10, 2.90, 0.03))
    return a
