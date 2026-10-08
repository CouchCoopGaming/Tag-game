"""Closed building shells. Four walls, a roof, and a room you can see into.

Wall boxes meet on a plane and do not share a volume, so the collider test
keeps a stable inside/outside. Windows sit in real openings.
"""

import os
import sys

from mathutils import Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick, _collider_samples, _point_inside, unity_to_blender
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


# Ground-floor shopfront. One tall opening; the kickplate, glass, and door sit in it.
SHOP_HEAD = 2.56
SHOP_PIER = 0.32


def _shop_style(kind):
    if kind in ("shop", "shop_market"):
        return "market"
    if kind == "shop_diner":
        return "diner"
    if kind == "shop_wash":
        return "wash"
    return None


def _house_row(span, n):
    wh = 1.12
    cy = 1.50
    holes = []
    for i in range(n):
        cx = ((i + 0.5) / float(n) - 0.5) * span * 0.56
        holes.append((cx, cy, min(0.88, span * 0.2), wh, False, False))
    return holes


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
    if kind == "house_front":
        ww = min(0.92, span * 0.18)
        return [
            (-span * 0.28, 1.52, ww, 1.18, False, False),
            (0.05, 1.08, 0.96, 2.02, False, "solid"),
            (span * 0.28, 1.52, ww, 1.18, False, False),
        ]
    if kind == "house_side":
        return _house_row(span, 2 if span > 5.0 else 1)
    if kind == "house_back":
        return _house_row(span, 2)
    style = _shop_style(kind)
    if style is None:
        return []
    # One shopfront void from the sidewalk to the head. Piers stay as brick.
    open_w = span - SHOP_PIER * 2
    cy = (0.04 + SHOP_HEAD) * 0.5
    holes = [(0.0, cy, open_w, SHOP_HEAD - 0.04, False, "opening")]
    # Above the taller sign band and the cornice. Short diners keep a parapet instead of a slit window.
    win_bot = 3.96
    win_top = wall_h - 0.26
    if win_top - win_bot >= 0.50:
        if wall_h > 5.5:
            wh = 1.15
            wcy = wall_h - 0.30 - wh * 0.5
        else:
            wh = min(0.82, win_top - win_bot - 0.06)
            wcy = (win_bot + win_top) * 0.5
        count = 3 if span > 6.2 else 2
        for i in range(count):
            cx = ((i + 0.5) / float(count) - 0.5) * (span * 0.70)
            holes.append((cx, wcy, min(1.15, span * 0.16), wh, i % 2 == 0, False))
    return holes


# _wall shells are 0.22 m thick and centered on the wall origin.
_WALL_HALF = 0.11
# Sash sits this far behind the outer brick face.
_REVEAL = 0.13


def _window(g, origin, axis, inward, hole, trim, lod, body="Lib_Brick"):
    """Brick returns in the opening, sash 13 cm back, stone sill and lintel."""
    cx, cy, w, h = hole
    glass_w, glass_h = w - 0.16, h - 0.16
    face = _WALL_HALF
    # Glass just inside the inner end of the jamb so the two shells do not touch.
    glass_d = face - _REVEAL - 0.012
    # Jamb runs from 1 cm behind the face back to the reveal plane.
    jamb_outer = face - 0.010
    jamb_inner = face - _REVEAL
    jamb_mid = (jamb_outer + jamb_inner) * 0.5
    jamb_depth = jamb_outer - jamb_inner
    side = 0.036
    # Stay inside the hole. The wall panel stops 4 mm outside the cut.
    inset = side * 0.5 + 0.006
    if axis == "z":
        z_glass = _street(origin, inward, glass_d)
        z_jamb = _street(origin, inward, jamb_mid)
        g.box((cx - w * 0.5 + inset, cy, z_jamb), (side, h - 0.012, jamb_depth), body)
        g.box((cx + w * 0.5 - inset, cy, z_jamb), (side, h - 0.012, jamb_depth), body)
        inner_w = w - inset * 2 - side - 0.008
        g.box((cx, cy + h * 0.5 - inset, z_jamb), (inner_w, side, jamb_depth), body)
        g.box((cx, cy - h * 0.5 + inset, z_jamb), (inner_w, side, jamb_depth), body)
        # Sill and lintel sit entirely outside the brick face.
        z_stone = _street(origin, inward, face + 0.055)
        g.box((cx, cy - h * 0.5 - 0.045, z_stone), (w + 0.10, 0.055, 0.07), "Lib_Concrete")
        g.box((cx, cy + h * 0.5 + 0.045, z_stone), (w + 0.14, 0.06, 0.06), "Lib_Concrete")
        g.box((cx, cy, z_glass), (glass_w, glass_h, 0.012), "Lib_Window")
        if lod == 0:
            bar = _street(origin, inward, glass_d - 0.010)
            g.box((cx, cy, bar), (0.025, glass_h - 0.06, 0.012), trim)
            g.box((cx, cy, bar), (glass_w - 0.06, 0.025, 0.012), trim)
        _casing(g, origin, "z", inward, cx, cy, w, h, trim)
        return (cx, cy, z_glass, glass_w, glass_h, "z")
    x_glass = _street(origin, inward, glass_d)
    x_jamb = _street(origin, inward, jamb_mid)
    g.box((x_jamb, cy, cx - w * 0.5 + inset), (jamb_depth, h - 0.012, side), body)
    g.box((x_jamb, cy, cx + w * 0.5 - inset), (jamb_depth, h - 0.012, side), body)
    inner_w = w - inset * 2 - side - 0.008
    g.box((x_jamb, cy + h * 0.5 - inset, cx), (jamb_depth, side, inner_w), body)
    g.box((x_jamb, cy - h * 0.5 + inset, cx), (jamb_depth, side, inner_w), body)
    x_stone = _street(origin, inward, face + 0.055)
    g.box((x_stone, cy - h * 0.5 - 0.045, cx), (0.07, 0.055, w + 0.10), "Lib_Concrete")
    g.box((x_stone, cy + h * 0.5 + 0.045, cx), (0.06, 0.06, w + 0.14), "Lib_Concrete")
    g.box((x_glass, cy, cx), (0.012, glass_h, glass_w), "Lib_Window")
    if lod == 0:
        bar = _street(origin, inward, glass_d - 0.010)
        g.box((bar, cy, cx), (0.012, glass_h - 0.06, 0.025), trim)
        g.box((bar, cy, cx), (0.012, 0.025, glass_w - 0.06), trim)
    _casing(g, origin, "x", inward, cx, cy, w, h, trim)
    return (x_glass, cy, cx, glass_w, glass_h, "x")


