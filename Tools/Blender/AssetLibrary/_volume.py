"""Closed building shells. Four walls, a roof, and a room you can see into.

Wall boxes meet on a plane and do not share a volume, so the collider test
keeps a stable inside/outside. Windows sit in real openings.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from cabin import _prism, _prism_faces


def _panels(x0, x1, y0, y1, holes):
    """Solid rectangles on a wall after cutting (cx, cy, w, h) holes."""
    xs = [x0, x1]
    for cx, _cy, w, _h in holes:
        xs.append(cx - w * 0.5)
        xs.append(cx + w * 0.5)
    xs = sorted(set(round(v, 4) for v in xs))
    rects = []
    for i in range(len(xs) - 1):
        a, b = xs[i], xs[i + 1]
        if b - a < 0.04:
            continue
        covered = []
        for cx, cy, w, h in holes:
            if cx - w * 0.5 <= a + 1e-3 and cx + w * 0.5 >= b - 1e-3:
                covered.append((cy - h * 0.5, cy + h * 0.5))
        covered.sort()
        y = y0
        merged = []
        for c0, c1 in covered:
            if merged and c0 <= merged[-1][1]:
                merged[-1] = (merged[-1][0], max(merged[-1][1], c1))
            else:
                merged.append((c0, c1))
        for c0, c1 in merged:
            if c0 > y + 0.04:
                rects.append(((a + b) * 0.5, (y + c0) * 0.5, b - a, c0 - y))
            y = max(y, c1)
        if y1 > y + 0.04:
            rects.append(((a + b) * 0.5, (y + y1) * 0.5, b - a, y1 - y))
    return rects


def _fit_holes(span, wall_h, kind):
    """Openings along a wall of clear span `span`. (cx, cy, w, h, lit, door)."""
    if kind == "plain":
        return [
            (0.0, wall_h * 0.62, min(1.2, span * 0.28), 1.15, False, False),
        ]
    if kind == "alley":
        return [
            (-span * 0.18, 1.15, 1.0, 2.15, False, True),
            (span * 0.22, wall_h * 0.62, 0.9, 1.05, True, False),
        ]
    # Shop front: a wide window, a door, and a row upstairs.
    door_w = 1.05
    win_w = min(2.4, span * 0.36)
    holes = [
        (-span * 0.22, 1.58, win_w, 1.7, False, False),
        (span * 0.22, 1.15, door_w, 2.2, False, True),
    ]
    if wall_h > 4.2:
        for cx, lit in ((-span * 0.28, True), (0.0, False), (span * 0.28, True)):
            holes.append((cx, wall_h * 0.72, min(1.4, span * 0.2), 1.2, lit, False))
    kept = []
    for cx, cy, w, h, lit, door in holes:
        if abs(cx) + w * 0.5 > span * 0.5 - 0.18:
            continue
        if cy + h * 0.5 > wall_h - 0.18 or cy - h * 0.5 < 0.08:
            continue
        kept.append((cx, cy, w, h, lit, door))
    return kept


def _window(g, origin, axis, inward, hole, trim, lod):
    """Frame, recessed glass, mullions, and a backing. hole is (cx, cy, w, h)."""
    cx, cy, w, h = hole
    gap = 0.012
    fw, fh = w - gap * 2, h - gap * 2
    glass_w, glass_h = fw - 0.10, fh - 0.10
    depth = 0.08
    if axis == "z":
        z_out = origin
        z_glass = z_out + inward * depth
        z_back = z_out + inward * 0.34
        g.box((cx, cy + fh * 0.5 - 0.03, z_out + inward * 0.02), (fw, 0.06, 0.05), trim)
        g.box((cx, cy - fh * 0.5 + 0.03, z_out + inward * 0.02), (fw, 0.06, 0.05), trim)
        g.box((cx - fw * 0.5 + 0.03, cy, z_out + inward * 0.02), (0.06, fh, 0.05), trim)
        g.box((cx + fw * 0.5 - 0.03, cy, z_out + inward * 0.02), (0.06, fh, 0.05), trim)
        outer = z_out - inward * 0.18
        g.box((cx, cy - h * 0.5 - 0.02, outer), (w + 0.06, 0.05, 0.08), "Lib_Concrete")
        g.box((cx, cy, z_glass), (glass_w, glass_h, 0.012), "Lib_Window")
        if lod == 0:
            g.box((cx, cy, z_glass - inward * 0.008), (0.025, glass_h - 0.08, 0.012), trim)
            g.box((cx, cy, z_glass - inward * 0.008), (glass_w - 0.08, 0.025, 0.012), trim)
        return (cx, cy, z_glass, glass_w, glass_h, "z")
    x_out = origin
    x_glass = x_out + inward * depth
    g.box((x_out + inward * 0.02, cy + fh * 0.5 - 0.03, cx), (0.05, 0.06, fw), trim)
    g.box((x_out + inward * 0.02, cy - fh * 0.5 + 0.03, cx), (0.05, 0.06, fw), trim)
    g.box((x_out + inward * 0.02, cy, cx - fw * 0.5 + 0.03), (0.05, fh, 0.06), trim)
    g.box((x_out + inward * 0.02, cy, cx + fw * 0.5 - 0.03), (0.05, fh, 0.06), trim)
    outer = x_out - inward * 0.18
    g.box((outer, cy - h * 0.5 - 0.02, cx), (0.08, 0.05, w + 0.06), "Lib_Concrete")
    g.box((x_glass, cy, cx), (0.012, glass_h, glass_w), "Lib_Window")
    if lod == 0:
        g.box((x_glass - inward * 0.008, cy, cx), (0.012, glass_h - 0.08, 0.025), trim)
        g.box((x_glass - inward * 0.008, cy, cx), (0.012, 0.025, glass_w - 0.08), trim)
    return (x_glass, cy, cx, glass_w, glass_h, "x")


def _place_backing(g, info, inward, lit):
    mat = "Lib_WindowLit" if lit else "Lib_Interior"
    push = 0.22
    if info[5] == "z":
        g.box((info[0], info[1], info[2] + inward * push), (info[3] * 0.9, info[4] * 0.9, 0.02), mat)
    else:
        g.box((info[0] + inward * push, info[1], info[2]), (0.02, info[4] * 0.9, info[3] * 0.9), mat)


def _door(g, origin, axis, inward, hole, trim, lod):
    cx, cy, w, h = hole
    info = _window(g, origin, axis, inward, hole, trim, lod)
    # Handle on the glass, street side.
    if axis == "z" and lod == 0:
        g.box((cx + w * 0.28, cy, origin - inward * 0.02), (0.04, 0.12, 0.03), "Lib_Brass")
    elif lod == 0:
        g.box((origin - inward * 0.02, cy, cx + w * 0.28), (0.03, 0.12, 0.04), "Lib_Brass")
    return info


def _wall(g, cols, axis, origin, inward, span, y0, y1, holes, body, trim, lod, tag):
    """holes entries are (cx, cy, w, h, lit, door) in the wall's horizontal axis."""
    cut = [(h[0], h[1], h[2], h[3]) for h in holes]
    for cx, cy, w, h in _panels(-span * 0.5, span * 0.5, y0, y1, cut):
        # Keep a hairline of air between panels so two caps are not the same face.
        if axis == "z":
            g.box((cx, cy, origin), (max(0.05, w - 0.008), max(0.05, h - 0.008), 0.22), body, uv_scale=1.0)
            cols.append((tag, (cx, cy, origin), (max(0.04, w - 0.03), max(0.04, h - 0.03), 0.14)))
        else:
            g.box((origin, cy, cx), (0.22, max(0.05, h - 0.008), max(0.05, w - 0.008)), body, uv_scale=1.0)
            cols.append((tag, (origin, cy, cx), (0.14, max(0.04, h - 0.03), max(0.04, w - 0.03))))
    glass = []
    for cx, cy, w, h, lit, door in holes:
        if door:
            info = _door(g, origin, axis, inward, (cx, cy, w, h), trim, lod)
        else:
            info = _window(g, origin, axis, inward, (cx, cy, w, h), trim, lod)
        _place_backing(g, info, inward, lit)
        if info[5] == "z":
            cols.append(("box", "Col_Glass", (info[0], info[1], info[2]), (info[3] * 0.85, info[4] * 0.85, 0.008)))
        else:
            cols.append(("box", "Col_Glass", (info[0], info[1], info[2]), (0.008, info[4] * 0.85, info[3] * 0.85)))
        glass.append((info, lit))
    return glass


