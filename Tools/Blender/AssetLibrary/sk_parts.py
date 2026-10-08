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


def _rodrigues(v, axis, ang):
    c = math.cos(ang)
    s = math.sin(ang)
    ax, ay, az = axis
    dot = ax * v[0] + ay * v[1] + az * v[2]
    cx = ay * v[2] - az * v[1]
    cy = az * v[0] - ax * v[2]
    cz = ax * v[1] - ay * v[0]
    return (
        v[0] * c + cx * s + ax * dot * (1.0 - c),
        v[1] * c + cy * s + ay * dot * (1.0 - c),
        v[2] * c + cz * s + az * dot * (1.0 - c),
    )


def sweep_tube(g, pts, radius0, mat, segments=12, radius1=None):
    """One tube through Unity-space points. radius1 tapers the far end.

    Rings share vertices, so a fine bend shades smooth instead of showing a crease
    at every pipe joint.
    """
    if radius1 is None:
        radius1 = radius0
    n = len(pts)
    if n < 2 or radius0 <= 0 or radius1 <= 0:
        return

    def sub(a, b):
        return (a[0] - b[0], a[1] - b[1], a[2] - b[2])

    def add(a, b):
        return (a[0] + b[0], a[1] + b[1], a[2] + b[2])

    def mul(a, s):
        return (a[0] * s, a[1] * s, a[2] * s)

    def dot(a, b):
        return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]

    def cross(a, b):
        return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])

    def length(a):
        return math.sqrt(max(0.0, dot(a, a)))

    def norm(a):
        return mul(a, 1.0 / length(a))

    tangents = []
    for i in range(n):
        if i == 0:
            t = sub(pts[1], pts[0])
        elif i == n - 1:
            t = sub(pts[-1], pts[-2])
        else:
            t = sub(pts[i + 1], pts[i - 1])
        tangents.append(norm(t))
    seed = (0.0, 0.0, 1.0)
    if abs(dot(tangents[0], seed)) > 0.85:
        seed = (1.0, 0.0, 0.0)
    normal = norm(cross(tangents[0], seed))
    verts = []
    rings = []
    for i, p in enumerate(pts):
        if i:
            axis = cross(tangents[i - 1], tangents[i])
            alen = length(axis)
            if alen > 1e-8:
                ang = math.atan2(alen, dot(tangents[i - 1], tangents[i]))
                normal = _rodrigues(normal, norm(axis), ang)
            normal = sub(normal, mul(tangents[i], dot(normal, tangents[i])))
            normal = norm(normal)
        binormal = norm(cross(tangents[i], normal))
        t = i / float(n - 1)
        radius = radius0 + (radius1 - radius0) * t
        ring = []
        for s in range(segments):
            a = 2.0 * math.pi * s / float(segments)
            radial = add(mul(normal, math.cos(a) * radius), mul(binormal, math.sin(a) * radius))
            ring.append(len(verts))
            verts.append(add(p, radial))
        rings.append(ring)
    faces = []
    for i in range(n - 1):
        for s in range(segments):
            ns = (s + 1) % segments
            faces.append((rings[i][s], rings[i][ns], rings[i + 1][ns], rings[i + 1][s]))
    faces.append(tuple(reversed(rings[0])))
    faces.append(tuple(rings[-1]))
    g.mesh(verts, faces, mat)


def tri_plate(g, center, side, thick, mat, bevel=0.0, segs=0, point="down"):
    """Equilateral plate. `point` is down, up, left, or right in the Unity XY plane."""
    h = side * math.sqrt(3.0) / 2.0
    xy = [(0.0, -2.0 * h / 3.0), (-side * 0.5, h / 3.0), (side * 0.5, h / 3.0)]
    if point == "up":
        xy = [(x, -y) for x, y in xy]
    elif point == "right":
        xy = [(-y, x) for x, y in xy]
    elif point == "left":
        xy = [(y, -x) for x, y in xy]
    cx, cy, cz = center
    t = thick * 0.5
    verts = [(cx + x, cy + y, cz - t) for x, y in xy]
    verts += [(cx + x, cy + y, cz + t) for x, y in xy]
    g.mesh(verts, [
        (0, 2, 1),
        (3, 4, 5),
        (0, 1, 4, 3),
        (1, 2, 5, 4),
        (2, 0, 3, 5),
    ], mat, bevel=bevel, segs=segs)


def catenary(g, a, b, sag, radius, mat, steps=8, segments=6):
    pts = []
    for i in range(steps + 1):
        t = i / float(steps)
        drop = math.sin(t * math.pi) * sag
        pts.append((
            a[0] + (b[0] - a[0]) * t,
            a[1] + (b[1] - a[1]) * t - drop,
            a[2] + (b[2] - a[2]) * t,
        ))
    polyline(g, pts, radius, mat, segments)


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


