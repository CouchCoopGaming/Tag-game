"""Pass 25 strips, climb side view, and hip-sit before/after side stills."""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, "/workspace/Tools")
import RenderPass7 as p7
import RenderPass9 as p9
import NoClipCheck as nc

PANEL = "/tmp/p25-panels"
HIP = "/tmp/p25-hip"
FRAMES = "/tmp/noclip-frames-p25.txt"
os.makedirs(PANEL, exist_ok=True)
os.makedirs(HIP, exist_ok=True)


def load_frames(path):
    clips = {}
    name = None
    for line in open(path):
        parts = line.split()
        if not parts:
            continue
        if parts[0] == "CLIP":
            name = parts[1]
            clips[name] = {"solid": parts[4], "frames": []}
        elif parts[0] == "F" and name:
            clips[name]["frames"].append((float(parts[1]), [float(x) for x in parts[2:]]))
    return clips


def nearest(frames, t):
    return min(frames, key=lambda row: abs(row[0] - t))


def cm(v):
    return round(float(v) * 100.0, 1)


def zeros():
    return [0.0] * 32


def put(n, **kw):
    idx = {
        "hip": 0, "spineYaw": 4, "spine": 3, "head": 6,
        "tL": 9, "tR": 11, "kL": 13, "kR": 14,
        "aL": 15, "aR": 18, "yL": 16, "yR": 19, "eL": 21, "eR": 22,
        "rollL": 30, "rollR": 31,
    }
    for key, val in kw.items():
        n[idx[key]] = val
    return n


def studio():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 256
    scene.render.resolution_y = 340
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.image_settings.compression = 15
    scene.eevee.taa_render_samples = 8
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.exposure = 0.0
    world = bpy.data.worlds.new("Pass25Studio")
    scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = (0.10, 0.11, 0.13, 1.0)
    bg.inputs["Strength"].default_value = 1.0
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])
    key = bpy.data.objects.new("Key", bpy.data.lights.new("Key", "SUN"))
    bpy.context.collection.objects.link(key)
    key.data.energy = 2.4
    key.data.color = (1.0, 0.96, 0.90)
    key.data.angle = math.radians(6)
    key.rotation_euler = (math.radians(52), math.radians(-8), math.radians(38))
    rim = bpy.data.objects.new("Rim", bpy.data.lights.new("Rim", "SUN"))
    bpy.context.collection.objects.link(rim)
    rim.data.energy = 1.6
    rim.data.color = (0.72, 0.82, 1.0)
    rim.data.angle = math.radians(10)
    rim.rotation_euler = (math.radians(118), math.radians(18), math.radians(-150))
    fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", "AREA"))
    bpy.context.collection.objects.link(fill)
    fill.location = (-1.6, -2.4, 1.8)
    fill.data.energy = 35
    fill.data.size = 3.0
    fill.data.color = (0.85, 0.88, 0.95)
    bpy.ops.mesh.primitive_plane_add(size=16, location=(0.0, 0.0, 0.0))
    floor = bpy.context.active_object
    floor.name = "StudioFloor"
    floor.data.materials.append(p7.principled("StudioFloor", (0.42, 0.42, 0.40, 1), 0.82))
    return floor


def clear_props():
    for ob in list(bpy.data.objects):
        if ob.name.startswith("Cord") or ob.name.startswith("Solid") or ob.name.startswith("Overlap") or ob.name.startswith("Ledge"):
            bpy.data.objects.remove(ob, do_unlink=True)


def paint_solid(ob):
    mat = bpy.data.materials.get("ObstacleMat")
    if mat is None:
        mat = p7.principled("ObstacleMat", (0.55, 0.38, 0.22, 1), 0.7)
    if ob.data.materials:
        ob.data.materials[0] = mat
    else:
        ob.data.materials.append(mat)


