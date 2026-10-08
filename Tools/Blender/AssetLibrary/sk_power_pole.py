"""Distribution pole span. Two 10.5 m timbers, crossarms, a transformer, sagging conductors."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import catenary, polyline, span_collider

# 8 m between poles. Short for a real span, long enough that the sag reads next to a person.
POLES = (-4.0, 4.0)
HEIGHT = 10.5
R0 = 0.165
R1 = 0.085
ARM_Y = 8.90


def _radius_at(y):
    return R0 + (R1 - R0) * (y / HEIGHT)


@register
def create():
    a = Asset(
        "PowerPole_Span",
        "StreetFurniture",
        "Two 10.5 m wood poles, 8 m apart. Crossarms at 8.9 m, three conductors sag 0.38 m, transformer and a guy. Conductors are visual only.",
    )
    a.climb_note = "Round timber, about 33 cm at the ground. Not a cling wall."
    a.vault_note = "No rail. Conductors are visual only."
    a.loose_pivot = True
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        steps = lod_pick(lod, 8, 4)
        bev = lod_pick(lod, 0.004, 0.0)
        for x in POLES:
            g.cone((x, HEIGHT * 0.5, 0), R0, R1, HEIGHT, "Lib_Bark", seg)
            g.box((x, ARM_Y, 0.06), (2.20, 0.09, 0.11), "Lib_Wood", uv_scale=1.2, bevel=bev, segs=1 if lod == 0 else 0)
            if lod == 0:
                for y in (1.6, 4.4, 7.1):
                    g.torus((x, y, 0), _radius_at(y) - 0.004, 0.011, "Lib_SteelDark", 14, 6)
                g.pipe((x - 0.42, ARM_Y - 0.55, 0.0), (x - 0.10, ARM_Y - 0.05, 0.05), 0.016, "Lib_Steel", 6)
                g.pipe((x + 0.42, ARM_Y - 0.55, 0.0), (x + 0.10, ARM_Y - 0.05, 0.05), 0.016, "Lib_Steel", 6)
                # Ground wire on the street face.
                polyline(g, [
                    (x, y, _radius_at(y) + 0.008) for y in (0.35, 1.8, 3.4, 5.0, 6.6)
                ], 0.004, "Lib_Steel", 4)
            for dx in (-0.85, 0.0, 0.85):
                ix = x + dx
                g.cylinder((ix, ARM_Y + 0.08, 0.12), 0.024, 0.08, "Lib_SteelDark", 8)
                g.cone((ix, ARM_Y + 0.18, 0.12), 0.046, 0.024, 0.09, "Lib_Glass", lod_pick(lod, 10, 6))
                g.cylinder((ix, ARM_Y + 0.24, 0.12), 0.011, 0.03, "Lib_Steel", 6)
            g.cylinder((x, 7.35, 0.16), 0.04, 0.05, "Lib_PaintWhite", 8)
        for dx in (-0.85, 0.0, 0.85):
            catenary(
                g,
                (POLES[0] + dx, ARM_Y + 0.24, 0.15),
                (POLES[1] + dx, ARM_Y + 0.24, 0.15),
                0.38,
                0.008,
                "Lib_Black",
                steps,
                5,
            )
        catenary(g, (POLES[0], 7.35, 0.20), (POLES[1], 7.35, 0.20), 0.46, 0.007, "Lib_Steel", steps, 5)
        px = POLES[1]
        g.box((px, 6.30, 0.18), (0.08, 0.32, 0.24), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((px, 6.05, 0.50), 0.20, 0.68, "Lib_Steel", seg, bevel=0.006 if lod == 0 else 0, segs=1 if lod == 0 else 0)
        g.cylinder((px, 6.42, 0.50), 0.22, 0.05, "Lib_SteelDark", seg)
        g.cylinder((px, 5.70, 0.50), 0.22, 0.045, "Lib_SteelDark", seg)
        if lod == 0:
            for ox, oz in ((-0.08, 0.50), (0.08, 0.50), (0.0, 0.64)):
                g.cylinder((px + ox, 6.50, oz), 0.016, 0.07, "Lib_Brass", 6)
            g.torus((px, 6.10, 0.50), 0.20, 0.010, "Lib_Rust", 16, 5)
        # Tensioned guy, essentially straight.
        guy_a = (POLES[0], 7.85, 0.08)
        guy_b = (-5.48, 0.14, 0.84)
        g.pipe(guy_a, guy_b, 0.008, "Lib_Steel", 6)
        g.box((-5.55, 0.04, 0.85), (0.28, 0.08, 0.22), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((-5.55, 0.11, 0.85), 0.028, 0.08, "Lib_Steel", 8)
        a.end()
    for i, x in enumerate(POLES):
        for j, (cy, h, rad) in enumerate((
            (1.15, 2.10, 0.128),
            (3.45, 2.30, 0.105),
            (5.85, 2.30, 0.088),
            (8.15, 2.10, 0.072),
            (9.90, 1.00, 0.060),
        )):
            a.capsule("Col_Pole_%d_%d" % (i, j), (x, cy, 0), rad, h, 1)
        # Arms are one timber through the pole. Colliders stay outside the timber so the two solids do not share a volume.
        for sign in (-1, 1):
            a.box("Col_Arm_%d_%d" % (i, sign), (x + sign * 0.64, ARM_Y, 0.06), (0.92, 0.06, 0.07))
    a.capsule("Col_Transformer", (POLES[1], 6.05, 0.50), 0.16, 0.58, 1)
    a.box("Col_Bracket", (POLES[1], 6.30, 0.22), (0.05, 0.22, 0.12))
    dx, dy, dz = guy_b[0] - guy_a[0], guy_b[1] - guy_a[1], guy_b[2] - guy_a[2]
    glen = math.sqrt(dx * dx + dy * dy + dz * dz)
    ux, uy, uz = dx / glen, dy / glen, dz / glen
    inset = 0.40
    c, s, e = span_collider(
        (guy_a[0] + ux * inset, guy_a[1] + uy * inset, guy_a[2] + uz * inset),
        (guy_b[0] - ux * inset, guy_b[1] - uy * inset, guy_b[2] - uz * inset),
        0.006,
    )
    a.box("Col_Guy", c, s, euler=e)
    a.box("Col_Anchor", (-5.55, 0.04, 0.85), (0.22, 0.06, 0.16))
    return a
