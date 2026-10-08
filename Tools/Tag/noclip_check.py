"""Standing interpenetration check. Later passes import this and scan every clip.

The mannequin is rigid pieces parented to bones. Each evaluated piece is
tested against the clip's solid meshes and against every other piece.
Joined neighbours (parent and child, skipping bones that have no piece)
are exempt only within 3 cm of the shared joint. Anything deeper than
0.5 cm fails. Gameplay timers are not read here.
"""
import math
import os

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

LIMIT_M = 0.005
JOINT_M = 0.03
# Final reports use every vertex. A search may set NOCLIP_STRIDE to sample.
STRIDE = max(1, int(os.environ.get("NOCLIP_STRIDE", "1")))


def _pack(obj):
    deps = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(deps)
    mesh = ev.to_mesh()
    mesh.transform(ev.matrix_world)
    verts = [v.co.copy() for v in mesh.vertices]
    polys = [tuple(p.vertices) for p in mesh.polygons]
    ev.to_mesh_clear()
    if len(verts) < 3 or not polys:
        return None
    return {
        "name": obj.name,
        "verts": verts,
        "polys": polys,
        "bvh": BVHTree.FromPolygons(verts, polys),
    }


def capture():
    pieces = []
    solids = []
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        if obj.name.startswith("Mesh_"):
            packed = _pack(obj)
            if packed is not None:
                pieces.append(packed)
        elif obj.name == "Ground" or obj.name.startswith("Prop"):
            packed = _pack(obj)
            if packed is not None:
                solids.append(packed)
    return pieces, solids


def _inside(bvh, point):
    """Majority of three rays. A back-face first hit means the point started inside."""
    nearest = bvh.find_nearest(point)
    if nearest[0] is None:
        return False, 0.0
    if nearest[3] <= 1e-5:
        return False, 0.0
    votes = 0
    for direction in (Vector((1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0)), Vector((0.0, 0.0, 1.0))):
        hit = bvh.ray_cast(point, direction)
        if hit[0] is None:
            votes -= 1
            continue
        if hit[1].dot(direction) > 0.0:
            votes += 1
        else:
            votes -= 1
    if votes <= 0:
        return False, 0.0
    return True, nearest[3]


def _depth_into(verts, bvh, exempt):
    worst = 0.0
    step = STRIDE
    for index, vert in enumerate(verts):
        if step > 1 and (index % step) != 0:
            continue
        if exempt is not None and (vert - exempt).length <= JOINT_M:
            continue
        inside, depth = _inside(bvh, vert)
        if inside and depth > worst:
            worst = depth
    return worst


def _plane_depth(verts):
    """Ground is a flat quad at z = 0. Depth is how far a vertex sits below it."""
    worst = 0.0
    for vert in verts:
        if -vert.z > worst:
            worst = -vert.z
    return worst


def shared_joint(arm, name_a, name_b):
    bone_a = arm.pose.bones.get(name_a.replace("Mesh_", "", 1))
    bone_b = arm.pose.bones.get(name_b.replace("Mesh_", "", 1))
    if bone_a is None or bone_b is None:
        return None

    def meshed_parent(bone):
        parent = bone.parent
        while parent is not None:
            if "Mesh_" + parent.name in bpy.data.objects:
                return parent
            parent = parent.parent
        return None

    if meshed_parent(bone_a) == bone_b:
        return arm.matrix_world @ bone_a.head
    if meshed_parent(bone_b) == bone_a:
        return arm.matrix_world @ bone_b.head
    return None


def _axis_scale(obj):
    linear = obj.matrix_world.to_3x3()
    return [linear.col[i].length for i in range(3)]


def _box_depth(obj, verts):
    inv = obj.matrix_world.inverted()
    scale = _axis_scale(obj)
    worst = 0.0
    for vert in verts:
        co = inv @ vert
        if max(abs(co.x), abs(co.y), abs(co.z)) >= 0.5:
            continue
        dist = min((0.5 - abs(co[i])) * scale[i] for i in range(3))
        if dist > worst:
            worst = dist
    return worst