def cord(origin):
    end = origin + Vector((0.0, 0.4, 2.4)).normalized() * 1.45
    mat = p7.principled("CordMat", (0.95, 0.85, 0.35, 1), 0.35)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Emission Color"].default_value = (0.95, 0.85, 0.35, 1)
    bsdf.inputs["Emission Strength"].default_value = 0.35
    span = end - origin
    made = []
    for i in range(8):
        t0 = i / 8.0
        t1 = (i + 1) / 8.0
        nc.add_cyl("Cord%d" % i, origin + span * t0, origin + span * t1, 0.006)
        ob = bpy.data.objects["Cord%d" % i]
        if ob.data.materials:
            ob.data.materials[0] = mat
        else:
            ob.data.materials.append(mat)
        made.append(ob)
    return made


def pose_of(arm, base, nums, kind):
    clear_props()
    if len(nums) == 26:
        nums = list(nums) + [0.0] * 6
    pieces, _deps = nc.pose_frame(arm, base, nums)
    if kind in ("ground", "vault"):
        pieces = nc.seat_feet(arm, pieces)
    return pieces


def measure(pieces):
    def c(name):
        return nc.piece_cloud(pieces, (name,))

    hip = c("Mesh_Hips")
    fl = c("Mesh_Foot_L")
    fr = c("Mesh_Foot_R")
    hl = c("Mesh_Hand_L")
    hr = c("Mesh_Hand_R")
    chest = c("Mesh_Chest")
    hc = hip.mean(0)
    return {
        "hip": hc,
        "hip_low": float(hip[:, 2].min()),
        "fl": fl,
        "fr": fr,
        "foot_y": (float(fl.mean(0)[1] - hc[1]), float(fr.mean(0)[1] - hc[1])),
        "foot_x": (float(fl.mean(0)[0] - hc[0]), float(fr.mean(0)[0] - hc[0])),
        "sole": (
            float(hip[:, 2].min()) - float(fl[:, 2].min()),
            float(hip[:, 2].min()) - float(fr[:, 2].min()),
        ),
        "foot_z": (float(fl[:, 2].min()), float(fr[:, 2].min())),
        "hand_y": (float(hl.mean(0)[1] - hc[1]), float(hr.mean(0)[1] - hc[1])),
        "hand_x": (float(hl.mean(0)[0] - hc[0]), float(hr.mean(0)[0] - hc[0])),
        "chest_y": float(chest.mean(0)[1] - hc[1]) if len(chest) else 0.0,
    }


def support_point(m):
    lz, rz = m["foot_z"]
    low = min(lz, rz)
    pts = []
    if lz <= low + 0.04:
        pts.append(m["fl"].mean(0))
    if rz <= low + 0.04:
        pts.append(m["fr"].mean(0))
    acc = pts[0]
    for p in pts[1:]:
        acc = acc + p
    return acc / float(len(pts))


def vault_box(samples):
    low_ys = []
    for s in samples:
        for cloud in (s["fl"], s["fr"]):
            for p in cloud:
                if p[2] < 0.08:
                    low_ys.append(float(p[1]))
    if not low_ys:
        low_ys = [float(s["fl"][:, 1].min()) for s in samples]
    face = min(low_ys) - 0.04
    clear_z = []
    for s in samples:
        for cloud in (s["fl"], s["fr"]):
            for p in cloud:
                if p[1] < face:
                    clear_z.append(float(p[2]))
    top = (min(clear_z) - 0.03) if clear_z else 0.12
    depth = 0.55
    height = max(top - (nc.FLOOR["z"] - 0.02), 0.08)
    center = (0.0, face - depth * 0.5, (nc.FLOOR["z"] - 0.02) + height * 0.5)
    size = (0.70, depth, height)
    return center, size, face, top


def add_vault_box(center, size):
    ob = nc.add_cube("Ledge", center, size)
    paint_solid(ob)
    return [ob]


def solids_for(kind, pieces, vault):
    if kind == "rope":
        hand = nc.piece_cloud(pieces, ("Mesh_Hand_L",))
        return cord(nc.centroid(hand))
    if kind == "vault":
        return add_vault_box(vault[0], vault[1])
    if kind == "ground":
        return []
    made = nc.place_solid(kind, pieces)
    for ob in made:
        paint_solid(ob)
    return made


