"""Shared builder for the tag asset library.

Coordinates in asset scripts are Unity meters: +X right, +Y up, +Z forward.
Blender stays Z-up internally. Export bakes the Unity axis (X, Z, -Y) and the
FBX centimeter scale. Unity's importer (useFileScale) brings that back to meters.

Mesh fileIDs match this project's imported FBX assets:
    xxHash64("Type:Mesh->{name}0") as a signed int64.
A mesh named LOD0 therefore hashes the string "Type:Mesh->LOD00".
"""

from __future__ import annotations

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(ROOT, "..", "..", ".."))
LIB_ROOT = os.path.join(REPO, "Assets", "Art", "Props", "Library")
TEX_DIR = os.path.join(LIB_ROOT, "Textures")
STILL_DIR = os.path.join(REPO, "Docs", "AssetStills", "pass1")

# color, metallic, smoothness. Matches the park kit, extended for city/harbor.
PALETTE = {
    "Lib_Steel": ((0.722, 0.753, 0.784), 0.55, 0.40),
    "Lib_SteelDark": ((0.28, 0.30, 0.33), 0.72, 0.34),
    "Lib_PaintYellow": ((0.961, 0.835, 0.278), 0.0, 0.45),
    "Lib_PaintRed": ((0.886, 0.231, 0.184), 0.0, 0.40),
    "Lib_PaintBlue": ((0.239, 0.494, 1.0), 0.0, 0.40),
    "Lib_PaintWhite": ((0.93, 0.93, 0.90), 0.0, 0.35),
    "Lib_PaintGreen": ((0.20, 0.46, 0.28), 0.0, 0.35),
    "Lib_Rubber": ((0.165, 0.165, 0.18), 0.0, 0.22),
    "Lib_Concrete": ((0.78, 0.78, 0.76), 0.0, 0.22),
    "Lib_Asphalt": ((0.16, 0.16, 0.17), 0.0, 0.16),
    # Kept in the palette. The straight tile is one asphalt surface and does not use these.
    "Lib_AsphaltWear": ((0.128, 0.128, 0.132), 0.0, 0.14),
    "Lib_AsphaltPatch": ((0.07, 0.07, 0.072), 0.0, 0.18),
    "Lib_Brick": ((0.64, 0.32, 0.24), 0.0, 0.28),
    "Lib_Mortar": ((0.72, 0.70, 0.66), 0.0, 0.20),
    # Grey-tan chinking. Dark and matte so the groove does not catch the sky.
    "Lib_Chink": ((0.26, 0.22, 0.17), 0.0, 0.08),
    # Flat fallback under the growth-ring texture on a log end.
    "Lib_LogEnd": ((0.55, 0.38, 0.22), 0.0, 0.22),
    "Lib_Wood": ((0.62, 0.42, 0.24), 0.0, 0.32),
    "Lib_WoodDark": ((0.36, 0.22, 0.13), 0.0, 0.28),
    # Round piles. Vertical grain, a wet shaft, a green waterline, a grey top.
    "Lib_Pile": ((0.42, 0.30, 0.16), 0.0, 0.30),
    "Lib_PileWet": ((0.18, 0.12, 0.07), 0.0, 0.18),
    "Lib_PileAlgae": ((0.26, 0.34, 0.16), 0.0, 0.42),
    "Lib_PileTop": ((0.50, 0.48, 0.42), 0.0, 0.48),
    "Lib_Mulch": ((0.361, 0.227, 0.18), 0.0, 0.15),
    "Lib_Foliage": ((0.15, 0.32, 0.11), 0.0, 0.08),
    "Lib_FoliageDark": ((0.08, 0.20, 0.08), 0.0, 0.08),
    "Lib_FoliageLite": ((0.26, 0.36, 0.09), 0.0, 0.08),
    "Lib_Needle": ((0.10, 0.24, 0.11), 0.0, 0.08),
    "Lib_Palm": ((0.13, 0.34, 0.11), 0.0, 0.10),
    "Lib_PalmDry": ((0.30, 0.32, 0.10), 0.0, 0.10),
    "Lib_Glass": ((0.62, 0.78, 0.82), 0.04, 0.88),
    "Lib_Orange": ((0.93, 0.40, 0.08), 0.0, 0.42),
    "Lib_ContainerRed": ((0.58, 0.16, 0.13), 0.18, 0.30),
    "Lib_ContainerBlue": ((0.12, 0.28, 0.48), 0.18, 0.30),
    "Lib_Black": ((0.07, 0.07, 0.08), 0.15, 0.40),
    # Dark powder coat. Flat color, not a baked tile.
    "Lib_Iron": ((0.09, 0.16, 0.10), 0.18, 0.42),
    "Lib_Rust": ((0.45, 0.24, 0.14), 0.28, 0.24),
    "Lib_Water": ((0.025, 0.07, 0.09), 0.02, 0.55),
    "Lib_Brass": ((0.74, 0.58, 0.28), 0.85, 0.55),
    "Lib_Chain": ((0.68, 0.70, 0.72), 0.62, 0.38),
    "Lib_Soil": ((0.28, 0.18, 0.10), 0.0, 0.12),
    "Lib_Siding": ((0.78, 0.80, 0.78), 0.0, 0.30),
    "Lib_Roof": ((0.28, 0.30, 0.32), 0.05, 0.25),
    # Ranch and boathouse courses. Several short staggered rows per metre.
    "Lib_RanchRoof": ((0.34, 0.26, 0.18), 0.04, 0.28),
    # Truncated-dome panel. Yellow field, the domes are the normal.
    "Lib_Warn": ((0.76, 0.52, 0.10), 0.0, 0.12),
    "Lib_Awning": ((0.55, 0.12, 0.16), 0.0, 0.28),
    "Lib_Court": ((0.16, 0.38, 0.62), 0.0, 0.30),
    "Lib_Lane": ((0.90, 0.82, 0.28), 0.0, 0.35),
    "Lib_Hydrant": ((0.62, 0.10, 0.07), 0.0, 0.28),
    "Lib_WoodWeather": ((0.45, 0.38, 0.28), 0.0, 0.18),
    "Lib_Lamp": ((1.0, 0.86, 0.55), 0.0, 0.90),
    "Lib_Window": ((0.14, 0.20, 0.26), 0.04, 0.82),
    # Display glass. Dark, slightly metallic, transmissive, and not a light panel.
    "Lib_ShopGlass": ((0.06, 0.09, 0.12), 0.22, 0.92),
    "Lib_WindowLit": ((0.55, 0.36, 0.16), 0.0, 0.40),
    "Lib_PaintCream": ((0.86, 0.78, 0.66), 0.0, 0.32),
    "Lib_PaintTeal": ((0.10, 0.36, 0.40), 0.0, 0.30),
    "Lib_Interior": ((0.18, 0.13, 0.10), 0.0, 0.45),
    "Lib_CourtDecal": ((0.16, 0.16, 0.17), 0.0, 0.18),
    "Lib_Bark": ((0.34, 0.24, 0.14), 0.0, 0.22),
    "Lib_MetalWorn": ((0.42, 0.40, 0.38), 0.55, 0.28),
    "Lib_CraneYellow": ((0.78, 0.62, 0.16), 0.15, 0.32),
    # Flat varnish. Not the baked wood tile, which reads blotchy on a curved hull.
    "Lib_Varnish": ((0.52, 0.34, 0.16), 0.0, 0.38),
    # Per-log shift, about ±8% value, so a wall is not one flat tan.
    "Lib_Log0": ((0.478, 0.313, 0.147), 0.0, 0.36),
    "Lib_Log1": ((0.545, 0.328, 0.148), 0.0, 0.36),
    "Lib_Log2": ((0.52, 0.34, 0.16), 0.0, 0.36),
    "Lib_Log3": ((0.50, 0.362, 0.148), 0.0, 0.36),
    "Lib_Log4": ((0.562, 0.367, 0.173), 0.0, 0.36),
    # Untextured board paint and container enamel. The baked tiles read as brick.
    "Lib_Board": ((0.63, 0.46, 0.29), 0.0, 0.32),
    "Lib_Batten": ((0.38, 0.24, 0.14), 0.0, 0.28),
    "Lib_BoxRed": ((0.55, 0.16, 0.12), 0.12, 0.34),
    "Lib_BoxBlue": ((0.12, 0.30, 0.50), 0.12, 0.34),
    "Lib_BoxGreen": ((0.15, 0.36, 0.24), 0.12, 0.34),
}

# Blender emission (color, strength). Unity gets the same color on _EmissionColor.
EMISSIVE = {
    "Lib_Lamp": ((1.0, 0.75, 0.38), 8.0),
    "Lib_Window": ((0.55, 0.75, 0.90), 0.22),
    "Lib_WindowLit": ((1.0, 0.68, 0.32), 0.20),
}

