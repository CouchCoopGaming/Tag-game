"""Compact hatch shell.

Published 2025 compact-hatch exterior, converted from inches:
length 179.0, width 70.9, height 55.7, wheelbase 107.7,
track 60.5 front. The hatch is 5.8 in shorter than the compact
sedan on the same wheelbase, so the front overhang stays 35.5 in
and the cut is in the tail. Tire 235/40R18. No badges or brand marks.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from _common import Asset, register
import shell

INCH = 0.0254


def spec():
    return shell.make_sedan(
        length=179.0 * INCH,
        width=70.9 * INCH,
        height=55.7 * INCH,
        wheelbase=107.7 * INCH,
        track=60.5 * INCH,
        front_overhang=35.5 * INCH,
        tire_radius=(18.0 * INCH + 2.0 * 0.235 * 0.40) * 0.5,
        tire_width=0.235,
        nose_y=0.80,
        paint="Lib_PaintGreen",
        name="Hatch_Compact",
        deck_ratio=0.86,
        roof_span=0.30,
        cowl_setback=0.36,
        belly=0.15,
        c_pillar_deg=46.0,
    )


@register
def create():
    body = spec()
    asset = Asset(
        "Hatch_Compact",
        "Vehicles",
        "Compact hatch shell, 4.547 m long, 1.801 m wide, roof 1.415 m. Fastback tail, flush glass.",
    )
    asset.climb_note = "Sheet metal. Not a cling wall."
    asset.vault_note = "Hood and roof are landings, not vault rails."
    for lod in (0, 1):
        geo = asset.begin(lod)
        shell.build_sedan(geo, body, lod)
        asset.end()
    shell.add_sedan_colliders(asset, body)
    asset._sedan_spec = body
    return asset
