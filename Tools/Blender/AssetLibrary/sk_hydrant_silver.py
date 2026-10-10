"""Industrial yard hydrant. Silver barrel, black caps, handwheel on top."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from sk_parts import add_hydrant, hydrant_colliders


@register
def create():
    a = Asset(
        "FireHydrant_Silver",
        "StreetFurniture",
        "Yard hydrant, 0.81 m to the handwheel. Worn silver barrel, black caps, brass nozzles.",
    )
    a.climb_note = "Round barrel under 0.85 m. Not a cling wall."
    a.vault_note = "Too short and too narrow to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        add_hydrant(g, lod, "Lib_MetalWorn", "Lib_Black", "Lib_Brass", band=None, wheel=True)
        a.end()
    hydrant_colliders(a, wheel=True)
    return a
