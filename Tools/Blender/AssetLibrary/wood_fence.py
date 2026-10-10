"""6 ft dog-ear privacy fence. Bay, gate, corner, and end.

The bay is 8 ft with one post at the origin, so the next module brings the far post.
Pickets face +Z on an X run. Rails sit on the back, clear of the boards.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register

POST = 0.09
POST_H = 1.90
PICKET_H = 1.83
EAR = 0.042
BOARD_T = 0.022
GAP = 0.008
RAIL_T = 0.038
RAIL_H = 0.089
RAIL_YS = (0.40, 0.95, 1.50)
# 4 mm behind the picket, so the rail reads as nailed to the boards.
RAIL_Z = -(BOARD_T * 0.5 + 0.004 + RAIL_T * 0.5)
RAIL_X0 = POST * 0.5 + 0.004


def _profile(c, w, h, ear):
    hw = w * 0.5
    return [
        (c - hw, 0.0),
        (c + hw, 0.0),
        (c + hw, h - ear),
        (c + hw - ear, h),
        (c - hw + ear, h),
        (c - hw, h - ear),
    ]


def _extrude(g, profile, thick0, thick1, axis):
    """Closed dog-ear. axis 'z' extrudes a profile of (x, y). axis 'x' uses (z, y)."""
    n = len(profile)
    if axis == "z":
        verts = [(p[0], p[1], thick0) for p in profile] + [(p[0], p[1], thick1) for p in profile]
    else:
        verts = [(thick0, p[1], p[0]) for p in profile] + [(thick1, p[1], p[0]) for p in profile]
    xs = [p[0] for p in profile]
    span = max(1e-4, max(xs) - min(xs))
    uvs = [((p[0] - min(xs)) / span * 0.14, p[1]) for p in profile]
    uvs = uvs + uvs
    faces = [tuple(range(n, 2 * n)), tuple(reversed(range(n)))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    g.quad(verts, faces, uvs, "Lib_Wood")


def _picket(g, along, face, width, axis):
    half = BOARD_T * 0.5
    prof = _profile(along, width, PICKET_H, EAR)
    if axis == "z":
        _extrude(g, prof, face - half, face + half, "z")
    else:
        _extrude(g, prof, face - half, face + half, "x")


def _layout(a, b, width):
    """Centers and board width for pickets filling [a, b] with a fixed gap."""
    span = b - a
    count = max(1, int(round(span / 0.155)))
    pitch = span / count
    board = pitch - GAP
    centers = [a + (i + 0.5) * pitch for i in range(count)]
    return centers, board


def _post(g, cols, x, z):
    g.box((x, POST_H * 0.5, z), (POST, POST_H, POST), "Lib_WoodDark", uv_scale=1.0)
    g.box((x, POST_H + 0.013, z), (POST + 0.012, 0.018, POST + 0.012), "Lib_SteelDark")
    cols.append(("box", "Col_Post", (x, POST_H * 0.5, z), (POST - 0.024, POST_H - 0.08, POST - 0.024)))


def _rails_x(g, cols, x0, x1, z):
    span = x1 - x0
    if span < 0.12:
        return
    cx = (x0 + x1) * 0.5
    for y in RAIL_YS:
        g.box((cx, y, z), (span, RAIL_H, RAIL_T), "Lib_WoodDark")
        cols.append(("box", "Col_Rail", (cx, y, z), (span - 0.04, RAIL_H - 0.028, 0.020)))


def _rails_z(g, cols, z0, z1, x):
    span = z1 - z0
    if span < 0.12:
        return
    cz = (z0 + z1) * 0.5
    for y in RAIL_YS:
        g.box((x, y, cz), (RAIL_T, RAIL_H, span), "Lib_WoodDark")
        cols.append(("box", "Col_Rail", (x, y, cz), (0.020, RAIL_H - 0.028, span - 0.04)))


def _pickets_x(g, cols, x0, x1, z, lod):
    centers, board = _layout(x0, x1, 0.147)
    if lod == 0:
        for x in centers:
            _picket(g, x, z, board, "z")
            cols.append(("box", "Col_Picket", (x, 0.86, z), (max(0.05, board - 0.046), 1.46, 0.008)))
    else:
        span = (centers[-1] + board * 0.5) - (centers[0] - board * 0.5)
        cx = (centers[0] + centers[-1]) * 0.5
        g.box((cx, PICKET_H * 0.5, z), (span, PICKET_H, BOARD_T), "Lib_Wood", uv_scale=1.0)


def _pickets_z(g, cols, z0, z1, x, lod):
    centers, board = _layout(z0, z1, 0.147)
    if lod == 0:
        for z in centers:
            _picket(g, z, x, board, "x")
            cols.append(("box", "Col_Picket", (x, 0.86, z), (0.008, 1.46, max(0.05, board - 0.046))))
    else:
        span = (centers[-1] + board * 0.5) - (centers[0] - board * 0.5)
        cz = (centers[0] + centers[-1]) * 0.5
        g.box((x, PICKET_H * 0.5, cz), (BOARD_T, PICKET_H, span), "Lib_Wood", uv_scale=1.0)


def _finish(a, cols):
    from _volume import _apply_cols
    a._cols = cols
    _apply_cols(a)


def _blurb_bay():
    return (
        "Privacy fence bay, 2.44 m (8 ft) long, dog-ear pickets 1.83 m (6 ft). "
        "One post at the origin; the next module brings the far post. "
        "Three rails sit on the back. Gaps are about 8 mm."
    )


@register
def create():
    a = Asset("WoodFence", "Buildings", _blurb_bay())
    a.loose_pivot = True
    a.climbable = True
    a.vaultable = False
    a.climb_note = "Each picket is solid. The gaps are about 8 mm and are not a passage."
    a.vault_note = "Top is 1.83 m. Too high to vault from the ground."
    x0, x1 = 0.06, 2.38
    for lod in (0, 1):
        g = a.begin(lod)
        cols = []
        _post(g, cols, 0.0, 0.0)
        _pickets_x(g, cols, x0, x1, 0.0, lod)
        _rails_x(g, cols, RAIL_X0, 2.36, RAIL_Z)
        if lod == 0:
            a._pending = cols
        a.end()
    _finish(a, a._pending)
    return a


@register
def create_gate():
    a = Asset(
        "WoodFence_Gate",
        "Buildings",
        "Privacy gate, posts 1.22 m apart, closed dog-ear leaf. "
        "Rails and a diagonal brace are on the back. Hinge on the origin post, latch on the far post.",
    )
    a.loose_pivot = True
    a.climbable = True
    a.vaultable = False
    a.climb_note = "The closed leaf is solid. The gaps are about 8 mm."
    a.vault_note = "Top is 1.83 m. Too high to vault from the ground."
    for lod in (0, 1):
        g = a.begin(lod)
        cols = []
        _post(g, cols, 0.0, 0.0)
        _post(g, cols, 1.22, 0.0)
        _pickets_x(g, cols, 0.08, 1.14, 0.0, lod)
        _rails_x(g, cols, RAIL_X0, 1.22 - RAIL_X0, RAIL_Z)
        # Brace behind the rails, from the hinge side up to the latch side.
        x0, x1 = 0.16, 1.06
        y0, y1 = 0.32, 1.58
        length = math.hypot(x1 - x0, y1 - y0)
        angle = math.degrees(math.atan2(y1 - y0, x1 - x0))
        cz = RAIL_Z - RAIL_T * 0.5 - 0.004 - 0.014
        g.box(((x0 + x1) * 0.5, (y0 + y1) * 0.5, cz), (length, 0.040, 0.028), "Lib_WoodDark", euler=(0.0, 0.0, angle))
        if lod == 0:
            g.box((0.062, 1.15, 0.03), (0.016, 0.10, 0.028), "Lib_SteelDark")
            g.box((1.158, 1.05, 0.03), (0.016, 0.06, 0.024), "Lib_Steel")
            a._pending = cols
            a._brace = (((x0 + x1) * 0.5, (y0 + y1) * 0.5, cz), length, angle)
        a.end()
    _finish(a, a._pending)
    center, length, angle = a._brace
    a.box("Col_Brace", center, (length - 0.24, 0.022, 0.014), euler=(0.0, 0.0, angle))
    return a


@register
def create_corner():
    a = Asset(
        "WoodFence_Corner",
        "Buildings",
        "Privacy corner. One post, a 1.22 m run on +X and a 1.22 m run on +Z. "
        "Rails sit on the inside of the turn.",
    )
    a.loose_pivot = True
    a.climbable = True
    a.vaultable = False
    a.climb_note = "Each picket is solid. The gaps are about 8 mm and are not a passage."
    a.vault_note = "Top is 1.83 m. Too high to vault from the ground."
    for lod in (0, 1):
        g = a.begin(lod)
        cols = []
        _post(g, cols, 0.0, 0.0)
        _pickets_x(g, cols, 0.08, 1.16, 0.0, lod)
        _pickets_z(g, cols, 0.08, 1.16, 0.0, lod)
        _rails_x(g, cols, RAIL_X0, 1.14, RAIL_Z)
        _rails_z(g, cols, RAIL_X0, 1.14, RAIL_Z)
        if lod == 0:
            a._pending = cols
        a.end()
    _finish(a, a._pending)
    return a


@register
def create_end():
    a = Asset(
        "WoodFence_End",
        "Buildings",
        "Privacy end. One post and a short dog-ear run, about 0.62 m, for a termination.",
    )
    a.loose_pivot = True
    a.climbable = True
    a.vaultable = False
    a.climb_note = "Each picket is solid. The gaps are about 8 mm and are not a passage."
    a.vault_note = "Top is 1.83 m. Too high to vault from the ground."
    for lod in (0, 1):
        g = a.begin(lod)
        cols = []
        _post(g, cols, 0.0, 0.0)
        _pickets_x(g, cols, 0.08, 0.62, 0.0, lod)
        _rails_x(g, cols, RAIL_X0, 0.60, RAIL_Z)
        if lod == 0:
            a._pending = cols
        a.end()
    _finish(a, a._pending)
    return a
