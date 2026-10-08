#!/usr/bin/env python3
"""Hand-built Batman '66 / Spider-Verse comic words from the bundled Bangers font.

Each cell is a 1024 square drawn at 2x and downsampled. The burst, the inner
burst, the Ben-Day screen, and the letters are all in the texture so the game
can show one bilinear quad.
"""
import base64
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
FONT_PATH = os.path.join(ROOT, "Assets", "Art", "FX", "Fonts", "Bangers-Regular.ttf")
PNG_PATH = os.path.join(ROOT, "Assets", "Art", "FX", "ComicAtlas.png")
CS_PATH = os.path.join(ROOT, "Assets", "Scripts", "FX", "ComicAtlas.cs")
STILL_DIR = os.path.join(ROOT, "Docs", "AnimStills", "pass4")

CELL = 1024
SS = 2

# 5-wide, 7-row glyphs. Same bits the old in-game font used.
OLD_GLYPHS = {
    "P": (0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10),
    "O": (0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E),
    "W": (0x11, 0x11, 0x11, 0x15, 0x15, 0x15, 0x0A),
    "B": (0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E),
    "A": (0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11),
    "M": (0x11, 0x1B, 0x15, 0x11, 0x11, 0x11, 0x11),
    "!": (0x04, 0x04, 0x04, 0x04, 0x04, 0x00, 0x04),
    "H": (0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11),
}


def dilate(mask, radius):
    """Even ink width. Square kernels in short steps read as a marker, not a halo."""
    img = mask
    r = int(round(radius))
    while r > 0:
        step = 3 if r >= 3 else 1
        img = img.filter(ImageFilter.MaxFilter(step * 2 + 1))
        r -= step
    return img


def erode(mask, radius):
    img = mask
    r = int(round(radius))
    while r > 0:
        step = 3 if r >= 3 else 1
        img = img.filter(ImageFilter.MinFilter(step * 2 + 1))
        r -= step
    return img


def raster(size, pts):
    mask = Image.new("L", (size, size), 0)
    if len(pts) >= 3:
        ImageDraw.Draw(mask).polygon([(int(round(x)), int(round(y))) for x, y in pts], fill=255)
    return mask


def paint(canvas, mask, color):
    ink = Image.new("RGBA", canvas.size, color[:3] + (255,))
    if color[3] >= 255:
        ink.putalpha(mask)
    else:
        faded = mask.point(lambda p, a=color[3]: (p * a) >> 8)
        ink.putalpha(faded)
    canvas.alpha_composite(ink)


def shift_mask(mask, dx, dy):
    out = Image.new("L", mask.size, 0)
    out.paste(mask, (int(dx), int(dy)))
    return out


def star_points(spec, cx, cy, scale, ax, ay):
    pts = []
    for deg, rad in spec:
        a = math.radians(deg - 90.0)
        pts.append((cx + math.cos(a) * rad * scale * ax, cy + math.sin(a) * rad * scale * ay))
    return pts


def fit_scale(spec, ax, ay, size, outline, margin):
    """Largest scale whose dilated outline stays inside the cell."""
    limit = size * 0.5 - outline - margin
    need = 1.0
    for deg, rad in spec:
        a = math.radians(deg - 90.0)
        need = max(need, abs(math.cos(a) * rad * ax), abs(math.sin(a) * rad * ay))
    return limit / need