def _parapet(g, cols, hx, hz, deck_y, body, lod):
    cap_y = deck_y + 0.28
    t = 0.16
    gap = 0.012
    # Four rims. A small gap at each corner so two faces never share a plane.
    span_x = hx * 2 - t * 2 - gap * 2
    span_z = hz * 2 - t * 2 - gap * 2
    g.box((0, cap_y, hz - t * 0.5), (span_x, 0.55, t), body)
    g.box((0, cap_y, -hz + t * 0.5), (span_x, 0.55, t), body)
    g.box((hx - t * 0.5, cap_y, 0), (t, 0.55, span_z), body)
    g.box((-hx + t * 0.5, cap_y, 0), (t, 0.55, span_z), body)
    cope = deck_y + 0.58
    if lod < 2:
        g.box((0, cope, hz - t * 0.5), (span_x, 0.06, t + 0.04), "Lib_Concrete")
        g.box((0, cope, -hz + t * 0.5), (span_x, 0.06, t + 0.04), "Lib_Concrete")
        g.box((hx - t * 0.5, cope, 0), (t + 0.04, 0.06, span_z), "Lib_Concrete")
        g.box((-hx + t * 0.5, cope, 0), (t + 0.04, 0.06, span_z), "Lib_Concrete")
    cols.append(("Col_ParapetF", (0, cap_y, hz - t * 0.5), (span_x - 0.04, 0.48, t - 0.03)))
    cols.append(("Col_ParapetB", (0, cap_y, -hz + t * 0.5), (span_x - 0.04, 0.48, t - 0.03)))