def fender_arch(g, x_face, y, z, radius, band, stand, mat, outward=1.0, steps=10):
    """Half-ring lip in the YZ plane, proud of a side panel. Ends sit above the axle."""
    a0 = math.radians(16.0)
    a1 = math.pi - a0
    x0 = x_face
    x1 = x_face + outward * stand
    r_in = radius - band * 0.5
    r_out = radius + band * 0.5
    verts = []
    rings = []
    for i in range(steps + 1):
        ang = a0 + (a1 - a0) * (i / float(steps))
        sy = math.sin(ang)
        cz = math.cos(ang)
        base = len(verts)
        for rad, x in ((r_in, x0), (r_out, x0), (r_out, x1), (r_in, x1)):
            verts.append((x, y + rad * sy, z + rad * cz))
        rings.append(base)
    faces = []
    for i in range(steps):
        a = rings[i]
        b = rings[i + 1]
        for k in range(4):
            k2 = (k + 1) % 4
            faces.append((a + k, a + k2, b + k2, b + k))
    faces.append((rings[0], rings[0] + 1, rings[0] + 2, rings[0] + 3))
    last = rings[-1]
    faces.append((last + 3, last + 2, last + 1, last))
    g.mesh(verts, faces, mat)


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


def _lathe(g, profile, segments, mat, band=None):
    """Closed solid of revolution. profile is (radius, y) from the ground up.

    band is (y0, y1, material). Faces lying fully inside that span keep the
    same surface and pick up the second material, so a painted stripe does
    not become a second solid.
    """
    verts = []
    rings = []
    heights = []
    for radius, y in profile:
        if radius < 1e-5:
            rings.append([len(verts)])
            heights.append(y)
            verts.append((0.0, y, 0.0))
            continue
        ring = []
        for i in range(segments):
            ang = 2.0 * math.pi * i / segments
            ring.append(len(verts))
            verts.append((radius * math.sin(ang), y, radius * math.cos(ang)))
        rings.append(ring)
        heights.append(y)
    faces = []
    face_mats = []
    for (a, ya), (b, yb) in zip(zip(rings, heights), zip(rings[1:], heights[1:])):
        use = mat
        if band and min(ya, yb) >= band[0] - 1e-5 and max(ya, yb) <= band[1] + 1e-5:
            use = band[2]
        if len(a) == 1:
            c = a[0]
            n = len(b)
            for i in range(n):
                faces.append((c, b[i], b[(i + 1) % n]))
                face_mats.append(use)
        elif len(b) == 1:
            c = b[0]
            n = len(a)
            for i in range(n):
                faces.append((c, a[(i + 1) % n], a[i]))
                face_mats.append(use)
        else:
            n = len(a)
            for i in range(n):
                j = (i + 1) % n
                faces.append((a[i], b[i], b[j], a[j]))
                face_mats.append(use)
    g.mesh(verts, faces, mat, face_mats=face_mats)


def _hydrant_profile(wheel):
    """One dry-barrel casting. 7 in barrel, nozzle section, bonnet.

    The nozzle colliders sit in the caps, outside this solid. The barrel
    collider sits in the 0.090 m straight section.
    """
    profile = [
        (0.0, 0.000),
        (0.150, 0.000),
        (0.150, 0.032),
        (0.104, 0.046),
        (0.090, 0.072),
        (0.090, 0.168),
        (0.090, 0.248),
        (0.090, 0.348),
        (0.114, 0.372),
        (0.114, 0.518),
    ]
    if wheel:
        profile += [
            (0.064, 0.548),
            (0.040, 0.578),
            (0.040, 0.630),
            (0.0, 0.630),
        ]
    else:
        # Lip, then a dome: the wall stays nearly vertical off the lip and
        # the radius falls faster toward the crown, so it is not a straight cone.
        profile += [
            (0.108, 0.530),
            (0.128, 0.542),
            (0.142, 0.554),
            (0.144, 0.566),
            (0.144, 0.578),
            (0.140, 0.592),
            (0.130, 0.610),
            (0.112, 0.628),
            (0.088, 0.646),
            (0.060, 0.662),
            (0.034, 0.676),
            (0.016, 0.688),
            (0.012, 0.700),
            (0.012, 0.736),
            (0.0, 0.736),
        ]
    return profile


def _hose_nozzle(g, lod, sign, y, cap, nut, seg):
    """2.5 in hose nozzle. Cap and pentagon lug are one stack on a brass boss."""
    # Boss runs well inside the casting so its end cap is not on the barrel wall.
    g.cylinder((sign * 0.130, y, 0.0), 0.034, 0.120, "Lib_Brass", seg, axis="X")
    g.cylinder((sign * 0.214, y, 0.0), 0.050, 0.086, cap, seg, axis="X")
    g.cylinder((sign * 0.250, y, 0.0), 0.016, 0.024, nut, 5, axis="X")
    if lod != 0:
        return
    g.box((sign * 0.214, y - 0.058, 0.0), (0.024, 0.026, 0.014), "Lib_SteelDark")
    g.sphere((sign * 0.084, 0.292, 0.016), 0.012, "Lib_SteelDark", 8)
    chain(
        g,
        (sign * 0.214, y - 0.064, 0.0),
        (sign * 0.088, 0.292, 0.014),
        n=5,
        sag=0.030,
        radius=0.0038,
    )