def benday(mask, color, spacing, radius, angle):
    """Print dots. Centers sit fully inside the mask so the edge stays a clean contour."""
    w, h = mask.size
    inset = erode(mask, radius + 2)
    src = np.asarray(inset)
    ang = math.radians(angle)
    ca, sa = math.cos(ang), math.sin(ang)
    step = float(spacing)
    reach = int(math.hypot(w, h)) + step
    n = int(reach / step) + 2
    rad = int(radius)
    disk_y, disk_x = np.ogrid[-rad:rad + 1, -rad:rad + 1]
    disk = ((disk_x * disk_x + disk_y * disk_y) <= rad * rad).astype(np.uint8) * 255
    ink = np.zeros((h, w), np.uint8)
    for iy in range(-n, n):
        for ix in range(-n, n):
            gx = (ix + (0.5 if (iy & 1) else 0.0)) * step
            gy = iy * step
            x = int(round(ca * gx - sa * gy + w * 0.5))
            y = int(round(sa * gx + ca * gy + h * 0.5))
            if x < rad or y < rad or x >= w - rad or y >= h - rad:
                continue
            if src[y, x] < 200:
                continue
            y0, y1 = y - rad, y + rad + 1
            x0, x1 = x - rad, x + rad + 1
            patch = ink[y0:y1, x0:x1]
            np.maximum(patch, disk, out=patch)
    dot = Image.new("RGBA", (w, h), color)
    dot.putalpha(Image.fromarray(ink, "L"))
    return dot


def burst_masks(size, outer_spec, inner_spec, ax, ay, outline, inner_outline, inner_scale, ox, oy):
    # Padding keeps bilinear sampling inside this cell when the four words share one atlas.
    scale = fit_scale(outer_spec, ax, ay, size, outline, 56)
    cx = cy = size * 0.5
    outer_pts = star_points(outer_spec, cx, cy, scale, ax, ay)
    inner_pts = star_points(inner_spec, cx + ox, cy + oy, scale * inner_scale, ax, ay)
    outer = raster(size, outer_pts)
    inner = raster(size, inner_pts)
    outer_key = dilate(outer, outline)
    inner_key = dilate(inner, inner_outline)
    return outer, outer_key, inner, inner_key


def darker(rgb, mul):
    return tuple(max(0, min(255, int(c * mul))) for c in rgb[:3]) + (255,)


def paint_speed_lines(canvas, outer):
    """Short wedges behind a tag word so BAM and WHAM read bigger than a punch."""
    bb = outer.getbbox()
    if not bb:
        return
    cx = canvas.size[0] * 0.5
    cy = canvas.size[1] * 0.5
    rad = max(bb[2] - bb[0], bb[3] - bb[1]) * 0.5
    limit = canvas.size[0] - 18
    overlay = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    wedges = (
        (-32, 1.22, 11), (-12, 1.36, 8), (14, 1.18, 13), (38, 1.30, 9),
        (64, 1.16, 12), (92, 1.34, 8), (118, 1.20, 14), (148, 1.28, 9),
        (176, 1.14, 11), (204, 1.32, 8), (232, 1.18, 13), (258, 1.30, 9),
        (286, 1.15, 12), (314, 1.28, 8), (340, 1.18, 11), (358, 1.26, 10),
    )
    for deg, reach, half in wedges:
        a = math.radians(deg - 90.0)
        ca, sa = math.cos(a), math.sin(a)
        px, py = -sa, ca
        r0 = rad * 0.58
        r1 = min(rad * reach, limit - 4)
        # Keep the tip on the canvas.
        for _ in range(6):
            x1 = cx + ca * r1
            y1 = cy + sa * r1
            if 16 <= x1 <= limit and 16 <= y1 <= limit:
                break
            r1 *= 0.92
        root = half * 0.35
        draw.polygon((
            (cx + ca * r0 + px * root, cy + sa * r0 + py * root),
            (cx + ca * r1 + px * half, cy + sa * r1 + py * half),
            (cx + ca * r1 - px * half, cy + sa * r1 - py * half),
            (cx + ca * r0 - px * root, cy + sa * r0 - py * root),
        ), fill=(12, 8, 14, 255))
    canvas.alpha_composite(overlay)


