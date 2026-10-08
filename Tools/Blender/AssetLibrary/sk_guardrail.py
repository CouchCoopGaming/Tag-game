"""Short W-beam guardrail. Two posts and a corrugated rail. Not the harbor railing."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Guardrail_WBeam",
        "StreetFurniture",
        "W-beam bay, 2.40 m rail, posts to 0.72 m. Corrugated face toward +Z.",
    )
    a.climb_note = "Posts are 8 cm. The rail is not a cling."
    a.vault_note = "Rail top is 0.70 m."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        for x in (-0.90, 0.90):
            g.box((x, 0.012, 0.0), (0.18, 0.024, 0.16), "Lib_SteelDark", bevel=bev, segs=bs)
            g.box((x, 0.37, 0.0), (0.08, 0.70, 0.06), "Lib_Steel", bevel=bev, segs=bs)
            # Blockout holds the rail off the post.
            g.box((x, 0.55, 0.055), (0.06, 0.18, 0.05), "Lib_SteelDark")
        # Back plate. Ribs sit on its +Z face.
        g.box((0, 0.55, 0.092), (2.36, 0.30, 0.016), "Lib_Steel", bevel=bev, segs=bs)
        for y, depth in ((0.46, 0.04), (0.55, 0.055), (0.64, 0.04)):
            g.box((0, y, 0.10 + depth * 0.35), (2.28, 0.045, depth), "Lib_Steel")
        g.box((0, 0.70, 0.10), (2.28, 0.02, 0.03), "Lib_Steel")
        g.box((0, 0.40, 0.10), (2.28, 0.02, 0.03), "Lib_Steel")
        if lod == 0:
            for x in (-1.14, 1.14):
                g.box((x, 0.55, 0.118), (0.06, 0.10, 0.012), "Lib_PaintYellow")
        a.end()
    a.box("Col_PostL", (-0.90, 0.36, 0.0), (0.05, 0.60, 0.04))
    a.box("Col_PostR", (0.90, 0.36, 0.0), (0.05, 0.60, 0.04))
    a.box("Col_Rail", (0, 0.55, 0.092), (2.05, 0.20, 0.010))
    return a
