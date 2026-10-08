"""Modern midsize sedan shell. One loft for every model year.

The side, plan, and section curves are built from the published inch sheet
(length 193.5, width 72.4, height 56.9, wheelbase 111.2, clearance 5.4,
front overhang 39.0) and from the silhouette a current family sedan has:
a long low hood, a windshield about 27 degrees from horizontal, a roof arc,
and a fastback pillar into a short deck. Nothing here is traced from a drawing.

The cage is subdivided (level 2 on LOD0) and the creases are applied before
the glass, lamp, and panel-gap cuts. Years share that shell and only change
the fascia, lamps, wheels, and mirror color.
"""

import math

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
# Tuck the 235 section inside the published body so the fender still covers it.
TIRE_X = min(63.0 * INCH * 0.5, HALF_W - TIRE_W * 0.5 - 0.015)

Z_NOSE = HALF_L
Z_LIP = 2.26
Z_COWL = 1.28
Z_HEADER = 0.50
Z_PEAK = -0.08
Z_ROOF_R = -0.78
Z_DECK = -1.40
Z_DECK_R = -2.02
Z_TAIL = -HALF_L

Y_LIP = 0.73
Y_COWL = 0.92
Y_HEADER = 1.40
Y_PEAK = HEIGHT + 0.084
Y_ROOF_R = 1.355
Y_DECK = 1.09
Y_DECK_R = 1.03
Y_NOSE = 0.50
Y_TAIL = 0.50

PAINT = "Lib_PaintCrimson"
GLASS = "Lib_AutoGlass"

_SLAB = [
    (0, 1, 2, 3),
    (4, 7, 6, 5),
    (0, 4, 5, 1),
    (1, 5, 6, 2),
    (2, 6, 7, 3),
    (3, 7, 4, 0),
]

# Spokes, spoke thickness (local Z), grille, lamps, mirror material.
YEARS = {
    2021: {"spokes": 5, "thick": 0.036, "grille": "slat4", "lamps": "separate", "tails": "separate", "mirror": PAINT, "fog": "Lib_SignalAmber"},
    2022: {"spokes": 10, "thick": 0.014, "grille": "slat8", "lamps": "separate", "tails": "separate", "mirror": PAINT, "fog": "Lib_Headlamp"},
    2023: {"spokes": 7, "thick": 0.020, "grille": "mesh", "lamps": "tier", "tails": "separate", "mirror": PAINT, "fog": "Lib_SignalAmber"},
    2024: {"spokes": 12, "thick": 0.010, "grille": "mesh", "lamps": "thin", "tails": "thin", "mirror": "Lib_Black", "fog": "Lib_Headlamp"},
    2025: {"spokes": 15, "thick": 0.007, "grille": "mesh", "lamps": "bar", "tails": "bar", "mirror": PAINT, "fog": "Lib_Headlamp"},
}

_TEMPLATE = {}