def _place_backing(g, info, inward, lit):
    mat = "Lib_WindowLit" if lit else "Lib_Interior"
    push = 0.22
    if info[5] == "z":
        g.box((info[0], info[1], info[2] + inward * push), (info[3] * 0.9, info[4] * 0.9, 0.02), mat)
    else:
        g.box((info[0] + inward * push, info[1], info[2]), (0.02, info[4] * 0.9, info[3] * 0.9), mat)


def _casing(g, origin, axis, inward, cx, cy, w, h, trim):
    """Architrave proud of the outer face, clear of the wall shell."""
    # Proud of the stone sill and lintel, which end about 20 cm out from the wall center.
    n = origin - inward * 0.220
    t = 0.075
    d = 0.032
    if axis == "z":
        g.box((cx, cy + h * 0.5 + t * 0.4, n), (w + t * 1.6, t, d), trim)
        g.box((cx, cy - h * 0.5 - t * 0.4, n), (w + t * 1.6, t, d), trim)
        g.box((cx - w * 0.5 - t * 0.4, cy, n), (t, h, d), trim)
        g.box((cx + w * 0.5 + t * 0.4, cy, n), (t, h, d), trim)
    else:
        g.box((n, cy + h * 0.5 + t * 0.4, cx), (d, t, w + t * 1.6), trim)
        g.box((n, cy - h * 0.5 - t * 0.4, cx), (d, t, w + t * 1.6), trim)
        g.box((n, cy, cx - w * 0.5 - t * 0.4), (d, h, t), trim)
        g.box((n, cy, cx + w * 0.5 + t * 0.4), (d, h, t), trim)


def _solid_door(g, origin, axis, inward, hole, trim, lod):
    """A paneled entry door, recessed a few centimetres, with casing."""
    cx, cy, w, h = hole
    n = origin - inward * 0.02
    if axis == "z":
        g.box((cx, cy, n), (w - 0.08, h - 0.05, 0.045), "Lib_WoodDark")
        if lod == 0:
            g.box((cx, cy + h * 0.22, n - inward * 0.028), (w * 0.62, h * 0.36, 0.012), "Lib_Wood")
            g.box((cx, cy - h * 0.22, n - inward * 0.028), (w * 0.62, h * 0.38, 0.012), "Lib_Wood")
            g.box((cx + w * 0.28, cy, n - inward * 0.04), (0.04, 0.12, 0.03), "Lib_Brass")
        center = (cx, cy, n)
        size = (w - 0.14, h - 0.10, 0.03)
    else:
        g.box((n, cy, cx), (0.045, h - 0.05, w - 0.08), "Lib_WoodDark")
        if lod == 0:
            g.box((n - inward * 0.028, cy + h * 0.22, cx), (0.012, h * 0.36, w * 0.62), "Lib_Wood")
            g.box((n - inward * 0.028, cy - h * 0.22, cx), (0.012, h * 0.38, w * 0.62), "Lib_Wood")
            g.box((n - inward * 0.04, cy, cx + w * 0.28), (0.03, 0.12, 0.04), "Lib_Brass")
        center = (n, cy, cx)
        size = (0.03, h - 0.10, w - 0.14)
    _casing(g, origin, axis, inward, cx, cy, w, h, trim)
    return center, size


def _door(g, origin, axis, inward, hole, trim, lod):
    cx, cy, w, h = hole
    info = _window(g, origin, axis, inward, hole, trim, lod, "Lib_Brick")
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
        if door == "opening":
            continue
        if door == "solid":
            center, size = _solid_door(g, origin, axis, inward, (cx, cy, w, h), trim, lod)
            cols.append(("box", "Col_Door", center, size))
            continue
        if door:
            info = _door(g, origin, axis, inward, (cx, cy, w, h), trim, lod)
        else:
            info = _window(g, origin, axis, inward, (cx, cy, w, h), trim, lod, body)
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