def _clutter(g, deck_y, lod):
    y = deck_y + 0.28
    g.box((-1.3, y, -0.4), (1.15, 0.52, 0.78), "Lib_SteelDark")
    if lod == 0:
        g.box((-1.3, y, -0.02), (1.05, 0.28, 0.02), "Lib_Steel")
    g.cylinder((0.2, deck_y + 0.45, 0.7), 0.10, 0.85, "Lib_Steel", 8)
    g.cylinder((1.5, deck_y + 0.7, -0.3), 0.48, 0.85, "Lib_Steel", 10 if lod == 0 else 8)
    g.cylinder((1.5, deck_y + 1.16, -0.3), 0.50, 0.06, "Lib_SteelDark", 10 if lod == 0 else 8)


def _escape(g, cols, back_z, wall_h, lod):
    """Ladder and a platform just off the back wall. back_z is the outer face, negative."""
    z = back_z - 0.42
    for x in (-0.55, 0.55):
        g.pipe((x, 0.05, z), (x, 3.15, z), 0.028, "Lib_SteelDark", 6)
        cols.append(("cap", (x, 1.6, z), 0.025, 3.0))
    rungs = lod_pick(lod, 8, 4, 0)
    for i in range(rungs):
        y = 0.4 + i * (2.6 / max(1, rungs - 1))
        g.pipe((-0.55, y, z), (0.55, y, z), 0.014, "Lib_Steel", 5)
    g.box((0, 3.2, z + 0.05), (1.5, 0.05, 0.7), "Lib_Steel")
    cols.append(("box", "Col_Escape", (0, 3.2, z + 0.05), (1.4, 0.04, 0.6)))
    if lod < 2:
        g.pipe((-0.7, 3.25, z + 0.32), (0.7, 3.25, z + 0.32), 0.02, "Lib_Steel", 6)
        g.pipe((-0.7, 3.25, z + 0.32), (-0.7, 4.15, z + 0.32), 0.02, "Lib_Steel", 6)
        g.pipe((0.7, 3.25, z + 0.32), (0.7, 4.15, z + 0.32), 0.02, "Lib_Steel", 6)
        g.pipe((-0.7, 4.15, z + 0.32), (0.7, 4.15, z + 0.32), 0.02, "Lib_Steel", 6)


