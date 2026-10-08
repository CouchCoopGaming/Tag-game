"""Small log cabin. 4.6 x 3.6 m, porch on +Z, gable roof."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Cabin",
        "Buildings",
        "Log cabin 4.6 x 3.6 m, walls 2.15 m, ridge at 3.45 m, porch on +Z. Door is closed.",
    )
    a.climbable = True
    a.climb_note = "Log walls are cling. Door is closed. Roof slopes are landings."
    a.vault_note = "Porch rail is 0.95 m above the porch deck (deck at 0.25 m, rail top at 1.20 m)."
    a.vaultable = True
    a.vault_height = 0.95
    width, depth = 4.6, 3.6
    for lod in (0, 1, 2):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6, 4)
        courses = lod_pick(lod, 9, 6, 3)
        log_r = 0.10
        # Corner posts so logs meet flush instead of spearing through each other.
        for x in (-width * 0.5, width * 0.5):
            for z in (-depth * 0.5, depth * 0.5):
                g.box((x, 1.05, z), (0.20, 2.10, 0.20), "Lib_WoodDark", bevel=0.004 if lod == 0 else 0, segs=1, uv_scale=1.0)
        span_x = width - 0.46
        span_z = depth - 0.46
        for i in range(courses):
            y = 0.18 + i * (1.85 / max(1, courses - 1))
            # Front logs stop at the door jambs below the header.
            if y < 1.85:
                g.cylinder((-1.28, y, depth * 0.5 - 0.12), log_r, 1.55, "Lib_WoodDark", seg, axis="X")
                g.cylinder((1.28, y, depth * 0.5 - 0.12), log_r, 1.55, "Lib_WoodDark", seg, axis="X")
            else:
                g.cylinder((0, y, depth * 0.5 - 0.12), log_r, span_x, "Lib_WoodDark", seg, axis="X")
            g.cylinder((0, y, -depth * 0.5 + 0.12), log_r, span_x, "Lib_Wood", seg, axis="X")
            g.cylinder((width * 0.5 - 0.12, y, 0), log_r, span_z, "Lib_Wood", seg, axis="Z")
            g.cylinder((-width * 0.5 + 0.12, y, 0), log_r, span_z, "Lib_Wood", seg, axis="Z")
        g.box((0, 1.05, depth * 0.5 - 0.08), (0.90, 1.85, 0.06), "Lib_Wood", uv_scale=1.2)
        if lod == 0:
            g.box((0.28, 1.00, depth * 0.5 - 0.04), (0.04, 0.08, 0.04), "Lib_Brass")
        # Porch deck and roof.
        g.box((0, 0.12, depth * 0.5 + 0.7), (3.2, 0.10, 1.3), "Lib_Wood", bevel=0.004 if lod == 0 else 0, segs=1, uv_scale=1.0)
        for x in (-1.3, 1.3):
            g.box((x, 1.15, depth * 0.5 + 1.25), (0.12, 2.05, 0.12), "Lib_WoodDark")
        g.box((0, 2.25, depth * 0.5 + 0.7), (3.4, 0.08, 1.5), "Lib_Roof", uv_scale=1.0)
        if lod < 2:
            g.pipe((-1.3, 0.95, depth * 0.5 + 1.25), (1.3, 0.95, depth * 0.5 + 1.25), 0.03, "Lib_WoodDark", 6)
        # Gable roof slabs.
        _roof(g, width + 0.4, lod)
        a.end()
    a.loose_pivot = True
    a.box("Climb_FrontL", (-1.28, 1.05, depth * 0.5 - 0.12), (1.50, 1.65, 0.14))
    a.box("Climb_FrontR", (1.28, 1.05, depth * 0.5 - 0.12), (1.50, 1.65, 0.14))
    a.box("Climb_FrontHead", (0, 2.02, depth * 0.5 - 0.12), (3.6, 0.16, 0.12))
    a.box("Climb_Back", (0, 1.05, -depth * 0.5 + 0.12), (width - 0.5, 1.9, 0.20))
    a.box("Climb_SideL", (-width * 0.5 + 0.12, 1.05, 0), (0.20, 1.9, depth - 0.5))
    a.box("Climb_SideR", (width * 0.5 - 0.12, 1.05, 0), (0.20, 1.9, depth - 0.5))
    a.box("Col_Door", (0, 1.05, depth * 0.5 - 0.08), (0.90, 1.85, 0.06))
    a.box("Col_Porch", (0, 0.12, depth * 0.5 + 0.7), (3.2, 0.10, 1.3))
    a.box("Col_PorchRoof", (0, 2.25, depth * 0.5 + 0.7), (3.4, 0.08, 1.5))
    a.capsule("Vault_PorchRail", (0, 0.95, depth * 0.5 + 1.25), 0.03, 2.6, 0)
    # Roof colliders sit inside the slabs. Pivot is the cabin floor, porch is +Z.
    _add_roof(a, width + 0.4, -2.05, 2.15, 0.0, 3.45, 0.06)
    return a


def _roof(g, width, lod):
    g.mesh(_prism(width, -2.05, 2.15, 0.0, 3.45, 0.06), _prism_faces(), "Lib_Roof", uv_scale=1.0)
    g.mesh(_prism(width, 2.05, 2.15, 0.0, 3.45, 0.06), _prism_faces(), "Lib_Roof", uv_scale=1.0)


def _add_roof(asset, width, z0, y0, z1, y1, thick):
    center, size, euler = _roof_box(z0, y0, z1, y1, thick, width)
    asset.box("Col_RoofS", center, size, euler=euler)
    center, size, euler = _roof_box(z0 * -1.0, y0, z1 * -1.0 if z1 else 0.0, y1, thick, width)
    # North eave is the mirrored z. _roof builds the second prism from +z0.
    asset.box("Col_RoofN", center, size, euler=euler)


def _roof_box(z0, y0, z1, y1, thick, width):
    dz, dy = (z1 - z0), (y1 - y0)
    length = math.hypot(dz, dy) or 1.0
    nz, ny = -dy / length, dz / length
    if ny < 0:
        nz, ny = -nz, -ny
    cz = (z0 + z1) * 0.5 + nz * thick * 0.45
    cy = (y0 + y1) * 0.5 + ny * thick * 0.45
    angle = math.degrees(math.atan2(abs(dy), abs(dz)))
    pitch = -angle if dz * dy > 0 else angle
    return (0.0, cy, cz), (width * 0.82, thick * 0.5, length * 0.70), (pitch, 0.0, 0.0)


def _prism(width, z0, y0, z1, y1, thick):
    dz, dy = (z1 - z0), (y1 - y0)
    length = math.hypot(dz, dy) or 1.0
    # Normal in the ZY plane pointing "up-ish".
    nz, ny = -dy / length, dz / length
    if ny < 0:
        nz, ny = -nz, -ny
    xs = (-width * 0.5, width * 0.5)
    verts = []
    for x in xs:
        for z, y in ((z0, y0), (z1, y1)):
            verts.append((x, y, z))
    for x in xs:
        for z, y in ((z0, y0), (z1, y1)):
            verts.append((x, y + ny * thick, z + nz * thick))
    return verts


def _prism_faces():
    # 0,1 bottom edge at x0; 2,3 at x1; 4..7 top.
    return [
        (0, 2, 3, 1),
        (4, 5, 7, 6),
        (0, 1, 5, 4),
        (2, 6, 7, 3),
        (0, 4, 6, 2),
        (1, 3, 7, 5),
    ]
