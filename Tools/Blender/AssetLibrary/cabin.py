"""Small log cabin. 4.6 x 3.6 m, porch on +Z, gable roof."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Cabin",
        "Buildings",
        "Log cabin 4.6 x 3.6 m, walls 2.15 m, ridge at 3.45 m, porch on +Z. The door is a closed frame-and-panel in the log opening.",
    )
    a.climbable = True
    a.climb_note = "Log walls are cling. Door is closed. Roof slopes are landings."
    a.vault_note = "Porch rail is 0.95 m above the porch deck (deck at 0.25 m, rail top at 1.20 m)."
    a.vaultable = True
    a.vault_height = 0.95
    width, depth = 4.6, 3.6
    for lod in (0, 1, 2):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6, 4)
        courses = lod_pick(lod, 9, 6, 3)
        log_r = 0.10
        # Corner posts so logs meet flush instead of spearing through each other.
        for x in (-width * 0.5, width * 0.5):
            for z in (-depth * 0.5, depth * 0.5):
                g.box((x, 1.05, z), (0.20, 2.10, 0.20), "Lib_WoodDark", bevel=0.004 if lod == 0 else 0, segs=1, uv_scale=1.0)
        span_x = width - 0.46
        span_z = depth - 0.46
        for i in range(courses):
            y = 0.18 + i * (1.85 / max(1, courses - 1))
            # Front logs stop at the door jambs below the header.
            if y < 1.85:
                g.cylinder((-1.28, y, depth * 0.5 - 0.12), log_r, 1.55, "Lib_WoodDark", seg, axis="X", grain=1.0)
                g.cylinder((1.28, y, depth * 0.5 - 0.12), log_r, 1.55, "Lib_WoodDark", seg, axis="X", grain=1.0)
            else:
                g.cylinder((0, y, depth * 0.5 - 0.12), log_r, span_x, "Lib_WoodDark", seg, axis="X", grain=1.0)
            g.cylinder((0, y, -depth * 0.5 + 0.12), log_r, span_x, "Lib_Wood", seg, axis="X", grain=1.0)
            g.cylinder((width * 0.5 - 0.12, y, 0), log_r, span_z, "Lib_Wood", seg, axis="Z", grain=2.0)
            g.cylinder((-width * 0.5 + 0.12, y, 0), log_r, span_z, "Lib_Wood", seg, axis="Z", grain=2.0)
            _chink_course(g, i, courses, y, width, depth, log_r)
            # Cut ends proud of the corner posts. The outer face is end grain.
            if lod == 0:
                for x in (-width * 0.5 - 0.08, width * 0.5 + 0.08):
                    g.cylinder((x, y, -depth * 0.5 - 0.02), log_r * 0.96, 0.06, "Lib_Wood", max(6, seg // 2), axis="X", grain=1.0)
                    _log_end(g, (x, y, -depth * 0.5 - 0.02), "X", log_r * 0.96, -1 if x < 0 else 1)
                for z in (-depth * 0.5 - 0.02, depth * 0.5 + 0.02):
                    g.cylinder((-width * 0.5 - 0.08, y, z), log_r * 0.96, 0.06, "Lib_WoodDark", max(6, seg // 2), axis="Z", grain=2.0)
                    g.cylinder((width * 0.5 + 0.08, y, z), log_r * 0.96, 0.06, "Lib_Wood", max(6, seg // 2), axis="Z", grain=2.0)
                    end = -1 if z < 0 else 1
                    _log_end(g, (-width * 0.5 - 0.08, y, z), "Z", log_r * 0.96, end)
                    _log_end(g, (width * 0.5 + 0.08, y, z), "Z", log_r * 0.96, end)
        # Frame sits on the log centre line, behind the outer round. Panels are inside the frame.
        z_door = depth * 0.5 - 0.12
        g.box((-0.48, 1.05, z_door), (0.10, 1.95, 0.10), "Lib_WoodDark", uv_scale=1.2)
        g.box((0.48, 1.05, z_door), (0.10, 1.95, 0.10), "Lib_WoodDark", uv_scale=1.2)
        g.box((0, 1.98, z_door), (0.86, 0.10, 0.10), "Lib_WoodDark", uv_scale=1.2)
        g.box((0, 0.16, z_door), (0.86, 0.08, 0.10), "Lib_WoodDark", uv_scale=1.2)
        g.box((0, 1.10, z_door), (0.86, 0.08, 0.08), "Lib_WoodDark", uv_scale=1.2)
        if lod < 2:
            g.box((0, 1.54, z_door), (0.68, 0.72, 0.028), "Lib_Wood", uv_scale=1.2)
            g.box((0, 0.62, z_door), (0.68, 0.76, 0.028), "Lib_Wood", uv_scale=1.2)
            g.box((0, 1.54, z_door + 0.02), (0.32, 0.28, 0.012), "Lib_ShopGlass")
            g.box((0, 1.54, z_door + 0.028), (0.32, 0.012, 0.008), "Lib_Wood")
            g.box((0, 1.54, z_door + 0.028), (0.012, 0.28, 0.008), "Lib_Wood")
        if lod == 0:
            g.box((0.32, 1.00, z_door + 0.03), (0.035, 0.07, 0.03), "Lib_Brass")
            for x, sign in ((-width * 0.5 - 0.01, -1), (width * 0.5 + 0.01, 1)):
                g.box((x, 1.35, 0.15), (0.05, 0.72, 0.62), "Lib_Wood")
                g.box((x + sign * 0.02, 1.35, 0.15), (0.015, 0.48, 0.40), "Lib_ShopGlass")
                g.box((x + sign * 0.028, 1.35, 0.15), (0.01, 0.48, 0.016), "Lib_Wood")
                g.box((x + sign * 0.028, 1.35, 0.15), (0.01, 0.016, 0.40), "Lib_Wood")
            g.box((1.15, 3.55, -0.35), (0.48, 1.15, 0.48), "Lib_Brick", uv_scale=1.0)
            g.box((1.15, 4.16, -0.35), (0.58, 0.08, 0.58), "Lib_Concrete")
        g.box((0, 0.05, depth * 0.5 + 1.95), (1.15, 0.08, 0.32), "Lib_Wood", grain=1.0)
        g.box((0, 0.14, depth * 0.5 + 1.62), (1.20, 0.10, 0.28), "Lib_Wood", grain=1.0)
        # Porch deck, posts, and a beam the front eave actually sits on.
        g.box((0, 0.12, depth * 0.5 + 0.7), (3.2, 0.10, 1.3), "Lib_Wood", bevel=0.004 if lod == 0 else 0, segs=1, uv_scale=1.0, grain=1.0)
        for x in (-1.3, 1.3):
            g.box((x, 1.15, depth * 0.5 + 1.25), (0.12, 2.05, 0.12), "Lib_WoodDark")
        g.box((0, 2.245, 3.08), (2.84, 0.16, 0.30), "Lib_WoodDark", grain=1.0)
        g.box((0, 2.29, depth * 0.5 + 0.7), (3.4, 0.08, 1.5), "Lib_Roof", uv_scale=1.0)
        if lod < 2:
            g.pipe((-1.3, 0.95, depth * 0.5 + 1.25), (1.3, 0.95, depth * 0.5 + 1.25), 0.03, "Lib_WoodDark", 6, grain=1.0)
            for x in (-0.86, -0.43, 0.0, 0.43, 0.86):
                g.box((x, 0.58, depth * 0.5 + 1.25), (0.035, 0.66, 0.028), "Lib_Wood")
        # Gable roof slabs.
        _roof(g, width + 0.4, lod)
        a.end()
    a.loose_pivot = True
    # One box inside each log. A single wall slab sticks out of the round courses.
    _log_climb(a, width, depth)
    a.box("Col_Door", (0, 1.05, depth * 0.5 - 0.12), (0.66, 1.70, 0.024))
    a.box("Col_StepLow", (0, 0.05, depth * 0.5 + 1.95), (1.00, 0.05, 0.24))
    a.box("Col_StepHigh", (0, 0.15, depth * 0.5 + 1.62), (1.05, 0.06, 0.20))
    a.box("Col_Porch", (0, 0.12, depth * 0.5 + 0.7), (3.2, 0.10, 1.3))
    a.box("Col_PorchRoof", (0, 2.29, depth * 0.5 + 0.7), (3.2, 0.06, 1.35))
    a.capsule("Vault_PorchRail", (0, 0.95, depth * 0.5 + 1.25), 0.03, 2.6, 0)
    # Roof colliders sit inside the slabs. Pivot is the cabin floor, porch is +Z.
    _add_roof(a, width + 0.4, -2.05, 2.15, 0.0, 3.45, 0.06)
    return a


def _chink_course(g, i, courses, y, width, depth, log_r):
    """Pale fill in the gap between this course and the next. Kept off the log centres."""
    if i >= courses - 1:
        return
    step = 1.85 / (courses - 1)
    mid = y + step * 0.5
    gap_h = 0.05
    # Two skins, outside the climb boxes (those sit on the log centre ±5 cm).
    skin = 0.035
    span_x = width - 0.70
    span_z = depth - 0.70
    z_front = depth * 0.5 - 0.12
    z_back = -depth * 0.5 + 0.12
    x_side = width * 0.5 - 0.12
    for sign in (-1, 1):
        g.box((0, mid, z_back + sign * 0.075), (span_x, gap_h, skin), "Lib_Concrete", uv_scale=0.8)
        g.box((-x_side + sign * 0.075, mid, 0), (skin, gap_h, span_z), "Lib_Concrete", uv_scale=0.8)
        g.box((x_side + sign * 0.075, mid, 0), (skin, gap_h, span_z), "Lib_Concrete", uv_scale=0.8)
        if y < 1.85:
            g.box((-1.28, mid, z_front + sign * 0.075), (1.35, gap_h, skin), "Lib_Concrete", uv_scale=0.8)
            g.box((1.28, mid, z_front + sign * 0.075), (1.35, gap_h, skin), "Lib_Concrete", uv_scale=0.8)
        else:
            g.box((0, mid, z_front + sign * 0.075), (span_x, gap_h, skin), "Lib_Concrete", uv_scale=0.8)


def _log_end(g, center, axis, radius, sign):
    """Flat cut face with two growth rings, proud of the side-grain stub."""
    face = sign * 0.03
    proud = sign * 0.006
    if axis == "X":
        disc = (center[0] + face + proud, center[1], center[2])
        ring = (center[0] + face + proud + sign * 0.006, center[1], center[2])
    else:
        disc = (center[0], center[1], center[2] + face + proud)
        ring = (center[0], center[1], center[2] + face + proud + sign * 0.006)
    g.cylinder(disc, radius * 0.97, 0.008, "Lib_Board", 8, axis=axis)
    _growth_ring(g, ring, axis, radius * 0.62, 0.007)


def _growth_ring(g, center, axis, radius, tube):
    steps = 8
    verts = []
    for i in range(steps):
        ang = 2.0 * math.pi * i / steps
        ca, sa = math.cos(ang), math.sin(ang)
        for rr, along in (
            (radius - tube, 0.0),
            (radius + tube, 0.0),
            (radius - tube, tube),
            (radius + tube, tube),
        ):
            if axis == "X":
                verts.append((center[0] + along, center[1] + ca * rr, center[2] + sa * rr))
            else:
                verts.append((center[0] + ca * rr, center[1] + sa * rr, center[2] + along))
    faces = []
    for i in range(steps):
        n = (i + 1) % steps
        a, b = i * 4, n * 4
        faces.append((a + 1, b + 1, b + 3, a + 3))
        faces.append((a + 2, b + 2, b + 0, a + 0))
        faces.append((a + 2, a + 3, b + 3, b + 2))
        faces.append((a + 0, b + 0, b + 1, a + 1))
    g.mesh(verts, faces, "Lib_Black")


def _log_climb(asset, width, depth):
    courses = 9
    span_x = width - 0.46
    span_z = depth - 0.46
    z_front = depth * 0.5 - 0.12
    z_back = -depth * 0.5 + 0.12
    x_side = width * 0.5 - 0.12
    thick = 0.10
    for i in range(courses):
        y = 0.18 + i * (1.85 / (courses - 1))
        if y < 1.85:
            # The door frame shares this course. Keep the box on the log
            # centre so the ray does not clip the frame.
            asset.box("Climb_FrontL_%d" % i, (-1.28, y, z_front), (0.64, 0.08, 0.08))
            asset.box("Climb_FrontR_%d" % i, (1.28, y, z_front), (0.64, 0.08, 0.08))
        else:
            asset.box("Climb_FrontHead_%d" % i, (0.0, y, z_front), (span_x - 0.20, thick, thick))
        asset.box("Climb_Back_%d" % i, (0.0, y, z_back), (span_x - 0.20, thick, thick))
        asset.box("Climb_SideL_%d" % i, (-x_side, y, 0.0), (thick, thick, span_z - 0.20))
        asset.box("Climb_SideR_%d" % i, (x_side, y, 0.0), (thick, thick, span_z - 0.20))


def _roof(g, width, lod):
    g.mesh(_prism(width, -2.05, 2.15, 0.0, 3.45, 0.06), _prism_faces(), "Lib_Roof", uv_scale=1.0)
    g.mesh(_prism(width, 2.05, 2.15, 0.0, 3.45, 0.06), _prism_faces(), "Lib_Roof", uv_scale=1.0)


def _add_roof(asset, width, z0, y0, z1, y1, thick):
    center, size, euler = _roof_box(z0, y0, z1, y1, thick, width)
    asset.box("Col_RoofS", center, size, euler=euler)
    center, size, euler = _roof_box(z0 * -1.0, y0, z1 * -1.0 if z1 else 0.0, y1, thick, width)
    # North eave is the mirrored z. _roof builds the second prism from +z0.
    asset.box("Col_RoofN", center, size, euler=euler)


def _roof_box(z0, y0, z1, y1, thick, width):
    dz, dy = (z1 - z0), (y1 - y0)
    length = math.hypot(dz, dy) or 1.0
    nz, ny = -dy / length, dz / length
    if ny < 0:
        nz, ny = -nz, -ny
    cz = (z0 + z1) * 0.5 + nz * thick * 0.45
    cy = (y0 + y1) * 0.5 + ny * thick * 0.45
    angle = math.degrees(math.atan2(abs(dy), abs(dz)))
    pitch = -angle if dz * dy > 0 else angle
    return (0.0, cy, cz), (width * 0.82, thick * 0.5, length * 0.70), (pitch, 0.0, 0.0)


def _prism(width, z0, y0, z1, y1, thick):
    dz, dy = (z1 - z0), (y1 - y0)
    length = math.hypot(dz, dy) or 1.0
    # Normal in the ZY plane pointing "up-ish".
    nz, ny = -dy / length, dz / length
    if ny < 0:
        nz, ny = -nz, -ny
    xs = (-width * 0.5, width * 0.5)
    verts = []
    for x in xs:
        for z, y in ((z0, y0), (z1, y1)):
            verts.append((x, y, z))
    for x in xs:
        for z, y in ((z0, y0), (z1, y1)):
            verts.append((x, y + ny * thick, z + nz * thick))
    return verts


def _prism_faces():
    # 0,1 bottom edge at x0; 2,3 at x1; 4..7 top.
    return [
        (0, 2, 3, 1),
        (4, 5, 7, 6),
        (0, 1, 5, 4),
        (2, 6, 7, 3),
        (0, 4, 6, 2),
        (1, 3, 7, 5),
    ]