def _downspouts(g, hx, back_z, top):
    for x in (-hx - 0.06, hx + 0.06):
        g.pipe((x, top, back_z - 0.08), (x, 0.02, back_z - 0.08), 0.035, "Lib_SteelDark", 6)


def _interior(g, hx, hz, front_in):
    """A shallow room behind the front glass: floor, counter, back wall."""
    g.box((0, 0.04, 0), (hx * 2 - 0.7, 0.06, hz * 2 - 0.7), "Lib_Wood")
    g.box((0, 0.95, front_in - 1.15), (1.8, 0.9, 0.45), "Lib_WoodDark")
    g.box((0, 1.6, front_in - 2.4), (hx * 2 - 0.9, 2.6, 0.08), "Lib_Interior")


def _awning(g, cols, hx, front_z, y, mat, lod):
    depth = 1.15
    g.box((0, y, front_z + depth * 0.5), (hx * 1.7, 0.06, depth), mat)
    g.box((0, y - 0.08, front_z + depth - 0.04), (hx * 1.7, 0.10, 0.06), mat)
    cols.append(("box", "Col_Awning", (0, y, front_z + depth * 0.5), (hx * 1.65, 0.05, depth - 0.04)))
    if lod == 0:
        g.pipe((-hx * 0.7, y - 0.06, front_z + depth - 0.1), (-hx * 0.7, 0.02, front_z + 0.15), 0.025, "Lib_SteelDark", 6)
        g.pipe((hx * 0.7, y - 0.06, front_z + depth - 0.1), (hx * 0.7, 0.02, front_z + 0.15), 0.025, "Lib_SteelDark", 6)


def _sign(g, text, front_z, y, mat, lod):
    g.box((0, y, front_z + 0.06), (2.4, 0.55, 0.06), mat)
    if lod == 0 and text:
        g.text(text, (0, y, front_z + 0.11), 0.22, "Lib_PaintCream", extrude=0.008, yaw=0)


def _gable_slab(g, x, y_base, z0, z1, y_peak, z_peak, thick, mat):
    """A closed triangular wall. x is the outer face; thick steps inward (+X)."""
    x1 = x + thick
    verts = [
        (x, y_base, z0), (x, y_base, z1), (x, y_peak, z_peak),
        (x1, y_base, z0), (x1, y_base, z1), (x1, y_peak, z_peak),
    ]
    faces = [
        (0, 2, 1),
        (3, 4, 5),
        (0, 1, 4, 3),
        (1, 2, 5, 4),
        (2, 0, 3, 5),
    ]
    g.mesh(verts, faces, mat, uv_scale=1.0)


def _hip(g, hx, hz, eave, rise):
    """Closed hip: flat ceiling at the eave, four slopes up to a short ridge."""
    ridge = hx * 0.42
    y0 = eave + 0.01
    y1 = eave + rise
    # Bottom, then ridge. Winding is repaired by recalc in mesh().
    verts = [
        (-hx, y0, -hz),
        (hx, y0, -hz),
        (hx, y0, hz),
        (-hx, y0, hz),
        (-ridge, y1, 0.0),
        (ridge, y1, 0.0),
    ]
    faces = [
        (0, 1, 2, 3),
        (0, 4, 5, 1),
        (3, 2, 5, 4),
        (1, 5, 2),
        (0, 3, 4),
    ]
    g.mesh(verts, faces, "Lib_Roof", uv_scale=1.0)


