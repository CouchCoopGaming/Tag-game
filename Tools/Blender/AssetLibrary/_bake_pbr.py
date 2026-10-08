"""Bake tileable PBR maps from Blender shader nodes.

Each set is a 1 m tile: albedo, roughness, a normal from the height, and an
ambient-occlusion map from the same height. Emission bakes are one sample,
so the texture nodes come through without lighting noise.
"""

import os

import bpy

from _common import TEX_DIR

SIZE = 512


def _out(nt, socket):
    emit = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(socket, emit.inputs["Color"])
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(emit.outputs["Emission"], out.inputs["Surface"])
    return emit


def _mix(nt, fac, a, b, blend="MIX"):
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = blend
    if isinstance(fac, (int, float)):
        mix.inputs["Factor"].default_value = float(fac)
    else:
        nt.links.new(fac, mix.inputs["Factor"])
    if isinstance(a, (tuple, list)):
        mix.inputs["A"].default_value = (a[0], a[1], a[2], 1.0)
    else:
        nt.links.new(a, mix.inputs["A"])
    if isinstance(b, (tuple, list)):
        mix.inputs["B"].default_value = (b[0], b[1], b[2], 1.0)
    else:
        nt.links.new(b, mix.inputs["B"])
    return mix.outputs["Result"]


def _gray(nt, value):
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    if not isinstance(value, (int, float)):
        nt.links.new(value, mix.inputs["Factor"])
        mix.inputs["Factor"].default_value = 0.0
    else:
        mix.inputs["Factor"].default_value = float(value)
    mix.inputs["A"].default_value = (0.0, 0.0, 0.0, 1.0)
    mix.inputs["B"].default_value = (1.0, 1.0, 1.0, 1.0)
    return mix.outputs["Result"]


def _noise(nt, scale, detail=4.0, rough=0.5, distortion=0.0):
    n = nt.nodes.new("ShaderNodeTexNoise")
    n.inputs["Scale"].default_value = scale
    n.inputs["Detail"].default_value = detail
    n.inputs["Roughness"].default_value = rough
    n.inputs["Distortion"].default_value = distortion
    return n


def _mapping(nt, scale):
    tex = nt.nodes.new("ShaderNodeTexCoord")
    mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = scale
    nt.links.new(tex.outputs["UV"], mp.inputs["Vector"])
    return mp.outputs["Vector"]


def _brick_color(nt):
    brick = nt.nodes.new("ShaderNodeTexBrick")
    brick.inputs["Scale"].default_value = 1.0
    brick.inputs["Brick Width"].default_value = 0.24
    brick.inputs["Row Height"].default_value = 0.125
    brick.inputs["Mortar Size"].default_value = 0.014
    brick.inputs["Mortar Smooth"].default_value = 0.05
    brick.offset = 0.5
    brick.inputs["Color1"].default_value = (0.74, 0.34, 0.20, 1.0)
    brick.inputs["Color2"].default_value = (0.52, 0.22, 0.13, 1.0)
    brick.inputs["Mortar"].default_value = (0.32, 0.30, 0.28, 1.0)
    n = _noise(nt, 5.0, 6.0, 0.55)
    # A light mix keeps the bond. Multiplying by noise was flattening the mortar.
    color = _mix(nt, 0.18, brick.outputs["Color"], n.outputs["Color"], "MIX")
    # Fac is the mortar mask. Bricks are the high part of the height.
    inv = nt.nodes.new("ShaderNodeMath")
    inv.operation = "SUBTRACT"
    inv.inputs[0].default_value = 1.0
    nt.links.new(brick.outputs["Fac"], inv.inputs[1])
    rough = _mix(nt, brick.outputs["Fac"], (0.55, 0.55, 0.55), (0.88, 0.88, 0.88))
    return color, _gray(nt, inv.outputs["Value"]), rough


def _concrete_color(nt):
    coarse = _noise(nt, 3.2, 5.0, 0.6, 0.2)
    fine = _noise(nt, 22.0, 8.0, 0.7)
    stains = _noise(nt, 1.4, 3.0, 0.45)
    base = _mix(nt, 0.55, (0.62, 0.60, 0.56), coarse.outputs["Color"], "MULTIPLY")
    base = _mix(nt, 0.18, base, fine.outputs["Color"], "MULTIPLY")
    # Darker wet-looking stains, lighter scuffs.
    stained = _mix(nt, stains.outputs["Fac"], base, (0.42, 0.40, 0.36))
    scuff = _noise(nt, 8.0, 2.0, 0.3)
    color = _mix(nt, 0.22, stained, _mix(nt, scuff.outputs["Fac"], base, (0.78, 0.76, 0.70)))
    height = _mix(nt, 0.65, coarse.outputs["Fac"], fine.outputs["Fac"])
    rough = _mix(nt, stains.outputs["Fac"], (0.78, 0.78, 0.78), (0.92, 0.92, 0.92))
    return color, height, rough