def _street(origin, inward, dist):
    """dist > 0 is toward the street, measured from the wall center."""
    return origin - inward * dist


def _box_ax(g, axis, along, y, normal, sa, sy, sn, mat):
    if axis == "z":
        g.box((along, y, normal), (sa, sy, sn), mat)
        return (along, y, normal), (sa, sy, sn)
    g.box((normal, y, along), (sn, sy, sa), mat)
    return (normal, y, along), (sn, sy, sa)


def _col_box(cols, name, center, size):
    if min(size) < 0.008:
        return
    cols.append(("box", name, center, size))


_SHOP = {
    "market": {
        "door": "center", "bay": 1.15, "kick": 0.50,
        "kick_mat": "Lib_PaintCream", "mullion": None,
        "door_mat": "Lib_WoodDark", "sign_mat": "Lib_PaintRed", "text_mat": "Lib_PaintCream",
        "rail": False,
    },
    "diner": {
        "door": "right", "bay": 1.40, "kick": 0.36,
        "kick_mat": "Lib_Steel", "mullion": "Lib_Steel",
        "door_mat": "Lib_PaintRed", "sign_mat": "Lib_PaintCream", "text_mat": "Lib_PaintRed",
        "rail": True,
    },
    "wash": {
        "door": "left", "bay": 1.10, "kick": 0.64,
        "kick_mat": "Lib_PaintWhite", "mullion": "Lib_PaintTeal",
        "door_mat": "Lib_PaintWhite", "sign_mat": "Lib_PaintTeal", "text_mat": "Lib_PaintWhite",
        "rail": False, "shelves": False, "kick_rail": False,
    },
}