# Grayscale-or-color albedo multiplied is baked as full color. UV is meters.
TEXTURED = (
    "Lib_Brick", "Lib_Asphalt", "Lib_Wood", "Lib_WoodDark", "Lib_Concrete",
    "Lib_Siding", "Lib_Roof", "Lib_RanchRoof", "Lib_Warn", "Lib_Soil", "Lib_Hydrant", "Lib_WoodWeather",
    "Lib_CourtDecal", "Lib_Bark", "Lib_MetalWorn", "Lib_ContainerRed", "Lib_ContainerBlue",
    "Lib_CraneYellow", "Lib_LogEnd", "Lib_Pile",
)
NORMALS = (
    "Lib_Brick", "Lib_Roof", "Lib_Warn", "Lib_Water", "Lib_Concrete", "Lib_Wood", "Lib_WoodDark",
    "Lib_Asphalt", "Lib_Bark", "Lib_MetalWorn", "Lib_ContainerRed", "Lib_ContainerBlue",
    "Lib_CraneYellow", "Lib_LogEnd",
    "Lib_Log0", "Lib_Log1", "Lib_Log2", "Lib_Log3", "Lib_Log4",
)
ROUGHNESS = (
    "Lib_Brick", "Lib_Hydrant", "Lib_WoodWeather", "Lib_Asphalt", "Lib_Wood",
    "Lib_WoodDark", "Lib_Concrete", "Lib_Bark", "Lib_MetalWorn",
    "Lib_ContainerRed", "Lib_ContainerBlue", "Lib_CraneYellow",
)
AO = (
    "Lib_Brick", "Lib_Concrete", "Lib_Wood", "Lib_WoodDark", "Lib_Asphalt",
    "Lib_Bark", "Lib_MetalWorn", "Lib_ContainerRed", "Lib_ContainerBlue", "Lib_CraneYellow",
)

# Modular street kit. Straight tiles are ROAD_W wide and TILE_L long.
# Tops: road 0.12 m, sidewalk 0.27 m (15 cm curb). Pivot is ground center.
ROAD_W = 6.0
TILE_L = 4.0
ROAD_TOP = 0.12
WALK_TOP = 0.27
SIDE_W = 2.0

REGISTRY = []


def register(fn):
    REGISTRY.append(fn)
    return fn


def lod_pick(lod, hi, mid, low=None):
    if lod <= 0:
        return hi
    if lod == 1:
        return mid
    return low if low is not None else mid


def unity_to_blender(x, y, z):
    return (x, -z, y)


def blender_to_unity(x, y, z):
    return (x, z, -y)


def _rotl64(x, r):
    x &= 0xFFFFFFFFFFFFFFFF
    return ((x << r) | (x >> (64 - r))) & 0xFFFFFFFFFFFFFFFF


def xxh64(data, seed=0):
    """XXH64, seed 0. Same digest Unity uses for imported mesh fileIDs."""
    mask = 0xFFFFFFFFFFFFFFFF
    p1 = 0x9E3779B185EBCA87
    p2 = 0xC2B2AE3D27D4EB4F
    p3 = 0x165667B19E3779F9
    p4 = 0x85EBCA77C2B2AE63
    p5 = 0x27D4EB2F165667C5

    def u64(b):
        return int.from_bytes(b, "little")

    def round64(acc, inp):
        acc = (acc + (inp * p2)) & mask
        acc = _rotl64(acc, 31)
        return (acc * p1) & mask

    def merge(h, acc):
        h ^= round64(0, acc)
        return (h * p1 + p4) & mask

    length = len(data)
    if length >= 32:
        acc1 = (seed + p1 + p2) & mask
        acc2 = (seed + p2) & mask
        acc3 = seed & mask
        acc4 = (seed - p1) & mask
        i = 0
        while i + 32 <= length:
            acc1 = round64(acc1, u64(data[i:i + 8]))
            acc2 = round64(acc2, u64(data[i + 8:i + 16]))
            acc3 = round64(acc3, u64(data[i + 16:i + 24]))
            acc4 = round64(acc4, u64(data[i + 24:i + 32]))
            i += 32
        h = (_rotl64(acc1, 1) + _rotl64(acc2, 7) + _rotl64(acc3, 12) + _rotl64(acc4, 18)) & mask
        h = merge(merge(merge(merge(h, acc1), acc2), acc3), acc4)
    else:
        h = (seed + p5) & mask
        i = 0
    h = (h + length) & mask
    while i + 8 <= length:
        h ^= round64(0, u64(data[i:i + 8]))
        h = (_rotl64(h, 27) * p1 + p4) & mask
        i += 8
    if i + 4 <= length:
        h = (h ^ ((u64(data[i:i + 4]) * p1) & mask)) & mask
        h = (_rotl64(h, 23) * p2 + p3) & mask
        i += 4
    while i < length:
        h ^= (data[i] * p5) & mask
        h = (_rotl64(h, 11) * p1) & mask
        i += 1
    h ^= h >> 33
    h = (h * p2) & mask
    h ^= h >> 29
    h = (h * p3) & mask
    h ^= h >> 32
    return h


def _signed64(u):
    u &= (1 << 64) - 1
    return u - (1 << 64) if u >= (1 << 63) else u


def mesh_file_id(name):
    return _signed64(xxh64(("Type:Mesh->%s0" % name).encode("utf-8")))


def _quat_from_matrix(m):
    """Shepperd quaternion (x, y, z, w) from a 3x3 rotation."""
    t = m[0][0] + m[1][1] + m[2][2]
    if t > 0:
        s = math.sqrt(t + 1.0) * 2.0
        w = 0.25 * s
        x = (m[2][1] - m[1][2]) / s
        y = (m[0][2] - m[2][0]) / s
        z = (m[1][0] - m[0][1]) / s
    elif m[0][0] > m[1][1] and m[0][0] > m[2][2]:
        s = math.sqrt(1.0 + m[0][0] - m[1][1] - m[2][2]) * 2.0
        w = (m[2][1] - m[1][2]) / s
        x = 0.25 * s
        y = (m[0][1] + m[1][0]) / s
        z = (m[0][2] + m[2][0]) / s
    elif m[1][1] > m[2][2]:
        s = math.sqrt(1.0 + m[1][1] - m[0][0] - m[2][2]) * 2.0
        w = (m[0][2] - m[2][0]) / s
        x = (m[0][1] + m[1][0]) / s
        y = 0.25 * s
        z = (m[1][2] + m[2][1]) / s
    else:
        s = math.sqrt(1.0 + m[2][2] - m[0][0] - m[1][1]) * 2.0
        w = (m[1][0] - m[0][1]) / s
        x = (m[0][2] + m[2][0]) / s
        y = (m[1][2] + m[2][1]) / s
        z = 0.25 * s
    n = math.sqrt(x * x + y * y + z * z + w * w) or 1.0
    return (x / n, y / n, z / n, w / n)


def unity_euler_quat(euler_deg):
    """Unity Quaternion.Euler: Z then X then Y, left-handed axes."""
    x, y, z = [math.radians(v) * 0.5 for v in euler_deg]
    cx, sx = math.cos(x), math.sin(x)
    cy, sy = math.cos(y), math.sin(y)
    cz, sz = math.cos(z), math.sin(z)
    # q = qY * qX * qZ
    # qZ = (0, 0, sz, cz), qX = (sx, 0, 0, cx), qY = (0, sy, 0, cy)
    # First qX * qZ
    ax, ay, az, aw = sx * cz, sx * sz, cx * sz, cx * cz
    # Then qY * that
    qx = cy * ax + sy * az
    qy = sy * aw + cy * ay
    qz = cy * az - sy * ax
    qw = cy * aw - sy * ay
    return (qx, qy, qz, qw)


def _apply_unity_quat(q, p):
    x, y, z, w = q
    vx, vy, vz = p
    tx = 2.0 * (y * vz - z * vy)
    ty = 2.0 * (z * vx - x * vz)
    tz = 2.0 * (x * vy - y * vx)
    return (
        vx + w * tx + (y * tz - z * ty),
        vy + w * ty + (z * tx - x * tz),
        vz + w * tz + (x * ty - y * tx),
    )


