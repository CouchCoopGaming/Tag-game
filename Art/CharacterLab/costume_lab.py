"""Prep-only costume prototypes for the couch tag mannequin.

Builds modular cloth in a separate lab scene and skins it to the shipped
Hier skeleton. The mannequin FBX, its bind, and the game scenes are only read.
"""
import json
import math
import os
import sys
import time

import bpy
from mathutils import Quaternion, Vector
from mathutils.bvhtree import BVHTree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT_DIR = os.path.join(ROOT, "Art", "CharacterLab")
BLEND = os.path.join(OUT_DIR, "CostumeLab.blend")
LOADOUTS = os.path.join(OUT_DIR, "loadouts.json")
FIT_PATH = os.path.join(ROOT, "Docs", "Characters", "pass1", "fit.txt")

# Cloth sits outside the rendered hull. Hoodies and jackets are the thick end of the band.
BAND_MIN = 0.003
BAND_MAX = 0.010
CLOTH_OFFSET = 0.006
HOODIE_OFFSET = 0.009
SHOE_OFFSET = 0.008
PEN_LIMIT = 0.005
# Rounded hundredths of a centimetre. A printed 0.50 is the limit.
PEN_CM = 0.50

# stations, sides. Tuned so one long outfit lands near 8–15k triangles.
DENSITY = {
    "chest": (22, 32),
    "spine": (8, 28),
    "shoulder": (8, 20),
    "sleeve": (14, 20),
    "jog": (14, 22),
    "shoe": (12, 18),
    "hood": (14, 26),
    "helmet": (14, 26),
    "cap": (10, 22),
    "collar": (5, 20),
    "pad": (5, 12),
}

# Candidate insets, metres. The joint search picks the first one that clears.
INSET_STEPS = [i / 100.0 for i in range(2, 16)]
JOINT_CLEAR = 0.008
BEVEL_M = 0.0025

# Street kit, then Bram's own jacket kit. Same bones, different meshes.
# Shortest split at each joint, metres. The flex search can only go longer.
JOINT_FLOOR = {
    ("UpperArm_", "LowerArm_"): 0.060,
    ("UpperLeg_", "LowerLeg_"): 0.090,
    ("LowerLeg_", "Foot_"): 0.055,
    ("Hips", "UpperLeg_"): 0.060,
}

# Shells that must stay this far (metres) off a neighbouring plate at rest,
# so a small idle swing does not bury them.
CLEAR_ROWS = (
    ("Lab_HoodieChest", ("Mesh_Shoulder_L", "Mesh_Shoulder_R", "Mesh_UpperArm_L", "Mesh_UpperArm_R"), 0.040),
    ("Lab_HoodieBramChest", ("Mesh_Shoulder_L", "Mesh_Shoulder_R", "Mesh_UpperArm_L", "Mesh_UpperArm_R"), 0.040),
    ("Lab_HoodieHips", ("Mesh_UpperLeg_L", "Mesh_UpperLeg_R"), 0.015),
    ("Lab_HoodieBramHips", ("Mesh_UpperLeg_L", "Mesh_UpperLeg_R"), 0.015),
    ("Lab_HoodieSpine", ("Mesh_UpperLeg_L", "Mesh_UpperLeg_R"), 0.045),
    ("Lab_HoodieBramSpine", ("Mesh_UpperLeg_L", "Mesh_UpperLeg_R"), 0.045),
    ("Lab_SleeveShL", ("Mesh_Chest",), 0.055),
    ("Lab_SleeveShR", ("Mesh_Chest",), 0.055),
    ("Lab_SleeveBramShL", ("Mesh_Chest",), 0.055),
    ("Lab_SleeveBramShR", ("Mesh_Chest",), 0.055),
    ("Lab_SleeveShL", ("Mesh_Shoulder_L",), 0.022),
    ("Lab_SleeveShR", ("Mesh_Shoulder_R",), 0.022),
    ("Lab_SleeveBramShL", ("Mesh_Shoulder_L",), 0.022),
    ("Lab_SleeveBramShR", ("Mesh_Shoulder_R",), 0.022),
    ("Lab_JogUL", ("Mesh_Hips",), 0.030),
    ("Lab_JogUR", ("Mesh_Hips",), 0.030),
    ("Lab_JogUBramL", ("Mesh_Hips",), 0.030),
    ("Lab_JogUBramR", ("Mesh_Hips",), 0.030),
    ("Lab_JogHipL", ("Mesh_Hips",), 0.012),
    ("Lab_JogHipR", ("Mesh_Hips",), 0.012),
    ("Lab_JogHipBramL", ("Mesh_Hips",), 0.012),
    ("Lab_JogHipBramR", ("Mesh_Hips",), 0.012),
    ("Lab_JogAnkleL", ("Mesh_Foot_L",), 0.032),
    ("Lab_JogAnkleR", ("Mesh_Foot_R",), 0.032),
    ("Lab_JogAnkleBramL", ("Mesh_Foot_L",), 0.032),
    ("Lab_JogAnkleBramR", ("Mesh_Foot_R",), 0.032),
    ("Lab_ShoeL", ("Mesh_LowerLeg_L",), 0.014),
    ("Lab_ShoeR", ("Mesh_LowerLeg_R",), 0.014),
    ("Lab_ShoeBramL", ("Mesh_LowerLeg_L",), 0.014),
    ("Lab_ShoeBramR", ("Mesh_LowerLeg_R",), 0.014),
    ("Lab_SleeveUL", ("Mesh_Chest", "Mesh_Shoulder_L"), 0.030),
    ("Lab_SleeveUR", ("Mesh_Chest", "Mesh_Shoulder_R"), 0.030),
    ("Lab_SleeveUBramL", ("Mesh_Chest", "Mesh_Shoulder_L"), 0.030),
    ("Lab_SleeveUBramR", ("Mesh_Chest", "Mesh_Shoulder_R"), 0.030),
)

# parent bone, child bone, flexion degrees, local-X sign, pad token, pad bone
JOINT_ROWS = (
    ("UpperArm_", "LowerArm_", 50.0, -1.0, "SleeveElbow", "UpperArm_"),
    ("UpperLeg_", "LowerLeg_", 150.0, 1.0, "JogKnee", "UpperLeg_"),
    ("LowerLeg_", "Foot_", 55.0, 1.0, "JogAnkle", "LowerLeg_"),
)

OUTFITS = (
    {
        "tag": "",
        "cloth": 0.006,
        "thick": 0.009,
        "hem": 0.002,
        "chest_name": "Lab_HoodieChest",
        "spine_name": "Lab_HoodieSpine",
        "hip_name": "Lab_HoodieHips",
        "shoe_name": "Lab_Shoe",
        "cuff_name": "Lab_SleeveCuff",
        "shoulder_name": "Lab_SleeveSh",
    },
    {
        "tag": "Bram",
        "cloth": 0.008,
        "thick": 0.009,
        "hem": 0.045,
        "chest_name": "Lab_HoodieBramChest",
        "spine_name": "Lab_HoodieBramSpine",
        "hip_name": "Lab_HoodieBramHips",
        "shoe_name": "Lab_ShoeBram",
        "cuff_name": "Lab_SleeveBramCuff",
        "shoulder_name": "Lab_SleeveBramSh",
    },
)

PLAYER = {
    "Reed": (0.886, 0.235, 0.227),
    "Bram": (0.184, 0.435, 0.878),
    "Pip": (0.941, 0.478, 0.102),
    "Sol": (0.66, 0.50, 0.84),
}
TRIM = (0.10, 0.10, 0.12)
SHOE_COLOR = (0.93, 0.93, 0.91)

BODY_NAMES = (
    "Mesh_Head", "Mesh_Neck", "Mesh_Chest", "Mesh_Spine", "Mesh_Hips",
    "Mesh_Shoulder_L", "Mesh_Shoulder_R",
    "Mesh_UpperArm_L", "Mesh_UpperArm_R", "Mesh_LowerArm_L", "Mesh_LowerArm_R",
    "Mesh_Hand_L", "Mesh_Hand_R",
    "Mesh_UpperLeg_L", "Mesh_UpperLeg_R", "Mesh_LowerLeg_L", "Mesh_LowerLeg_R",
    "Mesh_Foot_L", "Mesh_Foot_R",
)


def log(msg):
    print(msg, flush=True)


class Piece:
    def __init__(self, name, bone, own, kind):
        self.name = name
        self.bone = bone
        self.own = own
        self.kind = kind  # cloth or accessory
        self.chains = []  # list of rings; ring is list of Vector or None
        self.loop = True
        self.boxes = []  # (center, ax, ay, az, hx, hy, hz) in armature space
        self.obj = None
        self.offset = CLOTH_OFFSET

    def vert_count(self):
        n = 0
        for chain in self.chains:
            for ring in chain:
                n += sum(1 for p in ring if p is not None)
        return n


def import_hier():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    arm.location = (0.0, 0.0, 0.0)
    arm.rotation_euler = (0.0, 0.0, 0.0)
    arm.scale = (1.0, 1.0, 1.0)
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = (0.0, 0.0, 0.0)
        bone.location = (0.0, 0.0, 0.0)
        bone.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()
    return arm


def tint_mannequin():
    rgba = (0.25, 0.22, 0.20, 1.0)
    seen = set()
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Mesh_"):
            continue
        for mat in obj.data.materials:
            if mat is None or mat.name in seen:
                continue
            seen.add(mat.name)
            mat.diffuse_color = rgba
            if mat.use_nodes:
                for node in mat.node_tree.nodes:
                    if node.type == "BSDF_PRINCIPLED":
                        node.inputs["Base Color"].default_value = rgba
                        if "Roughness" in node.inputs:
                            node.inputs["Roughness"].default_value = 0.72


