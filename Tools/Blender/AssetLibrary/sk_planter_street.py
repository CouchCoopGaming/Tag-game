"""Downtown steel planter.

Real size: 6 ft long by 18 in wide, rim at 18 in (1.83 x 0.46 m, rim 0.46 m).
Shrubs are one closed canopy each so the sphere colliders can sit inside them.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _canopy(g, origin, scale, lod):
    """One remeshed solid. Extra spheres only break up the silhouette."""
    ox, oy, oz = origin
    spheres = (
        ((ox, oy + 0.06 * scale, oz), 0.155 * scale),
        ((ox + 0.07 * scale, oy + 0.12 * scale, oz + 0.035 * scale), 0.105 * scale),
        ((ox - 0.06 * scale, oy + 0.11 * scale, oz - 0.03 * scale), 0.095 * scale),
        ((ox + 0.01 * scale, oy + 0.20 * scale, oz - 0.01 * scale), 0.085 * scale),
    )
    g.blob(spheres, "Lib_Foliage", voxel=0.055 if lod == 0 else 0.09)
    if lod == 0:
        for k in range(5):
            ang = k * 1.2 + 0.4
            g.box(
                (
                    ox + math.cos(ang) * 0.12 * scale,
                    oy + 0.18 * scale + (k % 3) * 0.025,
                    oz + math.sin(ang) * 0.08 * scale,
                ),
                (0.09 * scale, 0.007, 0.04 * scale),
                "Lib_FoliageDark" if k % 2 else "Lib_FoliageLite",
                euler=(28, k * 36, 12),
            )


@register
def create():
    a = Asset(
        "Planter_Street",
        "StreetFurniture",
        "Street planter 1.83 x 0.46 m, rim at 0.46 m. Steel frame, wood slats, soil, two shrub canopies.",
    )
    a.climb_note = "Rim is 0.46 m. Too low to cling."
    a.vault_note = "Too low to vault."
    # Canopy centers. Collider spheres sit inside the main blob sphere.
    shrubs = (
        ((-0.42, 0.34, 0.00), 1.08),
        ((0.34, 0.33, -0.015), 0.86),
    )
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = 1 if lod == 0 else 0
        # Corner posts, then rails. Slats sit in the opening, not on the same face.
        for x in (-0.875, 0.875):
            for z in (-0.20, 0.20):
                g.box((x, 0.24, z), (0.06, 0.46, 0.06), "Lib_SteelDark", bevel=bev, segs=bs)
                if lod == 0:
                    g.box((x, 0.06, z), (0.07, 0.04, 0.07), "Lib_Rust")
        g.box((0, 0.44, -0.20), (1.70, 0.045, 0.045), "Lib_Steel", bevel=bev, segs=bs)
        g.box((0, 0.44, 0.20), (1.70, 0.045, 0.045), "Lib_Steel", bevel=bev, segs=bs)
        g.box((0, 0.08, -0.20), (1.70, 0.04, 0.04), "Lib_SteelDark")
        g.box((0, 0.08, 0.20), (1.70, 0.04, 0.04), "Lib_SteelDark")
        # Rim is a ring, so the shrubs are not buried in a solid lid.
        g.box((0, 0.46, -0.225), (1.88, 0.035, 0.055), "Lib_Steel", bevel=bev, segs=bs)
        g.box((0, 0.46, 0.225), (1.88, 0.035, 0.055), "Lib_Steel", bevel=bev, segs=bs)
        g.box((-0.912, 0.46, 0), (0.055, 0.035, 0.40), "Lib_Steel", bevel=bev, segs=bs)
        g.box((0.912, 0.46, 0), (0.055, 0.035, 0.40), "Lib_Steel", bevel=bev, segs=bs)
        g.box((0, 0.04, 0), (1.68, 0.06, 0.32), "Lib_SteelDark")
        for i, x in enumerate((-0.62, -0.31, 0.0, 0.31, 0.62)):
            if lod == 1 and i % 2:
                continue
            mat = "Lib_WoodWeather" if i in (1, 3) else "Lib_Wood"
            g.box((x, 0.25, -0.232), (0.24, 0.32, 0.018), mat, uv_scale=1.5, bevel=0.0, segs=0)
            g.box((x, 0.25, 0.232), (0.24, 0.32, 0.018), mat, uv_scale=1.5, bevel=0.0, segs=0)
        g.box((0, 0.14, 0), (1.58, 0.10, 0.30), "Lib_Soil", uv_scale=1.3)
        g.box((0, 0.22, 0), (1.10, 0.06, 0.18), "Lib_Mulch", uv_scale=1.4)
        if lod == 0:
            g.box((-0.55, 0.18, 0.06), (0.22, 0.03, 0.10), "Lib_Soil")
        for origin, scale in shrubs:
            _canopy(g, origin, scale, lod)
        if lod == 0:
            g.sphere((-0.55, 0.58, 0.06), 0.016, "Lib_PaintRed", 6)
            g.sphere((-0.50, 0.55, 0.02), 0.012, "Lib_PaintCream", 6)
            g.sphere((0.48, 0.52, -0.05), 0.014, "Lib_PaintCream", 6)
            for x in (-0.78, 0.78):
                for z in (-0.14, 0.14):
                    g.box((x, 0.012, z), (0.07, 0.024, 0.05), "Lib_Rubber", bevel=0.002, segs=1)
        a.end()
    n = 0
    for x in (-0.875, 0.875):
        for z in (-0.20, 0.20):
            a.box("Col_Post_%d" % n, (x, 0.24, z), (0.034, 0.34, 0.034))
            n += 1
    a.box("Col_RailN", (0, 0.44, 0.20), (1.48, 0.022, 0.022))
    a.box("Col_RailS", (0, 0.44, -0.20), (1.48, 0.022, 0.022))
    a.box("Col_Floor", (0, 0.05, 0), (1.48, 0.028, 0.22))
    # Rim crown is 0.4775 m. Collider top is 0.7 cm under it.
    a.box("Col_RimN", (0, 0.464, 0.225), (1.70, 0.012, 0.028))
    a.box("Col_RimS", (0, 0.464, -0.225), (1.70, 0.012, 0.028))
    # Inside the main canopy sphere (radius 0.155 * scale), clear of the soil.
    a.sphere("Col_ShrubL", (-0.42, 0.405, 0.00), 0.118)
    a.sphere("Col_ShrubR", (0.34, 0.382, -0.015), 0.090)
    return a
