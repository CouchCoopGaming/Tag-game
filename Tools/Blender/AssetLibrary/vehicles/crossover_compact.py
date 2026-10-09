"""Compact crossover shell.

Published 2025 compact-crossover exterior, converted from inches:
length 184.8, width 73.5, height 66.2, wheelbase 106.3, track 63.4,
front overhang 35.0, ground clearance 7.8. Tire 235/65R17.
No badges or brand marks. Higher roof and a short hatch, not a sedan deck.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from _common import Asset, register
import shell

INCH = 0.0254


def spec():
    body = shell.make_sedan(
        length=4.66,
        width=1.80,
        height=66.2 * INCH,
        wheelbase=106.3 * INCH,
        track=63.4 * INCH,
        front_overhang=35.0 * INCH,
        tire_radius=(17.0 * INCH + 2.0 * 0.235 * 0.65) * 0.5,
        tire_width=0.235,
        nose_y=1.02,
        paint="Lib_PaintCream",
        name="Crossover_Compact_25",
        deck_ratio=0.90,
        roof_span=0.32,
        cowl_setback=0.62,
        belly=0.20,
    )
    body["cladding"] = True
    body["rocker_y"] = 0.36
    return body


@register
def create():
    body = spec()
    asset = Asset(
        "Crossover_Compact_25",
        "Vehicles",
        "2025 compact crossover. Body 4.66 m long and 1.80 m wide so the tires stay inside 4.70 x 1.90 m. Roof 1.681 m. High hatch, dark rocker cladding, flush glass.",
    )
    asset.climb_note = "Sheet metal. Not a cling wall."
    asset.vault_note = "Hood and roof are landings, not vault rails."
    for lod in (0, 1, 2):
        geo = asset.begin(lod)
        shell.build_sedan(geo, body, lod)
        asset.end()
    shell.add_sedan_colliders(asset, body)
    asset._sedan_spec = body
    return asset