def make_material(name, color, player):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (color[0], color[1], color[2], 1.0)
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Roughness"].default_value = 0.62
    if player:
        rgb = nt.nodes.new("ShaderNodeRGB")
        rgb.name = "PlayerColor"
        rgb.label = "PlayerColor"
        rgb.outputs[0].default_value = (color[0], color[1], color[2], 1.0)
        nt.links.new(rgb.outputs[0], bsdf.inputs["Base Color"])
        mat["player_color"] = 1
    else:
        bsdf.inputs["Base Color"].default_value = (color[0], color[1], color[2], 1.0)
        mat["player_color"] = 0
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 8.0
    noise.inputs["Detail"].default_value = 0.0
    bump = nt.nodes.new("ShaderNodeBump")
    # Solid seat colours. A strong noise bump read as torn, mottled cloth.
    bump.inputs["Strength"].default_value = 0.0
    nt.links.new(noise.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def capture_body():
    body = {}
    for name in BODY_NAMES:
        obj = bpy.data.objects[name]
        verts = [v.co.copy() for v in obj.data.vertices]
        polys = [tuple(p.vertices) for p in obj.data.polygons]
        bvh = BVHTree.FromPolygons(verts, polys)
        centroid = Vector((0.0, 0.0, 0.0))
        for v in verts:
            centroid += v
        centroid /= max(1, len(verts))
        acc = 0.0
        for poly in obj.data.polygons:
            acc += poly.normal.dot(poly.center - centroid)
        sign = 1.0 if acc >= 0.0 else -1.0
        mw = obj.matrix_world.copy()
        body[name] = {
            "obj": obj,
            "bvh": bvh,
            "sign": sign,
            "centroid": centroid,
            "mw": mw,
            "inv": mw.inverted(),
            "bmin": Vector((min(v.x for v in verts), min(v.y for v in verts), min(v.z for v in verts))),
            "bmax": Vector((max(v.x for v in verts), max(v.y for v in verts), max(v.z for v in verts))),
            "world": [mw @ v for v in verts],
        }
    return body


def refresh_body(body):
    for info in body.values():
        mw = info["obj"].matrix_world.copy()
        info["mw"] = mw
        info["inv"] = mw.inverted()


def aabb_distance(info, local):
    dx = 0.0
    if local.x < info["bmin"].x:
        dx = info["bmin"].x - local.x
    elif local.x > info["bmax"].x:
        dx = local.x - info["bmax"].x
    dy = 0.0
    if local.y < info["bmin"].y:
        dy = info["bmin"].y - local.y
    elif local.y > info["bmax"].y:
        dy = local.y - info["bmax"].y
    dz = 0.0
    if local.z < info["bmin"].z:
        dz = info["bmin"].z - local.z
    elif local.z > info["bmax"].z:
        dz = local.z - info["bmax"].z
    return math.sqrt(dx * dx + dy * dy + dz * dz)


def ray_inside(bvh, local):
    """Same three-ray vote the mannequin noclip check uses."""
    votes = 0
    for direction in (Vector((1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0)), Vector((0.0, 0.0, 1.0))):
        hit = bvh.ray_cast(local, direction)
        if hit[0] is None:
            votes -= 1
        elif hit[1].dot(direction) > 0.0:
            votes += 1
        else:
            votes -= 1
    return votes > 0


def gap_to_one(info, point):
    """Gap is negative inside the mesh. The vector points back out of the body."""
    local = info["inv"] @ point
    loc, normal, _idx, dist = info["bvh"].find_nearest(local)
    if loc is None:
        return None
    outside_box = aabb_distance(info, local) > 1e-4
    if dist <= 1e-5:
        inside = False
        dist = 0.0
    elif outside_box:
        inside = False
    else:
        inside = ray_inside(info["bvh"], local)
    if inside:
        direction = loc - local
        gap = -dist
    else:
        direction = local - loc
        gap = dist
    if direction.length < 1e-8:
        direction = normal if info["sign"] > 0.0 else -normal
    rot = info["mw"].to_3x3()
    nworld = rot @ direction
    if nworld.length < 1e-8:
        nworld = Vector((0.0, 0.0, 1.0))
    else:
        nworld.normalize()
    return gap, nworld


def gap_to_body(body, point, ignore=None, margin=0.04):
    best = None
    for name, info in body.items():
        if name == ignore:
            continue
        local = info["inv"] @ point
        if aabb_distance(info, local) > margin:
            continue
        hit = gap_to_one(info, point)
        if hit is None:
            continue
        if best is None or hit[0] < best[0]:
            best = (hit[0], hit[1], name)
    return best


def nearest_gap(body, point):
    """Smallest gap against every body mesh. None means nothing within the cull."""
    hit = gap_to_body(body, point, ignore=None, margin=0.08)
    return hit


def probe_body(body):
    surf = body["Mesh_Chest"]["world"][0]
    local = body["Mesh_Chest"]["obj"].data.vertices[0].co
    _loc, _n, _i, dist = body["Mesh_Chest"]["bvh"].find_nearest(local)
    log("PROBE local-surface-cm %.3f" % (dist * 100.0))
    if dist > 0.001:
        raise SystemExit("body BVH is not in mesh local space")
    center = Vector((0.0, 0.0, 1.25))
    hit = nearest_gap(body, center)
    log("PROBE chest-center cm %.2f mesh %s" % (hit[0] * 100.0, hit[2]))
    if hit[0] >= 0.0:
        raise SystemExit("chest center is outside the mannequin")
    outside = Vector((0.0, -0.16, 1.25))
    hit2 = nearest_gap(body, outside)
    if hit2 is None:
        raise SystemExit("outside probe missed every body mesh")
    log("PROBE outside cm %.2f mesh %s" % (hit2[0] * 100.0, hit2[2]))
    if hit2[0] <= 0.0:
        raise SystemExit("a point in front of the chest is inside the mannequin")
    _ = surf


def basis_from(axis):
    axis = axis.normalized()
    up = Vector((0.0, 0.0, 1.0))
    if abs(axis.dot(up)) > 0.85:
        up = Vector((0.0, 1.0, 0.0))
    bx = axis.cross(up).normalized()
    by = axis.cross(bx).normalized()
    return axis, bx, by


def bone_axis(arm, bone_name):
    bone = arm.data.bones[bone_name]
    head = arm.matrix_world @ bone.head_local
    tail = arm.matrix_world @ bone.tail_local
    return head, (tail - head).normalized()


def project_span(points, origin, axis):
    ts = [(p - origin).dot(axis) for p in points]
    return min(ts), max(ts)


def ray_hit_world(info, origin, direction):
    """First surface hit. Origin and direction are world space; the BVH is local."""
    inv = info["inv"]
    local_origin = inv @ origin
    local_dir = inv.to_3x3() @ direction
    if local_dir.length < 1e-8:
        return None
    local_dir.normalize()
    hit, _normal, _index, _dist = info["bvh"].ray_cast(local_origin, local_dir)
    if hit is None:
        return None
    return info["mw"] @ hit


def surface_along(info, origin, direction):
    """Outer skin. An inside-out cast hits the chest's inner plate and hides the cloth."""
    far = origin + direction * 0.45
    hit = ray_hit_world(info, far, -direction)
    if hit is not None:
        return hit
    return ray_hit_world(info, origin, direction)


def visual_gap(info, point):
    """Signed distance to the rendered hull. Positive is outside the outer surface."""
    centroid = info["mw"] @ info["centroid"]
    direction = point - centroid
    if direction.length < 1e-5:
        return None
    direction.normalize()
    hit = surface_along(info, centroid, direction)
    if hit is None:
        return None
    return (point - hit).dot(direction), direction


def visual_penetration(body, point, ignore=None):
    """How deep a point is inside any rendered body mesh, in metres.

    Nearest-surface distance. A centroid ray reports the far cap of a long
    limb and used to shove cloth that was already outside.
    """
    worst = 0.0
    where = ""
    normal = Vector((0.0, 0.0, 1.0))
    for name, info in body.items():
        if name == ignore:
            continue
        local = info["inv"] @ point
        if aabb_distance(info, local) > 0.03:
            continue
        hit = gap_to_one(info, point)
        if hit is None or hit[0] >= -1e-5:
            continue
        depth = -hit[0]
        if depth > worst:
            worst = depth
            where = name
            normal = hit[1]
    return worst, where, normal


def loft_piece(piece, points, origin, axis, t0, t1, n_stations, n_sides, offset, body, mask=None, fill_ring=True):
    """Shell stations by casting onto the faces. Limb shafts have verts only at the lips."""
    _ = points
    axis, bx, by = basis_from(axis)
    if t1 - t0 < 0.015:
        return
    own = body[piece.own]
    chain = []
    for i in range(n_stations):
        u = 0.0 if n_stations == 1 else i / (n_stations - 1)
        station = t0 + (t1 - t0) * u
        center = origin + axis * station
        ring = [None] * n_sides
        hits = 0
        for s in range(n_sides):
            ang = math.tau * s / n_sides
            direction = (bx * math.cos(ang) + by * math.sin(ang)).normalized()
            if mask is not None and not mask(direction, center):
                continue
            surface = surface_along(own, center, direction)
            if surface is None:
                continue
            placed = surface + direction * offset
            own_gap = gap_to_one(own, placed)
            if own_gap is not None and own_gap[0] < offset - 1e-4:
                placed = placed + own_gap[1] * (offset - own_gap[0])
            ring[s] = placed
            hits += 1
        if hits >= 3:
            if fill_ring:
                _fill_ring(ring)
            chain.append(ring)
    if len(chain) >= 2:
        _fill_stations(chain)
        piece.chains.append(chain)


def _fill_ring(ring):
    """A missed ray becomes a hole. Borrow the nearest sample on the ring."""
    n = len(ring)
    live = [i for i, p in enumerate(ring) if p is not None]
    if len(live) < 3:
        return
    for i, p in enumerate(ring):
        if p is not None:
            continue
        prev = max(live, key=lambda j: -min((i - j) % n, (j - i) % n))
        # nearest by circular distance
        best = live[0]
        best_d = n
        for j in live:
            dist = min((i - j) % n, (j - i) % n)
            if dist < best_d:
                best_d = dist
                best = j
        ring[i] = ring[best].copy()


def _fill_stations(chain):
    for index, ring in enumerate(chain):
        if any(p is not None for p in ring):
            continue
        donor = None
        for step in range(1, len(chain)):
            if index - step >= 0 and any(p is not None for p in chain[index - step]):
                donor = chain[index - step]
                break
            if index + step < len(chain) and any(p is not None for p in chain[index + step]):
                donor = chain[index + step]
                break
        if donor is None:
            continue
        chain[index] = [None if p is None else p.copy() for p in donor]


def close_top(piece, point):
    """Pull the last ring to one crown point so a hood is not an open tube."""
    if not piece.chains or not piece.chains[0]:
        return
    chain = piece.chains[0]
    n = len(chain[-1])
    chain.append([point.copy() for _ in range(n)])


def flex_clearance(d0, d1, flexion_deg, r0, r1):
    """Distance between points on two bones minus the two radii, after a flexion."""
    flex = math.radians(flexion_deg)
    dist2 = d0 * d0 + d1 * d1 + 2.0 * d0 * d1 * math.cos(flex)
    if dist2 < 0.0:
        dist2 = 0.0
    return math.sqrt(dist2) - r0 - r1


def trim_span_for_flex(span, radius_at, flexion, other_span, other_radius, keep_near_head):
    """Shorten the end nearest the joint until a folded pair clears 0.8 cm.

    radius_at(distance_from_joint) -> radius. The joint is t=span[0] when
    keep_near_head is False (cloth grows from the far end), else the joint is
    span[1].
    """
    t_far = span[1] if keep_near_head else span[0]
    t_joint = span[0] if keep_near_head else span[1]
    # Search the closest station we can keep.
    best = abs(t_far - t_joint) * 0.45
    for step in range(8, 28):
        dist = abs(t_far - t_joint) * (step / 28.0)
        r = radius_at(dist)
        d_other = abs(other_span[1] - other_span[0]) * 0.45
        r_other = other_radius(d_other)
        gap = flex_clearance(dist, d_other, flexion, r, r_other)
        if gap >= 0.008:
            best = dist
            break
    if keep_near_head:
        return span[0], t_joint - best if t_joint > span[0] else t_joint + best
    return (t_joint + best if t_far > t_joint else t_joint - best), span[1]


def radius_profile(points, origin, axis):
    samples = []
    for p in points:
        rel = p - origin
        t = rel.dot(axis)
        radial = rel - axis * t
        samples.append((t, radial.length))
    samples.sort()

    def at(dist_from_joint, joint_t):
        target = joint_t + dist_from_joint
        best = 0.04
        best_d = 1e9
        for t, r in samples:
            d = abs(t - target)
            if d < best_d:
                best_d = d
                best = r
        return best

    return samples, at


def finish_mesh(mesh):
    mesh.update()
    mesh.validate(clean_customdata=False)
    if not mesh.polygons or not mesh.vertices:
        return 0
    centroid = Vector((0.0, 0.0, 0.0))
    for v in mesh.vertices:
        centroid += v.co
    centroid /= len(mesh.vertices)
    acc = 0.0
    for poly in mesh.polygons:
        acc += poly.normal.dot(poly.center - centroid)
    if acc < 0.0:
        mesh.flip_normals()
    for poly in mesh.polygons:
        poly.use_smooth = True
    mesh.update()
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)


def build_mesh(piece):
    verts = []
    index = {}
    for ci, chain in enumerate(piece.chains):
        for ri, ring in enumerate(chain):
            for si, p in enumerate(ring):
                if p is None:
                    continue
                index[(ci, ri, si)] = len(verts)
                verts.append(p)
    faces = []
    for ci, chain in enumerate(piece.chains):
        if not chain:
            continue
        n_side = len(chain[0])
        for ri in range(len(chain) - 1):
            for si in range(n_side):
                si2 = (si + 1) % n_side
                if si2 == 0 and not piece.loop:
                    continue
                keys = ((ci, ri, si), (ci, ri, si2), (ci, ri + 1, si2), (ci, ri + 1, si))
                if not all(k in index for k in keys):
                    continue
                faces.append(tuple(index[k] for k in keys))
    mesh = bpy.data.meshes.new(piece.name + "Mesh")
    if len(verts) < 3 or not faces:
        mesh.from_pydata([], [], [])
        return mesh, 0
    mesh.from_pydata(verts, [], faces)
    tris = finish_mesh(mesh)
    return mesh, tris


