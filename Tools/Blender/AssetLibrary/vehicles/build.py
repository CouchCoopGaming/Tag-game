"""Export vehicle shells into Assets/Art/Props/Library/Vehicles.

  blender --background --python Tools/Blender/AssetLibrary/vehicles/build.py -- --only sedan_midsize
"""

import importlib
import os
import sys
import traceback

import bpy  # noqa: F401

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, ROOT)
sys.path.insert(0, HERE)

from _common import (  # noqa: E402
    check_pivot,
    export_fbx,
    fbx_model_names,
    manifest_entry,
    validate_colliders,
)
import shell  # noqa: E402
import write_unity  # noqa: E402

# Imported from the street-kit builder so the manifest merge stays by name.
sys.path.insert(0, ROOT)
from _sk_build import _merge, _prefabs  # noqa: E402

MODULES = (
    "sedan_midsize",
    "sedan_compact",
    "crossover_compact",
)


def main():
    only = None
    if "--only" in sys.argv:
        only = [part for part in sys.argv[sys.argv.index("--only") + 1].split(",") if part]
    write_unity.folder_meta(
        os.path.join(write_unity.LIB, "Vehicles"),
        "Library/Vehicles",
    )
    write_unity.folder_meta(
        os.path.join(write_unity.LIB, "Vehicles", "Prefabs"),
        "Library/Vehicles/Prefabs",
    )
    entries = []
    failures = []
    worst = (0.0, "")
    for stem in MODULES:
        if only and not any(part in stem for part in only):
            continue
        try:
            module = importlib.import_module(stem)
            asset = module.create()
            if hasattr(asset, "_sedan_spec"):
                shell.probe_sedan(asset, asset._sedan_spec)
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
            tris = ",".join(str(item["tris"]) for item in entry["lods"])
            flag = "WARN" if asset.warnings else "OK"
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
    print("VEHICLE_WORST_SLACK_CM", worst[0], worst[1])
    print("VEHICLE_COUNT", len(entries))


if __name__ == "__main__":
    main()
