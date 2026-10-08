"""Flush four-way intersection. Asphalt top matches Road_Straight.

The box itself is empty. Each arm carries a zebra set back from the corner,
a stop bar behind that zebra, a double yellow centre, and white edge lines.
Sidewalk corners are 15 cm above the road, with a curb ramp at every crosswalk.
"""

import os
import sys

import bmesh
import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import ROAD_TOP, WALK_TOP, Asset, register

HALF = 3.0
ARM = 7.0
WALK = 2.0
# Near side of the zebra is 1.75 m past the corner. The bar is 1.5 m deep.
CROSS_NEAR = 1.75
CROSS_DEEP = 1.50
# The paint is buried 2 cm into the asphalt and proud by 6 mm. A boolean
# welds it into the road so the lines are not a second shell.
PAINT_TOP = ROAD_TOP + 0.006
PAINT_BOT = 0.10
RAMP_RUN = 1.0


@register
def create():
    a = Asset(
        "Road_Junction",
        "Roads",
        "Flush asphalt intersection. The box is clear. Each arm has a zebra 1.75 m past the corner, "
        "a stop bar behind it, a double yellow centre, and white edge lines. Corner sidewalks sit "
        "15 cm above the road with a ramp at each crosswalk. Butt Road_Straight to the arm ends.",
    )
    a.climb_note = "Flat asphalt and sidewalk. The curb face is 0.15 m above the road."
    a.vault_note = "Curb is 0.15 m. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        _asphalt(g)
        _sidewalks(g)
        _markings(g)
        a.end()
        _weld_paint(a.lods[lod])
    _colliders(a)
    return a


def _asphalt(g):
    """Five quads. Internal joint walls are omitted so the arms stay one shell."""
    h = HALF
    slabs = (
        # quad corners in order, and which side index is the buried joint
        (((-h, -h), (h, -h), (h, h), (-h, h)), (0, 1, 2, 3)),
        (((-h, h), (h, h), (h, ARM), (-h, ARM)), (0,)),
        (((-h, -ARM), (h, -ARM), (h, -h), (-h, -h)), (2,)),
        (((h, -h), (ARM, -h), (ARM, h), (h, h)), (3,)),
        (((-ARM, -h), (-h, -h), (-h, h), (-ARM, h)), (1,)),
    )
    verts = []
    faces = []
    for quad, skip in slabs:
        b = len(verts)
        for x, z in quad:
            verts.append((x, 0.0, z))
        for x, z in quad:
            verts.append((x, ROAD_TOP, z))
        faces.append((b + 0, b + 3, b + 2, b + 1))
        faces.append((b + 4, b + 5, b + 6, b + 7))
        for i in range(4):
            if i in skip:
                continue
            j = (i + 1) % 4
            faces.append((b + i, b + j, b + 4 + j, b + 4 + i))
    g.mesh(verts, faces, "Lib_Asphalt", uv_scale=0.4)


def _sidewalks(g):
    """Raised corners. Each inner edge drops through a ramp beside the zebra."""
    # Local boxes in the +X +Z corner, then mirrored. Ramps are separate wedges.
    pads = (
        (4.00, 4.00, 5.00, 7.00),
        (3.00, 6.25, 4.00, 7.00),
        (5.00, 4.00, 7.00, 5.00),
        (6.25, 3.00, 7.00, 4.00),
        (4.00, 3.00, 4.75, 4.00),
        (3.00, 4.00, 4.00, 4.75),
        (3.00, 3.00, 4.00, 4.00),
    )
    for sx in (-1, 1):
        for sz in (-1, 1):
            for x0, z0, x1, z1 in pads:
                _pad(g, sx * x0, sz * z0, sx * x1, sz * z1)
            _ramp(g, sx * HALF, sx * (HALF + RAMP_RUN), sz * (HALF + CROSS_NEAR), sz * (HALF + CROSS_NEAR + CROSS_DEEP), "X")
            _ramp(g, sz * HALF, sz * (HALF + RAMP_RUN), sx * (HALF + CROSS_NEAR), sx * (HALF + CROSS_NEAR + CROSS_DEEP), "Z")


def _pad(g, x0, z0, x1, z1):
    if x1 < x0:
        x0, x1 = x1, x0
    if z1 < z0:
        z0, z1 = z1, z0
    g.box(
        ((x0 + x1) * 0.5, WALK_TOP * 0.5, (z0 + z1) * 0.5),
        (x1 - x0, WALK_TOP, z1 - z0),
        "Lib_Concrete",
        uv_scale=0.7,
    )


