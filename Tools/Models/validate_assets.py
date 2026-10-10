#!/usr/bin/env python3
"""Check Tag model assets against Docs/Models/STANDARD.md.

    python3 Tools/Models/validate_assets.py --root <checkout>
    blender --background --python Tools/Models/validate_assets.py -- --root <checkout>

Prints one line per asset and a summary:

    models-validate assets=N pass=P fail=F

Slack is recomputed from the FBX when Blender's bpy is importable. Otherwise
the manifest field slackCm, written by validate_colliders(tolerance=0.03),
is the measurement. A missing measurement fails.
"""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
import os
import re
import struct
import subprocess
import sys
import zlib
from collections import defaultdict


SLACK_LIMIT_CM = 3.0
LAND_GAP_M = 0.08
PIVOT_CENTER_M = 0.05
BELOW_M = -0.02
FLOAT_M = 0.03
STILL_MIN = (1280, 720)
STILL_MAX_BYTES = 400 * 1024
# Hero, side, and scale. The silhouette stays off the frame edge and its
# box covers this fraction of the frame. Close-ups are allowed to crop.
FRAME_AREA = (0.25, 0.85)
FRAME_LONG = 160
# Showcase figure. Authored albedo is linear. The pixel tests below are the lit values.
FIGURE_FBX = "Assets/Art/Props/Library/Showcase/Mannequin.fbx"
FIGURE_BLUE = (0.239, 0.494, 1.0)  # Lib_PaintBlue, 8-bit 61, 126, 255
FIGURE_WHITE = (0.93, 0.93, 0.90)  # Lib_PaintWhite, 8-bit 237, 237, 230
FIGURE_HEIGHT_M = 1.80
FIGURE_NAMES = {
    "Mannequin",
    "Mannequin.fbx",
    FIGURE_FBX,
    "Dummy_Mannequin_Tan_Hier_Hi",
    "Dummy_Mannequin_Tan_Hier_Hi.fbx",
}
PROVENANCE_LINE = "made in-house, CC0, free to use"
FIGURE_H = (1.75, 1.85)
YEAR_MIN, YEAR_MAX = 2022, 2026
NOCLIP_CM = 0.5
HIP_PLANT_M = 0.08
HIP_CROUCH_M = 0.12
HIP_FLEX_RATIO = 1.5
# A dressed segment must keep this much of its rest surface under cloth.
# The cloth band is 0.3–1.0 cm outside the hull. 1.5 cm is the search radius.
CLOTH_COVER_MIN = 0.90
CLOTH_COVER_M = 0.015

# Class ceilings: LOD0, LOD1, LOD2. LOD2 is required above LOD2_AT tris,
# and always for cars and buses.
LOD2_AT = 2000
LOD1_AT = 400
BUDGETS = {
    "prop": (2500, 1400, 700),
    "prop_dense": (6000, 2800, 1400),
    "road": (4000, 4000, 2000),
    "building": (3000, 1600, 900),
    "building_shell": (8000, 5000, 3400),
    "harbor": (4000, 2200, 1200),
    "harbor_large": (9000, 4500, 1800),
    "park": (4000, 2800, 1500),
    "tree": (3500, 1600, 900),
    "sedan_mid": (15000, 7000, 2800),
    "sedan_compact": (15000, 7000, 2800),
    "hatch": (15000, 7000, 2800),
    "crossover": (15000, 7000, 2800),
    "pickup": (15000, 7000, 2800),
    "bus40": (8000, 4000, 2000),
    "bus60": (8000, 4000, 2000),
    "figure": (2000, 1000, 500),
    "mannequin": (40000, 20000, 10000),
    "costume": (15000, 8000, 4000),
}
CAR_CLASSES = {"sedan_mid", "sedan_compact", "hatch", "crossover", "pickup"}
BUS_CLASSES = {"bus40", "bus60"}
ALWAYS_LOD2 = CAR_CLASSES | BUS_CLASSES

ENVELOPES = {
    "sedan_mid": ((4.70, 5.05), (1.75, 1.90), (1.38, 1.50)),
    "sedan_compact": ((4.20, 4.70), (1.70, 1.82), (1.38, 1.52)),
    "hatch": ((3.90, 4.50), (1.70, 1.85), (1.40, 1.56)),
    "crossover": ((4.20, 4.70), (1.75, 1.90), (1.55, 1.72)),
    "pickup": ((5.00, 5.90), (1.85, 2.10), (1.65, 1.95)),
    "bus40": ((11.80, 13.00), (2.40, 2.65), (3.00, 3.40)),
    "bus60": ((17.80, 19.00), (2.40, 2.65), (3.00, 3.40)),
}

# Documented exceptions from Docs/AssetLibrary.md and the builder.
ALLOW_BELOW = {
    "Piling", "Quay_Edge", "Boat", "FishingBoat", "Rowboat", "Buoy",
    "Dock_Straight", "Dock_Corner", "DockRamp", "Mooring", "HarborCrane",
    "MooringLine", "Gangway",
}
# Decals sit on the road. WallAC is a sleeve. Gangway and the sagged line mount on the dock.
ALLOW_FLOAT = {
    "Asphalt_Patch", "LaneArrow", "StopBar", "HarborWater", "MooringLine", "WallAC", "Gangway",
}
NO_COLLIDER = {"Asphalt_Patch", "LaneArrow", "StopBar", "HarborWater", "MooringLine"}
LOOSE_PIVOT = {"Dock_Corner"}
SHELLS = ("House", "Store", "WalkUp", "Cabin", "Gas", "Ranch", "Garage", "Shop")
HARBOR_LARGE = ("Container", "HarborWarehouse", "Quay_Edge", "Boat", "FishingBoat")
DENSE_PREFIX = ("BikeRack", "Newsstand", "Fountain", "Fence_", "Playground", "CourtFence")

TEXTURED = {
    "Lib_Brick", "Lib_Asphalt", "Lib_Wood", "Lib_WoodDark", "Lib_Concrete",
    "Lib_Siding", "Lib_Roof", "Lib_Warn", "Lib_Soil", "Lib_Hydrant", "Lib_WoodWeather",
    "Lib_CourtDecal", "Lib_Bark", "Lib_MetalWorn", "Lib_ContainerRed", "Lib_ContainerBlue",
    "Lib_CraneYellow", "Lib_LogEnd", "Lib_Pile",
}
TILE_EXCEPTION = {"Lib_CourtDecal"}
TEX_OK = {256, 512, 1024}

BRANDS = {
    "toyota", "camry", "corolla", "honda", "civic", "accord", "ford", "chevrolet",
    "chevy", "silverado", "tesla", "bmw", "mercedes", "audi", "nissan", "altima",
    "hyundai", "elantra", "kia", "volkswagen", "jeep", "dodge", "lexus", "acura",
    "mazda", "subaru", "porsche", "ferrari", "nike", "adidas", "starbucks",
    "mcdonald", "fedex", "usps",
}
BRAND_RE = re.compile(
    r"(?<![A-Za-z0-9])(" + "|".join(sorted(BRANDS, key=len, reverse=True)) + r")(?![A-Za-z0-9])",
    re.I,
)

CLOTH_OK = (
    "hoodie", "seam", "pocket", "sleeve", "jog", "shoe", "hood", "cap", "brim",
    "helmet", "visor", "collar", "hair", "pack", "roll",
)
CLOTH_BAN = ("cape", "scarf", "skirt", "dress", "cloak", "tie")

QUARTER = {"quarter", "hero", "threequarter", "three-quarter"}
SIDE = {"side", "profile"}
CLOSE = {"close", "nose", "door", "wheel", "bowl", "window", "detail", "blades", "head"}
SCALE = {"scale", "figure"}
PASS_RE = re.compile(r"(?:^|/)pass(\d+)(?:/|$)")
NAME_RE = re.compile(r"^[A-Z][A-Za-z0-9]*(_[A-Za-z0-9]+)*$")
COL_RE = re.compile(r"^(Col|Climb|Vault)_[A-Za-z0-9_-]+$")


def classify(name, category):
    if category == "Vehicles" or name.startswith(("Car_", "Sedan", "Hatch_", "Crossover", "Pickup", "Bus_")):
        if name.startswith("Bus_") or "Bus_City" in name:
            return "bus60" if "60" in name else "bus40"
        if "Pickup" in name:
            return "pickup"
        if "Crossover" in name:
            return "crossover"
        if "Hatch" in name:
            return "hatch"
        if "Compact" in name:
            return "sedan_compact"
        return "sedan_mid"
    if category == "Roads" or name.startswith(("StreetRoad", "StreetCurb", "Sidewalk", "Gutter")):
        return "road"
    if name.startswith("Tree"):
        return "tree"
    if category == "Buildings":
        if name.startswith(SHELLS):
            return "building_shell"
        return "building"
    if category == "Harbor":
        if name.startswith(HARBOR_LARGE):
            return "harbor_large"
        return "harbor"
    if category == "Showcase" or name == "Mannequin":
        return "figure"
    if name.startswith(DENSE_PREFIX):
        return "prop_dense"
    if category == "Park":
        return "park"
    return "prop"


def model_year(name):
    match = re.search(r"(?:^|_)(2[1-6])(?:_|$)", name)
    if not match:
        return None
    return 2000 + int(match.group(1))


def in_range(value, bounds):
    return bounds[0] <= value <= bounds[1]


def collider_top(col):
    center = col.get("center") or [0, 0, 0]
    kind = col.get("type")
    if kind == "box":
        size = col.get("size") or [0, 0, 0]
        hy = float(size[1]) * 0.5
        euler = col.get("euler") or [0, 0, 0]
        if any(abs(float(e)) > 1.0 for e in euler):
            hx = float(size[0]) * 0.5
            hz = float(size[2]) * 0.5
            hy = (hx * hx + hy * hy + hz * hz) ** 0.5
        return float(center[1]) + hy
    if kind == "sphere":
        return float(center[1]) + float(col.get("radius") or 0)
    if kind == "capsule":
        return float(center[1]) + float(col.get("height") or 0) * 0.5
    return float(center[1])


def claims_landing(entry):
    text = " ".join([
        entry.get("blurb") or "",
        entry.get("vaultNote") or "",
        entry.get("climbNote") or "",
    ]).lower()
    return "landing" in text or "walk the" in text or "hood and roof" in text