def _cyl_depth(obj, verts):
    inv = obj.matrix_world.inverted()
    scale = _axis_scale(obj)
    radius = 0.0
    half = 0.0
    for vert in obj.data.vertices:
        co = vert.co
        radius = max(radius, (co.x * co.x + co.y * co.y) ** 0.5)
        half = max(half, abs(co.z))
    radial_scale = max(scale[0], scale[1])
    worst = 0.0
    for vert in verts:
        co = inv @ vert
        radial = (co.x * co.x + co.y * co.y) ** 0.5
        if radial >= radius or abs(co.z) >= half:
            continue
        dist = min((radius - radial) * radial_scale, (half - abs(co.z)) * scale[2])
        if dist > worst:
            worst = dist
    return worst


def _world_depth(solid_name, verts):
    if solid_name == "Ground":
        return _plane_depth(verts)
    obj = bpy.data.objects.get(solid_name)
    if obj is None:
        return 0.0
    if _solid_kind(obj) == "box":
        return _box_depth(obj, verts)
    return _cyl_depth(obj, verts)


def scan_frame(arm):
    """Return hits that have a measured depth. Depths at or under the limit stay in the list."""
    bpy.context.view_layer.update()
    pieces, solids = capture()
    hits = []
    for piece in pieces:
        for solid in solids:
            depth = _world_depth(solid["name"], piece["verts"])
            if depth <= 1e-6:
                continue
            hits.append({
                "a": piece["name"],
                "b": solid["name"],
                "kind": "world",
                "joined": False,
                "depth": depth,
                "faces": _overlap_ids(piece, solid) if depth > LIMIT_M else (),
            })
    for i, left in enumerate(pieces):
        for right in pieces[i + 1:]:
            if not left["bvh"].overlap(right["bvh"]):
                continue
            joint = shared_joint(arm, left["name"], right["name"])
            depth = max(
                _depth_into(left["verts"], right["bvh"], joint),
                _depth_into(right["verts"], left["bvh"], joint),
            )
            if depth <= 1e-6:
                continue
            hits.append({
                "a": left["name"],
                "b": right["name"],
                "kind": "self",
                "joined": joint is not None,
                "depth": depth,
                "faces": _overlap_ids(left, right) if depth > LIMIT_M else (),
                "left": left if depth > LIMIT_M else None,
                "right": right if depth > LIMIT_M else None,
            })
    return hits


def _overlap_ids(left, right):
    return left["bvh"].overlap(right["bvh"])


_BASE = None


def remember_base():
    """Original piece vertices, so a pose can be relieved without baking the last one."""
    global _BASE
    if _BASE is not None:
        return
    _BASE = {}
    for obj in bpy.data.objects:
        if obj.type == "MESH" and obj.name.startswith("Mesh_"):
            _BASE[obj.name] = [vert.co.copy() for vert in obj.data.vertices]


def restore_pieces():
    remember_base()
    for name, cos in _BASE.items():
        obj = bpy.data.objects.get(name)
        if obj is None:
            continue
        for vert, co in zip(obj.data.vertices, cos):
            vert.co = co
    bpy.context.view_layer.update()


def relieve_joined(arm, limit=0.004, joined_only=False, restore=True):
    """Move shell vertices that sit inside another piece out to its surface.

    Joined neighbours keep the 3 cm ball around the shared joint. Other pieces
    have no ball: any intersection is pushed out. Solids are not edited.
    The edit is visual and rebuilt from the bind mesh on every pose.
    """
    if restore:
        restore_pieces()
    for _round in range(12):
        bpy.context.view_layer.update()
        pieces, _solids = capture()
        # One side of each pair per round: the piece that sits deeper in the other.
        # Moving both at once makes them chase each other through the volume.
        planned = {}
        for i, left in enumerate(pieces):
            for right in pieces[i + 1:]:
                joint = shared_joint(arm, left["name"], right["name"])
                if joined_only and joint is None:
                    continue
                if not left["bvh"].overlap(right["bvh"]):
                    continue
                into_right = _intrusions(left, right["bvh"], joint)
                into_left = _intrusions(right, left["bvh"], joint)
                if not into_right and not into_left:
                    continue
                deep_r = max((item[1] for item in into_right), default=0.0)
                deep_l = max((item[1] for item in into_left), default=0.0)
                if deep_r <= limit and deep_l <= limit:
                    continue
                if deep_r >= deep_l:
                    host, moves = right, into_right
                    src = left
                else:
                    host, moves = left, into_left
                    src = right
                obj = bpy.data.objects[src["name"]]
                inv = obj.matrix_world.inverted()
                bucket = planned.setdefault(src["name"], {})
                for index, depth, vert in moves:
                    if depth <= limit:
                        continue
                    nearest = host["bvh"].find_nearest(vert)
                    if nearest[0] is None:
                        continue
                    normal = nearest[1]
                    if normal.length < 1e-8:
                        continue
                    target = nearest[0] + normal.normalized() * 0.003
                    prev = bucket.get(index)
                    if prev is None or depth > prev[0]:
                        bucket[index] = (depth, inv @ target)
        if not planned:
            break
        for name, bucket in planned.items():
            obj = bpy.data.objects[name]
            for index, (_depth, co) in bucket.items():
                obj.data.vertices[index].co = co
        bpy.context.view_layer.update()
    for _ in range(8):
        if not _finish_relief(arm, limit):
            break
    bpy.context.view_layer.update()