class Geo:
    def __init__(self, lod):
        self.lod = lod
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.scale_layer = self.bm.faces.layers.float.new("uvscale")
        # 0 keeps the usual triplanar axes. 1 lays long grain on world X, 2 on world Z.
        self.grain_layer = self.bm.faces.layers.float.new("uvgrain")
        self.mats = []

    def slot(self, name):
        if name not in PALETTE:
            raise KeyError("unknown material " + name)
        if name not in self.mats:
            self.mats.append(name)
        return self.mats.index(name)

    def _ingest(self, src, mat, uv_scale, grain=0.0):
        mi = self.slot(mat)
        vmap = {}
        for v in src.verts:
            vmap[v] = self.bm.verts.new(v.co)
        self.bm.verts.index_update()
        src_uv = None
        if uv_scale < 0 and src.loops.layers.uv:
            src_uv = src.loops.layers.uv.active
        for f in src.faces:
            try:
                nf = self.bm.faces.new([vmap[v] for v in f.verts])
            except ValueError:
                continue
            nf.material_index = mi
            nf.smooth = True
            nf[self.scale_layer] = uv_scale
            nf[self.grain_layer] = grain
            if src_uv is not None:
                for loop, src_loop in zip(nf.loops, f.loops):
                    loop[self.uv].uv = src_loop[src_uv].uv[:]
        src.free()

    def _finish_src(self, bm, bevel, segs):
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        if bevel and segs and bevel > 0 and segs > 0:
            edges = [e for e in bm.edges if len(e.link_faces) == 2 and e.calc_face_angle(0.0) > math.radians(35)]
            if edges:
                # Keep the chamfer inside the smallest edge so parts do not balloon.
                limit = bevel
                for e in edges:
                    limit = min(limit, e.calc_length() * 0.35)
                if limit > 1e-5:
                    try:
                        bmesh.ops.bevel(
                            bm,
                            geom=edges,
                            offset=limit,
                            offset_type="OFFSET",
                            segments=int(segs),
                            profile=0.5,
                            affect="EDGES",
                            clamp_overlap=True,
                        )
                    except (TypeError, ValueError, RuntimeError):
                        pass
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        return bm

    def box(self, center, size, mat, bevel=0.0, segs=1, euler=(0, 0, 0), uv_scale=1.0, grain=0.0):
        sx, sy, sz = size
        if sx <= 0 or sy <= 0 or sz <= 0:
            return
        bm = bmesh.new()
        q = unity_euler_quat(euler)
        hx, hy, hz = sx * 0.5, sy * 0.5, sz * 0.5
        local = [
            (-hx, -hy, -hz), (hx, -hy, -hz), (hx, hy, -hz), (-hx, hy, -hz),
            (-hx, -hy, hz), (hx, -hy, hz), (hx, hy, hz), (-hx, hy, hz),
        ]
        verts = []
        for p in local:
            r = _apply_unity_quat(q, p)
            u = (r[0] + center[0], r[1] + center[1], r[2] + center[2])
            verts.append(bm.verts.new(unity_to_blender(*u)))
        # Outward winding in Unity (left-handed) becomes outward in Blender
        # because the axis change is a proper rotation. These faces were
        # authored CCW in Blender after the axis map of an axis-aligned cube.
        faces_idx = [
            (0, 1, 2, 3),  # checked below by recalc
            (4, 7, 6, 5),
            (0, 4, 5, 1),
            (1, 5, 6, 2),
            (2, 6, 7, 3),
            (3, 7, 4, 0),
        ]
        for idxs in faces_idx:
            bm.faces.new([verts[i] for i in idxs])
        self._finish_src(bm, bevel, segs)
        self._ingest(bm, mat, uv_scale, grain)

    def cylinder(self, center, radius, height, mat, segments=12, axis="Y", bevel=0.0, segs=1, uv_scale=1.0, cap_ends=True, grain=0.0):
        if radius <= 0 or height <= 0:
            return
        bm = bmesh.new()
        bmesh.ops.create_cone(
            bm,
            cap_ends=cap_ends,
            cap_tris=False,
            segments=max(3, int(segments)),
            radius1=radius,
            radius2=radius,
            depth=height,
        )
        if axis == "X":
            bmesh.ops.rotate(bm, verts=bm.verts, cent=Vector((0, 0, 0)), matrix=Matrix.Rotation(math.pi * 0.5, 4, "Y"))
        elif axis == "Z":
            bmesh.ops.rotate(bm, verts=bm.verts, cent=Vector((0, 0, 0)), matrix=Matrix.Rotation(math.pi * 0.5, 4, "X"))
        elif axis != "Y":
            raise ValueError(axis)
        # create_cone is Z-up in Blender, which is already Unity-Y after export
        # when axis == 'Y'. Rotations above are in Blender space and match
        # Unity X / Unity Z because of the axis map (see file header).
        bmesh.ops.translate(bm, verts=bm.verts, vec=Vector(unity_to_blender(*center)))
        self._finish_src(bm, bevel, segs)
        self._ingest(bm, mat, uv_scale, grain)

    def cylinder_bands(self, x, z, radius, bands, segments=8):
        """One closed vertical pile. bands is (y0, y1, material) from bottom to top."""
        if radius <= 0 or not bands:
            return
        seg = max(3, int(segments))
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new("UVMap")
        ys = [bands[0][0]]
        for _y0, y1, _mat in bands:
            ys.append(y1)
        rings = []
        for y in ys:
            ring = []
            for i in range(seg):
                ang = 2.0 * math.pi * i / seg
                ring.append(bm.verts.new(unity_to_blender(
                    x + radius * math.cos(ang), y, z + radius * math.sin(ang)
                )))
            rings.append(ring)
        span = max(1e-4, ys[-1] - ys[0])
        mats = []

        def side(i0, i1, y_a, y_b, mat):
            for i in range(seg):
                j = (i + 1) % seg
                face = bm.faces.new((rings[i0][i], rings[i0][j], rings[i1][j], rings[i1][i]))
                mats.append(mat)
                u0, u1 = i / seg, (i + 1) / seg
                v0 = (y_a - ys[0]) / span
                v1 = (y_b - ys[0]) / span
                coords = ((u0, v0), (u1, v0), (u1, v1), (u0, v1))
                for loop, uv_xy in zip(face.loops, coords):
                    loop[uv].uv = uv_xy

        for b, (y0, y1, mat) in enumerate(bands):
            side(b, b + 1, y0, y1, mat)
        cap0 = bm.faces.new(list(reversed(rings[0])))
        mats.append(bands[0][2])
        for loop in cap0.loops:
            loop[uv].uv = (0.5, 0.0)
        cap1 = bm.faces.new(rings[-1])
        mats.append(bands[-1][2])
        for loop in cap1.loops:
            loop[uv].uv = (0.5, 1.0)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        self._ingest_multi(bm, mats)

    def tube(self, points, radius, mat, segments=8):
        """One closed round tube along Unity points. Joints share rings, so segments do not overlap."""
        if radius <= 0 or len(points) < 2:
            return
        pts = [Vector(p) for p in points]
        seg = max(3, int(segments))
        bm = bmesh.new()
        rings = []
        for i, p in enumerate(pts):
            if i == 0:
                direction = pts[1] - pts[0]
            elif i == len(pts) - 1:
                direction = pts[-1] - pts[-2]
            else:
                direction = pts[i + 1] - pts[i - 1]
            if direction.length < 1e-6:
                continue
            direction.normalize()
            tmp = Vector((0.0, 0.0, 1.0)) if abs(direction.z) < 0.85 else Vector((1.0, 0.0, 0.0))
            x_axis = direction.cross(tmp).normalized()
            z_axis = x_axis.cross(direction).normalized()
            ring = []
            for s in range(seg):
                ang = 2.0 * math.pi * s / seg
                radial = x_axis * (math.cos(ang) * radius) + z_axis * (math.sin(ang) * radius)
                q = p + radial
                ring.append(bm.verts.new(unity_to_blender(q.x, q.y, q.z)))
            rings.append(ring)
        if len(rings) < 2:
            bm.free()
            return
        for i in range(len(rings) - 1):
            for s in range(seg):
                n = (s + 1) % seg
                bm.faces.new((rings[i][s], rings[i][n], rings[i + 1][n], rings[i + 1][s]))
        bm.faces.new(list(reversed(rings[0])))
        bm.faces.new(rings[-1])
        self._finish_src(bm, 0, 0)
        self._ingest(bm, mat, 1.0)

    def _ingest_multi(self, src, mats):
        vmap = {}
        for v in src.verts:
            vmap[v] = self.bm.verts.new(v.co)
        self.bm.verts.index_update()
        src_uv = src.loops.layers.uv.active if src.loops.layers.uv else None
        for f, mat in zip(src.faces, mats):
            try:
                nf = self.bm.faces.new([vmap[v] for v in f.verts])
            except ValueError:
                continue
            nf.material_index = self.slot(mat)
            nf.smooth = True
            nf[self.scale_layer] = -1.0
            nf[self.grain_layer] = 0.0
            if src_uv is not None:
                for loop, src_loop in zip(nf.loops, f.loops):
                    loop[self.uv].uv = src_loop[src_uv].uv[:]
        src.free()

    def cone(self, center, radius1, radius2, height, mat, segments=12, axis="Y", uv_scale=1.0):
        bm = bmesh.new()
        bmesh.ops.create_cone(
            bm,
            cap_ends=True,
            cap_tris=False,
            segments=max(3, int(segments)),
            radius1=radius1,
            radius2=radius2,
            depth=height,
        )
        if axis == "X":
            bmesh.ops.rotate(bm, verts=bm.verts, cent=Vector((0, 0, 0)), matrix=Matrix.Rotation(math.pi * 0.5, 4, "Y"))
        elif axis == "Z":
            bmesh.ops.rotate(bm, verts=bm.verts, cent=Vector((0, 0, 0)), matrix=Matrix.Rotation(math.pi * 0.5, 4, "X"))
        bmesh.ops.translate(bm, verts=bm.verts, vec=Vector(unity_to_blender(*center)))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        self._ingest(bm, mat, uv_scale)

    def sphere(self, center, radius, mat, segments=12, uv_scale=1.0):
        bm = bmesh.new()
        seg = max(4, int(segments))
        bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=max(4, seg // 2), radius=radius)
        bmesh.ops.translate(bm, verts=bm.verts, vec=Vector(unity_to_blender(*center)))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        self._ingest(bm, mat, uv_scale)

    def torus(self, center, major, minor, mat, major_seg=16, minor_seg=8, uv_scale=1.0):
        """Horizontal ring (rim) in the Unity XZ plane."""
        bm = bmesh.new()
        rings = []
        for i in range(major_seg):
            a = 2.0 * math.pi * i / major_seg
            ca, sa = math.cos(a), math.sin(a)
            ring = []
            for j in range(minor_seg):
                b = 2.0 * math.pi * j / minor_seg
                rad = major + minor * math.cos(b)
                ux = center[0] + rad * ca
                uy = center[1] + minor * math.sin(b)
                uz = center[2] + rad * sa
                ring.append(bm.verts.new(unity_to_blender(ux, uy, uz)))
            rings.append(ring)
        for i in range(major_seg):
            ni = (i + 1) % major_seg
            for j in range(minor_seg):
                nj = (j + 1) % minor_seg
                bm.faces.new((rings[i][j], rings[ni][j], rings[ni][nj], rings[i][nj]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        self._ingest(bm, mat, uv_scale)

    def pipe(self, a, b, radius, mat, segments=8, bevel=0.0, segs=0, uv_scale=1.0, grain=0.0):
        a = Vector(a)
        b = Vector(b)
        delta = b - a
        length = delta.length
        if length < 1e-5 or radius <= 0:
            return
        direction = delta / length
        tmp = Vector((0.0, 0.0, 1.0)) if abs(direction.z) < 0.85 else Vector((1.0, 0.0, 0.0))
        x_axis = direction.cross(tmp).normalized()
        z_axis = x_axis.cross(direction).normalized()
        bm = bmesh.new()
        rings = []
        steps = 2
        for s in range(steps):
            t = -0.5 + s
            origin = (a + b) * 0.5 + direction * (length * t)
            ring = []
            for i in range(segments):
                ang = 2.0 * math.pi * i / segments
                radial = x_axis * (math.cos(ang) * radius) + z_axis * (math.sin(ang) * radius)
                p = origin + radial
                ring.append(bm.verts.new(unity_to_blender(p.x, p.y, p.z)))
            rings.append(ring)
        for i in range(segments):
            ni = (i + 1) % segments
            bm.faces.new((rings[0][i], rings[0][ni], rings[1][ni], rings[1][i]))
        # caps
        bm.faces.new(list(reversed(rings[0])))
        bm.faces.new(rings[1])
        self._finish_src(bm, bevel, segs)
        self._ingest(bm, mat, uv_scale, grain)

    def blob(self, spheres, mat, voxel=0.12):
        """Union of (center, radius) spheres via a voxel remesh. One clean canopy."""
        src = bmesh.new()
        for center, radius in spheres:
            tmp = bmesh.new()
            seg = 10 if voxel < 0.15 else 6
            bmesh.ops.create_uvsphere(tmp, u_segments=seg, v_segments=max(4, seg // 2), radius=radius)
            bmesh.ops.translate(tmp, verts=tmp.verts, vec=Vector(unity_to_blender(*center)))
            vmap = {v: src.verts.new(v.co) for v in tmp.verts}
            src.verts.index_update()
            for f in tmp.faces:
                try:
                    src.faces.new([vmap[v] for v in f.verts])
                except ValueError:
                    pass
            tmp.free()
        me = bpy.data.meshes.new("blob")
        src.to_mesh(me)
        src.free()
        obj = bpy.data.objects.new("blob", me)
        bpy.context.scene.collection.objects.link(obj)
        mod = obj.modifiers.new("Remesh", "REMESH")
        mod.mode = "VOXEL"
        mod.voxel_size = voxel
        mod.use_smooth_shade = True
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier="Remesh")
        smooth = obj.modifiers.new("Smooth", "SMOOTH")
        smooth.iterations = 4
        bpy.ops.object.modifier_apply(modifier="Smooth")
        out = bmesh.new()
        out.from_mesh(obj.data)
        bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.meshes.remove(me)
        bmesh.ops.recalc_face_normals(out, faces=out.faces)
        self._ingest(out, mat, 1.0)

    def text(self, body, location, size, mat, extrude=0.008, yaw=0.0):
        """Centered text standing in the Unity XY plane, extruded toward +Z, then yawed."""
        curve = bpy.data.curves.new("LibText", "FONT")
        curve.body = body
        curve.align_x = "CENTER"
        curve.align_y = "CENTER"
        curve.size = size
        curve.extrude = extrude
        curve.resolution_u = 2
        obj = bpy.data.objects.new("LibText", curve)
        bpy.context.scene.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.convert(target="MESH")
        me = obj.data
        a = math.radians(yaw)
        ca, sa = math.cos(a), math.sin(a)
        bm = bmesh.new()
        vmap = []
        for v in me.vertices:
            x, y, z = v.co.x, v.co.y, v.co.z
            xr = x * ca + z * sa
            zr = -x * sa + z * ca
            u = (xr + location[0], y + location[1], zr + location[2])
            vmap.append(bm.verts.new(unity_to_blender(*u)))
        for poly in me.polygons:
            try:
                bm.faces.new([vmap[i] for i in poly.vertices])
            except ValueError:
                continue
        bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.meshes.remove(me)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        self._ingest(bm, mat, 1.0)

    def quad(self, verts, faces, uvs, mat):
        """Unity-space verts with authored UVs. Skips the meter projection."""
        bm = bmesh.new()
        uv_layer = bm.loops.layers.uv.new("UVMap")
        bverts = [bm.verts.new(unity_to_blender(*v)) for v in verts]
        for face in faces:
            nf = bm.faces.new([bverts[i] for i in face])
            for loop in nf.loops:
                idx = bverts.index(loop.vert)
                loop[uv_layer].uv = uvs[idx]
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        self._ingest(bm, mat, -1.0)

    def mesh(self, verts, faces, mat, uv_scale=1.0, bevel=0.0, segs=0, grain=0.0):
        """verts are Unity-space. faces are index tuples (quads preferred)."""
        bm = bmesh.new()
        bverts = [bm.verts.new(unity_to_blender(*v)) for v in verts]
        for f in faces:
            try:
                bm.faces.new([bverts[i] for i in f])
            except ValueError:
                continue
        self._finish_src(bm, bevel, segs)
        self._ingest(bm, mat, uv_scale, grain)

    def arc_pipe(self, center, radius, height0, height1, a0, a1, tube, mat, segments=8, steps=8):
        """Tube along a horizontal arc. Angles in degrees, 0 = +Z, 90 = +X."""
        pts = []
        for i in range(steps + 1):
            t = i / steps
            ang = math.radians(a0 + (a1 - a0) * t)
            y = height0 + (height1 - height0) * t
            # 0 deg faces +Z, 90 deg faces +X (Unity yaw).
            x = center[0] + radius * math.sin(ang)
            z = center[2] + radius * math.cos(ang)
            pts.append((x, y, z))
        for i in range(steps):
            self.pipe(pts[i], pts[i + 1], tube, mat, segments=segments)

    def prepare(self):
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=0.0004)
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        for e in self.bm.edges:
            if len(e.link_faces) == 2:
                e.smooth = e.calc_face_angle(0.0) < math.radians(48)
            else:
                e.smooth = False
        for f in self.bm.faces:
            f.smooth = True
        self._assign_uvs()

    def _assign_uvs(self):
        for f in self.bm.faces:
            scale = f[self.scale_layer]
            if scale < 0:
                continue
            if not scale:
                scale = 1.0
            grain = f[self.grain_layer]
            n = f.normal
            nu = Vector(blender_to_unity(n.x, n.y, n.z))
            if nu.length:
                nu.normalize()
            for loop in f.loops:
                c = loop.vert.co
                u = blender_to_unity(c.x, c.y, c.z)
                if abs(nu.y) >= abs(nu.x) and abs(nu.y) >= abs(nu.z):
                    uu, vv = u[0], u[2]
                elif abs(nu.x) >= abs(nu.z):
                    uu, vv = u[2], u[1]
                else:
                    uu, vv = u[0], u[1]
                # Bands vary along U, so long grain has to run along V.
                if grain >= 1.5:
                    if abs(nu.z) < 0.65 and abs(nu.y) < 0.65:
                        uu, vv = vv, uu
                elif grain >= 0.5:
                    if abs(nu.x) < 0.65:
                        uu, vv = vv, uu
                loop[self.uv].uv = (uu * scale, vv * scale)

    def tri_count(self):
        n = 0
        for f in self.bm.faces:
            n += max(0, len(f.verts) - 2)
        return n

    def unity_bounds(self):
        if not self.bm.verts:
            return (0, 0, 0), (0, 0, 0)
        xs, ys, zs = [], [], []
        for v in self.bm.verts:
            u = blender_to_unity(v.co.x, v.co.y, v.co.z)
            xs.append(u[0])
            ys.append(u[1])
            zs.append(u[2])
        return (min(xs), min(ys), min(zs)), (max(xs), max(ys), max(zs))


class Asset(object):
    def __init__(self, name, category, blurb):
        self.name = name
        self.category = category
        self.blurb = blurb
        self.lods = {}
        self._geo = None
        self.colliders = []
        self.climbable = False
        self.vaultable = False
        self.vault_height = 0.0
        self.climb_note = ""
        self.vault_note = ""
        self.allow_below = False
        self.warnings = []

    def begin(self, lod):
        self._geo = Geo(lod)
        return self._geo

    def end(self):
        g = self._geo
        g.prepare()
        self.lods[g.lod] = g
        self._geo = None

    def box(self, name, center, size, euler=(0, 0, 0), approx=False):
        col = {
            "name": name, "type": "box", "center": list(center), "size": list(size), "euler": list(euler),
        }
        if approx:
            # A thin slab across a grille or chain-link sheet. Holes are not a passage.
            col["approx"] = True
        self.colliders.append(col)

    def capsule(self, name, center, radius, height, direction=1):
        # Unity capsule height includes both hemispheres and must be >= 2r.
        height = max(height, radius * 2.0 + 0.001)
        self.colliders.append({
            "name": name, "type": "capsule", "center": list(center),
            "radius": float(radius), "height": float(height), "direction": int(direction),
        })

    def sphere(self, name, center, radius):
        self.colliders.append({
            "name": name, "type": "sphere", "center": list(center), "radius": float(radius),
        })


def _collider_samples(col, n_ring=8):
    """Surface samples only. Interior points false-trigger on nearby parts."""
    c = Vector(col["center"])
    if col["type"] == "box":
        sx, sy, sz = [v * 0.5 for v in col["size"]]
        q = unity_euler_quat(col.get("euler") or (0, 0, 0))
        pts = []
        for x in (-sx, sx):
            for y in (-sy, 0.0, sy):
                for z in (-sz, 0.0, sz):
                    r = _apply_unity_quat(q, (x, y, z))
                    pts.append((c.x + r[0], c.y + r[1], c.z + r[2]))
        for y in (-sy, sy):
            for z in (-sz, sz):
                r = _apply_unity_quat(q, (0.0, y, z))
                pts.append((c.x + r[0], c.y + r[1], c.z + r[2]))
        return pts
    if col["type"] == "sphere":
        r = col["radius"]
        return [
            (c.x + r, c.y, c.z), (c.x - r, c.y, c.z),
            (c.x, c.y + r, c.z), (c.x, c.y - r, c.z),
            (c.x, c.y, c.z + r), (c.x, c.y, c.z - r),
        ]
    r = col["radius"]
    h = col["height"]
    d = col["direction"]
    axis = [(1.0, 0.0, 0.0), (0.0, 1.0, 0.0), (0.0, 0.0, 1.0)][d]
    half = max(0.0, h * 0.5 - r)
    pts = []
    for sign in (-1.0, 1.0):
        origin = (
            c.x + axis[0] * (half + r) * sign,
            c.y + axis[1] * (half + r) * sign,
            c.z + axis[2] * (half + r) * sign,
        )
        pts.append(origin)
        ring_o = (
            c.x + axis[0] * half * sign,
            c.y + axis[1] * half * sign,
            c.z + axis[2] * half * sign,
        )
        for i in range(n_ring):
            ang = 2.0 * math.pi * i / n_ring
            if d == 1:
                ox, oy, oz = math.cos(ang) * r, 0.0, math.sin(ang) * r
            elif d == 0:
                ox, oy, oz = 0.0, math.cos(ang) * r, math.sin(ang) * r
            else:
                ox, oy, oz = math.cos(ang) * r, math.sin(ang) * r, 0.0
            pts.append((ring_o[0] + ox, ring_o[1] + oy, ring_o[2] + oz))
    return pts


def _point_inside(bvh, blender_point):
    origin = Vector(blender_point)
    direction = Vector((1.0, 0.17, 0.09)).normalized()
    hits = 0
    # A ray through a brick pier can cross more than a dozen faces.
    for _ in range(64):
        loc, _normal, _idx, _dist = bvh.ray_cast(origin, direction)
        if loc is None:
            break
        hits += 1
        origin = loc + direction * 0.0008
    return hits % 2 == 1


def validate_colliders(asset, tolerance=0.03):
    geo = asset.lods.get(0)
    if geo is None or not geo.bm.verts:
        return
    bvh = BVHTree.FromBMesh(geo.bm)
    worst = 0.0
    worst_name = ""
    for col in asset.colliders:
        if col.get("approx"):
            continue
        center = Vector(col["center"])
        for p in _collider_samples(col):
            # Step inward so a collider that kisses the surface still counts as inside.
            inward = Vector(p) + (center - Vector(p)).normalized() * min(0.008, tolerance)
            bp = Vector(unity_to_blender(inward.x, inward.y, inward.z))
            if _point_inside(bvh, bp):
                continue
            loc, _normal, _idx, dist = bvh.find_nearest(bp, 4.0)
            outside = dist if loc is not None else 1.0
            if outside > worst:
                worst = outside
                worst_name = col["name"]
            if outside > tolerance:
                asset.warnings.append(
                    "%s sticks out %.1f cm (limit %.1f cm)" % (col["name"], outside * 100.0, tolerance * 100.0)
                )
                break
    asset.collider_slack_cm = round(worst * 100.0, 2)
    asset.collider_slack_name = worst_name


def _reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def _link_image(nt, bsdf, path, socket, non_color=False):
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(path)
    tex.interpolation = "Smart"
    if non_color:
        try:
            tex.image.colorspace_settings.name = "Non-Color"
        except (TypeError, AttributeError):
            pass
    nt.links.new(tex.outputs["Color"], bsdf.inputs[socket])
    return tex


def _multiply_ao(nt, bsdf, path):
    """Darken the albedo by the baked occlusion so mortar and cracks read in stills."""
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(path)
    try:
        tex.image.colorspace_settings.name = "Non-Color"
    except (TypeError, AttributeError):
        pass
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 0.85
    base = bsdf.inputs["Base Color"]
    src = None
    for link in list(nt.links):
        if link.to_socket == base:
            src = link.from_socket
            nt.links.remove(link)
            break
    if src is not None:
        nt.links.new(src, mix.inputs["A"])
    else:
        mix.inputs["A"].default_value = tuple(base.default_value)
    nt.links.new(tex.outputs["Color"], mix.inputs["B"])
    nt.links.new(mix.outputs["Result"], base)


def _ensure_materials():
    for name, (color, metal, smooth) in PALETTE.items():
        mat = bpy.data.materials.get(name)
        if mat is None:
            mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        mat.diffuse_color = (color[0], color[1], color[2], 1.0)
        nt = mat.node_tree
        bsdf = nt.nodes.get("Principled BSDF")
        if bsdf is None:
            continue
        bsdf.inputs["Base Color"].default_value = (color[0], color[1], color[2], 1.0)
        bsdf.inputs["Metallic"].default_value = metal
        bsdf.inputs["Roughness"].default_value = 1.0 - smooth
        img_path = os.path.join(TEX_DIR, name + ".png")
        if name in TEXTURED and os.path.isfile(img_path):
            _link_image(nt, bsdf, img_path, "Base Color")
        rough_path = os.path.join(TEX_DIR, name + "_R.png")
        if name in ROUGHNESS and os.path.isfile(rough_path):
            _link_image(nt, bsdf, rough_path, "Roughness", non_color=True)
        normal_path = os.path.join(TEX_DIR, name + "_N.png")
        if name in NORMALS and os.path.isfile(normal_path):
            tex = _link_image(nt, bsdf, normal_path, "Normal", non_color=True)
            # _link_image wired Color into Normal; replace with a Normal Map node.
            for link in list(nt.links):
                if link.from_node == tex and link.to_socket == bsdf.inputs["Normal"]:
                    nt.links.remove(link)
            nmap = nt.nodes.new("ShaderNodeNormalMap")
            nmap.inputs["Strength"].default_value = 1.2
            nt.links.new(tex.outputs["Color"], nmap.inputs["Color"])
            nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
        ao_path = os.path.join(TEX_DIR, name + "_AO.png")
        if name in AO and os.path.isfile(ao_path):
            _multiply_ao(nt, bsdf, ao_path)
        if name in EMISSIVE:
            emit, strength = EMISSIVE[name]
            color_socket = "Emission Color" if "Emission Color" in bsdf.inputs else "Emission"
            bsdf.inputs[color_socket].default_value = (emit[0], emit[1], emit[2], 1.0)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = strength
        if name == "Lib_Water" and "Transmission Weight" in bsdf.inputs:
            bsdf.inputs["Transmission Weight"].default_value = 0.22
            bsdf.inputs["Roughness"].default_value = 0.18
        if name == "Lib_Window" and "Transmission Weight" in bsdf.inputs:
            bsdf.inputs["Transmission Weight"].default_value = 0.55
            bsdf.inputs["Roughness"].default_value = 0.06
        if name == "Lib_ShopGlass" and "Transmission Weight" in bsdf.inputs:
            bsdf.inputs["Transmission Weight"].default_value = 0.48
            bsdf.inputs["Roughness"].default_value = 0.07
            if "IOR" in bsdf.inputs:
                bsdf.inputs["IOR"].default_value = 1.45


def _object_from_geo(geo, name):
    me = bpy.data.meshes.new(name)
    geo.bm.to_mesh(me)
    for slot in geo.mats:
        m = bpy.data.materials.get(slot)
        me.materials.append(m if m else bpy.data.materials.new(slot))
    # Sharp edges from the bmesh smooth flags survive to_mesh as use_edge_sharp
    # when the mesh has custom split normals disabled. Mark them explicitly.
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def export_fbx(asset):
    _reset_scene()
    _ensure_materials()
    folder = os.path.join(LIB_ROOT, asset.category)
    os.makedirs(folder, exist_ok=True)
    objects = []
    for lod in sorted(asset.lods):
        obj = _object_from_geo(asset.lods[lod], "LOD%d" % lod)
        objects.append(obj)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    path = os.path.join(folder, asset.name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_NONE",
        bake_space_transform=True,
        axis_forward="-Z",
        axis_up="Y",
        mesh_smooth_type="EDGE",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        object_types={"MESH"},
        use_triangles=False,
        path_mode="AUTO",
    )
    return path


def bounds_of(asset):
    geo = asset.lods[0]
    return geo.unity_bounds()


def size_of(mn, mx):
    return (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2])


def manifest_entry(asset, fbx_names):
    mn, mx = bounds_of(asset)
    size = size_of(mn, mx)
    lods = []
    for lod in sorted(asset.lods):
        g = asset.lods[lod]
        lods.append({
            "lod": lod,
            "tris": g.tri_count(),
            "materials": list(g.mats),
            "mesh": "LOD%d" % lod,
            "fileID": mesh_file_id("LOD%d" % lod),
        })
    return {
        "name": asset.name,
        "category": asset.category,
        "blurb": asset.blurb,
        "size": [round(size[0], 3), round(size[1], 3), round(size[2], 3)],
        "min": [round(mn[0], 3), round(mn[1], 3), round(mn[2], 3)],
        "max": [round(mx[0], 3), round(mx[1], 3), round(mx[2], 3)],
        "lods": lods,
        "colliders": asset.colliders,
        "climbable": asset.climbable,
        "vaultable": asset.vaultable,
        "vaultHeight": asset.vault_height,
        "climbNote": asset.climb_note,
        "vaultNote": asset.vault_note,
        "warnings": asset.warnings,
        "fbxNames": fbx_names,
        "slackCm": getattr(asset, "collider_slack_cm", 0),
    }


def fbx_model_names(path):
    data = open(path, "rb").read()
    names = []
    # Model display names show up as printable tokens. Keep LOD* hits.
    token = b""
    found = []
    for byte in data:
        if 32 <= byte < 127:
            token += bytes((byte,))
        else:
            if token.startswith(b"LOD") and len(token) <= 8:
                found.append(token.decode())
            token = b""
    # unique preserve order
    for n in found:
        if n not in names:
            names.append(n)
    return names


def check_pivot(asset):
    geo = asset.lods[0]
    xs, zs = [], []
    ys = []
    for v in geo.bm.verts:
        u = blender_to_unity(v.co.x, v.co.y, v.co.z)
        ys.append(u[1])
        if u[1] < 0.25:
            xs.append(u[0])
            zs.append(u[2])
    if xs and not getattr(asset, "loose_pivot", False):
        cx = (min(xs) + max(xs)) * 0.5
        cz = (min(zs) + max(zs)) * 0.5
        if abs(cx) > 0.05 or abs(cz) > 0.05:
            asset.warnings.append("ground footprint off center by (%.3f, %.3f)" % (cx, cz))
    if ys:
        if not asset.allow_below and min(ys) < -0.02:
            asset.warnings.append("mesh extends %.3f m below the ground pivot" % (-min(ys)))
        if not asset.allow_below and not getattr(asset, "allow_float", False) and min(ys) > 0.03:
            asset.warnings.append("mesh floats %.3f m above the ground pivot" % min(ys))


# --- textures --------------------------------------------------------------

def _hash01(ix, iy, salt):
    n = (ix * 374761393 + iy * 668265263 + salt * 1442695041) & 0xFFFFFFFF
    n = (n ^ (n >> 13)) * 1274126177 & 0xFFFFFFFF
    n = n ^ (n >> 16)
    return (n & 0xFFFF) / 65535.0


def _value_noise(x, y, salt=0):
    x0, y0 = math.floor(x), math.floor(y)
    fx, fy = x - x0, y - y0
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)
    v00 = _hash01(x0, y0, salt)
    v10 = _hash01(x0 + 1, y0, salt)
    v01 = _hash01(x0, y0 + 1, salt)
    v11 = _hash01(x0 + 1, y0 + 1, salt)
    return (v00 * (1 - fx) + v10 * fx) * (1 - fy) + (v01 * (1 - fx) + v11 * fx) * fy


def _save_image(name, w, h, fn):
    os.makedirs(TEX_DIR, exist_ok=True)
    path = os.path.join(TEX_DIR, name + ".png")
    img = bpy.data.images.new(name, w, h, alpha=False, float_buffer=False)
    buf = [0.0] * (w * h * 4)
    for y in range(h):
        for x in range(w):
            r, g, b = fn(x, y, w, h)
            i = ((y * w) + x) * 4
            buf[i] = r
            buf[i + 1] = g
            buf[i + 2] = b
            buf[i + 3] = 1.0
    img.pixels.foreach_set(buf)
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)
    return path