def build_store(profile):
    def create():
        sx = profile["sx"]
        sz = profile["sz"]
        wall_h = profile["wall_h"]
        hx, hz = sx * 0.5, sz * 0.5
        thick = 0.22
        a = Asset(profile["name"], "Buildings", profile["blurb"])
        a.climbable = True
        a.climb_note = "The four walls are cling. Glass is solid. The roof is a parapet, not a rail."
        a.vault_note = "No ground-height rail. The parapet is on the roof."
        a.loose_pivot = True
        front_z = hz - thick * 0.5
        back_z = -hz + thick * 0.5
        side_x = hx - thick * 0.5
        # A few millimetres of air at the corners so the shells do not weld or overlap.
        span_x = sx - thick * 2 - 0.012
        span_z = sz - thick * 2 - 0.012
        for lod in (0, 1, 2):
            g = a.begin(lod)
            cols = []
            _wall(
                g, cols, "z", front_z, -1.0, span_x, 0.0, wall_h,
                _fit_holes(span_x, wall_h, profile["front"]),
                profile["body"], profile["trim"], lod, "Climb_Front",
            )
            _wall(
                g, cols, "z", back_z, 1.0, span_x, 0.0, wall_h,
                _fit_holes(span_x, wall_h, profile["back"]),
                profile["body"], profile["trim"], lod, "Climb_Back",
            )
            _wall(
                g, cols, "x", side_x, -1.0, span_z, 0.0, wall_h,
                _fit_holes(span_z, wall_h, profile["right"]),
                profile["body"], profile["trim"], lod, "Climb_Right",
            )
            _wall(
                g, cols, "x", -side_x, 1.0, span_z, 0.0, wall_h,
                _fit_holes(span_z, wall_h, profile["left"]),
                profile["body"], profile["trim"], lod, "Climb_Left",
            )
            deck = wall_h + 0.08
            g.box((0, deck, 0), (sx, 0.10, sz), "Lib_Concrete", uv_scale=0.6)
            cols.append(("box", "Col_Roof", (0, deck, 0), (sx - 0.08, 0.08, sz - 0.08)))
            _parapet(g, cols, hx, hz, deck + 0.05, profile["body"], lod)
            if lod < 2:
                _clutter(g, deck + 0.05, lod)
                _escape(g, cols, -hz, wall_h, lod)
                _downspouts(g, hx, -hz, wall_h)
                _interior(g, hx, hz, front_z)
                _awning(g, cols, hx * 0.92, hz, 3.05 if wall_h > 4 else wall_h - 0.4, profile["awning"], lod)
                _sign(g, profile["sign"], hz, 3.55 if wall_h > 4 else wall_h - 0.15, profile["accent"], lod)
                # A second awning on the corner face when that side is a shop.
                if profile["right"] == "shop" and lod < 2:
                    # Reuse the awning helper by swapping axes: a box on +X.
                    depth = 1.05
                    g.box((hx + depth * 0.5, 3.05, 0.2), (depth, 0.06, span_z * 0.7), profile["awning"])
            if lod == 0:
                a._cols = cols
            a.end()
        _apply_cols(a)
        return a
    return create


def _apply_cols(asset):
    seen = {}
    for item in getattr(asset, "_cols", []):
        kind = item[0]
        if kind == "box":
            name, center, size = item[1], item[2], item[3]
            # Skip slivers the validator cannot seat inside a thin shell.
            if min(size) < 0.006:
                continue
            base = name
            seen[base] = seen.get(base, 0) + 1
            if seen[base] > 1:
                name = "%s_%d" % (base, seen[base])
            asset.box(name, center, size)
        elif kind == "cap":
            pass
        else:
            name, center, size = kind, item[1], item[2]
            if min(abs(v) for v in size) < 0.006:
                continue
            base = name
            seen[base] = seen.get(base, 0) + 1
            label = base if seen[base] == 1 else "%s_%d" % (base, seen[base])
            asset.box(label, center, size)


