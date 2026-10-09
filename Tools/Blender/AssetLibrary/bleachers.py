"""Four-row park bleachers. Seats and footboards are the surfaces."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from _nudge import shift_z


ROWS = 4
LENGTH = 6.0


@register
def create():
    a = Asset(
        "Bleachers",
        "Park",
        "Four-row aluminum bleachers, 6 m long. Seat tops run from 0.48 m up to 1.56 m. "
        "A rail closes the back and the two ends.",
    )
    a.climb_note = "Seats and footboards are the surfaces. The frame is not a cling wall."
    a.vault_note = "The front footboard is under 0.40 m. Not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        _rows(g, lod)
        _frame(g, lod)
        a.end()
    for i in range(ROWS):
        seat_y, seat_z, foot_y, foot_z = _row(i)
        a.box("Col_Seat_%d" % i, (0.0, seat_y, seat_z), (LENGTH - 0.90, 0.02, 0.18))
        a.box("Col_Foot_%d" % i, (0.0, foot_y, foot_z), (LENGTH - 0.90, 0.02, 0.26))
    a.box("Col_Rail", (0.0, 1.85, -2.55), (LENGTH - 0.30, 0.70, 0.06), approx=True)
    shift_z(a, 0.50)
    return a


def _row(i):
    seat_y = 0.48 + i * 0.36
    seat_z = -0.15 - i * 0.72
    foot_y = seat_y - 0.22
    foot_z = seat_z + 0.40
    return seat_y, seat_z, foot_y, foot_z


def _rows(g, lod):
    n = lod_pick(lod, ROWS, ROWS, 3)
    for i in range(n):
        seat_y, seat_z, foot_y, foot_z = _row(i if n == ROWS else i)
        # LOD2 keeps the first three rows, which still read as bleachers.
        g.box((0.0, seat_y, seat_z), (LENGTH - 0.70, 0.04, 0.28), "Lib_Steel", uv_scale=0.5)
        g.box((0.0, foot_y, foot_z), (LENGTH - 0.70, 0.04, 0.36), "Lib_SteelDark", uv_scale=0.5)


def _frame(g, lod):
    for x in (-2.85, 2.85):
        g.box((x, 0.96, -1.20), (0.08, 1.60, 0.08), "Lib_SteelDark")
        g.box((x, 0.46, 0.15), (0.08, 0.60, 0.08), "Lib_SteelDark")
        g.box((x, 0.06, -0.50), (0.16, 0.08, 2.40), "Lib_Concrete", uv_scale=0.6)
    g.box((0.0, 0.70, -2.35), (LENGTH - 1.00, 0.08, 0.08), "Lib_SteelDark")
    if lod == 2:
        g.box((0.0, 1.85, -2.55), (LENGTH - 0.10, 0.08, 0.06), "Lib_Steel")
        return
    g.box((0.0, 2.05, -2.55), (LENGTH - 0.10, 0.06, 0.06), "Lib_Steel")
    if lod == 0:
        for i in range(7):
            x = -2.4 + i * 0.8
            g.box((x, 1.62, -2.55), (0.04, 0.52, 0.04), "Lib_Steel")
    for x in (-3.05, 3.05):
        g.box((x, 1.15, -1.15), (0.05, 1.10, 0.05), "Lib_SteelDark")
