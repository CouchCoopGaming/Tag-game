"""Decimate each costume piece into LOD1 and LOD2 and record worn totals.

Opens the lab blend, writes coarser meshes beside the LOD0 pieces, and
updates loadouts.json plus the lod block in the pass-1 fit file. Does not
touch the Hier armature, its bind, or any game scene.
"""
import json
import os
import sys

import bpy

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
LOADOUTS = os.path.join(ROOT, "Art", "CharacterLab", "loadouts.json")
FIT = os.path.join(ROOT, "Docs", "Characters", "pass1", "fit.txt")
LOD1_RATIO = 0.85
LOD2_RATIO = 0.55
# Costume ceilings: LOD0 15000, LOD1 8000, LOD2 4000.
LOD1_CAP = 8000
LOD2_CAP = 4000


def log(msg):
    print(msg, flush=True)


def id_for(who, index, label):
    parts = [who, str(index)] + [part.capitalize() for part in label.split("-")]
    return "_".join(parts)


def tri_count(mesh):
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)


def realize(obj):
    """Apply the decimate modifier without depending on the operator context."""
    depsgraph = bpy.context.evaluated_depsgraph_get()
    baked = bpy.data.meshes.new_from_object(obj.evaluated_get(depsgraph))
    old = obj.data
    obj.modifiers.clear()
    obj.data = baked
    if old.users == 0:
        bpy.data.meshes.remove(old)
    return obj


def make_lod(src, ratio, suffix):
    name = src.name.split(".")[0] + suffix
    old = bpy.data.objects.get(name)
    if old is not None:
        mesh = old.data
        bpy.data.objects.remove(old, do_unlink=True)
        if mesh is not None and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    dup = src.copy()
    dup.data = src.data.copy()
    dup.name = name
    for coll in src.users_collection:
        coll.objects.link(dup)
    dup.parent = src.parent
    dup.parent_type = src.parent_type
    dup.parent_bone = src.parent_bone
    dup.matrix_world = src.matrix_world.copy()
    # Exact copies left by a vent weld into one vertex so collapse can step down.
    weld = dup.modifiers.new("Weld", "WELD")
    weld.merge_threshold = 0.00001
    mod = dup.modifiers.new("Decimate", "DECIMATE")
    mod.decimate_type = "COLLAPSE"
    mod.ratio = ratio
    realize(dup)
    dup["piece"] = name
    dup["costume_lod"] = 1 if suffix == "_LOD1" else 2
    dup.hide_render = True
    return dup


def rename_sets(data):
    for spec in data["sets"]:
        spec["id"] = id_for(spec["character"], spec["variant"], spec["label"])
    worn = data.get("tris", {}).get("worn", {})
    renamed = {}
    for spec in data["sets"]:
        # The previous key was Who-index-label. Match by the spoken label.
        old = "%s-%d-%s" % (spec["character"], spec["variant"], spec["label"])
        if old in worn:
            renamed[spec["id"]] = worn[old]
        elif spec["id"] in worn:
            renamed[spec["id"]] = worn[spec["id"]]
    if renamed:
        data["tris"]["worn"] = renamed
    return data


def main():
    with open(LOADOUTS, encoding="utf-8") as handle:
        data = json.load(handle)
    data = rename_sets(data)
    pieces = {}
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Lab_"):
            continue
        if "_LOD" in obj.name:
            continue
        pieces[obj.name.split(".")[0]] = obj
    lod_tris = {1: {}, 2: {}}
    for name, obj in sorted(pieces.items()):
        base = tri_count(obj.data)
        ratio1 = LOD1_RATIO
        ratio2 = LOD2_RATIO
        lod1 = make_lod(obj, ratio1, "_LOD1")
        lod2 = make_lod(obj, ratio2, "_LOD2")
        n1 = tri_count(lod1.data)
        n2 = tri_count(lod2.data)
        # A coarse shell sometimes ignores a gentle collapse. Tighten until the
        # piece itself steps down, so the worn totals stay ordered.
        for _try in range(4):
            if n1 < base and n2 < n1:
                break
            ratio1 *= 0.7
            ratio2 *= 0.7
            bpy.data.objects.remove(lod1, do_unlink=True)
            bpy.data.objects.remove(lod2, do_unlink=True)
            lod1 = make_lod(obj, ratio1, "_LOD1")
            lod2 = make_lod(obj, ratio2, "_LOD2")
            n1 = tri_count(lod1.data)
            n2 = tri_count(lod2.data)
        lod_tris[1][name] = n1
        lod_tris[2][name] = n2
        log("PIECE %s lod0=%d lod1=%d lod2=%d" % (name, base, n1, n2))

    lines = []
    for spec in data["sets"]:
        names = spec["pieces"]
        lod0 = data["tris"]["worn"][spec["id"]]
        lod1 = sum(lod_tris[1][name] for name in names)
        lod2 = sum(lod_tris[2][name] for name in names)
        if not (lod2 < lod1 < lod0):
            raise SystemExit("lod not coarser: %s %d/%d/%d" % (spec["id"], lod0, lod1, lod2))
        if lod0 > 15000 or lod1 > LOD1_CAP or lod2 > LOD2_CAP:
            raise SystemExit("lod over budget: %s %d/%d/%d" % (spec["id"], lod0, lod1, lod2))
        spec["lods"] = [
            {"lod": 0, "mesh": "LOD0", "tris": lod0},
            {"lod": 1, "mesh": "LOD1", "tris": lod1},
            {"lod": 2, "mesh": "LOD2", "tris": lod2},
        ]
        lines.append("lod %s %d/%d/%d" % (spec["id"], lod0, lod1, lod2))
        log("WORN %s" % lines[-1])

    data["tris"]["lod1"] = lod_tris[1]
    data["tris"]["lod2"] = lod_tris[2]
    with open(LOADOUTS, "w", encoding="utf-8") as handle:
        json.dump(data, handle, indent=2)
        handle.write("\n")

    with open(FIT, encoding="utf-8") as handle:
        text = handle.read()
    kept = []
    for line in text.splitlines():
        if line.startswith("lod ") or line.startswith("# lod"):
            continue
        if line.startswith("worn "):
            # worn Reed-1-hood tris=10858 -> worn Reed_1_Hood tris=10858
            parts = line.split()
            old_id = parts[1]
            tris = parts[2]
            if "-" in old_id:
                who, index, label = old_id.split("-", 2)
                new_id = id_for(who, int(index), label)
            else:
                new_id = old_id
            kept.append("worn %s %s" % (new_id, tris))
            continue
        kept.append(line)
    kept.append("# lod LOD0/LOD1/LOD2 worn triangles")
    kept.extend(lines)
    with open(FIT, "w", encoding="utf-8") as handle:
        handle.write("\n".join(kept) + "\n")
    bpy.ops.wm.save_mainfile()
    log("SAVED lods")


if __name__ == "__main__":
    main()
