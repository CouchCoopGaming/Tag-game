#!/usr/bin/env python3
"""Render a quartet that meets Docs/Models/STANDARD.md (Stills spec).

    blender --background --python Tools/Models/shoot_quartet.py -- \\
        --mesh Assets/Art/Props/Library/Buildings/Cabin.fbx \\
        --out Docs/AssetStills/passN \\
        --name Cabin

Hero, side, and scale are orthographic. The mesh bounds cover about half
the frame area and stay 8% in from every edge. The close-up may crop.
The scale frame places the 1.8 m blue Mannequin and writes a PNG tEXt
tag Figure=Mannequin, so the colour test is not the only way to pass.

    python3 Tools/Models/shoot_quartet.py --self-check
"""

from __future__ import annotations

import argparse
import os
import struct
import sys
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import validate_assets as grader  # noqa: E402

WIDTH = 1280
HEIGHT = 720
MARGIN = 0.08
TARGET_AREA = 0.50
FIGURE_NAME = "Mannequin"


def fit_view(span_x, span_y, margin=MARGIN, target_area=TARGET_AREA):
    """World width the orthographic camera must show.

    Blender's ortho_scale is the long side of the frame. Returns
    (ortho_scale, area_fraction, margin_fraction). Area is the box area
    over the frame area.
    """
    span_x = max(float(span_x), 1e-4)
    span_y = max(float(span_y), 1e-4)
    aspect = HEIGHT / float(WIDTH)
    room = 1.0 - 2.0 * margin
    ortho = max(span_x / room, span_y / (room * aspect))
    view_h = ortho * aspect
    area = (span_x / ortho) * (span_y / view_h)
    if area < 0.25:
        # Pull in until the box area hits the target, then stop at the margin.
        need = (span_x * span_y) / (target_area * aspect)
        tighter = need ** 0.5
        ortho = max(tighter, span_x / room, span_y / (room * aspect))
        view_h = ortho * aspect
        area = (span_x / ortho) * (span_y / view_h)
    return ortho, area, margin


def tag_figure(path, name=FIGURE_NAME):
    """Insert a PNG tEXt chunk the checker reads as the named figure."""
    with open(path, "rb") as handle:
        data = handle.read()
    if not data.startswith(b"\x89PNG\r\n\x1a\n"):
        raise SystemExit("scale still is not a PNG: %s" % path)
    payload = b"Figure\x00" + name.encode("ascii")
    body = b"tEXt" + payload
    chunk = struct.pack(">I", len(payload)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)
    end = data.rfind(b"IEND")
    if end < 4:
        raise SystemExit("PNG has no IEND: %s" % path)
    out = data[:end - 4] + chunk + data[end - 4:]
    with open(path, "wb") as handle:
        handle.write(out)


def self_check():
    ortho, area, margin = fit_view(4.6, 1.5)
    assert 0.25 <= area <= 0.85, area
    assert margin >= 0.08
    wide, wide_area, _wide_margin = fit_view(10.0, 0.2)
    assert wide_area <= 0.85
    assert wide >= 10.0 / (1.0 - 2.0 * margin)
    assert grader.FIGURE_FBX.endswith("Mannequin.fbx")
    assert grader.FIGURE_BLUE == (0.239, 0.494, 1.0)
    assert grader.PROVENANCE_LINE == "made in-house, CC0, free to use"
    print("shoot-quartet self-check ok")
    return 0


def _args(argv):
    if "--" in argv:
        argv = argv[argv.index("--") + 1:]
    parser = argparse.ArgumentParser(description="Render a compliant still quartet")
    parser.add_argument("--mesh", help="FBX or blend mesh to frame")
    parser.add_argument("--out", help="Pass folder, Docs/AssetStills/passN")
    parser.add_argument("--name", help="Asset name used in the file tokens")
    parser.add_argument("--self-check", action="store_true")
    return parser.parse_args(argv)


def _snake(name):
    return name.strip().lower()


def _clear():
    import bpy
    bpy.ops.wm.read_factory_settings(use_empty=True)