def build_house(profile):
    def create():
        sx = profile["sx"]
        body_z = profile["sz"]
        porch = profile["porch"]
        wall_h = profile["wall_h"]
        # Shift so the foundation, including the porch, is centered on Z.
        shift = -porch * 0.5
        a = Asset(profile["name"], "Buildings", profile["blurb"])
        a.climbable = True
        a.vaultable = True
        a.vault_height = 0.95
        a.climb_note = "Siding walls are cling. Windows are glass. The porch is open on +Z."
        a.vault_note = "Porch rail is 0.95 m above the porch deck."
        hx = sx * 0.5
        hz = body_z * 0.5
        thick = 0.18
        for lod in (0, 1, 2):
            g = a.begin(lod)
            cols = []

            def wz(z):
                return z + shift

            front = wz(hz - thick * 0.5)
            back = wz(-hz + thick * 0.5)
            span_x = sx - thick * 2 - 0.012
            span_z = body_z - thick * 2 - 0.012
            _wall(
                g, cols, "z", front, -1.0, span_x, 0.0, wall_h,
                _fit_holes(span_x, wall_h, "house_front"),
                profile["body"], profile["trim"], lod, "Climb_Front",
            )
            _wall(
                g, cols, "z", back, 1.0, span_x, 0.0, wall_h,
                _fit_holes(span_x, wall_h, "plain"),
                profile["body"], profile["trim"], lod, "Climb_Back",
            )
            _wall(
                g, cols, "x", hx - thick * 0.5, -1.0, span_z, 0.0, wall_h,
                _fit_holes(span_z, wall_h, "plain"),
                profile["body"], profile["trim"], lod, "Climb_Right",
            )
            _wall(
                g, cols, "x", -(hx - thick * 0.5), 1.0, span_z, 0.0, wall_h,
                _fit_holes(span_z, wall_h, "plain"),
                profile["body"], profile["trim"], lod, "Climb_Left",
            )
            # Porch deck and roof, in front of the body. The walls themselves hit the ground.
            deck_z = wz(hz + porch * 0.5)
            g.box((0, 0.30, deck_z), (sx * 0.72, 0.16, porch - 0.05), "Lib_Wood")
            cols.append(("box", "Col_Porch", (0, 0.30, deck_z), (sx * 0.68, 0.12, porch - 0.12)))
            roof_y = 2.55
            porch_len = porch - 0.25
            porch_cz = wz(hz + 0.2 + porch_len * 0.5)
            g.box((0, roof_y, porch_cz), (sx * 0.78, 0.08, porch_len), "Lib_Roof")
            cols.append(("box", "Col_PorchRoof", (0, roof_y, porch_cz), (sx * 0.62, 0.04, porch_len - 0.16)))
            for x in (-sx * 0.30, sx * 0.30):
                g.box((x, 1.25, wz(hz + porch - 0.12)), (0.14, 2.0, 0.14), profile["trim"])
            if lod < 2:
                rail_z = wz(hz + porch - 0.12)
                g.pipe((-sx * 0.30, 1.25, rail_z), (sx * 0.30, 1.25, rail_z), 0.035, profile["trim"], 6)
                if lod == 0:
                    a.capsule("Vault_PorchRail", (0, 1.25, rail_z), 0.03, sx * 0.55, 0)
            if profile["roof"] == "hip":
                _hip(g, hx + 0.15, hz + 0.12, wall_h, profile["rise"])
            else:
                ridge = wall_h + profile["rise"]
                z0 = wz(-hz - 0.15)
                z1 = wz(0.0)
                g.mesh(_prism(sx + 0.35, z0, wall_h + 0.02, z1, ridge, 0.08), _prism_faces(), "Lib_Roof")
                g.mesh(_prism(sx + 0.35, wz(hz - 0.02), wall_h + 0.02, wz(0.0), ridge, 0.08), _prism_faces(), "Lib_Roof")
                # Closed gable slabs, just outside the wall so they do not weld to it.
                _gable_slab(
                    g, -hx - 0.06, wall_h + 0.04, wz(-hz + 0.08), wz(hz - 0.08),
                    ridge - 0.08, wz(0.0), 0.04, profile["body"],
                )
                _gable_slab(
                    g, hx + 0.02, wall_h + 0.04, wz(-hz + 0.08), wz(hz - 0.08),
                    ridge - 0.08, wz(0.0), 0.04, profile["body"],
                )
            if lod == 0:
                _interior(g, hx, hz, front)
                g.box((0, wall_h + profile["rise"] + 0.15, shift), (0.4, 0.5, 0.4), "Lib_Brick")
            if lod == 0:
                a._cols = cols
            a.end()
        _apply_cols(a)
        return a
    return create


def register_store(profile):
    fn = build_store(profile)
    fn.__module__ = profile["name"]
    return register(fn)


def register_house(profile):
    fn = build_house(profile)
    fn.__module__ = profile["name"]
    return register(fn)