def paint_burst(canvas, outer, outer_key, inner, inner_key, fill, inner_c, dot_spacing, dot_radius, dot_angle, fringe=None):
    shadow = shift_mask(outer_key, 14, 20)
    paint(canvas, shadow, (0, 0, 0, 80))
    if fringe:
        cyan, magenta = fringe
        paint(canvas, shift_mask(outer, -16, 4), cyan)
        paint(canvas, shift_mask(outer, 16, -4), magenta)
    paint(canvas, outer_key, (8, 6, 10, 255))
    paint(canvas, outer, fill)
    # Darker dots of the outer color, in the ring the inner burst does not cover.
    hole = dilate(inner_key, 4)
    dot_mask = Image.fromarray(np.where((np.asarray(outer) > 128) & (np.asarray(hole) < 128), 255, 0).astype(np.uint8), "L")
    canvas.alpha_composite(benday(dot_mask, darker(fill, 0.38), dot_spacing, dot_radius, dot_angle))
    lip = Image.fromarray(np.where((np.asarray(outer) > 128) & (np.asarray(erode(outer, 7)) < 128), 255, 0).astype(np.uint8), "L")
    lip_c = tuple(min(255, int(c + (255 - c) * 0.45)) for c in fill[:3]) + (255,)
    paint(canvas, lip, lip_c)
    paint(canvas, inner_key, (8, 6, 10, 255))
    paint(canvas, inner, inner_c)
    # A second, obvious screen on the inner burst, in a darker shade of that color.
    canvas.alpha_composite(benday(inner, darker(inner_c, 0.42), dot_spacing, max(6, dot_radius - 2), dot_angle + 18))
    return outer


def glyph_mask(font, ch, scale):
    bbox = font.getbbox(ch)
    pad = 8
    w = max(8, bbox[2] - bbox[0] + pad * 2)
    h = max(8, bbox[3] - bbox[1] + pad * 2)
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).text((pad - bbox[0], pad - bbox[1]), ch, font=font, fill=255)
    if abs(scale - 1.0) > 0.01:
        mask = mask.resize((max(1, int(mask.width * scale)), max(1, int(mask.height * scale))), Image.Resampling.LANCZOS)
    bb = mask.getbbox()
    if bb:
        mask = mask.crop(bb)
    return mask


def shear_mask(mask, shear):
    if abs(shear) < 0.001:
        return mask
    arr = np.asarray(mask)
    h, w = arr.shape
    extra = int(abs(shear) * h) + 4
    out = np.zeros((h, w + extra), np.uint8)
    shift = extra // 2
    for y in range(h):
        off = int(round(shear * (h * 0.12 - y))) + shift
        x0 = max(0, off)
        x1 = min(out.shape[1], off + w)
        if x1 <= x0:
            continue
        src0 = x0 - off
        src1 = src0 + (x1 - x0)
        out[y, x0:x1] = arr[y, src0:src1]
    img = Image.fromarray(out, "L")
    bb = img.getbbox()
    return img.crop(bb) if bb else img


def rotate_mask(mask, deg):
    if abs(deg) < 0.2:
        return mask
    img = mask.rotate(deg, expand=True, resample=Image.Resampling.BICUBIC)
    bb = img.getbbox()
    return img.crop(bb) if bb else img


