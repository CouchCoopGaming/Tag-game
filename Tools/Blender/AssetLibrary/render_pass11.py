"""Pass 11 stills. Walk-up brick, sunken pond, gas station, harbor.

  blender --background --python Tools/Blender/AssetLibrary/render_pass11.py
"""

import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass6 as p6  # noqa: E402
import render_pass9 as p9  # noqa: E402
import render_pass2 as r  # noqa: E402
from _common import TEX_DIR  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass11")


def _pond_water():
    mat = bpy.data.materials.get("Lib_Water")
    if mat is None or not mat.use_nodes:
        return
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    if bsdf is None:
        return
    bsdf.inputs["Roughness"].default_value = 0.08
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.55
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 2.4
    noise.inputs["Detail"].default_value = 4.0
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.35
    ramp.color_ramp.elements[0].color = (0.02, 0.08, 0.09, 1.0)
    ramp.color_ramp.elements[1].position = 0.72
    ramp.color_ramp.elements[1].color = (0.05, 0.16, 0.14, 1.0)
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    path = os.path.join(TEX_DIR, "Lib_Water_N.png")
    if os.path.isfile(path) and "Normal" in bsdf.inputs:
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(path, check_existing=True)
        try:
            tex.image.colorspace_settings.name = "Non-Color"
        except (TypeError, AttributeError):
            pass
        nmap = nt.nodes.new("ShaderNodeNormalMap")
        nmap.inputs["Strength"].default_value = 0.35
        nt.links.new(tex.outputs["Color"], nmap.inputs["Color"])
        nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])


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
    _pond_water()
    grass = p6._mat("Pass11Grass", (0.30, 0.40, 0.22), 0.95)
    p6._ground((0.16, 0.20, 0.14), y=-0.7)
    p6._sheet(0.0, 0.0, -5.0, 40.0, 24.0, grass)
    p6._sheet(0.0, 0.0, 18.5, 40.0, 8.0, grass)
    p6._sheet(-12.0, 0.0, 10.5, 12.0, 10.0, grass)
    p6._sheet(14.0, 0.0, 10.5, 12.0, 10.0, grass)
    p6._look(scene, (14.0, 6.2, -12.0), (0.5, 1.0, 4.0), lens=20.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_park.png"))


def _gas(found):
    scene = p6._begin(wide=True)
    p6._spawn(found, "GasCanopy", (0, 0, 0), 0)
    p6._ground((0.45, 0.46, 0.44))
    p6._look(scene, (16.0, 7.5, 12.0), (1.0, 1.6, -2.0), lens=22.0)
    r._render(scene, os.path.join(STILL_DIR, "gas_station.png"))


def _harbor(found):
    scene = p6._begin(wide=True)
    specs = [
        ("Quay_Edge", (0.0, 0.0, 0.0), 0.0, 1.0),
        ("Lighthouse", (-6.2, 0.0, 3.2), 20.0, 1.0),
        ("HarborShed", (5.4, 0.0, 3.6), 180.0, 1.0),
        ("Dock_Straight", (0.2, 0.0, -4.8), 0.0, 1.0),
        ("Dock_Straight", (0.2, 0.0, -10.8), 0.0, 1.0),
        ("FishingBoat", (3.6, 0.0, -6.2), 12.0, 1.0),
        ("Rowboat", (-3.4, 0.0, -8.4), -20.0, 1.0),
        ("Boat", (6.5, 0.0, -13.0), 200.0, 1.0),
        ("Buoy", (8.4, 0.0, -7.5), 0.0, 1.0),
        ("Piling", (2.6, 0.0, -12.4), 0.0, 1.0),
        ("Piling", (-2.4, 0.0, -12.6), 0.0, 1.0),
        ("Crate", (0.45, 0.66, -5.0), 8.0, 1.0),
        ("Crate", (0.7, 0.66, -5.7), -12.0, 1.0),
        ("Pallet", (-0.55, 0.66, -7.2), 15.0, 1.0),
        ("RopeCoil", (0.55, 0.66, -9.4), 0.0, 1.0),
        ("RopeCoil", (-0.4, 0.66, -4.4), 30.0, 1.0),
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


def _harbor_dock(found):
    scene = p6._begin(wide=True)
    specs = [
        ("Dock_Straight", (0.0, 0.0, 0.0), 0.0, 1.0),
        ("HarborRail", (1.5, 0.62, -1.2), 90.0, 1.0),
        ("HarborRail", (1.5, 0.62, 0.8), 90.0, 1.0),
        ("HarborRail", (-1.45, 0.62, -0.4), 90.0, 1.0),
        ("Piling", (2.2, 0.0, 1.6), 0.0, 1.0),
        ("Crate", (0.4, 0.66, -0.6), 10.0, 1.0),
        ("RopeCoil", (-0.7, 0.66, 0.8), 0.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.10, 0.16, 0.16), y=-0.4)
    p6._sheet(0.0, 0.30, 0.0, 16.0, 14.0, p9._water_mat())
    p6._look(scene, (5.5, 2.6, 4.2), (0.2, 0.6, 0.0), lens=28.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_dock.png"))


def _harbor_boats(found):
    scene = p6._begin(wide=True)
    specs = [
        ("FishingBoat", (1.4, 0.0, 0.2), 24.0, 1.0),
        ("Rowboat", (-1.8, 0.0, 1.3), -16.0, 1.0),
        ("Buoy", (3.2, 0.0, 2.4), 0.0, 1.0),
        ("Piling", (-2.6, 0.0, -1.4), 0.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.10, 0.16, 0.16), y=-0.5)
    p6._sheet(0.0, 0.30, 0.6, 14.0, 12.0, p9._water_mat())
    p6._look(scene, (6.2, 2.8, 5.4), (0.2, 0.5, 0.4), lens=32.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_boats.png"))


def _harbor_shed(found):
    scene = p6._begin(wide=True)
    specs = [
        ("HarborShed", (0.0, 0.0, 0.0), 200.0, 1.0),
        ("Crate", (2.3, 0.0, 1.1), 12.0, 1.0),
        ("Crate", (2.5, 0.0, 0.3), -8.0, 1.0),
        ("Pallet", (2.2, 0.0, -0.8), 20.0, 1.0),
        ("RopeCoil", (-1.8, 0.0, 1.4), 0.0, 1.0),
        ("Bollard", (-2.2, 0.0, 1.6), 0.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.34, 0.36, 0.32))
    p6._look(scene, (6.0, 2.8, 5.5), (0.4, 1.2, 0.2), lens=32.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_shed.png"))


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].split(",")
    found = r._catalog()
    shots = [
        ("park", lambda: _park(found)),
        ("harbor", lambda: _harbor(found)),
        ("walkup", lambda: p9._turntable(found, "WalkUp", os.path.join(STILL_DIR, "walkup.png"), 28, 12, 0.86)),
        ("gas", lambda: _gas(found)),
        ("dock", lambda: _harbor_dock(found)),
        ("boats", lambda: _harbor_boats(found)),
        ("shed", lambda: _harbor_shed(found)),
    ]
    for name, fn in shots:
        if only and not any(part in name for part in only):
            continue
        fn()
    print("PASS11_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
