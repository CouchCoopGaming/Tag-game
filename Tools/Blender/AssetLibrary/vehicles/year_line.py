"""Model years 2022 through 2026 for each vehicle line.

2025 shells that already shipped stay on disk. This module builds the
missing years. Each year keeps the line's envelope and changes the lamps,
the grille, or the wheel. No badges.
"""

import copy
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from _common import Asset
import body_a
import bus
import hatch_compact
import crossover_compact
import pickup_fullsize
import sedan_compact
import sedan_mid_a
import shell

# fascia, spokes, grille slats. None keeps that fascia's own slat count.
# 2025 is the shipped shell and is not rebuilt here.
SHELL_YEARS = (
    (2022, "split", 6, None),
    (2023, "classic", 7, 3),
    (2024, "bar", 5, 5),
    (2026, "bar", 6, 3),
)
FASCIA_WORD = {
    "split": "separate swept lamps",
    "classic": "upright lamps",
    "bar": "a full-width lamp bar",
}
# spokes, grille slats. 2025 stays at 5 spokes and 4 slats.
PICKUP_YEARS = (
    (2022, 6, 5),
    (2023, 7, 3),
    (2024, 4, 6),
    (2026, 8, 2),
)
# spokes, engine-door louvers. The unyeared bus stays the paint master.
BUS_YEARS = (
    (2022, 6, 5),
    (2023, 7, 3),
    (2024, 4, 6),
    (2025, 6, 2),
    (2026, 7, 6),
)


def _shell_asset(spec_fn, prefix, year, fascia, spokes, slats, blurb):
    name = "%s_%02d" % (prefix, year % 100)
    body = spec_fn()
    body["name"] = name
    body["fascia"] = fascia
    body["spokes"] = spokes
    if slats is not None:
        body["slats"] = slats
    asset = Asset(name, "Vehicles", blurb)
    asset.climb_note = "Sheet metal. Not a cling wall."
    asset.vault_note = "Hood and roof are landings, not vault rails."
    for lod in (0, 1, 2):
        geo = asset.begin(lod)
        shell.build_sedan(geo, body, lod)
        asset.end()
    shell.add_sedan_colliders(asset, body)
    asset._sedan_spec = body
    return asset


def _compact_sedans():
    out = []
    for year, fascia, spokes, slats in SHELL_YEARS:
        blurb = (
            "%d compact sedan. %s, %d-spoke wheels. Body 4.66 m long and 1.74 m wide "
            "so the mirrors and tires stay inside 4.70 x 1.82 m. Roof 1.415 m."
            % (year, FASCIA_WORD[fascia].capitalize(), spokes)
        )
        out.append(_shell_asset(sedan_compact.spec, "Sedan_Compact", year, fascia, spokes, slats, blurb))
    return out


def _hatches():
    out = []
    for year, fascia, spokes, slats in SHELL_YEARS:
        blurb = (
            "%d compact hatch. %s, %d-spoke wheels. Body 4.42 m long and 1.76 m wide "
            "so the full shell stays inside 4.50 x 1.85 m. Roof 1.415 m. Fastback tail."
            % (year, FASCIA_WORD[fascia].capitalize(), spokes)
        )
        out.append(_shell_asset(hatch_compact.spec, "Hatch_Compact", year, fascia, spokes, slats, blurb))
    return out


def _crossovers():
    out = []
    for year, fascia, spokes, slats in SHELL_YEARS:
        blurb = (
            "%d compact crossover. %s, %d-spoke wheels. Body 4.66 m long and 1.80 m wide "
            "so the tires stay inside 4.70 x 1.90 m. Roof 1.681 m. High hatch, dark rocker cladding."
            % (year, FASCIA_WORD[fascia].capitalize(), spokes)
        )
        out.append(_shell_asset(crossover_compact.spec, "Crossover_Compact", year, fascia, spokes, slats, blurb))
    return out


