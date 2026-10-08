"""Downtown steel planter. 1.80 m, slatted sides, soil, two shrubs."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Planter_Street",
        "StreetFurniture",
        "Street planter 1.80 x 0.48 m, 0.46 m tall. Steel frame, wood slats, soil and one shrub mass.",
    )
    a.climb_note = "Rim is 0.46 m. Too low to cling."
    a.vault_note = "Too low to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = 1 if lod == 0 else 0
        # Frame
        g.box((0, 0.24, -0.22), (1.80, 0.44, 0.04), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((0, 0.24, 0.22), (1.80, 0.44, 0.04), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((-0.88, 0.24, 0), (0.04, 0.44, 0.40), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((0.88, 0.24, 0), (0.04, 0.44, 0.40), "Lib_SteelDark", bevel=bev, segs=bs)
        # Stops short of the walls so the two solids do not share a face.
        g.box((0, 0.045, 0), (1.70, 0.05, 0.38), "Lib_SteelDark")
        g.box((0, 0.46, 0), (1.86, 0.04, 0.52), "Lib_Steel", bevel=bev, segs=bs)
        # Slats on the long faces, outside the frame.
        for i, x in enumerate((-0.66, -0.33, 0.0, 0.33, 0.66)):
            if lod == 1 and i % 2:
                continue
            g.box((x, 0.24, -0.255), (0.22, 0.40, 0.018), "Lib_Wood", uv_scale=1.4, bevel=bev, segs=1)
            g.box((x, 0.24, 0.255), (0.22, 0.40, 0.018), "Lib_Wood", uv_scale=1.4, bevel=bev, segs=1)
        g.box((0, 0.16, 0), (1.64, 0.08, 0.32), "Lib_Soil", uv_scale=1.2)
        g.sphere((-0.42, 0.58, 0.0), 0.20, "Lib_Foliage", lod_pick(lod, 12, 8))
        g.sphere((-0.22, 0.58, 0.08), 0.12, "Lib_FoliageDark", lod_pick(lod, 8, 6))
        g.sphere((-0.08, 0.66, 0.0), 0.14, "Lib_FoliageLite", lod_pick(lod, 10, 6))
        g.sphere((-0.02, 0.56, 0.0), 0.24, "Lib_Foliage", lod_pick(lod, 12, 8))
        g.sphere((0.36, 0.56, -0.02), 0.16, "Lib_FoliageLite", lod_pick(lod, 10, 6))
        g.sphere((0.18, 0.50, 0.08), 0.10, "Lib_Foliage", lod_pick(lod, 8, 6))
        g.sphere((0.50, 0.48, 0.08), 0.10, "Lib_FoliageDark", lod_pick(lod, 8, 6))
        if lod == 0:
            g.sphere((-0.22, 0.62, 0.12), 0.025, "Lib_PaintRed", 6)
            g.sphere((0.42, 0.60, -0.10), 0.022, "Lib_PaintCream", 6)
            for x in (-0.70, 0.70):
                for z in (-0.16, 0.16):
                    g.box((x, 0.015, z), (0.08, 0.03, 0.06), "Lib_Rubber", bevel=0.002, segs=1)
        a.end()
    a.box("Col_WallN", (0, 0.22, 0.22), (1.68, 0.36, 0.028))
    a.box("Col_WallS", (0, 0.22, -0.22), (1.68, 0.36, 0.028))
    a.box("Col_WallW", (-0.88, 0.22, 0), (0.028, 0.36, 0.32))
    a.box("Col_WallE", (0.88, 0.22, 0), (0.028, 0.36, 0.32))
    a.box("Col_Floor", (0, 0.045, 0), (1.60, 0.03, 0.32))
    a.box("Col_Rim", (0, 0.472, 0), (1.74, 0.014, 0.44))
    a.sphere("Col_ShrubL", (-0.42, 0.64, 0.0), 0.10)
    a.sphere("Col_ShrubR", (0.36, 0.62, -0.02), 0.08)
    return a
