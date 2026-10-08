"""Street half-court. Asphalt slab, NCAA-width lane, and a 6.25 m arc.

The slab stays 10 m wide (X) by 14 m long (Z). Lines are paint and have no collider.
Place Hoop just outside the -Z baseline; the rim then hangs just inside the end line.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _line(g, center, size, yaw=0.0):
    g.box(center, size, "Lib_PaintWhite", euler=(0, yaw, 0))


def _arc(g, z_center, radius, half_ang, steps, y):
    for i in range(steps):
        a0 = -half_ang + (2 * half_ang) * (i / steps)
        a1 = -half_ang + (2 * half_ang) * ((i + 1) / steps)
        am = (a0 + a1) * 0.5
        x = math.sin(am) * radius
        z = z_center + math.cos(am) * radius
        chord = radius * abs(a1 - a0)
        _line(g, (x, y, z), (0.08, 0.004, chord * 1.05), math.degrees(am))


@register
def create():
    a = Asset(
        "Court",
        "Park",
        "Street half-court, 14 m long by 10 m wide. Asphalt with a 3.66 m lane, 5.79 m free-throw, and a 6.25 m arc. Paint has no collider.",
    )
    a.climb_note = "Flat slab, 0.12 m thick."
    a.vault_note = "No rail. Place Hoop just outside the -Z baseline so the 3.05 m rim hangs inside the end line."
    y_key = 0.123
    y = 0.127
    baseline = -6.55
    rim_z = -6.40
    ft_z = baseline + 5.79
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.06, 0), (10.0, 0.12, 14.0), "Lib_Asphalt", bevel=0.008 if lod == 0 else 0, segs=1, uv_scale=0.35)
        # Colored key, under the white lines.
        g.box((0, y_key, (baseline + ft_z) * 0.5), (3.66, 0.004, ft_z - baseline), "Lib_Court")
        _line(g, (0, y, baseline), (9.1, 0.004, 0.08))
        _line(g, (0, y, 6.55), (9.1, 0.004, 0.08))
        _line(g, (-4.55, y, 0), (0.08, 0.004, 13.1))
        _line(g, (4.55, y, 0), (0.08, 0.004, 13.1))
        _line(g, (0, y, ft_z), (3.66, 0.004, 0.08))
        _line(g, (-1.83, y, (baseline + ft_z) * 0.5), (0.08, 0.004, ft_z - baseline))
        _line(g, (1.83, y, (baseline + ft_z) * 0.5), (0.08, 0.004, ft_z - baseline))
        steps = lod_pick(lod, 14, 8)
        _arc(g, rim_z, 6.25, math.asin(4.45 / 6.25), steps, y)
        _arc(g, ft_z, 1.80, math.pi * 0.5, lod_pick(lod, 8, 4), y)
        a.end()
    a.box("Col_Slab", (0, 0.06, 0), (10.0, 0.12, 14.0))
    return a
