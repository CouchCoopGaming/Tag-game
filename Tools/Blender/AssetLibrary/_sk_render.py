"""Street-kit stills. Close-ups plus one lineup with the 1.8 m figure.

  blender --background --python Tools/Blender/AssetLibrary/_sk_render.py -- --pass 1
"""

import importlib
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass2 as r  # noqa: E402
from _common import unity_to_blender  # noqa: E402

PASS = 1
if "--pass" in sys.argv:
    PASS = int(sys.argv[sys.argv.index("--pass") + 1])
ONLY = None
if "--only" in sys.argv:
    ONLY = sys.argv[sys.argv.index("--only") + 1]

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "street_kit", "pass%d" % PASS)
LIMIT = 400 * 1024

MODULES = (
    "sk_light_globe",
    "sk_light_mast",
    "sk_hydrant_yellow",
    "sk_hydrant_silver",
    "sk_bench_metal",
    "sk_bench_woodiron",
    "sk_trash_drum",
    "sk_recycling_dual",
    "sk_dumpster_rear",
    "sk_bus_shelter",
    "sk_newspaper_rack",
    "sk_meter_twin",
    "sk_bike_wave",
    "sk_bollard_lit",
    "sk_bollard_chain",
    "sk_cone_tall",
    "sk_barrier_water",
    "sk_barrier_sawhorse",
    "sk_power_pole",
    "sk_sign_yield",
    "sk_sign_oneway",
    "sk_sign_blades",
    "sk_planter_street",
    "sk_manhole_ring",
    "sk_storm_curb",
    "sk_roof_vent",
    "sk_tank_saddle",
    "sk_ladder_fixed",
    "sk_satellite",
    "sk_hydrant_red",
    "sk_ac_split",
    "sk_car_sedan",
    "sk_car_hatch",
    "sk_car_pickup",
    "sk_traffic_signal",
    "sk_bus_curbside",
    "sk_meter_single",
    "sk_mail_drop",
    "sk_wheel_stop",
    "sk_guardrail",
    "sk_pay_kiosk",
    "sk_bike_locker",
    "sk_call_box",
    "sk_speed_cushion",
    "sk_street_clock",
    "sk_bike_pump",
    "sk_menu_board",
    "sk_curb_ramp",
    "sk_fire_siamese",
    "sk_wayfinding",
    "sk_tree_guard",
    "sk_dog_bag",
    "sk_bus_flag",
    "mannequin",
)


def _load():
    found = {}
    for stem in MODULES:
        module = importlib.import_module(stem)
        asset = module.create()
        found[asset.name] = module.create
    return found


def _shot(fn, path, kind="asphalt", fill=0.72):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 28
    r._ensure_materials()
    r._world(scene, night=False)
    asset = fn()
    obj = r._spawn(asset, (0, 0, 0))
    r._ground(kind, 80.0)
    r._frame(scene, [obj], fill=fill, elevation=14.0, azimuth=38.0)
    r._render(scene, path)
    _fit(path)


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


def _close(fn, path, eye, aim, lens=48.0):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 32
    r._ensure_materials()
    r._world(scene, night=False)
    asset = fn()
    r._spawn(asset, (0, 0, 0))
    r._ground("asphalt", 40.0)
    _look(scene, eye, aim, lens)
    r._render(scene, path)
    _fit(path)


def _lineup(found, path):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    specs = [
        ("Mannequin", (0.0, 0.0, 0.4), 200),
        ("FireHydrant_Yellow", (1.15, 0.0, 0.0), 25),
        ("FireHydrant_Silver", (2.15, 0.0, 0.15), -15),
        ("TrashCan_Drum", (3.15, 0.0, 0.1), 20),
        ("RecyclingBin_Dual", (4.55, 0.0, 0.15), 8),
        ("Bench_Metal", (6.55, 0.0, 0.55), 90),
        ("Bench_WoodIron", (8.85, 0.0, 0.55), 90),
        ("Dumpster_Rear", (11.35, 0.0, 0.2), 18),
        ("LightPost_Globe", (13.4, 0.0, -0.4), 15),
        ("LightPost_Mast", (16.2, 0.0, -1.2), 0),
    ]
    objs = []
    for name, pos, yaw in specs:
        objs.append(r._spawn(found[name](), pos, yaw))
    r._ground("concrete", 80.0)
    r._frame(scene, objs, fill=0.70, elevation=13.0, azimuth=22.0)
    r._render(scene, path)
    _fit(path)


def _fit(path):
    size = os.path.getsize(path)
    if size <= LIMIT:
        print("SIZE_OK", size, path)
        return
    try:
        from PIL import Image
        img = Image.open(path)
        img.save(path, format="PNG", optimize=True, compress_level=9)
    except Exception as exc:
        print("PIL", exc)
    size = os.path.getsize(path)
    print("SIZE", size, path)
    if size > LIMIT:
        print("SIZE_OVER", size, path)