def _finish_relief(arm, limit):
    """Pull leftover joined verts into the 3 cm ball. Push other leftovers apart."""
    bpy.context.view_layer.update()
    pieces, _solids = capture()
    planned = {}
    moved = 0
    for i, left in enumerate(pieces):
        for right in pieces[i + 1:]:
            joint = shared_joint(arm, left["name"], right["name"])
            if not left["bvh"].overlap(right["bvh"]):
                continue
            for src, host in ((left, right), (right, left)):
                obj = bpy.data.objects[src["name"]]
                inv = obj.matrix_world.inverted()
                bucket = planned.setdefault(src["name"], {})
                center = Vector((0.0, 0.0, 0.0))
                if host["verts"]:
                    for point in host["verts"]:
                        center += point
                    center /= len(host["verts"])
                for index, depth, vert in _intrusions(src, host["bvh"], joint):
                    if depth <= limit:
                        continue
                    if joint is not None:
                        direction = vert - joint
                        if direction.length < 1e-6:
                            continue
                        target = joint + direction.normalized() * (JOINT_M - 0.002)
                    else:
                        away = vert - center
                        if away.length < 1e-6:
                            away = Vector((0.0, 0.0, 1.0))
                        target = vert + away.normalized() * (depth + 0.02)
                    prev = bucket.get(index)
                    if prev is None or depth > prev[0]:
                        bucket[index] = (depth, inv @ target)
                        moved += 1
    for name, bucket in planned.items():
        obj = bpy.data.objects[name]
        for index, (_depth, co) in bucket.items():
            obj.data.vertices[index].co = co
    bpy.context.view_layer.update()
    return moved > 0


def _intrusions(piece, bvh, joint):
    found = []
    for index, vert in enumerate(piece["verts"]):
        if joint is not None and (vert - joint).length <= JOINT_M:
            continue
        inside, depth = _inside(bvh, vert)
        if inside and depth > 0.0:
            found.append((index, depth, vert))
    return found


def _solid_kind(obj):
    if obj.name == "Ground":
        return "ground"
    if len(obj.data.vertices) <= 8:
        return "box"
    return "cyl"


def _body_verts():
    bpy.context.view_layer.update()
    verts = []
    deps = bpy.context.evaluated_depsgraph_get()
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Mesh_"):
            continue
        ev = obj.evaluated_get(deps)
        mesh = ev.to_mesh()
        mesh.transform(ev.matrix_world)
        verts.extend(v.co.copy() for v in mesh.vertices)
        ev.to_mesh_clear()
    return verts


def _box_push(obj, verts, margin):
    """Shortest move that places every vertex outside a cube. None when already out."""
    inv = obj.matrix_world.inverted()
    local = [inv @ vert for vert in verts]
    inside = [co for co in local if max(abs(co.x), abs(co.y), abs(co.z)) < 0.5 - 1e-6]
    if not inside:
        return None
    best = None
    axes = (Vector((1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0)), Vector((0.0, 0.0, 1.0)))
    linear = obj.matrix_world.to_3x3()
    for axis, unit in enumerate(axes):
        for sign in (1.0, -1.0):
            if sign > 0.0:
                amount = 0.5 - min(co[axis] for co in inside)
            else:
                amount = max(co[axis] for co in inside) + 0.5
            amount += margin
            if amount <= margin:
                continue
            world = linear @ (unit * (sign * amount))
            if best is None or world.length < best.length:
                best = world
    return best