def _dress_storefront(g, cols, axis, origin, inward, span, profile, style, lod):
    """Kickplate, mullioned glass, recessed door and transom, sign, cornice, sloped awning."""
    spec = _SHOP[style]
    mullion = spec["mullion"] or profile["trim"]
    half = (span - SHOP_PIER * 2) * 0.5
    door_w = 0.96
    if spec["door"] == "right":
        dc = half - 0.62
    elif spec["door"] == "left":
        dc = -half + 0.62
    else:
        dc = 0.0
    door_a, door_b = dc - door_w * 0.5, dc + door_w * 0.5
    kick = spec["kick"]
    glass_bot = kick + 0.045
    glass_top = SHOP_HEAD - 0.05
    # Display sits 14 cm behind the brick face. The wall shell is 0.22 m, centered.
    glass_n = _street(origin, inward, _WALL_HALF - 0.14)
    door_n = _street(origin, inward, -0.18)

    def bays_of(a0, a1):
        width = a1 - a0
        if width < 0.40:
            return []
        n = max(1, int(round(width / spec["bay"])))
        step = width / float(n)
        return [(a0 + i * step, a0 + (i + 1) * step) for i in range(n)]

    bays = bays_of(-half + 0.03, door_a - 0.05) + bays_of(door_b + 0.05, half - 0.03)
    # Bulkhead under the display only. The door runs to the sidewalk.
    for a0, a1 in bays:
        inset = 0.012
        kick_h = max(0.18, kick - 0.08)
        kick_y = 0.055 + kick_h * 0.5
        _c, _s = _box_ax(
            g, axis, (a0 + a1) * 0.5, kick_y, _street(origin, inward, 0.04),
            (a1 - a0) - inset * 2, kick_h, 0.10, spec["kick_mat"],
        )
        _col_box(cols, "Col_Kick", _c, ((a1 - a0) - 0.06, kick * 0.7, 0.10) if axis == "z" else (0.10, kick * 0.7, (a1 - a0) - 0.06))
        if lod == 0 and spec.get("kick_rail", True):
            _box_ax(g, axis, (a0 + a1) * 0.5, kick - 0.02, _street(origin, inward, 0.09), (a1 - a0) - 0.02, 0.035, 0.025, mullion)
    # Display panes and the mullions between them.
    gh = glass_top - glass_bot
    gcy = (glass_top + glass_bot) * 0.5
    for i, (a0, a1) in enumerate(bays):
        _c, _s = _box_ax(g, axis, (a0 + a1) * 0.5, gcy, glass_n, (a1 - a0) - 0.05, gh - 0.04, 0.012, "Lib_ShopGlass")
        _col_box(cols, "Col_Glass", _c, ((a1 - a0) - 0.08, gh - 0.08, 0.008) if axis == "z" else (0.008, gh - 0.08, (a1 - a0) - 0.08))
        if lod < 2:
            # Mullion fills the reveal, from just behind the brick face back to the glass.
            _box_ax(g, axis, a0, gcy, _street(origin, inward, 0.035), 0.045, gh, 0.09, mullion)
        if spec["rail"] and lod < 2:
            _box_ax(g, axis, (a0 + a1) * 0.5, 1.48, _street(origin, inward, _WALL_HALF - 0.12), (a1 - a0) - 0.08, 0.03, 0.016, "Lib_PaintRed")
    if bays and lod < 2:
        last = bays[-1][1]
        _box_ax(g, axis, last, gcy, _street(origin, inward, 0.035), 0.045, gh, 0.09, mullion)
    # Shelves and a dark interior card behind the glass, clear of the door recess.
    shelf_n = _street(origin, inward, -0.50)
    for a0, a1 in bays if spec.get("shelves", True) else []:
        if a1 - a0 < 0.50:
            continue
        bay_w = (a1 - a0) - 0.08
        for sy in (1.08, 1.58):
            _c, _s = _box_ax(g, axis, (a0 + a1) * 0.5, sy, shelf_n, bay_w, 0.028, 0.22, "Lib_Wood")
            along = bay_w - 0.06
            if axis == "z":
                _col_box(cols, "Col_Shelf", _c, (along * 0.64, 0.008, 0.05))
            else:
                _col_box(cols, "Col_Shelf", _c, (0.05, 0.008, along * 0.64))
        if lod == 0:
            goods_n = _street(origin, inward, -0.30)
            _box_ax(
                g, axis, a0 + (a1 - a0) * 0.32, 1.20, goods_n,
                min(0.26, (a1 - a0) * 0.32), 0.16, 0.14, "Lib_Orange",
            )
    card_n = _street(origin, inward, -0.82)
    _c, _s = _box_ax(g, axis, 0.0, 1.28, card_n, max(0.4, half * 1.55), 1.50, 0.02, "Lib_Interior")
    if axis == "z":
        _col_box(cols, "Col_Interior", _c, (max(0.3, half * 1.45), 1.40, 0.012))
    else:
        _col_box(cols, "Col_Interior", _c, (0.012, 1.40, max(0.3, half * 1.45)))
    # Recessed door, jambs, and a transom in the storefront plane.
    dh = 2.00
    _c, _s = _box_ax(g, axis, dc, 0.05 + dh * 0.5, door_n, door_w - 0.08, dh, 0.045, spec["door_mat"])
    _col_box(cols, "Col_Door", _c, (door_w - 0.12, dh - 0.08, 0.03) if axis == "z" else (0.03, dh - 0.08, door_w - 0.12))
    if lod == 0:
        lite_n = _street(origin, inward, -0.145)
        _box_ax(g, axis, dc, 1.45, lite_n, door_w * 0.55, 0.70, 0.012, "Lib_ShopGlass")
        _box_ax(g, axis, dc + door_w * 0.30, 1.05, _street(origin, inward, -0.14), 0.035, 0.12, 0.02, "Lib_Brass")
    # Jambs from the glass plane back to the door. They stay inside the opening.
    jamb_n = _street(origin, inward, -0.11)
    jamb_d = 0.14
    for edge, sign in ((door_a, -1.0), (door_b, 1.0)):
        _box_ax(g, axis, edge + sign * 0.02, 1.08, jamb_n, 0.04, 1.96, jamb_d, profile["trim"])
    _box_ax(g, axis, dc, 2.16, jamb_n, door_w - 0.02, 0.045, jamb_d, profile["trim"])
    th = glass_top - 2.20
    if th > 0.08:
        _c, _s = _box_ax(g, axis, dc, 2.20 + th * 0.5, glass_n, door_w - 0.08, th - 0.03, 0.012, "Lib_ShopGlass")
        _col_box(cols, "Col_Transom", _c, (door_w - 0.14, max(0.04, th - 0.06), 0.008) if axis == "z" else (0.008, max(0.04, th - 0.06), door_w - 0.14))
        _box_ax(g, axis, dc, 2.175, _street(origin, inward, 0.065), door_w, 0.04, 0.035, mullion)
    # Sign band. Letters are about 2.5x the pass-5 size, on a dark plate wider than the board.
    # Awning high edge is y=2.48. Backing starts at 2.52. Cornice stays under the upstairs sill.
    sign_w = min(span * 0.78, 5.2 if style == "diner" else 4.2)
    back_w = min(span - 0.20, sign_w + 0.28)
    back_n = _street(origin, inward, 0.148)
    sign_n = _street(origin, inward, 0.198)
    _c, _s = _box_ax(g, axis, 0.0, 2.98, back_n, back_w, 0.92, 0.030, "Lib_SteelDark")
    _col_box(cols, "Col_SignBack", _c, (back_w - 0.08, 0.82, 0.018) if axis == "z" else (0.018, 0.82, back_w - 0.08))
    _c, _s = _box_ax(g, axis, 0.0, 2.98, sign_n, sign_w, 0.74, 0.048, spec["sign_mat"])
    _col_box(cols, "Col_Sign", _c, (sign_w - 0.08, 0.64, 0.030) if axis == "z" else (0.030, 0.64, sign_w - 0.08))
    if lod == 0 and profile.get("sign"):
        yaw = 0.0 if axis == "z" else (90.0 if inward < 0.0 else -90.0)
        text_n = _street(origin, inward, 0.255)
        loc = (0.0, 2.98, text_n) if axis == "z" else (text_n, 2.98, 0.0)
        g.text(profile["sign"], loc, 0.62, spec["text_mat"], extrude=0.006, yaw=yaw)
    _awning_fabric(
        g, cols, axis, origin, inward, -half + 0.02, half - 0.02, profile["awning"], lod,
        scallops=profile.get("scallops", 8),
    )


