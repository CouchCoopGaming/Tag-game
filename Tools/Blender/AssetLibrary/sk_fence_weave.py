"""Chain-link terminal bay. Fabric is a galvanized diamond cutout, not a solid sheet.

8 ft between the terminal post and the far line post. Mesh is 2 inch.
A bottom tension wire sits 5 cm off the ground. The terminal post carries a brace.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import polyline

# One diamond repeat along the fence. Matches Lib_ChainMesh.png.
_UV = 1.0 / (0.0508 * (2.0 ** 0.5))


def _post(g, x, radius, height, cap, lod):
    seg = lod_pick(lod, 8, 6)
    g.cylinder((x, height * 0.5, 0), radius, height, "Lib_Steel", seg)
    g.cylinder((x, 0.025, 0), radius + 0.035, 0.05, "Lib_Concrete", 8)
    if cap == "dome":
        g.sphere((x, height + 0.012, 0), radius + 0.008, "Lib_Steel", seg)
    else:
        # Loop cap: a sleeve the top rail runs through.
        g.cylinder((x, 1.83, 0), 0.026, 0.05, "Lib_SteelDark", 8, axis="X")


@register
def create():
    a = Asset(
        "Fence_ChainWeave",
        "StreetFurniture",
        "Chain-link bay 2.44 m. 2 inch galvanized diamond, tension bars, 50 mm bottom wire, knuckled top, terminal brace.",
    )
    a.climb_note = "The fabric is a cutout sheet. Not a solid cling."
    a.vault_note = "Top rail is 1.83 m."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        _post(g, -1.22, 0.030, 1.96, "dome", lod)
        _post(g, 0.0, 0.024, 1.78, "loop", lod)
        _post(g, 1.22, 0.024, 1.78, "loop", lod)
        # Top rail ends in the terminal and runs out through the line-post caps.
        g.cylinder((0.05, 1.83, 0), 0.016, 2.55, "Lib_Steel", seg, axis="X")
        # Bottom tension wire, 5 cm off the ground, inside the fabric.
        g.cylinder((0.0, 0.05, 0.016), 0.0035, 2.36, "Lib_Steel", 6, axis="X")
        # Tension bars bite the fabric and the clamp bands bite the end posts.
        for x in (-1.05, 1.05):
            g.box((x, 0.95, 0.016), (0.012, 1.76, 0.022), "Lib_SteelDark")
        for band in (-1.16, 1.16):
            for y in (0.40, 0.95, 1.55):
                g.box((band, y, 0.014), (0.14, 0.030, 0.040), "Lib_Steel")
        # Fabric runs into the posts. 16 mm thick so an 8 mm collider inset stays inside.
        g.box((0, 0.95, 0.016), (2.40, 1.82, 0.016), "Lib_ChainMesh", bevel=0, uv_scale=_UV)
        # Brace on the back face, from the terminal post up to the top rail.
        polyline(g, [(-1.18, 1.42, -0.02), (-0.55, 1.83, -0.012)], 0.012, "Lib_Steel", seg)
        g.cylinder((-1.20, 1.42, -0.012), 0.040, 0.028, "Lib_SteelDark", 8)
        if lod == 0:
            # Knuckle the fabric over the top rail.
            x = -0.96
            while x <= 0.96:
                polyline(
                    g,
                    [(x, 1.78, 0.020), (x, 1.90, 0.0), (x, 1.78, -0.010)],
                    0.0032,
                    "Lib_Chain",
                    4,
                )
                x += 0.16
            # Ties along the top rail and the bottom wire.
            for x in (-0.8, -0.4, 0.4, 0.8):
                g.pipe((x, 1.74, 0.018), (x, 1.84, 0.0), 0.0025, "Lib_Steel", 4)
                g.pipe((x, 0.05, 0.016), (x, 0.12, 0.016), 0.0025, "Lib_Steel", 4)
            g.pipe((0.0, 0.55, 0.012), (0.0, 0.55, 0.020), 0.0025, "Lib_Steel", 4)
            g.pipe((0.0, 1.20, 0.012), (0.0, 1.20, 0.020), 0.0025, "Lib_Steel", 4)
        a.end()
    a.capsule("Col_PostT", (-1.22, 0.95, 0), 0.016, 1.50)
    a.capsule("Col_PostL", (0.0, 0.90, 0), 0.012, 1.40)
    a.capsule("Col_PostR", (1.22, 0.90, 0), 0.012, 1.40)
    # Thin boxes in the fabric, clear of the line post, the tension bars, and the wires.
    a.box("Col_MeshL", (-0.55, 0.96, 0.016), (0.86, 1.36, 0.010))
    a.box("Col_MeshR", (0.55, 0.96, 0.016), (0.86, 1.36, 0.010))
    return a
