"""Modern midsize sedan shell. One loft for every model year.

The cage is a closed cross-section loft. Each station is a smooth half-section
(rocker, door bulge, belt shoulder, greenhouse) and the greenhouse is the top
of that same section, not a box added on the hood. The windshield is the run
of stations from the cowl at 1.0 m up to the header, about 26 degrees from
horizontal. Glass, the grille cavity, and the lamp cavities are insets in
that skin. Years only change what sits in the cavities.

Sheet inches, family-sedan exterior page: length 193.5, width 72.4, height
56.9, wheelbase 111.2, clearance 5.4, front overhang 39.0. The hood height
(0.95 m) and deck height (1.0 m) are the class profile those inches describe.
Nothing here is traced from a drawing.
"""

import math

import bmesh

import shell
from _common import blender_to_unity

INCH = 0.0254
LENGTH = 193.5 * INCH
HALF_L = LENGTH * 0.5
WIDTH = 72.4 * INCH
HALF_W = WIDTH * 0.5
HEIGHT = 56.9 * INCH
WHEELBASE = 111.2 * INCH
FRONT_OVERHANG = 39.0 * INCH
Z_FRONT = HALF_L - FRONT_OVERHANG
Z_REAR = Z_FRONT - WHEELBASE
BELLY = 5.4 * INCH

TIRE_W = 0.235
RIM_R = 18.0 * INCH * 0.5
TIRE_R = RIM_R + TIRE_W * 0.45
AXLE_Y = TIRE_R
ARCH_R = TIRE_R + 0.020
TIRE_X = min(63.0 * INCH * 0.5, HALF_W - TIRE_W * 0.5 - 0.015)

Z_NOSE = HALF_L
Z_TAIL = -HALF_L
Z_DECK = -1.30
Z_ROOF_R = -0.72
Z_PEAK = -0.05
Z_HEADER = 0.38
Z_COWL = 1.10

# Nose cap is the fascia. 0.84 m is tall enough for a grille and a lamp
# slot on the forward face. The hood then rises to 0.95 m and the cowl to 1.0 m.
CROWN = (
    (Z_TAIL, 0.90),
    (-2.20, 0.93),
    (-1.95, 0.98),
    (Z_DECK, 1.00),
    (-1.00, 1.16),
    (Z_ROOF_R, 1.34),
    (-0.40, 1.42),
    (Z_PEAK, HEIGHT),
    (0.12, 1.42),
    (Z_HEADER, 1.35),
    (Z_COWL, 1.00),
    (1.55, 0.95),
    (2.05, 0.88),
    (2.25, 0.85),
    (Z_NOSE, 0.84),
)

SILL = (
    (Z_DECK, 0.97),
    (-0.40, 0.94),
    (0.55, 0.91),
    (Z_COWL + 0.4, 0.90),
)

# Half-width as a fraction of the published half-width.
PLAN = (
    (Z_TAIL, 0.64),
    (-1.90, 0.86),
    (-1.15, 0.95),
    (0.15, 0.985),
    (Z_COWL, 0.94),
    (1.70, 0.86),
    (2.20, 0.78),
    (Z_NOSE, 0.72),
)

HALF_N = 12
IDX_BULGE = 4
IDX_BELT = 6
TUMBLE = math.tan(math.radians(11.0))

PAINT = "Lib_PaintCrimson"
GLASS = "Lib_AutoGlass"

# Inserts sit behind the creased cap, in the flat middle of the nose.
# The plan curves back at the corners, so a full-width bar would poke out.
NOSE_Z = Z_NOSE - 0.016
TAIL_Z = Z_TAIL + 0.016

YEARS = {
    2021: {"spokes": 5, "thick": 0.036, "grille": "slat4", "lamps": "separate", "tails": "separate", "mirror": PAINT, "fog": "Lib_SignalAmber"},
    2022: {"spokes": 10, "thick": 0.014, "grille": "slat8", "lamps": "separate", "tails": "separate", "mirror": PAINT, "fog": "Lib_Headlamp"},
    2023: {"spokes": 7, "thick": 0.020, "grille": "mesh", "lamps": "tier", "tails": "separate", "mirror": PAINT, "fog": "Lib_SignalAmber"},
    2024: {"spokes": 12, "thick": 0.010, "grille": "mesh", "lamps": "thin", "tails": "thin", "mirror": "Lib_Black", "fog": "Lib_Headlamp"},
    2025: {"spokes": 15, "thick": 0.007, "grille": "mesh", "lamps": "bar", "tails": "bar", "mirror": PAINT, "fog": "Lib_Headlamp"},
}

