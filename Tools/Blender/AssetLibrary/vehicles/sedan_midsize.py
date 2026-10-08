"""Midsize sedan shell.

Published 2025 family-sedan exterior, converted from inches:
length 193.5, width 72.4, height 56.9, wheelbase 111.2, track 63.0,
front overhang 39.0. Tire 205/65R16. No badges or brand marks.
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
        length=193.5 * INCH,
        width=72.4 * INCH,
        height=56.9 * INCH,
        wheelbase=111.2 * INCH,
        track=63.0 * INCH,
        front_overhang=39.0 * INCH,
        tire_radius=(16.0 * INCH + 2.0 * 0.205 * 0.65) * 0.5,
        tire_width=0.205,
        nose_y=0.78,
        paint="Lib_PaintBlue",
        name="Sedan_Midsize",
    )


@register
def create():
    body = spec()
    asset = Asset(
        "Sedan_Midsize",
        "Vehicles",
        "Midsize sedan shell, 4.915 m long, 1.839 m wide, roof 1.445 m. Flush glass, two doors a side, lower grille.",
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
