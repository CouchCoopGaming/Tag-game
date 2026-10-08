"""Small wood harbor shed. Door and window face +Z. A window is on +X.

The roof bears on a top plate. A soffit closes the eave, and corner trim
covers the joint between the side walls and the front and back walls.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register

W = 3.6
D = 2.8
# Wall boxes are centered at 1.22 with height 2.15, so the top is 2.295.
WALL_TOP = 2.295
PLATE_BOTTOM = WALL_TOP + 0.004
PLATE_TOP = PLATE_BOTTOM + 0.055
# Roof underside sits 4 mm above the plate. The soffit board fills the overhang.
SOFFIT = PLATE_TOP + 0.004
YE = SOFFIT + 0.08
YR = YE + 0.68


def _soffit_y(z):
    ez = D * 0.5 + 0.22
    ins = 0.06
    z_e = ez - ins
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
    # Fascia past the eave edge, covering the soffit lip.
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
    """Fill the triangle above each side plate, 4 mm under the roof soffit."""
    zs = [-1.22, -0.61, 0.0, 0.61, 1.22]
    y_bot = PLATE_TOP + 0.004

    def y_top(z):
        return _soffit_y(z) - 0.004

    for sign in (-1.0, 1.0):
        x_c = sign * (W * 0.5 - 0.04)
        _loft(g, x_c - 0.03, x_c + 0.03, zs, y_bot, y_top, "Lib_WoodWeather")


def _plate(g):
    cy = (PLATE_BOTTOM + PLATE_TOP) * 0.5
    h = PLATE_TOP - PLATE_BOTTOM
    # Plates sit on the wall tops and stop short of each other at the corners.
    g.box((0, cy, -D * 0.5 + 0.04), (W - 0.20, h, 0.05), "Lib_Varnish")
    g.box((0.02, cy, D * 0.5 - 0.04), (W - 0.36, h, 0.05), "Lib_Varnish")
    g.box((-W * 0.5 + 0.04, cy, 0), (0.05, h, D - 0.24), "Lib_Varnish")
    g.box((W * 0.5 - 0.04, cy, 0), (0.05, h, D - 0.24), "Lib_Varnish")


def _soffit_boards(g):
    """Underside of the overhang, from just outside the wall to the fascia."""
    ez = D * 0.5 + 0.22
    z_in = D * 0.5 - 0.04 + 0.03 + 0.006
    z_out = ez + 0.028 - 0.014 - 0.006
    cz = (z_in + z_out) * 0.5
    depth = abs(z_out - z_in)
    y = PLATE_TOP - 0.012
    g.box((0, y, cz), (W + 0.20, 0.016, depth), "Lib_Varnish")
    g.box((0, y, -cz), (W + 0.20, 0.016, depth), "Lib_Varnish")


def _corners(g):
    """Trim boards outside the wall faces, covering the open corner."""
    y = 1.18
    height = 2.16
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            x_out = sx * (W * 0.5 - 0.01)
            z_front = sz * (D * 0.5 - 0.01)
            z_trim = sz * (abs(z_front) + 0.024)
            x0 = sx * (W * 0.5 - 0.16)
            x1 = sx * (abs(x_out) + 0.04)
            g.box(((x0 + x1) * 0.5, y, z_trim), (abs(x1 - x0), height, 0.036), "Lib_Varnish")
            x_trim = sx * (abs(x_out) + 0.024)
            z_a = sz * 1.05
            z_b = z_trim - sz * 0.048
            g.box((x_trim, y, (z_a + z_b) * 0.5), (0.036, height, abs(z_b - z_a)), "Lib_Varnish")


def _openings(g, lod):
    """Door and window on +Z, window on +X. All of them stand clear of the walls."""
    z_face = D * 0.5 - 0.01
    # Door frame, then the leaf, then two panels and a handle.
    g.box((0.175, 1.12, z_face + 0.030), (1.16, 2.08, 0.024), "Lib_Varnish")
    g.box((0.175, 1.08, z_face + 0.056), (0.90, 1.86, 0.020), "Lib_SteelDark")
    if lod == 0:
        g.box((0.175, 1.48, z_face + 0.074), (0.64, 0.58, 0.010), "Lib_Varnish")
        g.box((0.175, 0.70, z_face + 0.074), (0.64, 0.58, 0.010), "Lib_Varnish")
        g.box((0.48, 1.05, z_face + 0.086), (0.03, 0.10, 0.012), "Lib_Brass")
    # Front window, on the right-hand wall.
    g.box((1.12, 1.58, z_face + 0.028), (0.78, 0.66, 0.028), "Lib_Varnish")
    g.box((1.12, 1.22, z_face + 0.028), (0.86, 0.045, 0.036), "Lib_Varnish")
    if lod == 0:
        g.box((1.12, 1.60, z_face + 0.050), (0.52, 0.40, 0.012), "Lib_ShopGlass")
    # Side window on +X, so a three-quarter view shows an opening on that face too.
    x_face = W * 0.5 - 0.01
    g.box((x_face + 0.028, 1.55, 0.15), (0.028, 0.70, 0.78), "Lib_Varnish")
    g.box((x_face + 0.028, 1.16, 0.15), (0.036, 0.045, 0.86), "Lib_Varnish")
    if lod == 0:
        g.box((x_face + 0.050, 1.58, 0.15), (0.012, 0.42, 0.52), "Lib_ShopGlass")


@register
def create():
    a = Asset(
        "HarborShed",
        "Harbor",
        "Wood shed, 3.6 x 2.8 m, walls at 2.35 m. The gable sits on a top plate with a closed eave and corner trim. Door and window face +Z. Another window is on +X.",
    )
    a.climbable = True
    a.climb_note = "The board walls are cling. The door is closed."
    a.vault_note = "No rail. Wall top is 2.35 m."
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.04, 0), (W + 0.08, 0.08, D + 0.08), "Lib_Concrete", uv_scale=0.7)
        g.box((0, 1.22, -D * 0.5 + 0.04), (W - 0.16, 2.15, 0.06), "Lib_WoodWeather", uv_scale=1.0)
        g.box((-W * 0.5 + 0.04, 1.22, 0), (0.06, 2.15, D - 0.20), "Lib_WoodWeather", uv_scale=1.0)
        g.box((W * 0.5 - 0.04, 1.22, 0), (0.06, 2.15, D - 0.20), "Lib_WoodWeather", uv_scale=1.0)
        g.box((-0.95, 1.22, D * 0.5 - 0.04), (1.15, 2.15, 0.06), "Lib_WoodWeather", uv_scale=1.0)
        g.box((1.15, 1.22, D * 0.5 - 0.04), (0.85, 2.15, 0.06), "Lib_WoodWeather", uv_scale=1.0)
        g.box((0.175, 2.165, D * 0.5 - 0.04), (1.06, 0.26, 0.06), "Lib_WoodWeather", uv_scale=1.0)
        _plate(g)
        _gables(g)
        _soffit_boards(g)
        _corners(g)
        _openings(g, lod)
        _roof(g)
        a.end()
    a.box("Col_Floor", (0, 0.04, 0), (W - 0.05, 0.05, D - 0.05))
    a.box("Climb_Back", (0, 1.22, -D * 0.5 + 0.04), (W - 0.40, 1.90, 0.04))
    a.box("Climb_SideL", (-W * 0.5 + 0.04, 1.22, 0), (0.04, 1.90, D - 0.40))
    a.box("Climb_SideR", (W * 0.5 - 0.04, 1.22, 0), (0.04, 1.90, D - 0.40))
    a.box("Climb_FrontL", (-0.95, 1.00, D * 0.5 - 0.04), (0.70, 1.30, 0.02))
    a.box("Col_Door", (0.175, 1.08, D * 0.5 + 0.046), (0.76, 1.60, 0.014))
    return a