def _wood_color(nt, dark):
    coord = _mapping(nt, (1.0, 8.0, 1.0))
    wave = nt.nodes.new("ShaderNodeTexWave")
    wave.wave_type = "BANDS"
    wave.inputs["Scale"].default_value = 3.0
    wave.inputs["Distortion"].default_value = 2.5
    wave.inputs["Detail"].default_value = 3.0
    nt.links.new(coord, wave.inputs["Vector"])
    n = _noise(nt, 9.0, 6.0, 0.55)
    if dark:
        a, b = (0.28, 0.16, 0.08), (0.48, 0.30, 0.16)
    else:
        a, b = (0.55, 0.36, 0.18), (0.78, 0.55, 0.30)
    bands = _mix(nt, wave.outputs["Fac"], a, b)
    color = _mix(nt, 0.22, bands, n.outputs["Color"], "MULTIPLY")
    height = _mix(nt, 0.7, wave.outputs["Fac"], n.outputs["Fac"])
    rough = _mix(nt, wave.outputs["Fac"], (0.62, 0.62, 0.62), (0.78, 0.78, 0.78))
    return color, height, rough


def _bark_color(nt):
    coord = _mapping(nt, (3.0, 14.0, 1.0))
    n = _noise(nt, 7.0, 9.0, 0.65, 0.4)
    nt.links.new(coord, n.inputs["Vector"])
    ridges = _noise(nt, 2.2, 4.0, 0.4)
    color = _mix(nt, n.outputs["Fac"], (0.22, 0.14, 0.08), (0.48, 0.32, 0.18))
    color = _mix(nt, 0.35, color, ridges.outputs["Color"], "MULTIPLY")
    height = n.outputs["Fac"]
    rough = _gray(nt, 0.82)
    return color, _gray(nt, height), rough


def _asphalt_color(nt):
    grain = _noise(nt, 28.0, 8.0, 0.8)
    cracks = _noise(nt, 4.5, 12.0, 0.35, 1.2)
    # A color ramp keeps only the deepest noise as a crack.
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.42
    ramp.color_ramp.elements[0].color = (0.0, 0.0, 0.0, 1.0)
    ramp.color_ramp.elements[1].position = 0.52
    ramp.color_ramp.elements[1].color = (1.0, 1.0, 1.0, 1.0)
    nt.links.new(cracks.outputs["Fac"], ramp.inputs["Fac"])
    patch = nt.nodes.new("ShaderNodeTexBrick")
    patch.inputs["Scale"].default_value = 0.55
    patch.inputs["Brick Width"].default_value = 0.9
    patch.inputs["Row Height"].default_value = 0.7
    patch.inputs["Mortar Size"].default_value = 0.02
    patch.inputs["Color1"].default_value = (0.22, 0.22, 0.22, 1.0)
    patch.inputs["Color2"].default_value = (0.16, 0.16, 0.17, 1.0)
    patch.inputs["Mortar"].default_value = (0.10, 0.10, 0.10, 1.0)
    base = _mix(nt, 0.45, (0.16, 0.16, 0.17), grain.outputs["Color"], "MULTIPLY")
    patched = _mix(nt, 0.55, base, patch.outputs["Color"])
    # Cracks darken the albedo. The ramp is black in the crack.
    color = _mix(nt, 0.85, patched, ramp.outputs["Color"], "MULTIPLY")
    height = ramp.outputs["Color"]
    rough = _mix(nt, ramp.outputs["Color"], (0.95, 0.95, 0.95), (0.72, 0.72, 0.72))
    return color, height, rough


def _metal_color(nt, base, rust, streak_scale=6.0):
    coord = _mapping(nt, (1.2, 9.0, 1.0))
    n = _noise(nt, streak_scale, 6.0, 0.55, 0.8)
    nt.links.new(coord, n.inputs["Vector"])
    speckle = _noise(nt, 40.0, 4.0, 0.6)
    worn = _mix(nt, n.outputs["Fac"], base, rust)
    color = _mix(nt, 0.15, worn, speckle.outputs["Color"], "MULTIPLY")
    height = _mix(nt, 0.5, n.outputs["Fac"], speckle.outputs["Fac"])
    rough = _mix(nt, n.outputs["Fac"], (0.42, 0.42, 0.42), (0.78, 0.78, 0.78))
    return color, height, rough


