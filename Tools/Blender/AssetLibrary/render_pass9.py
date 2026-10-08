"""Pass 9 stills. Court hoops, continuous asphalt, storefront towers, playground colors.

  blender --background --python Tools/Blender/AssetLibrary/render_pass9.py
"""

import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass2 as r  # noqa: E402
import render_pass6 as p6  # noqa: E402
from _common import TEX_DIR, unity_to_blender  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass9")
HIER_FBX = os.path.join(
    r._common.REPO, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx"
)
# Hull runs about y=0.03 to y=1.04. Water at 0.40 is ~36% of that height.
WATER_Y = 0.40


def _water_mat():
    mat = p6._mat("Pass9Water", (0.012, 0.045, 0.038), 0.58, metal=0.0, transmission=0.0)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf and "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.18
    path = os.path.join(TEX_DIR, "Lib_Water_N.png")
    if bsdf and os.path.isfile(path):
        nt = mat.node_tree
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(path, check_existing=True)
        mapping = nt.nodes.new("ShaderNodeMapping")
        mapping.inputs["Scale"].default_value = (0.18, 0.18, 0.18)
        coord = nt.nodes.new("ShaderNodeTexCoord")
        nt.links.new(coord.outputs["Object"], mapping.inputs["Vector"])
        nt.links.new(mapping.outputs["Vector"], tex.inputs["Vector"])
        normal = nt.nodes.new("ShaderNodeNormalMap")
        normal.inputs["Strength"].default_value = 0.22
        nt.links.new(tex.outputs["Color"], normal.inputs["Color"])
        nt.links.new(normal.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def _foam_mat():
    return p6._mat("Pass9Foam", (0.62, 0.70, 0.66), 0.92)


def _mat(name):
    found = bpy.data.materials.get(name)
    if found is not None:
        return found
    return p6._mat(name, (0.5, 0.5, 0.5), 0.6)


def _cube(center, size, mat, yaw=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=unity_to_blender(*center))
    obj = bpy.context.active_object
    obj.scale = (size[0], size[2], size[1])
    obj.rotation_euler = (0.0, 0.0, math.radians(yaw))
    obj.data.materials.append(mat)
    return obj


def _ring(x, z, radius, mat):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=radius,
        minor_radius=0.018,
        major_segments=14,
        minor_segments=5,
        location=unity_to_blender(x, WATER_Y + 0.012, z),
    )
    obj = bpy.context.active_object
    obj.data.materials.append(mat)


def _fan(x, y, z, radius, mat):
    bpy.ops.mesh.primitive_cylinder_add(
        radius=radius, depth=0.04, vertices=8, location=unity_to_blender(x, y, z)
    )
    obj = bpy.context.active_object
    obj.data.materials.append(mat)


def _slab(u, face, du, dv, h, y0, mat, along, sign, out=0.006):
    """A box just outside the wall face. `u` runs along the facade."""
    v = face + sign * (abs(dv) * 0.5 + out)
    if along == "x":
        p6._block(u, v, du, abs(dv), h, mat, y0)
    else:
        p6._block(v, u, abs(dv), du, h, mat, y0)


def _frame_opening(u, face, w, h, y0, along, sign, mat):
    t = 0.06
    _slab(u, face, w + t * 2, 0.04, t, y0 + h, mat, along, sign, out=0.02)
    _slab(u, face, w + t * 2, 0.04, t, y0 - t, mat, along, sign, out=0.02)
    _slab(u - w * 0.5 - t * 0.5, face, t, 0.04, h, y0, mat, along, sign, out=0.02)
    _slab(u + w * 0.5 + t * 0.5, face, t, 0.04, h, y0, mat, along, sign, out=0.02)