def shoot(arm, pieces, solids, path, yaw, fill, lift):
    p7.paint(arm, p7.principled("MannequinGrey", (0.58, 0.59, 0.61, 1), 0.48))
    bpy.context.view_layer.update()
    p9.frame_yaw([arm], solids, yaw=yaw, lens=48, fill=fill, lift=lift)
    scene = bpy.context.scene
    scene.render.filepath = path
    scene.render.film_transparent = False
    bpy.ops.render.render(write_still=True)


def marker(m, sole):
    foot = support_point(m)
    pelvis = Vector((float(m["hip"][0]), float(m["hip"][1]), float(m["hip"][2])))
    nc.add_cyl("CordAxis", (foot[0], foot[1], sole - 0.02), (foot[0], foot[1], pelvis.z + 0.35), 0.012)
    axis = bpy.data.objects["CordAxis"]
    mat = p7.principled("AxisMat", (0.95, 0.25, 0.15, 1), 0.3)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Emission Color"].default_value = (0.95, 0.2, 0.1, 1)
    bsdf.inputs["Emission Strength"].default_value = 1.2
    if axis.data.materials:
        axis.data.materials[0] = mat
    else:
        axis.data.materials.append(mat)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.045, location=pelvis)
    dot = bpy.context.active_object
    dot.name = "CordDot"
    dotm = p7.principled("DotMat", (0.95, 0.85, 0.2, 1), 0.3)
    dbsdf = dotm.node_tree.nodes.get("Principled BSDF")
    dbsdf.inputs["Emission Color"].default_value = (0.95, 0.85, 0.15, 1)
    dbsdf.inputs["Emission Strength"].default_value = 1.4
    dot.data.materials.append(dotm)
    return [axis, dot]


