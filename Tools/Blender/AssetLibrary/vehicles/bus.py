"""Low-floor city bus shell. Nothing here is registered.

Published 40 ft diesel sheet, inches then meters:
length over bumpers 492 (41 ft 0 in), over body 482 (40 ft 2 in),
width 102, height over the roof unit 126 (10 ft 6 in),
wheelbase 283.75, tire 305/70R22.5, front step 14, approach 9 degrees.
Front overhang 87.75 in is the published figure on the 41 ft 0 in
low-floor body test. Rear overhang is the 120.5 in remainder that
closes 492 and 283.75. No badges or brand marks.

Glass sits 1.5 cm behind the outer skin. Door leaves meet on a 4 mm gap.
The roof and the roof unit are solid enough to climb.
"""

import math

from _common import Asset

INCH = 0.0254


def _pieces(z0, z1, holes):
    cursor = z0
    spans = []
    for a, b in sorted(holes):
        if a > cursor + 0.02:
            spans.append((cursor, a))
        cursor = max(cursor, b)
    if z1 > cursor + 0.02:
        spans.append((cursor, z1))
    return spans


def make_city40(name, body, skirt):
    length = 492.0 * INCH
    body_len = 482.0 * INCH
    width = 102.0 * INCH
    height = 126.0 * INCH
    wheelbase = 283.75 * INCH
    front_overhang = 87.75 * INCH
    tire_r = (22.5 * INCH + 2.0 * 0.305 * 0.70) * 0.5
    half = length * 0.5
    z_nose = half - (length - body_len) * 0.5
    z_tail = -z_nose
    z_front = half - front_overhang
    z_rear = z_front - wheelbase
    # Outer tire face sits 4 cm inside the body. Rear axle is dual.
    # Keep the tread inboard of the skirt so the rubber does not share the wall.
    outer_face = width * 0.5 - 0.14
    tire_x = outer_face - 0.305 * 0.5
    dual_x = tire_x - 0.305 - 0.04
    arch = tire_r + 0.10
    doors = (
        (z_front + arch + 0.08, z_front + arch + 0.08 + 0.813),
        (0.55, 0.55 + 0.813),
    )
    return {
        "name": name,
        "body": body,
        "skirt": skirt,
        "length": length,
        "width": width,
        "height": height,
        "roof": height - 0.16,
        "half_b": z_nose,
        "z_tail": z_tail,
        "z_front": z_front,
        "z_rear": z_rear,
        "tire_r": tire_r,
        "tire_w": 0.305,
        "tire_x": tire_x,
        "dual_x": dual_x,
        "axle_y": tire_r,
        "arch": arch,
        "doors": doors,
        "step": 14.0 * INCH,
    }


def _solid(asset, name, center, size):
    sx, sy, sz = size
    shrink = 0.018
    if min(sx, sy, sz) <= shrink * 2.0 + 0.004:
        return
    asset.box(
        name,
        center,
        (sx - shrink * 2.0, sy - shrink * 2.0, sz - shrink * 2.0),
    )


def _panel(g, center, size, mat, bev, segs):
    g.box(center, size, mat, bevel=bev, segs=segs)


def _wheel(g, x, z, spec, lod, detailed):
    """Rubber plus an outboard rim. The rim stays off the inboard face so a
    collider ray traveling across the bus does not stack extra shells."""
    r = spec["tire_r"]
    y = spec["axle_y"]
    seg = 16 if lod == 0 else 8
    g.cylinder((x, y, z), r, spec["tire_w"], "Lib_Rubber", seg, axis="X")
    if not detailed:
        return
    sign = 1.0 if x > 0 else -1.0
    face = x + sign * (spec["tire_w"] * 0.5 + 0.016)
    g.cylinder((face, y, z), r * 0.62, 0.012, "Lib_Steel", seg, axis="X")
    if lod == 0:
        for k in range(5):
            theta = math.radians(k * 72.0 + 10.0)
            g.box(
                (face, y + math.cos(theta) * r * 0.36, z + math.sin(theta) * r * 0.36),
                (0.008, r * 0.38, 0.016),
                "Lib_Steel",
                euler=(math.degrees(theta), 0, 0),
            )