def _pumper_nozzle(g, lod, y, cap, nut, seg):
    """4.5 in steamer toward +Z. Larger cap, same pentagon and chain."""
    g.cylinder((0.0, y, 0.140), 0.052, 0.130, "Lib_Brass", seg, axis="Z")
    g.cylinder((0.0, y, 0.245), 0.070, 0.100, cap, seg, axis="Z")
    g.cylinder((0.0, y, 0.288), 0.018, 0.024, nut, 5, axis="Z")
    if lod != 0:
        return
    g.box((0.0, y - 0.080, 0.245), (0.016, 0.030, 0.028), "Lib_SteelDark")
    g.sphere((0.040, 0.300, 0.078), 0.012, "Lib_SteelDark", 8)
    chain(
        g,
        (0.0, y - 0.086, 0.245),
        (0.036, 0.300, 0.082),
        n=5,
        sag=0.026,
        radius=0.0038,
    )


def add_hydrant(g, lod, body, cap, nut, band=None, wheel=False):
    """Dry-barrel hydrant. One casting, thin collar, chained caps.

    Wheel variant is the industrial yard hydrant. Hose nozzles are 2.5 in,
    the steamer is 4.5 in, and the operating nut lands near 0.78 m.
    """
    seg = 28 if lod == 0 else 12
    nose = 24 if lod == 0 else 10
    painted = (0.168, 0.248, band) if band and not wheel else None
    _lathe(g, _hydrant_profile(wheel), seg, body, band=painted)
    if lod == 0:
        bolt_ring(g, (0.0, 0.030, 0.0), 0.118, 6, 0.008, 0.014)
        g.torus((0.0, 0.140, 0.0), 0.096, 0.005, body, seg, 6)
        g.torus((0.0, 0.310, 0.0), 0.096, 0.005, body, seg, 6)
    y = 0.446
    for sign in (-1, 1):
        _hose_nozzle(g, lod, sign, y, cap, nut, nose)
    _pumper_nozzle(g, lod, y, cap, nut, nose)
    if wheel:
        g.cylinder((0.0, 0.70, 0.0), 0.020, 0.16, nut, 8)
        g.cylinder((0.0, 0.775, 0.0), 0.088, 0.028, cap, seg)
        g.torus((0.0, 0.775, 0.0), 0.108, 0.012, "Lib_Steel", 16 if lod == 0 else 8, 6)
        if lod == 0:
            for i in range(4):
                ang = math.radians(i * 90.0 + 18.0)
                polyline(g, [
                    (math.sin(ang) * 0.078, 0.775, math.cos(ang) * 0.078),
                    (math.sin(ang) * 0.108, 0.775, math.cos(ang) * 0.108),
                ], 0.007, "Lib_SteelDark", 4)
        g.cylinder((0.0, 0.798, 0.0), 0.016, 0.024, nut, 6)
        return
    if lod == 0:
        bolt_ring(g, (0.0, 0.590, 0.0), 0.096, 4, 0.008, 0.016, phase=0.4)
        g.cylinder((0.0, 0.724, 0.0), 0.020, 0.018, "Lib_SteelDark", 16)
    # 1.5 in operating nut, about 28 mm tall. The old prism read oversized.
    g.cylinder((0.0, 0.742, 0.0), 0.015, 0.028, nut, 5)


def hydrant_colliders(a, wheel=False):
    # Flange box sits in the casting foot. Barrel capsule stays in the
    # straight 0.090 m section, clear of the nozzle bosses.
    a.box("Col_Flange", (0.0, 0.014, 0.0), (0.16, 0.020, 0.16))
    a.capsule("Col_Barrel", (0.0, 0.200, 0.0), 0.062, 0.22, 1)
    # Sphere-like capsules in the middle of each cap, past the brass boss.
    a.capsule("Col_Nozzle_L", (-0.210, 0.446, 0.0), 0.020, 0.040, 0)
    a.capsule("Col_Nozzle_R", (0.210, 0.446, 0.0), 0.020, 0.040, 0)
    a.capsule("Col_Pumper", (0.0, 0.446, 0.236), 0.028, 0.056, 2)
    if wheel:
        a.capsule("Col_Neck", (0.0, 0.700, 0.0), 0.014, 0.08, 1)
        a.box("Col_Wheel", (0.0, 0.775, 0.0), (0.10, 0.018, 0.10))
    else:
        a.capsule("Col_Bonnet", (0.0, 0.630, 0.0), 0.024, 0.050, 1)
        a.capsule("Col_Nut", (0.0, 0.748, 0.0), 0.008, 0.016, 1)