def _facade(u_center, face, width, height, along, sign, awning_name):
    """Storefront on one face: framed glass, a door on a step, and an awning."""
    glass = _mat("Lib_ShopGlass")
    frame = _mat("Lib_SteelDark")
    door_mat = _mat("Lib_WoodDark")
    awning = _mat(awning_name)
    step = _mat("Lib_Concrete")
    cream = _mat("Lib_PaintCream")
    margin = 0.30
    door_w = 0.96
    door_h = 2.10
    u0 = u_center - width * 0.5
    door_u = u0 + margin + door_w * 0.5
    _slab(door_u, face, door_w + 0.12, 0.28, 0.14, 0.0, step, along, sign)
    _frame_opening(door_u, face, door_w, door_h, 0.16, along, sign, frame)
    _slab(door_u, face, door_w - 0.08, 0.035, door_h - 0.10, 0.20, door_mat, along, sign)
    _slab(door_u, face, 0.08, 0.02, 0.16, 1.05, cream, along, sign)
    glass_u0 = door_u + door_w * 0.5 + 0.22
    glass_u1 = u_center + width * 0.5 - margin
    glass_w = glass_u1 - glass_u0
    if glass_w > 0.8:
        bays = 2 if glass_w < 3.2 else 3
        bay = (glass_w - 0.10 * (bays - 1)) / bays
        for i in range(bays):
            bu = glass_u0 + bay * 0.5 + i * (bay + 0.10)
            _frame_opening(bu, face, bay - 0.04, 1.70, 0.42, along, sign, frame)
            _slab(bu, face, bay - 0.16, 0.02, 1.56, 0.48, glass, along, sign, out=0.008)
            _slab(bu, face, 0.035, 0.016, 1.50, 0.50, frame, along, sign, out=0.034)
        awn_u = (door_u - door_w * 0.5 + glass_u1) * 0.5
        awn_w = glass_u1 - (door_u - door_w * 0.5)
        _slab(awn_u, face, awn_w, 0.85, 0.06, 2.42, awning, along, sign)
        _slab(awn_u, face, awn_w, 0.10, 0.10, 2.30, awning, along, sign)
    # Cornice just under the roof, proud of the storefront.
    _slab(u_center, face, width + 0.24, 0.16, 0.18, height - 0.22, cream, along, sign)
    # Upper windows start above the awning so the ground floor stays a shop.
    win = _mat("Lib_ShopGlass")
    warm = _mat("Lib_WindowLit")
    cols = max(2, int(width / 1.45))
    y = 3.85
    row = 0
    while y < height - 1.35:
        for col in range(cols):
            wu = u_center - width * 0.5 + (col + 0.5) * (width / cols)
            pane = warm if (row + col) % 5 == 0 else win
            _frame_opening(wu, face, 0.62, 0.85, y, along, sign, frame)
            _slab(wu, face, 0.48, 0.018, 0.70, y + 0.06, pane, along, sign)
        y += 1.65
        row += 1


def _dress(cx, cz, sx, sz, height, brick, front, awning_name):
    shaft = _mat("Lib_Brick" if brick else "Lib_Siding")
    cap = _mat("Lib_Roof")
    steel = _mat("Lib_Steel")
    dark = _mat("Lib_SteelDark")
    p6._block(cx, cz, sx, sz, height, shaft, 0.0)
    p6._block(cx, cz, sx + 0.18, sz + 0.18, 0.16, _mat("Lib_PaintCream"), height - 0.04)
    p6._block(cx, cz, sx + 0.04, sz + 0.04, 0.62, cap, height + 0.08)
    ac_x = cx + sx * 0.16
    ac_z = cz - sz * 0.10
    p6._block(ac_x, ac_z, 1.20, 0.78, 0.62, steel, height + 0.72)
    _fan(ac_x - 0.28, height + 1.38, ac_z, 0.16, dark)
    _fan(ac_x + 0.28, height + 1.38, ac_z, 0.16, dark)
    if front == "minz":
        _facade(cx, cz - sz * 0.5, sx, height, "x", -1.0, awning_name)
    else:
        _facade(cz, cx + sx * 0.5, sz, height, "z", 1.0, awning_name)


def _city():
    """Land-side towers. The water face is a storefront, not a blank podium."""
    towers = (
        (-24, 16, 7.0, 5.5, 14.0, False, "Lib_Awning"),
        (-12, 18, 5.5, 4.8, 22.0, True, "Lib_PaintTeal"),
        (0, 15.5, 6.5, 5.2, 18.0, False, "Lib_Awning"),
        (12, 17, 6.0, 5.0, 26.0, False, "Lib_PaintBlue"),
        (24, 16.5, 7.0, 5.5, 13.0, True, "Lib_Awning"),
        (-18, 28, 8.5, 6.5, 9.0, True, "Lib_PaintTeal"),
        (8, 30, 9.0, 6.5, 8.0, True, "Lib_PaintBlue"),
    )
    for x, z, w, d, h, brick, awning in towers:
        _dress(x, z, w, d, h, brick, "minz", awning)


def _street_brick():
    """Brick row on the far sidewalk, front face on x = -5.38."""
    face = -5.38
    specs = (
        (-8.5, 8.0, 6.4, 7.2, "Lib_Awning"),
        (2.5, 7.4, 6.0, 9.4, "Lib_PaintTeal"),
        (12.5, 8.2, 6.6, 6.6, "Lib_Awning"),
    )
    for z, width, depth, height, awning in specs:
        cx = face - depth * 0.5
        _dress(cx, z, depth, width, height, True, "maxx", awning)