def _pass2_lineup(found, path):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    specs = [
        ("Mannequin", (0.0, 0.0, 0.6), 190),
        ("TrafficCone_Tall", (1.0, 0.0, 0.1), 10),
        ("Bollard_Lit", (1.9, 0.0, 0.0), 0),
        ("Bollard_Chain", (3.6, 0.0, 0.1), 0),
        ("ParkingMeter_Twin", (5.3, 0.0, 0.0), 15),
        ("NewspaperRack", (6.8, 0.0, 0.15), 8),
        ("BikeRack_Hoop3", (8.8, 0.0, 0.2), 8),
        ("Barrier_Sawhorse", (11.2, 0.0, 0.3), 12),
        ("Barrier_Water", (13.6, 0.0, 0.2), -8),
        ("BusShelter_City", (17.2, 0.0, 0.4), 20),
    ]
    objs = [r._spawn(found[name](), pos, yaw) for name, pos, yaw in specs]
    r._ground("concrete", 80.0)
    r._frame(scene, objs, fill=0.72, elevation=12.0, azimuth=24.0)
    r._render(scene, path)
    _fit(path)


def _pass3_lineup(found, path):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    specs = [
        ("Mannequin", (0.0, 0.0, 1.2), 200),
        ("Manhole_Ring", (1.3, 0.0, 0.2), 15),
        ("StormDrain_Curb", (3.2, 0.0, 0.15), 20),
        ("Planter_Street", (5.4, 0.0, 0.3), 12),
        ("Sign_OneWay", (7.3, 0.0, 0.0), 18),
        ("Sign_Yield", (8.8, 0.0, 0.0), -12),
        ("Sign_Blades", (10.4, 0.0, 0.0), 25),
        ("PowerPole_Span", (18.5, 0.0, 0.0), 8),
    ]
    objs = [r._spawn(found[name](), pos, yaw) for name, pos, yaw in specs]
    r._ground("concrete", 120.0)
    r._frame(scene, objs, fill=0.68, elevation=11.0, azimuth=18.0)
    r._render(scene, path)
    _fit(path)


def _pass4_lineup(found, path):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    specs = [
        ("Mannequin", (0.0, 0.0, 0.6), 200),
        ("RoofVent_Turbine", (1.2, 0.0, 0.1), 20),
        ("AC_MiniSplit", (2.5, 0.0, 0.1), 15),
        ("FireHydrant_Red", (3.7, 0.0, 0.0), 25),
        ("SatelliteDish", (5.1, 0.0, 0.2), 18),
        ("WaterTank_Saddle", (7.2, 0.0, 0.2), 12),
        ("Ladder_Fixed", (9.2, 0.0, 0.1), 8),
    ]
    objs = [r._spawn(found[name](), pos, yaw) for name, pos, yaw in specs]
    r._ground("concrete", 80.0)
    r._frame(scene, objs, fill=0.72, elevation=12.0, azimuth=22.0)
    r._render(scene, path)
    _fit(path)


def _pass5_lineup(found, path):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    specs = [
        ("Mannequin", (0.0, 0.0, 1.0), 200),
        ("Car_Hatch", (3.4, 0.0, 0.0), 25),
        ("Car_Sedan", (8.6, 0.0, 0.0), 20),
        ("Car_Pickup", (14.6, 0.0, 0.0), 15),
    ]
    objs = [r._spawn(found[name](), pos, yaw) for name, pos, yaw in specs]
    r._ground("asphalt", 80.0)
    r._frame(scene, objs, fill=0.74, elevation=12.0, azimuth=28.0)
    r._render(scene, path)
    _fit(path)


def _pass7_lineup(found, path):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    specs = [
        ("Mannequin", (0.0, 0.0, 0.5), 200),
        ("Planter_Street", (2.2, 0.0, 0.2), 18),
        ("SatelliteDish", (4.2, 0.0, 0.15), 24),
        ("Sign_Blades", (6.0, 0.0, 0.0), 30),
    ]
    objs = [r._spawn(found[name](), pos, yaw) for name, pos, yaw in specs]
    r._ground("concrete", 40.0)
    r._frame(scene, objs, fill=0.72, elevation=12.0, azimuth=24.0)
    r._render(scene, path)
    _fit(path)


