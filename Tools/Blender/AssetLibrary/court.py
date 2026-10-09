"""Street full court, 22 m by 15 m.

Paint is one decal. FIBA distances are measured from the hoop rims at
z = ±9.425. The slab is a standard 15 m wide, so the three-point corner
line sits 0.90 m in from the sideline. Every line is 5 cm.
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
        "Street full court, 22 m by 15 m. One decal carries FIBA paint measured from the rims: "
        "a 4.90 m key, a free-throw circle solid toward center court and dashed inside the lane, "
        "a restricted arc, and a 6.75 m three-point arc on the rim. The surface is even acrylic "
        "with light wear in the keys and at center court. Lines are 5 cm. No collider on the paint.",
    )
    a.climb_note = "Flat slab, 0.12 m thick."
    a.vault_note = "No rail. Place each Hoop so the backboard face is 1.2 m inside the baseline."
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