def _ss(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def _lerp_x(keys, z):
    if z <= keys[0][0]:
        return keys[0][1]
    if z >= keys[-1][0]:
        return keys[-1][1]
    for i in range(len(keys) - 1):
        z0, x0 = keys[i]
        z1, x1 = keys[i + 1]
        if z0 <= z <= z1:
            t = (z - z0) / (z1 - z0 or 1.0)
            return x0 + (x1 - x0) * t
    return keys[-1][1]


def y_crown(z):
    """Centerline of the body, nose to tail. Windshield is a straight 27 degree run."""
    if z >= Z_LIP:
        return Y_LIP + (Y_NOSE - Y_LIP) * _ss((z - Z_LIP) / (Z_NOSE - Z_LIP))
    if z >= Z_COWL:
        t = (Z_LIP - z) / (Z_LIP - Z_COWL)
        return Y_LIP + (Y_COWL - Y_LIP) * t
    if z >= Z_HEADER:
        t = (Z_COWL - z) / (Z_COWL - Z_HEADER)
        return Y_COWL + (Y_HEADER - Y_COWL) * t
    if z >= Z_ROOF_R:
        span = Z_HEADER - Z_ROOF_R
        t = (Z_HEADER - z) / span
        base = Y_HEADER + (Y_ROOF_R - Y_HEADER) * t
        t_peak = (Z_HEADER - Z_PEAK) / span
        shape = math.sin(math.pi * t)
        shape_peak = math.sin(math.pi * t_peak) or 1.0
        base_peak = Y_HEADER + (Y_ROOF_R - Y_HEADER) * t_peak
        return base + (Y_PEAK - base_peak) * (shape / shape_peak)
    if z >= Z_DECK:
        t = (Z_ROOF_R - z) / (Z_ROOF_R - Z_DECK)
        return Y_ROOF_R + (Y_DECK - Y_ROOF_R) * t
    if z >= Z_DECK_R:
        t = (Z_DECK - z) / (Z_DECK - Z_DECK_R)
        return Y_DECK + (Y_DECK_R - Y_DECK) * t
    t = (Z_DECK_R - z) / (Z_DECK_R - Z_TAIL or 1.0)
    return Y_DECK_R + (Y_TAIL - Y_DECK_R) * _ss(t)


def y_belt(z):
    """Sill rises from the cowl toward the deck."""
    t = _ss((Z_COWL - z) / (Z_COWL - Z_DECK))
    return 0.78 + 0.20 * t


def y_char(z):
    if z >= Z_COWL:
        return y_crown(z) - 0.055
    t = _ss((Z_COWL - z) / (Z_COWL - Z_DECK))
    return 0.56 + 0.14 * t


def x_plan(z):
    """Half-width. The arch is the widest point and stays inside the published width."""
    door = HALF_W - 0.034
    keys = [
        (Z_TAIL, HALF_W * 0.68),
        (Z_DECK_R, HALF_W * 0.86),
        (Z_DECK, HALF_W * 0.93),
        (Z_REAR - 0.15, door - 0.01),
        (0.0, door),
        (Z_FRONT + 0.15, door - 0.012),
        (Z_COWL, HALF_W * 0.90),
        (Z_LIP, HALF_W * 0.80),
        (Z_NOSE, HALF_W * 0.70),
    ]
    x = _lerp_x(keys, z)
    reach = ARCH_R + 0.10
    for axle in (Z_REAR, Z_FRONT):
        dz = abs(z - axle)
        if dz < reach:
            x += 0.055 * math.cos((dz / reach) * math.pi * 0.5)
    return min(HALF_W, x)


def _half_raw(z, mode):
    yc = y_crown(z)
    x = x_plan(z)
    yb = y_belt(z)
    yk = y_char(z)
    poke = 0.015 if mode == "cabin" else 0.002
    pts = [
        (0.0, BELLY),
        (min(0.40, x * 0.46), BELLY + 0.014),
        (x * 0.80, 0.24),
    ]
    if mode == "cabin":
        pts.extend((
            (x * 0.985, max(0.40, yk - 0.15)),
            (min(HALF_W, x + poke), yk),
            (x * 0.97, yk + 0.09),
            (x * 0.945, yb),
            (x * 0.76, yb + 0.20),
            (x * 0.44, min(yc - 0.045, yb + 0.40)),
            (0.0, yc),
        ))
    else:
        shoulder = min(yc - 0.045, max(yk, yc - 0.06))
        pts.extend((
            (x * 0.97, max(0.36, shoulder - 0.10)),
            (min(HALF_W, x + poke), shoulder),
            (x * 0.90, shoulder + 0.018),
            (x * 0.72, min(yc - 0.016, shoulder + 0.04)),
            (x * 0.46, min(yc - 0.008, shoulder + 0.055)),
            (x * 0.18, min(yc - 0.003, shoulder + 0.065)),
            (0.0, yc),
        ))
    return pts


def _mix(a, b, w):
    return [(a[i][0] + (b[i][0] - a[i][0]) * w, a[i][1] + (b[i][1] - a[i][1]) * w) for i in range(len(a))]


def _monotonic(pts):
    out = []
    prev = -1.0
    last = len(pts) - 1
    for i, (x, y) in enumerate(pts):
        y = max(y, prev + 0.005)
        x = 0.0 if i == last else max(0.0, min(HALF_W, x))
        out.append((x, y))
        prev = y
    return out


def half_at(z):
    hood = _half_raw(z, "hood")
    cabin = _half_raw(z, "cabin")
    if z >= Z_COWL:
        w = 0.0
    elif z <= Z_COWL - 0.18:
        w = 1.0
    else:
        w = _ss((Z_COWL - z) / 0.18)
    if z <= Z_DECK:
        w = 0.0
    elif z < Z_DECK + 0.20:
        w *= _ss((z - Z_DECK) / 0.20)
    return _monotonic(_mix(hood, cabin, w))


def skin_x(z, y):
    half = half_at(z)
    if y <= half[0][1]:
        return half[1][0]
    if y >= half[-1][1]:
        return half[-2][0]
    for i in range(len(half) - 1):
        y0, y1 = half[i][1], half[i + 1][1]
        if y0 <= y <= y1 and y1 > y0:
            t = (y - y0) / (y1 - y0)
            return half[i][0] + (half[i + 1][0] - half[i][0]) * t
    return half[4][0]


def _stations(step):
    zs = []
    z = Z_TAIL
    while z < Z_NOSE - 1e-6:
        zs.append(z)
        z += step
    zs.append(Z_NOSE)
    extras = (
        Z_LIP, Z_COWL, Z_COWL - 0.18, Z_HEADER, Z_PEAK, Z_ROOF_R,
        Z_DECK, Z_DECK + 0.20, Z_DECK_R, Z_REAR, Z_FRONT,
        Z_REAR - ARCH_R, Z_REAR + ARCH_R, Z_FRONT - ARCH_R, Z_FRONT + ARCH_R,
    )
    for extra in extras:
        if Z_TAIL < extra < Z_NOSE:
            zs.append(extra)
    zs = sorted(set(round(v, 4) for v in zs))
    out = [zs[0]]
    for z in zs[1:]:
        if z - out[-1] < 0.045:
            if abs(z - Z_NOSE) < 1e-6:
                out[-1] = z
            continue
        out.append(z)
    if abs(out[-1] - Z_NOSE) > 1e-6:
        out.append(Z_NOSE)
    if abs(out[0] - Z_TAIL) > 1e-6:
        out[0] = Z_TAIL
    return out


def _hard(rings):
    """Character line through the cabin, plus crisp nose and tail caps."""
    count = len(rings[0])
    # Right character is section point 4. Left mirror is count - 4.
    char = (4, count - 4)
    pairs = []
    for i in range(len(rings) - 1):
        z0 = rings[i][0][2]
        z1 = rings[i + 1][0][2]
        if Z_DECK + 0.12 < min(z0, z1) and max(z0, z1) < Z_COWL - 0.10:
            for idx in char:
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
    # Cap hubs added by the loft, in this order: nose ring, then tail ring.
    nose_hub = n * count
    tail_hub = nose_hub + 1
    for i in range(count):
        pairs.append((nose_hub, i))
        pairs.append((tail_hub, (n - 1) * count + i))
    return pairs


def _doors():
    front_jamb = Z_FRONT - ARCH_R - 0.05
    rear_jamb = Z_REAR + ARCH_R + 0.06
    b_pillar = (Z_FRONT + Z_REAR) * 0.5
    return (
        (b_pillar + 0.006, front_jamb),
        (rear_jamb, b_pillar - 0.006),
    )


def _panel_pockets():
    """4 mm grooves. Cut after subdivision so the line stays a line."""
    pockets = []
    for z0, z1 in _doors():
        zmid = (z0 + z1) * 0.5
        span = abs(z1 - z0)
        y_top = y_belt(zmid)
        y_bot = 0.30
        x0 = x_plan(z0) - 0.004
        x1 = x_plan(z1) - 0.004
        xm = x_plan(zmid) - 0.004
        for sign in (1.0, -1.0):
            pockets.append(((sign * x0, (y_top + y_bot) * 0.5, z0), (0.020, y_top - y_bot, 0.006)))
            pockets.append(((sign * x1, (y_top + y_bot) * 0.5, z1), (0.020, y_top - y_bot, 0.006)))
            pockets.append(((sign * xm, y_top, zmid), (0.020, 0.006, span)))
            pockets.append(((sign * xm, y_bot, zmid), (0.020, 0.006, span)))
    # Hood shut at the cowl, trunk shut at the deck.
    pockets.append(((0.0, Y_COWL - 0.04, Z_COWL), (1.40, 0.10, 0.007)))
    pockets.append(((0.0, Y_DECK - 0.02, Z_DECK), (1.20, 0.10, 0.007)))
    return pockets


def _lamp_pockets():
    pockets = []
    # Wide lower grille, slim upper bar, thin full-width lamp, fender wraps, fogs.
    pockets.append(((0.0, 0.40, Z_NOSE - 0.06), (1.48, 0.32, 0.16)))
    pockets.append(((0.0, 0.62, Z_NOSE - 0.05), (1.16, 0.042, 0.12)))
    pockets.append(((0.0, 0.735, Z_NOSE - 0.07), (1.72, 0.062, 0.16)))
    for sign in (1.0, -1.0):
        pockets.append(((sign * 0.78, 0.70, Z_NOSE - 0.28), (0.26, 0.055, 0.34)))
        pockets.append(((sign * 0.55, 0.24, Z_NOSE - 0.05), (0.26, 0.09, 0.10)))
    # High tail lamps under the deck edge, plus corner wraps and a plate bite.
    pockets.append(((0.0, 0.99, Z_DECK_R - 0.02), (1.46, 0.070, 0.12)))
    for sign in (1.0, -1.0):
        pockets.append(((sign * 0.74, 0.96, Z_DECK_R + 0.08), (0.22, 0.06, 0.26)))
    pockets.append(((0.0, 0.42, Z_TAIL + 0.05), (0.34, 0.12, 0.08)))
    pockets.append(((0.0, 0.28, Z_NOSE - 0.04), (0.32, 0.10, 0.08)))
    return pockets


def _wells():
    length = 0.26
    # Shift the cutter toward the fender. sign is the side, TIRE_X is positive.
    return [((sign * (TIRE_X + 0.04), AXLE_Y, axle), ARCH_R, length) for axle in (Z_REAR, Z_FRONT) for sign in (-1.0, 1.0)]


def _glass_prisms():
    prisms = []
    span = math.hypot(Z_COWL - Z_HEADER, Y_COWL - Y_HEADER) or 1.0
    uy = (Y_COWL - Y_HEADER) / span
    uz = (Z_COWL - Z_HEADER) / span
    margin = 0.055
    y0 = Y_HEADER + uy * margin
    z0 = Z_HEADER + uz * margin
    y1 = Y_COWL - uy * margin
    z1 = Z_COWL - uz * margin
    ny, nz = shell._rake_normal(y0, z0, y1, z1)
    hx0, hx1 = 0.58, 0.76
    prisms.append((shell._prism_from_rake(y0, z0, y1, z1, hx0, hx1, ny, nz, 0.07, 0.48), _SLAB))
    span_b = math.hypot(Z_DECK - Z_ROOF_R, Y_DECK - Y_ROOF_R) or 1.0
    by = (Y_DECK - Y_ROOF_R) / span_b
    bz = (Z_DECK - Z_ROOF_R) / span_b
    by0 = Y_ROOF_R + by * 0.06
    bz0 = Z_ROOF_R + bz * 0.06
    by1 = Y_DECK - by * 0.05
    bz1 = Z_DECK - bz * 0.05
    nby, nbz = shell._rake_normal(by0, bz0, by1, bz1)
    prisms.append((shell._prism_from_rake(by0, bz0, by1, bz1, 0.50, 0.64, nby, nbz, 0.06, 0.36), _SLAB))

    def side(corners):
        for sign in (1.0, -1.0):
            outer = []
            inner = []
            for z, y in corners:
                xs = sign * skin_x(z, y)
                outer.append((xs + sign * 0.05, y, z))
                inner.append((-sign * 0.04, y, z))
            prisms.append((outer + inner, _SLAB))

    yb = y_belt(0.95) + 0.04
    run = (1.16 - yb) / math.tan(math.radians(29.0))
    side((
        (0.96, yb),
        (0.16, y_belt(0.16) + 0.04),
        (0.08, 1.14),
        (0.96 - run, 1.16),
    ))
    yb_r = y_belt(-0.05) + 0.04
    side((
        (-0.02, yb_r),
        (-0.72, y_belt(-0.72) + 0.045),
        (-0.92, 1.16),
        (-0.06, 1.16),
    ))
    return prisms, (y0, z0, y1, z1, ny, nz, hx0, hx1), (by0, bz0, by1, bz1, nby, nbz)


def _gap_lines(g):
    for z0, z1 in _doors():
        zmid = (z0 + z1) * 0.5
        span = abs(z1 - z0)
        y_top = y_belt(zmid)
        y_bot = 0.30
        for sign in (1.0, -1.0):
            xm = sign * (x_plan(zmid) - 0.008)
            g.box((sign * (x_plan(z0) - 0.008), (y_top + y_bot) * 0.5, z0), (0.008, y_top - y_bot, 0.004), "Lib_Black")
            g.box((sign * (x_plan(z1) - 0.008), (y_top + y_bot) * 0.5, z1), (0.008, y_top - y_bot, 0.004), "Lib_Black")
            g.box((xm, y_top, zmid), (0.008, 0.004, span), "Lib_Black")
            g.box((xm, y_bot, zmid), (0.008, 0.004, span), "Lib_Black")
    g.box((0.0, Y_COWL - 0.02, Z_COWL), (1.28, 0.004, 0.004), "Lib_Black")
    g.box((0.0, Y_DECK, Z_DECK), (1.05, 0.004, 0.004), "Lib_Black")


def _glass_panes(g, ws, bl):
    y0, z0, y1, z1, ny, nz, hx0, hx1 = ws
    shell._pane(
        g,
        [(-hx0 * 0.92, y0, z0), (hx0 * 0.92, y0, z0), (hx1 * 0.92, y1, z1), (-hx1 * 0.92, y1, z1)],
        ny, nz, 0.042, 0.008, GLASS,
    )
    by0, bz0, by1, bz1, nby, nbz = bl
    shell._pane(
        g,
        [(-0.42, by0, bz0), (0.42, by0, bz0), (0.52, by1, bz1), (-0.52, by1, bz1)],
        nby, nbz, 0.042, 0.008, GLASS,
    )

    def pane(corners):
        for sign in (1.0, -1.0):
            front = []
            back = []
            for z, y in corners:
                xs = sign * (skin_x(z, y) - 0.016)
                front.append((xs, y, z))
                back.append((xs - sign * 0.008, y, z))
            g.mesh(front + back, _SLAB, GLASS)

    yb = y_belt(0.95) + 0.055
    run = (1.14 - yb) / math.tan(math.radians(29.0))
    pane((
        (0.90, yb),
        (0.20, y_belt(0.20) + 0.055),
        (0.12, 1.12),
        (0.90 - run, 1.14),
    ))
    pane((
        (0.02, y_belt(0.0) + 0.055),
        (-0.66, y_belt(-0.66) + 0.06),
        (-0.84, 1.13),
        (-0.02, 1.13),
    ))


def _wipers(g):
    theta = math.atan2(Y_HEADER - Y_COWL, Z_COWL - Z_HEADER)
    dy = 0.30
    dz = -dy / math.tan(theta)
    y = Y_COWL + 0.01
    z = Z_COWL - 0.03
    g.pipe((-0.04, y, z), (-0.34, y + dy, z + dz), 0.005, "Lib_Black", segments=5)
    g.pipe((0.12, y, z), (0.46, y + dy * 0.92, z + dz * 0.92), 0.005, "Lib_Black", segments=5)


def _cabin(g):
    """Seats and a dash in the greenhouse cavity, above the lower body collider."""
    for x in (-0.32, 0.32):
        g.box((x, 0.98, 0.28), (0.28, 0.14, 0.30), "Lib_Interior")
        g.box((x, 1.10, 0.10), (0.26, 0.20, 0.07), "Lib_Interior")
        g.box((x, 1.20, 0.08), (0.14, 0.07, 0.06), "Lib_Black")
    g.box((0.0, 0.97, 0.70), (0.78, 0.08, 0.20), "Lib_Black")
    g.box((0.0, 1.02, -0.32), (0.72, 0.16, 0.08), "Lib_Interior")


def build_shell(g, lod):
    step = 0.22 if lod == 0 else (0.40 if lod == 1 else 0.58)
    zs = _stations(step)
    rings = [shell._ring(half_at(z), z) for z in zs]
    if lod == 0:
        print("BODY_A_STATIONS", len(zs), "RING", len(rings[0]))
        for z in (Z_NOSE, 1.90, Z_COWL, Z_HEADER, 0.0, Z_ROOF_R, Z_DECK, Z_TAIL):
            half = half_at(z)
            print(
                "BODY_A_SEC z=%.3f crown=%.3f char=%.3f x=%.3f belt=%.3f" % (
                    z, half[-1][1], half[4][1], half[4][0], half[6][1],
                )
            )
        rake = math.degrees(math.atan2(Y_HEADER - Y_COWL, Z_COWL - Z_HEADER))
        print("BODY_A_RAKE_DEG", round(rake, 2))
    level = 2 if lod == 0 else 0
    hard = _hard(rings) if level else ()
    prisms, ws, bl = _glass_prisms()
    pockets = _panel_pockets() + _lamp_pockets()
    # Coarse LODs keep the openings and drop the 4 mm grooves.
    if lod == 2:
        pockets = _lamp_pockets()
    shell._loft(g, rings, PAINT, level, _wells(), pockets, prisms, 82.0, hard, joined=True)
    if lod < 2:
        _glass_panes(g, ws, bl)
        _gap_lines(g)
    if lod == 0:
        _wipers(g)
    if lod < 2:
        _cabin(g)


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


def _grille(g, year, lod):
    kind = YEARS[year]["grille"]
    z = Z_NOSE - 0.012
    w, h, y = 1.28, 0.24, 0.40
    if kind == "slat4":
        count = 4
        for i in range(count):
            yy = y - h * 0.38 + i * (h * 0.22)
            g.box((0.0, yy, z), (w, 0.012, 0.008), "Lib_SteelDark")
    elif kind == "slat8":
        count = 6 if lod == 0 else 3
        for i in range(count):
            yy = y - h * 0.40 + i * (h * 0.14)
            g.box((0.0, yy, z), (w - 0.06, 0.006, 0.006), "Lib_Steel")
    else:
        rows = 4 if lod == 0 else 2
        cols = 8 if lod == 0 else 3
        for i in range(rows):
            yy = y - h * 0.36 + i * (h * 0.20)
            g.box((0.0, yy, z), (w, 0.005, 0.005), "Lib_Black")
        for i in range(cols):
            xx = -w * 0.42 + i * (w * 0.84 / max(1, cols - 1))
            g.box((xx, y, z), (0.005, h * 0.80, 0.005), "Lib_Black")
    # Slim upper bar. 2024 is black trim; the others are bright metal.
    trim = "Lib_Black" if year == 2024 else "Lib_Steel"
    g.box((0.0, 0.62, z), (1.05, 0.018, 0.008), trim)


def _lamps_front(g, year, paint):
    kind = YEARS[year]["lamps"]
    z_face = Z_NOSE - 0.016
    y = 0.735
    if kind == "bar":
        g.box((0.0, y, z_face - 0.006), (1.58, 0.050, 0.010), "Lib_Black")
        g.box((0.0, y, z_face), (1.50, 0.028, 0.008), "Lib_Headlamp")
        for sign in (1.0, -1.0):
            g.box((sign * 0.78, 0.70, Z_NOSE - 0.20), (0.18, 0.032, 0.22), "Lib_Headlamp")
        return
    # 2021-2024: body-color shut in the middle of the shared slot, separate lamps outside.
    g.box((0.0, y, z_face), (0.62, 0.050, 0.016), paint)
    if kind == "tier":
        for sign in (1.0, -1.0):
            g.box((sign * 0.58, y + 0.008, z_face), (0.42, 0.016, 0.010), "Lib_Headlamp")
            g.box((sign * 0.52, y - 0.016, z_face - 0.004), (0.34, 0.012, 0.008), "Lib_Headlamp")
    elif kind == "thin":
        for sign in (1.0, -1.0):
            g.box((sign * 0.62, y, z_face), (0.46, 0.020, 0.008), "Lib_Headlamp")
    else:
        for sign in (1.0, -1.0):
            g.box((sign * 0.58, y, z_face - 0.004), (0.40, 0.046, 0.012), "Lib_Black")
            g.box((sign * 0.58, y, z_face), (0.32, 0.026, 0.008), "Lib_Headlamp")
            g.box((sign * 0.78, 0.70, Z_NOSE - 0.16), (0.10, 0.022, 0.12), "Lib_Headlamp")


def _lamps_rear(g, year, paint):
    kind = YEARS[year]["tails"]
    z_face = Z_DECK_R + 0.012
    y = 0.99
    if kind == "bar":
        g.box((0.0, y, z_face + 0.004), (1.40, 0.055, 0.010), "Lib_Black")
        g.box((0.0, y, z_face), (1.32, 0.030, 0.008), "Lib_Taillamp")
        for sign in (1.0, -1.0):
            g.box((sign * 0.74, 0.96, Z_DECK_R + 0.10), (0.14, 0.028, 0.16), "Lib_Taillamp")
        return
    g.box((0.0, y, z_face), (0.50, 0.055, 0.014), paint)
    tall = 0.040 if kind == "separate" else 0.020
    for sign in (1.0, -1.0):
        g.box((sign * 0.52, y, z_face), (0.36, tall, 0.008), "Lib_Taillamp")


def _mirrors(g, year, lod):
    mat = YEARS[year]["mirror"]
    z0, z1 = _doors()[0]
    z = z1 - 0.16
    y = y_belt(z) + 0.02
    x_skin = skin_x(z, y)
    bev = 0.006 if lod == 0 else 0.0
    segs = 1 if lod == 0 else 0
    for sign in (1.0, -1.0):
        g.cylinder((sign * (x_skin + 0.035), y, z), 0.012, 0.07, mat, 8, axis="X")
        cap_x = x_skin + 0.055 + 0.055
        g.box((sign * cap_x, y, z), (0.11, 0.15, 0.25), mat, bevel=bev, segs=segs)
        g.box((sign * (cap_x + 0.05), y, z + 0.01), (0.008, 0.09, 0.16), "Lib_SteelDark")


def _fogs(g, year):
    mat = YEARS[year]["fog"]
    for sign in (1.0, -1.0):
        g.box((sign * 0.55, 0.24, Z_NOSE - 0.012), (0.16, 0.045, 0.008), mat)


def dress(g, year, lod, paint):
    for axle in (Z_REAR, Z_FRONT):
        for sign in (-1.0, 1.0):
            _wheel(g, sign * TIRE_X, axle, year, lod)
    _grille(g, year, lod)
    _lamps_front(g, year, paint)
    _lamps_rear(g, year, paint)
    _fogs(g, year)
    g.box((0.0, 0.28, Z_NOSE - 0.010), (0.26, 0.07, 0.006), "Lib_SteelDark")
    g.box((0.0, 0.42, Z_TAIL + 0.012), (0.26, 0.09, 0.006), "Lib_SteelDark")
    if lod < 2:
        _mirrors(g, year, lod)


def add_colliders(asset):
    yz = TIRE_R * 0.56
    for i, axle in enumerate((Z_REAR, Z_FRONT)):
        for j, sign in enumerate((-1.0, 1.0)):
            asset.box(
                "Col_Wheel_%d%d" % (i, j),
                (sign * TIRE_X, AXLE_Y, axle),
                (0.018, yz, yz),
            )
    asset.box("Col_Cabin", (0.0, 0.50, 0.05), (0.86, 0.46, 1.15))
    asset.box("Col_Roof", (0.0, 1.348, -0.10), (0.38, 0.026, 0.42))
    asset.box("Col_Hood", (0.0, 0.62, 1.78), (0.70, 0.14, 0.72))
    asset.box("Col_Deck", (0.0, 0.82, -1.72), (0.72, 0.14, 0.50))


def probe_spec(name):
    ny, nz = shell._rake_normal(Y_HEADER, Z_HEADER, Y_COWL, Z_COWL)
    y = (Y_HEADER + Y_COWL) * 0.5
    z = (Z_HEADER + Z_COWL) * 0.5
    return {
        "name": name,
        "glass_tests": [
            (0.0, y - ny * 0.028, z - nz * 0.028),
            (skin_x(0.55, y_belt(0.55) + 0.12) - 0.010, y_belt(0.55) + 0.12, 0.55),
            (-(skin_x(-0.40, 1.05) - 0.010), 1.05, -0.40),
        ],
    }


def measure(asset):
    """Side-view outline. Glass holes drop the centerline, so this is max Y at each Z."""
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
        bucket = round(z * 10.0) / 10.0
        prev = best.get(bucket)
        if prev is None or y > prev[1]:
            best[bucket] = (z, y)

    def near(z_target):
        pick = None
        for _bucket, (z, y) in best.items():
            if abs(z - z_target) < 0.12 and (pick is None or y > pick[1]):
                pick = (z, y)
        return pick

    header = near(Z_HEADER)
    cowl = near(Z_COWL)
    angle = None
    if header and cowl and abs(cowl[0] - header[0]) > 0.05:
        angle = math.degrees(math.atan2(header[1] - cowl[1], cowl[0] - header[0]))
    outline = " ".join("%+.1f:%.2f" % (z, y) for z, y in (best[k] for k in sorted(best)[::2]))
    print(
        "BODY_A_MEASURE", asset.name,
        "rake", None if angle is None else round(angle, 1),
        "header", None if header is None else tuple(round(v, 3) for v in header),
        "cowl", None if cowl is None else tuple(round(v, 3) for v in cowl),
        "size", round(max_x * 2.0, 3), round(max_y - min_y, 3), round(max_z - min_z, 3),
        "ymax", round(max_y, 3), "ymin", round(min_y, 3),
    )
    print("BODY_A_OUTLINE", outline)
    return angle
