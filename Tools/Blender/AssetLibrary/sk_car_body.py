"""Closed car shells shared by the street-kit cars. Nothing is registered here."""

import math


def _loft(g, rings, mat):
    """Solid loft through closed rings of equal length. Ends are capped."""
    count = len(rings[0])
    verts = [p for ring in rings for p in ring]
    faces = []

    def vid(ring_i, i):
        return ring_i * count + (i % count)

    for ring_i in range(len(rings) - 1):
        for i in range(count):
            faces.append((vid(ring_i, i), vid(ring_i, i + 1), vid(ring_i + 1, i + 1), vid(ring_i + 1, i)))
    for ring_i, flip in ((0, True), (len(rings) - 1, False)):
        acc = [0.0, 0.0, 0.0]
        for p in rings[ring_i]:
            acc[0] += p[0]
            acc[1] += p[1]
            acc[2] += p[2]
        center = tuple(v / float(count) for v in acc)
        center_i = len(verts)
        verts.append(center)
        for i in range(count):
            a = vid(ring_i, i)
            b = vid(ring_i, i + 1)
            faces.append((center_i, b, a) if flip else (center_i, a, b))
    g.mesh(verts, faces, mat)


def _stations(z0, z1, step, extra):
    zs = set()
    z = z0
    while z < z1 - 1e-6:
        zs.add(round(z, 4))
        z += step
    zs.add(round(z1, 4))
    for z in extra:
        if z0 < z < z1:
            zs.add(round(z, 4))
    return sorted(zs)


def _lerp(keys, z):
    """keys are (z, y, x) sorted by z."""
    if z <= keys[0][0]:
        return keys[0][1], keys[0][2]
    if z >= keys[-1][0]:
        return keys[-1][1], keys[-1][2]
    for i in range(len(keys) - 1):
        z0, y0, x0 = keys[i]
        z1, y1, x1 = keys[i + 1]
        if z0 <= z <= z1:
            t = (z - z0) / (z1 - z0 or 1.0)
            return y0 + (y1 - y0) * t, x0 + (x1 - x0) * t
    return keys[-1][1], keys[-1][2]


def _arch_lift(z, axles, axle_y, arch_r, rocker_y):
    y = rocker_y
    for axle in axles:
        dz = z - axle
        if abs(dz) < arch_r:
            y = max(y, axle_y + math.sqrt(arch_r * arch_r - dz * dz))
    return y


def _flare(z, spec, axles):
    x = spec["body_x"]
    reach = spec["arch_r"] + 0.08
    for axle in axles:
        dz = abs(z - axle)
        if dz < reach:
            t = math.cos((dz / reach) * math.pi * 0.5)
            x = max(x, spec["body_x"] + spec["flare"] * t)
    return x


def _pillar(z, spec):
    if z >= spec["a_pillar_z"] or z <= spec["c_pillar_z"]:
        return True
    for pz, half in spec["pillars"]:
        if abs(z - pz) <= half:
            return True
    return False


def _glass_dip(z, spec, key):
    """How far the shell drops behind a raked pane, so the glass is a chord over a pocket."""
    z0, _y0, z1, _y1 = spec[key]
    lo, hi = (z0, z1) if z0 <= z1 else (z1, z0)
    if z <= lo or z >= hi:
        return 0.0
    t = (z - lo) / (hi - lo)
    return math.sin(math.pi * t) * 0.055


def _half(z, spec, axles):
    crown_y, crown_x = _lerp(spec["crown"], z)
    x_out = _flare(z, spec, axles)
    belt = min(spec["belt_y"], crown_y - 0.05)
    y_arch = min(_arch_lift(z, axles, spec["axle_y"], spec["arch_r"], spec["rocker_y"]), belt - 0.035)
    greenhouse = crown_y > spec["belt_y"] + 0.18 and spec["roof_z"][0] - 0.08 <= z <= spec["roof_z"][1] + 0.15
    rail_x = min(max(crown_x, 0.10), x_out - 0.035)
    y_rail = max(belt + 0.035, crown_y - 0.012)
    dip = _glass_dip(z, spec, "windshield") + _glass_dip(z, spec, "rear_glass")
    top = crown_y - dip
    if greenhouse and dip < 0.001:
        top += spec["roof_crown"]
    if dip < 0.001 and top < y_rail + 0.01:
        top = y_rail + 0.01
    y_mid = (belt + y_rail) * 0.5
    if greenhouse and not _pillar(z, spec):
        x_win = x_out - spec["inset"]
    else:
        x_win = x_out - 0.008
    x_win = min(x_win, x_out - 0.004)
    x_win = max(x_win, rail_x + 0.02)
    return [
        (0.0, spec["belly_y"]),
        (0.36, spec["belly_y"]),
        (min(0.62, x_out - 0.16), spec["belly_y"] + 0.025),
        (x_out, y_arch),
        (x_out, belt),
        (x_win, y_mid),
        (rail_x, y_rail),
        (0.0, top),
    ]


def _ring(half, z):
    pts = [(x, y, z) for x, y in half]
    for x, y in reversed(half[1:-1]):
        pts.append((-x, y, z))
    return pts