def _material(name, rgb, emit=False):
    import bpy
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    node = mat.node_tree.nodes.new("ShaderNodeEmission" if emit else "ShaderNodeBsdfPrincipled")
    color = (rgb[0], rgb[1], rgb[2], 1.0)
    if emit:
        node.inputs["Color"].default_value = color
        node.inputs["Strength"].default_value = 1.0
    else:
        node.inputs["Base Color"].default_value = color
        if "Roughness" in node.inputs:
            node.inputs["Roughness"].default_value = 0.6
    out = mat.node_tree.nodes.get("Material Output")
    if out is None:
        out = mat.node_tree.nodes.new("ShaderNodeOutputMaterial")
    mat.node_tree.links.new(node.outputs[0], out.inputs["Surface"])
    return mat


def _cylinder(name, radius, depth, center, material):
    import bpy
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=depth, location=center, vertices=12)
    obj = bpy.context.active_object
    obj.name = name
    obj.data.materials.append(material)
    return obj


def _uv_sphere(name, radius, center, material):
    import bpy
    bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, location=center, segments=12, ring_count=8)
    obj = bpy.context.active_object
    obj.name = name
    obj.data.materials.append(material)
    return obj


def _build_figure():
    """1.8 m Mannequin in Blender Z-up, emission so the albedo survives the light."""
    blue = _material("Lib_PaintBlue", grader.FIGURE_BLUE, emit=True)
    white = _material("Lib_PaintWhite", grader.FIGURE_WHITE, emit=True)
    rubber = _material("Lib_Rubber", (0.165, 0.165, 0.18), emit=True)
    parts = [
        _cylinder("LegL", 0.07, 0.84, (-0.10, 0.0, 0.42), rubber),
        _cylinder("LegR", 0.07, 0.84, (0.10, 0.0, 0.42), rubber),
        _cylinder("Torso", 0.16, 0.62, (0.0, 0.0, 1.05), blue),
        _cylinder("Neck", 0.07, 0.10, (0.0, 0.0, 1.40), blue),
        _uv_sphere("Head", 0.14, (0.0, 0.0, 1.66), white),
        _cylinder("ArmL", 0.045, 0.48, (-0.26, 0.0, 1.00), blue),
        _cylinder("ArmR", 0.045, 0.48, (0.26, 0.0, 1.00), blue),
    ]
    import bpy
    bpy.ops.object.empty_add(type="PLAIN_AXES", location=(0, 0, 0))
    root = bpy.context.active_object
    root.name = FIGURE_NAME
    for part in parts:
        part.parent = root
    return root


def _import_mesh(path):
    import bpy
    before = set(bpy.data.objects)
    ext = os.path.splitext(path)[1].lower()
    if ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    elif ext == ".obj":
        bpy.ops.wm.obj_import(filepath=path)
    else:
        raise SystemExit("unsupported mesh: %s" % path)
    imported = [obj for obj in bpy.data.objects if obj not in before and obj.type == "MESH"]
    if not imported:
        raise SystemExit("no mesh in %s" % path)
    return imported


def _meshes(objects):
    found = []
    for obj in objects:
        if getattr(obj, "type", None) == "MESH":
            found.append(obj)
        for child in getattr(obj, "children_recursive", []):
            if child.type == "MESH":
                found.append(child)
    return found


def _bounds(objects):
    import mathutils
    low = mathutils.Vector((1e9, 1e9, 1e9))
    high = mathutils.Vector((-1e9, -1e9, -1e9))
    meshes = _meshes(objects)
    if not meshes:
        raise SystemExit("nothing to frame")
    for obj in meshes:
        for corner in obj.bound_box:
            point = obj.matrix_world @ mathutils.Vector(corner)
            low.x, low.y, low.z = min(low.x, point.x), min(low.y, point.y), min(low.z, point.z)
            high.x = max(high.x, point.x)
            high.y = max(high.y, point.y)
            high.z = max(high.z, point.z)
    return low, high


