"""Midsize sedan line A, model years 2021 through 2025.

One body shell. 2025 is the new fascia (separate swept lamps, wide grille).
2021-2024 keep that shell and change the grille, lamp signature, wheel, and
mirror color. Every year is the crimson base. Extra colors are clones of 2025.

Sheet inches, 2025 family-sedan exterior page: height 56.9, width 72.4,
length 193.5, wheelbase 111.2, ground clearance 5.4, track 63.0.
The sheet does not print overhang. Front overhang is 39.0 in.
2021-2024 were 1.4 in shorter in the earlier brochure; this line shares the
2025 shell so the fascia is what changes. No badges.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from _common import Asset, register
import body_a
import variant_picker

PAINT = {
    "crimson": "Lib_PaintCrimson",
    "white": "Lib_PaintWhite",
    "black": "Lib_PaintBlack",
    "grey": "Lib_PaintGrey",
    "silver": "Lib_PaintSilver",
    "navy": "Lib_PaintNavy",
    "ocean": "Lib_PaintOcean",
}

_BUILT = {}


def _blurb(year, color):
    return (
        "Midsize sedan, %d fascia, 4.90 m long, 1.84 m wide, roof 1.45 m. "
        "Raked windshield, fastback pillar, 18 inch alloys. Color %s."
        % (year, color)
    )


def _finish(src_shell, year, color, name):
    paint = PAINT[color]
    asset = Asset(name, "Vehicles", _blurb(year, color))
    asset.climbable = True
    asset.climb_note = "Roof and hood are climbable sheet metal."
    asset.vault_note = "Hood and roof are landings, not vault rails."
    for lod in (0, 1, 2):
        geo = body_a._copy_open(src_shell.lods[lod], asset, lod)
        body_a.dress(geo, year, lod, paint)
        asset.end()
    body_a.add_colliders(asset)
    asset._sedan_spec = body_a.probe_spec(name)
    return asset


def _clone(src, name, blurb, new_paint):
    old_paint = "Lib_PaintCrimson"
    dst = Asset(name, "Vehicles", blurb)
    dst.climbable = True
    dst.climb_note = src.climb_note
    dst.vault_note = src.vault_note
    dst.colliders = [dict(col) for col in src.colliders]
    dst._sedan_spec = body_a.probe_spec(name)
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
    shell_asset = body_a.template(Asset)
    masters = {}
    for row in variant_picker.VARIANTS:
        year = row["year"]
        if year not in masters:
            masters[year] = _finish(shell_asset, year, "crimson", "Sedan_Mid_A_%02d" % (year % 100))
            if year in (2021, 2025):
                body_a.measure(masters[year])
        if row["color"] == "crimson":
            asset = masters[year]
            asset.name = row["name"]
            asset.blurb = _blurb(year, row["color"])
            asset._sedan_spec = body_a.probe_spec(row["name"])
        else:
            asset = _clone(masters[year], row["name"], _blurb(year, row["color"]), PAINT[row["color"]])
        _BUILT[row["name"]] = asset
    return list(_BUILT.values())


@register
def create():
    return create_variants()[0]