def _side(g, asset, spec, sign, lod, bev, segs):
    """One flank. sign +1 is the curb side, with two doors."""
    half = spec["width"] * 0.5
    x = sign * (half - 0.025)
    thick = 0.050
    z0 = spec["z_tail"] + 0.08
    z1 = spec["half_b"] - 0.08
    arches = (
        (spec["z_rear"] - spec["arch"], spec["z_rear"] + spec["arch"]),
        (spec["z_front"] - spec["arch"], spec["z_front"] + spec["arch"]),
    )
    doors = spec["doors"] if sign > 0 else ()
    skirt_y = 0.64
    skirt_h = 0.84
    for i, (a, b) in enumerate(_pieces(z0, z1, list(arches) + list(doors))):
        c = (x, skirt_y, (a + b) * 0.5)
        sz = (thick, skirt_h, b - a)
        _panel(g, c, sz, spec["skirt"], bev, segs)
        _solid(asset, "Col_Skirt_%d_%d" % (0 if sign < 0 else 1, i), c, sz)
    for a, b in arches:
        # Inboard of the inner tire so the liner does not share volume with the rubber.
        g.box((sign * 0.48, 1.02, (a + b) * 0.5), (0.04, 0.36, (b - a) * 0.80), "Lib_Black")
    rail_z0, rail_z1 = z0, z1
    c = (sign * (half + 0.008), 1.10, (rail_z0 + rail_z1) * 0.5)
    g.box(c, (0.012, 0.036, rail_z1 - rail_z0), "Lib_SteelDark")
    # Waist band closes the gap between the skirt and the glass.
    belt = (x, 1.16, (z0 + z1) * 0.5)
    belt_sz = (thick, 0.20, z1 - z0)
    _panel(g, belt, belt_sz, spec["body"], 0.0, 0)
    _solid(asset, "Col_Belt_%d" % (0 if sign < 0 else 1), belt, belt_sz)
    # Glass ends under the header so the pane is not buried in the rail.
    win_y, win_h = 1.82, 1.00
    holes = list(doors)
    panes = _pieces(z0 + 0.06, z1 - 0.06, holes)
    step = 1.28 if lod == 0 else 2.40
    pillar = 0.090
    pi = 0
    for a, b in panes:
        z = a
        while z < b - 0.20:
            pane_z1 = min(b, z + step)
            if pane_z1 - z < 0.25:
                break
            # Pillar at the rear edge of each pane, then glass across the rest.
            if pane_z1 < b - 0.05:
                pcenter = (x, win_y, z + pillar * 0.5)
                psize = (thick, win_h, pillar)
                _panel(g, pcenter, psize, spec["body"], 0.0, 0)
                _solid(asset, "Col_Pillar_%d_%d" % (0 if sign < 0 else 1, pi), pcenter, psize)
                pi += 1
                glass_a = z + pillar
            else:
                glass_a = z
            glass_b = pane_z1
            if glass_b - glass_a > 0.12:
                gz = (glass_a + glass_b) * 0.5
                gw = glass_b - glass_a - 0.008
                # Outer face 1.5 cm behind the skin. Skin outer is at sign * half.
                skin = sign * half
                inset = 0.015 + 0.004
                gx = skin - sign * inset
                g.box((gx, win_y, gz), (0.008, win_h - 0.04, gw), "Lib_TintGlass")
                g.box((gx - sign * 0.028, win_y, gz), (0.016, win_h - 0.06, gw - 0.02), "Lib_Interior")
            z = pane_z1
    for i, (a, b) in enumerate(doors):
        leaf = (b - a - 0.004) * 0.5
        for j, cz in enumerate((a + leaf * 0.5, b - leaf * 0.5)):
            lower = (x, 0.70, cz)
            lsz = (thick, 0.64, leaf - 0.002)
            _panel(g, lower, lsz, spec["body"], bev, segs)
            _solid(asset, "Col_Door_%d_%d" % (i, j), lower, lsz)
            gx = sign * half - sign * 0.019
            g.box((gx, 1.80, cz), (0.008, 1.00, leaf - 0.02), "Lib_TintGlass")
            g.box((gx - sign * 0.026, 1.80, cz), (0.012, 0.90, leaf - 0.04), "Lib_Interior")
            g.box((sign * (half + 0.004), 1.15, cz), (0.008, 0.012, leaf * 0.55), "Lib_SteelDark")
    header = (x, 2.70, (z0 + z1) * 0.5)
    hsz = (thick, 0.68, z1 - z0)
    _panel(g, header, hsz, spec["body"], bev, segs)
    _solid(asset, "Col_Header_%d" % (0 if sign < 0 else 1), header, hsz)


