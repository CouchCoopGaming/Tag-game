"""Flush sidewalk, gutter, and footing layout. No Blender.

Sidewalk tiles are 2 m wide and 4 m long. The curb is the local -X face.
Gutter tiles are 0.38 m across (faces at x = ±0.19) and 4 m long. The +X face
is the curb lip and the -X face is the road lip. Runs that are not a multiple
of 4 m get one scaled remainder tile butted to the previous end.
"""

import math

TILE = 4.0
WALK_HALF_W = 1.0
WALK_HALF_L = 2.0
# Authored box faces in gutter.py. Keep the same expression so the gap is exact.
CURB_LIP = 0.15 + 0.04
ROAD_LIP = -0.14 - 0.05
ROAD_HALF = 3.0
# Street: road edge, gutter lip, sidewalk curb, and shop footing share these planes.
STREET_FACE_X = 5.38
STREET_WALK_X = 4.38
STREET_GUTTER_X = 3.19
STREET_Z0 = -18.0
STREET_Z1 = 18.0
LIMIT = 0.01


def _yaw(yaw, x, z):
    a = math.radians(yaw)
    ca, sa = math.cos(a), math.sin(a)
    return (x * ca + z * sa, -x * sa + z * ca)


def _span_tiles(a0, a1):
    """Centers and length scales that cover [a0, a1] without a gap or an overlap."""
    span = a1 - a0
    if span <= 0.001:
        return []
    n = int(math.floor((span + 1e-9) / TILE))
    remain = span - n * TILE
    items = []
    cursor = a0
    for _ in range(n):
        items.append((cursor + TILE * 0.5, 1.0))
        cursor += TILE
    if remain > 0.005:
        items.append((cursor + remain * 0.5, remain / TILE))
    return items


def _place_run(name, origin, yaw, along0, along1, along_axis):
    """along_axis 'x' or 'z'. origin is the cross-axis center (x, z)."""
    out = []
    for center, scale in _span_tiles(along0, along1):
        if along_axis == "x":
            pos = (center, 0.0, origin)
        else:
            pos = (origin, 0.0, center)
        out.append((name, pos, yaw, scale))
    return out


def lot_placements(sx, sz):
    """Sidewalk and gutter around a building whose walls sit at ±sx/2, ±sz/2. Front is +Z."""
    hx, hz = sx * 0.5, sz * 0.5
    out = []
    # Front owns the corner squares. Sides stop at the front and back inner edges.
    out += _place_run("Sidewalk", hz + WALK_HALF_W, 90.0, -(hx + 2.0), hx + 2.0, "x")
    out += _place_run("Gutter", hz + 2.0 + CURB_LIP, 90.0, -(hx + 2.0), hx + 2.0, "x")
    out += _place_run("Sidewalk", -hz - WALK_HALF_W, -90.0, -(hx + 2.0), hx + 2.0, "x")
    out += _place_run("Gutter", -hz - 2.0 - CURB_LIP, -90.0, -(hx + 2.0), hx + 2.0, "x")
    out += _place_run("Sidewalk", -hx - WALK_HALF_W, 0.0, -hz, hz, "z")
    out += _place_run("Gutter", -hx - 2.0 - CURB_LIP, 0.0, -hz, hz, "z")
    out += _place_run("Sidewalk", hx + WALK_HALF_W, 180.0, -hz, hz, "z")
    out += _place_run("Gutter", hx + 2.0 + CURB_LIP, 180.0, -hz, hz, "z")
    return out


def street_hardscape():
    """Road, gutters, and both sidewalks. Shop footing is the plane x = STREET_FACE_X."""
    out = []
    out += _place_run("Road_Straight", 0.0, 0.0, STREET_Z0, STREET_Z1, "z")
    out += _place_run("Gutter", STREET_GUTTER_X, 0.0, STREET_Z0, STREET_Z1, "z")
    out += _place_run("Sidewalk", STREET_WALK_X, 0.0, STREET_Z0, STREET_Z1, "z")
    out += _place_run("Gutter", -STREET_GUTTER_X, 180.0, STREET_Z0, STREET_Z1, "z")
    out += _place_run("Sidewalk", -STREET_WALK_X, 180.0, STREET_Z0, STREET_Z1, "z")
    return out


def street_shops():
    """(name, position, yaw) with the front face on x = STREET_FACE_X. Yaw -90 turns +Z toward -X."""
    # depth is the building's Z size. Width runs along the street after the yaw.
    specs = (
        ("Store_Diner", 7.0, 10.0, -12.0),
        ("Store_Corner", 7.2, 8.0, 0.0),
        ("Store_Laundromat", 6.8, 9.2, 12.0),
    )
    out = []
    for name, depth, _width, z in specs:
        out.append((name, (STREET_FACE_X + depth * 0.5, 0.0, z), -90.0))
    return out


def _aabb(pos, yaw, hx, hz):
    xs, zs = [], []
    for x in (-hx, hx):
        for z in (-hz, hz):
            dx, dz = _yaw(yaw, x, z)
            xs.append(pos[0] + dx)
            zs.append(pos[2] + dz)
    return (min(xs), max(xs), min(zs), max(zs))


def _face(pos, yaw, local_x):
    dx, dz = _yaw(yaw, local_x, 0.0)
    return (pos[0] + dx, pos[2] + dz)


def _pair_gaps(items, axis):
    gaps = []
    ordered = sorted(items, key=lambda it: it[0] if axis == "x" else it[2])
    for prev, nxt in zip(ordered, ordered[1:]):
        if axis == "x":
            gaps.append(nxt[0] - prev[1])
        else:
            gaps.append(nxt[2] - prev[3])
    return gaps


