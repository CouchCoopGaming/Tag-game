"""Small wood harbor shed. Door and window face +Z. A window is on +X.

Siding is modeled board-and-batten. Corner trim overlaps the boards.
The door is a frame with two recessed panels. Window trim sits on the
siding with a projecting sill. The roof still bears on the top plate.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register

W = 3.6
D = 2.8
WALL_TOP = 2.295
PLATE_BOTTOM = WALL_TOP + 0.004
PLATE_TOP = PLATE_BOTTOM + 0.055
SOFFIT = PLATE_TOP + 0.004
YE = SOFFIT + 0.08
YR = YE + 0.68

FACE_Z = 1.39
FACE_X = 1.79
BOARD_OUTER_Z = FACE_Z + 0.013
BOARD_OUTER_X = FACE_X + 0.013


def _soffit_y(z):
    ez = D * 0.5 + 0.22
    z_e = ez - 0.06
    t = min(abs(z) / z_e, 1.0)
    y_ridge = YR - 0.10
    y_eave = YE - 0.08
    return y_ridge + (y_eave - y_ridge) * t


def _roof(g):
    """Closed gable. The inner eave is SOFFIT, 4 mm above the top plate."""
    ox = W * 0.5 + 0.18
    ez = D * 0.5 + 0.22
    ye = YE
    yr = YR
    ins = 0.06
    verts = [
        (-ox, yr, 0.0),
        (ox, yr, 0.0),
        (-ox, ye, ez),
        (ox, ye, ez),
        (-ox, ye, -ez),
        (ox, ye, -ez),
        (-ox + ins, yr - 0.10, 0.0),
        (ox - ins, yr - 0.10, 0.0),
        (-ox + ins, ye - 0.08, ez - ins),
        (ox - ins, ye - 0.08, ez - ins),
        (-ox + ins, ye - 0.08, -ez + ins),
        (ox - ins, ye - 0.08, -ez + ins),
    ]
    faces = [
        (0, 2, 3, 1),
        (0, 1, 5, 4),
        (0, 4, 2),
        (1, 3, 5),
        (6, 7, 9, 8),
        (6, 10, 11, 7),
        (6, 8, 10),
        (7, 11, 9),
        (0, 2, 8, 6),
        (0, 6, 10, 4),
        (1, 7, 9, 3),
        (1, 5, 11, 7),
        (2, 3, 9, 8),
        (4, 10, 11, 5),
    ]
    g.mesh(verts, faces, "Lib_Roof", uv_scale=0.9)
    g.box((0, ye - 0.04, ez + 0.028), (ox * 2 - 0.04, 0.16, 0.028), "Lib_Varnish")
    g.box((0, ye - 0.04, -(ez + 0.028)), (ox * 2 - 0.04, 0.16, 0.028), "Lib_Varnish")


def _loft(g, x0, x1, zs, y_bot, y_at, mat):
    verts = []
    rings = []
    for z in zs:
        yt = y_at(z)
        ring = []
        for x, y in ((x0, y_bot), (x1, y_bot), (x1, yt), (x0, yt)):
            ring.append(len(verts))
            verts.append((x, y, z))
        rings.append(ring)
    faces = []
    for s in range(len(rings) - 1):
        a = rings[s]
        b = rings[s + 1]
        for i in range(4):
            j = (i + 1) % 4
            faces.append((a[i], b[i], b[j], a[j]))
    faces.append(tuple(rings[0]))
    faces.append(tuple(reversed(rings[-1])))
    g.mesh(verts, faces, mat, uv_scale=1.0)


def _gables(g):
    """Fill the triangle above each side plate, then a few battens on that face."""
    zs = [-1.22, -0.61, 0.0, 0.61, 1.22]
    y_bot = PLATE_TOP + 0.004

    def y_top(z):
        return _soffit_y(z) - 0.004

    for sign in (-1.0, 1.0):
        x_c = sign * (W * 0.5 - 0.04)
        _loft(g, x_c - 0.03, x_c + 0.03, zs, y_bot, y_top, "Lib_Board")
        for z in (-0.55, 0.0, 0.55):
            top = _soffit_y(z) - 0.04
            bot = PLATE_TOP + 0.02
            if top - bot < 0.15:
                continue
            x = sign * (FACE_X - 0.002 + 0.006)
            g.box((x, (bot + top) * 0.5, z), (0.012, top - bot, 0.032), "Lib_Batten")


def _plate(g):
    cy = (PLATE_BOTTOM + PLATE_TOP) * 0.5
    h = PLATE_TOP - PLATE_BOTTOM
    g.box((0, cy, -D * 0.5 + 0.04), (W - 0.20, h, 0.05), "Lib_Varnish")
    g.box((0.02, cy, D * 0.5 - 0.04), (W - 0.36, h, 0.05), "Lib_Varnish")
    g.box((-W * 0.5 + 0.04, cy, 0), (0.05, h, D - 0.24), "Lib_Varnish")
    g.box((W * 0.5 - 0.04, cy, 0), (0.05, h, D - 0.24), "Lib_Varnish")


def _soffit_boards(g):
    ez = D * 0.5 + 0.22
    z_in = D * 0.5 - 0.04 + 0.03 + 0.006
    z_out = ez + 0.028 - 0.014 - 0.006
    cz = (z_in + z_out) * 0.5
    depth = abs(z_out - z_in)
    y = PLATE_TOP - 0.012
    g.box((0, y, cz), (W + 0.20, 0.016, depth), "Lib_Varnish")
    g.box((0, y, -cz), (W + 0.20, 0.016, depth), "Lib_Varnish")


def _clad(g, axis, face, sign, u0, u1, y0, y1):
    """Vertical boards on an outside face, battens over the joints."""
    if y1 - y0 < 0.08 or u1 - u0 < 0.08:
        return
    n = max(1, int(round((u1 - u0) / 0.15)))
    step = (u1 - u0) / n
    bw = min(0.132, step - 0.010)
    thick = 0.016
    cy = (y0 + y1) * 0.5
    hy = y1 - y0
    inner = face - sign * 0.003
    c_n = inner + sign * thick * 0.5
    batten_n = c_n + sign * (thick * 0.5 - 0.003 + 0.006)
    for i in range(n):
        u = u0 + (i + 0.5) * step
        if axis == "z":
            g.box((u, cy, c_n), (bw, hy, thick), "Lib_Board")
        else:
            g.box((c_n, cy, u), (thick, hy, bw), "Lib_Board")
    for i in range(n - 1):
        u = u0 + (i + 1) * step
        if axis == "z":
            g.box((u, cy, batten_n), (0.034, hy, 0.012), "Lib_Batten")
        else:
            g.box((batten_n, cy, u), (0.012, hy, 0.034), "Lib_Batten")


def _siding(g):
    # Front. The door and the window are openings in the boards.
    _clad(g, "z", FACE_Z, 1.0, -1.76, -0.50, 0.10, 2.26)
    _clad(g, "z", FACE_Z, 1.0, -0.50, 0.96, 2.10, 2.26)
    _clad(g, "z", FACE_Z, 1.0, 0.96, 1.76, 0.10, 1.30)
    _clad(g, "z", FACE_Z, 1.0, 1.46, 1.76, 1.24, 2.26)
    _clad(g, "z", FACE_Z, 1.0, 0.96, 1.54, 1.84, 2.26)
    # Back.
    _clad(g, "z", -FACE_Z, -1.0, -1.76, 1.76, 0.10, 2.26)
    # Sides. +X keeps a window.
    _clad(g, "x", -FACE_X, -1.0, -1.36, 1.36, 0.10, 2.26)
    _clad(g, "x", FACE_X, 1.0, -1.36, 1.36, 0.10, 1.30)
    _clad(g, "x", FACE_X, 1.0, -1.36, -0.16, 1.22, 2.26)
    _clad(g, "x", FACE_X, 1.0, 0.36, 1.36, 1.22, 2.26)
    _clad(g, "x", FACE_X, 1.0, -0.28, 0.48, 1.80, 2.26)


def _corners(g):
    """L-trim overlapping the board faces, and each other, at every corner."""
    y0, y1 = 0.10, 2.26
    cy = (y0 + y1) * 0.5
    h = y1 - y0
    trim_t = 0.024
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            z_outer = sz * BOARD_OUTER_Z
            z_inner = z_outer - sz * 0.004
            z_c = z_inner + sz * trim_t * 0.5
            x_in = sx * (BOARD_OUTER_X - 0.18)
            x_out = sx * (BOARD_OUTER_X + 0.014)
            g.box(((x_in + x_out) * 0.5, cy, z_c), (abs(x_out - x_in), h, trim_t), "Lib_Varnish")
            x_outer = sx * BOARD_OUTER_X
            x_inner = x_outer - sx * 0.004
            x_c = x_inner + sx * trim_t * 0.5
            z_a = sz * (BOARD_OUTER_Z - 0.22)
            z_b = z_inner + sz * 0.010
            g.box((x_c, cy, (z_a + z_b) * 0.5), (trim_t, h, abs(z_b - z_a)), "Lib_Varnish")


def _extrude_x(g, x0, x1, profile):
    """Closed chamfer. profile is four (y, z) corners, extruded along X."""
    verts = []
    for x in (x0, x1):
        for y, z in profile:
            verts.append((x, y, z))
    faces = [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    g.mesh(verts, faces, "Lib_Varnish")


def _extrude_y(g, y0, y1, profile):
    """Closed chamfer. profile is four (x, z) corners, extruded along Y."""
    verts = []
    for y in (y0, y1):
        for x, z in profile:
            verts.append((x, y, z))
    faces = [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    g.mesh(verts, faces, "Lib_Varnish")


def _panel_bevel(g, x0, x1, y0, y1, z_front, z_panel):
    """Butt-joined chamfer around a recess. Rails run full width; stiles stop at them."""
    inset = 0.014
    t = 0.005
    _extrude_x(g, x0, x1, (
        (y0, z_front),
        (y0 + t, z_front),
        (y0 + inset, z_panel),
        (y0 + inset - t, z_panel),
    ))
    _extrude_x(g, x0, x1, (
        (y1, z_front),
        (y1 - t, z_front),
        (y1 - inset, z_panel),
        (y1 - inset + t, z_panel),
    ))
    _extrude_y(g, y0 + inset, y1 - inset, (
        (x0, z_front),
        (x0 + t, z_front),
        (x0 + inset, z_panel),
        (x0 + inset - t, z_panel),
    ))
    _extrude_y(g, y0 + inset, y1 - inset, (
        (x1, z_front),
        (x1 - t, z_front),
        (x1 - inset, z_panel),
        (x1 - inset + t, z_panel),
    ))


def _door(g, lod):
    """Butt-jointed stiles and rails, one wood color, beveled panel recess."""
    z0 = BOARD_OUTER_Z - 0.006
    thick = 0.038
    zc = z0 + thick * 0.5
    z_front = z0 + thick
    # Stiles, full height. Inner faces are x = -0.32 and x = 0.78.
    g.box((-0.40, 1.10, zc), (0.16, 2.06, thick), "Lib_Varnish")
    g.box((0.86, 1.10, zc), (0.16, 2.06, thick), "Lib_Varnish")
    # Rails butt into the stiles, 3 mm of bury, not a block through the corner.
    rail_w = 1.106
    g.box((0.23, 0.20, zc), (rail_w, 0.28, thick), "Lib_Varnish")
    g.box((0.23, 1.08, zc), (rail_w, 0.16, thick), "Lib_Varnish")
    g.box((0.23, 1.98, zc), (rail_w, 0.28, thick), "Lib_Varnish")
    # Same varnish, set back about 2 cm. Thick enough to hold Col_Door.
    panel_zc = 1.404
    panel_t = 0.022
    g.box((0.23, 0.67, panel_zc), (1.12, 0.70, panel_t), "Lib_Varnish")
    g.box((0.23, 1.50, panel_zc), (1.12, 0.72, panel_t), "Lib_Varnish")
    if lod == 0:
        z_panel = panel_zc + panel_t * 0.5
        _panel_bevel(g, -0.32, 0.78, 0.34, 1.00, z_front, z_panel)
        _panel_bevel(g, -0.32, 0.78, 1.16, 1.84, z_front, z_panel)
        g.box((0.86, 1.05, z_front + 0.004), (0.045, 0.07, 0.012), "Lib_Brass")
        g.sphere((0.86, 1.05, z_front + 0.022), 0.022, "Lib_Brass", 10)


def _window_z(g, cx, cy, lod):
    """Trim overlapping the front boards, sill proud of the trim."""
    thick = 0.020
    zc = BOARD_OUTER_Z - 0.004 + thick * 0.5
    g.box((cx - 0.30, cy, zc), (0.06, 0.62, thick), "Lib_Varnish")
    g.box((cx + 0.30, cy, zc), (0.06, 0.62, thick), "Lib_Varnish")
    g.box((cx, cy + 0.28, zc), (0.66, 0.06, thick), "Lib_Varnish")
    g.box((cx, cy - 0.34, BOARD_OUTER_Z + 0.010), (0.78, 0.045, 0.055), "Lib_Varnish")
    if lod == 0:
        g.box((cx, cy + 0.02, BOARD_OUTER_Z - 0.006), (0.48, 0.42, 0.008), "Lib_ShopGlass")


def _window_x(g, cz, cy, lod):
    thick = 0.020
    xc = BOARD_OUTER_X - 0.004 + thick * 0.5
    g.box((xc, cy, cz - 0.30), (thick, 0.62, 0.06), "Lib_Varnish")
    g.box((xc, cy, cz + 0.30), (thick, 0.62, 0.06), "Lib_Varnish")
    g.box((xc, cy + 0.28, cz), (thick, 0.06, 0.66), "Lib_Varnish")
    g.box((BOARD_OUTER_X + 0.010, cy - 0.34, cz), (0.055, 0.045, 0.78), "Lib_Varnish")
    if lod == 0:
        g.box((BOARD_OUTER_X - 0.006, cy + 0.02, cz), (0.008, 0.42, 0.48), "Lib_ShopGlass")


@register
def create():
    a = Asset(
        "HarborShed",
        "Harbor",
        "Wood shed, 3.6 x 2.8 m. Board-and-batten siding, corner trim on the boards, "
        "a butt-jointed frame-and-panel door with a beveled recess, and trimmed windows.",
    )
    a.climbable = True
    a.climb_note = "The board walls are cling. The door is closed."
    a.vault_note = "No rail. Wall top is 2.35 m."
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.04, 0), (W + 0.08, 0.08, D + 0.08), "Lib_Concrete", uv_scale=0.7)
        g.box((0, 1.22, -D * 0.5 + 0.04), (W - 0.16, 2.15, 0.06), "Lib_Board")
        g.box((-W * 0.5 + 0.04, 1.22, 0), (0.06, 2.15, D - 0.20), "Lib_Board")
        g.box((W * 0.5 - 0.04, 1.22, 0), (0.06, 2.15, D - 0.20), "Lib_Board")
        g.box((-0.95, 1.22, D * 0.5 - 0.04), (1.15, 2.15, 0.06), "Lib_Board")
        g.box((1.15, 1.22, D * 0.5 - 0.04), (0.85, 2.15, 0.06), "Lib_Board")
        g.box((0.175, 2.165, D * 0.5 - 0.04), (1.06, 0.26, 0.06), "Lib_Board")
        _siding(g)
        _corners(g)
        _door(g, lod)
        _window_z(g, 1.18, 1.58, lod)
        _window_x(g, 0.10, 1.55, lod)
        _plate(g)
        _gables(g)
        _soffit_boards(g)
        _roof(g)
        a.end()
    a.box("Col_Floor", (0, 0.04, 0), (3.40, 0.04, 2.50))
    a.box("Climb_Back", (0, 1.22, -D * 0.5 + 0.04), (W - 0.40, 1.90, 0.04))
    a.box("Climb_SideL", (-W * 0.5 + 0.04, 1.22, 0), (0.04, 1.90, D - 0.40))
    a.box("Climb_SideR", (W * 0.5 - 0.04, 1.22, 0), (0.04, 1.90, D - 0.40))
    a.box("Climb_FrontL", (-0.95, 1.00, D * 0.5 - 0.04), (0.70, 1.30, 0.02))
    # Inside the lower door panel, clear of the frame overlap.
    a.box("Col_Door", (0.23, 0.66, BOARD_OUTER_Z + 0.001), (0.70, 0.36, 0.008))
    return a