def style_letter(mask, face_top, face_bot, side, highlight, stroke, depth):
    arr = np.asarray(mask)
    h, w = arr.shape
    solid = (arr > 140).astype(np.uint8) * 255
    face_m = Image.fromarray(solid, "L")
    stroke_m = dilate(face_m, stroke)
    # Extrusion is a stack of the face stepping down-right, then a black rim.
    ext = np.zeros((h + depth + 4, w + depth + 4), np.uint8)
    body = solid
    for i in range(depth, 0, -1):
        ext[i:i + h, i:i + w] = np.maximum(ext[i:i + h, i:i + w], body)
    ext_m = Image.fromarray(ext, "L")
    ext_key = dilate(ext_m, max(3, stroke // 3))
    # Face stroke sits at the upper left so the colored side shows on the lower right.
    canvas = Image.new("RGBA", ext_key.size, (0, 0, 0, 0))
    paint(canvas, ext_key, (6, 4, 8, 255))
    paint(canvas, ext_m, side)
    face_key = Image.new("L", canvas.size, 0)
    face_key.paste(stroke_m, (0, 0))
    face = Image.new("L", canvas.size, 0)
    face.paste(face_m, (0, 0))
    paint(canvas, face_key, (6, 4, 8, 255))
    # Thin white key just inside the black, then the color sits inside that.
    white_px = max(4, stroke // 4)
    color_m = erode(face_m, white_px)
    paint(canvas, face, (255, 255, 255, 255))
    face = Image.new("L", canvas.size, 0)
    face.paste(color_m, (0, 0))
    # Vertical ink: bright cap, then a harder shadow in the lower third.
    fh, fw = np.asarray(face).shape
    fa = np.asarray(face)
    grad = np.zeros((fh, fw, 4), np.uint8)
    ys = np.linspace(0.0, 1.0, fh)
    ramp = np.clip((ys - 0.42) / 0.58, 0.0, 1.0)
    cols = np.zeros((fh, 3), np.float32)
    for c in range(3):
        cols[:, c] = face_top[c] + (face_bot[c] - face_top[c]) * ramp
    on = fa > 128
    grad[:, :, 0] = cols[:, 0][:, None]
    grad[:, :, 1] = cols[:, 1][:, None]
    grad[:, :, 2] = cols[:, 2][:, None]
    grad[:, :, 3] = np.where(on, 255, 0).astype(np.uint8)
    # Upper-left rim highlight, the part a comic inker leaves white.
    up = np.zeros_like(fa)
    up[:-5, :-4] = fa[5:, 4:]
    rim = on & (up < 128) & (np.arange(fh)[:, None] < fh * 0.62)
    grad[rim, 0] = highlight[0]
    grad[rim, 1] = highlight[1]
    grad[rim, 2] = highlight[2]
    canvas.alpha_composite(Image.fromarray(grad, "RGBA"))
    bb = canvas.getbbox()
    if bb:
        canvas = canvas.crop(bb)
    return canvas


def clamp_rot(deg):
    if deg > 6.0:
        return 6.0
    if deg < -6.0:
        return -6.0
    return deg


def compose_word(ss, spec):
    size = CELL * ss
    outline = 22 * ss
    inner_outline = 14 * ss
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    outer, outer_key, inner, inner_key = burst_masks(
        size, spec["outer"], spec["inner"], spec["ax"], spec["ay"],
        outline, inner_outline, spec["inner_scale"], spec["ox"] * ss, spec["oy"] * ss)
    if spec.get("speed"):
        paint_speed_lines(canvas, outer_key)
    paint_burst(
        canvas, outer, outer_key, inner, inner_key,
        spec["fill"] + (255,), spec["inner_c"] + (255,),
        22 * ss, 8 * ss, spec["dot_angle"], spec.get("fringe"))
    font = ImageFont.truetype(FONT_PATH, int(spec["font"] * ss))
    text = spec["text"]
    scales = spec["scales"]
    rots = spec["rots"]
    n = len(text)
    styled = []
    for i, ch in enumerate(text):
        m = shear_mask(glyph_mask(font, ch, scales[i]), 0.22)
        m = rotate_mask(m, clamp_rot(rots[i]))
        stroke = max(8, int(m.height * 0.075))
        depth = max(10, int(m.height * 0.11))
        styled.append(style_letter(m, spec["top"], spec["bot"], spec["side"] + (255,), spec["hi"] + (255,), stroke, depth))
    # The same overlap on every pair, bang included, so the word reads as one hit.
    avg_w = sum(im.width for im in styled) / float(n)
    gap = int(round(-0.13 * avg_w))
    widths = [im.width for im in styled]
    heights = [im.height for im in styled]
    total = sum(widths) + gap * (n - 1)
    burst_box = outer_key.getbbox()
    burst_w = (burst_box[2] - burst_box[0]) if burst_box else int(size * 0.8)
    target = min(int(burst_w * 0.82), int(size * 0.80))
    if total > 0 and abs(total - target) > 2:
        s = target / float(total)
        styled = [im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.Resampling.LANCZOS) for im in styled]
        widths = [im.width for im in styled]
        heights = [im.height for im in styled]
        gap = int(round(gap * s))
        total = sum(widths) + gap * (n - 1)
    arch = max(8, int(total * 0.055))
    placed = layout_positions(size, styled, widths, heights, gap, arch)
    for _ in range(5):
        if not off_canvas(size, placed, 14):
            break
        s = 0.94
        styled = [im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.Resampling.LANCZOS) for im in styled]
        widths = [im.width for im in styled]
        heights = [im.height for im in styled]
        gap = int(round(gap * s))
        arch = int(arch * s)
        placed = layout_positions(size, styled, widths, heights, gap, arch)
    for im, x, y in placed:
        canvas.alpha_composite(im, (x, y))
    if ss != 1:
        canvas = canvas.resize((CELL, CELL), Image.Resampling.LANCZOS)
    return canvas


def layout_positions(size, styled, widths, heights, gap, arch):
    n = len(styled)
    total = sum(widths) + gap * (n - 1)
    x = (size - total) // 2
    # One baseline for every letter, including the bang. The arc lifts the middle.
    tall = max(heights) if heights else 0
    baseline = int(size * 0.56 + tall * 0.08)
    positions = []
    cursor = x
    for i, im in enumerate(styled):
        t = 0.0 if n == 1 else i / (n - 1)
        lift = int(arch * (1.0 - (2.0 * t - 1.0) ** 2))
        y = baseline - im.height - lift
        positions.append((im, cursor, y))
        cursor += im.width + gap
    return positions


def off_canvas(size, placed, margin):
    for im, x, y in placed:
        a = np.asarray(im.split()[-1])
        ys, xs = np.where(a > 32)
        if len(xs) == 0:
            continue
        if x + int(xs.min()) < margin or y + int(ys.min()) < margin:
            return True
        if x + int(xs.max()) >= size - margin or y + int(ys.max()) >= size - margin:
            return True
    return False


def sticks_out(body, placed):
    h, w = body.shape
    outside = 0
    total = 0
    for im, x, y in placed:
        a = np.asarray(im.split()[-1])
        ih, iw = a.shape
        for yy in range(0, ih, 3):
            for xx in range(0, iw, 3):
                if a[yy, xx] < 160:
                    continue
                total += 1
                px, py = x + xx, y + yy
                if px < 0 or py < 0 or px >= w or py >= h or body[py, px] < 128:
                    outside += 1
    if total == 0:
        return False
    return outside / float(total) > 0.015


def build_cells(only=None):
    cells = []
    for spec in WORDS:
        if only and spec["text"] not in only:
            continue
        cells.append(compose_word(SS, spec))
    return cells


def sheet(cells):
    atlas = Image.new("RGBA", (CELL * len(cells), CELL), (0, 0, 0, 0))
    for i, im in enumerate(cells):
        atlas.paste(im, (i * CELL, 0))
    return atlas


def old_word(draw, text, origin_x, origin_y, pixel):
    step = 6 * pixel
    x = origin_x
    for ch in text:
        rows = OLD_GLYPHS[ch]
        for row_i, bits in enumerate(rows):
            for col in range(5):
                if (bits & (1 << (4 - col))) == 0:
                    continue
                rx = x + col * pixel
                ry = origin_y + row_i * pixel
                draw.rectangle((rx, ry, rx + pixel - 1, ry + pixel - 1), fill=(255, 255, 255, 255))
        x += step


def park_background(w, h):
    img = Image.new("RGBA", (w, h), (0, 0, 0, 255))
    px = img.load()
    horizon = int(h * 0.46)
    for y in range(h):
        t = y / float(horizon) if y < horizon else (y - horizon) / float(h - horizon)
        if y < horizon:
            r = int(118 + 78 * t)
            g = int(186 + 28 * t)
            b = int(232 - 28 * t)
        else:
            r = int(74 + 18 * math.sin(t * 3.0))
            g = int(132 - 24 * t)
            b = int(52 + 8 * t)
        for x in range(0, w, 2):
            shade = 0
            if y >= horizon and ((x * 3 + y * 5) % 47) < 3:
                shade = -18
            px[x, y] = (max(0, r + shade), max(0, g + shade), max(0, b + shade), 255)
            if x + 1 < w:
                px[x + 1, y] = px[x, y]
    d = ImageDraw.Draw(img)
    d.polygon([(int(w * 0.42), horizon), (int(w * 0.60), horizon), (int(w * 0.86), h), (int(w * 0.16), h)], fill=(196, 170, 116, 255))
    d.rectangle((int(w * 0.06), int(h * 0.52), int(w * 0.20), int(h * 0.74)), fill=(176, 178, 172, 255))
    d.rectangle((int(w * 0.06), int(h * 0.52), int(w * 0.20), int(h * 0.56)), fill=(150, 152, 148, 255))
    d.rectangle((int(w * 0.78), int(h * 0.62), int(w * 0.94), int(h * 0.67)), fill=(122, 78, 40, 255))
    d.rectangle((int(w * 0.78), int(h * 0.60), int(w * 0.94), int(h * 0.62)), fill=(90, 58, 30, 255))
    for cx, cy, rad in ((int(w * 0.14), int(h * 0.40), 54), (int(w * 0.28), int(h * 0.38), 36), (int(w * 0.86), int(h * 0.36), 48), (int(w * 0.70), int(h * 0.40), 28)):
        d.ellipse((cx - rad, cy - int(rad * 0.85), cx + rad, cy + int(rad * 0.55)), fill=(42, 112, 48, 255))
        d.rectangle((cx - 5, cy + 4, cx + 5, cy + rad), fill=(96, 66, 36, 255))
    return img


def place(dst, word, cx, cy, width, tilt):
    scale = width / float(word.width)
    im = word.resize((max(1, int(word.width * scale)), max(1, int(word.height * scale))), Image.Resampling.LANCZOS)
    im = im.rotate(tilt, expand=True, resample=Image.Resampling.BICUBIC)
    dst.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))