def _pass8_lineup(found, path):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    specs = [
        ("Mannequin", (-0.15, 0.0, 0.35), 200),
        ("RoofVent_Turbine", (1.05, 0.0, 0.05), 30),
    ]
    objs = [r._spawn(found[name](), pos, yaw) for name, pos, yaw in specs]
    r._ground("concrete", 20.0)
    r._frame(scene, objs, fill=0.58, elevation=12.0, azimuth=24.0)
    r._render(scene, path)
    _fit(path)


def _pass9_lineup(found, path):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    specs = [
        ("Mannequin", (0.0, 0.0, 1.15), 200),
        ("Car_Hatch", (3.3, 0.0, 0.2), 26),
        ("Car_Sedan", (8.4, 0.0, 0.15), 20),
        ("Car_Pickup", (14.2, 0.0, 0.05), 16),
    ]
    objs = [r._spawn(found[name](), pos, yaw) for name, pos, yaw in specs]
    r._ground("asphalt", 80.0)
    r._frame(scene, objs, fill=0.76, elevation=11.0, azimuth=30.0)
    r._render(scene, path)
    _fit(path)


def _with_figure(found, prop, path, prop_pos, prop_yaw, fig_pos):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 28
    r._ensure_materials()
    r._world(scene, night=False)
    specs = [
        ("Mannequin", fig_pos, 200),
        (prop, prop_pos, prop_yaw),
    ]
    objs = [r._spawn(found[name](), pos, yaw) for name, pos, yaw in specs]
    r._ground("asphalt", 30.0)
    r._frame(scene, objs, fill=0.78, elevation=12.0, azimuth=28.0)
    r._render(scene, path)
    _fit(path)


