"""Harbor passenger ferry. Bow on -Z. Not the cabin cruiser and not the fishing boat."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick, unity_to_blender
from _nudge import shift_z


@register
def create():
    a = Asset(
        "Ferry",
        "Harbor",
        "Passenger ferry, 12.4 m overall, beam 4.0 m. Bow is -Z. Open foredeck, wheelhouse aft. "
        "The hull extends below the pivot.",
    )
    a.allow_below = True
    a.climb_note = "The hull side and the wheelhouse wall are cling faces. The foredeck is the standing surface."
    a.vault_note = "Rail is about 1.05 m above the deck."
    for lod in (0, 1):
        g = a.begin(lod)
        _hull(g)
        _deck(g)
        _cabin(g, lod)
        if lod < 2:
            _rails(g, lod)
        a.end()
    a.box("Col_Hull", (0.0, -0.05, 0.35), (3.20, 1.05, 9.40))
    a.box("Col_Bow", (0.0, -0.02, -5.05), (1.50, 0.60, 0.70))
    a.box("Col_Deck", (0.0, 0.575, 0.20), (3.30, 0.035, 9.80))
    a.box("Col_CabinLow", (0.0, 1.15, 3.55), (2.30, 0.78, 2.20))
    a.box("Col_CabinHigh", (0.0, 2.15, 3.55), (2.20, 0.86, 2.10))
    a.box("Col_Roof", (0.0, 2.78, 3.55), (2.50, 0.08, 2.50))
    a.box("Col_RailL", (-1.78, 1.15, -1.3), (0.06, 0.70, 6.4), approx=True)
    a.box("Col_RailR", (1.78, 1.15, -1.3), (0.06, 0.70, 6.4), approx=True)
    shift_z(a, 0.45)
    return a


def _hull(g):
    # Main hull. Waterline is y = 0. Keel is below the pivot.
    g.box((0.0, -0.08, 0.40), (3.60, 1.26, 9.80), "Lib_PaintWhite", uv_scale=0.35)
    _bow(g)
    for sign in (-1, 1):
        g.box((sign * 1.84, -0.08, 0.40), (0.05, 0.24, 9.60), "Lib_PaintRed", uv_scale=0.35)
        g.box((sign * 1.86, 0.42, 0.40), (0.05, 0.06, 9.50), "Lib_Rubber")


def _bow(g):
    """Closed wedge. Wide face butts the hull with 8 mm of air. Narrow face is the bow."""
    z_aft, z_bow = -4.508, -6.20
    x_aft, x_bow = 1.55, 0.35
    y_bot, y_top = -0.45, 0.48
    verts = [
        (-x_aft, y_bot, z_aft), (x_aft, y_bot, z_aft), (x_bow, y_bot, z_bow), (-x_bow, y_bot, z_bow),
        (-x_aft, y_top, z_aft), (x_aft, y_top, z_aft), (x_bow, y_top, z_bow), (-x_bow, y_top, z_bow),
    ]
    faces = (
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    )
    bm = g.bm
    mi = g.slot("Lib_PaintWhite")
    made = [bm.verts.new(unity_to_blender(*v)) for v in verts]
    bm.verts.index_update()
    for idxs in faces:
        nf = bm.faces.new([made[i] for i in idxs])
        nf.material_index = mi
        nf.smooth = False
        nf[g.scale_layer] = 0.4
        nf[g.grain_layer] = 0.0


def _deck(g):
    g.box((0.0, 0.575, 0.20), (3.40, 0.04, 10.00), "Lib_Concrete", uv_scale=0.4)


def _cabin(g, lod):
    # Wheelhouse aft, clear of the deck top.
    g.box((0.0, 1.15, 3.55), (2.50, 0.90, 2.40), "Lib_PaintWhite", uv_scale=0.5)
    g.box((0.0, 2.15, 3.55), (2.40, 1.00, 2.30), "Lib_PaintBlue", uv_scale=0.5)
    g.box((0.0, 2.78, 3.55), (2.70, 0.10, 2.60), "Lib_PaintWhite", uv_scale=0.45)
    if lod == 0:
        g.box((0.0, 1.70, 4.78), (1.40, 0.55, 0.03), "Lib_Glass")
        g.box((1.28, 1.70, 3.55), (0.03, 0.50, 1.10), "Lib_Glass")
        g.cylinder((0.0, 3.05, 3.20), 0.06, 0.40, "Lib_SteelDark", 8)
    elif lod == 1:
        g.box((0.0, 1.70, 4.78), (1.40, 0.55, 0.03), "Lib_Glass")


def _rails(g, lod):
    step = lod_pick(lod, 1.15, 0.0)
    for sign in (-1, 1):
        x = sign * 1.78
        g.box((x, 1.26, -1.3), (0.05, 0.04, 6.6), "Lib_Steel")
        if step <= 0:
            g.box((x, 0.96, -1.3), (0.04, 0.58, 6.6), "Lib_SteelDark")
            continue
        z = -4.4
        while z <= 1.81:
            g.box((x, 0.94, z), (0.04, 0.58, 0.04), "Lib_SteelDark")
            z += step
