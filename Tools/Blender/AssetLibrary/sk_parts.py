"""Shared pieces for the street-furniture kit. No asset is registered here."""

import math


def bolt_ring(g, center, radius, count, bolt_r=0.011, bolt_h=0.016, mat="Lib_Steel", phase=0.0):
    y = center[1]
    for i in range(count):
        ang = phase + 2.0 * math.pi * i / count
        x = center[0] + math.sin(ang) * radius
        z = center[2] + math.cos(ang) * radius
        g.cylinder((x, y, z), bolt_r, bolt_h, mat, 6)


def polyline(g, pts, radius, mat, segments=6):
    for i in range(len(pts) - 1):
        g.pipe(pts[i], pts[i + 1], radius, mat, segments)


def chain(g, a, b, n=5, sag=0.05, radius=0.0045):
    pts = []
    for i in range(n + 1):
        t = i / float(n)
        drop = math.sin(t * math.pi) * sag
        pts.append((
            a[0] + (b[0] - a[0]) * t,
            a[1] + (b[1] - a[1]) * t - drop,
            a[2] + (b[2] - a[2]) * t,
        ))
    polyline(g, pts, radius, "Lib_Chain", 4)


def look_euler(a, b):
    """Unity euler that aims a box's local +Z from a to b."""
    dx = b[0] - a[0]
    dy = b[1] - a[1]
    dz = b[2] - a[2]
    horiz = math.hypot(dx, dz) or 1e-8
    yaw = math.degrees(math.atan2(dx, dz))
    pitch = -math.degrees(math.atan2(dy, horiz))
    return (pitch, yaw, 0.0)


def span_collider(a, b, radius):
    """Inscribed box along a pipe. Cross-section sits inside the tube."""
    dx = b[0] - a[0]
    dy = b[1] - a[1]
    dz = b[2] - a[2]
    length = math.sqrt(dx * dx + dy * dy + dz * dz)
    center = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5, (a[2] + b[2]) * 0.5)
    side = radius * math.sqrt(2.0)
    # Keep the ends off the pipe caps.
    return center, (side, side, max(0.02, length - 0.01)), look_euler(a, b)


def add_hydrant(g, lod, body, cap, nut, band=None, wheel=False):
    """Dry-barrel hydrant. Wheel variant is the industrial yard hydrant."""
    seg = 16 if lod == 0 else 8
    half = max(8, seg // 2)
    bev = 0.003 if lod == 0 else 0.0
    bs = 1 if lod == 0 else 0
    g.cylinder((0, 0.028, 0), 0.17, 0.056, "Lib_SteelDark", seg, bevel=bev, segs=bs)
    if lod == 0:
        bolt_ring(g, (0, 0.062, 0), 0.132, 6, 0.012, 0.018)
    g.cylinder((0, 0.09, 0), 0.105, 0.06, "Lib_SteelDark", seg)
    g.cylinder((0, 0.36, 0), 0.086, 0.48, body, seg, bevel=bev, segs=bs)
    g.cylinder((0, 0.40, 0), 0.104, 0.11, body, seg)
    g.torus((0, 0.22, 0), 0.092, 0.008, body, seg, 6)
    g.torus((0, 0.52, 0), 0.092, 0.008, body, seg, 6)
    if band:
        g.cylinder((0, 0.55, 0), 0.112, 0.032, band, seg)
    for sign in (-1, 1):
        g.cylinder((sign * 0.128, 0.40, 0), 0.032, 0.10, "Lib_Brass", half, axis="X", bevel=bev, segs=bs)
        g.cylinder((sign * 0.186, 0.40, 0), 0.042, 0.028, cap, half, axis="X")
        g.cylinder((sign * 0.204, 0.40, 0), 0.014, 0.012, nut, 6, axis="X")
        if lod == 0:
            chain(g, (sign * 0.19, 0.38, 0.02), (sign * 0.06, 0.26, 0.07), sag=0.045)
    g.cylinder((0, 0.38, 0.155), 0.048, 0.12, "Lib_Brass", half, axis="Z", bevel=bev, segs=bs)
    g.cylinder((0, 0.38, 0.224), 0.058, 0.030, cap, half, axis="Z")
    g.cylinder((0, 0.38, 0.244), 0.016, 0.014, nut, 6, axis="Z")
    if lod == 0:
        chain(g, (0.02, 0.36, 0.23), (0.09, 0.24, 0.10), sag=0.05)
    if wheel:
        g.cylinder((0, 0.66, 0), 0.048, 0.12, "Lib_SteelDark", seg)
        g.cylinder((0, 0.74, 0), 0.022, 0.06, nut, 8)
        g.cylinder((0, 0.80, 0), 0.090, 0.016, "Lib_SteelDark", seg)
        g.torus((0, 0.80, 0), 0.105, 0.013, "Lib_Steel", 18 if lod == 0 else 10, 6)
        if lod == 0:
            for i in range(4):
                ang = math.radians(i * 90.0 + 20.0)
                polyline(g, [
                    (0, 0.80, 0),
                    (math.sin(ang) * 0.105, 0.80, math.cos(ang) * 0.105),
                ], 0.008, "Lib_SteelDark", 4)
        g.cylinder((0, 0.80, 0), 0.018, 0.02, nut, 8)
    else:
        g.cone((0, 0.67, 0), 0.108, 0.042, 0.16, body, seg)
        g.cylinder((0, 0.762, 0), 0.030, 0.036, nut, 5)
        g.cylinder((0, 0.786, 0), 0.012, 0.016, "Lib_Brass", 6)
        if lod == 0:
            g.torus((0, 0.748, 0), 0.034, 0.006, "Lib_SteelDark", 10, 5)


def hydrant_colliders(a, wheel=False):
    a.box("Col_Flange", (0, 0.028, 0), (0.22, 0.040, 0.22))
    a.capsule("Col_Barrel", (0, 0.36, 0), 0.078, 0.50, 1)
    a.capsule("Col_Nozzle_L", (-0.128, 0.40, 0), 0.028, 0.09, 0)
    a.capsule("Col_Nozzle_R", (0.128, 0.40, 0), 0.028, 0.09, 0)
    a.capsule("Col_Pumper", (0, 0.38, 0.155), 0.042, 0.11, 2)
    if wheel:
        a.capsule("Col_Neck", (0, 0.66, 0), 0.040, 0.12, 1)
        a.box("Col_Wheel", (0, 0.80, 0), (0.12, 0.012, 0.12))
    else:
        a.capsule("Col_Bonnet", (0, 0.66, 0), 0.040, 0.12, 1)
        a.capsule("Col_Nut", (0, 0.762, 0), 0.024, 0.05, 1)