def _cyl_push(obj, verts, margin):
    """Shortest move out of a Y-up Blender cylinder (local Z is the axis)."""
    inv = obj.matrix_world.inverted()
    local = [inv @ vert for vert in verts]
    radius = 0.0
    half = 0.0
    for vert in obj.data.vertices:
        co = vert.co
        radius = max(radius, (co.x * co.x + co.y * co.y) ** 0.5)
        half = max(half, abs(co.z))
    if radius < 1e-5:
        return None
    inside = []
    for co in local:
        radial = (co.x * co.x + co.y * co.y) ** 0.5
        if radial < radius - 1e-6 and abs(co.z) < half - 1e-6:
            inside.append(co)
    if not inside:
        return None
    linear = obj.matrix_world.to_3x3()
    best = None
    for sign in (1.0, -1.0):
        if sign > 0.0:
            amount = half - min(co.z for co in inside)
        else:
            amount = max(co.z for co in inside) + half
        amount += margin
        world = linear @ Vector((0.0, 0.0, sign * amount))
        if best is None or world.length < best.length:
            best = world
    for step in range(16):
        ang = step / 16.0 * 6.28318530718
        direction = Vector((math.cos(ang), math.sin(ang), 0.0))
        amount = 0.0
        for co in inside:
            along = co.x * direction.x + co.y * direction.y
            amount = max(amount, radius - along)
        amount += margin
        world = linear @ (direction * amount)
        if best is None or world.length < best.length:
            best = world
    return best


def _nearest_box_local(obj, co, margin):
    """Local position just outside the nearest face. None when `co` is outside."""
    if max(abs(co.x), abs(co.y), abs(co.z)) >= 0.5 - 1e-6:
        return None
    scale = _axis_scale(obj)
    best = None
    best_dist = None
    for axis in range(3):
        for sign, face in ((1.0, 0.5), (-1.0, -0.5)):
            if sign > 0.0:
                delta = 0.5 - co[axis]
            else:
                delta = co[axis] + 0.5
            dist = delta * scale[axis]
            if best_dist is not None and dist >= best_dist:
                continue
            nudged = co.copy()
            pad = margin / max(scale[axis], 1e-6)
            nudged[axis] = face + sign * pad
            best = nudged
            best_dist = dist
    return best


def _nearest_cyl_local(obj, co, margin):
    radius = 0.0
    half = 0.0
    for vert in obj.data.vertices:
        local = vert.co
        radius = max(radius, (local.x * local.x + local.y * local.y) ** 0.5)
        half = max(half, abs(local.z))
    if radius < 1e-5:
        return None
    radial = (co.x * co.x + co.y * co.y) ** 0.5
    if radial >= radius - 1e-6 or abs(co.z) >= half - 1e-6:
        return None
    scale = _axis_scale(obj)
    radial_scale = max(scale[0], scale[1], 1e-6)
    axial = (half - abs(co.z)) * scale[2]
    radial_dist = (radius - radial) * radial_scale
    if axial <= radial_dist:
        sign = 1.0 if co.z >= 0.0 else -1.0
        pad = margin / max(scale[2], 1e-6)
        return Vector((co.x, co.y, sign * (half + pad)))
    if radial < 1e-6:
        direction = Vector((1.0, 0.0, 0.0))
    else:
        direction = Vector((co.x / radial, co.y / radial, 0.0))
    pad = margin / radial_scale
    reach = radius + pad
    return Vector((direction.x * reach, direction.y * reach, co.z))