def main():
    clips = load_frames(FRAMES)
    p7.clear()
    arm = p7.import_runner()
    base = arm.rotation_quaternion.copy()
    floor = studio()
    p7.reset_arm(arm, base)
    p9.face_travel(arm, base)
    bind, _deps = nc.gather(arm)
    sole = float(nc.piece_cloud(bind, ("Mesh_Foot_L", "Mesh_Foot_R"))[:, 2].min())
    nc.FLOOR["z"] = sole
    floor.location.z = sole
    print("SOLE", cm(sole))
    three = -math.pi / 2 + 0.9
    side = -math.pi / 2
    order = [
        ("exit-WallRun", "wall"),
        ("exit-WallJump", "wall"),
        ("exit-ClimbTopOut", "lid"),
        ("exit-ClingDrop", "wall"),
        ("exit-Vault", "vault"),
        ("exit-Mantle", "lid"),
        ("exit-Slide", "ground"),
        ("exit-AirDash", "ground"),
        ("exit-Punch", "ground"),
        ("exit-Lunge", "ground"),
        ("exit-ZipDrop", "zip"),
        ("exit-LaunchLand", "ground"),
        ("exit-GrappleArrive", "rope"),
        ("exit-GrappleRelease", "rope"),
        ("exit-Stagger", "ground"),
        ("exit-TagBackEnd", "ground"),
        ("exit-SoftLand", "ground"),
        ("exit-Roll", "ground"),
        ("exit-RollAbsorb", "ground"),
    ]
    frames = clips["exit-Vault"]["frames"]
    end = frames[-1][0]
    vault_samples = []
    for u in (0, 0.25, 0.5, 0.75, 1):
        _t, nums = nearest(frames, end * u)
        pieces = pose_of(arm, base, nums, "vault")
        vault_samples.append(measure(pieces))
    vault = vault_box(vault_samples)
    print("VAULT box far", cm(vault[2]), "top", cm(vault[3]))

    def one(name, kind, u, yaw, dest, fill, lift, floor_z):
        frames = clips[name]["frames"]
        end = frames[-1][0]
        t, nums = nearest(frames, end * u)
        floor.location.z = floor_z
        pieces = pose_of(arm, base, nums, kind)
        m = measure(pieces)
        solids = solids_for(kind, pieces, vault)
        flags = []
        if max(abs(m["hand_x"][0]), abs(m["hand_x"][1])) > 0.55:
            flags.append("arm out")
        if max(abs(m["foot_x"][0]), abs(m["foot_x"][1])) > 0.30:
            flags.append("wide")
        if min(m["sole"]) < 0.45 and max(m["sole"]) < 0.55:
            flags.append("frog")
        print(
            "CHECK", name, "u", u,
            "footY", cm(m["foot_y"][0]), cm(m["foot_y"][1]),
            "footX", cm(m["foot_x"][0]), cm(m["foot_x"][1]),
            "sole", cm(m["sole"][0]), cm(m["sole"][1]),
            "handX", cm(m["hand_x"][0]), cm(m["hand_x"][1]),
            "handY", cm(m["hand_y"][0]), cm(m["hand_y"][1]),
            "chestY", cm(m["chest_y"]),
            "flags", ",".join(flags) if flags else "-",
        )
        shoot(arm, pieces, solids, dest, yaw, fill, lift)
        return m

    for name, kind in order:
        floor_z = sole - 0.28 if kind == "lid" else sole
        for i, u in enumerate((0, 0.25, 0.5, 0.75, 1)):
            one(name, kind, u, three, os.path.join(PANEL, "%s-%d.png" % (name, i)), 0.72, 0.28, floor_z)
        print("PANELS", name)
    for i, u in enumerate((0, 0.25, 0.5, 0.75, 1)):
        one("exit-ClimbTopOut", "lid", u, side, os.path.join(PANEL, "exit-ClimbTopOut-side-%d.png" % i), 0.72, 0.22, sole - 0.28)
    print("PANELS side")

    # Full-size hip stills. Before is the pass 24 beat. After is the new dump.
    scene = bpy.context.scene
    scene.render.resolution_x = 640
    scene.render.resolution_y = 860
    befores = {
        "exit-ClimbTopOut": put(zeros(), hip=34, spine=14, head=-10, tL=32, tR=32, kL=-136, kR=-136, aL=-30, aR=-26, yL=4, yR=-2, eL=-14, eR=-10),
        "exit-Vault": put(zeros(), hip=18, spine=10, head=4, tL=0, tR=-2, kL=-38, kR=-34, aL=-32, aR=-16, yL=-6, yR=2, eL=-16, eR=-14),
        "exit-Mantle": put(zeros(), hip=-12, spine=8, spineYaw=-6, head=-6, tL=18, tR=16, kL=-20, kR=-18, aL=20, aR=22, yL=14, yR=-16, eL=-50, eR=-46, rollL=-78, rollR=78),
    }
    after_u = {"exit-ClimbTopOut": 0.0, "exit-Vault": 0.5, "exit-Mantle": 0.0}
    kind_of = {"exit-ClimbTopOut": "lid", "exit-Vault": "vault", "exit-Mantle": "lid"}
    for name in ("exit-ClimbTopOut", "exit-Vault", "exit-Mantle"):
        kind = kind_of[name]
        floor.location.z = sole - 0.28 if kind == "lid" else sole
        pieces = pose_of(arm, base, befores[name], kind)
        m = measure(pieces)
        solids = solids_for(kind, pieces, vault) + marker(m, sole)
        path = os.path.join(HIP, name + "-before.png")
        shoot(arm, pieces, solids, path, side, 0.82, 0.12)
        print("HIP before", name, "footY", cm(m["foot_y"][0]), cm(m["foot_y"][1]))
        frames = clips[name]["frames"]
        end = frames[-1][0]
        _t, nums = nearest(frames, end * after_u[name])
        pieces = pose_of(arm, base, nums, kind)
        m = measure(pieces)
        solids = solids_for(kind, pieces, vault) + marker(m, sole)
        path = os.path.join(HIP, name + "-after.png")
        shoot(arm, pieces, solids, path, side, 0.82, 0.12)
        print("HIP after", name, "footY", cm(m["foot_y"][0]), cm(m["foot_y"][1]), "pelvisY", cm(m["hip"][1]))
    print("STILLS_EXIT")


if __name__ == "__main__":
    main()
