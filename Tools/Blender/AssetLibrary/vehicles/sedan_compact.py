"""Compact sedan shell.

Published 2025 compact-sedan exterior, converted from inches:
length 184.8, width 70.9, height 55.7, wheelbase 107.7, track 60.9,
front overhang 35.5. Tire 215/55R16. No badges or brand marks.
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
        length=184.8 * INCH,
        width=70.9 * INCH,
        height=55.7 * INCH,
        wheelbase=107.7 * INCH,
        track=60.9 * INCH,
        front_overhang=35.5 * INCH,
        tire_radius=(16.0 * INCH + 2.0 * 0.215 * 0.55) * 0.5,
        tire_width=0.215,
        nose_y=0.74,
        paint="Lib_PaintRed",
        name="Sedan_Compact_25",
    )


@register
def create():
    body = spec()
    asset = Asset(
        "Sedan_Compact_25",
        "Vehicles",
        "2025 compact sedan shell, 4.694 m long, 1.801 m wide, roof 1.415 m. Flush glass, two doors a side, lower grille.",
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
