"""Vehicle stills. Side profile, hero, and a 1.8 m figure.

  blender --background --python Tools/Blender/AssetLibrary/vehicles/render_stills.py -- --only sedan_midsize
"""

import importlib
import math
import os
import sys

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, ROOT)
sys.path.insert(0, HERE)

import render_pass2 as r  # noqa: E402
from _common import unity_to_blender  # noqa: E402

STILL_ROOT = os.path.join(r._common.REPO, "Docs", "AssetStills", "vehicles")
LIMIT = 400 * 1024

MODULES = (
    "sedan_mid_a",
    "sedan_compact",
    "crossover_compact",
    "hatch_compact",
    "bus_city40",
    "bus_city40_blue",
    "bus_city40_red",
    "bus_city60",
    "mannequin",
)

FONT = os.path.join(r._common.REPO, "Tools", "StrafeJumpSim", "Fonts", "LiberationSans-Regular.ttf")


def _wanted(stem, only):
    if not only:
        return True
    if stem == "mannequin":
        return True
    if only == "envelope":
        return stem in ("sedan_compact", "hatch_compact", "crossover_compact")
    return only in stem or stem in only


def _load(only=None):
    sys.path.insert(0, HERE)
    sys.path.insert(0, ROOT)
    found = {}
    for stem in MODULES:
        if not _wanted(stem, only):
            continue
        module = importlib.import_module(stem)
        if hasattr(module, "create_variants"):
            for asset in module.create_variants():
                found[asset.name] = (lambda a=asset: a)
        else:
            asset = module.create()
            found[asset.name] = module.create
    return found


def _fit(path):
    size = os.path.getsize(path)
    print("SIZE", size, path)
    if size > LIMIT:
        print("SIZE_OVER", size, path)


def _look(scene, eye, aim, lens):
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = lens
    cam_data.sensor_width = 36.0
    cam_data.sensor_height = 24.0
    cam_data.sensor_fit = "AUTO"
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.rotation_mode = "QUATERNION"
    cam.location = Vector(unity_to_blender(*eye))
    direction = Vector(unity_to_blender(*aim)) - cam.location
    cam.rotation_quaternion = direction.to_track_quat("-Z", "Y")
    return cam


def _shot(fn, path, fill=0.84):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 28
    r._ensure_materials()
    r._world(scene, night=False)
    obj = r._spawn(fn(), (0, 0, 0))
    r._ground("asphalt", 80.0)
    r._frame(scene, [obj], fill=fill, elevation=14.0, azimuth=38.0)
    r._render(scene, path)
    _fit(path)


def _fascia(fn, path):
    """Fascia close-up, 1280x720. The nose fills the frame."""
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.cycles.samples = 28
    r._ensure_materials()
    r._world(scene, night=False)
    obj = r._spawn(fn(), (0, 0, 0))
    import body_a
    body_a.shade_object(obj)
    r._ground("asphalt", 40.0)
    _look(scene, (0.15, 0.62, 4.6), (0.0, 0.55, 1.9), 55.0)
    r._render(scene, path)
    _fit(path)


def _close(fn, path, eye, aim, lens):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 28
    r._ensure_materials()
    r._world(scene, night=False)
    r._spawn(fn(), (0, 0, 0))
    r._ground("asphalt", 40.0)
    _look(scene, eye, aim, lens)
    r._render(scene, path)
    _fit(path)


def _with_figure(found, prop, path, prop_pos, prop_yaw, fig_pos, fill=0.78, wide=False, shade=False):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=wide)
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    objs = [
        r._spawn(found["Mannequin"](), fig_pos, 200),
        r._spawn(found[prop](), prop_pos, prop_yaw),
    ]
    if shade:
        import body_a
        body_a.shade_object(objs[1])
    r._ground("asphalt", 30.0)
    r._frame(scene, objs, fill=fill, elevation=12.0, azimuth=28.0)
    r._render(scene, path)
    _fit(path)


