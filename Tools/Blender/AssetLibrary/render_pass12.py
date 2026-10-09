"""Pass 12 stills. Continuous park ground, harbor hulls, closer gas station.

  blender --background --python Tools/Blender/AssetLibrary/render_pass12.py
"""

import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass6 as p6  # noqa: E402
import render_pass9 as p9  # noqa: E402
import render_pass2 as r  # noqa: E402
from _common import unity_to_blender  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass12")


def _grass_with_hole(cx, cz, rx, rz):
    """One grass sheet. The ellipse is the pond, tucked under the bank crest."""
    grass = p6._mat("Pass12Grass", (0.30, 0.40, 0.22), 0.95)
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0.0, 0.0, 0.0))
    plane = bpy.context.active_object
    plane.scale = (64.0, 56.0, 1.0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.mesh.primitive_cylinder_add(
        radius=1.0,
        depth=6.0,
        location=unity_to_blender(cx, 0.0, cz),
    )
    cutter = bpy.context.active_object
    cutter.scale = (rx, rz, 1.0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    mod = plane.modifiers.new("PondHole", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.object = cutter
    mod.solver = "EXACT"
    bpy.context.view_layer.objects.active = plane
    bpy.ops.object.modifier_apply(modifier="PondHole")
    bpy.data.objects.remove(cutter, do_unlink=True)
    plane.data.materials.append(grass)


def _park(found):
    scene = p6._begin(wide=True)
    specs = []
    for z in (-10.0, -6.0, -2.0, 2.0):
        specs.append(("Sidewalk", (0.0, 0.0, z), 0.0, 1.0))
    for pos in ((1.7, 0.0, -8.0), (-1.7, 0.0, -4.0), (1.7, 0.0, 0.0), (-1.7, 0.0, 4.0)):
        specs.append(("ParkLamp", pos, 0.0, 1.0))
    for z in (-8.5, -3.5, 1.5):
        specs.append(("TrashCan_Slat", (1.55, 0.0, z), 0.0, 1.0))
    specs.append(("Pond", (1.6, 0.0, 10.6), 0.0, 1.0))
    specs.append(("Playground", (-9.2, 0.0, -1.5), 18.0, 1.0))
    specs.append(("PicnicTable", (7.6, 0.0, -2.2), 80.0, 1.0))
    specs.append(("PicnicTable", (8.0, 0.0, 1.6), -15.0, 1.0))
    specs.append(("Fountain", (-3.4, 0.0, 4.6), 20.0, 1.0))
    specs.append(("Bench_Wood", (5.4, 0.0, 6.6), -70.0, 1.0))
    specs.append(("Tree_Maple", (-5.5, 0.0, -6.5), 15.0, 1.0))
    specs.append(("Tree_Maple", (6.2, 0.0, 5.2), -8.0, 1.0))
    specs.append(("Tree_Pine", (-6.8, 0.0, 6.8), 10.0, 1.0))
    for pos, yaw in (
        ((-4.2, 0.0, -3.2), 10),
        ((-3.4, 0.0, -2.4), 40),
        ((-4.6, 0.0, -2.2), -20),
        ((4.6, 0.0, 3.4), 15),
        ((5.4, 0.0, 2.8), 50),
        ((9.2, 0.0, -0.4), 5),
        ((-8.0, 0.0, 3.5), 25),
    ):
        specs.append(("Shrub", pos, float(yaw), 1.0))
    p6._place(found, specs)
    import render_pass11 as p11
    p11._pond_water()
    p6._ground((0.16, 0.20, 0.14), y=-0.7)
    # Crest is 5.35 x 3.85. The hole stops inside it so the bank covers the edge.
    _grass_with_hole(1.6, 10.6, 5.00, 3.50)
    p6._look(scene, (14.0, 6.2, -12.0), (0.5, 1.0, 4.0), lens=20.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_park.png"))


def _gas(found):
    scene = p6._begin(wide=True)
    p6._spawn(found, "GasCanopy", (0, 0, 0), 0)
    p6._ground((0.45, 0.46, 0.44))
    p6._look(scene, (7.4, 2.6, 6.4), (0.3, 1.45, -2.2), lens=28.0)
    r._render(scene, os.path.join(STILL_DIR, "gas_station.png"))


def _harbor(found):
    scene = p6._begin(wide=True)
    specs = [
        ("Quay_Edge", (0.0, 0.0, 0.0), 0.0, 1.0),
        ("Lighthouse", (-6.2, 0.0, 3.2), 20.0, 1.0),
        ("HarborShed", (5.4, 0.0, 3.6), 180.0, 1.0),
        ("Dock_Straight", (0.2, 0.0, -4.8), 0.0, 1.0),
        ("Dock_Straight", (0.2, 0.0, -10.8), 0.0, 1.0),
        ("Gangway", (0.2, 0.0, -1.9), 180.0, 1.0),
        ("FishingBoat", (3.6, 0.0, -6.2), 12.0, 1.0),
        ("Rowboat", (-3.4, 0.08, -8.4), -20.0, 1.0),
        ("Boat", (6.5, 0.0, -13.0), 200.0, 1.0),
        ("Buoy", (8.4, 0.0, -7.5), 0.0, 1.0),
        ("Piling", (2.6, 0.0, -12.4), 0.0, 1.0),
        ("Piling", (-2.4, 0.0, -12.6), 0.0, 1.0),
        ("Crate", (0.45, 0.66, -5.0), 8.0, 1.0),
        ("Crate", (0.7, 0.66, -5.7), -12.0, 1.0),
        ("Pallet", (-0.55, 0.66, -7.2), 15.0, 1.0),
        ("RopeCoil", (0.55, 0.66, -9.4), 0.0, 1.0),
        ("RopeCoil", (-0.4, 0.66, -4.4), 30.0, 1.0),
        ("FuelDock", (0.75, 0.62, -6.5), 90.0, 1.0),
        ("FishCrate", (1.05, 0.66, -8.2), 8.0, 1.0),
        ("FishCrate", (-0.85, 0.66, -5.6), -6.0, 1.0),
        ("LobsterTrap", (-0.7, 0.66, -8.8), 20.0, 1.0),
        ("LifeRing", (-3.6, 0.90, 1.6), 180.0, 1.0),
        ("LifeRing", (3.4, 0.90, 2.2), 200.0, 1.0),
        ("LifeRing", (-1.05, 0.62, -9.6), 90.0, 1.0),
    ]
    for z in (-5.2, -7.2, -9.2, -11.2):
        specs.append(("HarborRail", (1.55, 0.62, z), 90.0, 1.0))
        specs.append(("HarborRail", (-1.25, 0.62, z), 90.0, 1.0))
    p6._place(found, specs)
    p6._ground((0.22, 0.26, 0.20), y=-0.05)
    water = p9._water_mat()
    p6._sheet(0.0, 0.34, -8.0, 36.0, 28.0, water)
    p6._look(scene, (-14.0, 6.0, -16.0), (0.5, 1.4, -2.0), lens=22.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_harbor.png"))


def _harbor_boats(found):
    scene = p6._begin(wide=True)
    specs = [
        ("FishingBoat", (0.5, 0.0, -0.2), 32.0, 1.0),
        ("Rowboat", (-1.8, 0.08, 1.0), -22.0, 1.0),
        ("Buoy", (2.4, 0.0, 1.6), 15.0, 1.0),
        ("Piling", (-2.8, 0.0, -1.2), 0.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.10, 0.16, 0.16), y=-0.5)
    p6._sheet(0.0, 0.34, 0.4, 16.0, 14.0, p9._water_mat())
    p6._look(scene, (5.4, 1.7, 4.6), (0.1, 0.45, 0.3), lens=36.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_boats.png"))


def _harbor_shed(found):
    scene = p6._begin(wide=True)
    specs = [
        ("HarborShed", (0.0, 0.0, 0.0), 200.0, 1.0),
        ("Crate", (2.3, 0.0, 1.1), 12.0, 1.0),
        ("FishCrate", (2.4, 0.0, 0.2), -8.0, 1.0),
        ("LobsterTrap", (2.1, 0.0, -0.9), 18.0, 1.0),
        ("LifeRing", (-2.0, 0.0, 1.3), 40.0, 1.0),
        ("RopeCoil", (-1.6, 0.0, 0.2), 0.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.34, 0.36, 0.32))
    p6._look(scene, (5.2, 2.4, 4.6), (0.3, 1.3, 0.1), lens=32.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_shed.png"))


def _gangway(found):
    scene = p6._begin(wide=True)
    specs = [
        ("Gangway", (0.0, 0.0, 0.0), 0.0, 1.0),
        ("Quay_Edge", (0.0, 0.0, -0.6), 0.0, 1.0),
        ("Dock_Straight", (0.0, 0.0, 4.2), 0.0, 1.0),
        ("LifeRing", (-1.4, 0.90, -1.0), 160.0, 1.0),
        ("FishCrate", (0.7, 0.62, 2.4), 12.0, 1.0),
        ("FuelDock", (-0.8, 0.62, 3.2), 0.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.18, 0.22, 0.20), y=-0.2)
    p6._sheet(0.0, 0.34, 2.0, 20.0, 16.0, p9._water_mat())
    p6._look(scene, (2.6, 1.15, 2.6), (0.0, 0.82, -0.2), lens=34.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_gangway.png"))


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].split(",")
    found = r._catalog()
    shots = [
        ("park", lambda: _park(found)),
        ("harbor", lambda: _harbor(found)),
        ("boats", lambda: _harbor_boats(found)),
        ("shed", lambda: _harbor_shed(found)),
        ("gas", lambda: _gas(found)),
        ("gangway", lambda: _gangway(found)),
    ]
    for name, fn in shots:
        if only and not any(part in name for part in only):
            continue
        fn()
    print("PASS12_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
