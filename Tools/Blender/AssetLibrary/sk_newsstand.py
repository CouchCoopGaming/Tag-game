"""Sidewalk newsstand. Open service window, wire racks, pitched roof.

1.80 m wide, 1.20 m deep. The counter top is 1.12 m. The street face is +Z.
The roof rises toward the street so a fold-up shutter can sit under the eave.
Magazines are blank blocks. No masthead and no prices.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import sweep_tube

W = 1.80
D = 1.20
OX = W * 0.5
OZ = D * 0.5
T = 0.06
# Roof soffit. Higher at +Z. The shutter tip stays under this line.
ZB = -0.68
ZF = 0.80
ZD = 0.748
SOFFIT_B = 2.100
SLOPE = 0.1056
ROOF_T = 0.042
ROOF_X = 0.98
# Panels stop this far short of a neighbour so faces are not coplanar.
GAP = 0.006
WIRE_X = 0.952
WIRE_R = 0.005

# y, z, height, width along the wall, thickness, material, tilt degrees.
MAGS = (
    (0.58, -0.18, 0.26, 0.20, 0.008, "Lib_PaintRed", 12.0),
    (0.54, 0.18, 0.22, 0.16, 0.006, "Lib_PaintCream", 16.0),
    (1.08, -0.16, 0.30, 0.22, 0.010, "Lib_BoxBlue", 14.0),
    (1.04, 0.20, 0.24, 0.18, 0.007, "Lib_PaintWhite", 11.0),
    (1.54, -0.14, 0.24, 0.18, 0.008, "Lib_PaintTeal", 15.0),
    (1.50, 0.18, 0.20, 0.15, 0.006, "Lib_Orange", 13.0),
)

_BOX = (
    (0, 1, 2, 3),
    (4, 7, 6, 5),
    (0, 3, 7, 4),
    (1, 5, 6, 2),
    (0, 4, 5, 1),
    (3, 2, 6, 7),
)


def soffit(z):
    return SOFFIT_B + SLOPE * (z - ZB)


def _sloped_box(g, verts, mat, bevel=0.0):
    g.mesh(verts, _BOX, mat, bevel=bevel, segs=1 if bevel else 0)


def _shell(g, lod):
    """Walls leave a cavity. The street face is open above the breast."""
    bev = lod_pick(lod, 0.0015, 0.0)
    green = "Lib_PaintGreen"
    inner = OX - T
    # Sides run the full depth. Front and back stop short of the inner face.
    for sign in (-1.0, 1.0):
        xo = sign * OX
        xi = sign * inner
        _sloped_box(g, [
            (xo, 0.0, -OZ), (xi, 0.0, -OZ), (xi, 0.0, OZ), (xo, 0.0, OZ),
            (xo, soffit(-OZ) - 0.004, -OZ), (xi, soffit(-OZ) - 0.004, -OZ),
            (xi, soffit(OZ) - 0.004, OZ), (xo, soffit(OZ) - 0.004, OZ),
        ], green, bevel=bev)
    back_x = inner - GAP
    back_top = soffit(-OZ) - 0.006
    g.box((0, back_top * 0.5, -(OZ - T * 0.5)), (back_x * 2.0, back_top, T), green, bevel=bev)
    # Front pier, header, breast. Flat tops stay under the sloping soffit.
    front_top = soffit(OZ - T) - 0.006
    pier_o = inner - GAP
    pier_i = 0.50 + GAP
    pier_w = pier_o - pier_i
    pier_x = (pier_o + pier_i) * 0.5
    pier_z = OZ - T * 0.5
    g.box((-pier_x, front_top * 0.5, pier_z), (pier_w, front_top, T), green, bevel=bev)
    g.box((pier_x, front_top * 0.5, pier_z), (pier_w, front_top, T), green, bevel=bev)
    win_lo, win_hi = 1.055, 1.745
    g.box((0, (front_top + win_hi) * 0.5, pier_z), (1.00, front_top - win_hi, T), green, bevel=bev)
    g.box((0, win_lo * 0.5, pier_z), (1.00, win_lo, T), green, bevel=bev)
    # Floor and ceiling stay clear of every wall and of the roof.
    g.box((0, 0.028, 0), (1.60, 0.036, 0.98), "Lib_Interior")
    g.box((0, 2.020, 0), (1.55, 0.024, 0.90), "Lib_Interior")


def _roof(g):
    """One pitched slab. The front edge turns down into a drip, so the fascia is the roof."""
    y_top_b = soffit(ZB) + ROOF_T
    y_top_f = soffit(ZF) + ROOF_T
    y_drip = soffit(ZF) - 0.040
    prof = (
        (ZB, y_top_b),
        (ZF, y_top_f),
        (ZF, y_drip),
        (ZD, y_drip),
        (ZD, soffit(ZD)),
        (ZB, soffit(ZB)),
    )
    verts = []
    for x in (-ROOF_X, ROOF_X):
        for z, y in prof:
            verts.append((x, y, z))
    # Caps are fanned so the concave drip notch does not triangulate outside the profile.
    faces = [
        (0, 1, 5), (1, 4, 5), (1, 3, 4), (1, 2, 3),
        (11, 7, 6), (11, 10, 7), (10, 9, 7), (9, 8, 7),
    ]
    n = 6
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, j + n, i + n))
    g.mesh(verts, faces, "Lib_BoxGreen")


def _shutter(g, lod):
    """Fold-up leaf on the head of the opening, about 14 cm proud of the face."""
    ang = math.radians(15.0)
    length = 0.40
    thick = 0.012
    x0, x1 = -0.46, 0.46
    hy, hz = 1.735, 0.622
    dy = length * math.cos(ang)
    dz = length * math.sin(ang)
    ny, nz = -math.sin(ang), math.cos(ang)
    oy, oz = ny * thick, nz * thick

    def corner(x, tip):
        return (x, hy + (dy if tip else 0.0), hz + (dz if tip else 0.0))

    outer = [corner(x0, False), corner(x1, False), corner(x1, True), corner(x0, True)]
    inner = [(p[0], p[1] - oy, p[2] - oz) for p in outer]
    g.mesh(outer + inner, _BOX, "Lib_PaintGreen")
    # Head and jambs sit on the pier, clear of the shutter and of the counter.
    g.box((0, 1.800, 0.612), (1.04, 0.018, 0.012), "Lib_Steel")
    g.box((-0.530, 1.44, 0.612), (0.016, 0.52, 0.012), "Lib_Steel")
    g.box((0.530, 1.44, 0.612), (0.016, 0.52, 0.012), "Lib_Steel")
    if lod == 0:
        for x in (-0.56, 0.56):
            sweep_tube(g, [(x, 1.58, 0.66), (x, 2.02, 0.72)], 0.006, "Lib_SteelDark", segments=8)


def _interior(g, lod):
    """Back wall, a paper shelf, a sill, and a ceiling light."""
    g.box((0, 1.46, -0.512), (1.36, 0.70, 0.010), "Lib_WoodDark")
    g.box((0, 1.380, -0.22), (1.16, 0.016, 0.26), "Lib_Wood")
    if lod == 0:
        g.box((-0.38, 1.478, -0.20), (0.16, 0.16, 0.10), "Lib_PaintCream")
        g.box((-0.16, 1.456, -0.24), (0.14, 0.12, 0.08), "Lib_PaintWhite")
        g.box((0.02, 1.446, -0.18), (0.08, 0.10, 0.06), "Lib_Orange")
        g.box((0.16, 1.438, -0.22), (0.07, 0.08, 0.05), "Lib_PaintRed")
        g.box((0.34, 1.488, -0.16), (0.12, 0.18, 0.08), "Lib_BoxBlue")
        g.box((0, 1.148, 0.28), (0.88, 0.014, 0.16), "Lib_Wood")
        g.box((-0.26, 1.286, 0.30), (0.14, 0.24, 0.008), "Lib_PaintCream")
        g.box((-0.06, 1.256, 0.30), (0.12, 0.18, 0.008), "Lib_PaintWhite")
        g.box((0.14, 1.226, 0.28), (0.08, 0.12, 0.05), "Lib_Orange")
        g.box((0.30, 1.246, 0.29), (0.10, 0.16, 0.045), "Lib_BoxRed")
    else:
        g.box((0, 1.470, -0.20), (0.36, 0.14, 0.10), "Lib_PaintCream")
        g.box((0, 1.148, 0.28), (0.70, 0.014, 0.14), "Lib_Wood")
    g.box((0, 1.955, 0.02), (0.62, 0.012, 0.05), "Lib_WindowLit")


def _counter(g, lod):
    """Top is 1.12 m. The slab starts just outside the breast."""
    g.box((0, 1.102, 0.78), (1.32, 0.036, 0.32), "Lib_Steel", bevel=lod_pick(lod, 0.0015, 0.0))
    for x in (-0.46, 0.46):
        g.box((x, 0.986, 0.76), (0.012, 0.172, 0.14), "Lib_SteelDark")
    if lod == 0:
        g.box((-0.28, 1.146, 0.84), (0.16, 0.036, 0.09), "Lib_PaintCream")
        g.box((0.22, 1.140, 0.80), (0.14, 0.026, 0.08), "Lib_PaintWhite")


def _door(g, lod):
    z = -(OZ + 0.018)
    g.box((0.10, 1.00, z), (0.64, 1.72, 0.012), "Lib_BoxGreen")
    if lod == 0:
        g.cylinder((-0.10, 1.02, z - 0.020), 0.008, 0.18, "Lib_Steel", 8, axis="Y")
        g.cylinder((-0.10, 0.86, z - 0.018), 0.010, 0.008, "Lib_Brass", 8, axis="Z")
        for y in (0.42, 1.58):
            g.cylinder((0.40, y, z - 0.018), 0.008, 0.04, "Lib_SteelDark", 8, axis="Y")


def _mag_center(h, thick, tilt):
    ang = math.radians(tilt)
    half_h = h * 0.5
    half_t = thick * 0.5
    inner = WIRE_X + WIRE_R + 0.012
    return inner + half_h * math.sin(ang) + half_t * math.cos(ang)


def _racks(g, lod):
    """Bolted wire grid. Each magazine is a thick block tilted back onto a lip."""
    mags = MAGS if lod == 0 else MAGS[2:4]
    seg = 8 if lod == 0 else 6
    for sign in (1.0, -1.0):
        for y, z, h, width, thick, mat, tilt in mags:
            cx = _mag_center(h, thick, tilt)
            g.box(
                (sign * cx, y, z),
                (thick, h, width),
                mat,
                euler=(0.0, 0.0, sign * tilt),
            )
        if lod == 0:
            for z in (-0.42, 0.0, 0.40):
                g.cylinder((sign * WIRE_X, 1.04, z), WIRE_R, 1.36, "Lib_SteelDark", seg, axis="Y")
                # Standoff from the wall to the wire. It stops short of both.
                g.cylinder((sign * 0.924, 0.50, z), 0.006, 0.032, "Lib_Steel", seg, axis="X")
                g.cylinder((sign * 0.924, 1.60, z), 0.006, 0.032, "Lib_Steel", seg, axis="X")
            for z0, z1 in ((-0.40, -0.02), (0.02, 0.38)):
                cz = (z0 + z1) * 0.5
                length = z1 - z0
                for hy in (0.40, 0.86, 1.28, 1.68):
                    g.cylinder((sign * WIRE_X, hy, cz), 0.0045, length, "Lib_SteelDark", seg, axis="Z")
            for row in (0, 1, 2):
                pair = MAGS[row * 2:row * 2 + 2]
                lip_y = 9.0
                lip_x = 0.0
                for y, _z, h, _w, thick, _mat, tilt in pair:
                    ang = math.radians(tilt)
                    half_h = h * 0.5
                    half_t = thick * 0.5
                    cx = _mag_center(h, thick, tilt)
                    outer = cx + half_h * math.sin(ang) + half_t * math.cos(ang)
                    bottom = y - half_h * math.cos(ang) - half_t * math.sin(ang)
                    lip_x = max(lip_x, outer)
                    lip_y = min(lip_y, bottom)
                g.cylinder(
                    (sign * (lip_x + 0.014), lip_y - 0.012, 0.0),
                    0.0045, 0.70, "Lib_SteelDark", seg, axis="Z",
                )
        else:
            g.cylinder((sign * WIRE_X, 1.10, -0.16), 0.006, 0.70, "Lib_SteelDark", seg, axis="Y")
            g.cylinder((sign * WIRE_X, 1.10, 0.18), 0.006, 0.70, "Lib_SteelDark", seg, axis="Y")
            g.cylinder((sign * 1.04, 0.90, 0.0), 0.005, 0.50, "Lib_SteelDark", seg, axis="Z")


def _base(g):
    """Dark kick on the street face, and a concrete curb where the booth meets the walk."""
    g.box((0, 0.085, OZ + 0.016), (1.44, 0.09, 0.012), "Lib_SteelDark")
    g.box((0, 0.013, 0.67), (W + 0.16, 0.026, 0.12), "Lib_Concrete")
    g.box((0, 0.013, -0.67), (W + 0.16, 0.026, 0.12), "Lib_Concrete")
    g.box((-0.944, 0.013, 0), (0.072, 0.026, 1.08), "Lib_Concrete")
    g.box((0.944, 0.013, 0), (0.072, 0.026, 1.08), "Lib_Concrete")


@register
def create():
    a = Asset(
        "Newsstand_Corner",
        "StreetFurniture",
        "Newsstand, 1.80 m wide, counter 1.12 m, open hatch and wire racks. No masthead.",
    )
    a.climb_note = "Roof is the stand."
    a.vault_note = "Counter lip is 1.12 m."
    for lod in (0, 1):
        g = a.begin(lod)
        _shell(g, lod)
        _roof(g)
        _shutter(g, lod)
        _interior(g, lod)
        _counter(g, lod)
        _door(g, lod)
        _racks(g, lod)
        _base(g)
        a.end()
    # Inside the wall solids, the high end of the roof, and the counter.
    a.box("Col_WallL", (-OX + T * 0.5, 1.00, 0), (0.028, 1.84, 0.96))
    a.box("Col_WallR", (OX - T * 0.5, 1.00, 0), (0.028, 1.84, 0.96))
    a.box("Col_WallB", (0, 1.00, -(OZ - T * 0.5)), (1.50, 1.84, 0.028))
    a.box("Col_Breast", (0, 0.50, 0.575), (0.64, 0.72, 0.022))
    a.box("Col_PierL", (-0.68, 1.05, OZ - T * 0.5), (0.22, 1.90, 0.028))
    a.box("Col_PierR", (0.68, 1.05, OZ - T * 0.5), (0.22, 1.90, 0.028))
    # Crown is the street edge of the pitch. This box stays under it.
    a.box("Col_Roof", (0, 2.279, 0.775), (1.60, 0.022, 0.030))
    a.box("Col_RoofMid", (0, 2.208, 0.15), (1.50, 0.016, 0.10))
    a.box("Col_RoofBack", (0, 2.146, -0.45), (1.50, 0.012, 0.16))
    a.box("Col_Counter", (0, 1.104, 0.78), (1.10, 0.016, 0.22))
    return a
