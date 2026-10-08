"""Export every registered asset to FBX and write manifest.json.

Run:
  blender --background --python Tools/Blender/AssetLibrary/build_all.py
"""

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
    generate_textures,
    load_asset_modules,
    manifest_entry,
    validate_colliders,
)
import _common  # noqa: E402


def main():
    print("textures")
    generate_textures()
    load_asset_modules()
    print("assets", len(_common.REGISTRY))
    entries = []
    failures = []
    for fn in list(_common.REGISTRY):
        name = fn.__module__
        try:
            asset = fn()
            if not hasattr(asset, "name"):
                raise TypeError("%s did not return an Asset" % name)
            check_pivot(asset)
            validate_colliders(asset)
            path = export_fbx(asset)
            names = fbx_model_names(path)
            entry = manifest_entry(asset, names)
            entry["fbx"] = os.path.relpath(path, os.path.join(ROOT, "..", "..", "..")).replace("\\", "/")
            entries.append(entry)
            flag = "WARN" if asset.warnings else "OK"
            tris = ",".join(str(item["tris"]) for item in entry["lods"])
            print("%s %s size=%s tris=%s slack=%s %s" % (
                flag, asset.name, entry["size"], tris, entry["slackCm"], "; ".join(asset.warnings)
            ))
        except Exception as exc:
            traceback.print_exc()
            failures.append("%s: %s" % (name, exc))
    out = os.path.join(ROOT, "manifest.json")
    with open(out, "w", encoding="utf-8") as handle:
        json.dump(entries, handle, indent=2)
    print("WROTE", out, "count", len(entries), "failures", len(failures))
    if failures:
        for line in failures:
            print("FAIL", line)
        sys.exit(1)


if __name__ == "__main__":
    main()