def _transit_set(found, name, folder, length, height, door_z):
    out = os.path.join(STILL_ROOT, folder, "pass1")
    os.makedirs(out, exist_ok=True)
    _shot_az(found[name], os.path.join(out, "hero.png"), 42.0, elevation=10.0, fill=0.88, wide=True)
    dist = max(18.0, length * 1.55)
    _side_dims(
        found[name], None, os.path.join(out, "side.png"),
        (dist, height * 0.46, length * 0.04), (0.0, height * 0.40, 0.0), 46.0,
        wide=True,
    )
    # Nose and the destination glass. This is the close-up.
    _side_dims(
        found[name], None, os.path.join(out, "nose.png"),
        (1.8, 1.7, length * 0.5 + 4.2), (0.0, 1.55, length * 0.38), 36.0,
        wide=True,
    )
    # Figure at the curb door so the 1.8 m body reads against the step and the glass.
    _close_pair(
        found, name, os.path.join(out, "scale.png"),
        (0.0, 0.0, 0.0), 0.0,
        (1.85, 0.0, door_z), 90.0,
        (6.2, 1.55, door_z + 2.4), (1.15, 1.15, door_z), 32.0,
        wide=True,
    )


def _close_pair(found, prop, path, prop_pos, prop_yaw, fig_pos, fig_yaw, eye, aim, lens, wide=False):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=wide)
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    r._spawn(found["Mannequin"](), fig_pos, fig_yaw)
    r._spawn(found[prop](), prop_pos, prop_yaw)
    r._ground("asphalt", 40.0)
    _look(scene, eye, aim, lens)
    r._render(scene, path)
    _fit(path)


def _shot_az(fn, path, azimuth, elevation=14.0, fill=0.84, wide=False, shade=False):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=wide)
    scene.cycles.samples = 28
    r._ensure_materials()
    r._world(scene, night=False)
    obj = r._spawn(fn(), (0, 0, 0))
    if shade:
        import body_a
        body_a.shade_object(obj)
    r._ground("asphalt", 80.0)
    r._frame(scene, [obj], fill=fill, elevation=elevation, azimuth=azimuth)
    r._render(scene, path)
    _fit(path)


def _dimensions(length, height, wheelbase, z_front, z_rear):
    """Side-view ticks in the sky and beside the body. Numbers only, no names."""
    from _common import Asset

    asset = Asset("Dims", "Vehicles", "Dimension overlay.")
    g = asset.begin(0)
    # Camera sits on +X. Bars are thick enough to read at a full-car frame.
    x = 1.45
    y_len = height + 0.22
    g.box((x, y_len, 0.0), (0.04, 0.028, length), "Lib_Lane")
    g.box((x, y_len - 0.10, length * 0.5), (0.04, 0.20, 0.028), "Lib_Lane")
    g.box((x, y_len - 0.10, -length * 0.5), (0.04, 0.20, 0.028), "Lib_Lane")
    g.text("%d mm" % int(round(length * 1000)), (x, y_len + 0.14, 0.0), 0.16, "Lib_Lane", extrude=0.012, yaw=90.0, font=FONT)
    z = -length * 0.5 - 0.28
    g.box((x, height * 0.5, z), (0.04, height, 0.028), "Lib_Lane")
    g.box((x, 0.02, z), (0.04, 0.02, 0.22), "Lib_Lane")
    g.box((x, height, z), (0.04, 0.02, 0.22), "Lib_Lane")
    g.text("%d mm" % int(round(height * 1000)), (x, height * 0.45, z - 0.28), 0.12, "Lib_Lane", extrude=0.012, yaw=90.0, font=FONT)
    mid = (z_front + z_rear) * 0.5
    g.box((x, 0.42, mid), (0.03, 0.022, wheelbase), "Lib_Lane")
    g.box((x, 0.42, z_front), (0.03, 0.16, 0.022), "Lib_Lane")
    g.box((x, 0.42, z_rear), (0.03, 0.16, 0.022), "Lib_Lane")
    g.text("%d mm" % int(round(wheelbase * 1000)), (x, 0.62, mid), 0.12, "Lib_Lane", extrude=0.012, yaw=90.0, font=FONT)
    asset.end()
    return asset


def _side_dims(fn, dims, path, eye, aim, lens, wide=False, shade=False, guides=None):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=wide)
    scene.cycles.samples = 28
    r._ensure_materials()
    r._world(scene, night=False)
    obj = r._spawn(fn(), (0, 0, 0))
    if shade:
        import body_a
        body_a.shade_object(obj)
    if dims is not None:
        r._spawn(dims, (0, 0, 0))
    if guides is not None:
        r._spawn(guides, (0, 0, 0))
    r._ground("asphalt", 40.0)
    _look(scene, eye, aim, lens)
    r._render(scene, path)
    _fit(path)