def _pickups():
    out = []
    for year, spokes, slats in PICKUP_YEARS:
        name = "Pickup_FullSize_%02d" % (year % 100)
        spec = copy.deepcopy(pickup_fullsize.SPEC)
        spec["spokes"] = spokes
        spec["slats"] = slats
        asset = Asset(
            name,
            "Vehicles",
            "%d full-size pickup, about 5.06 m long. %d-spoke wheels and a %d-bar grille. "
            "Cab roof 1.76 m, bed rails 1.12 m. Closed cab and wrapped fenders."
            % (year, spokes, slats),
        )
        asset.climb_note = "Sheet metal. Not a cling wall."
        asset.vault_note = "Bed sides are about 1.12 m. Not a vault rail."
        saved = pickup_fullsize.SPEC
        pickup_fullsize.SPEC = spec
        try:
            for lod in (0, 1, 2):
                geo = asset.begin(lod)
                pickup_fullsize.body.build(geo, spec, lod)
                asset.end()
        finally:
            pickup_fullsize.SPEC = saved
        asset.box("Col_Cab", (0, 0.82, 0.78), (0.90, 0.48, 1.00))
        asset.box("Col_Roof", (0, 1.70, 0.40), (0.36, 0.04, 0.32))
        asset.box("Col_Bed", (0, 0.55, -1.20), (1.20, 0.02, 1.60))
        asset.box("Col_SideL", (-0.845, 0.98, -1.30), (0.02, 0.12, 1.70))
        asset.box("Col_SideR", (0.845, 0.98, -1.30), (0.02, 0.12, 1.70))
        asset.box("Col_Hood", (0, 0.66, 1.62), (0.84, 0.32, 0.84))
        asset.box("Col_Nose", (0, 0.43, 1.81), (0.84, 0.18, 1.22))
        asset.box("Col_FenderL", (-0.64, 0.46, 2.18), (0.28, 0.12, 0.36))
        asset.box("Col_FenderR", (0.64, 0.46, 2.18), (0.28, 0.12, 0.36))
        asset.box("Col_Bumper", (0, 0.40, 2.488), (1.20, 0.08, 0.010))
        asset.box("Col_Tailgate", (0, 0.7875, -2.568), (0.90, 0.405, 0.014))
        pickup_fullsize._wheel_cols(asset, (1.58,), "Fr")
        pickup_fullsize._wheel_cols(asset, (-1.62,), "Rr")
        out.append(asset)
    return out


def _buses():
    out = []
    for year, spokes, louvers in BUS_YEARS:
        suffix = "%02d" % (year % 100)
        out.append(bus.create_city40(
            "Bus_City40_%s" % suffix,
            "Lib_PaintCream",
            "Lib_PaintBlue",
            "%d low-floor city bus, 12.50 m over bumpers, 2.59 m wide, roof unit 3.20 m. "
            "%d-spoke wheels, %d engine-door louvers. Cream body, blue skirt, curb-side doors."
            % (year, spokes, louvers),
            spokes=spokes,
            louvers=louvers,
        ))
        out.append(bus.create_city60(
            "Bus_City60_%s" % suffix,
            "Lib_PaintCream",
            "Lib_PaintBlue",
            "%d articulated low-floor city bus, 18.54 m over bumpers, 2.59 m wide, roof unit 3.20 m. "
            "%d-spoke wheels, %d engine-door louvers. Three axles, turntable cover, curb-side doors."
            % (year, spokes, louvers),
            spokes=spokes,
            louvers=louvers,
        ))
    return out


def _midsize_2026():
    shell_asset = body_a.template(Asset)
    return sedan_mid_a._finish(shell_asset, 2026, "crimson", "Sedan_Mid_A_26")


def create_variants():
    assets = [_midsize_2026()]
    assets.extend(_compact_sedans())
    assets.extend(_hatches())
    assets.extend(_crossovers())
    assets.extend(_pickups())
    assets.extend(_buses())
    return assets
