"""City bus shelter. Glass sides, a bench, a blank ad frame, and a drip-edge roof.

Open toward -Z. No route mark and no poster art.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _frame_pane(g, center, size, lod, axis="z"):
    """Steel rectangle around a glass sheet. size is the outer opening."""
    cx, cy, cz = center
    w, h, _d = size
    t = 0.035
    if axis == "z":
        g.box((cx, cy + h * 0.5 - t * 0.5, cz), (w, t, 0.04), "Lib_Steel")
        g.box((cx, cy - h * 0.5 + t * 0.5, cz), (w, t, 0.04), "Lib_Steel")
        g.box((cx - w * 0.5 + t * 0.5, cy, cz), (t, h, 0.04), "Lib_Steel")
        g.box((cx + w * 0.5 - t * 0.5, cy, cz), (t, h, 0.04), "Lib_Steel")
        g.box((cx, cy, cz), (w - t * 2.0, h - t * 2.0, 0.012), "Lib_HonorGlass")
    else:
        g.box((cx, cy + h * 0.5 - t * 0.5, cz), (0.04, t, w), "Lib_Steel")
        g.box((cx, cy - h * 0.5 + t * 0.5, cz), (0.04, t, w), "Lib_Steel")
        g.box((cx, cy, cz - w * 0.5 + t * 0.5), (0.04, h, t), "Lib_Steel")
        g.box((cx, cy, cz + w * 0.5 - t * 0.5), (0.04, h, t), "Lib_Steel")
        g.box((cx, cy, cz), (0.012, h - t * 2.0, w - t * 2.0), "Lib_HonorGlass")


def _roof(g, lod):
    """Flat roof with a turned-down lip on all four edges."""
    bev = lod_pick(lod, 0.003, 0.0)
    bs = 1 if lod == 0 else 0
    g.box((0, 2.46, 0.02), (3.50, 0.06, 1.62), "Lib_SteelDark", bevel=bev, segs=bs)
    lip_y = 2.405
    g.box((0, lip_y, -0.80), (3.54, 0.055, 0.028), "Lib_Steel")
    g.box((0, lip_y, 0.84), (3.54, 0.055, 0.028), "Lib_Steel")
    g.box((-1.76, lip_y, 0.02), (0.028, 0.055, 1.66), "Lib_Steel")
    g.box((1.76, lip_y, 0.02), (0.028, 0.055, 1.66), "Lib_Steel")


def _bench(g, lod):
    bev = lod_pick(lod, 0.002, 0.0)
    bs = 1 if lod == 0 else 0
    for x in (-0.85, 0.0, 0.85):
        g.box((x, 0.24, 0.30), (0.04, 0.46, 0.04), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((x, 0.24, 0.48), (0.04, 0.46, 0.04), "Lib_SteelDark")
        g.box((x, 0.46, 0.39), (0.04, 0.04, 0.22), "Lib_SteelDark")
    for i, z in enumerate((0.22, 0.32, 0.42)):
        if lod == 1 and i == 1:
            continue
        g.box((0, 0.48, z), (2.20, 0.028, 0.07), "Lib_MetalWorn", bevel=bev, segs=1)
    g.box((0, 0.78, 0.50), (2.20, 0.08, 0.028), "Lib_MetalWorn")
    g.box((0, 0.96, 0.50), (2.20, 0.08, 0.028), "Lib_MetalWorn")


def _ad(g, lod):
    """Blank insert in a frame. No art and no type."""
    bev = lod_pick(lod, 0.002, 0.0)
    g.box((-0.55, 1.45, 0.58), (1.05, 1.40, 0.03), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
    g.box((-0.55, 1.45, 0.598), (0.90, 1.22, 0.012), "Lib_PaintCream")


@register
def create():
    a = Asset(
        "BusShelter_City",
        "StreetFurniture",
        "3.5 m shelter, open on -Z. Roof at 2.49 m with a drip lip, glass sides, slat bench, blank ad frame.",
    )
    a.climb_note = "Posts are 8 cm. The roof is a landing, not a cling wall."
    a.vault_note = "Roof edge is 2.49 m. Too high to vault from the ground."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        # Posts sit clear of the glass planes so a collider sample stays in one solid.
        for x in (-1.62, 1.62):
            for z in (-0.70, 0.74):
                g.box((x, 1.22, z), (0.08, 2.44, 0.08), "Lib_Steel", bevel=bev, segs=bs)
                g.box((x, 0.02, z), (0.16, 0.04, 0.16), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        _roof(g, lod)
        # Back wall, two panes. Side panes are inset from the posts.
        _frame_pane(g, (-0.78, 1.35, 0.62), (1.40, 1.70, 0), lod, axis="z")
        _frame_pane(g, (0.78, 1.35, 0.62), (1.40, 1.70, 0), lod, axis="z")
        _frame_pane(g, (-1.50, 1.30, 0.02), (1.10, 1.55, 0), lod, axis="x")
        _frame_pane(g, (1.50, 1.30, 0.02), (1.10, 1.55, 0), lod, axis="x")
        _bench(g, lod)
        _ad(g, lod)
        a.end()
    for i, x in enumerate((-1.62, 1.62)):
        for j, z in enumerate((-0.70, 0.74)):
            a.box("Col_Post_%d%d" % (i, j), (x, 1.15, z), (0.05, 2.10, 0.05))
            a.box("Col_Foot_%d%d" % (i, j), (x, 0.016, z), (0.12, 0.024, 0.12))
    # Roof crown is 2.49 m. Collider top sits just under it, inside the slab.
    a.box("Col_Roof", (0, 2.470, 0.02), (3.30, 0.030, 1.40))
    a.box("Col_GlassBL", (-0.78, 1.35, 0.62), (1.20, 1.50, 0.012), approx=True)
    a.box("Col_GlassBR", (0.78, 1.35, 0.62), (1.20, 1.50, 0.012), approx=True)
    a.box("Col_GlassL", (-1.50, 1.30, 0.02), (0.012, 1.35, 0.90), approx=True)
    a.box("Col_GlassR", (1.50, 1.30, 0.02), (0.012, 1.35, 0.90), approx=True)
    for i, z in enumerate((0.22, 0.32, 0.42)):
        for j, x in enumerate((-0.42, 0.42)):
            a.box("Col_Seat_%d%d" % (i, j), (x, 0.48, z), (0.70, 0.016, 0.04))
    a.box("Col_Back0", (0, 0.78, 0.50), (2.05, 0.05, 0.018))
    a.box("Col_Back1", (0, 0.96, 0.50), (2.05, 0.05, 0.018))
    for i, x in enumerate((-0.85, 0.0, 0.85)):
        a.box("Col_LegF_%d" % i, (x, 0.24, 0.30), (0.028, 0.40, 0.028))
        a.box("Col_LegB_%d" % i, (x, 0.24, 0.48), (0.028, 0.40, 0.028))
    a.box("Col_Ad", (-0.55, 1.45, 0.572), (0.80, 1.05, 0.016))
    return a
