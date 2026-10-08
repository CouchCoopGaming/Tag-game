"""ISO shipping container. Corrugated enamel, corner castings, locking bars.

20 ft is 6.06 x 2.44 x 2.59 m. Doors face +Z. Ribs are modeled steel,
not a brick tile. Container_20 is rust red, with blue and green twins.
"""

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
        "20-foot container, rust-red enamel. 6.06 x 2.44 x 2.59 m. Vertical corrugation, corner castings, four locking bars on the +Z doors.",
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


def _castings(g, length, lod):
    hx = WIDTH * 0.5 - CAST * 0.5
    hz = length * 0.5 - CAST * 0.5
    post_h = HEIGHT - CAST + 0.04
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            x = sx * hx
            z = sz * hz
            g.box((x, CAST * 0.5, z), (CAST, CAST, CAST), "Lib_SteelDark")
            g.box((x, HEIGHT - CAST * 0.5, z), (CAST, CAST, CAST), "Lib_SteelDark")
            g.box((x, HEIGHT * 0.5, z), (0.11, post_h, 0.11), "Lib_SteelDark")
            if lod == 0:
                # Dark pocket so the casting reads as a block with a hole, not a plain cube.
                g.box((x, CAST * 0.5, z + sz * (CAST * 0.5 - 0.004)), (0.07, 0.09, 0.014), "Lib_Black")
                g.box((x, HEIGHT - CAST * 0.5, z + sz * (CAST * 0.5 - 0.004)), (0.07, 0.09, 0.014), "Lib_Black")
                g.box((x + sx * (CAST * 0.5 - 0.004), HEIGHT * 0.5, z), (0.014, 0.09, 0.07), "Lib_Black")


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


def _rib_center(face, sign, depth):
    return face + sign * (depth * 0.5 - 0.002)


def _side_ribs(g, length, color, pitch, depth, logo_gap):
    face = _face_x()
    y0, y1 = 0.24, 2.32
    z0 = -(length * 0.5 - 0.30)
    z1 = length * 0.5 - 0.30
    z = z0
    while z <= z1 + 0.001:
        for sign in (-1.0, 1.0):
            if logo_gap and sign > 0 and logo_gap[0] < z < logo_gap[1]:
                continue
            cx = _rib_center(sign * face, sign, depth)
            g.box((cx, (y0 + y1) * 0.5, z), (depth, y1 - y0, 0.058), color)
        z += pitch


def _end_ribs(g, length, color, pitch, depth):
    face = -(length * 0.5 - 0.06)
    y0, y1 = 0.24, 2.32
    x = -0.96
    while x <= 0.961:
        cz = _rib_center(face, -1.0, depth)
        g.box((x, (y0 + y1) * 0.5, cz), (0.058, y1 - y0, depth), color)
        x += pitch


def _roof_ribs(g, length, color, pitch):
    top = HEIGHT * 0.5 + (HEIGHT - 0.10) * 0.5
    depth = 0.022
    cy = top - 0.002 + depth * 0.5
    z0 = -(length * 0.5 - 0.36)
    z1 = length * 0.5 - 0.36
    z = z0
    while z <= z1 + 0.001:
        g.box((0, cy, z), (1.92, depth, 0.08), color)
        z += pitch


def _door_ribs(g, outer_z, color):
    y0, y1 = 0.36, 2.22
    cy = (y0 + y1) * 0.5
    depth = 0.024
    cz = outer_z + (depth * 0.5 - 0.002)
    for x in (-0.88, -0.62, -0.40, -0.14, 0.14, 0.40, 0.62, 0.88):
        g.box((x, cy, cz), (0.05, y1 - y0, depth), color)


def _bars(g, outer_z):
    """Four locking bars, two on each door, with cams and handles."""
    depth = 0.032
    cz = outer_z + (depth * 0.5 - 0.002)
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
        _door_ribs(g, outer, color)
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
            pitch = 0.15 if lod == 0 else 0.32
            depth = 0.030 if lod == 0 else 0.024
            gap = (-0.82, 0.72) if lod == 0 else None
            _side_ribs(g, length, color, pitch, depth, gap)
            _end_ribs(g, length, color, pitch, depth)
            _roof_ribs(g, length, color, pitch * 2.0)
        if lod == 0:
            _logo(g, color)
        a.end()
    # Inside the plate, clear of the corner-casting overlap.
    a.box("Climb_Body", (0, 1.29, 0), (1.96, 2.14, length - 0.52))
    return a