def _awning_fabric(g, cols, axis, origin, inward, along0, along1, mat, lod, clip_pos=False, clip_neg=False, scallops=8):
    """Sloped cloth, a scalloped valance, and an angled steel frame. Not a flat slab."""
    depth = 1.26
    y_hi = 2.48
    y_lo = 2.06
    thick = 0.028
    n_hi = _street(origin, inward, 0.14)
    n_lo = _street(origin, inward, 0.14 + depth)

    def p(along, y, normal):
        if axis == "z":
            return (along, y, normal)
        return (normal, y, along)

    v = [
        p(along0, y_hi, n_hi), p(along1, y_hi, n_hi), p(along1, y_lo, n_lo), p(along0, y_lo, n_lo),
        p(along0, y_hi - thick, n_hi), p(along1, y_hi - thick, n_hi),
        p(along1, y_lo - thick, n_lo), p(along0, y_lo - thick, n_lo),
    ]
    g.mesh(v, [
        (0, 3, 2, 1), (4, 5, 6, 7),
        (0, 4, 7, 3), (1, 2, 6, 5),
        (0, 1, 5, 4), (3, 7, 6, 2),
    ], mat, uv_scale=1.0)
    # Three axis-aligned slices live inside the slope so the collider does not leave the cloth.
    span = along1 - along0
    slices = 5
    for i in range(slices):
        t = (i + 0.5) / float(slices)
        y_top = y_hi + (y_lo - y_hi) * t
        normal = _street(origin, inward, 0.14 + depth * t)
        seg = depth / float(slices) * 0.28
        cy = y_top - thick * 0.55
        if axis == "z":
            _col_box(cols, "Col_Awning", ((along0 + along1) * 0.5, cy, normal), (span * 0.72, thick * 0.28, seg))
        else:
            _col_box(cols, "Col_Awning", (normal, cy, (along0 + along1) * 0.5), (seg, thick * 0.28, span * 0.72))
    scallops = scallops if lod == 0 else 3
    for i in range(scallops):
        a0 = along0 + span * i / scallops
        a1 = along0 + span * (i + 1) / scallops
        drop = 0.15 if i % 2 == 0 else 0.24
        _box_ax(g, axis, (a0 + a1) * 0.5, y_lo - thick - drop * 0.5, n_lo, (a1 - a0) * 0.94, drop, 0.016, mat)
    if lod < 2:
        if not clip_neg:
            g.pipe(p(along0 - 0.04, y_hi - thick - 0.02, n_hi), p(along0 - 0.04, y_lo - thick - 0.02, n_lo), 0.016, "Lib_SteelDark", 5)
        if not clip_pos:
            g.pipe(p(along1 + 0.04, y_hi - thick - 0.02, n_hi), p(along1 + 0.04, y_lo - thick - 0.02, n_lo), 0.016, "Lib_SteelDark", 5)
        front_n = _street(origin, inward, 0.14 + depth + 0.025)
        bar0 = along0 if clip_neg else along0 - 0.04
        bar1 = along1 if clip_pos else along1 + 0.04
        g.pipe(p(bar0, y_lo - thick - 0.02, front_n), p(bar1, y_lo - thick - 0.02, front_n), 0.016, "Lib_SteelDark", 5)
        mid = (along0 + along1) * 0.5
        g.pipe(p(mid, y_hi - thick - 0.03, n_hi), p(mid, y_lo - thick - 0.03, n_lo), 0.014, "Lib_SteelDark", 5)


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


def _hip(g, hx, hz, eave, rise, zshift=0.0):
    """Closed hip: flat ceiling at the eave, four slopes up to a short ridge."""
    ridge = hx * 0.42
    y0 = eave + 0.01
    y1 = eave + rise

    def zz(z):
        return z + zshift

    # Bottom, then ridge. Winding is repaired by recalc in mesh().
    verts = [
        (-hx, y0, zz(-hz)),
        (hx, y0, zz(-hz)),
        (hx, y0, zz(hz)),
        (-hx, y0, zz(hz)),
        (-ridge, y1, zz(0.0)),
        (ridge, y1, zz(0.0)),
    ]
    faces = [
        (0, 1, 2, 3),
        (0, 4, 5, 1),
        (3, 2, 5, 4),
        (1, 5, 2),
        (0, 3, 4),
    ]
    g.mesh(verts, faces, "Lib_Roof", uv_scale=1.0)


def _extrude(g, poly, y0, y1, mat):
    """Closed extrusion of an XZ polygon. poly is (x, z) around the outline."""
    n = len(poly)
    if n < 3 or y1 - y0 < 0.02:
        return
    verts = [(x, y0, z) for x, z in poly] + [(x, y1, z) for x, z in poly]
    faces = [tuple(range(n - 1, -1, -1)), tuple(range(n, n * 2))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, j + n, i + n))
    g.mesh(verts, faces, mat, uv_scale=1.0)


