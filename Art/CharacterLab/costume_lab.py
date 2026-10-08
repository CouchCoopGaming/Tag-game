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
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT_DIR = os.path.join(ROOT, "Art", "CharacterLab")
BLEND = os.path.join(OUT_DIR, "CostumeLab.blend")
LOADOUTS = os.path.join(OUT_DIR, "loadouts.json")
FIT_PATH = os.path.join(ROOT, "Docs", "Characters", "pass1", "fit.txt")

# Cloth sits in this band off the skin. Penetration fails above 0.5 cm.
BAND_MIN = 0.003
BAND_MAX = 0.010
CLOTH_OFFSET = 0.006
SHOE_OFFSET = 0.0045
PEN_LIMIT = 0.005
# Rounded hundredths of a centimetre. A printed 0.50 is the limit.
PEN_CM = 0.50

# stations, sides. Tuned so one long outfit lands near 8–15k triangles.
DENSITY = {
    "chest": (22, 32),
    "spine": (5, 24),
    "shoulder": (8, 20),
    "sleeve": (14, 20),
    "jog": (14, 22),
    "shoe": (12, 18),
    "hood": (14, 26),
    "helmet": (14, 26),
    "cap": (10, 22),
    "collar": (5, 20),
}

PLAYER = {
    "Reed": (0.886, 0.235, 0.227),
    "Bram": (0.184, 0.435, 0.878),
    "Pip": (0.941, 0.478, 0.102),
    "Sol": (0.478, 0.271, 0.769),
}

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
    noise.inputs["Scale"].default_value = 42.0
    noise.inputs["Detail"].default_value = 2.0
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.12
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
    """How deep a point is inside any rendered body mesh, in metres."""
    worst = 0.0
    where = ""
    normal = Vector((0.0, 0.0, 1.0))
    for name, info in body.items():
        if name == ignore:
            continue
        local = info["inv"] @ point
        if aabb_distance(info, local) > 0.025:
            continue
        hit = visual_gap(info, point)
        if hit is None or hit[0] >= -1e-5:
            continue
        depth = -hit[0]
        if depth > worst:
            worst = depth
            where = name
            normal = hit[1]
    return worst, where, normal