def _shell(g, spec, step, mat, axles):
    extra = [k[0] for k in spec["crown"]]
    extra.extend((spec["a_pillar_z"], spec["c_pillar_z"]))
    extra.extend((spec["windshield"][0], spec["windshield"][2], spec["rear_glass"][0], spec["rear_glass"][2]))
    extra.append((spec["windshield"][0] + spec["windshield"][2]) * 0.5)
    for pz, half in spec["pillars"]:
        extra.extend((pz - half, pz, pz + half))
    for axle in axles:
        extra.extend((axle - spec["arch_r"], axle, axle + spec["arch_r"]))
    zs = _stations(spec["z0"], spec["z1"], step, extra)
    rings = [_ring(_half(z, spec, axles), z) for z in zs]
    _loft(g, rings, mat)


def _slab(g, y0, z0, y1, z1, half_x0, half_x1, thick, mat):
    dy, dz = y1 - y0, z1 - z0
    norm = math.hypot(dy, dz) or 1.0
    ny, nz = (-dy / norm) * thick, (dz / norm) * thick
    verts = []
    for oy, oz in ((0.0, 0.0), (ny, nz)):
        verts.append((-half_x0, y0 + oy, z0 + oz))
        verts.append((half_x0, y0 + oy, z0 + oz))
        verts.append((half_x1, y1 + oy, z1 + oz))
        verts.append((-half_x1, y1 + oy, z1 + oz))
    g.mesh(verts, [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ], mat)


def _side_glass(g, spec, z0, z1, sign, mat):
    """Chord across the window pocket. Bottom enters the belt, top enters the drip rail, ends enter the pillars."""
    y0 = spec["belt_y"] + 0.012
    y1 = spec["roof_y"] - 0.028
    zmid = (z0 + z1) * 0.5
    x_out = _flare(zmid, spec, spec["axles"])
    x_bot = x_out - 0.014
    x_top = spec["roof_x"] - 0.012
    if x_top > x_bot - 0.05:
        x_top = x_bot - 0.05
    thick = 0.012
    verts = []
    for dx in (-thick * 0.5, thick * 0.5):
        for z, y, x in ((z0, y0, x_bot), (z1, y0, x_bot), (z1, y1, x_top), (z0, y1, x_top)):
            verts.append((sign * (x + dx), y, z))
    g.mesh(verts, [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ], mat)


def _raked(g, spec, z0, z1):
    """Pane on the crown chord. Both ends run past the opening into the shell."""
    if z0 <= z1:
        za, zb = z0 - 0.04, z1 + 0.04
    else:
        za, zb = z0 + 0.04, z1 - 0.04
    ya, xa = _lerp(spec["crown"], za)
    yb, xb = _lerp(spec["crown"], zb)
    _slab(
        g, ya - 0.012, za, yb - 0.012, zb,
        max(0.16, xa - 0.05), max(0.16, xb - 0.04),
        0.014, "Lib_ShopGlass",
    )


def _glass(g, spec):
    hz, _hy, cz, _cy = spec["windshield"]
    _raked(g, spec, hz, cz)
    if spec.get("bed_z0") is not None:
        _ry0, _ry1 = spec["rear_glass"][1], spec["rear_glass"][3]
        z_cap = spec["z0"]
        _slab(
            g, _ry0, z_cap + 0.018, _ry1, z_cap - 0.010,
            spec["roof_x"] - 0.08, spec["roof_x"] - 0.05,
            0.014, "Lib_ShopGlass",
        )
    else:
        rz0, _ry0, rz1, _ry1 = spec["rear_glass"]
        _raked(g, spec, rz0, rz1)
    for z0, z1 in spec["windows"]:
        lo, hi = (z0, z1) if z0 <= z1 else (z1, z0)
        for sign in (1.0, -1.0):
            _side_glass(g, spec, lo - 0.06, hi + 0.06, sign, "Lib_ShopGlass")


def _seams(g, spec):
    cuts = [spec["a_pillar_z"], spec["c_pillar_z"]]
    for pz, half in spec["pillars"]:
        cuts.append(pz - half)
        cuts.append(pz + half)
    for z in cuts:
        if not (spec["z0"] + 0.3 < z < spec["z1"] - 0.3):
            continue
        x = _flare(z, spec, spec["axles"])
        y0 = spec["rocker_y"] + 0.04
        y1 = spec["belt_y"] + 0.012
        for sign in (1.0, -1.0):
            g.box((sign * (x - 0.004), (y0 + y1) * 0.5, z), (0.012, y1 - y0, 0.010), "Lib_Black")


def _handles(g, spec):
    for z0, z1 in spec["windows"]:
        z = (z0 + z1) * 0.5
        x = _flare(z, spec, spec["axles"])
        for sign in (1.0, -1.0):
            g.box((sign * (x - 0.002), spec["belt_y"] + 0.045, z + 0.06), (0.016, 0.022, 0.09), "Lib_Black")