def _quoins(g, hx, hz, wall_h, body, lod, pitch0=0.40):
    """Alternating header and stretcher stones outside each corner, clear of the wall shells."""
    corners = ((1.0, 1.0), (-1.0, 1.0), (1.0, -1.0), (-1.0, -1.0))
    if lod >= 2:
        for sx, sz in corners:
            _notch(g, hx, hz, sx, sz, 0.04, wall_h - 0.04, body)
        return
    pitch = pitch0 if lod == 0 else pitch0 * 2.0
    y = 0.03
    course = 0
    while y + 0.16 < wall_h - 0.02:
        y1 = min(y + pitch - 0.024, wall_h - 0.03)
        if y1 - y < 0.10:
            break
        # The sign band, awning frame, and cornice own the projecting corner through here.
        exposed = y1 < 1.86 or y > 3.94
        for sx, sz in corners:
            if exposed:
                _quoin_l(g, hx, hz, sx, sz, y, y1, course % 2 == 0, body)
            else:
                _notch(g, hx, hz, sx, sz, y, y1, body)
        course += 1
        y += pitch


def _notch(g, hx, hz, sx, sz, y0, y1, mat):
    ax, az = abs(hx), abs(hz)
    g.box(
        (sx * (ax - 0.112), (y0 + y1) * 0.5, sz * (az - 0.112)),
        (0.188, y1 - y0, 0.188),
        mat,
    )


def _quoin_l(g, hx, hz, sx, sz, y0, y1, long_front, mat):
    ax, az = abs(hx), abs(hz)
    if long_front:
        local = (
            (ax - 0.50, az + 0.008),
            (ax - 0.50, az + 0.030),
            (ax + 0.018, az + 0.030),
            (ax + 0.018, az - 0.216),
            (ax - 0.216, az - 0.216),
            (ax - 0.216, az + 0.008),
        )
    else:
        local = (
            (ax + 0.008, az - 0.50),
            (ax + 0.030, az - 0.50),
            (ax + 0.030, az + 0.018),
            (ax - 0.216, az + 0.018),
            (ax - 0.216, az - 0.216),
            (ax + 0.008, az - 0.216),
        )
    _extrude(g, [(sx * x, sz * z) for x, z in local], y0, y1, mat)


def _outward_span(origin, inward, dist, depth):
    """Inner and outer coordinates of a projecting band. Inner is closer to the wall."""
    center = origin - inward * dist
    half = depth * 0.5
    return center + inward * half, center - inward * half


def _shop_wrap(g, cols, profile, hx, hz, front_z, side_x, span_x, span_z, lod):
    """Cornice, sign-band moulding, and awning frames meet at the corners on a mitre."""
    shops = {
        "front": _shop_style(profile["front"]),
        "back": _shop_style(profile["back"]),
        "right": _shop_style(profile["right"]),
        "left": _shop_style(profile["left"]),
    }
    faces = {
        "front": ("z", front_z, -1.0, span_x),
        "back": ("z", -front_z, 1.0, span_x),
        "right": ("x", side_x, -1.0, span_z),
        "left": ("x", -side_x, 1.0, span_z),
    }
    # (y, height, street dist, depth, material)
    layers = (
        (3.52, 0.08, 0.17, 0.08, "Lib_Brick", "Col_String", (0.04, 0.03)),
        (3.68, 0.16, 0.28, 0.18, "Lib_Concrete", "Col_Cornice", (0.08, 0.08)),
        (3.80, 0.05, 0.22, 0.10, profile["trim"], None, None),
        (3.40, 0.050, 0.245, 0.046, profile["trim"], None, None),
        (2.58, 0.044, 0.245, 0.042, profile["trim"], None, None),
    )
    if lod >= 2:
        layers = layers[:2]
    for layer in layers:
        _wrap_layer(g, cols, faces, shops, layer)
    # Each awning ends on its own pier bracket. No steel past the cloth.


def _neighbor_end(face_name, end):
    """The face and the corner signs at one end of a wall. end is +1 or -1 along the face."""
    if face_name == "front":
        return ("right" if end > 0 else "left", end, 1.0)
    if face_name == "back":
        return ("right" if end > 0 else "left", end, -1.0)
    if face_name == "right":
        return ("front" if end > 0 else "back", 1.0, end)
    return ("front" if end > 0 else "back", -1.0, end)


def _wrap_layer(g, cols, faces, shops, layer):
    y, height, dist, depth, mat, col_name, col_inset = layer
    y0, y1 = y - height * 0.5, y + height * 0.5
    cuts = {}
    for name, (axis, origin, inward, span) in faces.items():
        inner, outer = _outward_span(origin, inward, dist, depth)
        cuts[name] = (inner, outer, axis, origin, inward, span)
    for name, (_axis, _origin, _inward, _span) in faces.items():
        if not shops[name]:
            continue
        a0, a1 = _run_ends(name, cuts)
        if a1 - a0 < 0.30:
            continue
        axis, origin, inward, _span = faces[name]
        mid = (a0 + a1) * 0.5
        rw = a1 - a0
        center_n = _street(origin, inward, dist)
        _c, _s = _box_ax(g, axis, mid, y, center_n, rw, height, depth, mat)
        if col_name and col_inset is not None:
            sy, sn = col_inset
            if axis == "z":
                _col_box(cols, col_name, _c, (max(0.20, rw - 0.40), sy, sn))
            else:
                _col_box(cols, col_name, _c, (sn, sy, max(0.20, rw - 0.40)))
    seen = set()
    for name in faces:
        if not shops[name]:
            continue
        for end in (-1.0, 1.0):
            other, sx, sz = _neighbor_end(name, end)
            key = (round(sx, 0), round(sz, 0))
            if key in seen:
                continue
            if not shops[name] and not shops[other]:
                continue
            seen.add(key)
            _miter_corner(g, cuts, name, other, sx, sz, y0, y1, mat)
            if not shops[other]:
                _short_return(g, cuts, other, sx, sz, y, height, depth, mat)


