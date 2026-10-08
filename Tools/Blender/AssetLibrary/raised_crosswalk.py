"""Raised crosswalk. Replaces a 6 x 4 m road tile. Crown is 8 cm above the road."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from cabin import _prism, _prism_faces, _roof_box


@register
def create():
    a = Asset(
        "RaisedCrosswalk",
        "Roads",
        "Speed table, 6 m wide and 4 m long. Road top 0.12 m, crown 0.20 m, ramps 0.8 m. Stripes are paint.",
    )
    a.climb_note = "The crown is 8 cm above the road. Walk it. Not a cling wall."
    a.vault_note = "8 cm rise. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.06, 0), (6.0, 0.12, 4.0), "Lib_Asphalt", uv_scale=0.35)
        g.box((0, 0.16, 0), (6.0, 0.08, 2.2), "Lib_Asphalt", uv_scale=0.35)
        # Ramps: south rises toward +Z, north falls toward +Z.
        g.mesh(_prism(6.0, -2.0, 0.12, -1.1, 0.20, 0.08), _prism_faces(), "Lib_Asphalt", uv_scale=0.35)
        g.mesh(_prism(6.0, 2.0, 0.12, 1.1, 0.20, 0.08), _prism_faces(), "Lib_Asphalt", uv_scale=0.35)
        stripes = lod_pick(lod, 6, 4)
        for i in range(stripes):
            z = -0.9 + i * (1.8 / max(1, stripes - 1))
            g.box((0, 0.204, z), (5.4, 0.006, 0.16), "Lib_PaintWhite")
        a.end()
    a.box("Col_Slab", (0, 0.06, 0), (6.0, 0.12, 4.0))
    a.box("Col_Crown", (0, 0.16, 0), (5.7, 0.07, 2.0))
    center, size, euler = _roof_box(-2.0, 0.12, -1.1, 0.20, 0.08, 5.2)
    a.box("Col_RampS", center, size, euler=euler)
    center, size, euler = _roof_box(2.0, 0.12, 1.1, 0.20, 0.08, 5.2)
    a.box("Col_RampN", center, size, euler=euler)
    return a