def _lamps(g, spec, lod):
    paint = spec["paint"]
    z_nose = spec["z1"] - 0.08
    for x in (-spec["lamp_x"], spec["lamp_x"]):
        g.box((x, spec["lamp_y"], z_nose - 0.02), (0.30, 0.12, 0.07), "Lib_Black")
        g.box((x, spec["lamp_y"], z_nose + 0.012), (0.24, 0.08, 0.018), "Lib_PaintCream")
        if lod == 0:
            g.box((x, spec["lamp_y"], z_nose + 0.024), (0.12, 0.045, 0.010), "Lib_ShopGlass")
    g.box((0, spec["lamp_y"] - 0.02, z_nose + 0.01), (0.55, 0.10, 0.025), "Lib_Black")
    if lod == 0:
        for i in range(4):
            g.box((0, spec["lamp_y"] - 0.05 + i * 0.025, z_nose + 0.026), (0.46, 0.008, 0.008), "Lib_SteelDark")
    if spec.get("bed_z0") is None:
        z_tail = spec["z0"] + 0.06
        z_bump = spec["z0"] + 0.02
    else:
        z_tail = spec["bed_z0"] + 0.05
        z_bump = spec["bed_z0"] + 0.02
    for x in (-spec["lamp_x"], spec["lamp_x"]):
        g.box((x, spec["tail_y"], z_tail + 0.015), (0.28, 0.11, 0.06), "Lib_Black")
        g.box((x, spec["tail_y"], z_tail - 0.012), (0.22, 0.07, 0.016), "Lib_PaintRed")
    g.box((0, spec["bumper_y"], spec["z1"] - 0.02), (spec["body_x"] * 1.92, 0.14, 0.07), "Lib_Black")
    width = spec["bed_x"] if spec.get("bed_z0") is not None else spec["body_x"]
    g.box((0, spec["bumper_y"], z_bump), (width * 1.92, 0.14, 0.07), "Lib_Black")
    for sign in (1.0, -1.0):
        g.box((sign * (spec["body_x"] + 0.02), spec["belt_y"] + 0.08, spec["a_pillar_z"] - 0.02), (0.10, 0.07, 0.14), paint)


def _wheels(g, spec, lod, axles):
    seg = 12 if lod == 0 else 8
    r = spec["tire_r"]
    half_w = spec["tire_half_w"]
    for z in axles:
        for x in (-spec["tire_x"], spec["tire_x"]):
            g.cylinder((x, spec["axle_y"], z), r, half_w * 2.0, "Lib_Rubber", seg, axis="X")
            cap = x + (half_w * 0.92 if x > 0 else -half_w * 0.92)
            g.cylinder((cap, spec["axle_y"], z), r * 0.62, 0.012, "Lib_Steel", seg, axis="X")
            if lod == 0:
                for k in range(5):
                    ang = math.radians(k * 72.0 + 8.0)
                    g.box(
                        (cap, spec["axle_y"] + math.sin(ang) * r * 0.28, z + math.cos(ang) * r * 0.28),
                        (0.010, r * 0.46, 0.018),
                        "Lib_SteelDark",
                        euler=(math.degrees(ang), 0, 0),
                    )


def _bed(g, spec, step, mat):
    z1 = spec["bed_z1"]
    z0 = spec["bed_z0"]
    floor_y = spec["bed_floor_y"]
    rail = spec["bed_rail_y"]
    length = z1 - z0
    mid = (z0 + z1) * 0.5
    g.box((0, floor_y, mid), (spec["bed_x"] * 2.0 - 0.02, 0.06, length + 0.04), mat)
    g.box((0, (floor_y + rail) * 0.5, z0 + 0.02), ((spec["bed_x"] - 0.015) * 2.0, rail - floor_y + 0.04, 0.06), mat)
    axle = spec["bed_axle"]
    extra = [axle - spec["arch_r"], axle, axle + spec["arch_r"], z0, z1]
    zs = _stations(z0, z1, step, extra)
    for sign in (1.0, -1.0):
        rings = []
        for z in zs:
            lift = _arch_lift(z, (axle,), spec["axle_y"], spec["arch_r"], floor_y + 0.02)
            y_lo = min(lift, rail - 0.10)
            reach = spec["arch_r"] + 0.08
            flare = 0.0
            dz = abs(z - axle)
            if dz < reach:
                flare = spec["flare"] * math.cos((dz / reach) * math.pi * 0.5)
            x_i = spec["bed_x"] - 0.03
            x_o = spec["bed_x"] + flare
            if sign < 0:
                x_o, x_i = -x_o, -x_i
            rings.append([
                (x_o, y_lo, z),
                (x_o, rail, z),
                (x_i, rail, z),
                (x_i, max(y_lo, floor_y), z),
            ])
        _loft(g, rings, mat)


def build(g, spec, lod):
    step = spec["step"] if lod == 0 else spec["step"] * 1.8
    _shell(g, spec, step, spec["paint"], spec["axles"])
    _glass(g, spec)
    _seams(g, spec)
    _handles(g, spec)
    _lamps(g, spec, lod)
    _wheels(g, spec, lod, spec["wheel_axles"])
    if spec.get("bed_z0") is not None:
        _bed(g, spec, step, spec["paint"])
        _wheels(g, spec, lod, (spec["bed_axle"],))