def realize(piece, arm, mat):
    mesh, tris = build_mesh(piece)
    if piece.obj is None:
        obj = bpy.data.objects.new(piece.name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        obj.parent = arm
        obj.matrix_parent_inverse = arm.matrix_world.inverted()
        mod = obj.modifiers.new("Armature", "ARMATURE")
        mod.object = arm
        mod.use_vertex_groups = True
        piece.obj = obj
    else:
        old = piece.obj.data
        piece.obj.data = mesh
        if old.users == 0:
            bpy.data.meshes.remove(old)
    obj = piece.obj
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    # Object-linked so lineup copies can recolor one parameter without new topology.
    obj.material_slots[0].link = "OBJECT"
    obj.material_slots[0].material = mat
    obj.vertex_groups.clear()
    if len(obj.data.vertices) > 0:
        vg = obj.vertex_groups.new(name=piece.bone)
        vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    obj["costume_bone"] = piece.bone
    obj["costume_kind"] = piece.kind
    obj["costume_own"] = piece.own
    return tris


def set_modifiers(pieces, enabled):
    for piece in pieces:
        if piece.obj is None:
            continue
        for mod in piece.obj.modifiers:
            mod.show_viewport = enabled
            mod.show_render = enabled


def enforce_band(piece, body):
    """Seat cloth outside its plate, at least 3 mm and inside the 1 cm band."""
    own = body[piece.own]
    cut = 0
    moved = 0
    target = max(BAND_MIN, getattr(piece, "offset", CLOTH_OFFSET))
    hi = BAND_MAX - 0.0002
    for chain in piece.chains:
        for ring in chain:
            for s, p in enumerate(ring):
                if p is None:
                    continue
                own_hit = gap_to_one(own, p)
                if own_hit is None:
                    ring[s] = None
                    cut += 1
                    continue
                gap, normal = own_hit
                if piece.kind != "cloth":
                    if gap < BAND_MIN:
                        ring[s] = p + normal * (0.004 - gap)
                        moved += 1
                    continue
                placed = p
                if gap < target:
                    placed = p + normal * (target - gap)
                elif gap > hi:
                    placed = p - normal * (gap - hi)
                seated = placed
                neighbour = gap_to_body(body, placed, ignore=piece.own, margin=0.04)
                if neighbour is not None and neighbour[0] < 0.003:
                    depth = 0.003 - neighbour[0]
                    placed = placed + neighbour[1] * depth
                    check = gap_to_one(own, placed)
                    still = gap_to_body(body, placed, ignore=piece.own, margin=0.05)
                    # Own-bone offset only. A sample that stays inside another
                    # plate is cut, not shoved out into the next shell.
                    if (
                        check is None
                        or check[0] < BAND_MIN - 1e-4
                        or check[0] > hi
                        or (still is not None and still[0] < 0.003)
                    ):
                        ring[s] = None
                        cut += 1
                        continue
                check = gap_to_one(own, placed)
                # Restoring the own-bone seat is fine only when that seat is
                # outside every other plate. Otherwise the sample is buried.
                seated_foreign = gap_to_body(body, seated, ignore=piece.own, margin=0.05)
                seated_ok = seated_foreign is None or seated_foreign[0] >= 0.003
                if (check is None or check[0] < target - 1e-4) and seated_ok:
                    placed = seated
                    check = gap_to_one(own, placed)
                if check is None or check[0] < BAND_MIN - 1e-4:
                    ring[s] = None
                    cut += 1
                    continue
                if check[0] > hi:
                    placed = placed - check[1] * (check[0] - hi)
                    check = gap_to_one(own, placed)
                    if check is None or check[0] < BAND_MIN - 1e-4 or check[0] > BAND_MAX:
                        ring[s] = None
                        cut += 1
                        continue
                if (placed - p).length > 1e-5:
                    moved += 1
                ring[s] = placed
    return moved, cut


def add_box(piece, center, ax, ay, az, hx, hy, hz, nu, nv):
    """A quad grid for each of the six faces. Volume is the oriented box."""
    ax = ax.normalized()
    ay = ay.normalized()
    az = az.normalized()
    piece.boxes.append((center.copy(), ax.copy(), ay.copy(), az.copy(), hx, hy, hz))
    faces = (
        (ax, ay, az, hx, hy, hz),
        (-ax, ay, -az, hx, hy, hz),
        (ay, ax, az, hy, hx, hz),
        (-ay, ax, -az, hy, hx, hz),
        (az, ax, ay, hz, hx, hy),
        (-az, ax, ay, hz, hx, hy),
    )
    for normal, u, v, hn, hu, hv in faces:
        chain = []
        # A thin shell on the face so the grid has one ring pair (outer only would be a single layer).
        # Two stations: the face and a 1 mm inset, so the shell has thickness without filling the box.
        for depth, scale in ((0.0, 1.0), (0.001, 0.96)):
            ring = []
            origin = center + normal * (hn - depth)
            for iu in range(nu):
                fu = -1.0 + 2.0 * iu / (nu - 1)
                for iv in range(nv):
                    fv = -1.0 + 2.0 * iv / (nv - 1)
                    ring.append(origin + u * (hu * fu * scale) + v * (hv * fv * scale))
            # Store as a row-major ring list split into nv-length rows via one chain of rows.
            chain.append(ring)
        # Rebuild as rows so quads form. Each "ring" is a row along v.
        rows = []
        for station in chain:
            # station is nu*nv flattened. Split into nu rows of nv.
            for iu in range(nu):
                rows.append(station[iu * nv:(iu + 1) * nv])
        # Using one chain of rows loses the depth pairing. Build two layers as separate
        # row chains so consecutive rows quad. Depth pairing is handled below.
        piece.loop = False
    # Build a proper closed box grid instead of the row attempt above.
    piece.chains = [c for c in piece.chains]  # keep any existing
    _add_box_chains(piece, center, ax, ay, az, hx, hy, hz, nu, nv)


def _add_box_chains(piece, center, ax, ay, az, hx, hy, hz, nu, nv):
    ax = ax.normalized()
    ay = ay.normalized()
    az = az.normalized()

    def grid(origin, u, v, hu, hv, nu_, nv_):
        chain = []
        for iu in range(nu_):
            fu = -1.0 + (0.0 if nu_ == 1 else 2.0 * iu / (nu_ - 1))
            row = []
            for iv in range(nv_):
                fv = -1.0 + (0.0 if nv_ == 1 else 2.0 * iv / (nv_ - 1))
                row.append(origin + u * (hu * fu) + v * (hv * fv))
            chain.append(row)
        return chain

    specs = (
        (center + az * hz, ax, ay, hx, hy),
        (center - az * hz, ax, ay, hx, hy),
        (center + ay * hy, ax, az, hx, hz),
        (center - ay * hy, ax, az, hx, hz),
        (center + ax * hx, ay, az, hy, hz),
        (center - ax * hx, ay, az, hy, hz),
    )
    saved = piece.loop
    for origin, u, v, hu, hv in specs:
        piece.chains.append(grid(origin, u, v, hu, hv, nu, nv))
    piece.loop = False
    # loop False is required for box grids. Loft pieces that share this object
    # are not used; boxes are their own pieces.
    piece._box_loop = saved


def tube(piece, points_fn):
    """points_fn(i, s, n_i, n_s) -> Vector. Append one closed chain."""
    n_i, n_s = points_fn.n
    chain = []
    for i in range(n_i):
        ring = []
        for s in range(n_s):
            ring.append(points_fn(i, s, n_i, n_s))
        chain.append(ring)
    piece.chains.append(chain)


def bone_bend_axis(arm, bone_name):
    bone = arm.data.bones[bone_name]
    rot = arm.matrix_world.to_3x3() @ bone.matrix_local.to_3x3()
    return (rot @ Vector((1.0, 0.0, 0.0))).normalized()


def rotate_point(point, pivot, axis, degrees):
    spun = Quaternion(axis, math.radians(degrees)) @ (point - pivot)
    return pivot + spun


def ring_samples(center, normal, radius, count=8):
    _axis, bx, by = basis_from(normal)
    found = []
    for index in range(count):
        ang = math.tau * index / count
        found.append(center + (bx * math.cos(ang) + by * math.sin(ang)) * radius)
    return found


def rings_clear(left, right, margin):
    for a in left:
        for b in right:
            if (a - b).length < margin:
                return False
    return True


def end_radius(points, origin, axis, joint_t, offset):
    near = []
    for point in points:
        rel = point - origin
        t = rel.dot(axis)
        if abs(t - joint_t) > 0.08:
            continue
        near.append((rel - axis * t).length)
    if not near:
        for point in points:
            rel = point - origin
            t = rel.dot(axis)
            near.append((rel - axis * t).length)
    near.sort()
    return near[len(near) // 2] + offset


def inset_for_flex(joint, axis_from, axis_to, bend, radius_from, radius_to, degrees):
    """First inset where the two end rings stay apart after the child bends."""
    for dist in INSET_STEPS:
        parent_center = joint - axis_from * dist
        child_center = joint + axis_to * dist
        parent_ring = ring_samples(parent_center, axis_from, radius_from)
        child_ring = [
            rotate_point(point, joint, bend, degrees)
            for point in ring_samples(child_center, axis_to, radius_to)
        ]
        if rings_clear(parent_ring, child_ring, JOINT_CLEAR):
            return dist
    return INSET_STEPS[-1]


def opening_direction(bend, bone_axis, rot_sign):
    swing = bend.cross(bone_axis)
    if swing.length < 1e-6:
        return Vector((0.0, -1.0, 0.0))
    swing.normalize()
    if rot_sign < 0.0:
        swing = -swing
    return -swing


def bevel_cuffs(piece, body, amount=BEVEL_M, stations=2):
    """Pull the rim toward the bone. Leave at least 3 mm of own-bone cloth."""
    guide = getattr(piece, "guide", None)
    if guide is None:
        return
    origin, axis = guide
    own = body[piece.own]
    for chain in piece.chains:
        count = len(chain)
        if count < 3:
            continue
        for ri, ring in enumerate(chain):
            edge = min(ri, count - 1 - ri)
            if edge >= stations:
                continue
            pull = amount * float(stations - edge) / float(stations)
            live = [point for point in ring if point is not None]
            if len(live) < 3:
                continue
            center = sum(live, Vector()) / len(live)
            t = (center - origin).dot(axis)
            axial = origin + axis * t
            for si, point in enumerate(ring):
                if point is None:
                    continue
                radial = point - axial
                length = radial.length
                if length < pull + 0.004:
                    continue
                moved = point - radial / length * pull
                hit = gap_to_one(own, moved)
                if hit is None or hit[0] < BAND_MIN:
                    continue
                if hit[0] > BAND_MAX:
                    continue
                ring[si] = moved


def rib_pad(piece, extra=0.0025):
    """Alternate stations sit a little proud so the pad reads as a soft sleeve."""
    guide = getattr(piece, "guide", None)
    if guide is None:
        return
    origin, axis = guide
    for chain in piece.chains:
        for ri, ring in enumerate(chain):
            if ri % 2 == 0:
                continue
            live = [point for point in ring if point is not None]
            if len(live) < 3:
                continue
            center = sum(live, Vector()) / len(live)
            t = (center - origin).dot(axis)
            axial = origin + axis * t
            for si, point in enumerate(ring):
                if point is None:
                    continue
                radial = point - axial
                length = radial.length
                if length < 1e-5:
                    continue
                ring[si] = point + radial / length * extra


def make_loft(pieces, body, name, bone, own, kind, origin, axis, t0, t1, key, offset, mask=None, fill_ring=True, expand=0.04):
    if t1 < t0:
        t0, t1 = t1, t0
    if expand and t1 - t0 < expand:
        mid = (t0 + t1) * 0.5
        t0 = mid - expand * 0.5
        t1 = mid + expand * 0.5
    n_st, n_side = DENSITY[key]
    piece = Piece(name, bone, own, kind)
    piece.loop = bool(fill_ring)
    piece.offset = offset
    axis = axis.normalized()
    piece.guide = (origin.copy(), axis.copy())
    loft_piece(piece, body[own]["world"], origin, axis, t0, t1, n_st, n_side, offset, body, mask, fill_ring)
    pieces.append(piece)
    return piece


def span_limits(points, origin, axis):
    return project_span(points, origin, axis)


def _bone_pair(parent_bone, child_bone, side):
    parent = parent_bone + side if parent_bone.endswith("_") else parent_bone
    child = child_bone + side if child_bone.endswith("_") else child_bone
    return parent, child


def _collect_insets(arm, body, cloth):
    found = {}
    rows = JOINT_ROWS + (("Hips", "UpperLeg_", 50.0, 1.0, "JogHip", "UpperLeg_"),)
    for parent_bone, child_bone, flex, rot_sign, _pad, _pad_bone in rows:
        for side in ("L", "R"):
            parent, child = _bone_pair(parent_bone, child_bone, side)
            parent_head, parent_axis = bone_axis(arm, parent)
            child_head, child_axis = bone_axis(arm, child)
            joint = child_head
            bend = bone_bend_axis(arm, child)
            parent_t = (joint - parent_head).dot(parent_axis)
            r_parent = end_radius(body["Mesh_" + parent]["world"], parent_head, parent_axis, parent_t, cloth)
            r_child = end_radius(body["Mesh_" + child]["world"], child_head, child_axis, 0.0, cloth)
            dist = inset_for_flex(
                joint, parent_axis, child_axis, bend,
                r_parent, r_child, rot_sign * flex,
            )
            dist = max(dist, JOINT_FLOOR[(parent_bone, child_bone)])
            found[(parent, child)] = {
                "dist": dist,
                "flex": rot_sign * flex,
                "bend": bend,
                "child_axis": child_axis,
                "rot_sign": rot_sign,
            }
            log(
                "INSET %s -> %s %.1fcm r=%.1f/%.1f"
                % (parent, child, dist * 100.0, r_parent * 100.0, r_child * 100.0)
            )
    return found


def _mark_bevel(piece):
    piece.bevel = True
    return piece


def build_outfits(pieces, arm, body):
    """Street kit and Bram's jacket kit. Each shell is one bone."""
    z_axis = Vector((0.0, 0.0, 1.0))
    for outfit in OUTFITS:
        cloth = outfit["cloth"]
        thick = outfit["thick"]
        tag = outfit["tag"]
        chest_z = [point.z for point in body["Mesh_Chest"]["world"]]
        spine_z = [point.z for point in body["Mesh_Spine"]["world"]]
        hip_z = [point.z for point in body["Mesh_Hips"]["world"]]
        def keep_torso(direction, _center):
            return abs(direction.x) < 0.50

        def keep_back(direction, _center):
            # Face is -Y. The lower-back panel stays behind the lifted thigh.
            return direction.y > 0.20

        def keep_waist(direction, _center):
            # Rear seat, plus a narrow belly the thighs do not cross.
            if direction.y > 0.20:
                return True
            return direction.y < -0.55 and abs(direction.x) < 0.22

        make_loft(
            pieces, body, outfit["chest_name"], "Chest", "Mesh_Chest", "cloth",
            Vector((0.0, 0.0, 0.0)), z_axis,
            min(chest_z) + 0.012, max(chest_z) - 0.008, "chest", thick,
            keep_torso,
        )
        make_loft(
            pieces, body, outfit["spine_name"], "Spine", "Mesh_Spine", "cloth",
            Vector((0.0, 0.0, 0.0)), z_axis,
            min(spine_z) + 0.024, max(spine_z) - 0.006, "spine", thick,
            keep_back,
        )
        # Waistband on the iliac crest. The thigh sweep owns the pelvis below it.
        hip_hi = max(hip_z) - 0.016
        band = 0.028 if outfit["hem"] > 0.02 else 0.036
        make_loft(
            pieces, body, outfit["hip_name"], "Hips", "Mesh_Hips", "cloth",
            Vector((0.0, 0.0, 0.0)), z_axis,
            hip_hi - band, hip_hi, "spine", thick, keep_waist,
        )
        insets = _collect_insets(arm, body, cloth)
        for side in ("L", "R"):
            _build_side(pieces, arm, body, outfit, insets, side)
        log("OUTFIT %s" % (tag or "street"))


def _span(arm, body, bone, mesh):
    origin, axis = bone_axis(arm, bone)
    lo, hi = project_span(body[mesh]["world"], origin, axis)
    return origin, axis, lo, hi


def _opening_mask(keep):
    def mask(direction, _center):
        return direction.dot(keep) > 0.05
    return mask


def _add_pad(pieces, body, name, bone, mesh, origin, axis, t0, t1, offset, keep):
    piece = make_loft(
        pieces, body, name, bone, mesh, "cloth",
        origin, axis, t0, t1, "pad", offset,
        _opening_mask(keep), False, 0.0,
    )
    # Valleys stay near 4 mm so the ribs can sit proud inside the 1 cm band.
    piece.offset = 0.004
    rib_pad(piece)
    return _mark_bevel(piece)


def _bone_t(arm, origin, axis, bone):
    point = arm.matrix_world @ arm.data.bones[bone].head_local
    return (point - origin).dot(axis)


def _tail_t(arm, origin, axis, bone):
    point = arm.matrix_world @ arm.data.bones[bone].tail_local
    return (point - origin).dot(axis)


def _build_side(pieces, arm, body, outfit, insets, side):
    """Spans are measured from the joint, not from the mesh bounds."""
    tag = outfit["tag"]
    cloth = outfit["cloth"]
    thick = outfit["thick"]
    elbow = insets[("UpperArm_" + side, "LowerArm_" + side)]["dist"]
    knee = insets[("UpperLeg_" + side, "LowerLeg_" + side)]["dist"]
    ankle = insets[("LowerLeg_" + side, "Foot_" + side)]["dist"]
    hip = insets[("Hips", "UpperLeg_" + side)]["dist"]
    elbow_row = insets[("UpperArm_" + side, "LowerArm_" + side)]
    knee_row = insets[("UpperLeg_" + side, "LowerLeg_" + side)]
    ankle_row = insets[("LowerLeg_" + side, "Foot_" + side)]
    hip_row = insets[("Hips", "UpperLeg_" + side)]

    origin, axis, _lo, _hi = _span(arm, body, "UpperArm_" + side, "Mesh_UpperArm_" + side)
    _mark_bevel(make_loft(
        pieces, body, outfit["shoulder_name"] + side, "UpperArm_" + side, "Mesh_UpperArm_" + side, "cloth",
        origin, axis, 0.028, 0.056, "sleeve", thick, None, True, 0.0,
    ))
    elbow_t = _bone_t(arm, origin, axis, "LowerArm_" + side)
    _mark_bevel(make_loft(
        pieces, body, "Lab_SleeveU%s%s" % (tag, side), "UpperArm_" + side, "Mesh_UpperArm_" + side, "cloth",
        origin, axis, 0.080, elbow_t - elbow, "sleeve", cloth,
    ))
    keep = opening_direction(elbow_row["bend"], elbow_row["child_axis"], elbow_row["rot_sign"])
    _add_pad(
        pieces, body, "Lab_SleeveElbow%s%s" % (tag, side),
        "UpperArm_" + side, "Mesh_UpperArm_" + side,
        origin, axis, elbow_t - elbow + 0.010, elbow_t - 0.016, cloth, keep,
    )

    origin, axis, _lo, _hi = _span(arm, body, "LowerArm_" + side, "Mesh_LowerArm_" + side)
    wrist_t = _tail_t(arm, origin, axis, "LowerArm_" + side)
    _mark_bevel(make_loft(
        pieces, body, "Lab_SleeveL%s%s" % (tag, side), "LowerArm_" + side, "Mesh_LowerArm_" + side, "cloth",
        origin, axis, elbow, wrist_t - 0.040, "sleeve", cloth,
    ))
    _mark_bevel(make_loft(
        pieces, body, outfit["cuff_name"] + side, "LowerArm_" + side, "Mesh_LowerArm_" + side, "cloth",
        origin, axis, wrist_t - 0.032, wrist_t - 0.008, "sleeve", thick, None, True, 0.0,
    ))

    origin, axis, _lo, _hi = _span(arm, body, "UpperLeg_" + side, "Mesh_UpperLeg_" + side)
    knee_t = _bone_t(arm, origin, axis, "LowerLeg_" + side)
    # The thigh root sits inside the pelvis. Start the shell below that bury.
    jog_t0 = max(hip, 0.255)
    _mark_bevel(make_loft(
        pieces, body, "Lab_JogU%s%s" % (tag, side), "UpperLeg_" + side, "Mesh_UpperLeg_" + side, "cloth",
        origin, axis, jog_t0, knee_t - knee, "jog", cloth,
    ))
    keep = opening_direction(knee_row["bend"], knee_row["child_axis"], knee_row["rot_sign"])
    _add_pad(
        pieces, body, "Lab_JogKnee%s%s" % (tag, side),
        "UpperLeg_" + side, "Mesh_UpperLeg_" + side,
        origin, axis, knee_t - knee + 0.012, knee_t - 0.018, cloth, keep,
    )
    keep = opening_direction(hip_row["bend"], hip_row["child_axis"], hip_row["rot_sign"])
    _add_pad(
        pieces, body, "Lab_JogHip%s%s" % (tag, side),
        "UpperLeg_" + side, "Mesh_UpperLeg_" + side,
        origin, axis, 0.205, 0.242, cloth, keep,
    )

    origin, axis, _lo, _hi = _span(arm, body, "LowerLeg_" + side, "Mesh_LowerLeg_" + side)
    ankle_t = _bone_t(arm, origin, axis, "Foot_" + side)
    _mark_bevel(make_loft(
        pieces, body, "Lab_JogL%s%s" % (tag, side), "LowerLeg_" + side, "Mesh_LowerLeg_" + side, "cloth",
        origin, axis, knee, ankle_t - ankle - 0.022, "jog", cloth,
    ))
    keep = opening_direction(ankle_row["bend"], ankle_row["child_axis"], ankle_row["rot_sign"])
    _add_pad(
        pieces, body, "Lab_JogAnkle%s%s" % (tag, side),
        "LowerLeg_" + side, "Mesh_LowerLeg_" + side,
        origin, axis, ankle_t - ankle - 0.008, ankle_t - 0.036, cloth, keep,
    )

    origin, axis, _lo, _hi = _span(arm, body, "Foot_" + side, "Mesh_Foot_" + side)
    toe_t = _tail_t(arm, origin, axis, "Foot_" + side)
    shoe = make_loft(
        pieces, body, outfit["shoe_name"] + side, "Foot_" + side, "Mesh_Foot_" + side, "cloth",
        origin, axis, ankle + 0.015, toe_t - 0.006, "shoe", SHOE_OFFSET,
    )
    close_top(shoe, origin + axis * (toe_t + 0.006))


def clear_neighbours(pieces, body):
    """Drop samples that sit too close to a plate on another bone."""
    by_name = {piece.name: piece for piece in pieces}
    for name, meshes, margin in CLEAR_ROWS:
        piece = by_name.get(name)
        if piece is None:
            continue
        cut = 0
        nearest = None
        for chain in piece.chains:
            for ring in chain:
                for si, point in enumerate(ring):
                    if point is None:
                        continue
                    gap = None
                    for mesh in meshes:
                        hit = gap_to_one(body[mesh], point)
                        if hit is None:
                            continue
                        if gap is None or hit[0] < gap:
                            gap = hit[0]
                    if gap is None:
                        continue
                    if nearest is None or gap < nearest:
                        nearest = gap
                    if gap < margin:
                        ring[si] = None
                        cut += 1
        log(
            "CLEAR %s cut=%d nearest-cm=%s"
            % (name, cut, "none" if nearest is None else "%.2f" % (nearest * 100.0))
        )


def catalog(arm, body):
    pieces = []
    x_axis = Vector((1.0, 0.0, 0.0))
    y_axis = Vector((0.0, 1.0, 0.0))
    z_axis = Vector((0.0, 0.0, 1.0))

    def add_loft(name, bone, own, kind, origin, axis, t0, t1, key, offset, mask=None):
        n_st, n_side = DENSITY[key]
        piece = Piece(name, bone, own, kind)
        piece.loop = True
        piece.offset = offset
        pts = body[own]["world"]
        loft_piece(piece, pts, origin, axis, t0, t1, n_st, n_side, offset, body, mask)
        pieces.append(piece)
        return piece

    # Street shells and Bram's own jacket shells. Data lives in OUTFITS.
    build_outfits(pieces, arm, body)

    # Head wear. Face is -Y. A wide opening reads as a hood, a narrow one as a helmet.
    hpts = body["Mesh_Head"]["world"]
    h_center = sum(hpts, Vector()) / len(hpts)

    def face_mask(limit):
        def mask(direction, _center):
            return direction.dot(Vector((0.0, -1.0, 0.0))) < limit
        return mask

    hz0, hz1 = project_span(hpts, Vector((0.0, 0.0, 0.0)), z_axis)
    hood = add_loft(
        "Lab_Hood", "Head", "Mesh_Head", "cloth",
        Vector((0.0, 0.0, 0.0)), z_axis, hz0 + (hz1 - hz0) * 0.08, hz1 + 0.006,
        "hood", HOODIE_OFFSET, face_mask(0.22),
    )
    close_top(hood, Vector((h_center.x, h_center.y, hz1 + HOODIE_OFFSET + 0.004)))
    helmet = add_loft(
        "Lab_Helmet", "Head", "Mesh_Head", "cloth",
        Vector((0.0, 0.0, 0.0)), z_axis, hz0 + (hz1 - hz0) * 0.10, hz1 + 0.004,
        "helmet", HOODIE_OFFSET, face_mask(0.45),
    )
    close_top(helmet, Vector((h_center.x, h_center.y, hz1 + HOODIE_OFFSET + 0.002)))
    cap = add_loft(
        "Lab_Cap", "Head", "Mesh_Head", "cloth",
        Vector((0.0, 0.0, 0.0)), z_axis, h_center.z + 0.01, hz1 - 0.002,
        "cap", 0.007, None,
    )
    close_top(cap, Vector((h_center.x, h_center.y, hz1 + 0.008)))
    # Bram's beanie. A tighter crown than the cap, no brim, so the outline is his.
    beanie = add_loft(
        "Lab_CapBeanie", "Head", "Mesh_Head", "cloth",
        Vector((0.0, 0.0, 0.0)), z_axis, hz0 + (hz1 - hz0) * 0.42, hz1 + 0.005,
        "cap", 0.008, face_mask(0.02),
    )
    close_top(beanie, Vector((h_center.x, h_center.y, hz1 + 0.010)))

    # Hood-down collar, on the neck, clear of the jaw and the hoodie hem.
    npts = body["Mesh_Neck"]["world"]
    n0, n1 = project_span(npts, Vector((0.0, 0.0, 0.0)), z_axis)
    add_loft(
        "Lab_Collar", "Neck", "Mesh_Neck", "cloth",
        Vector((0.0, 0.0, 0.0)), z_axis, n0 + (n1 - n0) * 0.04, n0 + (n1 - n0) * 0.96,
        "collar", 0.008, None,
    )
    add_loft(
        "Lab_CollarBram", "Neck", "Mesh_Neck", "cloth",
        Vector((0.0, 0.0, 0.0)), z_axis, n0 + (n1 - n0) * 0.02, n1 + 0.012,
        "collar", 0.009, None,
    )

    # Brim: a thin plate in front of the brow, outside the nose.
    brim = Piece("Lab_Brim", "Head", "Mesh_Head", "accessory")
    brim.loop = False
    brow_z = h_center.z + 0.045
    nose_y = min(p.y for p in hpts)
    y_root = nose_y - 0.012
    y_tip = nose_y - 0.075
    z_mid = brow_z
    # Plate thickness 0.4 cm, width 11 cm.
    brim_chain = []
    for iz, zoff in enumerate((-0.002, 0.002)):
        row = []
        # rows along width, stored as rings along depth so the grid quads.
        brim_chain.append([])
    # Build as rows along X (width), stations along Y (depth).
    n_w, n_d = 10, 6
    chain = []
    for id_ in range(n_d):
        fy = y_root + (y_tip - y_root) * id_ / (n_d - 1)
        row = []
        for iw in range(n_w):
            fx = -0.055 + 0.110 * iw / (n_w - 1)
            row.append(Vector((fx, fy, z_mid)))
        chain.append(row)
    # Thickness: a second sheet. Separate chain would not connect. Interleave
    # by making each ring a loop around the plate cross-section (thin tube).
    brim.chains = []
    brim.loop = True
    tube_chain = []
    for id_ in range(n_d):
        fy = y_root + (y_tip - y_root) * id_ / (n_d - 1)
        ring = []
        for iw in range(n_w):
            fx = -0.055 + 0.110 * iw / (n_w - 1)
            ring.append(Vector((fx, fy, z_mid + 0.002)))
        for iw in range(n_w - 1, -1, -1):
            fx = -0.055 + 0.110 * iw / (n_w - 1)
            ring.append(Vector((fx, fy, z_mid - 0.002)))
        tube_chain.append(ring)
    brim.chains.append(tube_chain)
    # Box volume matches the plate. Shrink by 0.5 mm so the surface is the boundary.
    brim.boxes.append((
        Vector((0.0, (y_root + y_tip) * 0.5, z_mid)),
        x_axis, y_axis, z_axis,
        0.054, abs(y_tip - y_root) * 0.5 - 0.001, 0.0015,
    ))
    pieces.append(brim)

    # Visor, shorter than the brim, helmet-only.
    visor = Piece("Lab_Visor", "Head", "Mesh_Head", "accessory")
    visor.loop = True
    y0 = nose_y - 0.006
    y1 = nose_y - 0.038
    vz = h_center.z + 0.01
    vchain = []
    for id_ in range(4):
        fy = y0 + (y1 - y0) * id_ / 3.0
        ring = []
        for iw in range(8):
            fx = -0.04 + 0.08 * iw / 7.0
            ring.append(Vector((fx, fy, vz + 0.0015)))
        for iw in range(7, -1, -1):
            fx = -0.04 + 0.08 * iw / 7.0
            ring.append(Vector((fx, fy, vz - 0.0015)))
        vchain.append(ring)
    visor.chains.append(vchain)
    visor.boxes.append((
        Vector((0.0, (y0 + y1) * 0.5, vz)),
        x_axis, y_axis, z_axis,
        0.039, abs(y1 - y0) * 0.5 - 0.001, 0.0012,
    ))
    pieces.append(visor)

    # Hair strips on the back of the crown. Accessory length, root in the cloth band.
    hair = Piece("Lab_Hair", "Head", "Mesh_Head", "accessory")
    hair.loop = True
    directions = (
        Vector((0.00, 0.25, 0.95)),
        Vector((0.28, 0.35, 0.85)),
        Vector((-0.28, 0.35, 0.85)),
        Vector((0.12, 0.55, 0.70)),
        Vector((-0.12, 0.55, 0.70)),
    )
    for direction in directions:
        direction = direction.normalized()
        # Walk out from the head centre to the surface.
        surf = h_center
        step = 0.004
        for _ in range(40):
            surf = surf + direction * step
            hit = gap_to_one(body["Mesh_Head"], surf)
            if hit is not None and hit[0] > 0.0:
                break
        root = surf + direction * 0.004
        chain = []
        n_st, n_side = 8, 6
        for i in range(n_st):
            u = i / (n_st - 1)
            center = root + direction * (0.055 * u)
            radius = 0.007 * (1.0 - u) + 0.002
            _ax, bx, by = basis_from(direction)
            ring = []
            for s in range(n_side):
                ang = math.tau * s / n_side
                ring.append(center + (bx * math.cos(ang) + by * math.sin(ang)) * radius)
            chain.append(ring)
        hair.chains.append(chain)
    pieces.append(hair)

    # Backpack, inner face just off the hoodie, outer face a few centimetres out.
    chest = body["Mesh_Chest"]["world"]
    back_y = max(p.y for p in chest)
    inner = back_y + 0.012
    depth = 0.045
    pack = Piece("Lab_Pack", "Chest", "Mesh_Chest", "accessory")
    pack.loop = False
    center = Vector((0.0, inner + depth * 0.5, 1.27))
    add_box(pack, center, x_axis, y_axis, z_axis, 0.070, depth * 0.5, 0.085, 5, 4)
    pieces.append(pack)
    bram_pack = Piece("Lab_PackBram", "Chest", "Mesh_Chest", "accessory")
    bram_pack.loop = False
    bram_center = Vector((0.0, inner + 0.055, 1.22))
    add_box(bram_pack, bram_center, x_axis, y_axis, z_axis, 0.090, 0.028, 0.070, 5, 4)
    pieces.append(bram_pack)

    # Kangaroo pocket just outside the hoodie shell, still inside the 1 cm band.
    front_y = min(p.y for p in chest)
    pocket = Piece("Lab_Pocket", "Chest", "Mesh_Chest", "cloth")
    pocket.loop = False
    p_center = Vector((0.0, front_y - 0.0085, 1.24))
    add_box(pocket, p_center, x_axis, y_axis, z_axis, 0.055, 0.0012, 0.045, 8, 5)
    pieces.append(pocket)

    # Hood crown roll so the hood reads against the helmet. Outer skin is past 1 cm.
    roll = Piece("Lab_HoodRoll", "Head", "Mesh_Head", "accessory")
    roll.loop = True
    minor = 0.004
    n_major, n_minor = 16, 8
    # Sit the roll outside the hood shell so the two head pieces do not share a volume.
    chain = []
    head_info = body["Mesh_Head"]
    for i in range(n_major):
        ang = math.tau * i / n_major
        direction = Vector((math.sin(ang), math.cos(ang), 0.35)).normalized()
        if direction.y < -0.05:
            chain.append([None] * n_minor)
            continue
        surface = surface_along(head_info, h_center, direction)
        if surface is None:
            chain.append([None] * n_minor)
            continue
        center = surface + direction * (0.016 + minor)
        ring = []
        for s in range(n_minor):
            a2 = math.tau * s / n_minor
            radial = direction * math.cos(a2) + z_axis * math.sin(a2)
            if radial.length < 1e-6:
                radial = Vector((1.0, 0.0, 0.0))
            radial.normalize()
            ring.append(center + radial * minor)
        chain.append(ring)
    roll.chains.append(chain)
    pieces.append(roll)

    # A raised back seam and two drawstrings push the long outfit into the tri budget
    # without adding a new silhouette that can snag.
    seam = Piece("Lab_Seam", "Chest", "Mesh_Chest", "cloth")
    seam.loop = True
    seam_chain = []
    n_st, n_side = 16, 8
    for i in range(n_st):
        z = 1.19 + (1.36 - 1.19) * i / (n_st - 1)
        center = Vector((0.0, back_y + 0.008, z))
        ring = []
        for s in range(n_side):
            ang = math.tau * s / n_side
            ring.append(center + Vector((math.cos(ang) * 0.0022, math.sin(ang) * 0.0016, 0.0)))
        seam_chain.append(ring)
    seam.chains.append(seam_chain)
    pieces.append(seam)

    for which, x in (("L", -0.025), ("R", 0.025)):
        cord = Piece("Lab_Cord" + which, "Chest", "Mesh_Chest", "cloth")
        cord.loop = True
        chain = []
        for i in range(12):
            z = 1.30 - i * 0.010
            y = front_y - 0.008
            center = Vector((x, y, z))
            ring = []
            for s in range(6):
                ang = math.tau * s / 6.0
                ring.append(center + Vector((math.cos(ang) * 0.0018, 0.0, math.sin(ang) * 0.0018)))
            chain.append(ring)
        cord.chains.append(chain)
        pieces.append(cord)

    for piece in pieces:
        # No length subdivision. Doubling every ring pushed a long outfit past 15000 triangles.
        moved, cut = enforce_band(piece, body)
        log("BUILT %s verts=%d moved=%d cut=%d" % (piece.name, piece.vert_count(), moved, cut))
    trim_overlaps(arm, pieces, body)
    for piece in pieces:
        if getattr(piece, "bevel", False):
            bevel_cuffs(piece, body)
    clear_neighbours(pieces, body)
    return pieces


def trim_overlaps(arm, pieces, body):
    """Move samples out of any body plate other than their own. Drop what will not seat."""
    _ = arm
    cut = 0
    moved = 0
    for piece in pieces:
        own = body[piece.own]
        for chain in piece.chains:
            for ring in chain:
                for si, rest in enumerate(ring):
                    if rest is None:
                        continue
                    depth, _where, normal = visual_penetration(body, rest, ignore=piece.own)
                    if depth <= 0.004:
                        continue
                    placed = rest + normal * (depth + 0.005)
                    gap = gap_to_one(own, placed)
                    if gap is not None and BAND_MIN - 1e-4 <= gap[0] <= BAND_MAX:
                        ring[si] = placed
                        moved += 1
                    else:
                        ring[si] = None
                        cut += 1
    log("TRIM moved=%d cut=%d" % (moved, cut))
    return cut


def subdivide_length(piece):
    doubled = []
    for chain in piece.chains:
        if len(chain) < 2:
            doubled.append(chain)
            continue
        out = []
        for index, ring in enumerate(chain):
            out.append(ring)
            if index == len(chain) - 1:
                break
            nxt = chain[index + 1]
            mid = []
            for a, b in zip(ring, nxt):
                if a is None or b is None:
                    mid.append(None)
                else:
                    mid.append((a + b) * 0.5)
            out.append(mid)
        doubled.append(out)
    piece.chains = doubled


def tri_of(piece):
    if piece.obj is None or piece.obj.data is None:
        return 0
    piece.obj.data.calc_loop_triangles()
    return len(piece.obj.data.loop_triangles)


def id_for(who, index, label):
    """Underscore id so a license row can name the loadout exactly."""
    parts = [who, str(index)] + [part.capitalize() for part in label.split("-")]
    return "_".join(parts)


COVER_LONG = [
    "Mesh_Chest",
    "Mesh_Shoulder_L", "Mesh_Shoulder_R",
    "Mesh_UpperArm_L", "Mesh_UpperArm_R", "Mesh_LowerArm_L", "Mesh_LowerArm_R",
    "Mesh_UpperLeg_L", "Mesh_UpperLeg_R", "Mesh_LowerLeg_L", "Mesh_LowerLeg_R",
    "Mesh_Foot_L", "Mesh_Foot_R",
]
COVER_CROP = [
    "Mesh_Chest",
    "Mesh_Shoulder_L", "Mesh_Shoulder_R",
    "Mesh_UpperArm_L", "Mesh_UpperArm_R",
    "Mesh_UpperLeg_L", "Mesh_UpperLeg_R",
    "Mesh_Foot_L", "Mesh_Foot_R",
]


def _pad_names(tag):
    names = []
    for token in ("SleeveElbow", "JogKnee", "JogHip", "JogAnkle"):
        for side in ("L", "R"):
            names.append("Lab_%s%s%s" % (token, tag, side))
    return names


def loadout_map():
    pads = _pad_names("")
    long_core = [
        "Lab_HoodieChest", "Lab_HoodieSpine", "Lab_HoodieHips", "Lab_Seam", "Lab_Pocket",
        "Lab_SleeveShL", "Lab_SleeveShR",
        "Lab_SleeveUL", "Lab_SleeveUR", "Lab_SleeveLL", "Lab_SleeveLR",
        "Lab_SleeveCuffL", "Lab_SleeveCuffR",
        "Lab_JogUL", "Lab_JogUR", "Lab_JogLL", "Lab_JogLR",
        "Lab_ShoeL", "Lab_ShoeR",
    ] + pads
    crop_core = [
        "Lab_HoodieChest", "Lab_HoodieSpine", "Lab_HoodieHips", "Lab_Seam", "Lab_Pocket",
        "Lab_SleeveShL", "Lab_SleeveShR",
        "Lab_SleeveUL", "Lab_SleeveUR",
        "Lab_JogUL", "Lab_JogUR",
        "Lab_ShoeL", "Lab_ShoeR",
    ] + pads
    bram_core = [
        "Lab_HoodieBramChest", "Lab_HoodieBramSpine", "Lab_HoodieBramHips",
        "Lab_SleeveBramShL", "Lab_SleeveBramShR",
        "Lab_SleeveUBramL", "Lab_SleeveUBramR", "Lab_SleeveLBramL", "Lab_SleeveLBramR",
        "Lab_SleeveBramCuffL", "Lab_SleeveBramCuffR",
        "Lab_JogUBramL", "Lab_JogUBramR", "Lab_JogLBramL", "Lab_JogLBramR",
        "Lab_ShoeBramL", "Lab_ShoeBramR",
        "Lab_CapBeanie",
    ] + _pad_names("Bram")
    specs = [
        ("Reed", 1, "hood", long_core + ["Lab_Hood", "Lab_HoodRoll"]),
        ("Reed", 2, "cap", long_core + ["Lab_Cap", "Lab_Brim", "Lab_Pack"]),
        ("Reed", 3, "helmet", long_core + ["Lab_Helmet", "Lab_Visor"]),
        ("Bram", 1, "beanie", bram_core),
        ("Bram", 2, "jacket", bram_core + ["Lab_PackBram"]),
        ("Bram", 3, "collar", bram_core + ["Lab_CollarBram"]),
        ("Pip", 1, "cap", crop_core + ["Lab_Cap", "Lab_Brim", "Lab_Pack"]),
        ("Pip", 2, "hair", crop_core + ["Lab_Hair"]),
        ("Pip", 3, "helmet", crop_core + ["Lab_Helmet", "Lab_Visor"]),
        ("Sol", 1, "collar-cap", long_core + ["Lab_Collar", "Lab_Cap", "Lab_Brim"]),
        ("Sol", 2, "hood", long_core + ["Lab_Hood", "Lab_HoodRoll"]),
        ("Sol", 3, "helmet-pack", long_core + ["Lab_Helmet", "Lab_Visor", "Lab_Pack"]),
    ]
    sets = []
    for who, index, label, names in specs:
        # Pip's crop is bare forearms and shins. The waist still wears the hip shell.
        # Pip's crop is bare forearms and shins. The waist still wears the hip shell.
        hide = list(COVER_CROP if who == "Pip" else COVER_LONG)
        sets.append({
            "id": id_for(who, index, label),
            "character": who,
            "variant": index,
            "label": label,
            "color": list(PLAYER[who]),
            "pieces": names,
            "hide": hide,
        })
    return sets


def write_loadouts(sets, counts):
    os.makedirs(OUT_DIR, exist_ok=True)
    blob = {"sets": sets, "tris": counts, "colors": {k: list(v) for k, v in PLAYER.items()}}
    with open(LOADOUTS, "w", encoding="utf-8") as handle:
        json.dump(blob, handle, indent=2)
        handle.write("\n")


def bone_matrix(arm, bone_name):
    pb = arm.pose.bones[bone_name]
    return arm.matrix_world @ pb.matrix @ pb.bone.matrix_local.inverted()


def posed_point(arm, bone_name, rest):
    return bone_matrix(arm, bone_name) @ rest


def verify_skin(arm, pieces):
    """One bent elbow. Analytic skinning has to match the depsgraph."""
    bone = arm.pose.bones["LowerArm_L"]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = (math.radians(40.0), 0.0, 0.0)
    bpy.context.view_layer.update()
    piece = next(p for p in pieces if p.name == "Lab_SleeveLL" and p.vert_count() > 0)
    rest = None
    for ring in piece.chains[0]:
        for vert in ring:
            if vert is not None:
                rest = vert
                break
        if rest is not None:
            break
    if rest is None:
        raise SystemExit("lower sleeve has no vertices")
    analytic = posed_point(arm, piece.bone, rest)
    deps = bpy.context.evaluated_depsgraph_get()
    ev = piece.obj.evaluated_get(deps)
    me = ev.to_mesh()
    # Find the evaluated vert nearest the analytic pose.
    mw = ev.matrix_world
    best = 1e9
    for v in me.vertices:
        d = (mw @ v.co - analytic).length
        if d < best:
            best = d
    ev.to_mesh_clear()
    bone.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    log("SKIN err-cm %.3f" % (best * 100.0))
    if best > 0.15:
        raise SystemExit("armature skinning does not match the bone matrix")


def depth_cm(meters):
    return round(meters * 100.0 + 1e-9, 2)


def ray_radius(samples, ang):
    n = len(samples)
    if n < 2:
        return None
    for i in range(n):
        a0 = samples[i][0]
        a1 = samples[(i + 1) % n][0]
        span = (a1 - a0) % math.tau
        if span > math.radians(45.0):
            continue
        rel = (ang - a0) % math.tau
        if rel > span + 1e-6:
            continue
        x0, y0 = samples[i][1], samples[i][2]
        x1, y1 = samples[(i + 1) % n][1], samples[(i + 1) % n][2]
        dx = math.cos(ang)
        dy = math.sin(ang)
        ex = x1 - x0
        ey = y1 - y0
        den = dx * ey - dy * ex
        if abs(den) < 1e-8:
            return math.hypot(x0, y0)
        t = (x0 * ey - y0 * ex) / den
        if t < 0.0:
            return None
        return t
    return None


def loft_depth(point, ring0, ring1):
    pts0 = [p for p in ring0 if p is not None]
    pts1 = [p for p in ring1 if p is not None]
    if len(pts0) < 2 or len(pts1) < 2:
        return 0.0
    c0 = sum(pts0, Vector()) / len(pts0)
    c1 = sum(pts1, Vector()) / len(pts1)
    axis = c1 - c0
    length = axis.length
    if length < 1e-5:
        return 0.0
    direction = axis / length
    t = (point - c0).dot(direction)
    if t < -0.004 or t > length + 0.004:
        return 0.0
    u = 0.0 if t < 0.0 else (1.0 if t > length else t / length)
    bx = direction.cross(Vector((0.0, 0.0, 1.0)))
    if bx.length < 1e-4:
        bx = direction.cross(Vector((0.0, 1.0, 0.0)))
    bx.normalize()
    by = direction.cross(bx).normalized()
    center = c0.lerp(c1, u)
    rel = point - center
    rel = rel - direction * rel.dot(direction)
    dist = rel.length
    samples = []
    for p, q in zip(ring0, ring1):
        if p is None or q is None:
            continue
        s = p.lerp(q, u)
        r = s - center
        samples.append((math.atan2(r.dot(by), r.dot(bx)), r.dot(bx), r.dot(by)))
    if len(samples) < 2:
        return 0.0
    samples.sort(key=lambda item: item[0])
    if dist < 1e-6:
        rads = []
        for i in range(len(samples)):
            rad = ray_radius(samples, samples[i][0])
            if rad is not None:
                rads.append(rad)
        if not rads:
            return 0.0
        return min(rads)
    ang = math.atan2(rel.dot(by), rel.dot(bx))
    rad = ray_radius(samples, ang)
    if rad is None or dist >= rad:
        return 0.0
    depth = rad - dist
    # A loft is a hollow shell. A sample in the middle of the tube is wearing
    # the garment. Only the band next to the wall is a real crossing.
    if depth > 0.012:
        return 0.0
    return depth


def box_depth(point, box, matrix):
    center, ax, ay, az, hx, hy, hz = box
    center_w = matrix @ center
    rot = matrix.to_3x3()
    axes = ((rot @ ax).normalized(), (rot @ ay).normalized(), (rot @ az).normalized())
    halves = (hx, hy, hz)
    rel = point - center_w
    deltas = []
    for axis, half in zip(axes, halves):
        coord = abs(rel.dot(axis))
        if coord > half:
            return 0.0
        deltas.append(half - coord)
    return min(deltas)


def pose_chains(piece, matrix):
    posed = []
    for chain in piece.chains:
        posed_chain = []
        for ring in chain:
            posed_chain.append([None if p is None else matrix @ p for p in ring])
        posed.append(posed_chain)
    return posed


def segments_for(posed_chains):
    found = []
    for chain in posed_chains:
        for index in range(len(chain) - 1):
            lo = [1e9, 1e9, 1e9]
            hi = [-1e9, -1e9, -1e9]
            count = 0
            for ring in (chain[index], chain[index + 1]):
                for vert in ring:
                    if vert is None:
                        continue
                    count += 1
                    lo[0] = min(lo[0], vert.x)
                    lo[1] = min(lo[1], vert.y)
                    lo[2] = min(lo[2], vert.z)
                    hi[0] = max(hi[0], vert.x)
                    hi[1] = max(hi[1], vert.y)
                    hi[2] = max(hi[2], vert.z)
            if count >= 3:
                found.append(((lo, hi), chain[index], chain[index + 1]))
    return found


def piece_volume_depth(point, segments, boxes, matrix):
    worst = 0.0
    for bounds, ring0, ring1 in segments:
        if not point_in_bounds(point, bounds, 0.003):
            continue
        depth = loft_depth(point, ring0, ring1)
        if depth > worst:
            worst = depth
    for box in boxes:
        depth = box_depth(point, box, matrix)
        if depth > worst:
            worst = depth
    return worst


def iter_verts(piece):
    for ci, chain in enumerate(piece.chains):
        for ri, ring in enumerate(chain):
            for si, vert in enumerate(ring):
                if vert is not None:
                    yield ci, ri, si, vert


def measure(arm, body, pieces, sets, clips):
    """Pose every 30 fps frame. Returns the fit line and per-vertex pushes."""
    by_name = {piece.name: piece for piece in pieces}
    bones = sorted({piece.bone for piece in pieces})
    # Rest same-bone pairs are rigid. Check them once.
    rest_pair = {}
    for i, a in enumerate(pieces):
        for b in pieces[i + 1:]:
            if a.bone != b.bone:
                continue
            rest_pair[(a.name, b.name)] = pair_depth(a, b, bone_matrix(arm, a.bone))
    for pair, depth in sorted(rest_pair.items(), key=lambda item: -item[1]):
        if depth_cm(depth) >= 0.40:
            log("REST-PAIR %s %s %.2f" % (pair[0], pair[1], depth_cm(depth)))
    log("REST-PAIRS %d worst-cm %.2f" % (
        len(rest_pair),
        max([0.0] + [depth_cm(v) for v in rest_pair.values()]),
    ))
    if os.environ.get("COSTUME_RESTONLY") == "1":
        return {
            "frames": 0, "fails": 0, "world": max(rest_pair.values()) if rest_pair else 0.0,
            "pushes": {}, "worst": "rest", "band_close": 0,
            "rest_pair": max(rest_pair.values()) if rest_pair else 0.0,
        }

    pushes = {}
    world = 0.0
    frames = 0
    fails = 0
    band_close = 0
    worst_desc = ""
    by_clip = []
    t0 = time.time()
    for clip_name, show, ctx, times in clips:
        clip_world = 0.0
        clip_fails = 0
        clip_frames = 0
        log("CLIP %s frames %d" % (clip_name, len(times)))
        clip_body = 0.0
        clip_body_name = ""
        for index, age in enumerate(times):
            show(arm, age, ctx)
            bpy.context.view_layer.update()
            refresh_body(body)
            mats = {name: bone_matrix(arm, name) for name in bones}
            posed = {}
            body_depth = {}
            for piece in pieces:
                matrix = mats[piece.bone]
                posed[piece.name] = pose_chains(piece, matrix)
                worst = 0.0
                where = ""
                for ci, ri, si, rest in iter_verts(piece):
                    world_v = matrix @ rest
                    depth, where_name, normal = visual_penetration(body, world_v)
                    gap = -depth
                    if depth > worst:
                        worst = depth
                        where = where_name
                    # Cut only what is inside past the 0.5 cm limit. Moving a vert
                    # off one bone shoves it into another.
                    if gap < -0.004:
                        key = (piece.name, ci, ri, si)
                        score = depth
                        prev = pushes.get(key)
                        if prev is None or score > prev[0]:
                            pushes[key] = (score, matrix.copy(), world_v.copy(), normal.copy(), gap, clip_name)
                body_depth[piece.name] = worst
                piece._hit_where = where
            bounds = {}
            segs = {}
            for piece in pieces:
                if piece.vert_count() == 0:
                    bounds[piece.name] = None
                    segs[piece.name] = []
                    continue
                bounds[piece.name] = bounds_of(posed[piece.name], piece.boxes, mats[piece.bone])
                segs[piece.name] = segments_for(posed[piece.name])
            cross = {}
            for i, a in enumerate(pieces):
                for b in pieces[i + 1:]:
                    if a.bone == b.bone:
                        continue
                    if a.vert_count() == 0 or b.vert_count() == 0:
                        continue
                    if not bounds_overlap(bounds[a.name], bounds[b.name]):
                        continue
                    depth = cross_depth(a, b, posed, mats, segs, bounds)
                    if depth > 0.0:
                        cross[(a.name, b.name)] = depth
                        record_cross_push(pushes, a, b, posed, mats, segs, clip_name)
            for spec in sets:
                names = spec["pieces"]
                present = [name for name in names if name in by_name and by_name[name].vert_count() > 0]
                local = 0.0
                for name in present:
                    if body_depth.get(name, 0.0) > local:
                        local = body_depth[name]
                name_set = set(present)
                for (a, b), depth in rest_pair.items():
                    if a in name_set and b in name_set and depth > local:
                        local = depth
                for (a, b), depth in cross.items():
                    if a in name_set and b in name_set and depth > local:
                        local = depth
                frames += 1
                clip_frames += 1
                if depth_cm(local) > PEN_CM:
                    fails += 1
                    clip_fails += 1
                if local > world:
                    world = local
                    worst_desc = "%s t=%.3f cm=%.2f" % (clip_name, age, depth_cm(local))
                if local > clip_world:
                    clip_world = local
            if body_depth:
                top_name, top_depth = max(body_depth.items(), key=lambda item: item[1])
                if top_depth > clip_body:
                    clip_body = top_depth
                    where = by_name[top_name]._hit_where if top_name in by_name else ""
                    clip_body_name = "%s->%s" % (top_name, where)
            if os.environ.get("COSTUME_DEBUG") == "1" and depth_cm(max([0.0] + list(body_depth.values()) + list(cross.values()))) > PEN_CM:
                top_c = max(cross.items(), key=lambda item: item[1]) if cross else (("none", "none"), 0.0)
                top_b = max(body_depth.items(), key=lambda item: item[1]) if body_depth else ("none", 0.0)
                if index % 5 == 0:
                    where = by_name[top_b[0]]._hit_where if top_b[0] in by_name else ""
                    log("DBG %s t=%.3f body %s->%s %.2f cross %s %s %.2f" % (
                        clip_name, age, top_b[0], where, depth_cm(top_b[1]), top_c[0][0], top_c[0][1], depth_cm(top_c[1]),
                    ))
            if index % 20 == 0:
                log("  %s %d/%d elapsed %.1f worst %s" % (clip_name, index, len(times), time.time() - t0, worst_desc))
        log(
            "CLIP-FIT %s worldMax=%.2f frames=%d fails=%d body=%s %.2f"
            % (
                clip_name, depth_cm(clip_world), clip_frames, clip_fails,
                clip_body_name, depth_cm(clip_body),
            )
        )
        by_clip.append((clip_name, clip_world, clip_frames, clip_fails))
    return {
        "frames": frames,
        "fails": fails,
        "world": world,
        "pushes": pushes,
        "worst": worst_desc,
        "band_close": band_close,
        "rest_pair": max(rest_pair.values()) if rest_pair else 0.0,
        "by_clip": by_clip,
    }


def pair_depth(a, b, matrix):
    posed_b = pose_chains(b, matrix)
    segs_b = segments_for(posed_b)
    worst = 0.0
    for _ci, _ri, _si, rest in iter_verts(a):
        depth = piece_volume_depth(matrix @ rest, segs_b, b.boxes, matrix)
        if depth > worst:
            worst = depth
    posed_a = pose_chains(a, matrix)
    segs_a = segments_for(posed_a)
    for _ci, _ri, _si, rest in iter_verts(b):
        depth = piece_volume_depth(matrix @ rest, segs_a, a.boxes, matrix)
        if depth > worst:
            worst = depth
    return worst


def bounds_of(posed_chains, boxes, matrix):
    lo = [1e9, 1e9, 1e9]
    hi = [-1e9, -1e9, -1e9]
    count = 0

    def take(point):
        nonlocal count
        count += 1
        lo[0] = min(lo[0], point.x)
        lo[1] = min(lo[1], point.y)
        lo[2] = min(lo[2], point.z)
        hi[0] = max(hi[0], point.x)
        hi[1] = max(hi[1], point.y)
        hi[2] = max(hi[2], point.z)

    for chain in posed_chains:
        for ring in chain:
            for vert in ring:
                if vert is not None:
                    take(vert)
    rot = matrix.to_3x3()
    for center, ax, ay, az, hx, hy, hz in boxes:
        origin = matrix @ center
        axes = (rot @ ax, rot @ ay, rot @ az)
        halves = (hx, hy, hz)
        for sx in (-1.0, 1.0):
            for sy in (-1.0, 1.0):
                for sz in (-1.0, 1.0):
                    corner = origin + axes[0] * (hx * sx) + axes[1] * (hy * sy) + axes[2] * (hz * sz)
                    take(corner)
    if count == 0:
        return None
    return lo, hi


def bounds_overlap(a, b, pad=0.008):
    if a is None or b is None:
        return False
    for i in range(3):
        if a[1][i] + pad < b[0][i] or b[1][i] + pad < a[0][i]:
            return False
    return True


def point_in_bounds(point, bounds, pad=0.004):
    if bounds is None:
        return False
    return (
        bounds[0][0] - pad <= point.x <= bounds[1][0] + pad
        and bounds[0][1] - pad <= point.y <= bounds[1][1] + pad
        and bounds[0][2] - pad <= point.z <= bounds[1][2] + pad
    )


def cross_depth(a, b, posed, mats, segs, bounds):
    worst = 0.0
    bb = bounds[b.name]
    mb = mats[b.bone]
    for chain in posed[a.name]:
        for ring in chain:
            for vert in ring:
                if vert is None or not point_in_bounds(vert, bb, 0.004):
                    continue
                depth = piece_volume_depth(vert, segs[b.name], b.boxes, mb)
                if depth > worst:
                    worst = depth
    ba = bounds[a.name]
    ma = mats[a.bone]
    for chain in posed[b.name]:
        for ring in chain:
            for vert in ring:
                if vert is None or not point_in_bounds(vert, ba, 0.004):
                    continue
                depth = piece_volume_depth(vert, segs[a.name], a.boxes, ma)
                if depth > worst:
                    worst = depth
    return worst


def record_cross_push(pushes, a, b, posed, mats, segs, clip_name):
    _push_side(pushes, a, b, posed, mats, segs, clip_name)
    _push_side(pushes, b, a, posed, mats, segs, clip_name)


def _push_side(pushes, src, dst, posed, mats, segs, clip_name):
    matrix = mats[src.bone]
    dst_m = mats[dst.bone]
    dst_bounds = None
    for ci, chain in enumerate(src.chains):
        for ri, ring in enumerate(chain):
            for si, rest in enumerate(ring):
                if rest is None:
                    continue
                world_v = posed[src.name][ci][ri][si]
                depth = piece_volume_depth(world_v, segs[dst.name], dst.boxes, dst_m)
                if depth <= 0.0002:
                    continue
                key = (src.name, ci, ri, si)
                score = depth
                prev = pushes.get(key)
                if prev is not None and score <= prev[0]:
                    continue
                # Radial push away from the destination bone origin in world space.
                origin = dst_m.to_translation()
                normal = world_v - origin
                if normal.length < 1e-4:
                    normal = Vector((0.0, 0.0, 1.0))
                else:
                    normal.normalize()
                pushes[key] = (score, matrix.copy(), world_v.copy(), normal.copy(), -depth, clip_name)


def apply_pushes(pieces, body, pushes):
    by_name = {piece.name: piece for piece in pieces}
    moved = 0
    cut = 0
    for (name, ci, ri, si), (score, matrix, world_v, normal, gap, _clip) in pushes.items():
        piece = by_name[name]
        ring = piece.chains[ci][ri]
        if ring[si] is None:
            continue
        # gap is negative when the sample was inside.
        if gap < -0.004:
            ring[si] = None
            cut += 1
    return moved, cut


def cloth_band_stats(pieces, body):
    mins = []
    maxs = []
    over = 0
    under = 0
    for piece in pieces:
        if piece.kind != "cloth":
            continue
        own = body[piece.own]
        for _c, _r, _s, vert in iter_verts(piece):
            hit = gap_to_one(own, vert)
            if hit is None:
                continue
            mins.append(hit[0])
            maxs.append(hit[0])
            if hit[0] < BAND_MIN:
                under += 1
            if hit[0] > BAND_MAX:
                over += 1
    if not mins:
        return 0.0, 0.0, 0, 0
    return min(mins), max(mins), under, over


def accessory_span(pieces, body):
    worst = 0.0
    name = ""
    for piece in pieces:
        if piece.kind != "accessory":
            continue
        own = body[piece.own]
        for _c, _r, _s, vert in iter_verts(piece):
            hit = gap_to_one(own, vert)
            if hit is not None and hit[0] > worst:
                worst = hit[0]
                name = piece.name
    return worst, name


def save_blend():
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)


def log_body_height(arm, body):
    """Sole-to-crown of the shipped meshes. The armature scale stays 1."""
    zs = [p.z for info in body.values() for p in info["world"]]
    head = body["Mesh_Head"]["world"]
    foot = body["Mesh_Foot_L"]["world"]
    sole = min(zs)
    crown = max(zs)
    log(
        "HEIGHT armature-scale %.3f %.3f %.3f sole-z %.4f crown-z %.4f span-m %.4f head-z %.4f..%.4f foot-z %.4f..%.4f"
        % (
            arm.scale.x, arm.scale.y, arm.scale.z,
            sole, crown, crown - sole,
            min(p.z for p in head), max(p.z for p in head),
            min(p.z for p in foot), max(p.z for p in foot),
        )
    )


def build_all():
    arm = import_hier()
    tint_mannequin()
    body = capture_body()
    log_body_height(arm, body)
    probe_body(body)
    pieces = catalog(arm, body)
    player = make_material("LabPlayer", PLAYER["Reed"], True)
    jogger = make_material("LabJogger", (0.14, 0.14, 0.16), False)
    shoe = make_material("LabShoe", SHOE_COLOR, False)
    pack = make_material("LabPack", (0.12, 0.12, 0.13), False)
    trim = make_material("LabTrim", TRIM, False)
    jog_names = {"Lab_JogUL", "Lab_JogUR", "Lab_JogLL", "Lab_JogLR"}
    shoe_names = {"Lab_ShoeL", "Lab_ShoeR"}
    pack_names = {"Lab_Pack"}
    trim_names = {"Lab_SleeveCuffL", "Lab_SleeveCuffR", "Lab_Collar"}
    counts = {}
    for piece in pieces:
        if piece.name in jog_names:
            mat = jogger
        elif piece.name in shoe_names:
            mat = shoe
        elif piece.name in pack_names:
            mat = pack
        elif piece.name in trim_names:
            mat = trim
        else:
            mat = player
        tris = realize(piece, arm, mat)
        counts[piece.name] = tris
        log("TRIS %s %d" % (piece.name, tris))
    sets = loadout_map()
    worn = {}
    for spec in sets:
        total = sum(counts.get(name, 0) for name in spec["pieces"])
        worn[spec["id"]] = total
        log("WORN %s tris=%d" % (spec["id"], total))
    write_loadouts(sets, {"pieces": counts, "worn": worn})
    bmin, bmax, under, over = cloth_band_stats(pieces, body)
    acc, acc_name = accessory_span(pieces, body)
    log("CLOTH-BAND min-cm %.2f max-cm %.2f under=%d over=%d" % (bmin * 100.0, bmax * 100.0, under, over))
    log("ACCESSORY-MAX cm %.2f %s" % (acc * 100.0, acc_name))
    verify_skin(arm, pieces)
    return arm, body, pieces, sets


def make_clips(arm):
    os.environ["NOCLIP_SETTLE"] = "runner"
    os.environ["PASS18_RENDER"] = "0"
    import importlib.util
    spec = importlib.util.spec_from_file_location(
        "pass18_clips", os.path.join(ROOT, "Tools", "Tag", "render_pass18_noclip.py")
    )
    clips = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(clips)
    r = clips.r
    p = clips.p
    os.environ["NOCLIP_SETTLE"] = "runner"
    set_modifiers_off = True
    # Context setup poses the mannequin. Costume modifiers stay off so the
    # wall-plant search does not skin every shell on every candidate.
    _ = set_modifiers_off
    yaws = {
        "slide": p.SLIDE_YAW,
        "roll": p.SLIDE_YAW,
        "wall": 140.0,
        "idle": p.SLIDE_YAW,
        "walk": p.SLIDE_YAW,
        "run": p.SLIDE_YAW,
        "sprint": p.SLIDE_YAW,
    }
    wanted = os.environ.get("COSTUME_CLIPS", "idle,walk,run,sprint,wall,slide,roll")
    order = [name for name in ("idle", "walk", "run", "sprint", "wall", "slide", "roll") if name in wanted.split(",")]
    built = []
    for name in order:
        p.clear_props()
        p.ensure_pose(arm)
        if name in ("slide", "wall", "roll"):
            ctx = r.build_context(arm, name, yaws[name])
            show = r.SHOWS[name] if name != "roll" else r.show_roll
            if name == "slide":
                show = r.show_slide
            if name == "wall":
                show = r.show_wall
            times = r.ages_30(r.DUR[name])
        elif name == "walk":
            ctx = clips.build_extra(arm, "loco", yaws[name])
            ctx["speed"] = 1.6
            ctx["rate"] = clips.cadence_at(1.6)
            show = clips.show_loco
            times = r.ages_30(math.tau / ctx["rate"])
        else:
            kind = {"run": "loco", "sprint": "sprint", "idle": "idle"}[name]
            ctx = clips.build_extra(arm, kind, yaws[name])
            show = clips.show_idle if name == "idle" else clips.show_loco
            times = r.ages_30(clips.DUR[kind])
        built.append((name, show, ctx, times))
        log("READY %s seconds %.3f frames %d" % (name, times[-1] if times else 0.0, len(times)))
    p.clear_pose(arm)
    arm.location = (0.0, 0.0, 0.0)
    arm.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    return built


def fit_main():
    arm, body, pieces, sets = build_all()
    set_modifiers(pieces, False)
    clips = make_clips(arm)
    set_modifiers(pieces, True)
    # Body matrices were captured at rest. Refresh after the clip setup posed them.
    p_clear = True
    _ = p_clear
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = (0.0, 0.0, 0.0)
    arm.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    refresh_body(body)
    stats = None
    iterations = int(os.environ.get("COSTUME_ITERS", "4"))
    for iteration in range(iterations):
        stats = measure(arm, body, pieces, sets, clips)
        log(
            "PASS %d frames=%d worldMax=%.2f fails=%d pushes=%d %s"
            % (iteration, stats["frames"], depth_cm(stats["world"]), stats["fails"], len(stats["pushes"]), stats["worst"])
        )
        if stats["fails"] == 0:
            break
        if iteration == 3 or os.environ.get("COSTUME_NOPUSH") == "1":
            break
        # Rest matrices for the band check after a push.
        for bone in arm.pose.bones:
            bone.rotation_euler = (0.0, 0.0, 0.0)
        arm.location = (0.0, 0.0, 0.0)
        bpy.context.view_layer.update()
        refresh_body(body)
        moved, cut = apply_pushes(pieces, body, stats["pushes"])
        log("SHAPE moved=%d cut=%d" % (moved, cut))
        if moved == 0 and cut == 0:
            break
        for piece in pieces:
            mat = piece.obj.material_slots[0].material if piece.obj and piece.obj.material_slots else None
            if mat is None:
                continue
            realize(piece, arm, mat)
        log("CUT-ONLY")
    arm.rotation_euler = (0.0, 0.0, 0.0)
    arm.location = (0.0, 0.0, 0.0)
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.location = (0.0, 0.0, 0.0)
        bone.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    refresh_body(body)
    bmin, bmax, under, over = cloth_band_stats(pieces, body)
    acc, acc_name = accessory_span(pieces, body)
    counts = {piece.name: tri_of(piece) for piece in pieces}
    worn = {spec["id"]: sum(counts.get(name, 0) for name in spec["pieces"]) for spec in sets}
    write_loadouts(sets, {"pieces": counts, "worn": worn})
    line = "costume-fit sets=%d frames=%d worldMax=%.2f fails=%d" % (
        len(sets), stats["frames"], depth_cm(stats["world"]), stats["fails"],
    )
    os.makedirs(os.path.dirname(FIT_PATH), exist_ok=True)
    with open(FIT_PATH, "w", encoding="utf-8") as handle:
        handle.write(line + "\n")
        handle.write(
            "cloth-band min=%.2f max=%.2f under=%d over=%d\n" % (bmin * 100.0, bmax * 100.0, under, over)
        )
        handle.write("accessory-max=%.2f piece=%s\n" % (acc * 100.0, acc_name))
        handle.write("worst %s\n" % stats["worst"])
        for clip_name, clip_world, clip_frames, clip_fails in stats.get("by_clip", []):
            handle.write(
                "clip-fit %s worldMax=%.2f frames=%d fails=%d\n"
                % (clip_name, depth_cm(clip_world), clip_frames, clip_fails)
            )
        for spec in sets:
            handle.write("worn %s tris=%d\n" % (spec["id"], worn[spec["id"]]))
    log(line)
    log("CLOTH-BAND min-cm %.2f max-cm %.2f under=%d over=%d" % (bmin * 100.0, bmax * 100.0, under, over))
    log("ACCESSORY-MAX cm %.2f %s" % (acc * 100.0, acc_name))
    save_blend()
    log("WROTE %s" % BLEND)
    return line


def build_main():
    build_all()
    save_blend()
    log("WROTE %s" % BLEND)


if __name__ == "__main__":
    mode = "build"
    if "--" in sys.argv:
        extra = sys.argv[sys.argv.index("--") + 1:]
        if extra:
            mode = extra[0]
    if mode == "fit":
        fit_main()
    else:
        build_main()