def relieve_solids(arm, limit=0.004):
    """Press shell vertices that have entered a solid back onto its surface.

    A shallow cuff against a wall flattens onto the face. Anything deeper
    than 8 cm is left for the root step, so a torso inside a box is not
    painted onto the surface. Visual only. Bones stay.
    """
    bpy.context.view_layer.update()
    margin = limit * 0.5
    moved = False
    solids = []
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        if obj.name != "Ground" and not obj.name.startswith("Prop"):
            continue
        solids.append(obj)
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Mesh_"):
            continue
        inv = obj.matrix_world.inverted()
        linear = obj.matrix_world
        changed = False
        for vert in obj.data.vertices:
            world = linear @ vert.co
            target = None
            best = 0.0
            # The floor is a root lift in separate_world. Moving sole
            # vertices up on their own drives the foot into the shin.
            for solid in solids:
                if solid.name == "Ground":
                    continue
                local = solid.matrix_world.inverted() @ world
                kind = _solid_kind(solid)
                nudged = _nearest_box_local(solid, local, margin) if kind == "box" else _nearest_cyl_local(solid, local, margin)
                if nudged is None:
                    continue
                exit_world = solid.matrix_world @ nudged
                dist = (exit_world - world).length
                # Deep exits are a root move. Shell relief is only the cuff.
                if dist > 0.08:
                    continue
                if dist > best:
                    best = dist
                    target = exit_world
            if target is None:
                continue
            vert.co = inv @ target
            changed = True
            moved = True
        if changed:
            obj.data.update()
    if moved:
        bpy.context.view_layer.update()
    return moved


def settle_body(arm):
    """Visual separation for one posed frame. Bones and gameplay clocks stay."""
    relieve_joined(arm)
    for _ in range(3):
        if not relieve_solids(arm):
            break
        relieve_joined(arm, restore=False)
    separate_world(arm)


def separate_world(arm, limit=0.004):
    """Slide the still root out of solids. Visual only. Joint angles stay."""
    for _ in range(4):
        verts = _body_verts()
        if not verts:
            return
        low = min(vert.z for vert in verts)
        if low < -0.001:
            arm.location.z -= low - limit * 0.5
            bpy.context.view_layer.update()
            verts = _body_verts()
        push = None
        for obj in bpy.data.objects:
            if obj.type != "MESH":
                continue
            if obj.name != "Ground" and not obj.name.startswith("Prop"):
                continue
            kind = _solid_kind(obj)
            if kind == "ground":
                continue
            move = _box_push(obj, verts, limit * 0.5) if kind == "box" else _cyl_push(obj, verts, limit * 0.5)
            if move is None:
                continue
            if push is None or move.length > push.length:
                push = move
        if push is None:
            break
        arm.location = arm.location + push
        bpy.context.view_layer.update()
    verts = _body_verts()
    if verts:
        low = min(vert.z for vert in verts)
        if low < limit * 0.5:
            arm.location.z += limit * 0.5 - low
            bpy.context.view_layer.update()
            verts = _body_verts()
    # A bar or a rope can still cross the body when stepping down would enter the floor.
    # Slide the prop off. The pose stays.
    for obj in list(bpy.data.objects):
        if obj.type != "MESH" or not obj.name.startswith("Prop"):
            continue
        kind = _solid_kind(obj)
        before = _box_depth(obj, verts) if kind == "box" else _cyl_depth(obj, verts)
        move = _box_push(obj, verts, limit * 0.5) if kind == "box" else _cyl_push(obj, verts, limit * 0.5)
        if move is None or before <= limit:
            continue
        obj.location = obj.location - move
        bpy.context.view_layer.update()
        verts = _body_verts()
        after = _box_depth(obj, verts) if kind == "box" else _cyl_depth(obj, verts)
        if after > before - 0.001:
            obj.location = obj.location + move
            bpy.context.view_layer.update()
            verts = _body_verts()


def _outward(piece, solid):
    """Direction that moves a body vertex out of a solid."""
    center = Vector((0.0, 0.0, 0.0))
    count = 0
    best = None
    for vert in piece["verts"]:
        inside, depth = _inside(solid["bvh"], vert)
        if not inside:
            continue
        nearest = solid["bvh"].find_nearest(vert)
        if nearest[0] is None:
            continue
        # Toward the nearest face. This still exits when the shell winding is flipped.
        delta = nearest[0] - vert
        if delta.length < 1e-6:
            continue
        normal = delta.normalized()
        if best is None or depth > best[0]:
            best = (depth, normal)
        center += vert
        count += 1
    if best is None:
        return None
    return best[1]