def png_size(data):
    if len(data) < 24 or data[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    return struct.unpack(">II", data[16:24])


def jpeg_size(data):
    if len(data) < 4 or data[:2] != b"\xff\xd8":
        return None
    i = 2
    while i + 9 < len(data):
        if data[i] != 0xFF:
            i += 1
            continue
        marker = data[i + 1]
        if marker in (0xC0, 0xC1, 0xC2):
            height, width = struct.unpack(">HH", data[i + 5:i + 9])
            return width, height
        if marker == 0xD8 or marker == 0xD9 or marker == 0x01 or 0xD0 <= marker <= 0xD7:
            i += 2
            continue
        if i + 4 > len(data):
            break
        seglen = struct.unpack(">H", data[i + 2:i + 4])[0]
        i += 2 + seglen
    return None


def image_size(path):
    with open(path, "rb") as handle:
        head = handle.read(65536)
    if path.lower().endswith(".png"):
        return png_size(head)
    if path.lower().endswith((".jpg", ".jpeg")):
        return jpeg_size(head)
    return png_size(head) or jpeg_size(head)


def _paeth(left, up, up_left):
    estimate = left + up - up_left
    da = abs(estimate - left)
    db = abs(estimate - up)
    dc = abs(estimate - up_left)
    if da <= db and da <= dc:
        return left
    if db <= dc:
        return up
    return up_left


def decode_png(data):
    """RGB bytes for a non-interlaced PNG. Indexed 4-bit stills are the common case."""
    if not data.startswith(b"\x89PNG\r\n\x1a\n"):
        return None
    pos = 8
    width = height = None
    bit_depth = color_type = interlace = None
    palette = b""
    idat = []
    while pos + 8 <= len(data):
        length = struct.unpack(">I", data[pos:pos + 4])[0]
        tag = data[pos + 4:pos + 8]
        chunk = data[pos + 8:pos + 8 + length]
        pos += 12 + length
        if tag == b"IHDR":
            width, height, bit_depth, color_type, _comp, _filt, interlace = struct.unpack(
                ">IIBBBBB", chunk
            )
        elif tag == b"PLTE":
            palette = chunk
        elif tag == b"IDAT":
            idat.append(chunk)
        elif tag == b"IEND":
            break
    if not width or interlace or bit_depth not in (1, 2, 4, 8):
        return None
    if color_type == 3:
        channels = 1
        if len(palette) < 3 or len(palette) % 3:
            return None
    elif color_type in (0, 2, 4, 6) and bit_depth == 8:
        channels = {0: 1, 2: 3, 4: 2, 6: 4}[color_type]
    else:
        return None
    try:
        raw = zlib.decompress(b"".join(idat))
    except zlib.error:
        return None
    if color_type == 3:
        stride = (width * bit_depth + 7) // 8
    else:
        stride = width * channels
    if len(raw) < height * (stride + 1):
        return None
    rows = []
    cursor = 0
    prev = bytearray(stride)
    bpp = max(1, channels) if color_type != 3 else max(1, bit_depth // 8)
    for _y in range(height):
        filt = raw[cursor]
        cursor += 1
        row = bytearray(raw[cursor:cursor + stride])
        cursor += stride
        if filt == 1:
            for x in range(stride):
                left = row[x - bpp] if x >= bpp else 0
                row[x] = (row[x] + left) & 255
        elif filt == 2:
            for x in range(stride):
                row[x] = (row[x] + prev[x]) & 255
        elif filt == 3:
            for x in range(stride):
                left = row[x - bpp] if x >= bpp else 0
                row[x] = (row[x] + ((left + prev[x]) // 2)) & 255
        elif filt == 4:
            for x in range(stride):
                left = row[x - bpp] if x >= bpp else 0
                up = prev[x]
                up_left = prev[x - bpp] if x >= bpp else 0
                row[x] = (row[x] + _paeth(left, up, up_left)) & 255
        elif filt != 0:
            return None
        prev = row
        rows.append(row)
    rgb = bytearray(width * height * 3)
    out = 0
    for row in rows:
        if color_type == 3:
            indices = _palette_indices(row, width, bit_depth)
            for index in indices:
                src = index * 3
                if src + 3 > len(palette):
                    rgb[out:out + 3] = b"\x00\x00\x00"
                else:
                    rgb[out:out + 3] = palette[src:src + 3]
                out += 3
            continue
        if channels == 3:
            rgb[out:out + stride] = row
            out += stride
            continue
        for x in range(width):
            if channels == 1:
                value = row[x]
                rgb[out:out + 3] = bytes((value, value, value))
            elif channels == 2:
                value = row[x * 2]
                rgb[out:out + 3] = bytes((value, value, value))
            else:
                src = x * 4
                rgb[out:out + 3] = row[src:src + 3]
            out += 3
    return width, height, bytes(rgb)


def _palette_indices(row, width, bit_depth):
    indices = []
    if bit_depth == 8:
        return list(row[:width])
    mask = (1 << bit_depth) - 1
    packed = 0
    filled = 0
    for byte in row:
        packed = (packed << 8) | byte
        filled += 8
        while filled >= bit_depth and len(indices) < width:
            filled -= bit_depth
            indices.append((packed >> filled) & mask)
    return indices


def decode_jpeg(path):
    """Downscale a JPEG with ffmpeg. None when ffmpeg cannot read it."""
    size = image_size(path)
    if not size or size[0] < 2 or size[1] < 2:
        return None
    long_side = max(size)
    step = max(1, long_side // FRAME_LONG)
    width = max(2, size[0] // step)
    height = max(2, size[1] // step)
    width -= width % 2
    height -= height % 2
    if width < 2 or height < 2:
        return None
    try:
        raw = subprocess.check_output(
            [
                "ffmpeg", "-v", "error", "-i", path,
                "-vf", "scale=%d:%d:flags=area" % (width, height),
                "-f", "rawvideo", "-pix_fmt", "rgb24", "-",
            ],
            timeout=30,
        )
    except (OSError, subprocess.SubprocessError):
        return None
    if len(raw) != width * height * 3:
        return None
    return width, height, raw


def load_rgb(path):
    """RGB grid for the framing test. ffmpeg downscales; PNG decode is the fallback."""
    decoded = decode_jpeg(path)
    if decoded is not None:
        return decoded
    try:
        with open(path, "rb") as handle:
            data = handle.read()
    except OSError:
        return None
    if data.startswith(b"\x89PNG\r\n\x1a\n"):
        decoded = decode_png(data)
        if decoded is None:
            return None
        return _downsample(decoded)
    return None


def _downsample(decoded):
    width, height, rgb = decoded
    step = max(1, max(width, height) // FRAME_LONG)
    if step == 1:
        return decoded
    grid_w = width // step
    grid_h = height // step
    out = bytearray(grid_w * grid_h * 3)
    cursor = 0
    for y in range(grid_h):
        row = y * step * width
        for x in range(grid_w):
            src = (row + x * step) * 3
            out[cursor:cursor + 3] = rgb[src:src + 3]
            cursor += 3
    return grid_w, grid_h, bytes(out)


def _pix(rgb, width, x, y):
    i = (y * width + x) * 3
    return rgb[i], rgb[i + 1], rgb[i + 2]


def _color_dist(a, b):
    return abs(a[0] - b[0]) + abs(a[1] - b[1]) + abs(a[2] - b[2])


def _corner_color(rgb, width, height, x0, y0, size=6):
    """Mean and luminance variance of a corner block. High variance is not backdrop."""
    acc = [0, 0, 0]
    lums = []
    count = 0
    for y in range(y0, min(height, y0 + size)):
        for x in range(x0, min(width, x0 + size)):
            red, green, blue = _pix(rgb, width, x, y)
            acc[0] += red
            acc[1] += green
            acc[2] += blue
            lums.append(red + green + blue)
            count += 1
    if not count:
        return None, 1e9
    mean = (acc[0] // count, acc[1] // count, acc[2] // count)
    average = sum(lums) / float(count)
    variance = sum((value - average) ** 2 for value in lums) / float(count)
    return mean, variance


def _backdrop_colors(rgb, width, height):
    size = 6 if width >= 12 and height >= 12 else 2
    corners = (
        (0, 0),
        (max(0, width - size), 0),
        (0, max(0, height - size)),
        (max(0, width - size), max(0, height - size)),
    )
    colors = []
    for x0, y0 in corners:
        mean, variance = _corner_color(rgb, width, height, x0, y0, size)
        if mean is not None and variance <= 400:
            colors.append(mean)
    return colors


def _foreground_mask(rgb, width, height, backdrop):
    mask = bytearray(width * height)
    if not backdrop:
        # The object owns the corners. Treat the frame as filled.
        for i in range(width * height):
            mask[i] = 1
        return mask
    limit = 80
    for y in range(height):
        for x in range(width):
            color = _pix(rgb, width, x, y)
            nearest = min(_color_dist(color, bg) for bg in backdrop)
            if nearest > limit:
                mask[y * width + x] = 1
    return mask


def _mask_box(mask, width, height):
    min_x, min_y = width, height
    max_x, max_y = -1, -1
    edge = 0
    count = 0
    for y in range(height):
        row = y * width
        for x in range(width):
            if not mask[row + x]:
                continue
            count += 1
            if x < min_x:
                min_x = x
            if y < min_y:
                min_y = y
            if x > max_x:
                max_x = x
            if y > max_y:
                max_y = y
            if x == 0 or y == 0 or x == width - 1 or y == height - 1:
                edge += 1
    if max_x < 0:
        return None, 0.0, edge
    area = ((max_x - min_x + 1) * (max_y - min_y + 1)) / float(width * height)
    box = (min_x, min_y, max_x, max_y)
    return box, area, edge


def _is_figure_blue(red, green, blue):
    # Lib_PaintBlue torso, lit. A grey sky is not blue-dominant.
    return blue >= 120 and blue >= red + 40 and blue >= green + 8 and green >= 50


def _is_figure_white(red, green, blue):
    # Lib_PaintWhite head.
    return red >= 200 and green >= 200 and blue >= 190 and max(red, green, blue) - min(red, green, blue) <= 40


def _is_figure_skin(red, green, blue):
    # The library stills stand the tan Hier next to the prop.
    return (
        red >= 130 and green >= 70 and blue >= 50
        and red >= green + 15 and red >= blue + 15
        and (green - blue) < 50 and (red - blue) < 100
    )


def _components(points, width, height):
    if not points:
        return []
    parent = list(range(len(points)))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    index_of = {pix: i for i, pix in enumerate(points)}
    for i, pix in enumerate(points):
        x = pix % width
        y = pix // width
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if nx < 0 or ny < 0 or nx >= width or ny >= height:
                continue
            other = index_of.get(ny * width + nx)
            if other is None:
                continue
            left, right = find(i), find(other)
            if left != right:
                parent[right] = left
    groups = defaultdict(list)
    for i, pix in enumerate(points):
        groups[find(i)].append(pix)
    return list(groups.values())


def _standing_clear(members, width, height, max_width, min_height, max_height, min_aspect):
    xs = [pix % width for pix in members]
    ys = [pix // width for pix in members]
    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)
    box_w = max_x - min_x + 1
    box_h = max_y - min_y + 1
    if box_w > width * max_width or box_h < height * min_height or box_h > height * max_height:
        return False
    if box_h < box_w * min_aspect:
        return False
    if min_x <= 0 or min_y <= 0 or max_x >= width - 1 or max_y >= height - 1:
        return False
    return True


def provenance_claimed(text):
    """The in-house line, on a line by itself. A longer sentence does not count."""
    return any(line.strip() == PROVENANCE_LINE for line in (text or "").splitlines())


def figure_name_tag(path):
    """PNG tEXt or JPEG comment naming the scale figure. Colour is then irrelevant."""
    if not path or not os.path.isfile(path):
        return False
    try:
        with open(path, "rb") as handle:
            data = handle.read(8_000_000)
    except OSError:
        return False
    values = []
    if data.startswith(b"\x89PNG\r\n\x1a\n"):
        pos = 8
        while pos + 8 <= len(data):
            length, kind = struct.unpack(">I4s", data[pos:pos + 8])
            pos += 8
            if pos + length > len(data):
                break
            chunk = data[pos:pos + length]
            pos += length + 4
            if kind == b"tEXt":
                key, _, value = chunk.partition(b"\x00")
                if key.lower() == b"figure":
                    values.append(value.split(b"\x00", 1)[0].decode("latin1", "replace").strip())
            elif kind == b"IEND":
                break
    else:
        pos = 2
        while pos + 4 <= len(data) and data[pos] == 0xFF:
            mark = data[pos + 1]
            if mark == 0xD8:
                pos += 2
                continue
            if mark == 0xD9 or mark == 0xDA:
                break
            size = struct.unpack(">H", data[pos + 2:pos + 4])[0]
            if mark == 0xFE:
                raw = data[pos + 4:pos + 2 + size]
                text = raw.decode("latin1", "replace")
                for line in text.splitlines():
                    if line.lower().startswith("figure="):
                        values.append(line.split("=", 1)[1].strip())
                    elif line.lower().startswith("figure:"):
                        values.append(line.split(":", 1)[1].strip())
            pos += 2 + size
    for value in values:
        norm = value.replace("\\", "/").strip()
        if norm in FIGURE_NAMES or os.path.basename(norm) in FIGURE_NAMES:
            return True
    return False


def _figure_visible(rgb, width, height):
    """The 1.8 m figure, fully inside the frame.

    Vehicle stills use the blue Mannequin (white head). Library stills use
    the tan Hier. A rust patch or a red body is not a standing figure.
    """
    blues = []
    whites = []
    skins = []
    for y in range(height):
        for x in range(width):
            red, green, blue = _pix(rgb, width, x, y)
            pix = y * width + x
            if _is_figure_blue(red, green, blue):
                blues.append(pix)
            elif _is_figure_white(red, green, blue):
                whites.append((x, y))
            elif _is_figure_skin(red, green, blue):
                skins.append(pix)
    for members in _components(blues, width, height):
        if len(members) < 12:
            continue
        if not _standing_clear(members, width, height, 0.18, 0.08, 0.55, 1.3):
            continue
        xs = [pix % width for pix in members]
        ys = [pix // width for pix in members]
        min_x, max_x = min(xs), max(xs)
        min_y = min(ys)
        box_h = max(ys) - min_y + 1
        head_bottom = min_y + max(2, int(box_h * 0.40))
        head = 0
        for x, y in whites:
            if min_y - 2 <= y <= head_bottom and min_x - 2 <= x <= max_x + 2:
                head += 1
        if head >= 3:
            return True
    for members in _components(skins, width, height):
        if len(members) < 40:
            continue
        if _standing_clear(members, width, height, 0.22, 0.12, 0.62, 1.8):
            return True
    return False


_FRAME_CACHE = {}


def frame_facts(path):
    """Backdrop-difference silhouette. Cached per file."""
    cached = _FRAME_CACHE.get(path)
    if cached is not None:
        return cached
    decoded = load_rgb(path) if path and os.path.isfile(path) else None
    if decoded is None:
        facts = None
    else:
        width, height, rgb = decoded
        backdrop = _backdrop_colors(rgb, width, height)
        mask = _foreground_mask(rgb, width, height, backdrop)
        _box, area, edge = _mask_box(mask, width, height)
        facts = {
            "area": area,
            "edge": edge,
            "figure": _figure_visible(rgb, width, height),
            "read": True,
        }
    _FRAME_CACHE[path] = facts
    return facts


def frame_reasons(role, info):
    """Hero, side, scale, and any other submitted still must show the whole object.

    Close-ups are not framed this way. `hero` is the quarter role. A still
    with no role token is `frame`: same silhouette test, so a lone 1280×720
    clip cannot skip it. Scale is the only role that also needs the figure.
    """
    if role == "close" or role not in ("quarter", "side", "scale", "frame"):
        return []
    path = info.get("full") or ""
    facts = frame_facts(path)
    if not facts:
        info["frame"] = None
        return ["stills-%s-frame" % role]
    info["frame"] = {"area": round(facts["area"], 3), "edge": facts["edge"], "figure": facts["figure"]}
    reasons = []
    if facts["edge"] >= 4:
        reasons.append("stills-%s-edge" % role)
    low, high = FRAME_AREA
    if facts["area"] < low or facts["area"] > high:
        reasons.append("stills-%s-coverage" % role)
    if role == "scale" and not facts["figure"] and not figure_name_tag(path):
        reasons.append("stills-scale-figure")
    return reasons


def read_text(path):
    with open(path, "r", encoding="utf-8", errors="replace") as handle:
        return handle.read()


def ascii_tokens(path, limit=40_000_000):
    size = os.path.getsize(path)
    with open(path, "rb") as handle:
        data = handle.read(min(size, limit))
    tokens = []
    cur = []
    for byte in data:
        if 32 <= byte < 127:
            cur.append(chr(byte))
        else:
            if len(cur) >= 4:
                tokens.append("".join(cur))
            cur = []
    if len(cur) >= 4:
        tokens.append("".join(cur))
    return tokens


def brand_hits(text):
    found = []
    for token in text.splitlines():
        letters = sum(1 for ch in token if ch.isalpha())
        if letters < 4 or letters / max(len(token), 1) < 0.75:
            continue
        for match in BRAND_RE.finditer(token):
            word = match.group(1).lower()
            if word not in found:
                found.append(word)
    return found


def snake_keys(name):
    """Full asset name only. Colour, year, and variant tokens stay on the key."""
    parts = [part.lower() for part in name.split("_") if part]
    if not parts:
        return []
    return ["_".join(parts)]


def named_role(stem):
    """Role token written on the file, or None when the still names no role."""
    tokens = re.split(r"[_\-]+", stem.lower())
    joined = "".join(tokens)
    if any(tok in SCALE or tok.replace("-", "") in {"scale", "figure"} for tok in tokens):
        return "scale"
    if any(tok in SIDE for tok in tokens):
        return "side"
    if any(tok in CLOSE for tok in tokens):
        return "close"
    if "threequarter" in joined or "three-quarter" in stem.lower():
        return "quarter"
    if any(tok in QUARTER for tok in tokens):
        return "quarter"
    return None


def role_of(stem):
    return named_role(stem) or "quarter"


# Folder names that are not part of the object. `street` in a filename is.
_DIR_NOISE = {
    "docs", "assetstills", "characters", "locostills", "street", "objects",
    "kit", "vehicles",
}
_FILE_NOISE = QUARTER | SIDE | CLOSE | SCALE | {
    "before", "after", "check", "lineup", "readability", "30px", "variants",
    "front", "rear", "top",
}


def _push_token(tokens, tok, drop):
    if not tok or tok in drop or tok in tokens:
        return
    if re.fullmatch(r"pass\d*", tok):
        return
    # A lone pass index, not a size token such as container 20 or speed 25.
    if tok.isdigit() and len(tok) == 1:
        return
    tokens.append(tok)


def object_tokens(rel):
    """Object tokens in a still path.

    Directory segments drop folder noise, including `street` in `street_kit`.
    The filename keeps `street` and `corner`.
    """
    rel = re.sub(r"\.(png|jpg|jpeg)$", "", rel.replace("\\", "/"), flags=re.I)
    directory, filename = os.path.split(rel)
    tokens = []
    for part in directory.split("/"):
        for tok in re.split(r"[_\-]+", part.lower()):
            _push_token(tokens, tok, _DIR_NOISE)
    for tok in re.split(r"[_\-]+", filename.lower()):
        _push_token(tokens, tok, _FILE_NOISE)
    return tokens


# Missing tokens that are just a qualifier, not a different object.
# `street` and `corner` are different objects, not qualifiers.
GENERIC_SUFFIX = {
    "post", "single", "walk", "rack", "planted", "city",
    "small", "large", "wood", "fixed", "removable",
}
STILL_ALIASES = {
    "Ranch_House": ["ranch"],
}


def _measure_still(row):
    if "pixels" not in row:
        row["pixels"] = image_size(row["path"])
        row["bytes"] = os.path.getsize(row["path"])
    return row


def _still_ok(row):
    _measure_still(row)
    size = row["pixels"]
    if size is None or size[0] < STILL_MIN[0] or size[1] < STILL_MIN[1]:
        return False
    return row["bytes"] <= STILL_MAX_BYTES


def _better_still(row, prev):
    row_ok = _still_ok(row)
    prev_ok = _still_ok(prev)
    if row_ok != prev_ok:
        return row_ok
    return row["pass"] > prev["pass"]


def key_hits(key_tokens, path_tokens, category=None):
    """True when the still is this asset, not a longer-named neighbour."""
    if not key_tokens:
        return False
    noise = set()
    if category:
        noise.add(category.lower().replace("_", ""))
    rel = [tok for tok in path_tokens if tok not in noise]
    extra = [tok for tok in rel if tok not in key_tokens]
    if extra:
        return False
    missing = [tok for tok in key_tokens if tok not in rel]
    if not missing:
        return True
    present = [tok for tok in key_tokens if tok in rel]
    if not present:
        return False
    if all(tok in GENERIC_SUFFIX for tok in missing) and any(tok not in GENERIC_SUFFIX for tok in present):
        return True
    return False


class StillIndex(object):
    def __init__(self, root):
        self.rows = []
        roots = [
            os.path.join(root, "Docs", "AssetStills"),
            os.path.join(root, "Docs", "Characters"),
            os.path.join(root, "Docs", "LocoStills"),
        ]
        for base in roots:
            if not os.path.isdir(base):
                continue
            for dirpath, _dirs, files in os.walk(base):
                rel_dir = os.path.relpath(dirpath, root).replace("\\", "/")
                match = PASS_RE.search(rel_dir + "/")
                if not match:
                    continue
                passed = int(match.group(1))
                for name in files:
                    if not name.lower().endswith((".png", ".jpg", ".jpeg")):
                        continue
                    rel = rel_dir + "/" + name
                    stem = os.path.splitext(name)[0]
                    self.rows.append({
                        "path": os.path.join(dirpath, name),
                        "rel": rel,
                        "stem": stem,
                        "pass": passed,
                        "role": role_of(stem),
                        "tokens": object_tokens(rel),
                    })

    def match(self, keys, scope, category=None):
        """Newest still per role. keys empty means every still in scope."""
        best = {}
        key_token_sets = [tuple(part for part in k.split("_") if part) for k in keys if k]
        for row in self.rows:
            if not still_in_scope(row, scope, category):
                continue
            if key_token_sets and not any(
                key_hits(list(tokens), row["tokens"], category) for tokens in key_token_sets
            ):
                continue
            role = row["role"]
            prev = best.get(role)
            if prev is None or _better_still(row, prev):
                best[role] = row
        for row in best.values():
            _measure_still(row)
        return best


def _spdx_of(cell):
    text = cell.upper().replace(" ", "")
    if "CC0-1.0" in text or "CC01.0" in text:
        return "CC0-1.0"
    if "OFL-1.1" in text:
        return "OFL-1.1"
    return ""


def parse_license_table(text):
    """One markdown table row per asset. The first cell is the asset name.

    A prose mention of a family name is not a row. The row counts only when
    that cell is the asset and the row carries CC0-1.0 or OFL-1.1.
    """
    named = {}
    lines = text.splitlines()
    for index, line in enumerate(lines):
        raw = line.strip()
        if not raw.startswith("|"):
            continue
        cells = [cell.strip().strip("`").strip() for cell in raw.strip("|").split("|")]
        if not cells or not cells[0]:
            continue
        if re.fullmatch(r":?-{3,}:?", cells[0].replace(" ", "")):
            continue
        # Header row: the next line is the markdown separator.
        if index + 1 < len(lines):
            nxt = lines[index + 1].strip().strip("|").split("|")[0].strip()
            if re.fullmatch(r":?-{3,}:?", nxt.replace(" ", "")):
                continue
        spdx = ""
        for cell in cells:
            spdx = _spdx_of(cell)
            if spdx:
                break
        if not spdx:
            continue
        named[cells[0]] = spdx
    return named


class LicenseBook(dict):
    """Asset name to SPDX, plus the lane-wide in-house line."""

    def __init__(self):
        super().__init__()
        self.provenance = False


def license_names(root):
    named = LicenseBook()
    for dirpath, _dirs, files in os.walk(root):
        if ".git" in dirpath.split(os.sep):
            continue
        for name in files:
            if name != "LICENSES.md":
                continue
            text = read_text(os.path.join(dirpath, name))
            named.update(parse_license_table(text))
            if provenance_claimed(text):
                named.provenance = True
    manifest = os.path.join(root, "Tools", "Blender", "AssetLibrary", "manifest.json")
    if os.path.isfile(manifest):
        try:
            data = json.loads(read_text(manifest))
        except json.JSONDecodeError:
            data = None
        if isinstance(data, dict) and provenance_claimed(str(data.get("provenance") or "")):
            named.provenance = True
    return named


def entry_licensed(entry, named):
    lic = entry.get("license")
    provenance = bool(getattr(named, "provenance", False))
    if isinstance(lic, dict) and lic:
        spdx = str(lic.get("spdx") or "")
        source = str(lic.get("source") or "")
        if source == "cc0-download" and not str(lic.get("url") or "").strip():
            return False, "license-bad"
        if spdx in ("CC0-1.0", "OFL-1.1") and source:
            if spdx == "OFL-1.1":
                return False, "license-ofl-on-mesh"
            return True, ""
        return False, "license-bad"
    spdx = named.get(entry.get("name") or "")
    if spdx == "CC0-1.0":
        return True, ""
    if spdx == "OFL-1.1":
        return False, "license-ofl-on-mesh"
    if provenance:
        return True, ""
    return False, "license"


def check_lod(cls, lods):
    reasons = []
    ceilings = BUDGETS[cls]
    counts = []
    for item in lods:
        counts.append(int(item.get("tris") or 0))
    if not counts:
        return ["lod-missing"], "n/a"
    lod0 = counts[0]
    need2 = cls in ALWAYS_LOD2 or lod0 > LOD2_AT
    need1 = lod0 > LOD1_AT
    if need1 and len(counts) < 2:
        reasons.append("lod1-missing")
    if need2 and len(counts) < 3:
        reasons.append("lod2-missing")
    labels = []
    prev = None
    for index, count in enumerate(counts[:3]):
        labels.append(str(count))
        if count > ceilings[index]:
            reasons.append("lod%d-budget" % index)
        if prev is not None and count > prev:
            reasons.append("lod%d-not-coarser" % index)
        prev = count
    # LOD2 may keep at most 60% of the LOD1 triangle count.
    if len(counts) >= 3 and counts[1] > 0 and counts[2] * 5 > counts[1] * 3:
        reasons.append("lod2-ratio")
    while len(labels) < 3:
        labels.append("-")
    return reasons, "/".join(labels)


def check_scale(cls, name, size):
    reasons = []
    if not size or len(size) < 3:
        return ["scale-missing"]
    width, height, length = float(size[0]), float(size[1]), float(size[2])
    if cls == "figure":
        if not in_range(height, FIGURE_H):
            reasons.append("figure-height")
        return reasons
    if cls in ENVELOPES:
        length_b, width_b, height_b = ENVELOPES[cls]
        if length < width:
            reasons.append("orientation")
        if not in_range(length, length_b):
            reasons.append("length")
        if not in_range(width, width_b):
            reasons.append("width")
        if not in_range(height, height_b):
            reasons.append("height")
        year = model_year(name)
        if cls in CAR_CLASSES:
            if year is None:
                reasons.append("model-year-missing")
            elif year < YEAR_MIN or year > YEAR_MAX:
                reasons.append("model-year=%d" % year)
    return reasons


def check_pivot(name, entry):
    reasons = []
    mn = entry.get("min")
    mx = entry.get("max")
    if not mn or not mx:
        return ["pivot-missing"]
    minx, miny, minz = float(mn[0]), float(mn[1]), float(mn[2])
    maxx, _maxy, maxz = float(mx[0]), float(mx[1]), float(mx[2])
    blurb = (entry.get("blurb") or "") + " " + (entry.get("climbNote") or "")
    below_ok = name in ALLOW_BELOW or "below the pivot" in blurb or "below its pivot" in blurb
    if miny < BELOW_M and not below_ok:
        reasons.append("below-pivot")
    if miny > FLOAT_M and name not in ALLOW_FLOAT:
        reasons.append("floats")
    # Footprint center is the builder's ground-contact test (verts below 0.25 m).
    # The full AABB is not the footprint: a porch or a nose shifts it on purpose.
    # Fail only when that builder recorded the warning.
    for warning in entry.get("warnings") or []:
        if "off center" in warning:
            reasons.append("pivot-offcenter")
            break
    return reasons


def check_colliders(name, entry, slack_cm):
    reasons = []
    cols = entry.get("colliders") or []
    blurb = (entry.get("blurb") or "").lower()
    empty_ok = name in NO_COLLIDER or "no collider" in blurb
    if not cols and not empty_ok:
        reasons.append("collider-missing")
    for col in cols:
        if not COL_RE.match(str(col.get("name") or "")):
            reasons.append("collider-name")
            break
    if slack_cm is None and cols:
        reasons.append("slack-unmeasured")
    elif slack_cm is not None and slack_cm > SLACK_LIMIT_CM:
        reasons.append("slack")
    for warning in entry.get("warnings") or []:
        if "sticks out" in warning:
            reasons.append("slack-warn")
            break
    if claims_landing(entry) and cols:
        mesh_top = float(entry["max"][1]) if entry.get("max") else None
        if mesh_top is not None:
            best = max(collider_top(col) for col in cols)
            gap = mesh_top - best
            if gap > LAND_GAP_M:
                reasons.append("land-gap=%.1fcm" % (gap * 100.0))
    return reasons


def check_materials(root, entry, tex_cache):
    reasons = []
    used = []
    for lod in entry.get("lods") or []:
        for mat in lod.get("materials") or []:
            if mat not in used:
                used.append(mat)
            if not str(mat).startswith("Lib_"):
                reasons.append("material-name")
    if "material-name" in reasons:
        reasons = [r for r in reasons if r != "material-name"]
        reasons.append("material-name")
    tex_root = os.path.join(root, "Assets", "Art", "Props", "Library", "Textures")
    for mat in used:
        if mat not in TEXTURED or mat in TILE_EXCEPTION:
            continue
        path = os.path.join(tex_root, mat + ".png")
        if not os.path.isfile(path):
            reasons.append("texel-missing")
            continue
        if path not in tex_cache:
            tex_cache[path] = image_size(path)
        size = tex_cache[path]
        if not size or size[0] != size[1] or size[0] not in TEX_OK:
            reasons.append("texel")
    # de-dup while keeping order
    out = []
    for reason in reasons:
        if reason not in out:
            out.append(reason)
    return out


def _key_sets(keys):
    sets = []
    for key in keys or []:
        if isinstance(key, (list, tuple)):
            parts = []
            for item in key:
                parts.extend(part for part in str(item).split("_") if part)
            if parts:
                sets.append(tuple(parts))
        else:
            parts = tuple(part for part in str(key).split("_") if part)
            if parts:
                sets.append(parts)
    return sets


def submitted_still_reasons(index, keys, scope, category, lane_keys, matched):
    """Frame every still this asset or its lane submitted, not only the winning quartet.

    A file with no role token still takes the silhouette test. Close-ups may crop.
    A still that names a different asset in the same lane is that asset's frame.
    A still that names nobody is a lane frame and counts here.
    """
    reasons = []
    seen = set()
    for info in (matched or {}).values():
        full = info.get("full") if info else None
        if full:
            seen.add(full)
    own = _key_sets(keys)
    lane = _key_sets(lane_keys) if lane_keys is not None else None
    for row in index.rows:
        if not still_in_scope(row, scope, category):
            continue
        if row["path"] in seen:
            continue
        hits_own = any(key_hits(list(tokens), row["tokens"], category) for tokens in own)
        if not hits_own:
            if lane is None:
                continue
            if any(key_hits(list(tokens), row["tokens"], category) for tokens in lane):
                continue
        role = named_role(row["stem"]) or "frame"
        if role == "close":
            continue
        _measure_still(row)
        if not _still_ok(row):
            continue
        info = {
            "path": row["rel"],
            "full": row["path"],
            "pixels": list(row["pixels"]) if row.get("pixels") else None,
            "bytes": row.get("bytes"),
        }
        reasons.extend(frame_reasons(role, info))
    return reasons


def stills_check(index, keys, scope, extra_readability=False, category=None, lane_keys=None):
    """Per-asset quartet check. PassN, 1280x720 or larger, each file under 400 KB.

    Returns (reasons, stillsCheck). stillsCheck is the record a ledger row can quote.
    `lane_keys` is every asset name in this still scope. When it is set, a still
    that matches none of them is framed too.
    """
    reasons = []
    matched = index.match(keys, scope, category)
    roles = {}
    for role in ("quarter", "side", "close", "scale"):
        row = matched.get(role)
        info = {"ok": False, "path": None, "pixels": None, "bytes": None, "pass": None}
        if row is None:
            reasons.append("stills-" + role)
        else:
            info["path"] = row.get("rel")
            info["full"] = row.get("path")
            info["pass"] = row.get("pass")
            size = row.get("pixels")
            nbytes = row.get("bytes")
            info["pixels"] = list(size) if size else None
            info["bytes"] = nbytes
            role_reasons = still_role_reasons(role, info)
            reasons.extend(role_reasons)
            info["ok"] = not role_reasons
        roles[role] = info
    reasons.extend(submitted_still_reasons(index, keys, scope, category, lane_keys, roles))
    if extra_readability:
        found = False
        for row in index.rows:
            if "readability" in row["stem"].lower():
                found = True
                break
        if not found:
            reasons.append("stills-readability")
    report = {"ok": not any(r.startswith("stills-") for r in reasons), "roles": roles}
    return reasons, report


def check_stills(index, keys, scope, extra_readability=False, category=None):
    reasons, _report = stills_check(index, keys, scope, extra_readability, category)
    return reasons


def empty_stills():
    roles = {}
    for role in ("quarter", "side", "close", "scale"):
        roles[role] = {"ok": False, "path": None, "pixels": None, "bytes": None, "pass": None}
    return {"ok": False, "roles": roles}


def name_tokens(name):
    return [part for part in (name or "").lower().split("_") if part]


def tokens_prefix(tokens, prefix):
    return len(tokens) >= len(prefix) and list(tokens[:len(prefix)]) == list(prefix)


def still_allowed(still_tokens, asset_tokens, asset_hash, population):
    """Exact name always. A shorter still only when every prefixed mesh matches.

    `population` is `(tokens, geometry sha256, ...)`. A family still such as
    `sedan_mid_a` is a token prefix of `Sedan_Mid_A_22`. It may be borrowed
    only when every asset that prefix covers has the same geometry hash
    (positions, indices, UVs per LOD). A material or colour change matches.
    A different cage does not. A missing digest denies the borrow.
    """
    still_tokens = list(still_tokens)
    asset_tokens = list(asset_tokens)
    if still_tokens == asset_tokens:
        return True
    if not still_tokens or not tokens_prefix(asset_tokens, still_tokens):
        return False
    if len(still_tokens) >= len(asset_tokens) or asset_hash is None:
        return False
    group = [item[1] for item in population if tokens_prefix(item[0], still_tokens)]
    return bool(group) and all(digest is not None and digest == asset_hash for digest in group)


def _fbx_nodes(data):
    """Top-level nodes of a binary FBX. Array properties are ('array', raw)."""
    if not data or not data.startswith(b"Kaydara FBX Binary") or len(data) < 27:
        return None
    version = struct.unpack_from("<I", data, 23)[0]
    is64 = version >= 7500
    offset = 27

    def read_array(pos):
        length, encoding, compressed = struct.unpack_from("<III", data, pos)
        pos += 12
        raw = data[pos:pos + compressed]
        pos += compressed
        if encoding == 1:
            raw = zlib.decompress(raw)
        elif encoding != 0:
            return pos, None
        return pos, raw

    def read_node(pos):
        if is64:
            end, count, prop_len = struct.unpack_from("<QQQ", data, pos)
            pos += 24
            null_size = 25
        else:
            end, count, prop_len = struct.unpack_from("<III", data, pos)
            pos += 12
            null_size = 13
        if end == 0 and count == 0 and prop_len == 0:
            return pos, None
        name_len = data[pos]
        pos += 1
        name = data[pos:pos + name_len].decode("latin1", "replace")
        pos += name_len
        props = []
        for _index in range(count):
            kind = chr(data[pos])
            pos += 1
            if kind == "Y":
                props.append(struct.unpack_from("<h", data, pos)[0])
                pos += 2
            elif kind == "C":
                props.append(data[pos])
                pos += 1
            elif kind == "I":
                props.append(struct.unpack_from("<i", data, pos)[0])
                pos += 4
            elif kind == "F":
                props.append(struct.unpack_from("<f", data, pos)[0])
                pos += 4
            elif kind == "D":
                props.append(struct.unpack_from("<d", data, pos)[0])
                pos += 8
            elif kind == "L":
                props.append(struct.unpack_from("<q", data, pos)[0])
                pos += 8
            elif kind in "fdlib":
                pos, raw = read_array(pos)
                props.append(("array", raw if raw is not None else b""))
            elif kind in "SR":
                size = struct.unpack_from("<I", data, pos)[0]
                pos += 4
                props.append(data[pos:pos + size])
                pos += size
            else:
                raise ValueError("fbx type %s" % kind)
        children = []
        while pos < end:
            if is64:
                end2, count2, prop2 = struct.unpack_from("<QQQ", data, pos)
            else:
                end2, count2, prop2 = struct.unpack_from("<III", data, pos)
            if end2 == 0 and count2 == 0 and prop2 == 0:
                pos += null_size
                break
            pos, child = read_node(pos)
            if child:
                children.append(child)
        return pos, (name, props, children)

    nodes = []
    while offset < len(data) - 16:
        if is64:
            end2, count2, prop2 = struct.unpack_from("<QQQ", data, offset)
        else:
            end2, count2, prop2 = struct.unpack_from("<III", data, offset)
        if end2 == 0 and count2 == 0 and prop2 == 0:
            break
        offset, node = read_node(offset)
        if node:
            nodes.append(node)
    return nodes


def geometry_digest(data):
    """SHA-256 of vertex positions, indices, and UVs for each LOD mesh.

    Material names and colour are not part of the hash. A paint sibling of
    the same cage matches. A different cage does not. When the file has
    LOD0 / LOD1 / LOD2 geometries, only those are hashed, in LOD order.
    """
    nodes = _fbx_nodes(data)
    if not nodes:
        return None
    found = []

    def take_arrays(node, chunks):
        name, props, children = node
        if name in ("Vertices", "PolygonVertexIndex", "UV", "UVIndex"):
            for prop in props:
                if isinstance(prop, tuple) and prop[0] == "array" and prop[1]:
                    chunks.append(name.encode("ascii"))
                    chunks.append(prop[1])
        for child in children:
            take_arrays(child, chunks)

    def walk(node):
        name, props, children = node
        if name == "Geometry":
            label = ""
            if len(props) > 1 and isinstance(props[1], (bytes, bytearray)):
                label = props[1].split(b"\x00", 1)[0].decode("latin1", "replace")
            chunks = []
            for child in children:
                take_arrays(child, chunks)
            if chunks:
                found.append((label, chunks))
        for child in children:
            walk(child)

    for node in nodes:
        if node[0] == "Objects":
            walk(node)
    if not found:
        return None
    lods = [item for item in found if re.fullmatch(r"LOD\d+", item[0])]
    chosen = lods if lods else found

    def sort_key(item):
        match = re.fullmatch(r"LOD(\d+)", item[0])
        if match:
            return (0, int(match.group(1)), item[0])
        return (1, 0, item[0])

    hasher = hashlib.sha256()
    for label, chunks in sorted(chosen, key=sort_key):
        hasher.update(b"mesh\0")
        hasher.update(label.encode("utf-8", "replace"))
        hasher.update(b"\0")
        for chunk in chunks:
            hasher.update(struct.pack("<I", len(chunk)))
            hasher.update(chunk)
    return hasher.hexdigest()


def mesh_digest(root, rel):
    """Geometry hash of an asset file. `loadouts.json` is not a mesh."""
    if not rel:
        return None
    if os.path.basename(str(rel)).lower() == "loadouts.json":
        return None
    path = rel if os.path.isabs(rel) else os.path.join(root, *str(rel).split("/"))
    blob = file_blob(path)
    if not blob:
        return None
    try:
        return geometry_digest(blob)
    except (ValueError, zlib.error, struct.error, IndexError):
        return None


def still_in_scope(row, scope, category):
    if scope not in row["rel"]:
        return False
    if "street_objects/" in row["rel"] or "street_kit/" in row["rel"]:
        if category not in ("StreetFurniture", "Vehicles", "Roads", "Utility", "Buildings"):
            return False
    if "/vehicles/" in row["rel"] and category not in (None, "Vehicles"):
        return False
    return True


def still_role_reasons(role, info):
    if not info or not info.get("path"):
        return ["stills-" + role]
    rel = info.get("path") or ""
    size = info.get("pixels")
    nbytes = info.get("bytes")
    if not PASS_RE.search(rel + "/"):
        return ["stills-%s-pass" % role]
    if not size or size[0] < STILL_MIN[0] or size[1] < STILL_MIN[1]:
        return ["stills-%s-res" % role]
    if nbytes is not None and nbytes > STILL_MAX_BYTES:
        return ["stills-%s-bytes" % role]
    return frame_reasons(role, info)


def role_info_from_still(still):
    _measure_still(still)
    size = still.get("pixels")
    info = {
        "ok": False,
        "path": still.get("rel"),
        "full": still.get("path"),
        "pass": still.get("pass"),
        "pixels": list(size) if size else None,
        "bytes": still.get("bytes"),
    }
    info["ok"] = not still_role_reasons(still.get("role") or "quarter", info)
    return info


def refresh_stills(row):
    report = row.get("stillsCheck") or empty_stills()
    row["stillsCheck"] = report
    kept = []
    for reason in row.get("reasons") or []:
        if reason.startswith("stills-") and reason != "stills-readability":
            continue
        if reason not in kept:
            kept.append(reason)
    fresh = []
    for role in ("quarter", "side", "close", "scale"):
        info = (report.get("roles") or {}).get(role) or {}
        for reason in still_role_reasons(role, info):
            if reason not in fresh:
                fresh.append(reason)
    report["ok"] = not fresh
    row["reasons"] = kept + fresh
    row["ok"] = not row["reasons"]


def borrowed_best(index, asset_tokens, asset_hash, population, scope, category):
    best = {}
    for still in index.rows:
        if not still_in_scope(still, scope, category):
            continue
        if not still_allowed(still["tokens"], asset_tokens, asset_hash, population):
            continue
        role = still["role"]
        prev = best.get(role)
        if prev is None or _better_still(still, prev):
            best[role] = still
    return best


def variant_base(still_tokens, asset_name, asset_category, population):
    """The asset whose name the still spells, when this row is a sibling."""
    matches = []
    for item in population:
        tokens, _digest, name = item[0], item[1], item[2]
        category = item[3] if len(item) > 3 else None
        if list(tokens) == list(still_tokens) and name != asset_name:
            matches.append((category, name))
    if not matches:
        return None
    same = [name for category, name in matches if category == asset_category]
    pool = same or [name for _category, name in matches]
    pool.sort(key=lambda name: (len(name), name))
    return pool[0]


def fill_borrowed(row, index, population, scope):
    best = borrowed_best(
        index, row.get("_tokens") or [], row.get("_digest"), population, scope, row.get("category"),
    )
    report = row.get("stillsCheck") or empty_stills()
    roles = report.setdefault("roles", {})
    borrowed_from = None
    for role, still in best.items():
        info = roles.get(role) or {}
        if info.get("ok"):
            continue
        borrowed = role_info_from_still(still)
        if not (borrowed.get("ok") or not info.get("path")):
            continue
        roles[role] = borrowed
        if list(still.get("tokens") or []) == list(row.get("_tokens") or []):
            continue
        base = variant_base(still.get("tokens") or [], row.get("name"), row.get("category"), population)
        if base:
            borrowed_from = base
    row["stillsCheck"] = report
    refresh_stills(row)
    if borrowed_from:
        row["variantOf"] = borrowed_from


def apply_identical_stills(rows, index, root):
    """Share a shorter still only when LOD geometry matches.

    The hash is vertex positions, indices, and UVs per LOD. A paint or colour
    sibling with that same cage uses the base quartet and is reported as
    `material-variant of X`. A different geometry hash needs its own quartet.
    Player colour Hiers copy the tan quartet only on that same geometry hash.
    Costumes are not hashed: `loadouts.json` is one file for every set.
    """
    population = []
    for row in rows:
        if row.get("kind") == "costume":
            row["_digest"] = None
        else:
            row["_digest"] = mesh_digest(root, row.get("fbx"))
        row["_tokens"] = name_tokens(row.get("name"))
        if row.get("kind") != "costume":
            population.append((row["_tokens"], row["_digest"], row.get("name"), row.get("category")))
    for row in rows:
        if row.get("kind") == "library":
            fill_borrowed(row, index, population, "Docs/AssetStills")
    tan = None
    for row in rows:
        if row.get("kind") != "rig":
            continue
        name = row.get("name") or ""
        rel = row.get("fbx") or ""
        if "Tan_Hier" in name and "Clearance" not in name and "Candidate" not in rel:
            tan = row
            break
    tan_digest = tan.get("_digest") if tan else None
    for row in rows:
        if row.get("kind") != "rig":
            continue
        name = row.get("name") or ""
        if not re.search(r"_(Blue|Lavender|Mint|Orange|Red)_Hier", name):
            continue
        if tan is not None and row.get("_digest") and row["_digest"] == tan_digest:
            row["stillsCheck"] = copy.deepcopy(tan["stillsCheck"])
            row["variantOf"] = tan.get("name")
        else:
            _reasons, stills = stills_check(index, snake_keys(name), "Docs")
            row["stillsCheck"] = stills
        refresh_stills(row)
    for row in rows:
        row.pop("_digest", None)
        row.pop("_tokens", None)


_BLOB_CACHE = {}


def file_blob(path):
    if not path or not os.path.isfile(path):
        return b""
    cached = _BLOB_CACHE.get(path)
    if cached is None:
        with open(path, "rb") as handle:
            cached = handle.read()
        _BLOB_CACHE[path] = cached
    return cached


def mesh_in_file(path, mesh_name):
    if not mesh_name:
        return False
    blob = file_blob(path)
    if not blob:
        return False
    return mesh_name.encode("utf-8") in blob


def lod_meshes(declared, path=None, piece_names=None):
    """LOD rows from the meshes that exist, for every asset type.

    Library, street, and vehicle assets name those meshes LOD0 / LOD1 / LOD2
    inside the FBX. A costume's LOD0 is the piece itself. LOD1 and LOD2 are
    the piece plus ``_LOD1`` / ``_LOD2`` in the lab blend. A single worn
    triangle total is not a LOD list. A declared level whose mesh is missing
    from the file is not counted, so the budget check reports it missing.
    """
    by_index = {}
    for item in declared or []:
        if item.get("lod") is None:
            idx = len(by_index)
        else:
            idx = int(item["lod"])
        by_index[idx] = item
    if not by_index:
        return []
    out = []
    file_ok = bool(path and os.path.isfile(path))
    for idx in sorted(by_index):
        item = by_index[idx]
        if piece_names:
            if not file_ok:
                continue
            if idx == 0:
                names = list(piece_names)
            else:
                names = ["%s_LOD%d" % (piece, idx) for piece in piece_names]
            if not names or not all(mesh_in_file(path, name) for name in names):
                continue
        elif file_ok:
            mesh = str(item.get("mesh") or ("LOD%d" % idx))
            if not mesh_in_file(path, mesh):
                continue
        out.append({
            "lod": idx,
            "tris": int(item.get("tris") or 0),
            "mesh": item.get("mesh") or ("LOD%d" % idx),
        })
    return out


def check_fbx_lods(path, lods):
    reasons = []
    if not os.path.isfile(path):
        return ["fbx-missing"]
    for item in lods or []:
        mesh = str(item.get("mesh") or ("LOD%d" % int(item.get("lod") or 0)))
        if not mesh_in_file(path, mesh):
            reasons.append("fbx-" + mesh.lower())
            break
    return reasons


def scan_brands(path, blurb, materials):
    hits = brand_hits(blurb or "")
    for mat in materials:
        hits.extend(h for h in brand_hits(str(mat)) if h not in hits)
    if path and os.path.isfile(path):
        text = "\n".join(ascii_tokens(path))
        for hit in brand_hits(text):
            if hit not in hits:
                hits.append(hit)
    if not hits:
        return []
    return ["brand=" + ",".join(hits)]


def latest_proof(root):
    base = os.path.join(root, "Docs", "LocoStills")
    best = None
    best_pass = -1
    if not os.path.isdir(base):
        return None, None
    for dirpath, _dirs, files in os.walk(base):
        if "proof.txt" not in files and "hip-range.txt" not in files:
            continue
        rel = os.path.relpath(dirpath, root).replace("\\", "/")
        match = PASS_RE.search(rel + "/")
        if not match:
            continue
        passed = int(match.group(1))
        proof_path = os.path.join(dirpath, "proof.txt")
        if os.path.isfile(proof_path) and passed >= best_pass:
            text = read_text(proof_path)
            if "pose=" in text:
                best = text
                best_pass = passed
    hip_text = ""
    hip_path = os.path.join(base, "pass6", "hip-range.txt")
    if not os.path.isfile(hip_path):
        for dirpath, _dirs, files in os.walk(base):
            if "hip-range.txt" in files:
                hip_path = os.path.join(dirpath, "hip-range.txt")
    if os.path.isfile(hip_path):
        hip_text = read_text(hip_path)
    return best, hip_text


def check_rig_text(proof, hip_text, root, is_candidate):
    reasons = []
    sampler = os.path.join(root, "Tools", "Tag", "render_loco_stills.py")
    sampled = False
    if os.path.isfile(sampler):
        body = read_text(sampler)
        if "1.0 / 30.0" in body or "1/30" in body:
            sampled = True
    if not sampled:
        reasons.append("sample-rate")
    if not proof:
        reasons.append("noclip-missing")
    else:
        match = re.search(
            r"rigJoint=(\d+)\s+pose=(\d+)",
            proof,
        )
        world = re.search(r"worldMax=([0-9.]+)", proof)
        self_max = re.search(r"selfMax=([0-9.]+)", proof)
        joints = re.search(
            r"joints knee=(\d+) hip=(\d+) neck=(\d+) ankle=(\d+)",
            proof,
        )
        if not match:
            reasons.append("pose-unreported")
        else:
            rig_joint = int(match.group(1))
            pose = int(match.group(2))
            if rig_joint != 0:
                reasons.append("rigJoint=%d" % rig_joint)
            if pose != 0:
                reasons.append("pose=%d" % pose)
        if world and float(world.group(1)) > NOCLIP_CM:
            reasons.append("world=%.2fcm" % float(world.group(1)))
        if self_max and float(self_max.group(1)) > NOCLIP_CM:
            reasons.append("self=%.2fcm" % float(self_max.group(1)))
        if joints:
            hip_n = int(joints.group(2))
            ankle_n = int(joints.group(4))
            knee_n = int(joints.group(1))
            if hip_n:
                reasons.append("hip=%d" % hip_n)
            if ankle_n:
                reasons.append("ankle=%d" % ankle_n)
            if knee_n:
                reasons.append("knee=%d" % knee_n)
    builder = os.path.join(root, "Tools", "Tag", "build_ball_joints.py")
    if is_candidate:
        if not os.path.isfile(builder):
            reasons.append("joints-missing")
        else:
            body = read_text(builder)
            if "UpperLeg_L" not in body or "Foot_L" not in body:
                reasons.append("joints-hip-ankle")
    else:
        reasons.append("joints-not-on-shipped-mesh")
    # Hip-sit has to be measured on loaded plants, landings, and crouches.
    hip_ok = False
    if hip_text:
        has_plant = re.search(r"plant|landing|crouch", hip_text, re.I)
        behind = re.findall(r"([0-9.]+)\s*cm behind", hip_text, re.I)
        ratio = re.search(r"hip flexion.*?([0-9.]+)\s*[x×]\s*spine", hip_text, re.I)
        if has_plant and behind and ratio:
            numbers = [float(n) for n in behind]
            if min(numbers) >= HIP_PLANT_M * 100.0 and float(ratio.group(1)) >= HIP_FLEX_RATIO:
                hip_ok = True
    if not hip_ok:
        reasons.append("hip-sit")
    return reasons


def bpy_slack(fbx_path, colliders):
    """Recompute worst collider slack in cm. None when bpy is not available."""
    try:
        import bpy  # type: ignore
        from mathutils import Vector  # type: ignore
        from mathutils.bvhtree import BVHTree  # type: ignore
    except ImportError:
        return None
    if not fbx_path or not os.path.isfile(fbx_path) or not colliders:
        return None
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx_path)
    mesh_obj = None
    for obj in bpy.data.objects:
        if obj.type == "MESH" and obj.name.startswith("LOD0"):
            mesh_obj = obj
            break
    if mesh_obj is None:
        return None
    deps = bpy.context.evaluated_depsgraph_get()
    eval_obj = mesh_obj.evaluated_get(deps)
    mesh = eval_obj.to_mesh()
    bvh = BVHTree.FromPolygons([mesh.vertices[i].co for i in range(len(mesh.vertices))], mesh.polygons)
    # Unity meters were baked into the FBX. Samples stay in the imported space.
    worst = 0.0
    for col in colliders:
        if col.get("approx"):
            continue
        center = Vector(col["center"])
        points = _sample_collider(col)
        for point in points:
            inward = Vector(point)
            direction = center - inward
            if direction.length > 1e-6:
                inward = inward + direction.normalized() * min(0.008, 0.03)
            # FBX import with bake_space_transform already stores Unity axes.
            loc, _normal, _idx, dist = bvh.find_nearest(inward, 4.0)
            if loc is None:
                outside = 1.0
            else:
                outside = dist
            # Inside test is the parity ray. A point already near the surface counts.
            if _inside(bvh, inward):
                continue
            if outside > worst:
                worst = outside
    eval_obj.to_mesh_clear()
    return round(worst * 100.0, 2)


def _inside(bvh, origin):
    from mathutils import Vector  # type: ignore
    direction = Vector((1.0, 0.17, 0.09)).normalized()
    hits = 0
    cursor = origin.copy()
    for _ in range(64):
        loc, _normal, _idx, _dist = bvh.ray_cast(cursor, direction)
        if loc is None:
            break
        hits += 1
        cursor = loc + direction * 0.0008
    return hits % 2 == 1


def _sample_collider(col):
    c = col["center"]
    if col.get("type") == "box":
        sx, sy, sz = [float(v) * 0.5 for v in col["size"]]
        pts = []
        for x in (-sx, sx):
            for y in (-sy, sy):
                for z in (-sz, sz):
                    pts.append((c[0] + x, c[1] + y, c[2] + z))
        return pts
    if col.get("type") == "sphere":
        r = float(col["radius"])
        return [
            (c[0] + r, c[1], c[2]), (c[0] - r, c[1], c[2]),
            (c[0], c[1] + r, c[2]), (c[0], c[1] - r, c[2]),
            (c[0], c[1], c[2] + r), (c[0], c[1], c[2] - r),
        ]
    r = float(col["radius"])
    h = float(col["height"])
    return [
        (c[0], c[1] + h * 0.5, c[2]),
        (c[0], c[1] - h * 0.5, c[2]),
        (c[0] + r, c[1], c[2]),
        (c[0] - r, c[1], c[2]),
    ]


def library_entries(root):
    path = os.path.join(root, "Tools", "Blender", "AssetLibrary", "manifest.json")
    if not os.path.isfile(path):
        return []
    with open(path, "r", encoding="utf-8") as handle:
        data = json.load(handle)
    return data if isinstance(data, list) else []


def unlisted_fbx(root, entries):
    known = set()
    for entry in entries:
        rel = (entry.get("fbx") or "").replace("\\", "/")
        if rel:
            known.add(rel)
        known.add("Assets/Art/Props/Library/%s/%s.fbx" % (entry.get("category"), entry.get("name")))
    folder = os.path.join(root, "Assets", "Art", "Props", "Library")
    extra = []
    if not os.path.isdir(folder):
        return extra
    for dirpath, _dirs, files in os.walk(folder):
        for name in files:
            if not name.endswith(".fbx"):
                continue
            full = os.path.join(dirpath, name)
            rel = os.path.relpath(full, root).replace("\\", "/")
            if rel not in known:
                extra.append(rel)
    return extra


def hier_assets(root):
    found = []
    base = os.path.join(root, "Assets", "Art", "Characters", "HiPoly")
    if not os.path.isdir(base):
        return found
    for dirpath, _dirs, files in os.walk(base):
        for name in files:
            if not name.endswith(".fbx"):
                continue
            if "Hier" not in name and "Clearance" not in name:
                continue
            found.append(os.path.join(dirpath, name))
    return found


def costume_sets(root):
    path = os.path.join(root, "Art", "CharacterLab", "loadouts.json")
    if not os.path.isfile(path):
        return []
    with open(path, "r", encoding="utf-8") as handle:
        data = json.load(handle)
    return data.get("sets") or []


def fit_table(root):
    path = os.path.join(root, "Docs", "Characters", "pass1", "fit.txt")
    table = {}
    lods = {}
    if not os.path.isfile(path):
        return table, lods, ""
    text = read_text(path)
    for line in text.splitlines():
        match = re.search(r"worn\s+(\S+)\s+tris=(\d+)", line)
        if match:
            table[match.group(1)] = int(match.group(2))
        levels = re.match(r"lod\s+(\S+)\s+(\d+)/(\d+)/(\d+)\s*$", line.strip())
        if levels:
            lods[levels.group(1)] = [int(levels.group(i)) for i in (2, 3, 4)]
    return table, lods, text


def _png_rgb(path):
    """Decode an 8-bit RGB or RGBA PNG to (width, height, raw RGB bytes)."""
    data = file_blob(path)
    if len(data) < 24 or data[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    pos = 8
    width = height = color = None
    idat = b""
    plte = None
    while pos + 8 <= len(data):
        length, kind = struct.unpack(">I4s", data[pos:pos + 8])
        pos += 8
        chunk = data[pos:pos + length]
        pos += length + 4
        if kind == b"IHDR":
            width, height, bit, color = struct.unpack(">IIBB", chunk[:10])
            if bit != 8 or color not in (2, 3, 6):
                return None
        elif kind == b"PLTE":
            plte = chunk
        elif kind == b"IDAT":
            idat += chunk
        elif kind == b"IEND":
            break
    if not width or not idat:
        return None
    try:
        raw = zlib.decompress(idat)
    except zlib.error:
        return None
    channels = {2: 3, 3: 1, 6: 4}[color]
    stride = width * channels
    rows = []
    cursor = 0
    prev = bytearray(stride)
    for _y in range(height):
        if cursor >= len(raw):
            return None
        filt = raw[cursor]
        cursor += 1
        row = bytearray(raw[cursor:cursor + stride])
        cursor += stride
        if len(row) < stride:
            return None
        if filt == 1:
            for x in range(stride):
                left = row[x - channels] if x >= channels else 0
                row[x] = (row[x] + left) & 255
        elif filt == 2:
            for x in range(stride):
                row[x] = (row[x] + prev[x]) & 255
        elif filt == 3:
            for x in range(stride):
                left = row[x - channels] if x >= channels else 0
                row[x] = (row[x] + ((left + prev[x]) // 2)) & 255
        elif filt == 4:
            for x in range(stride):
                left = row[x - channels] if x >= channels else 0
                up = prev[x]
                ul = prev[x - channels] if x >= channels else 0
                pred = left + up - ul
                pa, pb, pc = abs(pred - left), abs(pred - up), abs(pred - ul)
                pick = left if pa <= pb and pa <= pc else (up if pb <= pc else ul)
                row[x] = (row[x] + pick) & 255
        elif filt != 0:
            return None
        rows.append(row)
        prev = row
    rgb = bytearray(width * height * 3)
    out = 0
    for row in rows:
        if color == 2:
            rgb[out:out + stride] = row
            out += stride
        elif color == 6:
            for x in range(width):
                i = x * 4
                rgb[out] = row[i]
                rgb[out + 1] = row[i + 1]
                rgb[out + 2] = row[i + 2]
                out += 3
        else:
            if not plte:
                return None
            for x in range(width):
                idx = row[x] * 3
                rgb[out:out + 3] = plte[idx:idx + 3]
                out += 3
    return width, height, bytes(rgb)


def garment_coverage(rgb, width, height, step=3):
    """Share of the figure that is player-color, not the grey body under it.

    A shell sitting inside the body, or z-fighting with it, leaves the figure
    grey with torn color flecks. worldMax can still be under 0.5 cm.
    """
    if width < 8 or height < 8:
        return None
    def px(x, y):
        i = (y * width + x) * 3
        return rgb[i], rgb[i + 1], rgb[i + 2]
    corners = []
    for y in (0, min(6, height - 1), height - 1):
        for x in (0, min(6, width - 1), width - 1):
            corners.append(px(x, y))
    bg = tuple(sorted(channel[axis] for channel in corners)[len(corners) // 2] for axis in range(3))
    figure = 0
    garment = 0
    for y in range(0, height, step):
        for x in range(0, width, step):
            red, green, blue = px(x, y)
            chroma = max(red, green, blue) - min(red, green, blue)
            dist = abs(red - bg[0]) + abs(green - bg[1]) + abs(blue - bg[2])
            if dist < 30 and chroma < 18:
                continue
            figure += 1
            if chroma >= 28:
                garment += 1
    if figure < 20:
        return None
    return garment / float(figure)


# A clothed figure reads as a solid player color. Under this, the still is the
# grey body with torn patches, which is a shell inside the surface.
SHELL_COVERAGE_MIN = 0.15


def shell_buried(stills):
    """Fail when a costume still shows the shell inside the grey body."""
    roles = (stills or {}).get("roles") or {}
    coverages = []
    saw_image = False
    for role in ("quarter", "side", "close", "scale"):
        info = roles.get(role) or {}
        path = info.get("path")
        if not path:
            continue
        # path is relative. Caller passes absolute via info when set.
        full = info.get("full") or path
        if not os.path.isfile(full):
            continue
        if not full.lower().endswith(".png"):
            continue
        decoded = _png_rgb(full)
        if not decoded:
            continue
        saw_image = True
        width, height, rgb = decoded
        covered = garment_coverage(rgb, width, height)
        if covered is not None:
            coverages.append(covered)
    if not coverages:
        return ["shell-unmeasured"] if saw_image else []
    worst = min(coverages)
    if worst < SHELL_COVERAGE_MIN:
        return ["shell-buried=%.0f%%" % (worst * 100.0)]
    return []


def evaluate_library(root, entry, index, licensed, tex_cache, use_bpy):
    name = entry.get("name") or "?"
    category = entry.get("category") or "?"
    cls = classify(name, category)
    reasons = []
    if not NAME_RE.match(name):
        reasons.append("name")
    rel = (entry.get("fbx") or ("Assets/Art/Props/Library/%s/%s.fbx" % (category, name))).replace("\\", "/")
    fbx = os.path.join(root, *rel.split("/"))
    if category not in (
        "Buildings", "Harbor", "Park", "Roads", "StreetFurniture", "Utility", "Vehicles", "Showcase",
    ):
        reasons.append("category")
    expect = "Assets/Art/Props/Library/%s/%s.fbx" % (category, name)
    if rel != expect:
        reasons.append("folder")
    declared_lods = entry.get("lods") or []
    reasons.extend(check_fbx_lods(fbx, declared_lods))
    mesh_lods = lod_meshes(declared_lods, fbx)
    lod_reasons, lod_text = check_lod(cls, mesh_lods)
    reasons.extend(lod_reasons)
    reasons.extend(check_scale(cls, name, entry.get("size")))
    reasons.extend(check_pivot(name, entry))
    slack = entry.get("slackCm")
    if use_bpy:
        measured = bpy_slack(fbx, entry.get("colliders") or [])
        if measured is not None:
            slack = measured
    if slack is not None:
        slack = float(slack)
    reasons.extend(check_colliders(name, entry, slack))
    reasons.extend(check_materials(root, entry, tex_cache))
    ok_lic, why = entry_licensed(entry, licensed)
    if not ok_lic:
        reasons.append(why)
    keys = snake_keys(name)
    # Catalog stills are Category_Name. A few passes use a short stem.
    keys.append(category.lower() + "_" + keys[0])
    keys.extend(STILL_ALIASES.get(name, []))
    still_reasons, stills = stills_check(index, keys, "Docs/AssetStills", category=category)
    reasons.extend(still_reasons)
    materials = []
    for lod in entry.get("lods") or []:
        materials.extend(lod.get("materials") or [])
    reasons.extend(scan_brands(fbx if os.path.isfile(fbx) else "", entry.get("blurb") or "", materials))
    dedup = []
    for reason in reasons:
        if reason not in dedup:
            dedup.append(reason)
    slack_text = "n/a" if slack is None else ("%.2f" % slack)
    return {
        "kind": "library",
        "category": category,
        "name": name,
        "cls": cls,
        "ok": not dedup,
        "reasons": dedup,
        "slack": slack_text,
        "lod": lod_text,
        "size": entry.get("size"),
        "fbx": rel,
        "stillsCheck": stills,
    }


def evaluate_unlisted(rel):
    return {
        "kind": "library",
        "category": "Unlisted",
        "name": os.path.splitext(os.path.basename(rel))[0],
        "cls": "prop",
        "ok": False,
        "reasons": ["manifest-missing", "folder"],
        "slack": "n/a",
        "lod": "n/a",
        "size": None,
        "fbx": rel,
        "stillsCheck": {"ok": False, "roles": {}},
    }


def evaluate_hier(root, path, index, licensed, proof, hip_text):
    name = os.path.splitext(os.path.basename(path))[0]
    is_candidate = "Clearance" in name or "Candidate" in path.replace("\\", "/")
    is_color = bool(re.search(r"_(Blue|Lavender|Mint|Orange|Red)_Hier", name))
    reasons = []
    spdx = licensed.get(name) if isinstance(licensed, dict) else None
    if spdx == "CC0-1.0":
        pass
    elif spdx == "OFL-1.1":
        reasons.append("license-ofl-on-mesh")
    elif name not in licensed and not getattr(licensed, "provenance", False):
        reasons.append("license")
    stills = empty_stills()
    if is_color:
        # Joints stay on the tan line. Stills are shared only when the FBX
        # bytes match that mesh; apply_identical_stills fills them in.
        reasons.extend(["stills-quarter", "stills-side", "stills-close", "stills-scale"])
    else:
        reasons.extend(check_rig_text(proof, hip_text, root, is_candidate))
        keys = ["hip_hinge_candidate", "clearance"] if is_candidate else ["hip_hinge_current", "hier"]
        still_reasons, stills = stills_check(index, keys, "Docs/LocoStills")
        reasons.extend(still_reasons)
    dedup = []
    for reason in reasons:
        if reason not in dedup:
            dedup.append(reason)
    return {
        "kind": "rig",
        "category": "Player",
        "name": name,
        "cls": "mannequin",
        "ok": not dedup,
        "reasons": dedup,
        "slack": "n/a",
        "lod": "n/a",
        "size": None,
        "fbx": os.path.relpath(path, root).replace("\\", "/"),
        "stillsCheck": stills,
    }


def mesh_islands(faces):
    """Connected triangle islands. Faces are tuples of vertex indices."""
    parent = {}

    def find(vert):
        root = parent.setdefault(vert, vert)
        while parent[root] != root:
            parent[root] = parent[parent[root]]
            root = parent[root]
        return root

    for face in faces:
        if len(face) < 2:
            continue
        root = find(face[0])
        for vert in face[1:]:
            other = find(vert)
            if other != root:
                parent[other] = root
    return len({find(vert) for face in faces for vert in face})


def _tri_area(a, b, c):
    ab = (b[0] - a[0], b[1] - a[1], b[2] - a[2])
    ac = (c[0] - a[0], c[1] - a[1], c[2] - a[2])
    cx = ab[1] * ac[2] - ab[2] * ac[1]
    cy = ab[2] * ac[0] - ab[0] * ac[2]
    cz = ab[0] * ac[1] - ab[1] * ac[0]
    return 0.5 * math.sqrt(cx * cx + cy * cy + cz * cz)


def surface_coverage(samples, cloth_points, limit):
    """Share of rest-surface area whose centroid sits within `limit` of cloth.

    `samples` is `(area, centroid)`. `cloth_points` are points on the cloth.
    """
    total = 0.0
    covered = 0.0
    if not samples:
        return None
    cell = limit / 3.0 if limit else 0.005
    grid = defaultdict(list)
    for point in cloth_points:
        grid[(
            int(math.floor(point[0] / cell)),
            int(math.floor(point[1] / cell)),
            int(math.floor(point[2] / cell)),
        )].append(point)
    if not grid:
        return 0.0
    limit_sq = limit * limit

    def near(point):
        base = (
            int(math.floor(point[0] / cell)),
            int(math.floor(point[1] / cell)),
            int(math.floor(point[2] / cell)),
        )
        for dz in (-3, -2, -1, 0, 1, 2, 3):
            for dy in (-3, -2, -1, 0, 1, 2, 3):
                for dx in (-3, -2, -1, 0, 1, 2, 3):
                    if dx * dx + dy * dy + dz * dz > 9:
                        continue
                    for cloth in grid.get((base[0] + dx, base[1] + dy, base[2] + dz), ()):
                        gap = (
                            point[0] - cloth[0],
                            point[1] - cloth[1],
                            point[2] - cloth[2],
                        )
                        if gap[0] * gap[0] + gap[1] * gap[1] + gap[2] * gap[2] <= limit_sq:
                            return True
        return False

    for area, centroid in samples:
        if area <= 0.0:
            continue
        total += area
        if near(centroid):
            covered += area
    if total <= 0.0:
        return None
    return covered / total


def _blend_mul(matrix, point):
    x, y, z = point
    return (
        matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12],
        matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13],
        matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14],
    )


_BLEND_CACHE = {}


def blend_objects(path):
    """World-space triangles and island counts from a Blender 4.00 lab file.

    Returns `{object name: {"tris", "samples", "islands"}}`, or None when the
    file is not that blend layout. Costume pieces and body segments share one
    rest pose, so the object matrix is applied.
    """
    cached = _BLEND_CACHE.get(path)
    if cached is not None or path in _BLEND_CACHE:
        return cached
    if not path or not os.path.isfile(path):
        _BLEND_CACHE[path] = None
        return None
    with open(path, "rb") as handle:
        data = handle.read()
    if not data.startswith(b"BLENDER-v400"):
        _BLEND_CACHE[path] = None
        return None
    blocks = []
    offset = 12
    while offset + 24 <= len(data):
        code = data[offset:offset + 4]
        length = struct.unpack_from("<i", data, offset + 4)[0]
        old = struct.unpack_from("<Q", data, offset + 8)[0]
        data_off = offset + 24
        if code == b"ENDB" or length < 0 or data_off + length > len(data):
            break
        blocks.append((code, old, data_off, length))
        offset = data_off + length
    by_old = {old: (data_off, length) for code, old, data_off, length in blocks if old}

    def mesh_local(mesh_off, length):
        if length < 1704:
            return None
        totvert, _edges, totpoly, totloop = struct.unpack_from("<iiii", data, mesh_off + 224)
        if totvert <= 0 or totloop <= 0 or totpoly <= 0:
            return {"local_tris": [], "faces": []}
        layers_ptr = struct.unpack_from("<Q", data, mesh_off + 248)[0]
        totlayer = struct.unpack_from("<i", data, mesh_off + 248 + 220)[0]
        if layers_ptr not in by_old or totlayer <= 0:
            return None
        layer_off, layer_len = by_old[layers_ptr]
        points = None
        for index in range(totlayer):
            base = layer_off + index * 128
            if base + 128 > layer_off + layer_len:
                break
            layer_name = data[base + 32:base + 100].split(b"\0", 1)[0].decode("latin1", "replace")
            data_ptr = struct.unpack_from("<Q", data, base + 104)[0]
            if layer_name == "position" and data_ptr in by_old:
                raw_off = by_old[data_ptr][0]
                raw = data[raw_off:raw_off + totvert * 12]
                if len(raw) < totvert * 12:
                    return None
                coords = struct.unpack("<%df" % (totvert * 3), raw)
                points = list(zip(coords[0::3], coords[1::3], coords[2::3]))
        if not points:
            return None
        loop_ptr = struct.unpack_from("<Q", data, mesh_off + 992)[0]
        loop_layers = struct.unpack_from("<i", data, mesh_off + 992 + 220)[0]
        if loop_ptr not in by_old:
            return None
        loop_off, loop_len = by_old[loop_ptr]
        corners = None
        for index in range(loop_layers):
            base = loop_off + index * 128
            if base + 128 > loop_off + loop_len:
                break
            layer_name = data[base + 32:base + 100].split(b"\0", 1)[0].decode("latin1", "replace")
            data_ptr = struct.unpack_from("<Q", data, base + 104)[0]
            if layer_name == ".corner_vert" and data_ptr in by_old:
                raw_off = by_old[data_ptr][0]
                raw = data[raw_off:raw_off + totloop * 4]
                if len(raw) < totloop * 4:
                    return None
                corners = struct.unpack("<%di" % totloop, raw)
        poly_ptr = struct.unpack_from("<Q", data, mesh_off + 240)[0]
        if corners is None or poly_ptr not in by_old:
            return None
        poly_off = by_old[poly_ptr][0]
        poly_raw = data[poly_off:poly_off + (totpoly + 1) * 4]
        if len(poly_raw) < (totpoly + 1) * 4:
            return None
        offsets = struct.unpack("<%di" % (totpoly + 1), poly_raw)
        faces = []
        tris = []
        for index in range(totpoly):
            face = corners[offsets[index]:offsets[index + 1]]
            if len(face) < 3:
                continue
            faces.append(face)
            for corner in range(1, len(face) - 1):
                tri = (points[face[0]], points[face[corner]], points[face[corner + 1]])
                tris.append(tri)
        return {"local_tris": tris, "faces": faces}

    meshes = {}
    for code, old, data_off, length in blocks:
        if code != b"ME\x00\x00":
            continue
        meshes[old] = mesh_local(data_off, length)

    objects = {}
    for code, _old, data_off, length in blocks:
        if code != b"OB\x00\x00" or length < 796:
            continue
        name = data[data_off + 40:data_off + 106].split(b"\0", 1)[0].decode("latin1", "replace")
        if len(name) < 3:
            continue
        name = name[2:]
        data_ptr = struct.unpack_from("<Q", data, data_off + 376)[0]
        matrix = struct.unpack_from("<16f", data, data_off + 732)
        local = meshes.get(data_ptr)
        if not local:
            objects[name] = {"tris": [], "samples": [], "islands": 0}
            continue
        tris = []
        samples = []
        cloth_points = []
        for a, b, c in local["local_tris"]:
            wa, wb, wc = _blend_mul(matrix, a), _blend_mul(matrix, b), _blend_mul(matrix, c)
            tris.append((wa, wb, wc))
            area = _tri_area(wa, wb, wc)
            centroid = ((wa[0] + wb[0] + wc[0]) / 3.0, (wa[1] + wb[1] + wc[1]) / 3.0, (wa[2] + wb[2] + wc[2]) / 3.0)
            samples.append((area, centroid))
            cloth_points.extend((wa, wb, wc, centroid))
        objects[name] = {
            "tris": tris,
            "samples": samples,
            "points": cloth_points,
            "islands": mesh_islands(local["faces"]),
        }
    _BLEND_CACHE[path] = objects
    return objects


def cloth_reasons(root, pieces, hide):
    """Fail a shell that no longer covers its segment, or a piece cut into islands.

    Each loadout piece is one source mesh. More than one connected island means
    the cut added shards (`cloth-shards`). Each dressed body segment must keep
    at least 90% of its rest-pose area within the cloth band (`cloth-coverage`).
    """
    path = os.path.join(root, "Art", "CharacterLab", "CostumeLab.blend")
    if not pieces or not os.path.isfile(path):
        return []
    lab = blend_objects(path)
    if lab is None:
        return ["cloth-coverage", "cloth-shards"]
    reasons = []
    for piece in pieces:
        mesh = lab.get(piece)
        if mesh and mesh["islands"] > 1:
            reasons.append("cloth-shards")
            break
    dressed = list(hide or [])
    if not dressed:
        dressed = [name for name in lab if name.startswith("Mesh_")]
    for segment in dressed:
        body = lab.get(segment)
        points = []
        for piece in pieces:
            mesh = lab.get(piece)
            if mesh:
                points.extend(mesh.get("points") or [])
        if body is None or not body["samples"]:
            reasons.append("cloth-coverage")
            break
        fraction = surface_coverage(body["samples"], points, CLOTH_COVER_M)
        if fraction is None or fraction < CLOTH_COVER_MIN:
            reasons.append("cloth-coverage")
            break
    return reasons


def evaluate_costume(root, item, index, licensed, fit, fit_lods, fit_text, proof, lane_keys=None):
    name = item.get("id") or "?"
    reasons = []
    spdx = licensed.get(name) if isinstance(licensed, dict) else None
    if spdx == "CC0-1.0":
        pass
    elif spdx == "OFL-1.1":
        reasons.append("license-ofl-on-mesh")
    elif name not in licensed and not getattr(licensed, "provenance", False):
        reasons.append("license")
    pieces = item.get("pieces") or []
    if not pieces:
        reasons.append("pieces-missing")
    for piece in pieces:
        low = piece.lower()
        if any(ban in low for ban in CLOTH_BAN):
            reasons.append("not-clothing")
            break
        if not any(ok in low for ok in CLOTH_OK):
            reasons.append("not-clothing")
            break
    declared = list(item.get("lods") or [])
    if not declared and name in fit_lods:
        counts = fit_lods[name]
        declared = [
            {"lod": index, "tris": counts[index], "mesh": "LOD%d" % index}
            for index in range(len(counts))
        ]
    blend = os.path.join(root, "Art", "CharacterLab", "CostumeLab.blend")
    mesh_lods = lod_meshes(declared, blend if os.path.isfile(blend) else None, pieces)
    if not mesh_lods:
        reasons.append("lod-missing")
        lod_text = "n/a"
    else:
        lod_reasons, lod_text = check_lod("costume", mesh_lods)
        reasons.extend(lod_reasons)
    if not fit_text:
        reasons.append("fit-missing")
    else:
        header = re.search(r"worldMax=([0-9.]+)\s+fails=(\d+)", fit_text)
        if not header:
            reasons.append("fit-missing")
        else:
            if float(header.group(1)) > NOCLIP_CM:
                reasons.append("cloth=%.2fcm" % float(header.group(1)))
            if int(header.group(2)) != 0:
                reasons.append("cloth-fails=%s" % header.group(2))
        band = re.search(r"cloth-band min=([0-9.]+) max=([0-9.]+)", fit_text)
        if band:
            lo, hi = float(band.group(1)), float(band.group(2))
            if lo < 0.3 or hi > 1.0:
                reasons.append("cloth-band")
    # Costumes follow the clearance rig. A branch without that mesh has not caught up.
    candidate = os.path.join(
        root, "Assets", "Art", "Characters", "HiPoly", "Candidate",
        "Dummy_Mannequin_Tan_Hier_Clearance.fbx",
    )
    if not os.path.isfile(candidate):
        reasons.append("rig-not-clearance")
    if proof:
        match = re.search(r"pose=(\d+)", proof)
        if match and int(match.group(1)) != 0:
            reasons.append("rig-pose=%s" % match.group(1))
    else:
        reasons.append("rig-proof-missing")
    still_reasons, stills = stills_check(
        index, snake_keys(name), "Docs/Characters", extra_readability=True, lane_keys=lane_keys,
    )
    reasons.extend(still_reasons)
    reasons.extend(shell_buried(stills))
    reasons.extend(cloth_reasons(root, pieces, item.get("hide") or []))
    dedup = []
    for reason in reasons:
        if reason not in dedup:
            dedup.append(reason)
    return {
        "kind": "costume",
        "category": "Costume",
        "name": name,
        "cls": "costume",
        "ok": not dedup,
        "reasons": dedup,
        "slack": "n/a",
        "lod": lod_text,
        "size": None,
        "fbx": "Art/CharacterLab/loadouts.json",
        "stillsCheck": stills,
    }


def is_paperwork(reason):
    return reason == "license" or reason.startswith("license-") or reason.startswith("stills-")


def split_fails(rows):
    paperwork = 0
    geometry = 0
    for row in rows:
        if row["ok"]:
            continue
        if all(is_paperwork(reason) for reason in row["reasons"]):
            paperwork += 1
        else:
            geometry += 1
    return paperwork, geometry


def line_for(row):
    status = "PASS" if row["ok"] else "FAIL"
    reasons = "ok" if row["ok"] else ",".join(row["reasons"])
    base = row.get("variantOf")
    if base:
        note = "material-variant of %s" % base
        reasons = note if row["ok"] else reasons + "," + note
    return "%s %s/%s slack=%scm lod=%s class=%s reasons=%s" % (
        status, row["category"], row["name"], row["slack"], row["lod"], row["cls"], reasons,
    )


def run(root, report_path=None):
    root = os.path.abspath(root)
    index = StillIndex(root)
    licensed = license_names(root)
    tex_cache = {}
    use_bpy = False
    try:
        import bpy  # noqa: F401
        use_bpy = True
    except ImportError:
        use_bpy = False
    proof, hip_text = latest_proof(root)
    rows = []
    entries = library_entries(root)
    for entry in entries:
        rows.append(evaluate_library(root, entry, index, licensed, tex_cache, use_bpy))
    for rel in unlisted_fbx(root, entries):
        rows.append(evaluate_unlisted(rel))
    for path in hier_assets(root):
        rows.append(evaluate_hier(root, path, index, licensed, proof, hip_text))
    fit, fit_lods, fit_text = fit_table(root)
    costumes = costume_sets(root)
    lane_keys = [snake_keys(item.get("id") or "") for item in costumes]
    for item in costumes:
        rows.append(evaluate_costume(
            root, item, index, licensed, fit, fit_lods, fit_text, proof, lane_keys,
        ))
    apply_identical_stills(rows, index, root)
    rows.sort(key=lambda row: (row["category"], row["name"]))
    passed = sum(1 for row in rows if row["ok"])
    failed = len(rows) - passed
    paperwork, geometry = split_fails(rows)
    for row in rows:
        print(line_for(row))
    print("models-validate assets=%d pass=%d fail=%d" % (len(rows), passed, failed))
    print("models-split paperwork=%d geometry=%d" % (paperwork, geometry))
    if report_path:
        with open(report_path, "w", encoding="utf-8") as handle:
            json.dump({
                "assets": len(rows),
                "pass": passed,
                "fail": failed,
                "paperwork": paperwork,
                "geometry": geometry,
                "rows": rows,
            }, handle, indent=2)
            handle.write("\n")
    return 0 if failed == 0 else 1


def self_test():
    assert png_size(b"\x89PNG\r\n\x1a\n" + struct.pack(">II", 13, 0x49484452)[:0] or b"") is None
    blob = b"\x89PNG\r\n\x1a\n" + struct.pack(">I", 13) + b"IHDR" + struct.pack(">II", 1280, 720)
    assert png_size(blob + b"\x00" * 8) == (1280, 720)
    assert classify("Sedan_Mid_A_25", "Vehicles") == "sedan_mid"
    assert classify("Bus_City60", "Vehicles") == "bus60"
    assert classify("Brick_Wall", "Buildings") == "building"
    assert classify("Ranch_House", "Buildings") == "building_shell"
    assert model_year("Sedan_Mid_A_21") == 2021
    assert model_year("Sedan_Mid_A_25") == 2025
    assert model_year("Bus_City40") is None
    assert in_range(1.84, ENVELOPES["sedan_mid"][1])
    assert not in_range(1.975, ENVELOPES["sedan_mid"][1])
    reasons, _lod = check_lod("prop", [{"tris": 5502}, {"tris": 196}])
    assert "lod0-budget" in reasons and "lod2-missing" in reasons
    assert role_of("fountain_bowl") == "close"
    assert role_of("wood_pole_scale") == "scale"
    assert role_of("lineup-three-quarter") == "quarter"
    table = parse_license_table(
        "\n".join([
            "| Asset | SPDX | Source |",
            "| --- | --- | --- |",
            "| `Reed_1_Hood` | CC0-1.0 | original |",
            "The shipped `Sedan_Mid_A` shell is original.",
        ])
    )
    assert table == {"Reed_1_Hood": "CC0-1.0"}
    assert "Sedan_Mid_A" not in table
    declared = [
        {"lod": 0, "tris": 100, "mesh": "LOD0"},
        {"lod": 1, "tris": 40, "mesh": "LOD1"},
        {"lod": 2, "tris": 10, "mesh": "LOD2"},
    ]
    assert len(lod_meshes(declared, None)) == 3
    assert lod_meshes([{"tris": 100}], None, ["Lab_Hood"]) == []
    # Grey body with a few red flecks is buried. A solid red figure is not.
    grey = bytes([180, 178, 176])
    body = bytes([90, 88, 86])
    red = bytes([210, 40, 36])
    side = 24
    buried = bytearray(grey * (side * side))
    solid = bytearray(grey * (side * side))
    for y in range(4, 20):
        for x in range(4, 20):
            i = (y * side + x) * 3
            buried[i:i + 3] = body
            solid[i:i + 3] = red
            if (x + y) % 11 == 0:
                buried[i:i + 3] = red
    assert garment_coverage(bytes(buried), side, side, step=1) < SHELL_COVERAGE_MIN
    assert garment_coverage(bytes(solid), side, side, step=1) >= SHELL_COVERAGE_MIN
    assert snake_keys("Sedan_Mid_A_22") == ["sedan_mid_a_22"]
    assert snake_keys("FireHydrant_Red") == ["firehydrant_red"]
    assert object_tokens("Docs/AssetStills/pass1/planter_street_quarter.jpg") == ["planter", "street"]
    assert object_tokens("Docs/AssetStills/street_objects/pass1/planter_quarter.jpg") == ["planter"]
    assert object_tokens("Docs/AssetStills/pass1/woodfence_corner_quarter.jpg") == ["woodfence", "corner"]
    assert object_tokens("Docs/AssetStills/vehicles/sedan_mid_a/pass15/side.jpg") == ["sedan", "mid", "a"]
    assert not key_hits(["planter"], ["planter", "street"])
    assert not key_hits(["woodfence", "corner"], ["woodfence"])
    assert key_hits(["woodfence", "corner"], ["woodfence", "corner"])
    ratio_bad, _ratio_lod = check_lod("prop", [{"tris": 400}, {"tris": 196}, {"tris": 196}])
    assert "lod2-ratio" in ratio_bad
    ratio_ok, _ratio_ok_lod = check_lod("prop", [{"tris": 400}, {"tris": 200}, {"tris": 100}])
    assert "lod2-ratio" not in ratio_ok
    ratio_edge, _ratio_edge_lod = check_lod("prop", [{"tris": 400}, {"tris": 200}, {"tris": 120}])
    assert "lod2-ratio" not in ratio_edge
    siblings = [(["sedan", "mid", "a", "22"], "h1"), (["sedan", "mid", "a", "23"], "h2")]
    assert not still_allowed(["sedan", "mid", "a"], ["sedan", "mid", "a", "22"], "h1", siblings)
    assert still_allowed(["sedan", "mid", "a", "22"], ["sedan", "mid", "a", "22"], "h1", siblings)
    same = [(["container", "20"], "h"), (["container", "20", "blue"], "h")]
    assert still_allowed(["container", "20"], ["container", "20", "blue"], "h", same)
    fences = [(["woodfence"], "h0"), (["woodfence", "corner"], "h1")]
    assert still_allowed(["woodfence"], ["woodfence"], "h0", fences)
    assert not still_allowed(["woodfence"], ["woodfence", "corner"], "h1", fences)
    assert not still_allowed(["planter"], ["planter", "street"], None, [(["planter", "street"], None)])
    paints = [
        (["sedan", "mid", "a", "25"], "cage", "Sedan_Mid_A_25"),
        (["sedan", "mid", "a", "25", "white"], "cage", "Sedan_Mid_A_25_White"),
        (["sedan", "mid", "a", "22"], "other", "Sedan_Mid_A_22"),
    ]
    assert still_allowed(["sedan", "mid", "a", "25"], ["sedan", "mid", "a", "25", "white"], "cage", paints)
    assert not still_allowed(["sedan", "mid", "a"], ["sedan", "mid", "a", "25", "white"], "cage", paints)
    assert variant_base(
        ["sedan", "mid", "a", "25"], "Sedan_Mid_A_25_White", "Vehicles",
        [item + ("Vehicles",) for item in paints],
    ) == "Sedan_Mid_A_25"

    def _array_prop(code, raw):
        width = {"d": 8, "i": 4}[code]
        return code.encode() + struct.pack("<III", len(raw) // width, 0, len(raw)) + raw

    def _string_prop(text):
        raw = text if isinstance(text, bytes) else text.encode()
        return b"S" + struct.pack("<I", len(raw)) + raw

    def _emit(name, props, child_fns, start):
        name_b = name.encode("ascii")
        prop_blob = b"".join(props)
        cursor = start + 13 + len(name_b) + len(prop_blob)
        child_blob = b""
        for child_fn in child_fns:
            piece = child_fn(cursor)
            child_blob += piece
            cursor += len(piece)
        end = cursor + 13
        header = struct.pack("<III", end, len(props), len(prop_blob))
        header += bytes([len(name_b)]) + name_b
        return header + prop_blob + child_blob + (b"\x00" * 13)

    def _leaf(name, props):
        return lambda start: _emit(name, props, [], start)

    def _fbx(uv_byte):
        verts = struct.pack("<6d", 0, 0, 0, 1, 0, 0)
        indices = struct.pack("<3i", 0, 1, -3)
        uvs = struct.pack("<4d", 0, 0, uv_byte, 0)
        header = b"Kaydara FBX Binary  \x00\x1a\x00" + struct.pack("<I", 7400)

        def geom(start):
            return _emit("Geometry", [
                b"I" + struct.pack("<i", 7),
                _string_prop(b"LOD0\x00\x01Geometry"),
                _string_prop(b"Mesh"),
            ], [
                _leaf("Vertices", [_array_prop("d", verts)]),
                _leaf("PolygonVertexIndex", [_array_prop("i", indices)]),
                _leaf("UV", [_array_prop("d", uvs)]),
            ], start)

        body = _emit("Objects", [], [geom], len(header))
        return header + body + (b"\x00" * 13)

    same = geometry_digest(_fbx(1))
    assert same and same == geometry_digest(_fbx(1))
    assert same != geometry_digest(_fbx(2))
    noted = {"ok": True, "reasons": [], "variantOf": "Container_20", "category": "Harbor",
             "name": "Container_20_Blue", "slack": "0.00", "lod": "1/1/1", "cls": "harbor_large"}
    assert "material-variant of Container_20" in line_for(noted)
    assert line_for(noted).startswith("PASS ")

    def _png(width, height, paint):
        raw = bytearray()
        for y in range(height):
            raw.append(0)
            for x in range(width):
                raw.extend(paint(x, y))
        comp = zlib.compress(bytes(raw))

        def chunk(tag, payload):
            body = tag + payload
            return struct.pack(">I", len(payload)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

        ihdr = struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0)
        return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", comp) + chunk(b"IEND", b"")

    def _plate(width, height, box, figure=False):
        def paint(x, y):
            if figure and 8 <= x <= 12 and 6 <= y <= 20:
                if y <= 8:
                    return bytes((240, 240, 230))
                return bytes((60, 140, 250))
            x0, y0, x1, y1 = box
            if x0 <= x <= x1 and y0 <= y <= y1:
                return bytes((30, 30, 30))
            return bytes((180, 182, 184))
        return _png(width, height, paint)

    wide = _plate(80, 48, (18, 12, 58, 36))
    decoded = decode_png(wide)
    assert decoded is not None and decoded[0] == 80 and decoded[1] == 48
    import tempfile
    tmp = tempfile.mkdtemp(prefix="frame-")
    good = os.path.join(tmp, "good.png")
    cropped = os.path.join(tmp, "cropped.png")
    tiny = os.path.join(tmp, "tiny.png")
    huge = os.path.join(tmp, "huge.png")
    scaled = os.path.join(tmp, "scale.png")
    with open(good, "wb") as handle:
        handle.write(wide)
    with open(cropped, "wb") as handle:
        handle.write(_plate(80, 48, (0, 10, 50, 36)))
    with open(tiny, "wb") as handle:
        handle.write(_plate(80, 48, (30, 20, 40, 26)))
    with open(huge, "wb") as handle:
        handle.write(_plate(80, 48, (2, 2, 77, 45)))
    with open(scaled, "wb") as handle:
        handle.write(_plate(80, 48, (18, 12, 58, 36), figure=True))
    assert frame_reasons("quarter", {"full": good}) == []
    assert "stills-quarter-edge" in frame_reasons("quarter", {"full": cropped})
    assert "stills-side-coverage" in frame_reasons("side", {"full": tiny})
    assert "stills-quarter-coverage" in frame_reasons("quarter", {"full": huge})
    assert frame_reasons("close", {"full": cropped}) == []
    assert frame_reasons("scale", {"full": scaled}) == []
    assert "stills-scale-figure" in frame_reasons("scale", {"full": good})
    assert "stills-frame-edge" in frame_reasons("frame", {"full": cropped})
    green = os.path.join(tmp, "green-scale.png")
    with open(green, "wb") as handle:
        def _green(x, y):
            if 30 <= x <= 36 and 8 <= y <= 28:
                if y <= 11:
                    return bytes((180, 184, 186))
                return bytes((40, 170, 70))
            if 18 <= x <= 58 and 12 <= y <= 36:
                return bytes((30, 30, 30))
            return bytes((180, 182, 184))
        handle.write(_png(80, 48, _green))
    assert "stills-scale-figure" in frame_reasons("scale", {"full": green})
    raw = open(green, "rb").read()
    payload = b"Figure\x00Mannequin"
    body = b"tEXt" + payload
    chunk = struct.pack(">I", len(payload)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)
    end = raw.rfind(b"IEND") - 4
    tagged = os.path.join(tmp, "tagged-scale.png")
    with open(tagged, "wb") as handle:
        handle.write(raw[:end] + chunk + raw[end:])
    assert figure_name_tag(tagged)
    assert "stills-scale-figure" not in frame_reasons("scale", {"full": tagged})
    assert not figure_name_tag(green)
    assert provenance_claimed(PROVENANCE_LINE + "\n")
    assert not provenance_claimed("The models were made in-house, CC0, free to use today.")
    covered = LicenseBook()
    covered.provenance = True
    assert entry_licensed({"name": "Cabin"}, covered) == (True, "")
    bare = LicenseBook()
    assert entry_licensed({"name": "Cabin"}, bare) == (False, "license")
    download = {"name": "Bench", "license": {"spdx": "CC0-1.0", "source": "cc0-download", "url": ""}}
    assert entry_licensed(download, covered) == (False, "license-bad")
    assert named_role("sprint") is None
    assert named_role("hero") == "quarter"
    assert mesh_islands([(0, 1, 2), (2, 1, 3)]) == 1
    assert mesh_islands([(0, 1, 2), (3, 4, 5)]) == 2
    full = surface_coverage(
        [(1.0, (0.0, 0.0, 0.0)), (1.0, (0.01, 0.0, 0.0))],
        [(0.0, 0.0, 0.005), (0.01, 0.0, 0.005)],
        CLOTH_COVER_M,
    )
    shards = surface_coverage(
        [(1.0, (0.0, 0.0, 0.0)), (1.0, (0.2, 0.0, 0.0))],
        [(0.0, 0.0, 0.005)],
        CLOTH_COVER_M,
    )
    assert full is not None and full >= CLOTH_COVER_MIN
    assert shards is not None and shards < CLOTH_COVER_MIN
    print("self-test ok")
    return 0


def main(argv):
    if "--" in argv:
        argv = argv[argv.index("--") + 1:]
    parser = argparse.ArgumentParser(description="Validate Tag model assets against Docs/Models/STANDARD.md")
    parser.add_argument("--root", default=".", help="Checkout to grade")
    parser.add_argument("--report", default="", help="Optional JSON report path")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args(argv)
    if args.self_test:
        return self_test()
    return run(args.root, args.report or None)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