def write_stills(cells):
    os.makedirs(STILL_DIR, exist_ok=True)
    labels = ["POP!", "POW!", "BAM!", "WHAM!"]
    colors = [(255, 214, 30, 255), (255, 120, 20, 255), (230, 30, 46, 255), (150, 46, 230, 255)]
    before = Image.new("RGBA", (CELL * 2, CELL * 2), (16, 14, 24, 255))
    bd = ImageDraw.Draw(before)
    for i, text in enumerate(labels):
        ox = (i % 2) * CELL + CELL // 2
        oy = (i // 2) * CELL + CELL // 2
        c = colors[i]
        bd.ellipse((ox - 210, oy - 210, ox + 210, oy + 210), fill=c)
        for k in range(14):
            a = k / 14.0 * math.tau
            x = ox + math.cos(a) * 300
            y = oy + math.sin(a) * 230
            bd.ellipse((x - 34, y - 34, x + 34, y + 34), fill=c)
        pixel = 22
        word_w = len(text) * 6 * pixel - pixel
        old_word(bd, text, ox - word_w // 2, oy - int(3.5 * pixel), pixel)
    before.save(os.path.join(STILL_DIR, "comic-before.png"))

    after = Image.new("RGBA", (CELL * 2, CELL * 2), (18, 16, 28, 255))
    for i, im in enumerate(cells):
        after.paste(im, ((i % 2) * CELL, (i // 2) * CELL), im)
    after.save(os.path.join(STILL_DIR, "comic-after.png"))

    park = park_background(1920, 1080)
    spots = [(430, 690, 500, -8), (1040, 390, 540, 7), (860, 760, 470, -4), (1500, 600, 520, 9)]
    for im, (x, y, width, tilt) in zip(cells, spots):
        place(park, im, x, y, width, tilt)
    park.convert("RGB").save(os.path.join(STILL_DIR, "comic-park.png"), "PNG")


def write_cs(png_bytes):
    b64 = base64.b64encode(png_bytes).decode("ascii")
    lines = [
        "namespace Tag.FX",
        "{",
        "    /// <summary>",
        "    /// Bangers comic words, one 1024 cell each. Built by Tools/BuildComicAtlas.py.",
        "    /// </summary>",
        "    public static class ComicAtlas",
        "    {",
        "        public const int Cells = 4;",
        "        public const int CellWidth = %d;" % CELL,
        "        public const int CellHeight = %d;" % CELL,
        "",
        "        public static byte[] Png()",
        "        {",
        "            return System.Convert.FromBase64String(Data);",
        "        }",
        "",
        "        const string Data =",
    ]
    for i in range(0, len(b64), 120):
        lines.append('            "' + b64[i:i + 120] + '" +')
    lines[-1] = lines[-1][:-2] + ";"
    lines += ["    }", "}", ""]
    with open(CS_PATH, "w") as f:
        f.write("\n".join(lines))


# Angles are degrees clockwise from up. Radii are the fill, before the black outline.
# Side radii stay fat so the word sits in ink. Tips and the top and bottom cuts make the explosion.
POP_OUTER = [
    (-4, 0.98), (14, 0.64), (30, 0.86), (48, 0.58),
    (66, 0.90), (84, 0.72), (100, 1.00), (118, 0.68),
    (136, 0.84), (156, 0.60), (174, 0.78), (194, 0.96),
    (214, 0.58), (232, 0.86), (252, 0.70), (270, 1.02),
    (288, 0.66), (308, 0.84), (328, 0.56), (348, 0.90),
]
POP_INNER = [
    (8, 0.90), (46, 0.52), (88, 1.00), (132, 0.48),
    (176, 0.86), (220, 0.50), (268, 0.98), (318, 0.46), (356, 0.80),
]
POW_OUTER = [
    (-8, 0.92), (12, 0.50), (28, 0.74), (46, 0.40),
    (64, 0.88), (82, 0.56), (98, 1.12), (116, 0.52),
    (134, 0.78), (154, 0.42), (172, 0.70), (192, 0.90),
    (212, 0.46), (230, 0.76), (250, 0.48), (268, 1.10),
    (286, 0.54), (306, 0.80), (326, 0.42), (346, 0.86),
]
POW_INNER = [
    (0, 1.00), (40, 0.46), (78, 0.88), (120, 0.42),
    (162, 0.96), (206, 0.48), (250, 0.90), (294, 0.44), (338, 0.84),
]
BAM_OUTER = [
    (-2, 1.08), (16, 0.52), (34, 0.80), (54, 0.44),
    (74, 0.92), (94, 0.66), (112, 0.84), (132, 0.48),
    (152, 0.74), (172, 0.56), (190, 0.96), (210, 0.50),
    (230, 0.78), (250, 0.46), (270, 0.90), (290, 0.62),
    (310, 0.82), (332, 0.44), (352, 0.94),
]
BAM_INNER = [
    (12, 0.92), (58, 0.48), (104, 1.02), (156, 0.44),
    (206, 0.88), (258, 0.50), (310, 0.96), (356, 0.46),
]
WHAM_OUTER = [
    (-18, 0.78), (0, 1.06), (16, 0.46), (32, 0.82),
    (48, 0.40), (66, 0.96), (84, 0.62), (100, 0.74),
    (118, 0.42), (136, 1.10), (156, 0.50), (174, 0.70),
    (194, 0.44), (212, 0.92), (232, 0.58), (252, 0.46),
    (270, 1.04), (290, 0.52), (310, 0.80), (330, 0.40),
    (348, 0.88),
]
WHAM_INNER = [
    (-8, 0.96), (36, 0.44), (80, 1.04), (128, 0.40),
    (176, 0.90), (224, 0.48), (272, 1.00), (320, 0.42), (356, 0.82),
]

WORDS = [
    dict(text="POP!", outer=POP_OUTER, inner=POP_INNER, ax=1.02, ay=0.96,
         inner_scale=0.46, ox=18, oy=-16, dot_angle=18,
         fill=(255, 208, 0), inner_c=(24, 92, 255), dots=(196, 12, 36),
         top=(255, 72, 48), bot=(150, 8, 24), side=(92, 8, 16), hi=(255, 228, 214),
         scales=(1.06, 0.98, 1.04, 0.92), rots=(-4, 3, -2, 5), font=230),
    dict(text="POW!", outer=POW_OUTER, inner=POW_INNER, ax=1.16, ay=0.84,
         inner_scale=0.46, ox=-22, oy=10, dot_angle=72,
         fill=(255, 150, 0), inner_c=(186, 0, 32), dots=(104, 0, 110),
         top=(255, 252, 244), bot=(255, 214, 120), side=(110, 16, 0), hi=(255, 255, 255),
         scales=(1.06, 0.97, 1.03, 0.92), rots=(-5, 4, -3, 6), font=236),
    dict(text="BAM!", outer=BAM_OUTER, inner=BAM_INNER, ax=0.90, ay=1.10,
         inner_scale=0.46, ox=12, oy=-28, dot_angle=18, speed=True,
         fill=(214, 12, 36), inner_c=(255, 196, 0), dots=(255, 214, 48),
         top=(255, 250, 236), bot=(255, 196, 140), side=(110, 18, 8), hi=(255, 255, 255),
         scales=(1.05, 0.98, 1.06, 0.92), rots=(-4, 2, -5, 5), font=232),
    dict(text="WHAM!", outer=WHAM_OUTER, inner=WHAM_INNER, ax=1.08, ay=0.94,
         inner_scale=0.46, ox=24, oy=14, dot_angle=75, speed=True,
         fill=(26, 64, 240), inner_c=(255, 36, 140), dots=(6, 16, 48),
         top=(255, 236, 80), bot=(255, 150, 0), side=(42, 0, 96), hi=(255, 255, 230),
         scales=(1.04, 1.00, 0.97, 1.05, 0.90), rots=(-5, 3, -2, 4, 6), font=200,
         fringe=((0, 220, 255, 150), (255, 0, 140, 130))),
]


def report(cells):
    for spec, im in zip(WORDS, cells):
        a = np.asarray(im.split()[-1])
        ys = np.where(a.max(axis=1) > 8)[0]
        xs = np.where(a.max(axis=0) > 8)[0]
        print(spec["text"], "bbox", int(xs[0]), int(ys[0]), int(xs[-1]), int(ys[-1]),
              "edge", int(a[0].max()), int(a[-1].max()), int(a[:, 0].max()), int(a[:, -1].max()))


def main():
    only = sys.argv[1:]
    cells = build_cells(only if only else None)
    if only:
        for spec, im in zip([s for s in WORDS if s["text"] in only], cells):
            path = "/tmp/comic-%s.png" % spec["text"].strip("!").lower()
            bg = Image.new("RGBA", im.size, (32, 24, 48, 255))
            bg.alpha_composite(im)
            bg.save(path)
            print("wrote", path)
        return
    atlas = sheet(cells)
    os.makedirs(os.path.dirname(PNG_PATH), exist_ok=True)
    atlas.save(PNG_PATH, "PNG", optimize=True)
    raw = open(PNG_PATH, "rb").read()
    write_cs(raw)
    write_stills(cells)
    bg = Image.new("RGBA", (CELL, CELL), (32, 24, 48, 255))
    bg.alpha_composite(cells[1])
    bg.save("/tmp/comic-pow.png")
    report(cells)
    print("png", len(raw), "cells", len(cells))


if __name__ == "__main__":
    main()