def measure(sx=10.0, sz=7.0):
    """Largest separation between pieces that are supposed to touch. Overlap is negative."""
    gaps = []

    def add(label, gap):
        gaps.append((abs(gap), gap, label))

    lot = lot_placements(sx, sz)
    groups = {}
    for name, pos, yaw, scale in lot:
        key = (name, round(pos[2], 4) if abs(pos[0]) > abs(pos[2]) or name == "ignore" else 0, yaw, round(pos[0] if yaw in (90.0, -90.0) else pos[2], 4))
        # Group by asset, yaw, and the cross-axis center so each run is separate.
        if yaw in (90.0, -90.0):
            key = (name, yaw, round(pos[2], 5))
            axis = "x"
        else:
            key = (name, yaw, round(pos[0], 5))
            axis = "z"
        half_l = (WALK_HALF_L if name == "Sidewalk" else WALK_HALF_L) * scale
        half_w = WALK_HALF_W if name == "Sidewalk" else abs(CURB_LIP)
        box = _aabb(pos, yaw, half_w, half_l)
        groups.setdefault((key, axis), []).append(box)
    for (key, axis), boxes in groups.items():
        for gap in _pair_gaps(boxes, axis):
            add("lot run %s" % (key[0],), gap)

    hx, hz = sx * 0.5, sz * 0.5
    for name, pos, yaw, scale in lot:
        if name != "Sidewalk":
            continue
        box = _aabb(pos, yaw, WALK_HALF_W, WALK_HALF_L * scale)
        if yaw == 90.0:
            add("front footing", box[2] - hz)
        elif yaw == -90.0:
            add("back footing", -hz - box[3])
        elif yaw == 0.0:
            add("west footing", -hx - box[1])
        elif yaw == 180.0:
            add("east footing", box[0] - hx)

    # Curb lip of each gutter against the sidewalk curb, same run.
    walks = [it for it in lot if it[0] == "Sidewalk"]
    gutters = [it for it in lot if it[0] == "Gutter"]
    for gname, gpos, gyaw, gscale in gutters:
        lip = _face(gpos, gyaw, CURB_LIP)
        best = None
        for _n, wpos, wyaw, wscale in walks:
            if wyaw != gyaw:
                continue
            curb = _face(wpos, wyaw, -WALK_HALF_W)
            dist = math.hypot(lip[0] - curb[0], lip[1] - curb[1])
            # Same station along the run: the along-coordinate matches.
            if gyaw in (90.0, -90.0):
                same = abs(gpos[0] - wpos[0]) < 0.02
            else:
                same = abs(gpos[2] - wpos[2]) < 0.02
            if same and (best is None or dist < best):
                best = dist
        if best is not None:
            add("gutter to curb", best)

    street = street_hardscape()
    sgroups = {}
    for name, pos, yaw, scale in street:
        half_l = WALK_HALF_L * scale
        if name == "Road_Straight":
            half_w = ROAD_HALF
        elif name == "Sidewalk":
            half_w = WALK_HALF_W
        else:
            half_w = abs(CURB_LIP)
        box = _aabb(pos, yaw, half_w, half_l)
        sgroups.setdefault(name + str(round(pos[0], 3)), []).append(box)
    for key, boxes in sgroups.items():
        for gap in _pair_gaps(boxes, "z"):
            add("street " + key, gap)

    for name, pos, yaw, scale in street:
        if name == "Sidewalk" and pos[0] > 0.0:
            box = _aabb(pos, yaw, WALK_HALF_W, WALK_HALF_L * scale)
            add("shop footing", STREET_FACE_X - box[1])
            curb = _face(pos, yaw, -WALK_HALF_W)
            add("walk curb plane", abs(curb[0] - (STREET_GUTTER_X + CURB_LIP)))
        if name == "Gutter" and pos[0] > 0.0:
            lip = _face(pos, yaw, CURB_LIP)
            road = _face(pos, yaw, ROAD_LIP)
            add("east curb lip", abs(lip[0] - (STREET_WALK_X - WALK_HALF_W)))
            add("east road lip", abs(road[0] - ROAD_HALF))
        if name == "Gutter" and pos[0] < 0.0:
            lip = _face(pos, yaw, CURB_LIP)
            road = _face(pos, yaw, ROAD_LIP)
            add("west curb lip", abs(lip[0] - (-STREET_WALK_X + WALK_HALF_W)))
            add("west road lip", abs(road[0] + ROAD_HALF))

    worst = max(gaps, key=lambda item: item[0]) if gaps else (0.0, 0.0, "none")
    return worst[0], worst[1], worst[2], gaps


def main():
    sizes = ((10.0, 7.0), (8.0, 7.2), (9.2, 6.8), (12.0, 8.0))
    worst = 0.0
    detail = ""
    for sx, sz in sizes:
        mag, signed, label, _gaps = measure(sx, sz)
        print("lot %.1f x %.1f max %.6f m (%s %+.6f)" % (sx, sz, mag, label, signed))
        if mag > worst:
            worst = mag
            detail = label
    _mag, _signed, _label, street_gaps = measure()
    over = [g for g in street_gaps if g[0] > LIMIT]
    print("MAX_GAP %.6f m" % worst)
    if worst > LIMIT or over:
        print("SNAP_FAIL", detail, len(over))
        return 1
    print("SNAP_OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
