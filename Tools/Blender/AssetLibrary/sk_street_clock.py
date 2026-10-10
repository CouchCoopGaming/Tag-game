"""Two-face sidewalk post clock. No legend and no numerals.

Cast base, fluted shaft, round case with a dial on each side, hood and finial.
Hands read 10:10 from either face. Tick marks only.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


# Valley radii. Flutes add the ridges. Foot flares, shaft stays narrow.
POST = (
    (0.155, 0.030),
    (0.132, 0.070),
    (0.078, 0.160),
    (0.050, 0.300),
    (0.042, 0.520),
    (0.040, 1.550),
    (0.046, 1.920),
    (0.062, 2.040),
)

# Hood, origin at the bottom of the cap. Sits just above the case.
HOOD = (
    (0.210, 0.000),
    (0.196, 0.016),
    (0.140, 0.040),
    (0.070, 0.062),
    (0.022, 0.078),
)


def _fluted(g, profile, flutes, amp, seg, mat, origin=(0.0, 0.0, 0.0)):
    ox, oy, oz = origin
    n = len(profile)
    verts = []
    for i in range(seg):
        ang = 2.0 * math.pi * i / float(seg)
        wave = amp * (0.5 + 0.5 * math.cos(flutes * ang))
        s, c = math.sin(ang), math.cos(ang)
        for radius, y in profile:
            rr = radius + wave
            verts.append((ox + rr * s, oy + y, oz + rr * c))
    faces = []
    for i in range(seg):
        i2 = (i + 1) % seg
        for k in range(n - 1):
            a = i * n + k
            faces.append((a, a + 1, i2 * n + k + 1, i2 * n + k))
    bi = len(verts)
    verts.append((ox, oy + profile[0][1], oz))
    ti = bi + 1
    verts.append((ox, oy + profile[-1][1], oz))
    for i in range(seg):
        i2 = (i + 1) % seg
        faces.append((bi, i2 * n, i * n))
        faces.append((ti, i * n + n - 1, i2 * n + n - 1))
    g.mesh(verts, faces, mat)


def _hands(g, y, z, mirror):
    """10:10. The long axis follows the Z rotation so the tip stays on the dial."""
    for deg, length in ((60.0, 0.092), (-60.0, 0.128)):
        theta_deg = -deg if mirror else deg
        theta = math.radians(theta_deg)
        dx = -math.sin(theta)
        dy = math.cos(theta)
        dist = 0.024 + length * 0.5
        g.box(
            (dx * dist, y + dy * dist, z),
            (0.012, length, 0.005),
            "Lib_Black",
            euler=(0, 0, theta_deg),
        )


def _ticks(g, y, z):
    for i in range(12):
        ang = math.radians(i * 30.0)
        long = i % 3 == 0
        length = 0.026 if long else 0.014
        radius = 0.176
        g.box(
            (math.sin(ang) * radius, y + math.cos(ang) * radius, z),
            (0.007, length, 0.004),
            "Lib_Black",
            euler=(0, 0, -i * 30.0),
        )


def _face(g, lod, y, z_inner, mirror):
    """Bezel, dial, hub, ticks, and hands. z_inner is just outside the case."""
    seg = lod_pick(lod, 24, 12)
    s = 1.0 if z_inner > 0.0 else -1.0
    # Each disc steps outward so the solids stay a few millimetres apart.
    g.cylinder((0, y, z_inner + s * 0.005), 0.228, 0.010, "Lib_Steel", seg, axis="Z")
    g.cylinder((0, y, z_inner + s * 0.018), 0.198, 0.008, "Lib_PaintCream", seg, axis="Z")
    g.cylinder((0, y, z_inner + s * 0.030), 0.014, 0.008, "Lib_Black", lod_pick(lod, 10, 6), axis="Z")
    if lod == 0:
        _ticks(g, y, z_inner + s * 0.042)
    _hands(g, y, z_inner + s * 0.054, mirror)


def _body(g, lod):
    seg = lod_pick(lod, 28, 14)
    bev = lod_pick(lod, 0.0015, 0.0)
    bs = 1 if lod == 0 else 0
    g.box((0, 0.012, 0), (0.40, 0.024, 0.40), "Lib_SteelDark", bevel=bev, segs=bs)
    if lod == 0:
        for i in range(4):
            ang = math.pi * 0.25 + i * math.pi * 0.5
            g.cylinder(
                (math.sin(ang) * 0.175, 0.034, math.cos(ang) * 0.175),
                0.008,
                0.010,
                "Lib_Steel",
                6,
            )
    _fluted(g, POST, 8, 0.004, seg, "Lib_Black")
    # Neck stops short of the post and of the case. The case faces the street.
    g.cylinder((0, 2.103, 0), 0.036, 0.110, "Lib_Black", lod_pick(lod, 12, 8))
    g.cylinder((0, 2.400, 0), 0.236, 0.110, "Lib_Black", seg, axis="Z")
    _face(g, lod, 2.400, 0.063, False)
    _face(g, lod, 2.400, -0.063, True)
    _fluted(g, HOOD, 1, 0.0, lod_pick(lod, 20, 12), "Lib_Black", origin=(0.0, 2.642, 0.0))
    if lod == 0:
        g.sphere((0, 2.740, 0), 0.016, "Lib_Steel", 8)


@register
def create():
    a = Asset(
        "StreetClock_Post",
        "StreetFurniture",
        "Two-face post clock. Face center 2.40 m, dial 0.46 m, crown 2.76 m. No numerals.",
    )
    a.climb_note = "The shaft is about 8 cm across. Not a cling."
    a.vault_note = "Too tall and smooth to vault."
    for lod in (0, 1):
        _body(a.begin(lod), lod)
        a.end()
    a.box("Col_Plate", (0, 0.010, 0), (0.28, 0.016, 0.28))
    a.box("Col_Foot", (0, 0.090, 0), (0.10, 0.08, 0.10))
    a.capsule("Col_Post", (0, 1.05, 0), 0.026, 1.70, 1)
    a.box("Col_Case", (0, 2.400, 0), (0.22, 0.26, 0.04))
    a.box("Col_Hood", (0, 2.676, 0), (0.14, 0.032, 0.14))
    return a