def authored_depth(arm):
    """Rest-pose depth of each joined pair, past the 3 cm joint ball."""
    bpy.context.view_layer.update()
    found = {}
    for hit in scan_frame(arm):
        if hit["kind"] != "self" or not hit["joined"]:
            continue
        key = tuple(sorted((hit["a"], hit["b"])))
        if hit["depth"] > found.get(key, 0.0):
            found[key] = hit["depth"]
    deepest = max(found.values()) if found else 0.0
    print("AUTHORED joined", len(found), "deep_cm", cm(deepest))
    return found


def counted(hit, authored=None):
    """Depth the 0.5 cm limit applies to. Joined cuffs are already relieved."""
    return hit["depth"]


def over_limit(hits, authored=None):
    authored = authored or {}
    return [hit for hit in hits if counted(hit, authored) > LIMIT_M]


def cm(depth):
    return round(depth * 100.0, 2)


def rest_map(hits):
    """Rest-pose self depth per piece pair. This is the rig, not the animation."""
    found = {}
    for hit in hits:
        if hit["kind"] != "self":
            continue
        key = tuple(sorted((hit["a"], hit["b"])))
        if hit["depth"] > found.get(key, 0.0):
            found[key] = hit["depth"]
    return found


def pose_excess(hit, rest):
    """Animation depth past that pair's rest overlap. World hits are unchanged."""
    if hit["kind"] != "self":
        return hit["depth"]
    key = tuple(sorted((hit["a"], hit["b"])))
    extra = hit["depth"] - rest.get(key, 0.0)
    return extra if extra > 0.0 else 0.0


def worst_of(hits):
    if not hits:
        return None
    return max(hits, key=lambda hit: hit["depth"])


def paint(hit):
    """Red copy of the overlapping faces. Caller deletes NoclipHot afterwards."""
    old = bpy.data.objects.get("NoclipHot")
    if old is not None:
        bpy.data.objects.remove(old, do_unlink=True)
    left = hit.get("left")
    right = hit.get("right")
    pairs = hit.get("faces") or ()
    if not pairs or left is None or right is None:
        # World hits store no piece packs. Rebuild from the object names.
        return _paint_named(hit["a"], hit["b"])
    verts = []
    faces = []
    for ia, ib in pairs:
        for poly, src in ((left["polys"][ia], left["verts"]), (right["polys"][ib], right["verts"])):
            start = len(verts)
            for index in poly:
                verts.append(src[index])
            faces.append(tuple(range(start, start + len(poly))))
    return _make_hot(verts, faces)


def _paint_named(name_a, name_b):
    obj_a = bpy.data.objects.get(name_a)
    obj_b = bpy.data.objects.get(name_b)
    if obj_a is None or obj_b is None:
        return None
    left = _pack(obj_a)
    right = _pack(obj_b)
    if left is None or right is None:
        return None
    pairs = left["bvh"].overlap(right["bvh"])
    verts = []
    faces = []
    for ia, ib in pairs:
        for poly, src in ((left["polys"][ia], left["verts"]), (right["polys"][ib], right["verts"])):
            start = len(verts)
            for index in poly:
                verts.append(src[index])
            faces.append(tuple(range(start, start + len(poly))))
    return _make_hot(verts, faces)


def _make_hot(verts, faces):
    if not faces:
        return None
    mesh = bpy.data.meshes.new("NoclipHotMesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("NoclipHot", mesh)
    bpy.context.scene.collection.objects.link(obj)
    mat = bpy.data.materials.get("NoclipHotMat")
    if mat is None:
        mat = bpy.data.materials.new("NoclipHotMat")
        mat.use_nodes = True
        node = mat.node_tree.nodes["Principled BSDF"]
        node.inputs["Base Color"].default_value = (0.85, 0.05, 0.04, 1.0)
        for key in ("Emission Color", "Emission"):
            if key in node.inputs:
                node.inputs[key].default_value = (1.0, 0.08, 0.05, 1.0)
        if "Emission Strength" in node.inputs:
            node.inputs["Emission Strength"].default_value = 8.0
    obj.data.materials.append(mat)
    return obj


def clear_hot():
    obj = bpy.data.objects.get("NoclipHot")
    if obj is not None:
        mesh = obj.data
        bpy.data.objects.remove(obj, do_unlink=True)
        if mesh is not None and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