def _place_hier(pos, yaw):
    """Stand the Hier mannequin on pos. Scale so the figure is about 1.8 m."""
    if not os.path.isfile(HIER_FBX):
        print("HIER_MISSING", HIER_FBX)
        return None
    before = {obj.name for obj in bpy.data.objects}
    bpy.ops.import_scene.fbx(filepath=HIER_FBX)
    fresh = [obj for obj in bpy.data.objects if obj.name not in before]
    meshes = [obj for obj in fresh if obj.type == "MESH"]
    bpy.context.view_layer.update()
    zs = []
    for obj in meshes:
        for corner in obj.bound_box:
            zs.append((obj.matrix_world @ Vector(corner)).z)
    if not zs:
        print("HIER_EMPTY")
        return None
    foot = min(zs)
    height = max(zs) - foot
    scale = 1.8 / height if height > 0.2 else 1.0
    root = bpy.data.objects.new("HierRoot", None)
    bpy.context.scene.collection.objects.link(root)
    root.location = (0.0, 0.0, foot)
    tops = [obj for obj in fresh if obj.parent is None or obj.parent not in fresh]
    for obj in tops:
        world = obj.matrix_world.copy()
        obj.parent = root
        obj.matrix_world = world
    root.scale = (scale, scale, scale)
    root.location = Vector(unity_to_blender(*pos))
    root.rotation_euler = (0.0, 0.0, math.radians(yaw))
    print("HIER", "height", round(height, 3), "scale", round(scale, 4))
    return root