_TEMPLATE = {}


def _lerp(keys, z):
    if z <= keys[0][0]:
        return keys[0][1]
    if z >= keys[-1][0]:
        return keys[-1][1]
    for i in range(len(keys) - 1):
        z0, y0 = keys[i]
        z1, y1 = keys[i + 1]
        if z0 <= z <= z1:
            t = (z - z0) / (z1 - z0 or 1.0)
            return y0 + (y1 - y0) * t
    return keys[-1][1]


def y_crown(z):
    return _lerp(CROWN, z)


def y_sill(z):
    return _lerp(SILL, z)


def x_body(z):
    x = _lerp(PLAN, z) * HALF_W
    reach = ARCH_R + 0.08
    for axle in (Z_REAR, Z_FRONT):
        dz = abs(z - axle)
        if dz < reach:
            x += 0.030 * math.cos((dz / reach) * math.pi * 0.5)
    return min(HALF_W - 0.004, x)


def _arch_lip(z):
    lip = 0.20
    for axle in (Z_REAR, Z_FRONT):
        dz = abs(z - axle)
        if dz < ARCH_R:
            rise = math.sqrt(max(0.0, ARCH_R * ARCH_R - dz * dz))
            lip = max(lip, AXLE_Y + rise * 0.90)
    return lip


def _greenhouse_weight(z):
    room = y_crown(z) - y_sill(z)
    return max(0.0, min(1.0, (room - 0.05) / 0.16))


def half_at(z):
    """Twelve-point half section. Y climbs from the belly to the crown.

    The belt is a shoulder. Above it the side falls inward about 11 degrees
    when there is a real greenhouse, then the roof turns in to the crown.
    On the hood and the deck the greenhouse collapses onto the crown so the
    section stays a rounded panel instead of a fin.
    """
    yc = y_crown(z)
    xb = x_body(z)
    w = _greenhouse_weight(z)
    yb = (1.0 - w) * (yc - 0.050) + w * min(y_sill(z), yc - 0.10)
    yb = min(max(yb, BELLY + 0.20), yc - 0.040)
    lip = min(_arch_lip(z), yb - 0.10)
    y_rock = min(max(lip, BELLY + 0.06), yb - 0.10)

    ys = [0.0] * HALF_N
    ys[0] = BELLY
    ys[1] = BELLY + (y_rock - BELLY) * 0.42
    ys[2] = y_rock
    ys[3] = y_rock + (yb - y_rock) * 0.40
    ys[IDX_BULGE] = y_rock + (yb - y_rock) * 0.68
    ys[5] = y_rock + (yb - y_rock) * 0.86
    ys[IDX_BELT] = yb
    span = yc - yb
    for i, frac in ((7, 0.30), (8, 0.55), (9, 0.76), (10, 0.93)):
        ys[i] = yb + span * frac
    ys[-1] = yc
    for i in range(1, HALF_N):
        if ys[i] < ys[i - 1] + 0.006:
            ys[i] = ys[i - 1] + 0.006
    if ys[-1] > yc:
        # Collapsed crown: pack the last points under the real crown height.
        ys[-1] = yc
        for i in range(HALF_N - 2, IDX_BELT, -1):
            ys[i] = min(ys[i], ys[i + 1] - 0.006)

    xs = [0.0] * HALF_N
    xs[1] = xb * 0.46
    xs[2] = xb * 0.84
    xs[3] = xb * 0.95
    xs[IDX_BULGE] = xb
    xs[5] = xb * 0.985
    xs[IDX_BELT] = xb * 0.955
    pull = TUMBLE if w > 0.45 else (0.55 * xs[IDX_BELT] / max(span, 0.05))
    for i in (7, 8, 9):
        xs[i] = xs[IDX_BELT] - (ys[i] - yb) * pull
    xs[9] = max(0.06, min(xs[9], xs[IDX_BELT] - 0.02))
    xs[10] = max(0.03, xs[9] * 0.40)
    for i in range(1, HALF_N - 1):
        xs[i] = max(0.02, min(HALF_W - 0.002, xs[i]))
    return list(zip(xs, ys))


