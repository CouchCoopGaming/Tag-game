"""Two-lane straight. 6 m wide, 4 m long, driving surface at 0.12 m."""

import os
import sys

import bmesh
import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import ROAD_TOP, ROAD_W, TILE_L, WALK_TOP, Asset, register

# Same curb and gutter as Road_Junction so a tile butted to an arm continues both.
HALF = ROAD_W * 0.5
GUTTER = 0.22
GAP = 0.004
CURB = HALF + GUTTER
WALK = 2.0

# Buried into the asphalt and proud by 6 mm, then welded so the lines are one shell.
PAINT_TOP = ROAD_TOP + 0.006
PAINT_BOT = 0.10


@register
def create():
    a = Asset(
        "Road_Straight",
        "Roads",
        "6 m wide (two 3 m lanes) by 4 m long. Top at 0.12 m. One continuous asphalt surface, "
        "one white edge line each side, and a double yellow of two 10 cm lines with a 10 cm gap. "
        "A flush concrete gutter and a sidewalk run down both sides. Center the next tile 4 m along Z.",
    )
    a.climb_note = "Flat road and sidewalk."
    a.vault_note = "Curb is 0.15 m. Not a vault."
    half = HALF
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, ROAD_TOP * 0.5, 0), (ROAD_W, ROAD_TOP, TILE_L), "Lib_Asphalt", bevel=0, segs=1, uv_scale=0.4)
        y = (PAINT_TOP + PAINT_BOT) * 0.5
        h = PAINT_TOP - PAINT_BOT
        g.box((-half + 0.18, y, 0), (0.10, h, TILE_L), "Lib_PaintWhite")
        g.box((half - 0.18, y, 0), (0.10, h, TILE_L), "Lib_PaintWhite")
        # Two 10 cm lines. Inner edges at ±5 cm, so the gap is 10 cm.
        g.box((-0.10, y, 0), (0.10, h, TILE_L), "Lib_Lane")
        g.box((0.10, y, 0), (0.10, h, TILE_L), "Lib_Lane")
        _gutter_and_walk(g)
        a.end()
        _weld_paint(a.lods[lod])
    a.box("Col_Slab", (0, 0.060, 0), (ROAD_W - 0.04, 0.104, TILE_L - 0.04))
    gw = 0.16
    gx = HALF + GUTTER * 0.5
    for i, sign in enumerate((-1, 1)):
        a.box("Col_Gutter_%d" % i, (sign * gx, 0.060, 0), (gw, 0.104, TILE_L - 0.06))
        a.box(
            "Col_Walk_%d" % i,
            (sign * (CURB + WALK * 0.5), 0.13, 0),
            (WALK - 0.04, 0.25, TILE_L - 0.06),
        )
    return a


def _gutter_and_walk(g):
    """Concrete pan flush with the asphalt, then the curb and sidewalk. A few millimetres of air."""
    gw = GUTTER - GAP * 2.0
    gx = HALF + GAP + gw * 0.5
    length = TILE_L - 0.008
    for sign in (-1, 1):
        g.box((sign * gx, ROAD_TOP * 0.5, 0), (gw, ROAD_TOP, length), "Lib_Concrete", uv_scale=0.7)
        g.box(
            (sign * (CURB + WALK * 0.5), WALK_TOP * 0.5, 0),
            (WALK, WALK_TOP, length),
            "Lib_Concrete",
            uv_scale=0.7,
        )


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
    """Union the buried paint into the asphalt so the centre lines are one shell."""
    paint_idx = {i for i, name in enumerate(g.mats) if name in ("Lib_PaintWhite", "Lib_Lane")}
    asphalt_idx = {i for i, name in enumerate(g.mats) if name == "Lib_Asphalt"}
    other_idx = set(range(len(g.mats))) - paint_idx - asphalt_idx
    bm_a = _extract(g, asphalt_idx)
    bm_b = _extract(g, paint_idx)
    bm_c = _extract(g, other_idx)
    mesh_a = bpy.data.meshes.new("straight_asphalt")
    mesh_b = bpy.data.meshes.new("straight_paint")
    for name in g.mats:
        mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        mesh_a.materials.append(mat)
        mesh_b.materials.append(mat)
    bm_a.to_mesh(mesh_a)
    bm_b.to_mesh(mesh_b)
    oa = bpy.data.objects.new("straight_asphalt", mesh_a)
    ob = bpy.data.objects.new("straight_paint", mesh_b)
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
        f[g.scale_layer] = 0.4 if name == "Lib_Asphalt" else 1.0
        f[g.grain_layer] = 0.0
    g.prepare()
    bm_a.free()
    bm_b.free()
    bm_c.free()
    welded.free()
