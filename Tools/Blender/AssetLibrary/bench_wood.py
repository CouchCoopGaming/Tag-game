"""Park bench. One cast-iron end profile, seat at 0.45 m."""

import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _fillet_path(corners, radius, segs):
    """Round each interior corner of a 2D polyline."""
    pts = [corners[0]]
    for i in range(1, len(corners) - 1):
        p0, p1, p2 = corners[i - 1], corners[i], corners[i + 1]
        v1 = (p1[0] - p0[0], p1[1] - p0[1])
        v2 = (p2[0] - p1[0], p2[1] - p1[1])
        l1 = math.hypot(v1[0], v1[1]) or 1.0
        l2 = math.hypot(v2[0], v2[1]) or 1.0
        u1 = (v1[0] / l1, v1[1] / l1)
        u2 = (v2[0] / l2, v2[1] / l2)
        dot = max(-1.0, min(1.0, u1[0] * u2[0] + u1[1] * u2[1]))
        cross = u1[0] * u2[1] - u1[1] * u2[0]
        delta = math.atan2(cross, dot)
        if abs(delta) < 0.08:
            pts.append(p1)
            continue
        cut = min(radius, l1 * 0.45, l2 * 0.45)
        arc_r = cut / math.tan(abs(delta) * 0.5)
        a = (p1[0] - u1[0] * cut, p1[1] - u1[1] * cut)
        side = 1.0 if delta > 0.0 else -1.0
        left = (-u1[1], u1[0])
        center = (a[0] + left[0] * arc_r * side, a[1] + left[1] * arc_r * side)
        b = (p1[0] + u2[0] * cut, p1[1] + u2[1] * cut)
        ang0 = math.atan2(a[1] - center[1], a[0] - center[0])
        ang1 = math.atan2(b[1] - center[1], b[0] - center[0])
        sweep = ang1 - ang0
        while sweep > math.pi:
            sweep -= 2.0 * math.pi
        while sweep < -math.pi:
            sweep += 2.0 * math.pi
        steps = segs if segs > 1 else 2
        for s in range(steps + 1):
            ang = ang0 + sweep * (s / float(steps))
            pts.append((center[0] + math.cos(ang) * arc_r, center[1] + math.sin(ang) * arc_r))
    pts.append(corners[-1])
    return pts


def _fillet_loop(corners, radius, segs):
    """Fillet every corner of a closed loop, including the join."""
    wrapped = [corners[-1]] + list(corners) + [corners[0]]
    opened = _fillet_path(wrapped, radius, segs)
    return opened[1:-1]


def _cast_end(x, segs):
    """One filled profile: feet, legs, seat rail, back post, and arm, with a window between arm and seat.

    Points are (unity z, unity y). The outer loop is counterclockwise. The hole is clockwise.
    """
    radius = 0.04
    outer = _fillet_loop([
        (-0.500, 0.000),
        (-0.252, 0.000),
        (-0.252, 0.392),
        (0.212, 0.392),
        (0.212, 0.000),
        (0.500, 0.000),
        (0.500, 0.072),
        (0.268, 0.072),
        (0.268, 0.688),
        (-0.252, 0.688),
        (-0.252, 0.880),
        (-0.308, 0.880),
        (-0.308, 0.072),
        (-0.500, 0.072),
    ], radius, segs)
    hole = _fillet_loop([
        (-0.252, 0.448),
        (0.212, 0.448),
        (0.212, 0.632),
        (-0.252, 0.632),
    ], radius, segs)
    cu = bpy.data.curves.new("cast_end", "CURVE")
    cu.dimensions = "2D"
    cu.fill_mode = "BOTH"
    half = 0.026
    cu.extrude = half * 2.0

    def _spline(pts):
        sp = cu.splines.new("POLY")
        sp.points.add(len(pts) - 1)
        for i, (z, y) in enumerate(pts):
            sp.points[i].co = (float(z), float(y), 0.0, 1.0)
        sp.use_cyclic_u = True

    _spline(outer)
    _spline(hole)
    obj = bpy.data.objects.new("cast_end", cu)
    bpy.context.scene.collection.objects.link(obj)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    me = obj.data
    verts = []
    for v in me.vertices:
        # Curve X is Unity Z, curve Y is Unity Y, extrusion is centered on the end.
        verts.append((x + (v.co.z - half), v.co.y, v.co.x))
    faces = [tuple(p.vertices) for p in me.polygons]
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.meshes.remove(me)
    return verts, faces


def _end_frame(g, x, segs, mat):
    verts, faces = _cast_end(x, segs)
    g.mesh(verts, faces, mat, bevel=0.004 if segs > 2 else 0.0, segs=1 if segs > 2 else 0)
    # Same wood cap on both ends, biting the iron arm (top is 0.688) from the post to the front leg.
    g.box((x, 0.694, -0.02), (0.046, 0.020, 0.50), "Lib_Board")


@register
def create():
    a = Asset(
        "Bench_Wood",
        "StreetFurniture",
        "1.80 m park bench. Each end is one dark-green cast profile with a wood arm seated on it. Seat and back slats overhang the iron by about 6 cm. Seat at 0.45 m.",
    )
    a.climb_note = "Back slats are too broken up to cling. Not a wall-run panel."
    a.vault_note = "Seat is 0.45 m. Below the 0.90–1.05 m vault band."
    length = 1.80
    iron = "Lib_Iron"
    for lod in (0, 1):
        g = a.begin(lod)
        slats = lod_pick(lod, 5, 3)
        backs = lod_pick(lod, 3, 2)
        segs = lod_pick(lod, 4, 2)
        for x in (-0.74, 0.74):
            _end_frame(g, x, segs, iron)
        # Rails stop inside the casting. A boss at each end is the cast joint.
        g.box((0, 0.42, 0.24), (1.44, 0.036, 0.036), iron)
        g.box((0, 0.42, -0.28), (1.44, 0.036, 0.036), iron)
        g.box((0, 0.78, -0.28), (1.40, 0.032, 0.032), iron)
        for x in (-0.72, 0.72):
            g.box((x, 0.42, 0.24), (0.06, 0.052, 0.052), iron)
            g.box((x, 0.42, -0.28), (0.06, 0.052, 0.052), iron)
            g.box((x, 0.78, -0.28), (0.055, 0.048, 0.048), iron)
        span0, span1 = -0.26, 0.24
        pitch = (span1 - span0) / slats
        # Iron outer face is at x = ±0.766. 6.5 cm of wood past that.
        slat_len = 1.662
        for i in range(slats):
            z = span0 + (i + 0.5) * pitch
            g.box((0, 0.455, z), (slat_len, 0.032, pitch * 0.86), "Lib_Board")
        for i in range(backs):
            y = 0.56 + i * (0.24 / max(1, backs - 1))
            g.box((0, y, -0.30), (slat_len, 0.050, 0.028), "Lib_Board")
        a.end()
    a.box("Col_Seat", (0, 0.455, -0.01), (1.60, 0.026, 0.40))
    a.box("Col_Back", (0, 0.68, -0.30), (1.58, 0.22, 0.020))
    for i, x in enumerate((-0.74, 0.74)):
        a.box("Col_LegF_%d" % i, (x, 0.28, 0.24), (0.036, 0.28, 0.032))
        a.box("Col_LegR_%d" % i, (x, 0.46, -0.28), (0.036, 0.56, 0.032))
        a.box("Col_Arm_%d" % i, (x, 0.66, -0.02), (0.036, 0.028, 0.28))
    return a
