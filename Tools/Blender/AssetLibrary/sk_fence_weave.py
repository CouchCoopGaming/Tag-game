"""Chain-link bay with a diamond of wires. 8 ft between post centers.

A dark sheet sits behind the wires so the bay has a solid for the collider.
The wires are round tubes, not a twisted knuckle weave.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _clip(x0, y0, dx, dy, bounds):
    xmin, xmax, ymin, ymax = bounds
    ts = []
    if abs(dx) > 1e-9:
        ts.append((xmin - x0) / dx)
        ts.append((xmax - x0) / dx)
    if abs(dy) > 1e-9:
        ts.append((ymin - y0) / dy)
        ts.append((ymax - y0) / dy)
    hits = []
    for t in ts:
        x = x0 + dx * t
        y = y0 + dy * t
        if xmin - 1e-3 <= x <= xmax + 1e-3 and ymin - 1e-3 <= y <= ymax + 1e-3:
            key = (round(x, 4), round(y, 4))
            if key not in hits:
                hits.append(key)
    best = None
    best_d = 0.04
    for i in range(len(hits)):
        for j in range(i + 1, len(hits)):
            d = (hits[i][0] - hits[j][0]) ** 2 + (hits[i][1] - hits[j][1]) ** 2
            if d > best_d:
                best_d = d
                best = (hits[i], hits[j])
    return best


def _wires(g, z, direction):
    # About a 2.4 in diamond. Tubes stay proud of the sheet and clear of the collider.
    bounds = (-1.00, 1.00, 0.16, 1.74)
    spacing = 0.06
    dx, dy = direction
    perp = (dy, -dx)
    scale = spacing / math.hypot(*perp)
    for i in range(-48, 49):
        ox = perp[0] * scale * i
        oy = perp[1] * scale * i
        seg = _clip(ox, oy, dx, dy, bounds)
        if not seg:
            continue
        (x0, y0), (x1, y1) = seg
        g.pipe((x0, y0, z), (x1, y1, z), 0.0035, "Lib_Chain", 4)


def _post(g, x, lod):
    seg = lod_pick(lod, 8, 6)
    g.cylinder((x, 0.98, 0), 0.030, 1.96, "Lib_Steel", seg)
    g.cylinder((x, 0.03, 0), 0.06, 0.06, "Lib_Concrete", 8)
    g.sphere((x, 1.98, 0), 0.038, "Lib_Steel", seg)


@register
def create():
    a = Asset(
        "Fence_ChainWeave",
        "StreetFurniture",
        "Chain-link bay 2.44 m between posts, top rail at 1.83 m. Diamond is about 60 mm. A dark sheet backs the wires.",
    )
    a.climb_note = "Wires are not a solid cling. The sheet blocks passage."
    a.vault_note = "Top rail is 1.83 m."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        for x in (-1.22, 1.22):
            _post(g, x, lod)
        g.cylinder((0.0, 1.78, 0), 0.016, 2.48, "Lib_Steel", seg, axis="X")
        g.cylinder((0.0, 0.12, 0), 0.012, 2.48, "Lib_Steel", 6, axis="X")
        # Sheet is the solid. Wires bite its front face and stay out of the collider.
        g.box((0, 0.95, 0), (2.00, 1.58, 0.012), "Lib_SteelDark")
        if lod == 0:
            _wires(g, 0.0075, (1.0, 1.0))
            _wires(g, 0.0105, (1.0, -1.0))
        a.end()
    a.capsule("Col_PostL", (-1.22, 0.95, 0), 0.018, 1.40)
    a.capsule("Col_PostR", (1.22, 0.95, 0), 0.018, 1.40)
    # Back half of the sheet, clear of the wires.
    a.box("Col_Mesh", (0, 0.95, -0.0035), (1.70, 1.20, 0.003))
    # Back of the top rail, between the posts, under the crown.
    a.box("Col_Rail", (0, 1.784, -0.004), (1.90, 0.014, 0.008))
    return a
