"""Shared asphalt grid for the street-object roads.

Lanes are 3 m. Straight tiles are 4 m along Z. The driving surface is at 0.12 m,
the same socket as the older Road_Straight tile. Paint sits in the top centimetre
so a slab collider can hug the asphalt without sharing volume with the stripes.
"""

ROAD_TOP = 0.12
TILE_L = 4.0
LANE = 3.0


def asphalt(g, lod, width, length, wear=True):
    g.box((0, ROAD_TOP * 0.5, 0), (width, ROAD_TOP, length), "Lib_Asphalt", bevel=0, uv_scale=0.35)
    if lod != 0 or not wear:
        return
    g.box(
        (width * 0.16, ROAD_TOP + 0.001, length * 0.12),
        (width * 0.18, 0.006, length * 0.22),
        "Lib_AsphaltWear",
    )
    g.box(
        (-width * 0.14, ROAD_TOP + 0.001, -length * 0.16),
        (0.55, 0.006, 0.42),
        "Lib_AsphaltPatch",
    )


def paint(g, x, z, sx, sz, mat="Lib_PaintWhite"):
    """Stripe overlapping the asphalt by 2 mm and standing 6 mm proud."""
    g.box((x, ROAD_TOP + 0.002, z), (sx, 0.008, sz), mat)


def edge_lines(g, width, length):
    half = width * 0.5
    paint(g, -half + 0.20, 0, 0.12, length)
    paint(g, half - 0.20, 0, 0.12, length)


def dashes(g, x, length, mat="Lib_Lane"):
    dash, gap = 0.90, 1.10
    period = dash + gap
    z = -length * 0.5 + (gap + dash) * 0.5
    while z < length * 0.5 - dash * 0.35:
        paint(g, x, z, 0.12, dash, mat)
        z += period


def double_yellow(g, length):
    paint(g, -0.10, 0, 0.10, length, "Lib_Lane")
    paint(g, 0.10, 0, 0.10, length, "Lib_Lane")


def zebra(g, along, at, bars, bar_len, span):
    pitch = span / bars
    width = min(0.45, pitch * 0.55)
    for i in range(bars):
        t = -span * 0.5 + pitch * (i + 0.5)
        if along == "z":
            paint(g, t, at, width, bar_len)
        else:
            paint(g, at, t, bar_len, width)


def arrow_straight(g, x, z):
    paint(g, x, z, 0.18, 1.20)
    paint(g, x, z + 0.55, 0.16, 0.70)
    g.box((x, ROAD_TOP + 0.002, z + 0.62), (0.16, 0.008, 0.58), "Lib_PaintWhite", euler=(0, 36, 0))
    g.box((x, ROAD_TOP + 0.002, z + 0.62), (0.16, 0.008, 0.58), "Lib_PaintWhite", euler=(0, -36, 0))


def arrow_right(g, x, z):
    paint(g, x, z - 0.10, 0.18, 0.95)
    paint(g, x + 0.38, z + 0.32, 0.78, 0.18)
    g.box((x + 0.68, ROAD_TOP + 0.002, z + 0.32), (0.16, 0.008, 0.52), "Lib_PaintWhite", euler=(0, 40, 0))
    g.box((x + 0.68, ROAD_TOP + 0.002, z + 0.32), (0.16, 0.008, 0.52), "Lib_PaintWhite", euler=(0, -40, 0))


def slab_collider(a, width, length, name="Col_Slab"):
    """Top is 8 mm under the asphalt crown and clear of the paint skin."""
    a.box(name, (0, 0.056, 0), (width - 0.06, 0.112, length - 0.06))