def _prepare(nt, builder):
    nt.nodes.clear()
    color, height, rough = builder(nt)
    return color, height, rough


BUILDERS = {
    "Lib_Brick": _brick_color,
    "Lib_Concrete": _concrete_color,
    "Lib_Wood": lambda nt: _wood_color(nt, False),
    "Lib_WoodDark": lambda nt: _wood_color(nt, True),
    "Lib_Bark": _bark_color,
    "Lib_Asphalt": _asphalt_color,
    "Lib_MetalWorn": lambda nt: _metal_color(nt, (0.55, 0.54, 0.52), (0.45, 0.24, 0.12), 5.0),
    "Lib_ContainerRed": lambda nt: _metal_color(nt, (0.62, 0.10, 0.08), (0.42, 0.20, 0.10), 3.5),
    "Lib_ContainerBlue": lambda nt: _metal_color(nt, (0.10, 0.22, 0.42), (0.40, 0.22, 0.12), 3.5),
    "Lib_CraneYellow": lambda nt: _metal_color(nt, (0.78, 0.58, 0.12), (0.40, 0.28, 0.14), 4.0),
}


def _bake_socket(obj, socket, path):
    mat = obj.data.materials[0]
    nt = mat.node_tree
    # Drop any previous emission so each bake has one output.
    for node in list(nt.nodes):
        if node.bl_idname in ("ShaderNodeEmission", "ShaderNodeOutputMaterial", "ShaderNodeTexImage"):
            nt.nodes.remove(node)
    _out(nt, socket)
    img = bpy.data.images.new(os.path.basename(path), SIZE, SIZE, alpha=False)
    img.colorspace_settings.name = "Non-Color" if path.endswith(("_R.png", "_H.png")) else "sRGB"
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    nt.nodes.active = tex
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.bake(type="EMIT", margin=4, use_clear=True)
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    return img


def _save_from_floats(name, w, h, pixels):
    img = bpy.data.images.new(name, w, h, alpha=False)
    img.pixels.foreach_set(pixels)
    path = os.path.join(TEX_DIR, name + ".png")
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


def _maps_from_height(height_img, name, strength):
    w, h = height_img.size
    src = list(height_img.pixels)
    normal = [0.0] * (w * h * 4)
    ao = [0.0] * (w * h * 4)

    def sample(x, y):
        x %= w
        y %= h
        return src[(y * w + x) * 4]

    for y in range(h):
        for x in range(w):
            l = sample(x - 1, y)
            r = sample(x + 1, y)
            d = sample(x, y - 1)
            u = sample(x, y + 1)
            c = sample(x, y)
            nx = (l - r) * strength
            ny = (d - u) * strength
            nz = 1.0
            length = (nx * nx + ny * ny + nz * nz) ** 0.5 or 1.0
            i = (y * w + x) * 4
            normal[i] = nx / length * 0.5 + 0.5
            normal[i + 1] = ny / length * 0.5 + 0.5
            normal[i + 2] = nz / length * 0.5 + 0.5
            normal[i + 3] = 1.0
            # Crevices (low height) occlude. Keep it gentle so the tile stays readable.
            occ = 0.45 + 0.55 * (c ** 0.65)
            ao[i] = ao[i + 1] = ao[i + 2] = occ
            ao[i + 3] = 1.0
    _save_from_floats(name + "_N", w, h, normal)
    _save_from_floats(name + "_AO", w, h, ao)


def bake_all():
    os.makedirs(TEX_DIR, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 1
    scene.cycles.use_denoising = False
    bpy.ops.mesh.primitive_plane_add(size=1.0)
    obj = bpy.context.active_object
    mat = bpy.data.materials.new("Bake")
    mat.use_nodes = True
    obj.data.materials.append(mat)
    for name, builder in BUILDERS.items():
        nt = mat.node_tree
        color, height, rough = _prepare(nt, builder)
        # Sockets die when nodes are cleared, so bake one output at a time
        # and rebuild the graph for the next.
        _bake_socket(obj, color, os.path.join(TEX_DIR, name + ".png"))
        color, height, rough = _prepare(nt, builder)
        himg = _bake_socket(obj, height, os.path.join(TEX_DIR, name + "_H.png"))
        _maps_from_height(himg, name, 3.5 if name != "Lib_Brick" else 6.0)
        bpy.data.images.remove(himg, do_unlink=True)
        try:
            os.remove(os.path.join(TEX_DIR, name + "_H.png"))
        except OSError:
            pass
        color, height, rough = _prepare(nt, builder)
        _bake_socket(obj, rough, os.path.join(TEX_DIR, name + "_R.png"))
        print("BAKED", name)
