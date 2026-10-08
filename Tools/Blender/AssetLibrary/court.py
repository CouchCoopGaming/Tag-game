"""Street full court, 22 m by 12 m.

Paint is one decal: FIBA markings scaled onto this slab. The boundary is the
slab edge. Lengths scale by 22/28 and widths by 12/15. Every line is 5 cm.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from _court_decal import L, W


@register
def create():
    a = Asset(
        "Court",
        "Park",
        "Street full court, 22 m by 12 m. One decal carries the FIBA paint scaled to the slab: "
        "boundary, center line, center circle, and at each end a lane, free-throw circle, "
        "restricted arc, and a 3-point arc that meets the corner lines. Lines are 5 cm. No collider on the paint.",
    )
    a.climb_note = "Flat slab, 0.12 m thick."
    a.vault_note = "No rail. Place each Hoop at the baseline with the rim facing center court."
    hw, hl = W * 0.5, L * 0.5
    y = 0.126
    verts = [
        (-hw, y, -hl),
        (hw, y, -hl),
        (hw, y, hl),
        (-hw, y, hl),
    ]
    uvs = [(0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0)]
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.06, 0), (W, 0.12, L), "Lib_Asphalt", uv_scale=0.35)
        # Upward face. UV (0, 0) is the south-west corner, matching the decal.
        g.quad(verts, [(0, 3, 2, 1)], uvs, "Lib_CourtDecal")
        a.end()
    a.box("Col_Slab", (0, 0.06, 0), (W, 0.12, L))
    return a