def skin_x(z, y):
    half = half_at(z)
    if y <= half[0][1]:
        return half[1][0]
    if y >= half[-1][1]:
        return 0.0
    for i in range(len(half) - 1):
        y0, y1 = half[i][1], half[i + 1][1]
        if y0 <= y <= y1 and y1 > y0:
            t = (y - y0) / (y1 - y0)
            return half[i][0] + (half[i + 1][0] - half[i][0]) * t
    return half[IDX_BELT][0]


def _stations(lod):
    raw = [z for z, _y in CROWN]
    raw.append((Z_HEADER + Z_COWL) * 0.5)
    raw.append(Z_REAR)
    raw.append(Z_FRONT)
    if lod == 0:
        for axle in (Z_REAR, Z_FRONT):
            raw.append(axle - 0.26)
            raw.append(axle + 0.26)
    zs = sorted(set(round(z, 4) for z in raw if Z_TAIL - 1e-4 <= z <= Z_NOSE + 1e-4))
    kept = [zs[0]]
    gap = 0.05 if lod == 0 else (0.12 if lod == 1 else 0.22)
    for z in zs[1:]:
        if z - kept[-1] < gap and abs(z - Z_NOSE) > 1e-4:
            continue
        kept.append(z)
    if abs(kept[0] - Z_TAIL) > 1e-4:
        kept[0] = Z_TAIL
    if abs(kept[-1] - Z_NOSE) > 1e-4:
        kept.append(Z_NOSE)
    if lod >= 2 and len(kept) > 10:
        core = {round(Z_TAIL, 3), round(Z_NOSE, 3), round(Z_COWL, 3), round(Z_HEADER, 3), round(Z_PEAK, 3), round(Z_DECK, 3)}
        slim = [z for z in kept if round(z, 3) in core or z in (Z_REAR, Z_FRONT)]
        if len(slim) >= 8:
            kept = slim
    return kept


def _hard(rings):
    """Crisp nose and tail caps, and the belt line through the cabin."""
    count = len(rings[0])
    pairs = []
    belt_ids = (IDX_BELT, count - IDX_BELT)
    for i in range(len(rings) - 1):
        z0 = rings[i][0][2]
        if Z_DECK + 0.05 < z0 < Z_COWL - 0.02:
            for idx in belt_ids:
                pairs.append((i * count + idx, (i + 1) * count + idx))
    n = len(rings)
    for ring_i in (0, n - 1):
        base = ring_i * count
        for i in range(count):
            pairs.append((base + i, base + (i + 1) % count))
        other = 1 if ring_i == 0 else n - 2
        ob = other * count
        for i in range(count):
            pairs.append((base + i, ob + i))
    nose_hub = n * count
    tail_hub = nose_hub + 1
    for i in range(count):
        pairs.append((nose_hub, i))
        pairs.append((tail_hub, (n - 1) * count + i))
    return pairs


def _wells():
    length = 0.24
    return [
        ((sign * (TIRE_X + 0.045), AXLE_Y, axle), ARCH_R - 0.01, length)
        for axle in (Z_REAR, Z_FRONT)
        for sign in (-1.0, 1.0)
    ]


def _u_co(co):
    return blender_to_unity(co.x, co.y, co.z)


def _u_n(normal):
    return (normal.x, normal.z, -normal.y)


def _islands(bm):
    seen = set()
    islands = []
    bm.faces.ensure_lookup_table()
    for face in bm.faces:
        if face in seen:
            continue
        stack = [face]
        seen.add(face)
        group = [face]
        while stack:
            current = stack.pop()
            for edge in current.edges:
                for other in edge.link_faces:
                    if other in seen:
                        continue
                    seen.add(other)
                    stack.append(other)
                    group.append(other)
        islands.append(group)
    return islands


def _hold_crown(bm):
    """Put the smoothed centerline back on the published profile.

    Subdivision pulls the crown down toward the roof shoulders, which flattens
    the windshield. Lift falls off toward the sides so the tumble stays.
    """
    for vert in bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        target = y_crown(z)
        if y < 0.60 or y > target - 0.002:
            continue
        band = 0.42
        if abs(x) >= band:
            continue
        influence = (1.0 - abs(x) / band) ** 2
        short = target - y
        if short < 0.004 or influence < 0.04:
            continue
        vert.co.z += short * influence


def _drop_specks(bm, limit=16):
    islands = _islands(bm)
    if len(islands) < 2:
        return 0
    islands.sort(key=len)
    dropped = 0
    kill = []
    for group in islands[:-1]:
        if len(group) <= limit:
            kill.extend(group)
            dropped += 1
    if kill:
        bmesh.ops.delete(bm, geom=kill, context="FACES")
    return dropped