def _beside(fn, path):
    """Side ortho of the car with the reference profile beside it, not on it."""
    import body_a

    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.cycles.samples = 20
    r._ensure_materials()
    r._world(scene, night=False)
    body = r._spawn(fn(), (0.0, 0.0, 0.0))
    body_a.shade_object(body)
    guides = body_a.reference_asset(views=("side",))
    r._spawn(guides, (0.0, 0.0, 6.4))
    r._ground("asphalt", 40.0)
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 14.6
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.location = Vector(unity_to_blender(16.0, 0.72, 3.15))
    direction = Vector(unity_to_blender(0.0, 0.72, 3.15)) - cam.location
    cam.rotation_mode = "QUATERNION"
    cam.rotation_quaternion = direction.to_track_quat("-Z", "Y")
    r._render(scene, path)
    _fit(path)


def _lineup(found, names, path, azimuth=36.0, elevation=12.0, fill=0.90):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 16
    r._ensure_materials()
    r._world(scene, night=False)
    objs = []
    span = 3.55
    origin = (len(names) - 1) * span * 0.5
    for i, name in enumerate(names):
        objs.append(r._spawn(found[name](), (i * span - origin, 0.0, 0.0), 0.0))
    r._ground("asphalt", 80.0)
    r._frame(scene, objs, fill=fill, elevation=elevation, azimuth=azimuth)
    r._render(scene, path)
    _fit(path)


def _lineup_side(found, names, path, wide=False, shade=False):
    """Five cars in profile, nose to image-left, opened just enough to see the fascia."""
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=wide)
    scene.cycles.samples = 16
    r._ensure_materials()
    r._world(scene, night=False)
    objs = []
    span = 6.55
    origin = (len(names) - 1) * span * 0.5
    for i, name in enumerate(names):
        obj = r._spawn(found[name](), (0.0, 0.0, i * span - origin), 0.0)
        if shade:
            import body_a
            body_a.shade_object(obj)
        objs.append(obj)
    r._ground("asphalt", 90.0)
    # Azimuth 90 is pure side from +X, and that puts the nose on the left.
    # 74 degrees swings the camera toward the nose so the grille and lamps show.
    r._frame(scene, objs, fill=0.93, elevation=6.0, azimuth=74.0)
    r._render(scene, path)
    _fit(path)


def _quartet(found, name, out, length, height):
    """Quarter, side, nose close-up, and a 1.8 m figure. 1280x720."""
    os.makedirs(out, exist_ok=True)
    nose = length * 0.5
    _shot_az(found[name], os.path.join(out, "hero.png"), 42.0, elevation=11.0, fill=0.88, wide=True)
    dist = max(12.0, length * 2.4)
    _side_dims(
        found[name], None, os.path.join(out, "side.png"),
        (dist, height * 0.55, 0.0), (0.0, height * 0.45, 0.0), 70.0,
        wide=True,
    )
    _side_dims(
        found[name], None, os.path.join(out, "nose.png"),
        (1.35, height * 0.48, nose + 1.55), (0.0, height * 0.42, nose - 0.45), 42.0,
        wide=True,
    )
    _with_figure(
        found, name, os.path.join(out, "scale.png"),
        (0.0, 0.0, 0.0), 12, (1.55, 0.0, 0.35),
        fill=0.86, wide=True,
    )
    _squeeze(out)


def _squeeze(out):
    import subprocess
    for stem in ("hero.png", "side.png", "nose.png", "scale.png"):
        path = os.path.join(out, stem)
        if not os.path.isfile(path) or os.path.getsize(path) <= LIMIT:
            continue
        subprocess.check_call([
            "pngquant", "--quality=55-82", "--speed", "1", "--force",
            "--ext", ".png", "--skip-if-larger", path,
        ])
        print("QUANT", os.path.getsize(path), path)


