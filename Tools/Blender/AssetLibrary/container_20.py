"""ISO shipping container. Rolled trapezoid corrugation, corner castings, locking bars.

20 ft is 6.06 x 2.44 x 2.59 m. Doors face +Z. The wall is one closed
corrugated shell per face, not battens. Corner castings have an oval
hole on each exposed face. Container_20 is rust red, with blue and green twins.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

WIDTH = 2.44
HEIGHT = 2.59
CAST = 0.178


def _face_x():
    return WIDTH * 0.5 - 0.08


@register
def create():
    return build_container(
        "Container_20",
        6.06,
        "Lib_BoxRed",
        "20-foot container, rust-red enamel. 6.06 x 2.44 x 2.59 m. Trapezoid corrugation, corner castings with oval holes, four locking bars on the +Z doors.",
    )


@register
def create_blue():
    return build_container(
        "Container_20_Blue",
        6.06,
        "Lib_BoxBlue",
        "20-foot container, blue enamel. Same shell as Container_20.",
    )


@register
def create_green():
    return build_container(
        "Container_20_Green",
        6.06,
        "Lib_BoxGreen",
        "20-foot container, green enamel. Same shell as Container_20.",
    )


def _logo(g, color):
    """Original mark on a flat panel. Not a shipping-line trademark."""
    face = _face_x()
    panel_x = face - 0.002 + 0.008
    g.box((panel_x, 1.48, -0.05), (0.016, 0.92, 1.55), color)
    x = panel_x + 0.012
    g.box((x, 1.55, -0.05), (0.006, 0.62, 1.35), "Lib_PaintWhite")
    g.box((x + 0.006, 1.78, -0.42), (0.006, 0.18, 0.42), "Lib_BoxBlue", euler=(0, 18, 0))
    g.box((x + 0.006, 1.78, 0.28), (0.006, 0.18, 0.42), "Lib_BoxBlue", euler=(0, -18, 0))
    g.text("NORTHLINE", (x + 0.012, 1.28, -0.05), 0.10, "Lib_SteelDark", extrude=0.003, yaw=90)


class _Vert:
    def __init__(self):
        self.verts = []
        self.faces = []
        self._index = {}

    def v(self, p):
        key = (round(p[0], 5), round(p[1], 5), round(p[2], 5))
        if key in self._index:
            return self._index[key]
        i = len(self.verts)
        self.verts.append((p[0], p[1], p[2]))
        self._index[key] = i
        return i

    def face(self, ids):
        if len(set(ids)) >= 3:
            self.faces.append(tuple(ids))


def _cross(u, v):
    return (
        u[1] * v[2] - u[2] * v[1],
        u[2] * v[0] - u[0] * v[2],
        u[0] * v[1] - u[1] * v[0],
    )


def _add(a, b, s=1.0):
    return (a[0] + b[0] * s, a[1] + b[1] * s, a[2] + b[2] * s)


def _casting_box(g, cx, cy, cz, sx, sy, sz):
    """One closed casting. Three exposed faces each carry a blind oval hole."""
    h = CAST * 0.5
    segs = 4
    n = segs * 4
    hole = 0.040
    b = _Vert()
    faces = (
        ((sx * h, 0.0, 0.0), (0.0, 1.0, 0.0), (0.0, 0.0, sx), True, 0.024, 0.046),
        ((0.0, sy * h, 0.0), (1.0, 0.0, 0.0), (0.0, 0.0, -sy), True, 0.046, 0.024),
        ((0.0, 0.0, sz * h), (1.0, 0.0, 0.0), (0.0, sz, 0.0), True, 0.046, 0.024),
        ((-sx * h, 0.0, 0.0), (0.0, 1.0, 0.0), (0.0, 0.0, -sx), False, 0.0, 0.0),
        ((0.0, -sy * h, 0.0), (1.0, 0.0, 0.0), (0.0, 0.0, sy), False, 0.0, 0.0),
        ((0.0, 0.0, -sz * h), (1.0, 0.0, 0.0), (0.0, -sz, 0.0), False, 0.0, 0.0),
    )
    for origin, u, v, exposed, ru, rv in faces:
        world_o = (cx + origin[0], cy + origin[1], cz + origin[2])
        outward = _cross(u, v)
        ring = []
        corners = ((h, h), (-h, h), (-h, -h), (h, -h))
        for i in range(4):
            u0, v0 = corners[i]
            u1, v1 = corners[(i + 1) % 4]
            for s in range(segs):
                t = s / float(segs)
                uu = u0 + (u1 - u0) * t
                vv = v0 + (v1 - v0) * t
                p = _add(_add(world_o, u, uu), v, vv)
                ring.append(b.v(p))
        if not exposed:
            for i in range(1, n - 1):
                b.face((ring[0], ring[i], ring[i + 1]))
            continue
        a0 = math.atan2(1.0, 1.0)
        mouth = []
        floor = []
        for i in range(n):
            ang = a0 + 2.0 * math.pi * i / n
            ou = ru * math.cos(ang)
            ov = rv * math.sin(ang)
            mouth.append(b.v(_add(_add(world_o, u, ou), v, ov)))
            floor.append(b.v(_add(_add(_add(world_o, u, ou), v, ov), outward, -hole)))
        center = b.v(_add(world_o, outward, -hole))
        for i in range(n):
            j = (i + 1) % n
            b.face((ring[i], mouth[i], mouth[j], ring[j]))
            b.face((mouth[i], floor[i], floor[j], mouth[j]))
            b.face((center, floor[j], floor[i]))
    g.mesh(b.verts, b.faces, "Lib_SteelDark")


def _castings(g, length, lod):
    hx = WIDTH * 0.5 - CAST * 0.5
    hz = length * 0.5 - CAST * 0.5
    post_h = HEIGHT - CAST + 0.04
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            x = sx * hx
            z = sz * hz
            if lod == 0:
                _casting_box(g, x, CAST * 0.5, z, sx, -1.0, sz)
                _casting_box(g, x, HEIGHT - CAST * 0.5, z, sx, 1.0, sz)
            else:
                g.box((x, CAST * 0.5, z), (CAST, CAST, CAST), "Lib_SteelDark")
                g.box((x, HEIGHT - CAST * 0.5, z), (CAST, CAST, CAST), "Lib_SteelDark")
            g.box((x, HEIGHT * 0.5, z), (0.11, post_h, 0.11), "Lib_SteelDark")


def _rails(g, length):
    face = _face_x()
    span = length - 0.32
    for sx in (-1.0, 1.0):
        x = sx * (face - 0.012)
        g.box((x, 0.09, 0), (0.07, 0.10, span), "Lib_SteelDark")
        g.box((x, HEIGHT - 0.09, 0), (0.07, 0.10, span), "Lib_SteelDark")
    # Door-end header and sill, welded into the castings.
    z = length * 0.5 - 0.09
    g.box((0, 0.10, z), (WIDTH - 0.42, 0.10, 0.08), "Lib_SteelDark")
    g.box((0, HEIGHT - 0.10, z), (WIDTH - 0.42, 0.10, 0.08), "Lib_SteelDark")


# One period: valley, slope, crown, slope. The slopes are long enough to read as a roll.
PITCH = 0.220
VALLEY = 0.050
SLOPE = 0.060
CROWN = 0.050
SHEET = 0.004
PROUD = 0.042
BURY = 0.002


def _trap_pairs(start, n, pitch, outer_of, inner_of):
    """Stations along the run. outer_of(s, crown) -> (a, b) in the profile plane."""
    scale = pitch / PITCH
    valley = VALLEY * scale
    slope = SLOPE * scale
    crown = CROWN * scale
    pairs = []
    s = start
    for _ in range(n):
        pairs.append((outer_of(s, False), inner_of(s, False)))
        pairs.append((outer_of(s + valley, False), inner_of(s + valley, False)))
        pairs.append((outer_of(s + valley + slope, True), inner_of(s + valley + slope, True)))
        pairs.append((outer_of(s + valley + slope + crown, True), inner_of(s + valley + slope + crown, True)))
        s += pitch
    pairs.append((outer_of(s, False), inner_of(s, False)))
    return pairs


def _sheet_y(g, pairs, y0, y1, mat):
    """Closed corrugated shell. pairs are ((x, z) outer, (x, z) inner), extruded in Y."""
    m = len(pairs)
    verts = []

    def add(x, y, z):
        verts.append((x, y, z))
        return len(verts) - 1

    o0, i0, o1, i1 = [], [], [], []
    for (xo, zo), (xi, zi) in pairs:
        o0.append(add(xo, y0, zo))
        i0.append(add(xi, y0, zi))
        o1.append(add(xo, y1, zo))
        i1.append(add(xi, y1, zi))
    faces = []
    for i in range(m - 1):
        faces.append((o0[i], o0[i + 1], o1[i + 1], o1[i]))
        faces.append((i0[i], i1[i], i1[i + 1], i0[i + 1]))
        faces.append((o0[i], i0[i], i0[i + 1], o0[i + 1]))
        faces.append((o1[i], o1[i + 1], i1[i + 1], i1[i]))
    faces.append((o0[0], o1[0], i1[0], i0[0]))
    faces.append((o0[-1], i0[-1], i1[-1], o1[-1]))
    g.mesh(verts, faces, mat)


def _sheet_x(g, pairs, x0, x1, mat):
    """Roof shell. pairs are ((y, z) outer, (y, z) inner), extruded in X."""
    m = len(pairs)
    verts = []

    def add(x, y, z):
        verts.append((x, y, z))
        return len(verts) - 1

    o0, i0, o1, i1 = [], [], [], []
    for (yo, zo), (yi, zi) in pairs:
        o0.append(add(x0, yo, zo))
        i0.append(add(x0, yi, zi))
        o1.append(add(x1, yo, zo))
        i1.append(add(x1, yi, zi))
    faces = []
    for i in range(m - 1):
        faces.append((o0[i], o0[i + 1], o1[i + 1], o1[i]))
        faces.append((i0[i], i1[i], i1[i + 1], i0[i + 1]))
        faces.append((o0[i], i0[i], i0[i + 1], o0[i + 1]))
        faces.append((o1[i], o1[i + 1], i1[i + 1], i1[i]))
    faces.append((o0[0], o1[0], i1[0], i0[0]))
    faces.append((o0[-1], i0[-1], i1[-1], o1[-1]))
    g.mesh(verts, faces, mat)


def _runs(z0, z1, pitch):
    n = int((z1 - z0) / pitch)
    if n < 1:
        return
    # Keep the run on a whole number of periods so it ends on a valley.
    yield z0, n


def _side_shell(g, length, color, pitch, logo_gap):
    face = _face_x()
    y0, y1 = 0.22, 2.34
    z0 = -(length * 0.5 - 0.32)
    z1 = length * 0.5 - 0.32
    spans = [(-1.0, z0, z1)]
    if logo_gap:
        spans = [(-1.0, z0, z1), (1.0, z0, logo_gap[0]), (1.0, logo_gap[1], z1)]
    else:
        spans = [(-1.0, z0, z1), (1.0, z0, z1)]
    for sign, a, b in spans:
        for start, n in _runs(a, b, pitch) or ():
            def outer(s, crown, sign=sign):
                x = sign * (face + (PROUD if crown else -BURY))
                return (x, s)

            def inner(s, crown, sign=sign):
                x = sign * (face + (PROUD if crown else -BURY)) - sign * SHEET
                return (x, s)

            _sheet_y(g, _trap_pairs(start, n, pitch, outer, inner), y0, y1, color)


def _end_shell(g, length, color, pitch):
    face = -(length * 0.5 - 0.06)
    y0, y1 = 0.22, 2.34
    x0, x1 = -0.96, 0.96
    for start, n in _runs(x0, x1, pitch) or ():
        def outer(s, crown, face=face):
            z = face + (BURY if not crown else -PROUD)
            return (s, z)

        def inner(s, crown, face=face):
            z = face + (BURY if not crown else -PROUD) + SHEET
            return (s, z)

        # Profile runs along X. Reuse the Y extruder with (x, z) pairs.
        _sheet_y(g, _trap_pairs(start, n, pitch, outer, inner), y0, y1, color)


def _roof_shell(g, length, color, pitch):
    top = HEIGHT * 0.5 + (HEIGHT - 0.10) * 0.5
    z0 = -(length * 0.5 - 0.36)
    z1 = length * 0.5 - 0.36
    for start, n in _runs(z0, z1, pitch) or ():
        def outer(s, crown, top=top):
            y = top + (PROUD * 0.7 if crown else -BURY)
            return (y, s)

        def inner(s, crown, top=top):
            y = top + (PROUD * 0.7 if crown else -BURY) - SHEET
            return (y, s)

        _sheet_x(g, _trap_pairs(start, n, pitch, outer, inner), -0.96, 0.96, color)


def _door_shell(g, outer_z, x0, x1, color, pitch):
    y0, y1 = 0.36, 2.22
    for start, n in _runs(x0, x1, pitch) or ():
        def outer(s, crown, outer_z=outer_z):
            z = outer_z + (PROUD * 0.70 if crown else -BURY)
            return (s, z)

        def inner(s, crown, outer_z=outer_z):
            z = outer_z + (PROUD * 0.70 if crown else -BURY) - SHEET
            return (s, z)

        _sheet_y(g, _trap_pairs(start, n, pitch, outer, inner), y0, y1, color)


def _bars(g, outer_z):
    """Four locking bars, two on each door, with cams and handles."""
    depth = 0.028
    # The bar sits on the door crown and stays proud of it.
    cz = outer_z + PROUD * 0.70 + 0.016
    y0, y1 = 0.34, 2.24
    for x in (-0.74, -0.28, 0.28, 0.74):
        g.box((x, (y0 + y1) * 0.5, cz), (0.028, y1 - y0, depth), "Lib_SteelDark")
        g.box((x, y0 + 0.02, cz + 0.01), (0.07, 0.045, 0.04), "Lib_Steel")
        g.box((x, y1 - 0.02, cz + 0.01), (0.07, 0.045, 0.04), "Lib_Steel")
        grip = 0.07 if x < 0 else -0.07
        g.box((x, 1.18, cz + 0.028), (0.10, 0.024, 0.05), "Lib_Steel")
        g.box((x + grip, 1.08, cz + 0.04), (0.024, 0.16, 0.024), "Lib_Brass")


def _doors(g, length, color, lod):
    face = length * 0.5 - 0.06
    thick = 0.034
    zc = face + 0.015
    outer = zc + thick * 0.5
    y0, y1 = 0.22, 2.36
    cy = (y0 + y1) * 0.5
    hy = y1 - y0
    g.box((-0.505, cy, zc), (0.99, hy, thick), color)
    g.box((0.505, cy, zc), (0.99, hy, thick), color)
    g.box((0.0, cy, outer + 0.004), (0.03, hy, 0.012), "Lib_SteelDark")
    if lod == 0:
        pitch = PITCH
        _door_shell(g, outer, -0.90, -0.12, color, pitch)
        _door_shell(g, outer, 0.12, 0.90, color, pitch)
        for x, y in ((-1.01, 0.55), (-1.01, 1.35), (-1.01, 2.10), (1.01, 0.55), (1.01, 1.35), (1.01, 2.10)):
            g.cylinder((x, y, zc), 0.016, 0.07, "Lib_SteelDark", 8)
    if lod < 2:
        _bars(g, outer)


def build_container(name, length, color, blurb):
    a = Asset(name, "Harbor", blurb)
    a.climbable = True
    a.climb_note = "Long sides are cling. Door bars are on +Z. Collider is inside the wall plate."
    a.vault_note = "No rail. Roof is a landing at 2.59 m."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        g.box((0, HEIGHT * 0.5, 0), (WIDTH - 0.16, HEIGHT - 0.10, length - 0.12), color)
        _castings(g, length, lod)
        _rails(g, length)
        _doors(g, length, color, lod)
        if lod < 2:
            pitch = PITCH if lod == 0 else PITCH * 2.0
            gap = (-0.90, 0.80) if lod == 0 else None
            _side_shell(g, length, color, pitch, gap)
            _end_shell(g, length, color, pitch)
            _roof_shell(g, length, color, pitch)
        if lod == 0:
            _logo(g, color)
        a.end()
    # Inside the plate, clear of the corner-casting overlap.
    a.box("Climb_Body", (0, 1.29, 0), (1.96, 2.14, length - 0.52))
    return a