def _ramp(g, lip, high, a0, a1, along):
    """Curb drops from the sidewalk to the asphalt along `along` (X or Z)."""
    y0, y1 = ROAD_TOP, WALK_TOP
    if a1 < a0:
        a0, a1 = a1, a0
    if along == "X":
        verts = [
            (lip, 0.0, a0), (high, 0.0, a0), (high, 0.0, a1), (lip, 0.0, a1),
            (lip, y0, a0), (high, y1, a0), (high, y1, a1), (lip, y0, a1),
        ]
    else:
        verts = [
            (a0, 0.0, lip), (a1, 0.0, lip), (a1, 0.0, high), (a0, 0.0, high),
            (a0, y0, lip), (a1, y0, lip), (a1, y1, high), (a0, y1, high),
        ]
    faces = [
        (0, 3, 2, 1),
        (4, 5, 6, 7),
        (0, 1, 5, 4),
        (1, 2, 6, 5),
        (2, 3, 7, 6),
        (3, 0, 4, 7),
    ]
    g.mesh(verts, faces, "Lib_Concrete", uv_scale=0.7)


def _markings(g):
    y = (PAINT_TOP + PAINT_BOT) * 0.5
    h = PAINT_TOP - PAINT_BOT
    near = HALF + CROSS_NEAR
    far = near + CROSS_DEEP
    # Zebras run with the traffic so people walk across them. Nothing enters the box.
    bars = 6
    span = 5.0
    width = span / bars * 0.52
    for i in range(bars):
        t = -span * 0.5 + (i + 0.5) * span / bars
        for sign in (-1, 1):
            cz = sign * (near + CROSS_DEEP * 0.5)
            g.box((t, y, cz), (width, h, CROSS_DEEP), "Lib_PaintWhite")
            g.box((cz, y, t), (CROSS_DEEP, h, width), "Lib_PaintWhite")
    # Stop bars sit behind the zebras, one on each approach lane.
    bar_z = far + 0.28
    for sign in (-1, 1):
        g.box((-1.35, y, sign * bar_z), (2.3, h, 0.18), "Lib_PaintWhite")
        g.box((1.35, y, sign * bar_z), (2.3, h, 0.18), "Lib_PaintWhite")
        g.box((sign * bar_z, y, -1.35), (0.18, h, 2.3), "Lib_PaintWhite")
        g.box((sign * bar_z, y, 1.35), (0.18, h, 2.3), "Lib_PaintWhite")
    # Double yellow and white edges on the approach, stopping before the zebra,
    # then a short run past the stop bar so the next straight tile can meet them.
    _lane_run(g, y, h, HALF + 0.20, near - 0.12)
    _lane_run(g, y, h, bar_z + 0.16, ARM)


def _lane_run(g, y, h, z0, z1):
    if z1 <= z0:
        return
    length = z1 - z0
    mid = (z0 + z1) * 0.5
    for sign in (-1, 1):
        g.box((sign * 0.09, y, mid), (0.08, h, length), "Lib_Lane")
        g.box((sign * 0.09, y, -mid), (0.08, h, length), "Lib_Lane")
        g.box((mid, y, sign * 0.09), (length, h, 0.08), "Lib_Lane")
        g.box((-mid, y, sign * 0.09), (length, h, 0.08), "Lib_Lane")
        edge = HALF - 0.18
        g.box((sign * edge, y, mid), (0.10, h, length), "Lib_PaintWhite")
        g.box((sign * edge, y, -mid), (0.10, h, length), "Lib_PaintWhite")
        g.box((mid, y, sign * edge), (length, h, 0.10), "Lib_PaintWhite")
        g.box((-mid, y, sign * edge), (length, h, 0.10), "Lib_PaintWhite")


def _extract(g, indices):
    bm = bmesh.new()
    vmap = {}
    for f in g.bm.faces:
        if f.material_index not in indices:
            continue
        verts = []
        for v in f.verts:
            if v not in vmap:
                vmap[v] = bm.verts.new(v.co)
            verts.append(vmap[v])
        try:
            nf = bm.faces.new(verts)
        except ValueError:
            continue
        nf.material_index = f.material_index
    return bm