def _plane_spans(low, high, eye, aim):
    from mathutils import Vector
    forward = (Vector(aim) - Vector(eye))
    if forward.length < 1e-6:
        forward = Vector((0, -1, 0))
    forward.normalize()
    up = Vector((0, 0, 1))
    right = forward.cross(up)
    if right.length < 1e-6:
        right = Vector((1, 0, 0))
    right.normalize()
    up = right.cross(forward).normalized()
    xs = []
    ys = []
    for x in (low.x, high.x):
        for y in (low.y, high.y):
            for z in (low.z, high.z):
                rel = Vector((x, y, z)) - Vector(aim)
                xs.append(rel.dot(right))
                ys.append(rel.dot(up))
    return max(xs) - min(xs), max(ys) - min(ys)


def _look(eye, aim, ortho):
    import bpy
    from mathutils import Vector
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho
    cam = bpy.data.objects.new("Cam", cam_data)
    bpy.context.scene.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    cam.location = Vector(eye)
    direction = Vector(aim) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    return cam


def _world():
    import bpy
    scene = bpy.context.scene
    world = bpy.data.worlds.new("Sky")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs["Color"].default_value = (0.67, 0.75, 0.82, 1.0)
        bg.inputs["Strength"].default_value = 1.0
    idents = {
        item.identifier
        for item in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items
    }
    for engine in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "BLENDER_WORKBENCH"):
        if engine in idents:
            scene.render.engine = engine
            break
    scene.render.resolution_x = WIDTH
    scene.render.resolution_y = HEIGHT
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False


def _ground(low):
    import bpy
    bpy.ops.mesh.primitive_plane_add(size=80.0, location=(0.0, 0.0, low.z))
    obj = bpy.context.active_object
    obj.name = "Ground"
    obj.data.materials.append(_material("Ground", (0.55, 0.54, 0.52), emit=False))
    return obj


def _render(path, objects, eye, aim, crop=False):
    low, high = _bounds(objects)
    span_x, span_y = _plane_spans(low, high, eye, aim)
    ortho, _area, _margin = fit_view(span_x, span_y)
    if crop:
        ortho = max(span_y, span_x * HEIGHT / float(WIDTH)) * 0.55
    _look((eye[0], eye[1], eye[2]), (aim[0], aim[1], aim[2]), ortho)
    import bpy
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def render_quartet(mesh, out_dir, name):
    import bpy
    os.makedirs(out_dir, exist_ok=True)
    stem = _snake(name)
    _clear()
    _world()
    meshes = _import_mesh(os.path.abspath(mesh))
    low, high = _bounds(meshes)
    _ground(low)
    center = (low + high) * 0.5
    size = high - low
    reach = max(size.x, size.y, size.z, 1.0) * 4.0
    shots = {
        "hero": (center.x + reach, center.y - reach * 0.6, center.z + reach * 0.45),
        "side": (center.x + reach, center.y, center.z + size.z * 0.15),
        "close": (center.x + size.x * 0.2, center.y - max(size.y, 0.4), center.z + size.z * 0.55),
    }
    for role, eye in shots.items():
        path = os.path.join(out_dir, "%s_%s.png" % (stem, role))
        _render(path, meshes, eye, center, crop=(role == "close"))
        print("WROTE", path)
    figure = _build_figure()
    width = max(size.x, size.y)
    figure.location = (high.x + 0.45, center.y, low.z)
    scale_path = os.path.join(out_dir, "%s_scale.png" % stem)
    aim = (center.x + width * 0.25, center.y, center.z)
    eye = (aim[0] + reach, aim[1] - reach * 0.45, low.z + grader.FIGURE_HEIGHT_M)
    _render(scale_path, list(meshes) + [figure], eye, aim)
    tag_figure(scale_path, FIGURE_NAME)
    print("WROTE", scale_path)
    return 0


def main(argv):
    args = _args(argv)
    if args.self_check:
        return self_check()
    if not args.mesh or not args.out or not args.name:
        raise SystemExit("need --mesh, --out, and --name")
    try:
        import bpy  # noqa: F401
    except ImportError:
        raise SystemExit("run from Blender: blender --background --python Tools/Models/shoot_quartet.py -- --mesh ...")
    return render_quartet(args.mesh, args.out, args.name)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
