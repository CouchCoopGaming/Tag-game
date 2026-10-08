"""Red dry-barrel hydrant. Same body as the yellow one, hydrant paint, brass caps."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from sk_parts import add_hydrant, hydrant_colliders


@register
def create():
    a = Asset(
        "FireHydrant_Red",
        "StreetFurniture",
        "Red dry-barrel hydrant, 0.79 m to the operating nut. Brass nozzles and side chains.",
    )
    a.climb_note = "Round barrel under 0.8 m. Not a cling wall."
    a.vault_note = "Too short and too narrow to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        add_hydrant(g, lod, "Lib_Hydrant", "Lib_SteelDark", "Lib_Brass", band="Lib_PaintWhite", wheel=False)
        a.end()
    hydrant_colliders(a, wheel=False)
    return a