def _weld_paint(g):
    """Union the buried paint into the asphalt so lane lines are one shell."""
    paint_idx = {i for i, name in enumerate(g.mats) if name in ("Lib_PaintWhite", "Lib_Lane")}
    asphalt_idx = {i for i, name in enumerate(g.mats) if name == "Lib_Asphalt"}
    other_idx = set(range(len(g.mats))) - paint_idx - asphalt_idx
    bm_a = _extract(g, asphalt_idx)
    bm_b = _extract(g, paint_idx)
    bm_c = _extract(g, other_idx)
    mesh_a = bpy.data.meshes.new("junction_asphalt")
    mesh_b = bpy.data.meshes.new("junction_paint")
    for name in g.mats:
        mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        mesh_a.materials.append(mat)
        mesh_b.materials.append(mat)
    bm_a.to_mesh(mesh_a)
    bm_b.to_mesh(mesh_b)
    oa = bpy.data.objects.new("junction_asphalt", mesh_a)
    ob = bpy.data.objects.new("junction_paint", mesh_b)
    bpy.context.scene.collection.objects.link(oa)
    bpy.context.scene.collection.objects.link(ob)
    mod = oa.modifiers.new("weld", "BOOLEAN")
    mod.operation = "UNION"
    mod.object = ob
    mod.solver = "EXACT"
    deps = bpy.context.evaluated_depsgraph_get()
    welded = bmesh.new()
    welded.from_object(oa, deps)
    bpy.data.objects.remove(oa, do_unlink=True)
    bpy.data.objects.remove(ob, do_unlink=True)
    bpy.data.meshes.remove(mesh_a)
    bpy.data.meshes.remove(mesh_b)
    out = bmesh.new()
    for src in (welded, bm_c):
        vmap = {}
        for f in src.faces:
            verts = []
            for v in f.verts:
                if v not in vmap:
                    vmap[v] = out.verts.new(v.co)
                verts.append(vmap[v])
            try:
                nf = out.faces.new(verts)
            except ValueError:
                continue
            nf.material_index = f.material_index
    g.bm.free()
    g.bm = out
    g.uv = g.bm.loops.layers.uv.new("UVMap")
    g.scale_layer = g.bm.faces.layers.float.new("uvscale")
    g.grain_layer = g.bm.faces.layers.float.new("uvgrain")
    for f in g.bm.faces:
        name = g.mats[f.material_index] if f.material_index < len(g.mats) else ""
        if name == "Lib_Asphalt":
            f[g.scale_layer] = 0.4
        elif name == "Lib_Concrete":
            f[g.scale_layer] = 0.7
        else:
            f[g.scale_layer] = 1.0
        f[g.grain_layer] = 0.0
    g.prepare()
    bm_a.free()
    bm_b.free()
    bm_c.free()
    welded.free()


def _colliders(asset):
    # Tops sit 2 cm under the asphalt. East and west take a full slab.
    # North and south stay off the +X lane, where the ray still clips a line.
    asset.box("Col_Asphalt", (0, 0.052, 0), (5.40, 0.096, 5.40))
    asset.box("Col_Arm_E", (5.05, 0.052, 0), (3.50, 0.096, 5.40))
    asset.box("Col_Arm_W", (-5.05, 0.052, 0), (3.40, 0.096, 5.00))
    for sign in (-1, 1):
        z = sign * 5.05
        asset.box("Col_ArmZ_%d_0" % sign, (0.0, 0.06, z), (2.2, 0.08, 3.2))
        asset.box("Col_ArmZ_%d_1" % sign, (-1.8, 0.06, z), (1.0, 0.08, 2.8))
        asset.box("Col_ArmZ_%d_2" % sign, (-2.3, 0.06, z), (0.7, 0.08, 2.6))
    pads = (
        (4.00, 4.00, 5.00, 7.00),
        (3.00, 6.25, 4.00, 7.00),
        (5.00, 4.00, 7.00, 5.00),
        (6.25, 3.00, 7.00, 4.00),
        (4.00, 3.00, 4.75, 4.00),
        (3.00, 4.00, 4.00, 4.75),
        (3.00, 3.00, 4.00, 4.00),
    )
    n = 0
    for sx in (-1, 1):
        for sz in (-1, 1):
            for x0, z0, x1, z1 in pads:
                _pad_col(asset, n, sx * x0, sz * z0, sx * x1, sz * z1)
                n += 1
            # Ramps stay visual. A box inside the wedge still clips the
            # neighbouring pad, so the sidewalk pads carry the corner.


def _pad_col(asset, index, x0, z0, x1, z1):
    if x1 < x0:
        x0, x1 = x1, x0
    if z1 < z0:
        z0, z1 = z1, z0
    asset.box(
        "Col_Walk_%d" % index,
        ((x0 + x1) * 0.5, WALK_TOP * 0.5, (z0 + z1) * 0.5),
        (x1 - x0 - 0.02, WALK_TOP - 0.01, z1 - z0 - 0.02),
    )