def _harbor(found):
    scene = p6._begin(wide=True)
    # Dock end (local +Z) sits against the fenders. Piles stay in the water.
    dock_pos = (0.2, 0.0, -4.82)
    boat_pos = (3.35, 0.0, -4.70)
    boat_yaw = -8.0
    quay_bollard = (2.4, 1.15, -0.42)
    specs = [
        ("Quay_Edge", (0, 0, 0), 0),
        ("Container_20", (3.6, 0.90, 2.55), 90),
        ("HarborCrane", (-5.4, 0.90, 3.3), 180),
        ("Dock_Straight", dock_pos, 0),
        ("Piling", (-2.1, 0, -6.6), 0),
        ("Piling", (2.3, 0, -7.6), 0),
        ("Boat", boat_pos, boat_yaw),
        ("Buoy", (6.6, 0.0, -8.2), 0),
    ]
    objs = p6._place(found, specs)
    for local, cleat in (
        ((-0.52, 0.97, -1.45), (1.35, 0.70, dock_pos[2] - 1.55)),
        ((-0.52, 0.97, 1.70), (1.35, 0.70, dock_pos[2] + 1.55)),
    ):
        p6._rope(p6._yaw_point(boat_pos, boat_yaw, local), cleat)
    p6._rope(p6._yaw_point(boat_pos, boat_yaw, (-0.52, 0.97, -1.45)), quay_bollard)
    # Buried under the water and the land. Not the visible foreground.
    p6._ground((0.02, 0.035, 0.03), y=-0.18)
    water = _water_mat()
    # Basin only. The wall face is z = -1.42, so the sheet laps the concrete and stops.
    p6._sheet(0.0, WATER_Y, -50.7, 200.0, 98.8, water)
    land = p6._mat("Pass9Land", (0.34, 0.38, 0.30), 0.94)
    p6._sheet(0.0, 0.02, 46.0, 180.0, 78.0, land)
    foam = _foam_mat()
    p6._sheet(0.0, WATER_Y + 0.015, -1.62, 16.8, 0.36, foam)
    for side in (-0.96, 0.96):
        center = p6._yaw_point(boat_pos, boat_yaw, (side, WATER_Y + 0.018, 0.12))
        _cube(center, (0.045, 0.016, 4.4), foam, boat_yaw)
    bow = p6._yaw_point(boat_pos, boat_yaw, (0.0, WATER_Y + 0.018, -2.15))
    _cube(bow, (0.7, 0.016, 0.16), foam, boat_yaw)
    for x, z in ((-0.95, -7.22), (1.35, -7.22), (-0.95, -2.42), (1.35, -2.42), (-2.1, -6.6), (2.3, -7.6)):
        _ring(x, z, 0.22, foam)
    _city()
    _place_hier((-1.4, 0.90, 1.6), 210)
    r._frame(scene, objs, fill=0.70, elevation=15.0, azimuth=205.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_harbor.png"))


def _street(found):
    scene = p6._begin(wide=True)
    specs = list(p6._snap.street_hardscape())
    for name, pos, yaw in p6._snap.street_shops():
        specs.append((name, pos, yaw, 1.0))
    props = [
        ("LightPost_Single", (4.15, p6.WALK_Y, -10.0), -90),
        ("LightPost_Single", (4.15, p6.WALK_Y, 1.0), -90),
        ("LightPost_Single", (4.15, p6.WALK_Y, 11.5), -90),
        ("FireHydrant", (3.85, p6.WALK_Y, -4.2), 10),
        ("FireHydrant", (3.85, p6.WALK_Y, 6.8), -8),
        ("Bench_Wood", (4.35, p6.WALK_Y, -8.6), -90),
        ("Bench_Wood", (4.35, p6.WALK_Y, 5.2), -90),
        ("Bench_Wood", (4.35, p6.WALK_Y, 15.0), -90),
        ("TrashCan_Slat", (4.95, p6.WALK_Y, -14.2), 0),
        ("TrashCan_Lidded", (4.95, p6.WALK_Y, 9.4), 15),
        ("RecyclingBin", (4.70, p6.WALK_Y, 3.1), 8),
        ("NewspaperBox", (4.85, p6.WALK_Y, -2.2), -90),
        ("ParkingMeter", (3.62, p6.WALK_Y, 2.2), 90),
        ("TrafficLight", (4.20, p6.WALK_Y, 13.6), -90),
        ("Tree_Grate", (-4.38, 0.0, -11.0), 0),
        ("Tree_Grate", (-4.38, 0.0, 6.5), 18),
    ]
    specs.extend((name, pos, yaw, 1.0) for name, pos, yaw in props)
    p6._place(found, specs)
    _street_brick()
    p6._ground((0.40, 0.44, 0.38))
    _place_hier((3.9, p6.WALK_Y, -1.5), 200)
    p6._look(scene, (1.5, 3.4, -17.2), (7.2, 1.6, 9.0), lens=28.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_street.png"))


def _road(found):
    scene = p6._begin(wide=True)
    specs = []
    for z in (-4.0, 0.0, 4.0):
        specs.append(("Road_Straight", (0.0, 0.0, z), 0.0))
        specs.append(("Gutter", (3.19, 0.0, z), 0.0))
        specs.append(("Sidewalk", (4.38, 0.0, z), 0.0))
        specs.append(("Gutter", (-3.19, 0.0, z), 180.0))
        specs.append(("Sidewalk", (-4.38, 0.0, z), 180.0))
    p6._place(found, specs)
    p6._ground((0.40, 0.44, 0.38))
    p6._look(scene, (0.6, 1.45, -4.8), (2.55, 0.10, 1.6), lens=32.0)
    r._render(scene, os.path.join(STILL_DIR, "road_curb.png"))


def _court(found):
    scene = p6._begin(wide=True)
    objs = [
        p6._spawn(found, "Court", (0, 0, 0), 0),
        p6._spawn(found, "Hoop", (0, 0, -10.572), 0),
        p6._spawn(found, "Hoop", (0, 0, 10.572), 180),
    ]
    p6._ground((0.15, 0.16, 0.15))
    r._frame(scene, objs, fill=0.78, elevation=52.0, azimuth=38.0)
    r._render(scene, os.path.join(STILL_DIR, "court.png"))


def _turntable(found, name, path, yaw=28.0, elevation=18.0, fill=0.72, ground=(0.55, 0.56, 0.54)):
    scene = p6._begin(wide=True)
    obj = p6._spawn(found, name, (0, 0, 0), yaw)
    p6._ground(ground)
    r._frame(scene, [obj], fill=fill, elevation=elevation, azimuth=38.0)
    r._render(scene, path)


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1]
    found = r._catalog()
    shots = [
        ("harbor", lambda: _harbor(found)),
        ("street", lambda: _street(found)),
        ("road", lambda: _road(found)),
        ("playground", lambda: _turntable(found, "Playground", os.path.join(STILL_DIR, "playground.png"), 32)),
        ("court", lambda: _court(found)),
        ("recycling", lambda: _turntable(found, "RecyclingBin", os.path.join(STILL_DIR, "recycling_bin.png"), 28)),
        ("tree_grate", lambda: _turntable(found, "Tree_Grate", os.path.join(STILL_DIR, "tree_grate.png"), 24)),
        ("bench", lambda: _turntable(found, "Bench_Wood", os.path.join(STILL_DIR, "bench_wood.png"), 32)),
        ("newspaper", lambda: _turntable(found, "NewspaperBox", os.path.join(STILL_DIR, "newspaper_box.png"), 24)),
        ("meter", lambda: _turntable(found, "ParkingMeter", os.path.join(STILL_DIR, "parking_meter.png"), 26)),
        ("traffic", lambda: _turntable(found, "TrafficLight", os.path.join(STILL_DIR, "traffic_light.png"), 36, 12, 0.78)),
        ("trash", lambda: _turntable(found, "TrashCan_Lidded", os.path.join(STILL_DIR, "trash_can.png"), 30)),
    ]
    for name, fn in shots:
        if only and only not in name:
            continue
        fn()
    print("PASS9_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
