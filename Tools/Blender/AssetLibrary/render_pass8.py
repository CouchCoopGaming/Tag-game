"""Pass 8 stills. Quay wall, darker water, asphalt, playground, gazebo, hero props.

  blender --background --python Tools/Blender/AssetLibrary/render_pass8.py
"""

import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass2 as r  # noqa: E402
import render_pass6 as p6  # noqa: E402
from _common import TEX_DIR, unity_to_blender  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass8")
# Hull runs about y=0.03 to y=1.04. Water at 0.40 is ~36% of that height.
WATER_Y = 0.40


def _water_mat():
    mat = p6._mat("Pass8Water", (0.012, 0.045, 0.038), 0.58, metal=0.0, transmission=0.0)
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
    return p6._mat("Pass8Foam", (0.62, 0.70, 0.66), 0.92)


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


def _city():
    """Towers on the land behind the quay, same side as the apron."""
    dark = p6._mat("SkyBase8", (0.34, 0.36, 0.38), 0.9)
    light = p6._mat("SkyShaft8", (0.62, 0.64, 0.66), 0.86)
    brick = p6._mat("SkyBrick8", (0.48, 0.40, 0.36), 0.88)
    cap = p6._mat("SkyCap8", (0.24, 0.26, 0.28), 0.8)
    glass = p6._mat("SkyWin8", (0.16, 0.22, 0.30), 0.25, metal=0.15)
    warm = p6._mat("SkyLit8", (0.86, 0.72, 0.42), 0.4)
    towers = (
        (-24, 16, 7.0, 5.5, 14.0, light),
        (-12, 18, 5.5, 4.8, 22.0, brick),
        (0, 15.5, 6.5, 5.2, 18.0, light),
        (12, 17, 6.0, 5.0, 26.0, light),
        (24, 16.5, 7.0, 5.5, 13.0, brick),
        (-18, 28, 8.5, 6.5, 9.0, brick),
        (8, 30, 9.0, 6.5, 8.0, light),
    )
    for x, z, w, d, h, shaft in towers:
        base_h = max(2.4, h * 0.22)
        p6._block(x, z, w, d, base_h, dark, 0.02)
        p6._block(x, z, w * 0.96, d * 0.96, h - base_h, shaft, base_h + 0.08)
        p6._block(x, z, w * 1.04, d * 1.04, 0.45, cap, h + 0.12)
        face = z - d * 0.5 - 0.04
        cols = max(2, int(w / 1.4))
        rows = max(2, int((h - 2.0) / 1.7))
        for row in range(rows):
            for col in range(cols):
                wx = x - w * 0.5 + (col + 0.5) * (w / cols)
                wy = 1.8 + row * 1.6
                if wy > h - 1.2:
                    continue
                pane = warm if (row + col) % 5 == 0 else glass
                p6._block(wx, face, min(0.5, w / cols * 0.5), 0.06, 0.65, pane, wy)


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
        ("Mannequin", (-1.4, 0.90, 1.6), 210),
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
    land = p6._mat("Pass8Land", (0.34, 0.38, 0.30), 0.94)
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
        ("TrashCan_Slat", (4.95, p6.WALK_Y, 16.4), 0),
        ("Mannequin", (3.9, p6.WALK_Y, -1.5), 200),
        ("Tree", (-8.4, 0, -9.0), 0),
        ("Tree_Maple", (-8.2, 0, 7.5), 18),
    ]
    specs.extend((name, pos, yaw, 1.0) for name, pos, yaw in props)
    objs = p6._place(found, specs)
    p6._ground((0.40, 0.44, 0.38))
    p6._look(scene, (1.5, 3.4, -17.2), (7.2, 1.6, 9.0), lens=28.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_street.png"))
    return objs


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
        ("gazebo", lambda: _turntable(found, "Gazebo", os.path.join(STILL_DIR, "gazebo.png"), -52)),
        ("court", lambda: _turntable(found, "Court", os.path.join(STILL_DIR, "court.png"), 24, 52, 0.86, (0.15, 0.16, 0.15))),
        ("fire_hydrant", lambda: _turntable(found, "FireHydrant", os.path.join(STILL_DIR, "fire_hydrant.png"), 30)),
        ("median", lambda: _turntable(found, "Median_Planter", os.path.join(STILL_DIR, "median_planter.png"), 24)),
        ("cabin", lambda: _turntable(found, "Cabin", os.path.join(STILL_DIR, "cabin.png"), 28)),
        ("bike_rack", lambda: _turntable(found, "BikeRack", os.path.join(STILL_DIR, "bike_rack.png"), 36)),
        ("mailbox", lambda: _turntable(found, "Mailbox", os.path.join(STILL_DIR, "mailbox.png"), 28)),
        ("bus_shelter", lambda: _turntable(found, "BusShelter", os.path.join(STILL_DIR, "bus_shelter.png"), 32)),
        ("picnic_table", lambda: _turntable(found, "PicnicTable", os.path.join(STILL_DIR, "picnic_table.png"), 30)),
        ("chain_fence", lambda: _turntable(found, "ChainFence", os.path.join(STILL_DIR, "chain_fence.png"), 40)),
        ("sign_street", lambda: _turntable(found, "Sign_Street", os.path.join(STILL_DIR, "sign_street.png"), 26)),
        ("sign_stop", lambda: _turntable(found, "Sign_Stop", os.path.join(STILL_DIR, "sign_stop.png"), 22)),
        ("manhole", lambda: _turntable(found, "Manhole", os.path.join(STILL_DIR, "manhole.png"), 20, 58, 0.78)),
        ("storm_drain", lambda: _turntable(found, "StormDrain", os.path.join(STILL_DIR, "storm_drain.png"), 24, 52, 0.78)),
    ]
    for name, fn in shots:
        if only and only not in name:
            continue
        fn()
    print("PASS8_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