def _brick_pixel(x, y, w, h):
    # 1m tile, 4 bricks across, 8 courses. Running bond.
    u = x / w
    v = y / h
    course = v * 8.0
    ci = math.floor(course)
    fy = course - ci
    row_off = 0.5 if (ci % 2) else 0.0
    u2 = (u + row_off) % 1.0
    fx = (u2 * 4.0) % 1.0
    mortar = fy < 0.14 or fy > 0.97 or fx < 0.07 or fx > 0.97
    n = _value_noise(u * 6.0, v * 10.0, 3)
    if mortar:
        # Recessed joint. Darker than the face so the bond reads without the normal map.
        base = 0.38 + n * 0.05
        return (base, base * 0.97, base * 0.92)
    tint = 0.78 + n * 0.28
    # warm brick, slight per-brick shift
    brick_n = _hash01(math.floor(u2 * 4.0), ci, 9)
    r = (0.55 + brick_n * 0.18) * tint
    g = (0.24 + brick_n * 0.06) * tint
    b = (0.16 + brick_n * 0.03) * tint
    return (r, g, b)


def _asphalt_pixel(x, y, w, h):
    u = x / w
    v = y / h
    n = _value_noise(u * 18.0, v * 18.0, 1)
    n2 = _value_noise(u * 60.0, v * 60.0, 2)
    speckle = 1.0 if _hash01(x, y, 5) > 0.92 else 0.0
    vcol = 0.18 + n * 0.08 + n2 * 0.05 + speckle * 0.25
    return (vcol, vcol, vcol * 1.02)


