"""Two bollards with a sagging chain. Eyes at 0.62 m, posts 1.50 m apart."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import chain


def _post(g, x, lod, seg, bev):
    g.cylinder((x, 0.015, 0), 0.12, 0.03, "Lib_SteelDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
    g.cylinder((x, 0.42, 0), 0.055, 0.78, "Lib_Black", seg, bevel=bev, segs=1 if lod == 0 else 0)
    g.sphere((x, 0.84, 0), 0.055, "Lib_Black", seg)
    g.torus((x, 0.62, 0), 0.07, 0.008, "Lib_Steel", max(8, seg), 5)


@register
def create():
    a = Asset(
        "Bollard_Chain",
        "StreetFurniture",
        "Pair of bollards, 0.90 m tall, 1.50 m apart. Chain sags between eyes at 0.62 m.",
    )
    a.climb_note = "Posts are too narrow to cling. The chain is not a rail."
    a.vault_note = "Tops are 0.90 m and round. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 14, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        for x in (-0.75, 0.75):
            _post(g, x, lod, seg, bev)
        if lod == 0:
            chain(g, (-0.68, 0.62, 0), (0.68, 0.62, 0), n=8, sag=0.10, radius=0.006)
        else:
            chain(g, (-0.68, 0.62, 0), (0.68, 0.62, 0), n=3, sag=0.10, radius=0.006)
        a.end()
    for i, x in enumerate((-0.75, 0.75)):
        a.capsule("Col_Post_%d" % i, (x, 0.42, 0), 0.048, 0.78, 1)
        a.box("Col_Base_%d" % i, (x, 0.015, 0), (0.18, 0.024, 0.18))
    return a