def _run_ends(name, cuts):
    """Along-axis limits, stopped just short of each corner mitre."""
    pos_inner = cuts[_neighbor_end(name, 1.0)[0]][0]
    neg_inner = cuts[_neighbor_end(name, -1.0)[0]][0]
    return neg_inner + 0.012, pos_inner - 0.012


def _miter_corner(g, cuts, face_a, face_b, sx, sz, y0, y1, mat):
    """Two triangular prisms split on the diagonal, with a gap along the mitre."""
    # face_a is the Z wall when we came from front/back first, but either order can arrive.
    z_face = face_a if face_a in ("front", "back") else face_b
    x_face = face_b if face_b in ("right", "left") else face_a
    z0, z1 = cuts[z_face][0], cuts[z_face][1]
    x0, x1 = cuts[x_face][0], cuts[x_face][1]
    # Flip into the positive corner, build, flip back.
    def pos(x, z):
        return (abs(x), abs(z))

    def world(poly):
        return [(sx * abs(x) if False else (x if sx > 0 else -x), z if sz > 0 else -z) for x, z in poly]

    ax0, ax1 = sorted((abs(x0), abs(x1)))
    az0, az1 = sorted((abs(z0), abs(z1)))
    poly_f = (
        (ax0 + 0.010, az1 - 0.004),
        (ax1 - 0.014, az1 - 0.004),
        (ax0 + 0.010, az0 + 0.014),
    )
    poly_s = (
        (ax1 - 0.004, az0 + 0.010),
        (ax1 - 0.004, az1 - 0.014),
        (ax0 + 0.014, az0 + 0.010),
    )
    _extrude(g, world(poly_f), y0, y1, mat)
    _extrude(g, world(poly_s), y0, y1, mat)


def _short_return(g, cuts, plain, sx, sz, y, height, depth, mat):
    """A short run of the same band turning onto a wall that has no shopfront."""
    inner, outer, _axis, _origin, _inward, _span = cuts[plain]
    center_n = (inner + outer) * 0.5
    # Stop 12 mm before the mitre and run 0.62 m away from the corner.
    if plain in ("right", "left"):
        z_cut = cuts["front" if sz > 0 else "back"][0]
        end = z_cut - sz * 0.012
        far = end - sz * 0.62
        a0, a1 = (far, end) if far < end else (end, far)
        if a1 - a0 < 0.20:
            return
        _box_ax(g, "x", (a0 + a1) * 0.5, y, center_n, a1 - a0, height, depth, mat)
    else:
        x_cut = cuts["right" if sx > 0 else "left"][0]
        end = x_cut - sx * 0.012
        far = end - sx * 0.62
        a0, a1 = (far, end) if far < end else (end, far)
        if a1 - a0 < 0.20:
            return
        _box_ax(g, "z", (a0 + a1) * 0.5, y, center_n, a1 - a0, height, depth, mat)


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
            _quoins(g, hx, hz, wall_h, profile["body"], lod, profile.get("quoin_pitch", 0.40))
            # Cornice, sign moulding, and awning frames mitre around the corners.
            _shop_wrap(g, cols, profile, hx, hz, front_z, side_x, span_x, span_z, lod)
            for axis, origin, inward, span, face in (
                ("z", front_z, -1.0, span_x, profile["front"]),
                ("x", side_x, -1.0, span_z, profile["right"]),
                ("x", -side_x, 1.0, span_z, profile["left"]),
            ):
                style = _shop_style(face)
                if style:
                    _dress_storefront(g, cols, axis, origin, inward, span, profile, style, lod)
            if lod == 0:
                a._cols = cols
            a.end()
        _seat_cols(a)
        _apply_cols(a)
        return a
    return create


def _box_inside(bvh, center, size):
    """True when every validator sample, stepped inward, is inside the mesh."""
    col = {"type": "box", "center": list(center), "size": list(size)}
    origin = Vector(center)
    for p in _collider_samples(col):
        inward = Vector(p) + (origin - Vector(p)).normalized() * 0.008
        if not _point_inside(bvh, Vector(unity_to_blender(inward.x, inward.y, inward.z))):
            return False
    return True