def _wood_pixel(x, y, w, h, dark=False):
    u = x / w
    v = y / h
    board = v * 6.0
    bi = math.floor(board)
    fy = board - bi
    gap = fy < 0.06 or fy > 0.97
    grain = _value_noise(u * 2.0 + bi * 0.17, v * 28.0, 4)
    knot = _value_noise(u * 8.0, v * 8.0, 8 + bi)
    if dark:
        r, g, b = 0.32, 0.20, 0.11
    else:
        r, g, b = 0.55, 0.36, 0.20
    if gap:
        return (r * 0.45, g * 0.45, b * 0.45)
    m = 0.82 + grain * 0.28
    if knot > 0.82:
        m *= 0.75
    return (r * m, g * m, b * m)


def _concrete_pixel(x, y, w, h):
    u = x / w
    v = y / h
    n = _value_noise(u * 10.0, v * 10.0, 6)
    n2 = _value_noise(u * 40.0, v * 40.0, 7)
    pit = 0.85 if _hash01(x // 2, y // 2, 11) > 0.985 else 1.0
    vcol = (0.72 + n * 0.08 + n2 * 0.04) * pit
    return (vcol, vcol * 0.995, vcol * 0.97)


def _siding_pixel(x, y, w, h):
    u = x / w
    v = y / h
    course = v * 7.0
    fy = course - math.floor(course)
    shadow = 0.55 if fy < 0.08 else 1.0
    n = _value_noise(u * 4.0, v * 3.0, 12)
    base = 0.82 + n * 0.06
    return (base * shadow, base * shadow, base * 0.98 * shadow)


def _shake_at(u, v):
    """One exposed course fills the tile. Widths are 8–18 cm, not a brick bond.

    v is 0 at the thick butt and 1 at the thin head. u is metres across the course.
    """
    u2 = u % 1.0
    cursor = 0.0
    i = 0
    si = 0
    fx = 0.5
    width = 0.12
    while cursor < u2 + 1e-6 and i < 16:
        width = 0.08 + _hash01(i, 0, 11) * 0.10
        if _hash01(i, 0, 12) > 0.72:
            width = 0.06 + _hash01(i, 0, 13) * 0.04
        nxt = cursor + width
        if cursor <= u2 < nxt or nxt >= 1.0:
            si = i
            fx = (u2 - cursor) / max(width, 1e-4)
            break
        cursor = nxt
        i += 1
        si = i
    # The butt edge wanders a couple of centimetres so it is not a sawn line.
    wobble = (_hash01(si, 1, 9) - 0.5) * 0.05 + (_value_noise(u * 28.0, 0.2, 6) - 0.5) * 0.03
    fy = v
    return 0, fy, si, fx, wobble


def _roof_height(x, y, w, h):
    """Thick at the butt, feathered at the head, with a groove at each joint."""
    _ci, fy, _si, fx, wobble = _shake_at(x / float(w), y / float(h))
    side = min(fx, 1.0 - fx)
    butt = 0.16 + wobble
    if fy < butt or side < 0.04:
        return 0.02
    rise = min(fy - butt, side - 0.04, 0.25)
    # Taper: the head is thinner than the middle of the shake.
    taper = 1.0 - 0.35 * max(0.0, fy - 0.55)
    return (0.25 + 0.75 * max(0.0, min(1.0, rise * 7.0))) * taper


def _roof_normal_pixel(x, y, w, h):
    hx = _roof_height(x + 1, y, w, h) - _roof_height(x - 1, y, w, h)
    hy = _roof_height(x, y + 1, w, h) - _roof_height(x, y - 1, w, h)
    nx, ny, nz = -hx * 2.4, -hy * 2.4, 1.0
    length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    return (nx / length * 0.5 + 0.5, ny / length * 0.5 + 0.5, nz / length * 0.5 + 0.5)


def _ranch_roof_pixel(x, y, w, h):
    """Several short courses per metre, each shifted so the joints do not stack.

    v is metres up the slope. One tile is one metre, so a course is about 12–17 cm,
    not the single wide band of Lib_Roof.
    """
    u = x / float(w)
    v = y / float(h)
    exposures = (0.13, 0.16, 0.12, 0.15, 0.14, 0.17, 0.13)
    fv = v % 1.0
    acc = 0.0
    ci = 0
    fy = 0.0
    for i, exp in enumerate(exposures):
        if fv < acc + exp or i == len(exposures) - 1:
            ci = i
            fy = (fv - acc) / max(exp, 1e-4)
            break
        acc += exp
    offset = _hash01(ci, 3, 21) * 0.55
    _c, _f, si, fx, wobble = _shake_at((u + offset) % 1.0, fy)
    n = _hash01(si + ci * 3, 2, 14)
    weather = _hash01(ci, 5, 15)
    grain = 0.78 + _value_noise(fx * 1.6 + si * 0.13, fy * 40.0, 18) * 0.22
    tone = 0.92 + _hash01(ci, 6, 4) * 0.16
    brown = (0.42 + n * 0.10, 0.25 + n * 0.05, 0.13 + n * 0.03)
    grey = (0.48 + n * 0.08, 0.44 + n * 0.06, 0.38 + n * 0.04)
    t = 0.18 + weather * 0.55
    r = (brown[0] * (1.0 - t) + grey[0] * t) * grain * tone
    g = (brown[1] * (1.0 - t) + grey[1] * t) * grain * tone
    b = (brown[2] * (1.0 - t) + grey[2] * t) * grain * tone
    butt = 0.14 + wobble * 0.25
    if fy < butt * 0.45:
        shade = 0.22
    elif fy < butt:
        shade = 0.46
    else:
        shade = 1.0
    if fx < 0.04 or fx > 0.96:
        shade *= 0.42
    return (r * shade, g * shade, b * shade)


def _roof_pixel(x, y, w, h):
    """One cedar course: long vertical grain, random narrow widths, a thick dark butt."""
    u = x / float(w)
    v = y / float(h)
    _ci, fy, si, fx, wobble = _shake_at(u, v)
    n = _hash01(si, 2, 14)
    weather = _hash01(si, 2, 15)
    # Grain runs up the shake. A little wander, no cross-grain brick joint.
    grain = 0.78 + _value_noise(fx * 1.4 + si * 0.17, fy * 46.0, 18) * 0.28
    streak = 0.92 + 0.08 * math.sin((fy * 70.0) + _hash01(si, 4, 2) * 6.0)
    brown = (0.40 + n * 0.12, 0.24 + n * 0.05, 0.12 + n * 0.03)
    grey = (0.50 + n * 0.08, 0.46 + n * 0.06, 0.40 + n * 0.04)
    t = 0.22 + weather * 0.70
    r = (brown[0] * (1.0 - t) + grey[0] * t) * grain * streak
    g = (brown[1] * (1.0 - t) + grey[1] * t) * grain * streak
    b = (brown[2] * (1.0 - t) + grey[2] * t) * grain * streak
    butt = 0.15 + wobble
    if fy < butt * 0.55:
        shade = 0.18
    elif fy < butt:
        shade = 0.40
    elif fy > 0.92:
        shade = 0.82
    else:
        shade = 1.0
    if fx < 0.045 or fx > 0.955:
        shade *= 0.35
    return (r * shade, g * shade, b * shade)


def _pile_pixel(x, y, w, h):
    """Vertical grain around a round pile. No hoop seams."""
    u = x / float(w)
    v = y / float(h)
    stripe = 0.5 + 0.5 * math.sin(u * math.pi * 28.0 + _value_noise(u * 4.0, v * 0.6, 3) * 1.6)
    fine = _value_noise(u * 18.0, v * 2.0, 8)
    r = 0.34 + stripe * 0.14 + fine * 0.04
    g = 0.22 + stripe * 0.08 + fine * 0.03
    b = 0.11 + stripe * 0.04 + fine * 0.02
    return (r, g, b)


def _warn_height(x, y, w, h):
    """Truncated domes on a 60 mm grid. One tile is one metre."""
    u = (x / float(w)) % 1.0
    v = (y / float(h)) % 1.0
    pitch = 0.060
    fx = (u % pitch) / pitch - 0.5
    fy = (v % pitch) / pitch - 0.5
    d = math.hypot(fx, fy)
    if d > 0.30:
        return 0.15
    return 0.15 + 0.85 * (1.0 - d / 0.30)


def _warn_normal_pixel(x, y, w, h):
    hx = _warn_height(x + 1, y, w, h) - _warn_height(x - 1, y, w, h)
    hy = _warn_height(x, y + 1, w, h) - _warn_height(x, y - 1, w, h)
    nx, ny, nz = -hx * 1.6, -hy * 1.6, 1.0
    length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    return (nx / length * 0.5 + 0.5, ny / length * 0.5 + 0.5, nz / length * 0.5 + 0.5)


def _warn_pixel(x, y, w, h):
    u = x / float(w)
    v = y / float(h)
    n = _value_noise(u * 8.0, v * 8.0, 27)
    r = 0.72 + n * 0.06
    g = 0.48 + n * 0.04
    b = 0.08 + n * 0.02
    return (r, g, b)


def _soil_pixel(x, y, w, h):
    u = x / w
    v = y / h
    n = _value_noise(u * 12.0, v * 12.0, 15)
    r = 0.26 + n * 0.08
    g = 0.16 + n * 0.04
    b = 0.08 + n * 0.02
    return (r, g, b)


def _brick_height(px, py, w, h):
    u = px / float(w)
    v = py / float(h)
    course = v * 8.0
    ci = math.floor(course)
    fy = course - ci
    row_off = 0.5 if int(ci) % 2 else 0.0
    u2 = (u + row_off) % 1.0
    fx = (u2 * 4.0) % 1.0
    if fy < 0.14 or fx < 0.07:
        return 0.0
    edge = min(fy - 0.14, 1.0 - fy, fx - 0.07, 1.0 - fx)
    return 0.25 + 0.75 * max(0.0, min(1.0, edge * 10.0))


def _brick_normal_pixel(x, y, w, h):
    hx = _brick_height(x + 1, y, w, h) - _brick_height(x - 1, y, w, h)
    hy = _brick_height(x, y + 1, w, h) - _brick_height(x, y - 1, w, h)
    nx, ny, nz = -hx * 3.5, -hy * 3.5, 1.0
    length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    return (nx / length * 0.5 + 0.5, ny / length * 0.5 + 0.5, nz / length * 0.5 + 0.5)


def _hydrant_pixel(x, y, w, h):
    u = x / float(w)
    v = y / float(h)
    n = _value_noise(u * 7.0, v * 9.0, 21)
    scratch = _value_noise(u * 46.0, v * 5.0, 22)
    grime = max(0.0, 0.28 - v) / 0.28
    chip = 0.22 if scratch > 0.74 else 0.0
    r = 0.58 + n * 0.10 - grime * 0.30 + chip
    g = 0.07 + n * 0.03 - grime * 0.03 + chip * 0.7
    b = 0.05 + chip * 0.4
    return (max(0.02, min(1.0, r)), max(0.01, min(1.0, g)), max(0.01, min(1.0, b)))


def _hydrant_rough_pixel(x, y, w, h):
    u = x / float(w)
    v = y / float(h)
    grime = max(0.0, 0.28 - v) / 0.28
    n = _value_noise(u * 12.0, v * 12.0, 23)
    rough = 0.42 + grime * 0.40 + n * 0.12
    return (rough, rough, rough)


def _wood_weather_pixel(x, y, w, h):
    u = x / float(w)
    v = y / float(h)
    grain = _value_noise(u * 2.2, v * 22.0, 31)
    gray = _value_noise(u * 5.0, v * 3.0, 32)
    nail = 0.35 if _hash01(int(u * 8), int(v * 14), 33) > 0.97 else 1.0
    r = (0.42 + grain * 0.12) * (0.75 + gray * 0.35) * nail
    g = (0.36 + grain * 0.08) * (0.78 + gray * 0.30) * nail
    b = (0.28 + grain * 0.05) * (0.82 + gray * 0.25) * nail
    return (r, g, b)


def _generic_rough_pixel(x, y, w, h, salt, base):
    n = _value_noise(x / float(w) * 10.0, y / float(h) * 10.0, salt)
    rough = base + (n - 0.5) * 0.18
    rough = max(0.05, min(0.95, rough))
    return (rough, rough, rough)


def _water_height(x, y, w, h):
    u = x / float(w) * 9.0
    v = y / float(h) * 9.0
    return (
        math.sin(u * 6.2 + v * 1.4) * 0.45
        + math.sin(v * 5.1 + u * 0.6) * 0.35
        + math.sin((u + v) * 11.0) * 0.12
    )


def _water_normal_pixel(x, y, w, h):
    hx = _water_height(x + 1, y, w, h) - _water_height(x - 1, y, w, h)
    hy = _water_height(x, y + 1, w, h) - _water_height(x, y - 1, w, h)
    nx, ny, nz = -hx * 1.4, -hy * 1.4, 1.0
    length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    return (nx / length * 0.5 + 0.5, ny / length * 0.5 + 0.5, nz / length * 0.5 + 0.5)


def _log_height(x, y, w, h):
    """Subtle vertical checking. Cracks run with the grain, not across it."""
    u = x / float(w)
    v = y / float(h)
    fx = (u * 11.0) % 1.0
    crack = 0.22 if fx < 0.045 or fx > 0.955 else 0.0
    fine = _value_noise(u * 5.0, v * 16.0, 31) * 0.08
    return 0.55 + fine - crack


def _log_normal_pixel(x, y, w, h):
    hx = _log_height(x + 1, y, w, h) - _log_height(x - 1, y, w, h)
    hy = _log_height(x, y + 1, w, h) - _log_height(x, y - 1, w, h)
    nx, ny, nz = -hx * 0.9, -hy * 0.9, 1.0
    length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    return (nx / length * 0.5 + 0.5, ny / length * 0.5 + 0.5, nz / length * 0.5 + 0.5)


def generate_textures():
    from _court_decal import render as render_court
    from _bake_pbr import bake_all
    render_court(os.path.join(TEX_DIR, "Lib_CourtDecal.png"))
    bake_all()
    _reset_scene()
    w = h = 256
    # Siding, roof, soil, hydrant, and weathered dock planks stay on the
    # small procedural tiles. Brick, concrete, wood, asphalt, bark, and the
    # worn metals are the node-baked sets from _bake_pbr.
    _save_image("Lib_Siding", w, h, _siding_pixel)
    _save_image("Lib_Roof", 512, 256, _roof_pixel)
    _save_image("Lib_Roof_N", 512, 256, _roof_normal_pixel)
    _save_image("Lib_RanchRoof", 512, 512, _ranch_roof_pixel)
    _save_image("Lib_Pile", w, h, _pile_pixel)
    _save_image("Lib_Warn", w, h, _warn_pixel)
    _save_image("Lib_Warn_N", w, h, _warn_normal_pixel)
    _save_image("Lib_Soil", w, h, _soil_pixel)
    _save_image("Lib_Hydrant", w, h, _hydrant_pixel)
    _save_image("Lib_Hydrant_R", w, h, _hydrant_rough_pixel)
    _save_image("Lib_WoodWeather", w, h, _wood_weather_pixel)
    _save_image("Lib_WoodWeather_R", w, h, lambda x, y, W, H: _generic_rough_pixel(x, y, W, H, 44, 0.78))
    _save_image("Lib_Water_N", w, h, _water_normal_pixel)
    for i in range(5):
        _save_image("Lib_Log%d_N" % i, w, h, _log_normal_pixel)


def load_asset_modules():
    if ROOT not in sys.path:
        sys.path.insert(0, ROOT)
    skip = {
        "_common", "build_all", "render_pass1", "render_pass2", "render_pass3", "render_pass4", "render_pass5", "render_pass6", "render_pass7", "render_pass8", "render_pass9", "render_pass10", "render_pass11", "render_pass12", "render_pass13", "render_pass14", "render_pass15", "render_pass16", "render_pass17", "render_pass18", "render_pass19", "render_pass20", "render_pass21", "render_pass22", "render_pass23", "render_pass24", "render_pass25", "render_pass26", "render_pass27", "render_pass28", "render_pass29",
        "write_unity", "_kit",
    }
    names = []
    for fn in sorted(os.listdir(ROOT)):
        if not fn.endswith(".py"):
            continue
        stem = fn[:-3]
        if stem in skip or stem.startswith("_"):
            continue
        names.append(stem)
    import importlib
    for stem in names:
        importlib.import_module(stem)
    return names
