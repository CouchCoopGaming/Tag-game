"""City bus shelter with a framed glass back, slat bench, and a roof at 2.48 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "BusShelter_City",
        "StreetFurniture",
        "3.2 m shelter, open on -Z. Roof top 2.48 m, framed glass, slat bench, poster, route blade.",
    )
    a.climb_note = "Posts are 8 cm. The roof is a landing, not a cling wall."
    a.vault_note = "Roof edge is 2.48 m. Too high to vault from the ground."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = lod_pick(lod, 1, 0)
        for x in (-1.52, 1.52):
            for z in (-0.62, 0.62):
                g.box((x, 1.18, z), (0.08, 2.36, 0.08), "Lib_Steel", bevel=bev, segs=bs)
                g.box((x, 0.02, z), (0.16, 0.04, 0.16), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0, 2.44, 0.02), (3.40, 0.07, 1.62), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((0, 2.49, -0.72), (3.40, 0.04, 0.08), "Lib_Steel")
        g.box((0, 2.40, 0.78), (3.20, 0.05, 0.06), "Lib_Steel")
        # Back frame and glass. Mullions keep it from reading as one pane.
        g.box((0, 1.25, 0.66), (3.00, 0.06, 0.04), "Lib_Steel")
        g.box((0, 2.15, 0.66), (3.00, 0.06, 0.04), "Lib_Steel")
        g.box((0, 1.70, 0.66), (0.04, 0.90, 0.04), "Lib_Steel")
        g.box((-1.48, 1.70, 0.66), (0.04, 0.90, 0.04), "Lib_Steel")
        g.box((1.48, 1.70, 0.66), (0.04, 0.90, 0.04), "Lib_Steel")
        g.box((-0.74, 1.70, 0.655), (1.40, 0.82, 0.012), "Lib_Glass")
        g.box((0.74, 1.70, 0.655), (1.40, 0.82, 0.012), "Lib_Glass")
        g.box((1.56, 1.30, 0.0), (0.012, 1.50, 1.05), "Lib_Glass")
        g.box((1.56, 1.30, 0.0), (0.03, 0.04, 1.10), "Lib_Steel")
        # Poster on the back, outside the glass.
        g.box((-0.85, 1.55, 0.70), (0.90, 1.05, 0.02), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((-0.85, 1.55, 0.715), (0.72, 0.86, 0.008), "Lib_PaintCream")
        g.box((-0.85, 1.95, 0.72), (0.72, 0.08, 0.006), "Lib_PaintBlue")
        # Bench
        for x in (-0.7, 0.7):
            g.box((x, 0.22, 0.28), (0.05, 0.44, 0.32), "Lib_SteelDark", bevel=bev, segs=1)
        for i, z in enumerate((-0.02, 0.10, 0.22)):
            if lod == 1 and i == 1:
                continue
            g.box((0, 0.45, z), (2.05, 0.025, 0.06), "Lib_MetalWorn", bevel=bev, segs=1, uv_scale=1.3)
        # Route blade above the street-side corner.
        g.box((1.52, 2.85, -0.62), (0.06, 0.70, 0.06), "Lib_Steel")
        g.box((1.15, 3.05, -0.62), (0.70, 0.22, 0.025), "Lib_PaintYellow", bevel=bev, segs=1)
        g.box((1.15, 3.05, -0.635), (0.22, 0.10, 0.008), "Lib_Black")
        a.end()
    for i, x in enumerate((-1.52, 1.52)):
        for j, z in enumerate((-0.62, 0.62)):
            a.box("Col_Post_%d%d" % (i, j), (x, 1.18, z), (0.07, 2.30, 0.07))
            a.box("Col_Foot_%d%d" % (i, j), (x, 0.02, z), (0.14, 0.032, 0.14))
    a.box("Col_Roof", (0, 2.44, 0.02), (3.32, 0.05, 1.54))
    a.box("Col_GlassL", (-0.74, 1.70, 0.655), (1.36, 0.78, 0.01))
    a.box("Col_GlassR", (0.74, 1.70, 0.655), (1.36, 0.78, 0.01))
    a.box("Col_GlassSide", (1.56, 1.30, 0.0), (0.01, 1.44, 1.00))
    for i, z in enumerate((-0.02, 0.10, 0.22)):
        a.box("Col_Slat_%d" % i, (0, 0.45, z), (2.00, 0.02, 0.05))
    for i, x in enumerate((-0.7, 0.7)):
        a.box("Col_BenchLeg_%d" % i, (x, 0.22, 0.28), (0.04, 0.40, 0.28))
    a.box("Col_Blade", (1.15, 3.05, -0.62), (0.66, 0.18, 0.02))
    return a