def _in_greenhouse(y, z):
    """Above the belt and below the roof skin. This is the window band."""
    sill = y_sill(z)
    crown = y_crown(z)
    if crown - sill < 0.12:
        return False
    top = sill + (crown - sill) * 0.80
    return sill + 0.02 < y < top


def _pick_faces(bm):
    bm.faces.ensure_lookup_table()
    bm.normal_update()
    groups = {name: [] for name in (
        "windshield", "front_door", "rear_door", "quarter", "backlight",
        "grille", "lamp", "tail",
    )}
    debug = []
    for face in bm.faces:
        x, y, z = _u_co(face.calc_center_median())
        nx, ny, nz = _u_n(face.normal)
        if z > 1.70 and nz > 0.62 and 0.28 < y < 0.50 and abs(x) < 0.58:
            groups["grille"].append(face)
            continue
        if z > 1.70 and nz > 0.55 and 0.60 < y < 0.78 and abs(x) < 0.70:
            groups["lamp"].append(face)
            continue
        if z < -1.70 and nz < -0.55 and 0.68 < y < 0.86 and abs(x) < 0.62:
            groups["tail"].append(face)
            continue
        # Windshield: the raked center of the greenhouse, joined to the roof.
        if (
            Z_HEADER + 0.02 < z < Z_COWL - 0.03
            and abs(x) < 0.64
            and ny > 0.25 and nz > 0.08
            and y > max(1.00, y_sill(z) + 0.03)
            and y < y_crown(z) - 0.012
        ):
            groups["windshield"].append(face)
            continue
        if (
            Z_DECK + 0.06 < z < Z_ROOF_R - 0.04
            and abs(x) < 0.55
            and ny > 0.25 and nz < -0.08
            and y > 1.04
        ):
            groups["backlight"].append(face)
            continue
        # Side glass by position, so the whole opening above the belt is glass.
        # Outward normal keeps the wheel-well interior out of the set.
        outward = nx * (1.0 if x >= 0.0 else -1.0)
        if abs(x) > 0.28 and outward > 0.20 and _in_greenhouse(y, z):
            if 0.12 < z < 0.92:
                groups["front_door"].append(face)
            elif -0.66 < z < -0.04:
                groups["rear_door"].append(face)
            elif -1.08 < z < -0.76:
                groups["quarter"].append(face)
        elif Z_HEADER < z < Z_COWL and y > 1.05 and len(debug) < 6:
            debug.append((round(x, 2), round(y, 2), round(z, 2), round(nx, 2), round(ny, 2), round(nz, 2)))
    return groups, debug


def _inset(bm, faces, thickness, depth):
    if len(faces) < 4:
        return []
    result = bmesh.ops.inset_region(
        bm,
        faces=faces,
        thickness=thickness,
        depth=depth,
        use_boundary=True,
        use_even_offset=True,
        use_interpolate=True,
    )
    return [face for face in result.get("faces", []) if face.is_valid]


def _paint_inset(bm, faces, glass_idx, black_idx, glass):
    """Inset a region.

    inset_region returns the new border faces. The faces passed in stay, shrunk,
    as the recessed inner panel. Glass (or the lamp cavity) is that inner panel.
    The border is the black surround.
    """
    if len(faces) < 4:
        return 0
    depth = -0.012 if glass else -0.016
    border = _inset(bm, faces, 0.016 if glass else 0.012, depth)
    target = glass_idx if glass else black_idx
    for face in faces:
        if face.is_valid:
            face.material_index = target
    for face in border:
        if face.is_valid:
            face.material_index = black_idx
    return len(faces)


def _inset_openings(g, lod):
    bm = g.bm
    groups, debug = _pick_faces(bm)
    counts = {name: len(faces) for name, faces in groups.items()}
    print("GLASS_PICK", "lod", lod, counts)
    if lod == 0 and counts["windshield"] < 8:
        print("WINDSHIELD_DEBUG", debug)
        raise RuntimeError("windshield inset found %d faces" % counts["windshield"])
    glass_idx = g.slot(GLASS)
    black_idx = g.slot("Lib_Black")
    for name in ("windshield", "front_door", "rear_door", "quarter", "backlight"):
        _paint_inset(bm, groups[name], glass_idx, black_idx, glass=True)
    for name in ("grille", "lamp", "tail"):
        _paint_inset(bm, groups[name], glass_idx, black_idx, glass=False)
    for face in bm.faces:
        face.smooth = True
    bm.normal_update()
    if lod == 0 and counts["lamp"] < 4:
        print("LAMP_PICK_LOW", counts["lamp"])
    if lod == 0 and counts["grille"] < 4:
        print("GRILLE_PICK_LOW", counts["grille"])


