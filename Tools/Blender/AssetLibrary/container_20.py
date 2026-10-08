"""ISO 20-foot shipping container. 6.06 x 2.44 x 2.59 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    return build_container(
        "Container_20",
        6.06,
        "Lib_ContainerRed",
        "20-foot container, 6.06 m long, 2.44 m wide, 2.59 m tall. Doors face +Z. Corrugation stands proud of the wall plate.",
    )


def build_container(name, length, color, blurb):
    a = Asset(name, "Harbor", blurb)
    a.climbable = True
    a.climb_note = "Long sides are cling. Door hardware is on +Z. Collider is the wall plate; ribs stand about 2 cm proud."
    a.vault_note = "No rail. Roof is a landing at 2.59 m."
    width, height = 2.44, 2.59
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0
        g.box((0, height * 0.5, 0), (width - 0.08, height - 0.08, length - 0.12), color, uv_scale=0.4)
        # Corner posts.
        for x in (-width * 0.5 + 0.06, width * 0.5 - 0.06):
            for z in (-length * 0.5 + 0.06, length * 0.5 - 0.06):
                g.box((x, height * 0.5, z), (0.12, height, 0.12), "Lib_SteelDark", bevel=bev, segs=1)
        # Top and bottom rails.
        for y in (0.06, height - 0.06):
            g.box((0, y, 0), (width, 0.10, length), "Lib_SteelDark", uv_scale=0.5)
        ribs = lod_pick(lod, 18 if length < 8 else 32, 8 if length < 8 else 14, 0)
        if ribs:
            _ribs(g, length, width, height, ribs, color)
        _doors(g, length, width, height, lod, bev)
        if lod == 0:
            g.box((0, height + 0.02, 0), (0.5, 0.04, 0.9), "Lib_Steel", bevel=0.004, segs=1)
        a.end()
    # Wall plate, not the rib tips. Ribs stand about 2 cm proud of this box.
    a.box("Climb_Body", (0, height * 0.5, 0), (width - 0.12, height - 0.08, length - 0.12))
    return a


def _ribs(g, length, width, height, count, color):
    z0 = -length * 0.5 + 0.28
    z1 = length * 0.5 - 0.28
    for i in range(count):
        z = z0 + (z1 - z0) * (i / max(1, count - 1))
        g.box((-width * 0.5 - 0.012, height * 0.5, z), (0.025, height - 0.28, 0.06), color)
        g.box((width * 0.5 + 0.012, height * 0.5, z), (0.025, height - 0.28, 0.06), color)
    roof_n = max(3, count // 2)
    for i in range(roof_n):
        z = z0 + (z1 - z0) * (i / max(1, roof_n - 1))
        g.box((0, height + 0.012, z), (width - 0.28, 0.025, 0.08), color)


def _doors(g, length, width, height, lod, bev):
    z = length * 0.5 - 0.02
    g.box((-width * 0.22, height * 0.48, z), (width * 0.42, height * 0.82, 0.04), "Lib_SteelDark", bevel=bev, segs=1, uv_scale=0.6)
    g.box((width * 0.22, height * 0.48, z), (width * 0.42, height * 0.82, 0.04), "Lib_SteelDark", bevel=bev, segs=1, uv_scale=0.6)
    if lod < 2:
        for x in (-0.18, 0.18):
            g.box((x, height * 0.48, z + 0.03), (0.04, height * 0.78, 0.03), "Lib_Steel", bevel=bev, segs=1)
            g.box((x, height * 0.22, z + 0.05), (0.10, 0.08, 0.04), "Lib_Brass")
            g.box((x, height * 0.72, z + 0.05), (0.10, 0.08, 0.04), "Lib_Brass")
    if lod == 0:
        g.box((0.0, height * 0.55, z + 0.045), (0.06, 0.16, 0.03), "Lib_PaintYellow")
