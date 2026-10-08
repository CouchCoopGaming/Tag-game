"""Sidewalk collection mailbox. No legend, no eagle, no brand.

About 0.54 m wide, 0.46 m deep, 1.18 m to the hood. Hopper lip, deposit
slot, and a carrier door with a lock and a pull. The blank panel is empty.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _hood(g, lod):
    """Sloped hopper. Front lip drops over the slot. Sides stay inside the bevel."""
    bev = lod_pick(lod, 0.003, 0.0)
    bs = 1 if lod == 0 else 0
    g.box((0, 1.05, -0.01), (0.56, 0.14, 0.48), "Lib_BoxBlue", bevel=bev, segs=bs)
    g.box((0, 1.10, -0.02), (0.58, 0.035, 0.56), "Lib_BoxBlue", bevel=bev, segs=bs, euler=(12, 0, 0))
    g.box((0, 1.01, 0.27), (0.56, 0.06, 0.08), "Lib_BoxBlue", bevel=bev, segs=bs)


def _door(g, lod):
    """Carrier door, pull, and lock. The upper plate is a blank frame."""
    bev = lod_pick(lod, 0.002, 0.0)
    bs = 1 if lod == 0 else 0
    seg = lod_pick(lod, 10, 6)
    g.box((0, 0.50, 0.232), (0.36, 0.56, 0.016), "Lib_SteelDark", bevel=bev, segs=bs)
    g.cylinder((-0.168, 0.66, 0.242), 0.010, 0.016, "Lib_Steel", seg, axis="Z")
    g.cylinder((-0.168, 0.34, 0.242), 0.010, 0.016, "Lib_Steel", seg, axis="Z")
    g.cylinder((0, 0.32, 0.252), 0.012, 0.16, "Lib_Steel", seg, axis="X")
    g.cylinder((-0.09, 0.32, 0.246), 0.008, 0.02, "Lib_Steel", seg, axis="Z")
    g.cylinder((0.09, 0.32, 0.246), 0.008, 0.02, "Lib_Steel", seg, axis="Z")
    g.cylinder((0.10, 0.68, 0.246), 0.016, 0.018, "Lib_Brass", seg, axis="Z")
    if lod != 0:
        return
    g.cylinder((0.10, 0.68, 0.258), 0.006, 0.006, "Lib_Black", 8, axis="Z")
    # Empty frame. No wordmark.
    g.box((0, 0.78, 0.242), (0.18, 0.012, 0.006), "Lib_Steel")
    g.box((0, 0.64, 0.242), (0.18, 0.012, 0.006), "Lib_Steel")
    g.box((-0.084, 0.71, 0.242), (0.012, 0.15, 0.006), "Lib_Steel")
    g.box((0.084, 0.71, 0.242), (0.012, 0.15, 0.006), "Lib_Steel")


@register
def create():
    a = Asset(
        "MailDrop_Corner",
        "StreetFurniture",
        "Collection box, 0.54 m wide, 0.46 m deep, hood at 1.18 m. Slot, carrier door, blank frame. No legend.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = 1 if lod == 0 else 0
        seg = lod_pick(lod, 8, 6)
        g.box((0, 0.016, 0), (0.62, 0.032, 0.54), "Lib_SteelDark", bevel=bev, segs=bs)
        for x in (-0.24, 0.24):
            for z in (-0.20, 0.20):
                g.cylinder((x, 0.012, z), 0.018, 0.024, "Lib_Steel", seg)
        # Body sleeves the base plate. Top meets the hopper.
        g.box((0, 0.52, 0), (0.52, 1.00, 0.44), "Lib_BoxBlue", bevel=bev, segs=bs)
        for x in (-0.255, 0.255):
            g.box((x, 0.52, 0), (0.018, 0.96, 0.016), "Lib_SteelDark")
        _hood(g, lod)
        # Deposit slot under the lip, with a flap that sits just open.
        g.box((0, 0.97, 0.228), (0.34, 0.040, 0.020), "Lib_Black")
        g.box((0, 1.005, 0.246), (0.38, 0.050, 0.012), "Lib_SteelDark", euler=(10, 0, 0))
        _door(g, lod)
        if lod == 0:
            g.box((0.22, 0.22, 0.222), (0.08, 0.10, 0.006), "Lib_MetalWorn")
            g.box((-0.18, 0.18, 0.222), (0.05, 0.04, 0.006), "Lib_Rust")
        a.end()
    a.box("Col_Base", (0, 0.014, 0), (0.48, 0.020, 0.40))
    # Body core, clear of the door, the slot, and the hood overlap.
    a.box("Col_Body", (0, 0.50, -0.02), (0.40, 0.76, 0.28))
    # Between the body crown and the pitched roof, inside the hopper only.
    a.box("Col_Hood", (0, 1.045, -0.06), (0.32, 0.030, 0.16))
    return a
