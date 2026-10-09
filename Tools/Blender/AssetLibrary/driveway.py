"""Driveway apron. Wings match the sidewalk cross-section and the curb is cut only at the apron."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, unity_to_blender


# Footprint is centered. Curb face (the road edge) is x = -4.
# Wings are the 2 m sidewalk. The pad runs into the lot on +X.
CURB_X = -4.0
WALK_BACK = -2.0
WALK_TOP = 0.27
ROAD_H = 0.12
PAD_END = 4.0
OPEN_Z = 1.98


@register
def create():
    a = Asset(
        "Driveway",
        "Roads",
        "Apron that replaces two 4 m sidewalk tiles. The -X face is the curb and butts the road edge "
        "the same way Sidewalk does. Wings keep the 2 m walk and the 0.27 m top. The curb is cut "
        "only in the apron, which rises from the 0.12 m asphalt to that walk. The pad stays at 0.27 m.",
    )
    a.climb_note = "Flat concrete. The curb return on each wing is 0.15 m above the asphalt."
    a.vault_note = "Curb is 0.15 m above the road. Not a vault."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        _wings(g, lod)
        _apron(g)
        _pad(g)
        a.end()
    a.box("Col_WingL", (-3.0, 0.12, -3.01), (1.84, 0.20, 1.80))
    a.box("Col_WingR", (-3.0, 0.12, 3.01), (1.84, 0.20, 1.80))
    a.box("Col_Pad", (1.00, 0.12, 0.0), (5.70, 0.20, 3.70))
    _apron_colliders(a)
    return a


def _wings(g, lod):
    size = (2.0, WALK_TOP, 1.96)
    g.box((-3.0, WALK_TOP * 0.5, -3.01), size, "Lib_Concrete", uv_scale=0.7)
    g.box((-3.0, WALK_TOP * 0.5, 3.01), size, "Lib_Concrete", uv_scale=0.7)
    if lod == 0:
        y = WALK_TOP + 0.006
        g.box((-3.0, y, -3.01), (1.84, 0.004, 0.012), "Lib_Mortar")
        g.box((-3.0, y, 3.01), (1.84, 0.004, 0.012), "Lib_Mortar")


def _apron(g):
    x0, x1 = CURB_X, WALK_BACK
    z0, z1 = -OPEN_Z, OPEN_Z
    y0, y1 = ROAD_H, WALK_TOP
    verts = [
        (x0, 0.0, z0), (x1, 0.0, z0), (x1, 0.0, z1), (x0, 0.0, z1),
        (x0, y0, z0), (x1, y1, z0), (x1, y1, z1), (x0, y0, z1),
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
    mi = g.slot("Lib_Concrete")
    made = [bm.verts.new(unity_to_blender(*v)) for v in verts]
    bm.verts.index_update()
    for idxs in faces:
        nf = bm.faces.new([made[i] for i in idxs])
        nf.material_index = mi
        nf.smooth = False
        nf[g.scale_layer] = 0.7
        nf[g.grain_layer] = 0.0


def _pad(g):
    # 8 mm of air off the back of the apron.
    x0 = WALK_BACK + 0.008
    width = PAD_END - x0
    center = (x0 + PAD_END) * 0.5
    g.box((center, WALK_TOP * 0.5, 0.0), (width, WALK_TOP, OPEN_Z * 2.0), "Lib_Concrete", uv_scale=0.55)


def _apron_colliders(a):
    # Boxes stay under the sloping top. The low edge of each slice sets the height.
    for i in range(4):
        left = CURB_X + 0.06 + i * 0.46
        right = left + 0.40
        h = ROAD_H + (WALK_TOP - ROAD_H) * ((left - CURB_X) / (WALK_BACK - CURB_X))
        height = max(0.06, h - 0.025)
        a.box(
            "Col_Apron_%d" % i,
            ((left + right) * 0.5, height * 0.5, 0.0),
            (0.34, height, OPEN_Z * 2.0 - 0.16),
        )
