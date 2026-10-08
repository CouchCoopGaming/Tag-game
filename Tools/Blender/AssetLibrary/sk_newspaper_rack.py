"""Three honor boxes chained to a pole. No brand, no masthead.

Each box is about 0.50 m wide and 0.45 m deep, between 1.05 m and 1.14 m tall.
One has a rounded top, one a flat lid, one a slanted lid.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import chain

# Footprint is centered: pole base left edge lines up with the right lid edge.
POLE_X = -1.17
AX = -0.62
BX = 0.20
CX = 1.02
ZC = -0.02


def _feet(g, x, lod):
    """Four feet and square legs. The cabinet sleeves the top of each leg."""
    bev = lod_pick(lod, 0.0015, 0.0)
    for lx in (x - 0.18, x + 0.18):
        for lz in (-0.14, 0.10):
            g.box((lx, 0.012, lz), (0.072, 0.024, 0.072), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
            g.box((lx, 0.095, lz), (0.032, 0.150, 0.032), "Lib_Steel")


def _paper(g, x, y, z, lod):
    """Folded stack in front of the cabinet and behind the pane. Blocks, no words."""
    g.box((x + 0.004, y - 0.012, z - 0.010), (0.24, 0.22, 0.008), "Lib_PaintWhite")
    g.box((x, y, z), (0.28, 0.26, 0.012), "Lib_PaintCream")
    if lod != 0:
        return
    bars = (
        (-0.01, 0.090, 0.20, 0.012),
        (0.02, 0.055, 0.15, 0.009),
        (-0.02, 0.022, 0.22, 0.009),
        (0.01, -0.012, 0.13, 0.009),
        (-0.03, -0.046, 0.18, 0.009),
        (0.02, -0.078, 0.12, 0.009),
    )
    for dx, dy, w, h in bars:
        g.box((x + dx, y + dy, z + 0.008), (w, h, 0.003), "Lib_Black")
    g.box((x - 0.07, y + 0.04, z + 0.008), (0.07, 0.06, 0.003), "Lib_SteelDark")


def _window(g, x, y, z, w, h, lod):
    """Upper door: frame, tinted pane, paper, and a pull bar."""
    t = 0.016
    g.box((x, y + h * 0.5 - t * 0.5, z), (w, t, 0.016), "Lib_SteelDark")
    g.box((x, y - h * 0.5 + t * 0.5, z), (w, t, 0.016), "Lib_SteelDark")
    g.box((x - w * 0.5 + t * 0.5, y, z), (t, h, 0.016), "Lib_SteelDark")
    g.box((x + w * 0.5 - t * 0.5, y, z), (t, h, 0.016), "Lib_SteelDark")
    g.box((x, y, z - 0.006), (w - 0.036, h - 0.036, 0.008), "Lib_HonorGlass")
    _paper(g, x, y - 0.006, z - 0.022, lod)
    seg = lod_pick(lod, 8, 6)
    hy = y - h * 0.5 - 0.045
    g.cylinder((x - 0.055, hy, z + 0.006), 0.007, 0.024, "Lib_Steel", seg, axis="Z")
    g.cylinder((x + 0.055, hy, z + 0.006), 0.007, 0.024, "Lib_Steel", seg, axis="Z")
    g.cylinder((x, hy, z + 0.016), 0.010, 0.15, "Lib_Steel", seg, axis="X")


def _coin(g, center, size, lod):
    """Card and coin housing. Slot and a return, no legend."""
    g.box(center, size, "Lib_SteelDark", bevel=lod_pick(lod, 0.002, 0.0), segs=1 if lod == 0 else 0)
    if lod != 0:
        return
    cx, cy, cz = center
    face = cz + size[2] * 0.5 + 0.002
    g.box((cx, cy + size[1] * 0.18, face), (size[0] * 0.55, 0.008, 0.004), "Lib_Black")
    g.box((cx - 0.01, cy - size[1] * 0.16, face), (size[0] * 0.40, 0.020, 0.004), "Lib_Black")
    g.cylinder((cx + size[0] * 0.22, cy - 0.01, face), 0.008, 0.010, "Lib_Brass", 6, axis="Z")


def _wear(g, kind, x, lod):
    """Scuffs and blank sticker remnants. No lettering."""
    if lod != 0:
        return
    if kind == "round":
        g.box((x - 0.10, 0.30, 0.196), (0.10, 0.06, 0.008), "Lib_MetalWorn")
        g.box((x + 0.242, 0.58, 0.02), (0.008, 0.05, 0.07), "Lib_PaintCream", euler=(0, 8, 0))
        g.box((x + 0.06, 0.34, 0.196), (0.05, 0.035, 0.006), "Lib_PaintWhite", euler=(0, -6, 0))
    elif kind == "flat":
        g.box((x - 0.252, 0.42, 0.04), (0.008, 0.09, 0.06), "Lib_Rust")
        g.box((x - 0.08, 0.40, 0.210), (0.06, 0.04, 0.006), "Lib_PaintWhite", euler=(0, 7, 0))
        g.box((x + 0.10, 0.36, 0.210), (0.035, 0.028, 0.006), "Lib_PaintTeal", euler=(0, -4, 0))
    else:
        g.box((x + 0.04, 0.28, 0.196), (0.12, 0.045, 0.006), "Lib_MetalWorn")
        g.box((x - 0.242, 0.62, -0.02), (0.008, 0.055, 0.08), "Lib_Orange", euler=(0, -9, 0))


def _round_box(g, x, lod):
    bev = lod_pick(lod, 0.003, 0.0)
    bs = 1 if lod == 0 else 0
    _feet(g, x, lod)
    g.box((x, 0.52, ZC), (0.48, 0.76, 0.38), "Lib_PaintBlue", bevel=bev, segs=bs)
    g.cylinder((x, 0.90, ZC), 0.18, 0.44, "Lib_PaintBlue", lod_pick(lod, 20, 10), axis="X")
    g.box((x, 0.36, 0.185), (0.44, 0.30, 0.014), "Lib_PaintBlue", bevel=bev, segs=bs)
    g.cylinder((x - 0.232, 0.52, 0.198), 0.009, 0.50, "Lib_Steel", lod_pick(lod, 8, 6), axis="Y")
    _window(g, x - 0.06, 0.64, 0.206, 0.26, 0.30, lod)
    _coin(g, (x + 0.15, 0.76, 0.198), (0.13, 0.16, 0.07), lod)
    _wear(g, "round", x, lod)


def _flat_box(g, x, lod):
    bev = lod_pick(lod, 0.003, 0.0)
    bs = 1 if lod == 0 else 0
    g.box((x, 0.016, ZC), (0.44, 0.032, 0.36), "Lib_SteelDark", bevel=bev, segs=bs)
    g.box((x, 0.062, ZC), (0.34, 0.072, 0.28), "Lib_Steel")
    g.box((x, 0.51, ZC), (0.50, 0.86, 0.40), "Lib_PaintRed", bevel=bev, segs=bs)
    g.box((x, 0.95, ZC), (0.54, 0.10, 0.46), "Lib_PaintRed", bevel=bev, segs=bs)
    g.box((x, 0.40, 0.198), (0.46, 0.36, 0.014), "Lib_PaintRed", bevel=bev, segs=bs)
    g.cylinder((x - 0.242, 0.58, 0.210), 0.009, 0.56, "Lib_Steel", lod_pick(lod, 8, 6), axis="Y")
    _window(g, x, 0.70, 0.216, 0.36, 0.34, lod)
    _coin(g, (x + 0.12, 1.06, ZC), (0.16, 0.16, 0.14), lod)
    _wear(g, "flat", x, lod)


def _slant_box(g, x, lod):
    bev = lod_pick(lod, 0.003, 0.0)
    bs = 1 if lod == 0 else 0
    _feet(g, x, lod)
    g.box((x, 0.52, ZC), (0.48, 0.76, 0.38), "Lib_PaintGreen", bevel=bev, segs=bs)
    g.box((x, 0.90, ZC), (0.52, 0.14, 0.44), "Lib_PaintGreen", bevel=bev, segs=bs, euler=(12, 0, 0))
    g.box((x, 0.36, 0.185), (0.44, 0.30, 0.014), "Lib_PaintGreen", bevel=bev, segs=bs)
    g.cylinder((x - 0.232, 0.50, 0.198), 0.009, 0.46, "Lib_Steel", lod_pick(lod, 8, 6), axis="Y")
    _window(g, x - 0.02, 0.60, 0.206, 0.32, 0.28, lod)
    _coin(g, (x + 0.13, 0.98, -0.12), (0.13, 0.16, 0.11), lod)
    _wear(g, "slant", x, lod)


def _pole(g, lod):
    bev = lod_pick(lod, 0.002, 0.0)
    g.box((POLE_X, 0.014, ZC), (0.24, 0.028, 0.24), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
    g.cylinder((POLE_X, 0.76, ZC), 0.048, 1.48, "Lib_Steel", lod_pick(lod, 12, 8))
    g.cylinder((POLE_X, 1.515, ZC), 0.052, 0.04, "Lib_SteelDark", lod_pick(lod, 12, 8))
    g.box((POLE_X + 0.07, 0.50, 0.06), (0.04, 0.07, 0.028), "Lib_SteelDark")


def _cable(g, lod):
    n = lod_pick(lod, 7, 4)
    y = 0.46
    z = 0.10
    chain(g, (POLE_X + 0.05, 0.52, 0.04), (AX - 0.26, y, z), n=n, sag=0.06, radius=0.008)
    chain(g, (AX + 0.26, y, z), (BX - 0.28, y, z), n=n, sag=0.05, radius=0.008)
    chain(g, (BX + 0.28, y, z), (CX - 0.26, y, z), n=n, sag=0.05, radius=0.008)
    for x in (AX - 0.25, AX + 0.25, BX - 0.27, BX + 0.27, CX - 0.25):
        g.cylinder((x, y, z), 0.012, 0.03, "Lib_Steel", lod_pick(lod, 8, 6), axis="X")


@register
def create():
    a = Asset(
        "NewspaperRack",
        "StreetFurniture",
        "Three honor boxes, 0.50 m wide, lids from 1.05 m to 1.14 m, chained to a pole. No masthead.",
    )
    a.climb_note = "Not a cling surface."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        _pole(g, lod)
        _round_box(g, AX, lod)
        _flat_box(g, BX, lod)
        _slant_box(g, CX, lod)
        _cable(g, lod)
        a.end()
    # Each collider sits in one solid: cabinet clear of the lid, legs, and coin box.
    for x in (AX, CX):
        tag = "A" if x == AX else "C"
        for i, (lx, lz) in enumerate(((-0.18, -0.14), (-0.18, 0.10), (0.18, -0.14), (0.18, 0.10))):
            a.box("Col_Foot_%s_%d" % (tag, i), (x + lx, 0.010, lz), (0.050, 0.016, 0.050))
            a.box("Col_Leg_%s_%d" % (tag, i), (x + lx, 0.085, lz), (0.020, 0.090, 0.020))
        a.box("Col_Cab_%s" % tag, (x, 0.46, -0.06), (0.32, 0.48, 0.20))
    a.capsule("Col_Lid_A", (AX, 1.02, ZC), 0.050, 0.18, direction=0)
    a.box("Col_Base_B", (BX, 0.012, ZC), (0.32, 0.018, 0.24))
    a.box("Col_Cab_B", (BX, 0.50, -0.06), (0.36, 0.72, 0.22))
    a.box("Col_Lid_B", (BX - 0.10, 0.972, ZC), (0.24, 0.036, 0.30))
    a.box("Col_Lid_C", (CX - 0.10, 0.956, -0.131), (0.20, 0.064, 0.12), euler=(12, 0, 0))
    a.box("Col_Coin_B", (BX + 0.12, 1.07, ZC), (0.10, 0.12, 0.08))
    a.box("Col_PoleBase", (POLE_X, 0.012, ZC), (0.16, 0.018, 0.16))
    a.capsule("Col_Pole", (POLE_X, 0.78, ZC), 0.032, 1.49, direction=1)
    return a
