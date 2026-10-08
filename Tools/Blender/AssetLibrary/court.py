"""Street full court, 22 m by 12 m.

Both ends use a real NCAA lane (3.66 m) and free-throw (5.79 m). The three-point
arc is 6.75 m. A 1.80 m center circle fits between the free-throw lines.
Paint has no collider. Hoop rims face center court.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

W, L = 12.0, 22.0


def _line(g, center, size, yaw=0.0):
    g.box(center, size, "Lib_PaintWhite", euler=(0, yaw, 0))


def _arc(g, z_center, radius, half_ang, steps, y, sign=1.0):
    for i in range(steps):
        a0 = -half_ang + (2 * half_ang) * (i / steps)
        a1 = -half_ang + (2 * half_ang) * ((i + 1) / steps)
        am = (a0 + a1) * 0.5
        x = math.sin(am) * radius
        z = z_center + sign * math.cos(am) * radius
        chord = radius * abs(a1 - a0)
        _line(g, (x, y, z), (0.05, 0.004, chord * 1.08), math.degrees(am))


def _end(g, baseline, sign, y, y_key, steps, ft_steps):
    """sign +1 paints the south basket (key runs toward +Z)."""
    ft_z = baseline + sign * 5.79
    rim_z = baseline + sign * 1.575
    lane = 3.66
    g.box((0, y_key, (baseline + ft_z) * 0.5), (lane, 0.004, abs(ft_z - baseline)), "Lib_Court")
    _line(g, (0, y, ft_z), (lane, 0.004, 0.05))
    _line(g, (-lane * 0.5, y, (baseline + ft_z) * 0.5), (0.05, 0.004, abs(ft_z - baseline)))
    _line(g, (lane * 0.5, y, (baseline + ft_z) * 0.5), (0.05, 0.004, abs(ft_z - baseline)))
    _arc(g, rim_z, 6.75, math.asin(5.2 / 6.75), steps, y, sign)
    _arc(g, ft_z, 1.80, math.pi * 0.5, ft_steps, y, sign)
    _arc(g, rim_z, 1.25, math.pi * 0.5, max(4, ft_steps // 2), y, sign)


@register
def create():
    a = Asset(
        "Court",
        "Park",
        "Street full court, 22 m by 12 m. Both ends have a 3.66 m lane, a 5.79 m free-throw, and a 6.75 m arc. Paint has no collider.",
    )
    a.climb_note = "Flat slab, 0.12 m thick."
    a.vault_note = "No rail. Place each Hoop at the baseline with the rim facing center court."
    y_key = 0.123
    y = 0.127
    south = -L * 0.5 + 0.45
    north = L * 0.5 - 0.45
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.06, 0), (W, 0.12, L), "Lib_Asphalt", bevel=0.008 if lod == 0 else 0, segs=1, uv_scale=0.35)
        _line(g, (0, y, south), (W - 0.9, 0.004, 0.05))
        _line(g, (0, y, north), (W - 0.9, 0.004, 0.05))
        _line(g, (-W * 0.5 + 0.45, y, 0), (0.05, 0.004, L - 0.9))
        _line(g, (W * 0.5 - 0.45, y, 0), (0.05, 0.004, L - 0.9))
        _line(g, (0, y, 0), (W - 0.9, 0.004, 0.05))
        steps = lod_pick(lod, 16, 8)
        ft_steps = lod_pick(lod, 8, 4)
        _end(g, south, 1.0, y, y_key, steps, ft_steps)
        _end(g, north, -1.0, y, y_key, steps, ft_steps)
        _arc(g, 0.0, 1.80, math.pi, steps, y, 1.0)
        a.end()
    a.box("Col_Slab", (0, 0.06, 0), (W, 0.12, L))
    return a
