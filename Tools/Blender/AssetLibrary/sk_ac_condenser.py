"""Ground AC condenser. Fan on the top, unlike the wall mini-split.

Small unit is about a 2-ton: 0.66 m square, 0.70 m tall, on a 40 mm pad.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _shell(span, height):
    """Pad, cabinet, and lid in meters. Lid crown is the stand surface."""
    pad_top = 0.04
    body_top = pad_top + height - 0.004
    body_center = (pad_top - 0.004 + body_top) * 0.5
    body_h = body_top - (pad_top - 0.004)
    lid_h = 0.028
    lid_center = body_top + lid_h * 0.5 - 0.004
    crown = lid_center + lid_h * 0.5
    return pad_top, body_center, body_h, body_top, lid_center, lid_h, crown


def build_condenser(g, lod, span, height, fans):
    seg = lod_pick(lod, 14, 8)
    bev = lod_pick(lod, 0.004, 0.0)
    bs = 1 if lod == 0 else 0
    pad = span + 0.12
    pad_top, body_center, body_h, body_top, lid_center, lid_h, crown = _shell(span, height)
    g.box((0, pad_top * 0.5, 0), (pad, pad_top, pad), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.6)
    g.box((0, body_center, 0), (span, body_h, span), "Lib_PaintWhite", bevel=bev, segs=bs)
    g.box((0, lid_center, 0), (span + 0.02, lid_h, span + 0.02), "Lib_SteelDark", bevel=bev, segs=bs)
    step = span / (fans + 1)
    fan_r = min(0.20, span * 0.26)
    for i in range(fans):
        x = -span * 0.5 + step * (i + 1)
        # 2 mm bite into the lid so the housing is attached, clear of Col_Top.
        g.cylinder((x, crown + 0.006, 0), fan_r, 0.016, "Lib_SteelDark", seg)
        g.cylinder((x, crown + 0.020, 0), fan_r * 0.32, 0.014, "Lib_Black", 8)
        if lod == 0:
            g.torus((x, crown + 0.008, 0), fan_r * 0.62, 0.005, "Lib_Steel", 12, 5)
    if lod == 0:
        face = span * 0.5
        for y in (0.22, 0.36, 0.52, 0.66):
            if y >= body_top - 0.04:
                continue
            # Proud of the sheet, 2 mm overlap, so the louver is not a volume through the wall.
            g.box((face + 0.004, y, 0), (0.012, 0.014, span * 0.62), "Lib_SteelDark")
            g.box((-face - 0.004, y, 0), (0.012, 0.014, span * 0.62), "Lib_SteelDark")
        stub_x = face + 0.024
        stub_z = -span * 0.18
        # Flange bites the cabinet. The stub overlaps the flange and stays outside the wall.
        g.box((face + 0.008, 0.42, stub_z), (0.024, 0.11, 0.09), "Lib_Steel", bevel=0.002, segs=1)
        g.cylinder((stub_x, 0.40, stub_z), 0.018, 0.16, "Lib_Steel", 8)
        g.pipe((stub_x, 0.46, stub_z), (stub_x + 0.12, 0.46, stub_z), 0.014, "Lib_Steel", 6)
        g.box((0.0, 0.28, face + 0.004), (0.14, 0.045, 0.012), "Lib_Rust")


def _asset(name, blurb, span, height, fans):
    a = Asset(name, "StreetFurniture", blurb)
    a.climb_note = "Louvered sheet metal. Not a cling wall."
    a.vault_note = "Top is low and full of fans. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        build_condenser(g, lod, span, height, fans)
        a.end()
    _pad_top, body_center, body_h, _body_top, _lid_center, _lid_h, crown = _shell(span, height)
    a.box("Col_Pad", (0, 0.02, 0), (span + 0.04, 0.024, span + 0.04))
    a.box("Col_Box", (0, body_center, 0), (span - 0.12, body_h - 0.10, span - 0.12))
    # 8 mm under the lid crown, below the fan housings.
    a.box("Col_Top", (0, crown - 0.014, 0), (span - 0.08, 0.012, span - 0.08))
    return a


@register
def create():
    return _asset(
        "AC_Condenser_Small",
        "Ground condenser, 0.66 m square, 0.70 m tall, one top fan, on a 40 mm pad.",
        0.66, 0.70, 1,
    )
