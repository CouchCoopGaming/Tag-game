"""Export the street-kit assets and merge them into manifest.json.

  blender --background --python Tools/Blender/AssetLibrary/_sk_build.py

Does not rebuild the rest of the library. Prefabs are written for the new names only.
"""

import importlib
import json
import os
import sys
import traceback

import bpy  # noqa: F401

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import (  # noqa: E402
    ROOT,
    check_pivot,
    export_fbx,
    fbx_model_names,
    manifest_entry,
    validate_colliders,
)
import write_unity  # noqa: E402

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
    "sk_trash_cart",
    "sk_recycling_cart",
    "sk_cabinet_electrical",
    "sk_barrier_jersey",
    "sk_barrel_traffic",
    "sk_bollard_fixed",
    "sk_bollard_removable",
)


def _merge(entries):
    path = os.path.join(ROOT, "manifest.json")
    data = json.load(open(path, encoding="utf-8"))
    fresh = {entry["name"]: entry for entry in entries}
    seen = set()
    merged = []
    for entry in data:
        if entry["name"] in fresh:
            merged.append(fresh[entry["name"]])
            seen.add(entry["name"])
        else:
            merged.append(entry)
    for entry in entries:
        if entry["name"] not in seen:
            merged.append(entry)
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(merged, handle, indent=2)
        handle.write("\n")
    return path


def _prefabs(entries):
    palette, _textured, _normals, _ao, _emissive = write_unity.load_palette()
    mat_guids = {name: write_unity.guid("mat", name) for name in palette}
    script_guid = write_unity.guid("script", "LibraryPropMeta")
    for entry in entries:
        write_unity.write_fbx_meta(entry)
        write_unity.write_prefab(entry, write_unity.guid("fbx", entry["category"], entry["name"]), mat_guids, script_guid)


def main():
    only = None
    if "--only" in sys.argv:
        only = [part for part in sys.argv[sys.argv.index("--only") + 1].split(",") if part]
    entries = []
    failures = []
    worst = (0.0, "")
    for stem in MODULES:
        if only and not any(part in stem for part in only):
            continue
        try:
            module = importlib.import_module(stem)
            asset = module.create()
            check_pivot(asset)
            validate_colliders(asset)
            path = export_fbx(asset)
            names = fbx_model_names(path)
            entry = manifest_entry(asset, names)
            entry["fbx"] = os.path.relpath(path, os.path.join(ROOT, "..", "..", "..")).replace("\\", "/")
            entries.append(entry)
            slack = entry["slackCm"]
            if slack > worst[0]:
                worst = (slack, asset.name)
            flag = "WARN" if asset.warnings else "OK"
            tris = ",".join(str(item["tris"]) for item in entry["lods"])
            print("%s %s size=%s tris=%s slack=%s(%s) %s" % (
                flag, asset.name, entry["size"], tris, slack,
                getattr(asset, "collider_slack_name", ""),
                "; ".join(asset.warnings),
            ))
        except Exception as exc:
            traceback.print_exc()
            failures.append("%s: %s" % (stem, exc))
    if failures:
        for line in failures:
            print("FAIL", line)
        sys.exit(1)
    _merge(entries)
    _prefabs(entries)
    print("STREET_KIT_WORST_SLACK_CM", worst[0], worst[1])
    print("STREET_KIT_COUNT", len(entries))


if __name__ == "__main__":
    main()