def _pass12_lineup(found, path):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    specs = [
        ("Mannequin", (-1.3, 0.0, 1.4), 160),
        ("TrafficSignal_Mast", (0.0, 0.0, 0.0), 18),
    ]
    objs = [r._spawn(found[name](), pos, yaw) for name, pos, yaw in specs]
    r._ground("asphalt", 40.0)
    r._frame(scene, objs, fill=0.82, elevation=12.0, azimuth=32.0)
    r._render(scene, path)
    _fit(path)


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    found = _load()
    if PASS >= 18:
        shots = [
            ("tree_guard", lambda: _shot(found["TreeGuard_Square"], os.path.join(STILL_DIR, "tree_guard.png"), fill=0.84)),
            ("tree_guard_scale", lambda: _with_figure(
                found, "TreeGuard_Square", os.path.join(STILL_DIR, "tree_guard_scale.png"),
                (0.0, 0.0, 0.0), 20, (-1.2, 0.0, 0.7))),
            ("dog_bag", lambda: _shot(found["DogBag_Post"], os.path.join(STILL_DIR, "dog_bag.png"), fill=0.84)),
            ("dog_bag_scale", lambda: _with_figure(
                found, "DogBag_Post", os.path.join(STILL_DIR, "dog_bag_scale.png"),
                (0.4, 0.0, 0.0), 16, (-0.9, 0.0, 0.25))),
            ("bus_flag", lambda: _shot(found["BusFlag_Stop"], os.path.join(STILL_DIR, "bus_flag.png"), fill=0.86)),
            ("bus_flag_scale", lambda: _with_figure(
                found, "BusFlag_Stop", os.path.join(STILL_DIR, "bus_flag_scale.png"),
                (0.2, 0.0, 0.0), 8, (-1.1, 0.0, 0.7))),
        ]
    elif PASS >= 17:
        shots = [
            ("curb_ramp", lambda: _shot(found["CurbRamp_Detectable"], os.path.join(STILL_DIR, "curb_ramp.png"), fill=0.84)),
            ("curb_ramp_scale", lambda: _with_figure(
                found, "CurbRamp_Detectable", os.path.join(STILL_DIR, "curb_ramp_scale.png"),
                (0.0, 0.0, 0.0), 16, (-1.15, 0.0, 0.7))),
            ("fire_siamese", lambda: _shot(found["FireSiamese_Post"], os.path.join(STILL_DIR, "fire_siamese.png"), fill=0.84)),
            ("fire_siamese_scale", lambda: _with_figure(
                found, "FireSiamese_Post", os.path.join(STILL_DIR, "fire_siamese_scale.png"),
                (0.45, 0.0, 0.0), 18, (-0.9, 0.0, 0.35))),
            ("wayfinding", lambda: _shot(found["Wayfinding_Pylon"], os.path.join(STILL_DIR, "wayfinding.png"), fill=0.86)),
            ("wayfinding_scale", lambda: _with_figure(
                found, "Wayfinding_Pylon", os.path.join(STILL_DIR, "wayfinding_scale.png"),
                (0.0, 0.0, 0.0), 10, (-1.15, 0.0, 0.65))),
        ]
    elif PASS >= 16:
        shots = [
            ("street_clock", lambda: _shot(found["StreetClock_Post"], os.path.join(STILL_DIR, "street_clock.png"), fill=0.86)),
            ("street_clock_scale", lambda: _with_figure(
                found, "StreetClock_Post", os.path.join(STILL_DIR, "street_clock_scale.png"),
                (0.0, 0.0, 0.0), 12, (-1.15, 0.0, 0.7))),
            ("bike_pump", lambda: _shot(found["BikePump_Public"], os.path.join(STILL_DIR, "bike_pump.png"), fill=0.84)),
            ("bike_pump_scale", lambda: _with_figure(
                found, "BikePump_Public", os.path.join(STILL_DIR, "bike_pump_scale.png"),
                (0.45, 0.0, 0.0), 16, (-0.9, 0.0, 0.25))),
            ("menu_board", lambda: _shot(found["MenuBoard_Aframe"], os.path.join(STILL_DIR, "menu_board.png"), fill=0.84)),
            ("menu_board_scale", lambda: _with_figure(
                found, "MenuBoard_Aframe", os.path.join(STILL_DIR, "menu_board_scale.png"),
                (0.45, 0.0, 0.0), 20, (-0.9, 0.0, 0.55))),
        ]
    elif PASS >= 15:
        shots = [
            ("bike_locker", lambda: _shot(found["BikeLocker_Single"], os.path.join(STILL_DIR, "bike_locker.png"), fill=0.86)),
            ("bike_locker_scale", lambda: _with_figure(
                found, "BikeLocker_Single", os.path.join(STILL_DIR, "bike_locker_scale.png"),
                (0.55, 0.0, 0.0), 20, (-1.05, 0.0, 0.35))),
            ("call_box", lambda: _shot(found["CallBox_Pedestal"], os.path.join(STILL_DIR, "call_box.png"), fill=0.84)),
            ("call_box_scale", lambda: _with_figure(
                found, "CallBox_Pedestal", os.path.join(STILL_DIR, "call_box_scale.png"),
                (0.55, 0.0, 0.0), 16, (-0.85, 0.0, 0.2))),
            ("speed_cushion", lambda: _shot(found["SpeedCushion_Asphalt"], os.path.join(STILL_DIR, "speed_cushion.png"), fill=0.84)),
            ("speed_cushion_scale", lambda: _with_figure(
                found, "SpeedCushion_Asphalt", os.path.join(STILL_DIR, "speed_cushion_scale.png"),
                (0.0, 0.0, 0.0), 12, (-1.3, 0.0, 0.9))),
        ]
    elif PASS >= 14:
        shots = [
            ("wheel_stop", lambda: _shot(found["WheelStop_Concrete"], os.path.join(STILL_DIR, "wheel_stop.png"), fill=0.84)),
            ("wheel_stop_scale", lambda: _with_figure(
                found, "WheelStop_Concrete", os.path.join(STILL_DIR, "wheel_stop_scale.png"),
                (0.0, 0.0, 0.0), 18, (-1.15, 0.0, 0.85))),
            ("guardrail", lambda: _shot(found["Guardrail_WBeam"], os.path.join(STILL_DIR, "guardrail.png"), fill=0.86)),
            ("guardrail_scale", lambda: _with_figure(
                found, "Guardrail_WBeam", os.path.join(STILL_DIR, "guardrail_scale.png"),
                (0.0, 0.0, 0.0), 16, (-1.6, 0.0, 1.15))),
            ("pay_kiosk", lambda: _shot(found["PayKiosk_Lot"], os.path.join(STILL_DIR, "pay_kiosk.png"), fill=0.84)),
            ("pay_kiosk_scale", lambda: _with_figure(
                found, "PayKiosk_Lot", os.path.join(STILL_DIR, "pay_kiosk_scale.png"),
                (0.55, 0.0, 0.0), 18, (-0.85, 0.0, 0.25))),
        ]
    elif PASS >= 13:
        shots = [
            ("car_sedan", lambda: _shot(found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan.png"), kind="asphalt", fill=0.84)),
            ("car_sedan_nose", lambda: _close(
                found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan_nose.png"),
                (1.35, 1.15, 2.55), (0.15, 1.15, 0.75), 42)),
            ("car_sedan_door", lambda: _close(
                found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan_door.png"),
                (2.05, 1.22, 0.70), (0.40, 1.02, -0.35), 38)),
            ("bus_stop", lambda: _shot(found["BusStop_Curbside"], os.path.join(STILL_DIR, "bus_stop.png"), fill=0.86)),
            ("bus_stop_scale", lambda: _with_figure(
                found, "BusStop_Curbside", os.path.join(STILL_DIR, "bus_stop_scale.png"),
                (0.0, 0.0, 0.0), 24, (-1.7, 0.0, 1.15))),
            ("parking_meter", lambda: _shot(found["ParkingMeter_Single"], os.path.join(STILL_DIR, "parking_meter.png"), fill=0.82)),
            ("parking_meter_scale", lambda: _with_figure(
                found, "ParkingMeter_Single", os.path.join(STILL_DIR, "parking_meter_scale.png"),
                (0.55, 0.0, 0.0), 20, (-0.7, 0.0, 0.15))),
            ("mail_drop", lambda: _shot(found["MailDrop_Corner"], os.path.join(STILL_DIR, "mail_drop.png"), fill=0.84)),
            ("mail_drop_scale", lambda: _with_figure(
                found, "MailDrop_Corner", os.path.join(STILL_DIR, "mail_drop_scale.png"),
                (0.7, 0.0, 0.0), 18, (-0.75, 0.0, 0.2))),
        ]
    elif PASS >= 12:
        shots = [
            ("traffic_signal", lambda: _shot(
                found["TrafficSignal_Mast"], os.path.join(STILL_DIR, "traffic_signal.png"),
                kind="asphalt", fill=0.86)),
            ("traffic_head", lambda: _close(
                found["TrafficSignal_Mast"], os.path.join(STILL_DIR, "traffic_head.png"),
                (5.6, 3.5, 2.4), (4.5, 3.42, 0.1), 42)),
            ("traffic_ped", lambda: _close(
                found["TrafficSignal_Mast"], os.path.join(STILL_DIR, "traffic_ped.png"),
                (1.05, 2.20, 0.7), (0.14, 2.22, 0.0), 48)),
            ("kit_lineup", lambda: _pass12_lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
        ]
    elif PASS >= 11:
        shots = [
            ("car_sedan", lambda: _shot(found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan.png"), kind="asphalt", fill=0.84)),
            ("car_sedan_door", lambda: _close(
                found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan_door.png"),
                (1.55, 1.05, 1.15), (0.88, 0.92, 0.22), 46)),
            ("car_sedan_nose", lambda: _close(
                found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan_nose.png"),
                (1.45, 0.85, 3.7), (0.15, 0.7, 1.6), 42)),
            ("car_hatch", lambda: _shot(found["Car_Hatch"], os.path.join(STILL_DIR, "car_hatch.png"), kind="asphalt", fill=0.84)),
            ("car_pickup", lambda: _shot(found["Car_Pickup"], os.path.join(STILL_DIR, "car_pickup.png"), kind="asphalt", fill=0.84)),
            ("sign_blades", lambda: _close(
                found["Sign_Blades"], os.path.join(STILL_DIR, "sign_blades.png"),
                (1.15, 2.55, 1.35), (0.0, 2.92, 0.0), 42)),
            ("planter_street", lambda: _close(
                found["Planter_Street"], os.path.join(STILL_DIR, "planter_street.png"),
                (1.45, 0.85, 1.25), (-0.05, 0.55, 0.0), 40)),
            ("kit_lineup", lambda: _pass9_lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
        ]
    elif PASS >= 10:
        shots = [
            ("car_sedan", lambda: _shot(found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan.png"), kind="asphalt", fill=0.84)),
            ("car_sedan_nose", lambda: _close(
                found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan_nose.png"),
                (1.45, 0.85, 3.7), (0.15, 0.7, 1.6), 42)),
            ("car_sedan_wheel", lambda: _close(
                found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan_wheel.png"),
                (1.7, 0.55, 2.15), (0.82, 0.32, 1.35), 48)),
            ("car_hatch", lambda: _shot(found["Car_Hatch"], os.path.join(STILL_DIR, "car_hatch.png"), kind="asphalt", fill=0.84)),
            ("car_pickup", lambda: _shot(found["Car_Pickup"], os.path.join(STILL_DIR, "car_pickup.png"), kind="asphalt", fill=0.84)),
            ("sign_yield", lambda: _close(
                found["Sign_Yield"], os.path.join(STILL_DIR, "sign_yield.png"),
                (0.95, 2.35, 1.45), (0.0, 2.70, 0.06), 46)),
            ("sign_oneway", lambda: _close(
                found["Sign_OneWay"], os.path.join(STILL_DIR, "sign_oneway.png"),
                (1.05, 2.15, 1.35), (0.12, 2.48, 0.07), 42)),
            ("sign_blades", lambda: _close(
                found["Sign_Blades"], os.path.join(STILL_DIR, "sign_blades.png"),
                (1.15, 2.55, 1.35), (0.0, 2.92, 0.0), 42)),
            ("planter_street", lambda: _shot(found["Planter_Street"], os.path.join(STILL_DIR, "planter_street.png"), fill=0.86)),
            ("roof_vent", lambda: _shot(found["RoofVent_Turbine"], os.path.join(STILL_DIR, "roof_vent.png"), fill=0.82)),
            ("satellite", lambda: _shot(found["SatelliteDish"], os.path.join(STILL_DIR, "satellite.png"), fill=0.84)),
            ("kit_lineup", lambda: _pass9_lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
        ]
    elif PASS >= 9:
        close = 0.86
        shots = [
            ("car_sedan", lambda: _shot(found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan.png"), kind="asphalt", fill=0.82)),
            ("car_sedan_side", lambda: _close(
                found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan_side.png"),
                (3.2, 1.2, 0.35), (0.55, 0.8, -0.05), 46)),
            ("car_sedan_nose", lambda: _close(
                found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan_nose.png"),
                (1.5, 0.95, 4.1), (0.0, 0.6, 1.9), 48)),
            ("car_hatch", lambda: _shot(found["Car_Hatch"], os.path.join(STILL_DIR, "car_hatch.png"), kind="asphalt", fill=0.82)),
            ("car_hatch_side", lambda: _close(
                found["Car_Hatch"], os.path.join(STILL_DIR, "car_hatch_side.png"),
                (2.9, 1.15, 0.2), (0.4, 0.8, -0.1), 46)),
            ("car_pickup", lambda: _shot(found["Car_Pickup"], os.path.join(STILL_DIR, "car_pickup.png"), kind="asphalt", fill=0.84)),
            ("car_pickup_side", lambda: _close(
                found["Car_Pickup"], os.path.join(STILL_DIR, "car_pickup_side.png"),
                (3.4, 1.4, 0.1), (0.4, 0.95, -0.4), 42)),
            ("satellite", lambda: _shot(found["SatelliteDish"], os.path.join(STILL_DIR, "satellite.png"), fill=0.82)),
            ("satellite_bowl", lambda: _close(
                found["SatelliteDish"], os.path.join(STILL_DIR, "satellite_bowl.png"),
                (0.95, 0.85, 1.15), (0.0, 0.5, 0.15), 50)),
            ("cone_tall", lambda: _shot(found["TrafficCone_Tall"], os.path.join(STILL_DIR, "cone_tall.png"), fill=close)),
            ("bollard_lit", lambda: _shot(found["Bollard_Lit"], os.path.join(STILL_DIR, "bollard_lit.png"), fill=close)),
            ("bollard_chain", lambda: _shot(found["Bollard_Chain"], os.path.join(STILL_DIR, "bollard_chain.png"), fill=close)),
            ("meter_twin", lambda: _shot(found["ParkingMeter_Twin"], os.path.join(STILL_DIR, "meter_twin.png"), fill=close)),
            ("bike_wave", lambda: _shot(found["BikeRack_Hoop3"], os.path.join(STILL_DIR, "bike_wave.png"), fill=close)),
            ("newspaper_rack", lambda: _shot(found["NewspaperRack"], os.path.join(STILL_DIR, "newspaper_rack.png"), fill=close)),
            ("barrier_sawhorse", lambda: _shot(found["Barrier_Sawhorse"], os.path.join(STILL_DIR, "barrier_sawhorse.png"), fill=close)),
            ("barrier_water", lambda: _shot(found["Barrier_Water"], os.path.join(STILL_DIR, "barrier_water.png"), fill=close)),
            ("bus_shelter", lambda: _shot(found["BusShelter_City"], os.path.join(STILL_DIR, "bus_shelter.png"), fill=0.82)),
            ("sign_oneway", lambda: _shot(found["Sign_OneWay"], os.path.join(STILL_DIR, "sign_oneway.png"), fill=close)),
            ("sign_yield", lambda: _shot(found["Sign_Yield"], os.path.join(STILL_DIR, "sign_yield.png"), fill=close)),
            ("sign_blades", lambda: _shot(found["Sign_Blades"], os.path.join(STILL_DIR, "sign_blades.png"), fill=0.84)),
            ("planter_street", lambda: _shot(found["Planter_Street"], os.path.join(STILL_DIR, "planter_street.png"), fill=close)),
            ("manhole_ring", lambda: _shot(found["Manhole_Ring"], os.path.join(STILL_DIR, "manhole_ring.png"), fill=close)),
            ("storm_curb", lambda: _shot(found["StormDrain_Curb"], os.path.join(STILL_DIR, "storm_curb.png"), fill=close)),
            ("power_pole", lambda: _shot(found["PowerPole_Span"], os.path.join(STILL_DIR, "power_pole.png"), fill=0.84)),
            ("power_crossarm", lambda: _close(
                found["PowerPole_Span"], os.path.join(STILL_DIR, "power_crossarm.png"),
                (1.4, 9.55, 2.5), (-4.0, 8.95, 0.1), 42)),
            ("kit_lineup", lambda: _pass9_lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
        ]
    elif PASS >= 8:
        shots = [
            ("roof_vent", lambda: _shot(found["RoofVent_Turbine"], os.path.join(STILL_DIR, "roof_vent.png"), fill=0.72)),
            ("roof_vent_head", lambda: _close(
                found["RoofVent_Turbine"], os.path.join(STILL_DIR, "roof_vent_head.png"),
                (0.7, 0.7, 0.75), (0.0, 0.52, 0.0), 55)),
            ("kit_lineup", lambda: _pass8_lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
        ]
    elif PASS >= 7:
        shots = [
            ("satellite", lambda: _shot(found["SatelliteDish"], os.path.join(STILL_DIR, "satellite.png"), fill=0.78)),
            ("satellite_dish", lambda: _close(
                found["SatelliteDish"], os.path.join(STILL_DIR, "satellite_dish.png"),
                (1.15, 1.85, 1.35), (0.0, 1.48, 0.2), 48)),
            ("planter_street", lambda: _shot(found["Planter_Street"], os.path.join(STILL_DIR, "planter_street.png"))),
            ("sign_blades", lambda: _shot(found["Sign_Blades"], os.path.join(STILL_DIR, "sign_blades.png"), fill=0.78)),
            ("sign_blades_joint", lambda: _close(
                found["Sign_Blades"], os.path.join(STILL_DIR, "sign_blades_joint.png"),
                (0.85, 3.15, 0.95), (0.0, 2.9, 0.0), 55)),
            ("kit_lineup", lambda: _pass7_lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
        ]
    elif PASS >= 6:
        shots = [
            ("car_sedan", lambda: _shot(found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan.png"), kind="asphalt", fill=0.8)),
            ("car_sedan_side", lambda: _close(
                found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan_side.png"),
                (3.4, 1.15, 0.6), (0.7, 0.75, 0.1), 48)),
            ("car_sedan_nose", lambda: _close(
                found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan_nose.png"),
                (1.6, 0.95, 4.3), (0.0, 0.55, 2.0), 46)),
            ("car_hatch", lambda: _shot(found["Car_Hatch"], os.path.join(STILL_DIR, "car_hatch.png"), kind="asphalt", fill=0.8)),
            ("car_hatch_nose", lambda: _close(
                found["Car_Hatch"], os.path.join(STILL_DIR, "car_hatch_nose.png"),
                (1.5, 0.9, 3.8), (0.0, 0.55, 1.7), 46)),
            ("car_pickup", lambda: _shot(found["Car_Pickup"], os.path.join(STILL_DIR, "car_pickup.png"), kind="asphalt", fill=0.82)),
            ("car_pickup_side", lambda: _close(
                found["Car_Pickup"], os.path.join(STILL_DIR, "car_pickup_side.png"),
                (3.6, 1.35, 0.4), (0.6, 0.9, 0.2), 42)),
            ("kit_lineup", lambda: _pass5_lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
        ]
    elif PASS >= 5:
        shots = [
            ("car_sedan", lambda: _shot(found["Car_Sedan"], os.path.join(STILL_DIR, "car_sedan.png"), kind="asphalt", fill=0.8)),
            ("car_hatch", lambda: _shot(found["Car_Hatch"], os.path.join(STILL_DIR, "car_hatch.png"), kind="asphalt", fill=0.8)),
            ("car_pickup", lambda: _shot(found["Car_Pickup"], os.path.join(STILL_DIR, "car_pickup.png"), kind="asphalt", fill=0.82)),
            ("kit_lineup", lambda: _pass5_lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
        ]
    elif PASS >= 4:
        shots = [
            ("roof_vent", lambda: _shot(found["RoofVent_Turbine"], os.path.join(STILL_DIR, "roof_vent.png"))),
            ("tank_saddle", lambda: _shot(found["WaterTank_Saddle"], os.path.join(STILL_DIR, "tank_saddle.png"))),
            ("ladder_fixed", lambda: _shot(found["Ladder_Fixed"], os.path.join(STILL_DIR, "ladder_fixed.png"), fill=0.8)),
            ("satellite", lambda: _shot(found["SatelliteDish"], os.path.join(STILL_DIR, "satellite.png"), fill=0.78)),
            ("hydrant_red", lambda: _shot(found["FireHydrant_Red"], os.path.join(STILL_DIR, "hydrant_red.png"))),
            ("ac_split", lambda: _shot(found["AC_MiniSplit"], os.path.join(STILL_DIR, "ac_split.png"))),
            ("kit_lineup", lambda: _pass4_lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
        ]
    elif PASS >= 3:
        shots = [
            ("power_pole", lambda: _shot(found["PowerPole_Span"], os.path.join(STILL_DIR, "power_pole.png"), fill=0.82)),
            ("sign_yield", lambda: _shot(found["Sign_Yield"], os.path.join(STILL_DIR, "sign_yield.png"), fill=0.78)),
            ("sign_oneway", lambda: _shot(found["Sign_OneWay"], os.path.join(STILL_DIR, "sign_oneway.png"), fill=0.78)),
            ("sign_blades", lambda: _shot(found["Sign_Blades"], os.path.join(STILL_DIR, "sign_blades.png"), fill=0.78)),
            ("planter_street", lambda: _shot(found["Planter_Street"], os.path.join(STILL_DIR, "planter_street.png"))),
            ("manhole_ring", lambda: _shot(found["Manhole_Ring"], os.path.join(STILL_DIR, "manhole_ring.png"), fill=0.7)),
            ("storm_curb", lambda: _shot(found["StormDrain_Curb"], os.path.join(STILL_DIR, "storm_curb.png"))),
            ("kit_lineup", lambda: _pass3_lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
        ]
    elif PASS >= 2:
        shots = [
            ("bus_shelter", lambda: _shot(found["BusShelter_City"], os.path.join(STILL_DIR, "bus_shelter.png"), fill=0.78)),
            ("newspaper_rack", lambda: _shot(found["NewspaperRack"], os.path.join(STILL_DIR, "newspaper_rack.png"))),
            ("meter_twin", lambda: _shot(found["ParkingMeter_Twin"], os.path.join(STILL_DIR, "meter_twin.png"))),
            ("bike_wave", lambda: _shot(found["BikeRack_Hoop3"], os.path.join(STILL_DIR, "bike_wave.png"))),
            ("bollard_lit", lambda: _shot(found["Bollard_Lit"], os.path.join(STILL_DIR, "bollard_lit.png"))),
            ("bollard_chain", lambda: _shot(found["Bollard_Chain"], os.path.join(STILL_DIR, "bollard_chain.png"))),
            ("cone_tall", lambda: _shot(found["TrafficCone_Tall"], os.path.join(STILL_DIR, "cone_tall.png"))),
            ("barrier_water", lambda: _shot(found["Barrier_Water"], os.path.join(STILL_DIR, "barrier_water.png"))),
            ("barrier_sawhorse", lambda: _shot(found["Barrier_Sawhorse"], os.path.join(STILL_DIR, "barrier_sawhorse.png"))),
            ("kit_lineup", lambda: _pass2_lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
        ]
    else:
        shots = [
        ("light_globe", lambda: _shot(found["LightPost_Globe"], os.path.join(STILL_DIR, "light_globe.png"))),
        ("light_globe_head", lambda: _close(
            found["LightPost_Globe"], os.path.join(STILL_DIR, "light_globe_head.png"),
            (0.85, 3.15, 0.95), (0.0, 3.32, 0.0), 55)),
        ("light_mast", lambda: _shot(found["LightPost_Mast"], os.path.join(STILL_DIR, "light_mast.png"), fill=0.8)),
        ("light_mast_head", lambda: _close(
            found["LightPost_Mast"], os.path.join(STILL_DIR, "light_mast_head.png"),
            (1.15, 9.35, 6.15), (0.0, 9.55, 4.7), 48)),
        ("hydrant_yellow", lambda: _shot(found["FireHydrant_Yellow"], os.path.join(STILL_DIR, "hydrant_yellow.png"))),
        ("hydrant_silver", lambda: _shot(found["FireHydrant_Silver"], os.path.join(STILL_DIR, "hydrant_silver.png"))),
        ("bench_metal", lambda: _shot(found["Bench_Metal"], os.path.join(STILL_DIR, "bench_metal.png"))),
        ("bench_woodiron", lambda: _shot(found["Bench_WoodIron"], os.path.join(STILL_DIR, "bench_woodiron.png"))),
        ("trash_drum", lambda: _shot(found["TrashCan_Drum"], os.path.join(STILL_DIR, "trash_drum.png"))),
        ("recycling_dual", lambda: _shot(found["RecyclingBin_Dual"], os.path.join(STILL_DIR, "recycling_dual.png"))),
        ("dumpster_rear", lambda: _shot(found["Dumpster_Rear"], os.path.join(STILL_DIR, "dumpster_rear.png"))),
        ("kit_lineup", lambda: _lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
    ]
    for name, fn in shots:
        if ONLY and ONLY not in name:
            continue
        print("SHOT", name)
        fn()
    print("STREET_KIT_STILLS", STILL_DIR)


if __name__ == "__main__":
    main()
