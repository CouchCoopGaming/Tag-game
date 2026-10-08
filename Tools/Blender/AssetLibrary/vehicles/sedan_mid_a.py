"""Midsize sedan line A. Two fascias on one body, factory colors.

Sheet inches, later fascia (2025 family-sedan e-brochure exterior page):
height 56.9, width 72.4, length 193.5, wheelbase 111.2,
ground clearance 5.4, track front/rear 63.0/63.7.
LE tire P205/65R16, 10-spoke.
The sheet does not print overhang. Front overhang is 39.0 in so the
rear overhang is the 43.3 in remainder of length minus wheelbase.

Earlier fascia (2021 LE column on that year's e-brochure):
length 192.1, width 72.4, height 56.9, wheelbase 111.2,
clearance 5.7, track 62.6/62.8, tire P215/55R17.
The 1.4 in length change is overhang: 0.9 in off the nose, 0.5 in off the tail.

No badges. Asset blurbs do not use the sheet's brand.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from _common import PALETTE, Asset, register
import shell
import variant_picker

INCH = 0.0254

PALETTE.setdefault("Lib_PaintBlack", ((0.015, 0.015, 0.016), 0.55, 0.62))
PALETTE.setdefault("Lib_PaintGrey", ((0.27, 0.28, 0.29), 0.45, 0.50))
PALETTE.setdefault("Lib_PaintSilver", ((0.68, 0.70, 0.72), 0.72, 0.58))
PALETTE.setdefault("Lib_PaintNavy", ((0.07, 0.14, 0.30), 0.42, 0.52))
PALETTE.setdefault("Lib_PaintOcean", ((0.04, 0.40, 0.44), 0.28, 0.50))

PAINT = {
    "navy": "Lib_PaintNavy",
    "white": "Lib_PaintWhite",
    "black": "Lib_PaintBlack",
    "grey": "Lib_PaintGrey",
    "silver": "Lib_PaintSilver",
    "red": "Lib_PaintRed",
    "ocean": "Lib_PaintOcean",
}

# Built once per fascia, then recolored. Blender booleans are the slow part.
_BUILT = {}


def _year_spec(year, paint):
    if year == 2021:
        return shell.make_sedan(
            length=192.1 * INCH,
            width=72.4 * INCH,
            height=56.9 * INCH,
            wheelbase=111.2 * INCH,
            track=62.6 * INCH,
            front_overhang=38.1 * INCH,
            tire_radius=(17.0 * INCH + 2.0 * 0.215 * 0.55) * 0.5,
            tire_width=0.215,
            nose_y=0.76,
            paint=paint,
            name="Sedan_Mid_A_21",
            deck_ratio=0.72,
            roof_span=0.24,
            cowl_setback=0.40,
            belly=5.7 * INCH,
            cap_inset=0.058,
            tumble=0.07,
            fascia="split",
            spokes=5,
            tread=True,
            panel_gaps=True,
            cabin=True,
            wipers=True,
            rim_ratio=0.66,
        )
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
        paint=paint,
        name="Sedan_Mid_A",
        deck_ratio=0.72,
        roof_span=0.24,
        cowl_setback=0.40,
        belly=5.4 * INCH,
        cap_inset=0.058,
        tumble=0.07,
        fascia="bar",
        spokes=10,
        tread=True,
        panel_gaps=True,
        cabin=True,
        wipers=True,
        rim_ratio=0.60,
    )


def _blurb(year, color):
    if year == 2021:
        return (
            "Midsize sedan, earlier fascia, 4.879 m long, 1.839 m wide, roof 1.445 m. "
            "Flush glass, panel gaps, five-spoke alloys. Color %s." % color
        )
    return (
        "Midsize sedan, later fascia, 4.915 m long, 1.839 m wide, roof 1.445 m. "
        "Flush glass, panel gaps, ten-spoke alloys. Color %s." % color
    )


def _build(year, color):
    paint = PAINT[color]
    spec = _year_spec(year, paint)
    asset = Asset("pending", "Vehicles", _blurb(year, color))
    asset.climbable = True
    asset.climb_note = "Roof and hood are climbable sheet metal."
    asset.vault_note = "Hood and roof are landings, not vault rails."
    for lod in (0, 1, 2):
        geo = asset.begin(lod)
        shell.build_sedan(geo, spec, lod)
        asset.end()
    shell.add_sedan_colliders(asset, spec)
    asset._sedan_spec = spec
    return asset


def _clone(src, name, blurb, new_paint):
    old_paint = src._sedan_spec["paint"]
    dst = Asset(name, "Vehicles", blurb)
    dst.climbable = True
    dst.climb_note = src.climb_note
    dst.vault_note = src.vault_note
    dst.colliders = [dict(col) for col in src.colliders]
    dst._sedan_spec = dict(src._sedan_spec)
    dst._sedan_spec["paint"] = new_paint
    dst._sedan_spec["name"] = name
    for lod, geo in src.lods.items():
        g = dst.begin(lod)
        index = []
        for mat in geo.mats:
            index.append(g.slot(new_paint if mat == old_paint else mat))
        vmap = {}
        for vert in geo.bm.verts:
            vmap[vert] = g.bm.verts.new(vert.co.copy())
        g.bm.verts.index_update()
        for face in geo.bm.faces:
            try:
                nf = g.bm.faces.new([vmap[vert] for vert in face.verts])
            except ValueError:
                continue
            nf.material_index = index[face.material_index]
            nf.smooth = face.smooth
            nf[g.scale_layer] = face[geo.scale_layer]
        dst.end()
    return dst


def create_variants():
    if _BUILT:
        return list(_BUILT.values())
    masters = {}
    for row in variant_picker.VARIANTS:
        year = row["year"]
        if year not in masters:
            # First row of that year is the boolean master.
            masters[year] = (row["color"], _build(year, row["color"]))
        color, master = masters[year]
        if row["color"] == color:
            asset = master
            asset.name = row["name"]
            asset.blurb = _blurb(year, row["color"])
        else:
            asset = _clone(master, row["name"], _blurb(year, row["color"]), PAINT[row["color"]])
        _BUILT[row["name"]] = asset
    return list(_BUILT.values())


@register
def create():
    return create_variants()[0]
