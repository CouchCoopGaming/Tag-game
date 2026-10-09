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
import os
import re
import struct
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
FIGURE_H = (1.75, 1.85)
YEAR_MIN, YEAR_MAX = 2022, 2026
NOCLIP_CM = 0.5
HIP_PLANT_M = 0.08
HIP_CROUCH_M = 0.12
HIP_FLEX_RATIO = 1.5

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


def role_of(stem):
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
    return "quarter"


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


def license_names(root):
    named = {}
    for dirpath, _dirs, files in os.walk(root):
        if ".git" in dirpath.split(os.sep):
            continue
        for name in files:
            if name != "LICENSES.md":
                continue
            text = read_text(os.path.join(dirpath, name))
            named.update(parse_license_table(text))
    return named


def entry_licensed(entry, named):
    lic = entry.get("license")
    if isinstance(lic, dict):
        spdx = str(lic.get("spdx") or "")
        if spdx in ("CC0-1.0", "OFL-1.1") and lic.get("source"):
            if spdx == "OFL-1.1":
                return False, "license-ofl-on-mesh"
            return True, ""
        return False, "license-bad"
    spdx = named.get(entry.get("name") or "")
    if spdx == "CC0-1.0":
        return True, ""
    if spdx == "OFL-1.1":
        return False, "license-ofl-on-mesh"
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


def stills_check(index, keys, scope, extra_readability=False, category=None):
    """Per-asset quartet check. PassN, 1280x720 or larger, each file under 400 KB.

    Returns (reasons, stillsCheck). stillsCheck is the record a ledger row can quote.
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
            if not PASS_RE.search((row.get("rel") or "") + "/"):
                reasons.append("stills-%s-pass" % role)
            elif size is None or size[0] < STILL_MIN[0] or size[1] < STILL_MIN[1]:
                reasons.append("stills-%s-res" % role)
            elif nbytes is not None and nbytes > STILL_MAX_BYTES:
                reasons.append("stills-%s-bytes" % role)
            else:
                info["ok"] = True
        roles[role] = info
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
    return []


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
    elif name not in licensed:
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


def evaluate_costume(root, item, index, licensed, fit, fit_lods, fit_text, proof):
    name = item.get("id") or "?"
    reasons = []
    spdx = licensed.get(name) if isinstance(licensed, dict) else None
    if spdx == "CC0-1.0":
        pass
    elif spdx == "OFL-1.1":
        reasons.append("license-ofl-on-mesh")
    elif name not in licensed:
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
        index, snake_keys(name), "Docs/Characters", extra_readability=True,
    )
    reasons.extend(still_reasons)
    reasons.extend(shell_buried(stills))
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
    for item in costume_sets(root):
        rows.append(evaluate_costume(root, item, index, licensed, fit, fit_lods, fit_text, proof))
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