def _caps(g, asset, spec, lod, bev, segs):
    half = spec["width"] * 0.5
    zf = spec["half_b"]
    zt = spec["z_tail"]
    # Nose frame around a recessed windshield. Bumper face is the published overall.
    nose_bits = (
        ("Col_NoseSill", (0.0, 0.62, zf - 0.025), (1.70, 0.84, 0.050), spec["body"]),
        ("Col_NoseL", (-half + 0.16, 1.78, zf - 0.025), (0.28, 1.50, 0.050), spec["body"]),
        ("Col_NoseR", (half - 0.16, 1.78, zf - 0.025), (0.28, 1.50, 0.050), spec["body"]),
        ("Col_Dest", (0.0, 2.62, zf - 0.03), (1.70, 0.34, 0.060), "Lib_Black"),
    )
    for name, center, size, mat in nose_bits:
        _panel(g, center, size, mat, bev, segs)
        if name == "Col_NoseSill":
            # Stay under the belt seam. A ray along the seam picks up an extra hit.
            asset.box(name, (center[0], 0.52, center[2]), (1.50, 0.56, 0.014))
        else:
            _solid(asset, name, center, size)
    g.box((0.0, 2.62, zf + 0.004), (1.46, 0.20, 0.008), "Lib_WindowLit")
    # Windshield recessed 1.5 cm behind the nose skin (skin outer z = zf).
    g.box((0.0, 1.72, zf - 0.019), (half * 1.30, 1.20, 0.008), "Lib_TintGlass")
    g.box((0.0, 1.72, zf - 0.050), (half * 1.16, 1.08, 0.016), "Lib_Interior")
    bumper_z = spec["length"] * 0.5 - 0.04
    _panel(g, (0.0, 0.40, bumper_z), (half * 1.92, 0.16, 0.070), "Lib_Black", bev, segs)
    for sign in (-1.0, 1.0):
        g.box((sign * half * 0.62, 0.48, spec["length"] * 0.5 - 0.012), (0.34, 0.12, 0.018), "Lib_Black")
        g.box((sign * half * 0.62, 0.48, spec["length"] * 0.5 - 0.002), (0.26, 0.07, 0.010), "Lib_Headlamp")
        g.box((sign * (half + 0.02), 2.00, zf - 0.20), (0.22, 0.32, 0.10), "Lib_Black")
        g.box((sign * (half + 0.10), 2.00, zf - 0.20), (0.012, 0.04, 0.28), "Lib_SteelDark")
    # Tail frame. The rear window and the engine door are openings, not a solid slab.
    tail_bits = (
        ("Col_TailL", (-half + 0.18, 1.60, zt + 0.025), (0.32, 2.40, 0.050), spec["body"]),
        ("Col_TailR", (half - 0.18, 1.60, zt + 0.025), (0.32, 2.40, 0.050), spec["body"]),
        ("Col_TailHead", (0.0, 2.62, zt + 0.025), (1.60, 0.46, 0.050), spec["body"]),
        ("Col_TailSill", (0.0, 1.62, zt + 0.025), (1.60, 0.16, 0.050), spec["body"]),
        ("Col_Engine", (0.0, 0.92, zt + 0.025), (1.35, 1.05, 0.050), "Lib_SteelDark"),
    )
    for name, center, size, mat in tail_bits:
        _panel(g, center, size, mat, bev, segs)
        _solid(asset, name, center, size)
    g.box((0.0, 2.10, zt + 0.019), (1.05, 0.48, 0.008), "Lib_TintGlass")
    g.box((0.0, 2.10, zt + 0.046), (0.94, 0.38, 0.012), "Lib_Interior")
    for i in range(4 if lod == 0 else 2):
        g.box((0.0, 0.58 + i * 0.18, zt - 0.008), (1.10, 0.012, 0.008), "Lib_Black")
    _panel(g, (0.0, 0.38, -bumper_z), (half * 1.92, 0.16, 0.070), "Lib_Black", bev, segs)
    for sign in (-1.0, 1.0):
        g.box((sign * half * 0.72, 0.78, -spec["length"] * 0.5 + 0.012), (0.28, 0.16, 0.016), "Lib_Black")
        g.box((sign * half * 0.72, 0.86, -spec["length"] * 0.5 + 0.002), (0.22, 0.08, 0.010), "Lib_Taillamp")
        g.box((sign * half * 0.72, 0.68, -spec["length"] * 0.5 + 0.002), (0.22, 0.06, 0.010), "Lib_SignalAmber")


