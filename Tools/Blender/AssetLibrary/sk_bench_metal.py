"""All-metal slat bench. Seat top at 0.45 m. Tubular frame, no wood."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import polyline, span_collider


def _back_z(y):
    return -0.18 + (y - 0.40) / 0.44 * (-0.12)


@register
def create():
    a = Asset(
        "Bench_Metal",
        "StreetFurniture",
        "1.80 m metal bench. Eight seat slats and five back slats on a tubular frame. Seat top 0.45 m.",
    )
    a.climb_note = "Slats are too broken up to cling. Not a wall-run panel."
    a.vault_note = "Seat top is 0.45 m. Below the 0.90–1.05 m vault band."
    xs = (-0.72, 0.72)
    for lod in (0, 1):
        g = a.begin(lod)
        frame = "Lib_Black"
        slat_mat = "Lib_MetalWorn"
        seg = lod_pick(lod, 8, 6)
        seat_n = lod_pick(lod, 8, 4)
        back_n = lod_pick(lod, 5, 3)
        for x in xs:
            g.box((x, 0.012, -0.18), (0.10, 0.024, 0.12), frame, bevel=0.003 if lod == 0 else 0, segs=1)
            g.box((x, 0.012, 0.20), (0.10, 0.024, 0.12), frame, bevel=0.003 if lod == 0 else 0, segs=1)
            polyline(g, [
                (x, 0.03, -0.18),
                (x, 0.40, -0.18),
                (x, 0.40, 0.20),
                (x, 0.03, 0.20),
            ], 0.016, frame, seg)
            polyline(g, [
                (x, 0.40, -0.18),
                (x, 0.84, -0.30),
            ], 0.016, frame, seg)
            polyline(g, [
                (x, 0.62, 0.16),
                (x, 0.66, 0.0),
                (x, 0.70, -0.22),
            ], 0.014, frame, seg)
        polyline(g, [(-0.72, 0.40, -0.18), (0.72, 0.40, -0.18)], 0.014, frame, seg)
        polyline(g, [(-0.72, 0.40, 0.20), (0.72, 0.40, 0.20)], 0.014, frame, seg)
        polyline(g, [(-0.72, 0.16, 0.0), (0.72, 0.16, 0.0)], 0.012, frame, seg)
        for i in range(seat_n):
            z = -0.12 + i * (0.30 / max(1, seat_n - 1))
            g.box((0, 0.438, z), (1.64, 0.018, 0.022), slat_mat, bevel=0.002 if lod == 0 else 0, segs=1, uv_scale=1.4)
            if lod == 0:
                for x in xs:
                    g.cylinder((x, 0.438, z), 0.006, 0.012, "Lib_Steel", 6, axis="Z")
        for i in range(back_n):
            t = (i + 0.5) / back_n
            y = 0.52 + t * 0.28
            g.box((0, y, _back_z(y)), (1.64, 0.020, 0.016), slat_mat, bevel=0.002 if lod == 0 else 0, segs=1, uv_scale=1.4)
        a.end()
    for i, x in enumerate(xs):
        a.box("Col_FootA_%d" % i, (x, 0.012, -0.18), (0.09, 0.020, 0.10))
        a.box("Col_FootB_%d" % i, (x, 0.012, 0.20), (0.09, 0.020, 0.10))
        a.capsule("Col_LegA_%d" % i, (x, 0.215, -0.18), 0.014, 0.36, 1)
        a.capsule("Col_LegB_%d" % i, (x, 0.215, 0.20), 0.014, 0.36, 1)
        a.box("Col_Rail_%d" % i, (x, 0.40, 0.01), (0.020, 0.020, 0.34))
        center, size, euler = span_collider((x, 0.40, -0.18), (x, 0.84, -0.30), 0.012)
        a.box("Col_Back_%d" % i, center, size, euler=euler)
    for i in range(8):
        z = -0.12 + i * (0.30 / 7.0)
        a.box("Col_Seat_%d" % i, (0, 0.438, z), (1.60, 0.014, 0.018))
    for i in range(5):
        t = (i + 0.5) / 5.0
        y = 0.52 + t * 0.28
        a.box("Col_BackSlat_%d" % i, (0, y, _back_z(y)), (1.60, 0.016, 0.012))
    return a