def _section_report(zs):
    rake = math.degrees(math.atan2(1.35 - 1.00, Z_COWL - Z_HEADER))
    print("BODY_A_STATIONS", len(zs), "RAKE_DEG", round(rake, 2))
    half = half_at(0.0)
    belt = half[IDX_BELT]
    upper = half[9]
    dy = upper[1] - belt[1]
    dx = belt[0] - upper[0]
    tumble = math.degrees(math.atan2(dx, dy)) if dy > 0.02 else 0.0
    print(
        "BODY_A_TUMBLE_DEG", round(tumble, 2),
        "belt", tuple(round(v, 3) for v in belt),
        "rail", tuple(round(v, 3) for v in upper),
        "crown", round(half[-1][1], 3),
    )
    for z in (Z_NOSE, 1.55, Z_COWL, (Z_HEADER + Z_COWL) * 0.5, Z_HEADER, 0.0, Z_ROOF_R, Z_DECK, Z_TAIL):
        sec = half_at(z)
        print(
            "BODY_A_SEC z=%.3f crown=%.3f belt=%.3f xbelt=%.3f bulge=%.3f" % (
                z, sec[-1][1], sec[IDX_BELT][1], sec[IDX_BELT][0], sec[IDX_BULGE][0],
            )
        )


def build_shell(g, lod):
    zs = _stations(lod)
    if lod == 0:
        _section_report(zs)
        if len(zs) < 14:
            raise RuntimeError("expected at least 14 stations, got %d" % len(zs))
    rings = [shell._ring(half_at(z), z) for z in zs]
    level = 2 if lod == 0 else (1 if lod == 1 else 0)
    hard = _hard(rings) if level else ()
    shell._loft(g, rings, PAINT, level, _wells(), [], [], 70.0, hard, joined=False)
    if level:
        _hold_crown(g.bm)
    specks = _drop_specks(g.bm)
    if specks:
        print("BODY_A_SPECKS", "lod", lod, specks)
    if lod < 2:
        _inset_openings(g, lod)


def _copy_open(src, asset, lod):
    g = asset.begin(lod)
    index = []
    for mat in src.mats:
        index.append(g.slot(mat))
    vmap = {}
    for vert in src.bm.verts:
        vmap[vert] = g.bm.verts.new(vert.co.copy())
    g.bm.verts.index_update()
    for face in src.bm.faces:
        try:
            nf = g.bm.faces.new([vmap[vert] for vert in face.verts])
        except ValueError:
            continue
        nf.material_index = index[face.material_index]
        nf.smooth = face.smooth
        nf[g.scale_layer] = face[src.scale_layer]
    return g


def template(asset_cls):
    if _TEMPLATE:
        return _TEMPLATE["asset"]
    asset = asset_cls("_SedanMidAShell", "Vehicles", "Shared midsize shell.")
    for lod in (0, 1, 2):
        g = asset.begin(lod)
        build_shell(g, lod)
        asset.end()
    _TEMPLATE["asset"] = asset
    return asset