def _roof(g, asset, spec, bev, segs):
    half = spec["width"] * 0.5
    z0 = spec["z_tail"] + 0.12
    z1 = spec["half_b"] - 0.12
    top = spec["roof"]
    c = (0.0, top - 0.045, (z0 + z1) * 0.5)
    sz = (spec["width"] - 0.14, 0.090, z1 - z0)
    _panel(g, c, sz, spec["body"], bev, segs)
    _solid(asset, "Col_Roof", c, sz)
    # Roof unit. Its top is the published overall height. Climbers land here too.
    hz = spec["z_rear"] + 0.40
    hs = (1.55, spec["height"] - top + 0.012, 3.05)
    hc = (0.0, spec["height"] - hs[1] * 0.5, hz)
    _panel(g, hc, hs, "Lib_SteelDark", bev, segs)
    _solid(asset, "Col_RoofUnit", hc, hs)
    for sign in (-1.0, 1.0):
        g.box((sign * (half - 0.12), top + 0.02, z1 - 0.15), (0.08, 0.04, 0.10), "Lib_SignalAmber")
        g.box((sign * (half - 0.12), top + 0.02, z0 + 0.15), (0.08, 0.04, 0.10), "Lib_Taillamp")


def _floor(g, asset, spec, bev, segs):
    z0 = spec["z_tail"] + 0.10
    z1 = spec["half_b"] - 0.10
    c = (0.0, spec["step"] - 0.04, (z0 + z1) * 0.5)
    # Inboard of the inner dual so the deck does not cut the tires.
    sz = (0.90, 0.080, z1 - z0)
    _panel(g, c, sz, "Lib_SteelDark", bev, segs)
    asset.box("Col_Floor", c, (0.70, 0.028, sz[2] - 0.24))


def build_city_bus(g, asset, spec, lod):
    bev = 0.008 if lod == 0 else 0.0
    segs = 1 if lod == 0 else 0
    # Colliders are authored once. Lod 1 only refreshes the visible shell.
    real_box = asset.box
    if lod != 0:
        asset.box = lambda *args, **kwargs: None
    try:
        _build_city_bus(g, asset, spec, lod, bev, segs)
    finally:
        asset.box = real_box


def _build_city_bus(g, asset, spec, lod, bev, segs):
    _floor(g, asset, spec, bev, segs)
    _roof(g, asset, spec, bev, segs)
    _side(g, asset, spec, -1.0, lod, bev, segs)
    _side(g, asset, spec, 1.0, lod, bev, segs)
    _caps(g, asset, spec, lod, bev, segs)
    for z in (spec["z_front"], spec["z_rear"]):
        xs = (spec["tire_x"],) if z == spec["z_front"] else (spec["tire_x"], spec["dual_x"])
        for x in xs:
            for sign in (-1.0, 1.0):
                _wheel(g, sign * x, z, spec, lod, detailed=(x == spec["tire_x"]))
                # Slab in the middle of the tread. Corners stay inside the cylinder.
                asset.box(
                    "Col_Wheel_%d_%d" % (int(z > 0), int(sign > 0) + (0 if x == spec["tire_x"] else 2)),
                    (sign * x, spec["axle_y"], z),
                    (0.016, spec["tire_r"] * 0.86, spec["tire_r"] * 0.86),
                )
    if lod == 0:
        z = spec["z_tail"] + 1.4
        while z < spec["half_b"] - 1.6:
            for sign in (-1.0, 1.0):
                g.box((sign * 0.36, 0.95, z), (0.32, 0.52, 0.08), "Lib_PaintBlue")
            z += 0.85


def create_city40(name, body, skirt, blurb):
    spec = make_city40(name, body, skirt)
    asset = Asset(name, "Vehicles", blurb)
    asset.climbable = True
    asset.climb_note = "Roof and roof unit. Sheet metal, not a cling wall."
    asset.vault_note = "The skirt is a step. The roof is the landing."
    for lod in (0, 1):
        geo = asset.begin(lod)
        build_city_bus(geo, asset, spec, lod)
        asset.end()
    asset._bus_spec = spec
    return asset