def loft_piece(piece, points, origin, axis, t0, t1, n_stations, n_sides, offset, body, mask=None):
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
            # Buried in a neighbour (hip/thigh overlap, joint covers). Keep the exposed shell.
            depth, _where, _normal = visual_penetration(body, placed, ignore=piece.own)
            if depth > 0.002:
                continue
            ring[s] = placed
            hits += 1
        if hits >= max(4, n_sides // 5):
            chain.append(ring)
    if len(chain) >= 2:
        piece.chains.append(chain)


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
    """Seat cloth 0.6 cm outside the rendered hull. Drop bins that stay inside a neighbour."""
    own = body[piece.own]
    cut = 0
    moved = 0
    for chain in piece.chains:
        for ring in chain:
            for s, p in enumerate(ring):
                if p is None:
                    continue
                own_hit = visual_gap(own, p)
                if own_hit is None:
                    ring[s] = None
                    cut += 1
                    continue
                gap, normal = own_hit
                if piece.kind == "cloth":
                    placed = p + normal * (CLOTH_OFFSET - gap)
                    check = visual_gap(own, placed)
                    depth, _where, _normal = visual_penetration(body, placed, ignore=piece.own)
                    own_ok = check is not None and BAND_MIN <= check[0] <= BAND_MAX
                    if own_ok and depth < 0.001:
                        if (placed - p).length > 1e-5:
                            moved += 1
                        ring[s] = placed
                    else:
                        ring[s] = None
                        cut += 1
                elif gap < 0.002:
                    ring[s] = p + normal * (0.004 - gap)
                    moved += 1
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


def catalog(arm, body):
    pieces = []
    x_axis = Vector((1.0, 0.0, 0.0))
    y_axis = Vector((0.0, 1.0, 0.0))
    z_axis = Vector((0.0, 0.0, 1.0))

    def add_loft(name, bone, own, kind, origin, axis, t0, t1, key, offset, mask=None):
        n_st, n_side = DENSITY[key]
        piece = Piece(name, bone, own, kind)
        piece.loop = True
        pts = body[own]["world"]
        loft_piece(piece, pts, origin, axis, t0, t1, n_st, n_side, offset, body, mask)
        pieces.append(piece)
        return piece

    # Torso. Stop the hem above the hip shell so the slide and the roll have a gap.
    chest = body["Mesh_Chest"]["world"]
    chest_piece = add_loft(
        "Lab_HoodieChest", "Chest", "Mesh_Chest", "cloth",
        Vector((0.0, 0.0, 0.0)), z_axis, 1.175, 1.385, "chest", CLOTH_OFFSET,
    )
    # Leave the flanks clear so a swinging arm does not collect the shell.
    for chain in chest_piece.chains:
        for ring in chain:
            for index, vert in enumerate(ring):
                if vert is not None and abs(vert.x) > 0.090:
                    ring[index] = None
    add_loft("Lab_HoodieSpine", "Spine", "Mesh_Spine", "cloth",
             Vector((0.0, 0.0, 0.0)), z_axis, 1.078, 1.145, "spine", CLOTH_OFFSET)

    # Arms. Elbow flexion in these clips peaks near 50 degrees (the slide).
    # Shoulder covers stay bare: on this mannequin they sit inside the chest and the sleeve.
    for side in ("L", "R"):
        uo, ua = bone_axis(arm, "UpperArm_" + side)
        lo, la = bone_axis(arm, "LowerArm_" + side)
        upts = body["Mesh_UpperArm_" + side]["world"]
        lpts = body["Mesh_LowerArm_" + side]["world"]
        us = project_span(upts, uo, ua)
        ls = project_span(lpts, lo, la)
        # Joint is the lower-arm head, which is the upper-arm tail.
        u_keep0 = us[0] + (us[1] - us[0]) * 0.12
        u_keep1 = us[0] + (us[1] - us[0]) * 0.62
        l_keep0 = ls[0] + (ls[1] - ls[0]) * 0.28
        l_keep1 = ls[0] + (ls[1] - ls[0]) * 0.72
        add_loft(
            "Lab_SleeveU" + side, "UpperArm_" + side, "Mesh_UpperArm_" + side, "cloth",
            uo, ua, u_keep0, u_keep1, "sleeve", CLOTH_OFFSET,
        )
        add_loft(
            "Lab_SleeveL" + side, "LowerArm_" + side, "Mesh_LowerArm_" + side, "cloth",
            lo, la, l_keep0, l_keep1, "sleeve", CLOTH_OFFSET,
        )

    # Legs. The slide folds one knee to 150 degrees, so the tubes stop short of the joint.
    for side in ("L", "R"):
        uo, ua = bone_axis(arm, "UpperLeg_" + side)
        lo, la = bone_axis(arm, "LowerLeg_" + side)
        upts = body["Mesh_UpperLeg_" + side]["world"]
        lpts = body["Mesh_LowerLeg_" + side]["world"]
        us = project_span(upts, uo, ua)
        ls = project_span(lpts, lo, la)
        # The pelvis shell reaches the swinging thigh. Keep the jogger on the distal band.
        u_keep0 = us[0] + (us[1] - us[0]) * 0.42
        u_keep1 = us[0] + (us[1] - us[0]) * 0.52
        l_keep0 = ls[0] + (ls[1] - ls[0]) * 0.42
        l_keep1 = ls[0] + (ls[1] - ls[0]) * 0.78
        add_loft(
            "Lab_JogU" + side, "UpperLeg_" + side, "Mesh_UpperLeg_" + side, "cloth",
            uo, ua, u_keep0, u_keep1, "jog", CLOTH_OFFSET,
        )
        add_loft(
            "Lab_JogL" + side, "LowerLeg_" + side, "Mesh_LowerLeg_" + side, "cloth",
            lo, la, l_keep0, l_keep1, "jog", CLOTH_OFFSET,
        )

        # Shoe along the foot's forward axis. The foot bone tail is an FBX-axis
        # artifact, so the axis comes from the mesh, facing -Y.
        fpts = body["Mesh_Foot_" + side]["world"]
        centroid = sum(fpts, Vector()) / len(fpts)
        ft0, ft1 = project_span(fpts, centroid, -y_axis)
        add_loft(
            "Lab_Shoe" + side, "Foot_" + side, "Mesh_Foot_" + side, "cloth",
            centroid, -y_axis, ft0 + 0.01, ft1 - 0.008, "shoe", SHOE_OFFSET,
        )

    # Head wear. Face is -Y. A wide opening reads as a hood, a narrow one as a helmet.
    hpts = body["Mesh_Head"]["world"]
    h_center = sum(hpts, Vector()) / len(hpts)

    def face_mask(limit):
        def mask(direction, _center):
            return direction.dot(Vector((0.0, -1.0, 0.0))) < limit
        return mask

    hz0, hz1 = project_span(hpts, Vector((0.0, 0.0, 0.0)), z_axis)
    add_loft(
        "Lab_Hood", "Head", "Mesh_Head", "cloth",
        Vector((0.0, 0.0, 0.0)), z_axis, hz0 + (hz1 - hz0) * 0.42, hz1 - 0.006,
        "hood", 0.008, face_mask(0.20),
    )
    add_loft(
        "Lab_Helmet", "Head", "Mesh_Head", "cloth",
        Vector((0.0, 0.0, 0.0)), z_axis, hz0 + (hz1 - hz0) * 0.38, hz1 - 0.004,
        "helmet", 0.009, face_mask(0.55),
    )
    add_loft(
        "Lab_Cap", "Head", "Mesh_Head", "cloth",
        Vector((0.0, 0.0, 0.0)), z_axis, h_center.z + 0.01, hz1 - 0.004,
        "cap", 0.007, None,
    )

    # Hood-down collar, on the neck, clear of the jaw and the hoodie hem.
    npts = body["Mesh_Neck"]["world"]
    n0, n1 = project_span(npts, Vector((0.0, 0.0, 0.0)), z_axis)
    add_loft(
        "Lab_Collar", "Neck", "Mesh_Neck", "cloth",
        Vector((0.0, 0.0, 0.0)), z_axis, n0 + (n1 - n0) * 0.35, n0 + (n1 - n0) * 0.78,
        "collar", 0.008, None,
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
    back_y = max(p.y for p in chest)
    inner = back_y + 0.012
    depth = 0.045
    pack = Piece("Lab_Pack", "Chest", "Mesh_Chest", "accessory")
    pack.loop = False
    center = Vector((0.0, inner + depth * 0.5, 1.27))
    add_box(pack, center, x_axis, y_axis, z_axis, 0.070, depth * 0.5, 0.085, 5, 4)
    pieces.append(pack)

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
        # One extra ring between stations. Straight limbs stay on the shell; enforce seats the rest.
        if piece.loop and piece.chains and not piece.boxes:
            subdivide_length(piece)
        moved, cut = enforce_band(piece, body)
        log("BUILT %s verts=%d moved=%d cut=%d" % (piece.name, piece.vert_count(), moved, cut))
    return pieces


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


def loadout_map():
    long_core = [
        "Lab_HoodieChest", "Lab_Seam", "Lab_Pocket",
        "Lab_SleeveUL", "Lab_SleeveUR", "Lab_SleeveLL", "Lab_SleeveLR",
        "Lab_JogUL", "Lab_JogUR", "Lab_JogLL", "Lab_JogLR",
        "Lab_ShoeL", "Lab_ShoeR",
    ]
    crop_core = [
        "Lab_HoodieChest", "Lab_Seam", "Lab_Pocket",
        "Lab_SleeveUL", "Lab_SleeveUR",
        "Lab_JogUL", "Lab_JogUR",
        "Lab_ShoeL", "Lab_ShoeR",
    ]
    specs = [
        ("Reed", 1, "hood", long_core + ["Lab_Hood", "Lab_HoodRoll"]),
        ("Reed", 2, "cap", long_core + ["Lab_Cap", "Lab_Brim", "Lab_Pack"]),
        ("Reed", 3, "helmet", long_core + ["Lab_Helmet", "Lab_Visor"]),
        ("Bram", 1, "helmet", long_core + ["Lab_Helmet", "Lab_Visor"]),
        ("Bram", 2, "cap", long_core + ["Lab_Cap", "Lab_Brim", "Lab_Pack"]),
        ("Bram", 3, "hood", long_core + ["Lab_Hood", "Lab_HoodRoll"]),
        ("Pip", 1, "cap", crop_core + ["Lab_Cap", "Lab_Brim", "Lab_Pack"]),
        ("Pip", 2, "hair", crop_core + ["Lab_Hair"]),
        ("Pip", 3, "helmet", crop_core + ["Lab_Helmet", "Lab_Visor"]),
        ("Sol", 1, "collar-cap", long_core + ["Lab_Collar", "Lab_Cap", "Lab_Brim"]),
        ("Sol", 2, "hood", long_core + ["Lab_Hood", "Lab_HoodRoll"]),
        ("Sol", 3, "helmet-pack", long_core + ["Lab_Helmet", "Lab_Visor", "Lab_Pack"]),
    ]
    sets = []
    for who, index, label, names in specs:
        sets.append({
            "id": id_for(who, index, label),
            "character": who,
            "variant": index,
            "label": label,
            "color": list(PLAYER[who]),
            "pieces": names,
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
    return rad - dist


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
    for pair, depth in sorted(rest_pair.items(), key=lambda item: -item[1])[:8]:
        if depth_cm(depth) > 0.0:
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
    t0 = time.time()
    for clip_name, show, ctx, times in clips:
        log("CLIP %s frames %d" % (clip_name, len(times)))
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
                if depth_cm(local) > PEN_CM:
                    fails += 1
                if local > world:
                    world = local
                    worst_desc = "%s t=%.3f cm=%.2f" % (clip_name, age, depth_cm(local))
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
    return {
        "frames": frames,
        "fails": fails,
        "world": world,
        "pushes": pushes,
        "worst": worst_desc,
        "band_close": band_close,
        "rest_pair": max(rest_pair.values()) if rest_pair else 0.0,
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
            hit = visual_gap(own, vert)
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
            hit = visual_gap(own, vert)
            if hit is not None and hit[0] > worst:
                worst = hit[0]
                name = piece.name
    return worst, name


def save_blend():
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)


def build_all():
    arm = import_hier()
    tint_mannequin()
    body = capture_body()
    probe_body(body)
    pieces = catalog(arm, body)
    player = make_material("LabPlayer", PLAYER["Reed"], True)
    jogger = make_material("LabJogger", (0.16, 0.16, 0.18), False)
    shoe = make_material("LabShoe", (0.75, 0.73, 0.70), False)
    pack = make_material("LabPack", (0.12, 0.12, 0.13), False)
    jog_names = {"Lab_JogUL", "Lab_JogUR", "Lab_JogLL", "Lab_JogLR"}
    shoe_names = {"Lab_ShoeL", "Lab_ShoeR"}
    pack_names = {"Lab_Pack"}
    counts = {}
    for piece in pieces:
        if piece.name in jog_names:
            mat = jogger
        elif piece.name in shoe_names:
            mat = shoe
        elif piece.name in pack_names:
            mat = pack
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
        "run": p.SLIDE_YAW,
        "sprint": p.SLIDE_YAW,
    }
    wanted = os.environ.get("COSTUME_CLIPS", "idle,run,sprint,wall,slide,roll")
    order = [name for name in ("idle", "run", "sprint", "wall", "slide", "roll") if name in wanted.split(",")]
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
        if iteration == 3:
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