def _wheel(g, x, z, year, lod):
    sign = 1.0 if x > 0.0 else -1.0
    info = YEARS[year]
    segs = 18 if lod == 0 else (12 if lod == 1 else 8)
    carcass_w = TIRE_W * 0.82
    g.cylinder((x, AXLE_Y, z), TIRE_R * 0.975, carcass_w, "Lib_Rubber", segs, axis="X")
    if lod == 0:
        shell._torus_x(
            g, (x + sign * carcass_w * 0.46, AXLE_Y, z),
            TIRE_R * 0.90, 0.013, "Lib_Rubber", 12, 6,
        )
    face = x + sign * (carcass_w * 0.50 + 0.006)
    g.cylinder((face, AXLE_Y, z), RIM_R, 0.016, "Lib_Steel", segs, axis="X")
    g.cylinder((face + sign * 0.007, AXLE_Y, z), RIM_R * 0.22, 0.012, "Lib_SteelDark", max(8, segs // 2), axis="X")
    spokes = info["spokes"]
    if lod == 1:
        spokes = max(5, spokes // 2)
    if lod == 2:
        spokes = 5
    thick = info["thick"] if lod == 0 else max(info["thick"], 0.016)
    for k in range(spokes):
        theta = math.radians(k * (360.0 / spokes) + 7.0)
        arm = RIM_R * 0.50
        g.box(
            (face, AXLE_Y + math.cos(theta) * arm, z + math.sin(theta) * arm),
            (0.010, RIM_R * 0.76, thick),
            "Lib_Steel",
            euler=(math.degrees(theta), 0.0, 0.0),
        )
    if lod == 0:
        for i in range(12):
            theta = math.radians(i * 30.0 + 4.0)
            rad = TIRE_R - 0.012
            g.box(
                (x, AXLE_Y + math.cos(theta) * rad, z + math.sin(theta) * rad),
                (carcass_w * 0.50, 0.012, 0.038),
                "Lib_Rubber",
                euler=(math.degrees(theta), 0.0, 0.0),
            )


def _limit(z, y, margin):
    """Half-width that stays on the flat cap, inside the curved corners."""
    return max(0.16, skin_x(z, y) * 0.72 - margin)


def _grille(g, year, lod):
    kind = YEARS[year]["grille"]
    y = 0.39
    h = 0.14
    z = NOSE_Z - 0.002
    w = _limit(Z_NOSE, y, 0.02) * 2.0
    if kind == "slat4":
        for i in range(4):
            yy = y - h * 0.36 + i * (h * 0.24)
            g.box((0.0, yy, z), (w, 0.008, 0.004), "Lib_SteelDark")
    elif kind == "slat8":
        count = 6 if lod == 0 else 3
        for i in range(count):
            yy = y - h * 0.38 + i * (h * 0.15)
            g.box((0.0, yy, z), (w - 0.04, 0.005, 0.004), "Lib_Steel")
    else:
        rows = 3 if lod == 0 else 2
        cols = 6 if lod == 0 else 3
        for i in range(rows):
            yy = y - h * 0.30 + i * (h * 0.30)
            g.box((0.0, yy, z), (w, 0.004, 0.003), "Lib_Black")
        for i in range(cols):
            xx = -w * 0.40 + i * (w * 0.80 / max(1, cols - 1))
            g.box((xx, y, z), (0.004, h * 0.72, 0.003), "Lib_Black")


def _lamps_front(g, year):
    kind = YEARS[year]["lamps"]
    y = 0.69
    z = NOSE_Z
    limit = _limit(Z_NOSE, y, 0.02)
    if kind == "bar":
        g.box((0.0, y, z), (limit * 2.0, 0.022, 0.005), "Lib_Headlamp")
        return
    span = 0.36 if kind != "thin" else 0.40
    tall = 0.038 if kind == "separate" else 0.016
    x = min(0.48, limit - span * 0.5 - 0.02)
    for sign in (1.0, -1.0):
        if kind == "tier":
            g.box((sign * x, y + 0.008, z), (span, 0.012, 0.005), "Lib_Headlamp")
            g.box((sign * x, y - 0.010, z - 0.001), (span * 0.82, 0.010, 0.005), "Lib_Headlamp")
        else:
            g.box((sign * x, y, z - 0.002), (span + 0.02, tall + 0.010, 0.004), "Lib_Black")
            g.box((sign * x, y, z), (span, tall, 0.005), "Lib_Headlamp")


def _lamps_rear(g, year):
    kind = YEARS[year]["tails"]
    y = 0.78
    z = TAIL_Z
    limit = _limit(Z_TAIL, y, 0.05)
    if kind == "bar":
        g.box((0.0, y, z), (limit * 2.0, 0.024, 0.005), "Lib_Taillamp")
        return
    span = 0.30 if kind != "thin" else 0.34
    tall = 0.034 if kind == "separate" else 0.016
    x = min(0.46, limit - span * 0.5 - 0.02)
    for sign in (1.0, -1.0):
        g.box((sign * x, y, z), (span, tall, 0.006), "Lib_Taillamp")


def _mirrors(g, year, lod):
    mat = YEARS[year]["mirror"]
    z = 0.72
    y = y_sill(z) + 0.015
    x_skin = skin_x(z, y)
    bev = 0.004 if lod == 0 else 0.0
    segs = 1 if lod == 0 else 0
    for sign in (1.0, -1.0):
        g.cylinder((sign * (x_skin + 0.03), y, z), 0.010, 0.06, mat, 8, axis="X")
        cap_x = x_skin + 0.045 + 0.045
        g.box((sign * cap_x, y, z), (0.09, 0.11, 0.16), mat, bevel=bev, segs=segs)
        g.box((sign * (cap_x + 0.04), y, z + 0.01), (0.006, 0.07, 0.11), "Lib_SteelDark")


def _fogs(g, year):
    mat = YEARS[year]["fog"]
    y = 0.27
    limit = _limit(Z_NOSE, y, 0.05)
    x = min(0.46, limit - 0.10)
    for sign in (1.0, -1.0):
        g.box((sign * x, y, NOSE_Z), (0.14, 0.032, 0.005), mat)


def dress(g, year, lod, paint):
    del paint
    for axle in (Z_REAR, Z_FRONT):
        for sign in (-1.0, 1.0):
            _wheel(g, sign * TIRE_X, axle, year, lod)
    _grille(g, year, lod)
    _lamps_front(g, year)
    _lamps_rear(g, year)
    _fogs(g, year)
    if lod < 2:
        _mirrors(g, year, lod)


def add_colliders(asset):
    # Kept in the carcass. A wider box reaches the wheel-well opening and
    # the parity test calls that outside.
    yz = TIRE_R * 0.22
    for i, axle in enumerate((Z_REAR, Z_FRONT)):
        for j, sign in enumerate((-1.0, 1.0)):
            asset.box(
                "Col_Wheel_%d%d" % (i, j),
                (sign * TIRE_X, AXLE_Y, axle),
                (0.008, yz, yz),
            )
    asset.box("Col_Cabin", (0.0, 0.46, -0.05), (0.62, 0.32, 0.90))
    asset.box("Col_Roof", (0.0, 1.28, -0.05), (0.36, 0.06, 0.48))
    asset.box("Col_Hood", (0.0, 0.70, 1.62), (0.55, 0.16, 0.48))
    asset.box("Col_Deck", (0.0, 0.76, -1.62), (0.55, 0.14, 0.36))


def probe_spec(name):
    ny, nz = shell._rake_normal(1.35, Z_HEADER, 1.00, Z_COWL)
    y = (1.35 + 1.00) * 0.5
    z = (Z_HEADER + Z_COWL) * 0.5
    return {
        "name": name,
        "glass_tests": [
            (0.0, y + ny * 0.04, z + nz * 0.04),
            (0.0, y - ny * 0.05, z - nz * 0.05),
        ],
    }


def _centroid(faces):
    acc = [0.0, 0.0, 0.0]
    for face in faces:
        x, y, z = _u_co(face.calc_center_median())
        acc[0] += x
        acc[1] += y
        acc[2] += z
    n = float(len(faces) or 1)
    return (acc[0] / n, acc[1] / n, acc[2] / n)


def _near_wheel(center):
    x, y, z = center
    if abs(abs(x) - TIRE_X) > 0.22:
        return False
    for axle in (Z_REAR, Z_FRONT):
        if math.hypot(y - AXLE_Y, z - axle) < TIRE_R + 0.08:
            return True
    return False


def _near_mirror(center):
    x, y, z = center
    return abs(x) > 0.85 and 0.75 < y < 1.20 and 0.35 < z < 1.15


def _near_fascia(center):
    _x, y, z = center
    return (z > 1.45 and y < 1.05) or (z < -1.45 and y < 1.05)


def self_check(asset):
    """Print roof clearance, island classes, and side-silhouette deviation."""
    bm = asset.lods[0].bm
    mats = asset.lods[0].mats
    over = []
    for vert in bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        if abs(x) > x_body(z) + 0.03:
            continue
        limit = y_crown(z) + 0.035
        if y > limit and y > 1.02:
            over.append((round(x, 3), round(y, 3), round(z, 3), round(y - y_crown(z), 3)))
    over.sort(key=lambda row: -row[3])
    print("SHELL_CHECK faces_above_roof", len(over), "worst", over[:6])

    islands = _islands(bm)
    islands.sort(key=len, reverse=True)
    counts = {"body": 0, "wheel": 0, "mirror": 0, "lamp": 0, "glass": 0, "extra": 0}
    extras = []
    for index, group in enumerate(islands):
        center = _centroid(group)
        names = set()
        for face in group:
            if 0 <= face.material_index < len(mats):
                names.add(mats[face.material_index])
        if index == 0:
            kind = "body"
        elif _near_wheel(center) or "Lib_Rubber" in names:
            kind = "wheel"
        elif _near_mirror(center):
            kind = "mirror"
        elif _near_fascia(center) or names & {"Lib_Headlamp", "Lib_Taillamp", "Lib_SignalAmber"}:
            kind = "lamp"
        elif names <= {GLASS}:
            kind = "glass"
        else:
            kind = "extra"
            extras.append((len(group), tuple(round(v, 3) for v in center), sorted(names)))
        counts[kind] += 1
    print(
        "SHELL_CHECK islands body=%d wheels=%d mirrors=%d lamps=%d glass=%d extra=%d" % (
            counts["body"], counts["wheel"], counts["mirror"], counts["lamp"], counts["glass"], counts["extra"],
        )
    )
    if extras:
        print("SHELL_CHECK extra_detail", extras[:12])

    best = {}
    max_z = Z_TAIL
    min_z = Z_NOSE
    for vert in bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        max_z = max(max_z, z)
        min_z = min(min_z, z)
        if abs(x) > 0.22:
            continue
        bucket = round(z * 20.0) / 20.0
        prev = best.get(bucket)
        if prev is None or y > prev:
            best[bucket] = y
    worst = (0.0, 0.0, 0.0, 0.0)
    rows = []
    z = Z_TAIL
    while z <= Z_NOSE + 1e-6:
        bucket = round(z * 20.0) / 20.0
        mesh_y = best.get(bucket)
        target = y_crown(z)
        if mesh_y is not None:
            delta_cm = (mesh_y - target) * 100.0
            if abs(delta_cm) >= abs(worst[0]):
                worst = (delta_cm, z, mesh_y, target)
            if abs((z * 4.0) - round(z * 4.0)) < 0.04:
                rows.append("%+.2f: mesh %.3f target %.3f d %+.1fcm" % (z, mesh_y, target, delta_cm))
        z += 0.05
    print("SHELL_CHECK silhouette max_dev_cm %.2f at z=%.2f mesh=%.3f target=%.3f" % worst)
    print("SHELL_CHECK profile")
    for row in rows:
        print("  ", row)
    print(
        "SHELL_CHECK ends max_z=%.3f nose=%.3f min_z=%.3f tail=%.3f" % (
            max_z, Z_NOSE, min_z, Z_TAIL,
        )
    )
    problems = []
    if over:
        problems.append("faces above the roof")
    if counts["body"] != 1:
        problems.append("body islands %d" % counts["body"])
    if counts["extra"]:
        problems.append("extra islands %d" % counts["extra"])
    if abs(worst[0]) > 8.0:
        problems.append("silhouette deviation %.1f cm" % worst[0])
    if max_z > Z_NOSE + 0.012 or min_z < Z_TAIL - 0.012:
        problems.append("fascia past the cap")
    if problems:
        raise RuntimeError("shell check failed: " + "; ".join(problems))
    print("SHELL_CHECK ok")


def measure(asset):
    self_check(asset)
    best = {}
    max_x = 0.0
    max_y = 0.0
    min_y = 99.0
    max_z = -99.0
    min_z = 99.0
    for vert in asset.lods[0].bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        max_x = max(max_x, abs(x))
        max_y = max(max_y, y)
        min_y = min(min_y, y)
        max_z = max(max_z, z)
        min_z = min(min_z, z)
        if abs(x) > 0.15:
            continue
        bucket = round(z * 10.0) / 10.0
        prev = best.get(bucket)
        if prev is None or y > prev[1]:
            best[bucket] = (z, y)

    def near(z_target):
        pick = None
        for _bucket, (z, y) in best.items():
            if abs(z - z_target) < 0.08 and (pick is None or abs(z - z_target) < abs(pick[0] - z_target)):
                pick = (z, y)
        return pick

    header = near(Z_HEADER)
    cowl = near(Z_COWL)
    angle = None
    if header and cowl and abs(cowl[0] - header[0]) > 0.05:
        angle = math.degrees(math.atan2(header[1] - cowl[1], cowl[0] - header[0]))
    print(
        "BODY_A_MEASURE", asset.name,
        "rake", None if angle is None else round(angle, 1),
        "header", None if header is None else tuple(round(v, 3) for v in header),
        "cowl", None if cowl is None else tuple(round(v, 3) for v in cowl),
        "size", round(max_x * 2.0, 3), round(max_y - min_y, 3), round(max_z - min_z, 3),
        "ymax", round(max_y, 3),
    )
    return angle