def _seat_cols(asset):
    """Pull a shop box in until its samples clear the sill graze. Drop a climb box that cannot."""
    bvh = BVHTree.FromBMesh(asset.lods[0].bm)
    _checked = _failed = _fixed = _dropped = 0
    seated = []
    for item in getattr(asset, "_cols", []):
        if item[0] == "cap":
            seated.append(item)
            continue
        if item[0] == "box":
            name, center, size = item[1], list(item[2]), [float(v) for v in item[3]]
            packed = "box"
        else:
            name, center, size = item[0], list(item[1]), [float(v) for v in item[2]]
            packed = "named"
        _checked += 1
        if _box_inside(bvh, center, size):
            seated.append(item)
            continue
        _failed += 1
        sx, sy, sz = size
        along = 0 if sx >= sz else 2
        thick = min((0, 1, 2), key=lambda i: size[i])
        trials = []
        for cut in (0.04, 0.08, 0.12, 0.18):
            s = [sx, max(0.08, sy - cut), sz]
            trials.append((center, s))
        for shift in (0.035, 0.07, -0.03):
            trials.append(([center[0], center[1] + shift, center[2]], [sx, max(0.08, sy - 0.06), sz]))
        for shift in (0.03, -0.03, 0.06, -0.06):
            c = [center[0], center[1], center[2]]
            c[along] = c[along] + shift
            s = [sx, max(0.08, sy - 0.05), sz]
            s[along] = max(0.10, s[along] - abs(shift))
            trials.append((c, s))
        for cut in (0.03, 0.05):
            s = [sx, max(0.08, sy - 0.06), sz]
            s[thick] = max(0.06, s[thick] - cut)
            trials.append((center, s))
        for scale in (0.72, 0.55, 0.40):
            trials.append((center, [max(0.05, v * scale) for v in size]))
        found = None
        for c, s in trials:
            if min(s) < 0.05:
                continue
            if _box_inside(bvh, c, s):
                found = (c, s)
                break
        if found:
            _fixed += 1
            if packed == "box":
                seated.append(("box", name, found[0], found[1]))
            else:
                seated.append((name, found[0], found[1]))
        elif str(name).startswith("Climb") or str(name).startswith("Col_Shelf"):
            _dropped += 1
        else:
            seated.append(item)
    asset._cols = seated


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


def _house_trim(g, cols, profile, hx, hz, shift, wall_h, porch, lod):
    """Front steps, eave gutters, and a chimney seated above the ridge."""
    deck_depth = porch - 0.05
    deck_z = shift + hz + porch * 0.5
    front = deck_z + deck_depth * 0.5
    g.box((0, 0.20, front + 0.16), (1.20, 0.10, 0.30), "Lib_Concrete")
    cols.append(("box", "Col_StepHigh", (0, 0.20, front + 0.16), (1.08, 0.07, 0.22)))
    g.box((0, 0.10, front + 0.46), (1.32, 0.10, 0.30), "Lib_Concrete")
    cols.append(("box", "Col_StepLow", (0, 0.10, front + 0.46), (1.18, 0.07, 0.22)))
    if lod < 2:
        porch_len = porch - 0.25
        porch_cz = shift + hz + 0.2 + porch_len * 0.5
        porch_front = porch_cz + porch_len * 0.5
        g.pipe((-hx * 0.34, 2.64, porch_front + 0.09), (hx * 0.34, 2.64, porch_front + 0.09), 0.038, "Lib_SteelDark", 6)
        z_back = shift - hz - 0.15 - 0.09
        g.pipe((-hx * 0.92, wall_h + 0.08, z_back), (hx * 0.92, wall_h + 0.08, z_back), 0.038, "Lib_SteelDark", 6)
        z_wall = shift - hz + 0.28
        for x in (-hx + 0.32, hx - 0.32):
            g.pipe((x, wall_h - 0.04, z_wall), (x, 0.05, z_wall), 0.03, "Lib_SteelDark", 6)
        if profile["roof"] == "hip":
            y = wall_h + 0.04
            z0 = shift - hz + 0.2
            z1 = shift + hz - 0.25
            g.pipe((hx + 0.28, y, z0), (hx + 0.28, y, z1), 0.038, "Lib_SteelDark", 6)
            g.pipe((-hx - 0.28, y, z0), (-hx - 0.28, y, z1), 0.038, "Lib_SteelDark", 6)
    ridge_y = wall_h + profile["rise"]
    cx = hx * 0.28
    base = ridge_y + 0.18
    g.box((cx, base + 0.48, shift), (0.58, 0.96, 0.18), "Lib_Brick")
    g.box((cx, base + 1.02, shift), (0.72, 0.08, 0.28), "Lib_Concrete")
    if lod == 0:
        g.box((cx, base + 1.16, shift), (0.22, 0.18, 0.16), "Lib_Brick")
    cols.append(("box", "Col_Chimney", (cx, base + 0.48, shift), (0.46, 0.78, 0.12)))


def build_house(profile):
    def create():
        sx = profile["sx"]
        body_z = profile["sz"]
        porch = profile["porch"]
        wall_h = profile["wall_h"]
        step_run = 0.64
        # Shift so the foundation, porch, and front steps are centered on Z.
        shift = -(porch + step_run) * 0.5
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
                _fit_holes(span_x, wall_h, "house_back"),
                profile["body"], profile["trim"], lod, "Climb_Back",
            )
            _wall(
                g, cols, "x", hx - thick * 0.5, -1.0, span_z, 0.0, wall_h,
                _fit_holes(span_z, wall_h, "house_side"),
                profile["body"], profile["trim"], lod, "Climb_Right",
            )
            _wall(
                g, cols, "x", -(hx - thick * 0.5), 1.0, span_z, 0.0, wall_h,
                _fit_holes(span_z, wall_h, "house_side"),
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
                _hip(g, hx + 0.15, hz + 0.12, wall_h, profile["rise"], shift)
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
            _house_trim(g, cols, profile, hx, hz, shift, wall_h, porch, lod)
            if lod == 0:
                _interior(g, hx, hz, front)
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