def main():
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1]
    shot = None
    if "--shot" in sys.argv:
        shot = sys.argv[sys.argv.index("--shot") + 1]
    found = _load(only)
    if only is not None and "sedan_mid_a" in only:
        print("SHOT", "sedan_mid_a", shot or "all")
        out = os.path.join(STILL_ROOT, "sedan_mid_a", "pass15")
        os.makedirs(out, exist_ok=True)
        if shot in (None, "hero"):
            _shot_az(found["Sedan_Mid_A_25"], os.path.join(out, "hero.png"), 48.0, elevation=11.0, fill=0.90, wide=True, shade=True)
        # Clean profile so the belt and the window outline can be judged.
        if shot in (None, "side"):
            _side_dims(
                found["Sedan_Mid_A_25"],
                None,
                os.path.join(out, "side.png"),
                (14.0, 0.82, 0.0), (0.0, 0.72, 0.0), 90.0,
                wide=True, shade=True, guides=None,
            )
        if shot in (None, "front"):
            _shot_az(found["Sedan_Mid_A_25"], os.path.join(out, "front.png"), 0.0, elevation=2.0, fill=0.86, wide=True, shade=True)
        if shot in (None, "nose"):
            _fascia(found["Sedan_Mid_A_25"], os.path.join(out, "nose.png"))
        if shot in (None, "rear"):
            _shot_az(found["Sedan_Mid_A_25"], os.path.join(out, "rear.png"), 180.0, elevation=4.0, fill=0.86, wide=True, shade=True)
        if shot in (None, "top"):
            _shot_az(found["Sedan_Mid_A_25"], os.path.join(out, "top.png"), 0.0, elevation=86.0, fill=0.90, wide=True, shade=True)
        if shot == "beside":
            _beside(found["Sedan_Mid_A_25"], os.path.join(out, "beside.png"))
        if shot in (None, "scale"):
            _with_figure(
                found, "Sedan_Mid_A_25", os.path.join(out, "scale.png"),
                (0.4, 0.0, 0.0), 18, (-1.7, 0.0, 1.55), fill=0.88,
                wide=True, shade=True,
            )
        if shot == "lineup":
            _lineup_side(
                found,
                (
                    "Sedan_Mid_A_21",
                    "Sedan_Mid_A_22",
                    "Sedan_Mid_A_23",
                    "Sedan_Mid_A_24",
                    "Sedan_Mid_A_25",
                ),
                os.path.join(out, "lineup.png"),
                wide=True, shade=True,
            )
        return
    if only == "envelope":
        jobs = (
            ("Sedan_Compact_25", "sedan_compact", 4.66, 1.42),
            ("Hatch_Compact_25", "hatch_compact", 4.42, 1.42),
            ("Crossover_Compact_25", "crossover_compact", 4.66, 1.68),
        )
        for name, folder, length, height in jobs:
            print("SHOT", folder)
            _quartet(found, name, os.path.join(STILL_ROOT, folder, "pass17"), length, height)
        return
    if only == "street":
        import sk_car_hatch
        import sk_car_pickup
        import sk_car_sedan
        found["Car_Sedan_25"] = sk_car_sedan.create
        found["Car_Hatch_25"] = sk_car_hatch.create
        found["Car_Pickup_25"] = sk_car_pickup.create
        street = os.path.join(r._common.REPO, "Docs", "AssetStills", "street_kit")
        jobs = (
            ("Car_Sedan_25", "car_sedan", 4.835, 1.46),
            ("Car_Hatch_25", "car_hatch", 4.035, 1.52),
            ("Car_Pickup_25", "car_pickup", 5.105, 1.76),
        )
        for name, folder, length, height in jobs:
            print("SHOT", folder)
            _quartet(found, name, os.path.join(street, folder, "pass17"), length, height)
        return
    if only is None or "bus" in only:
        # Door center from the same overhang used by the shell.
        door_z = 5.10
        for name, folder in (
            ("Bus_City40", "bus_city40"),
            ("Bus_City40_Blue", "bus_city40_blue"),
            ("Bus_City40_Red", "bus_city40_red"),
        ):
            if only not in (None, "bus") and only not in folder and folder not in only:
                continue
            print("SHOT", folder)
            _transit_set(found, name, folder, 12.50, 3.20, door_z)
    if only is None or "city60" in only or only == "bus60":
        print("SHOT", "bus_city60")
        _transit_set(found, "Bus_City60", "bus_city60", 18.54, 3.20, 8.0)
    print("VEHICLE_STILLS", STILL_ROOT)


if __name__ == "__main__":
    main()
