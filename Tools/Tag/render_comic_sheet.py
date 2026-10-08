"""Before/after comic words. Bangers (OFL).

The before word sits inside the star. The after word is 1.5x that ink, on its own
layer: it passes the spikes on the left and the right, with a hard shadow, a short
extrude, and a dark stroke about 9% of cap height.
"""
import math
import os
import sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FONT_PATH = os.path.join(ROOT, "Tools", "Tag", "fonts", "Bangers-Regular.ttf")
OUT_DIR = os.path.join(ROOT, "Docs", "FxStills", "pass12")

# Same pools as ComicWords. Punch runs from a light tap through a sprint hit.
EVENTS = (
    ("PUNCH", (255, 118, 28), (255, 236, 70),
     ("POP!", "POW!", "SMACK!", "WHACK!", "THWACK!", "BAM!")),
    ("TAG", (168, 52, 242), (255, 255, 255),
     ("WHAM!", "BONK!", "KAPOW!")),
    ("TRANSFER", (242, 64, 122), (255, 244, 210),
     ("TAG!", "GOTCHA!", "MINE!")),
    ("WHIFF", (92, 176, 242), (255, 255, 255),
     ("WHIFF!", "SWISH!", "WHOOSH!")),
    ("LAND", (168, 112, 64), (255, 236, 200),
     ("THUD!", "WHUMP!", "THUMP!")),
    ("LAUNCH", (242, 196, 36), (255, 255, 255),
     ("BOING!", "SPROING!", "POING!")),
    ("ZIP", (36, 196, 204), (255, 255, 255),
     ("ZING!", "ZIP!", "WHIZZ!")),
    ("GRAPPLE", (32, 140, 124), (236, 255, 244),
     ("THWIP!", "FWIP!", "ZWIP!")),
    ("WALL", (230, 72, 40), (255, 236, 210),
     ("KRAK!", "FWOOSH!", "THOK!")),
    ("STAGGER", (132, 150, 64), (255, 248, 220),
     ("OOF!", "UGH!", "OUCH!")),
)

OUTERS = (1.00, 0.88, 1.08, 0.92, 1.04, 0.86, 1.12, 0.90, 0.98, 1.06, 0.87, 1.02, 0.94, 1.09)
INNERS = (0.72, 0.64, 0.76, 0.66, 0.74, 0.68, 0.62, 0.78, 0.65, 0.73, 0.70, 0.63, 0.75, 0.67)
OUTLINE = 1.10
# Before word is the inside-the-star lettering. After is 1.5x that ink.
BEFORE_FRAC = 0.78
AFTER_TIMES = 1.50
STROKE_FRAC = 0.09


def font(size):
    return ImageFont.truetype(FONT_PATH, size)


def hash01(i, salt):
    x = math.sin(i * 12.9898 + salt * 78.233) * 43758.5453
    return x - math.floor(x)


def star_poly(cx, cy, radius, outline):
    scale = OUTLINE if outline else 1.0
    pts = []
    for i in range(14):
        ang = i * math.tau / 14.0 - math.pi / 2.0 + (0.04 if i % 2 == 0 else -0.03)
        outer = radius * OUTERS[i] * scale
        inner = radius * INNERS[i] * scale
        mid = ang + math.tau / 28.0
        pts.append((cx + math.cos(ang) * outer, cy + math.sin(ang) * outer))
        pts.append((cx + math.cos(mid) * inner, cy + math.sin(mid) * inner))
    return pts


def paint_star(draw, cx, cy, radius, color):
    draw.polygon(star_poly(cx, cy, radius, True), fill=(8, 8, 8, 255))
    draw.polygon(star_poly(cx, cy, radius, False), fill=color + (255,))


def halftone(im, cx, cy, radius, color):
    pix = im.load()
    dark = tuple(int(c * 0.40) for c in color)
    w, h = im.size
    cells = 16.0
    span = radius * 2.4
    for y in range(h):
        for x in range(w):
            r, g, b, a = pix[x, y]
            if a < 200 or abs(r - color[0]) > 18 or abs(g - color[1]) > 18:
                continue
            u = (x - (cx - span * 0.5)) / span
            if u < 0.0 or u > 1.0:
                continue
            fx = ((x - cx) / span) * cells
            fy = ((y - cy) / span) * cells
            dx = (fx - math.floor(fx)) - 0.5
            dy = (fy - math.floor(fy)) - 0.5
            rad = 0.30 * (1.0 - u) + 0.09 * u
            if dx * dx + dy * dy <= rad * rad:
                pix[x, y] = dark + (255,)


def text_size(draw, text, face, stroke):
    bb = draw.textbbox((0, 0), text, font=face, stroke_width=stroke)
    return bb[2] - bb[0], bb[3] - bb[1], bb


def cap_height(face):
    dummy = Image.new("L", (8, 8))
    bb = ImageDraw.Draw(dummy).textbbox((0, 0), "H", font=face)
    return max(1, bb[3] - bb[1])


def crop_alpha(im):
    bb = im.getchannel("A").getbbox()
    if bb is None:
        return im
    return im.crop(bb)


def near_fill(r, g, b, a, fill):
    if a < 210:
        return False
    return abs(r - fill[0]) <= 42 and abs(g - fill[1]) <= 42 and abs(b - fill[2]) <= 42


def fill_span(im, fill):
    pix = im.load()
    xs = []
    ys = []
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = pix[x, y]
            if near_fill(r, g, b, a, fill):
                xs.append(x)
                ys.append(y)
    if not xs:
        return None
    return min(xs), max(xs), min(ys), max(ys)


def scale_fill_width(im, fill, target_w):
    span = fill_span(im, fill)
    width = (span[1] - span[0] + 1) if span else im.width
    if width <= 0 or target_w <= 1:
        return im
    scale = target_w / float(width)
    nw = max(1, int(round(im.width * scale)))
    nh = max(1, int(round(im.height * scale)))
    if nw == im.width and nh == im.height:
        return im
    return im.resize((nw, nh), Image.Resampling.LANCZOS)


def render_before(word, target_w, fill):
    """One tilted word, heavier stroke, then scaled so the ink matches target_w."""
    size = 200
    face = font(size)
    stroke = max(2, int(round(size * 0.14)))
    dummy = Image.new("RGBA", (8, 8))
    draw = ImageDraw.Draw(dummy)
    tw, th, bb = text_size(draw, word, face, stroke)
    im = Image.new("RGBA", (tw + 8, th + 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.text((4 - bb[0], 4 - bb[1]), word, font=face, fill=fill + (255,),
           stroke_width=stroke, stroke_fill=(0, 0, 0, 255))
    im = im.rotate(hash01(len(word), 3) * 10 - 5, expand=True, resample=Image.Resampling.BICUBIC)
    im = crop_alpha(im)
    return scale_fill_width(im, fill, target_w)


def letter_jitter(i, word):
    return (
        (hash01(i, 11 + len(word)) - 0.5) * 0.10,
        (hash01(i, 19 + len(word)) - 0.5) * 0.16,
        (hash01(i, 23 + len(word)) - 0.5) * 7.0,
    )


def render_after(word, target_w, fill):
    """Per-letter jitter, 9% stroke, hard shadow, two-step extrude. Ink width is target_w."""
    size = 180
    face = font(size)
    cap = cap_height(face)
    stroke = max(2, int(round(cap * STROKE_FRAC)))
    dummy = Image.new("RGBA", (8, 8))
    draw = ImageDraw.Draw(dummy)
    extrude = max(3, int(round(cap * 0.07)))
    shadow = max(4, int(round(cap * 0.11)))
    pieces = []
    x = 0.0
    dark_fill = tuple(max(0, int(c * 0.45)) for c in fill)
    mid_fill = tuple(min(255, c + 28) for c in dark_fill)
    for i, ch in enumerate(word):
        tw, th, bb = text_size(draw, ch, face, stroke)
        pad = shadow + extrude + 8
        plate = Image.new("RGBA", (tw + pad * 2, th + pad * 2), (0, 0, 0, 0))
        d = ImageDraw.Draw(plate)
        ox = pad - bb[0]
        oy = pad - bb[1]
        d.text((ox + shadow, oy + shadow), ch, font=face, fill=(12, 10, 8, 235))
        d.text((ox + extrude, oy + extrude), ch, font=face, fill=dark_fill + (255,))
        d.text((ox + extrude // 2, oy + extrude // 2), ch, font=face, fill=mid_fill + (255,))
        d.text((ox, oy), ch, font=face, fill=fill + (255,),
               stroke_width=stroke, stroke_fill=(0, 0, 0, 255))
        jx, jy, rot = letter_jitter(i, word)
        plate = plate.rotate(rot, expand=True, resample=Image.Resampling.BICUBIC)
        pieces.append((plate, x + jx * cap, jy * cap))
        # 0.78 is about 13% tighter than the 0.90 advance, so the letters read as one word.
        x += tw * 0.78
    width = int(x + pieces[-1][0].width) + 4
    height = max(p.height for p, _, _ in pieces) + int(cap)
    sheet = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    base_y = int(cap * 0.40)
    for plate, px, py in pieces:
        sheet.alpha_composite(plate, (max(0, int(px)), max(0, int(base_y + py))))
    tilt = hash01(len(word), 4) * 12 - 6
    sheet = sheet.rotate(tilt, expand=True, resample=Image.Resampling.BICUBIC)
    sheet = crop_alpha(sheet)
    return scale_fill_width(sheet, fill, target_w)


def burst(w, h, color, radius):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    cx, cy = w // 2, h // 2
    paint_star(draw, cx, cy, radius, color)
    halftone(im, cx, cy, radius, color)
    return im


def opaque_bounds(im):
    pix = im.load()
    xs = []
    ys = []
    for y in range(im.height):
        for x in range(im.width):
            if pix[x, y][3] > 40:
                xs.append(x)
                ys.append(y)
    if not xs:
        return None
    return min(xs), max(xs), min(ys), max(ys)


def compose(word, color, fill, radius, scale, after):
    """Star, then the word in front. The canvas grows so overflow is not clipped."""
    spike = radius * max(OUTERS) * OUTLINE + 3
    side = int(math.ceil(spike * 2 + 8))
    star = burst(side, side, color, radius)
    sb = opaque_bounds(star)
    outer_w = (sb[1] - sb[0] + 1) if sb else side
    before_w = outer_w * BEFORE_FRAC
    target = before_w * AFTER_TIMES if after else before_w
    target *= scale
    if scale <= 0.02:
        return star, sb, None
    word_im = render_after(word, target, fill) if after else render_before(word, target, fill)
    cx, cy = side // 2, side // 2
    x = int(round(cx - word_im.width / 2.0))
    y = int(round(cy - word_im.height / 2.0))
    left = min(0, x - 2)
    top = min(0, y - 2)
    right = max(side, x + word_im.width + 2)
    bottom = max(side, y + word_im.height + 2)
    canvas = Image.new("RGBA", (right - left, bottom - top), (0, 0, 0, 0))
    canvas.alpha_composite(star, (-left, -top))
    canvas.alpha_composite(word_im, (x - left, y - top))
    star_box = None if sb is None else (sb[0] - left, sb[1] - left, sb[2] - top, sb[3] - top)
    return canvas, star_box, fill_span(canvas, fill)


def fit(im, box):
    rgb = Image.new("RGB", im.size, (28, 26, 24))
    rgb.paste(Image.new("RGB", im.size, (28, 26, 24)), mask=im.getchannel("A") if im.mode == "RGBA" else None)
    if im.mode == "RGBA":
        rgb = Image.alpha_composite(rgb.convert("RGBA"), im).convert("RGB")
    else:
        rgb = im.convert("RGB")
    rgb.thumbnail(box, Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", box, (28, 26, 24))
    canvas.paste(rgb, ((box[0] - rgb.width) // 2, (box[1] - rgb.height) // 2))
    return canvas


def save_limited(im, path, limit=400 * 1024):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.convert("RGB").save(path, optimize=True)
    if os.path.getsize(path) <= limit:
        print("SIZE", os.path.basename(path), os.path.getsize(path), im.size)
        return path
    rgb = im.convert("RGB")
    for colors in (192, 160, 128, 96):
        q = rgb.quantize(colors=colors, method=Image.Quantize.MEDIANCUT)
        q.save(path, optimize=True)
        if os.path.getsize(path) <= limit:
            print("SIZE", os.path.basename(path), os.path.getsize(path), "colors", colors, im.size)
            return path
    w, h = rgb.size
    while w > 900 and os.path.getsize(path) > limit:
        w = int(w * 0.92)
        h = int(h * 0.92)
        rgb = rgb.resize((w, h), Image.Resampling.LANCZOS)
        rgb.save(path, optimize=True)
    print("SIZE", os.path.basename(path), os.path.getsize(path), "scaled", rgb.size)
    return path


def sides(star_box, ink):
    if star_box is None or ink is None:
        return "MISSING"
    left_past = ink[0] < star_box[0] - 1
    right_past = ink[1] > star_box[1] + 1
    if left_past and right_past:
        return "PAST"
    if left_past or right_past:
        return "ONE"
    return "INSIDE"


def main():
    label = font(22)
    small = font(15)
    radius = 86
    cell = (214, 168)
    words = []
    for name, color, fill, pool in EVENTS:
        for word in pool:
            words.append((name, color, fill, word))
    cols = 6
    rows = (len(words) + cols - 1) // cols
    gap = 6
    cap_h = 18
    sheet_w = cols * cell[0] + (cols + 1) * gap
    row_h = cell[1] + cap_h
    band_h = rows * row_h + (rows + 1) * gap
    head = 214
    sheet_h = head + 26 + band_h + 34 + band_h + 16
    sheet = Image.new("RGB", (sheet_w, sheet_h), (22, 20, 18))
    draw = ImageDraw.Draw(sheet)
    draw.text((12, 8), "Comic words    before inside the star    after 1.5x in front", font=label, fill=(255, 220, 120))
    draw.text((12, 36), "Word waits until the burst is in, overshoots to 1.15, settles at 1. Life stays 0.45s.", font=small, fill=(230, 220, 200))

    pop_scales = (0.0, 0.55, 1.15, 1.0)
    pop_labels = ("burst in", "word 0.55", "overshoot 1.15", "settle 1.0")
    for i, (sc, title) in enumerate(zip(pop_scales, pop_labels)):
        frame, _, _ = compose("POW!", (255, 118, 28), (255, 236, 70), 78, sc, True)
        thumb = fit(frame, (168, 96))
        x = 12 + i * 176
        sheet.paste(thumb, (x, 62))
        draw.text((x, 162), title, font=small, fill=(255, 236, 200))

    measures = []

    def paste_band(y0, after, tag):
        draw.text((12, y0), tag, font=label, fill=(255, 228, 160))
        y0 += 26
        for i, (name, color, fill, word) in enumerate(words):
            col = i % cols
            row = i // cols
            x = gap + col * (cell[0] + gap)
            y = y0 + gap + row * (row_h + gap)
            frame, star_box, ink = compose(word, color, fill, radius, 1.0, after)
            sheet.paste(fit(frame, cell), (x, y))
            draw.text((x + 4, y + cell[1] + 1), "%s  %s" % (word, name), font=small, fill=(255, 244, 220))
            flag = sides(star_box, ink)
            ratio = 0.0
            if star_box and ink:
                ratio = (ink[1] - ink[0] + 1) / float(star_box[1] - star_box[0] + 1)
            measures.append((word, name, after, ratio, flag, ink, star_box))
        return y0 + gap + rows * (row_h + gap)

    y = paste_band(head, False, "BEFORE   inside the star")
    paste_band(y + 10, True, "AFTER   1.5x   shadow, extrude, 9% stroke, past the spikes")

    path = os.path.join(OUT_DIR, "comic-before-after.png")
    save_limited(sheet, path)

    # 4-up at half the linear size of a full burst (radius 150 -> 300px quadrant).
    quad_words = (
        ("THWACK!", (255, 118, 28), (255, 236, 70), "PUNCH"),
        ("KAPOW!", (168, 52, 242), (255, 255, 255), "TAG"),
        ("SPROING!", (242, 196, 36), (255, 255, 255), "LAUNCH"),
        ("GOTCHA!", (242, 64, 122), (255, 244, 210), "TRANSFER"),
    )
    full_r = 150
    frames = []
    caps = []
    for word, color, fill, name in quad_words:
        frame, star_box, ink = compose(word, color, fill, full_r, 1.0, True)
        frames.append((word, name, frame, star_box, ink, fill))
        if ink:
            caps.append((word, ink[3] - ink[2] + 1, frame.width))
    # Half linear: each quadrant is half the full-burst width.
    full_w = max(fr.width for _, _, fr, _, _, _ in frames)
    half = max(120, full_w // 2)
    board = Image.new("RGB", (half * 2, half * 2 + 28), (18, 16, 14))
    bd = ImageDraw.Draw(board)
    bd.text((8, 4), "Split-screen 4-up   half the full burst", font=small, fill=(255, 220, 120))
    for i, (word, name, frame, star_box, ink, fill) in enumerate(frames):
        thumb = fit(frame, (half, half))
        x = (i % 2) * half
        y = 28 + (i // 2) * half
        board.paste(thumb, (x, y))
        bd.rectangle((x, y, x + half - 1, y + half - 1), outline=(255, 210, 80))
        bd.text((x + 8, y + 6), "%s  %s" % (name, word), font=small, fill=(255, 244, 210))
        if ink and frame.width:
            shown = (ink[3] - ink[2] + 1) * (half / float(max(frame.width, frame.height)))
            print("FOUR", word, "cap_px", round(shown, 1), "quadrant", half)
    four = os.path.join(OUT_DIR, "comic-4up.png")
    save_limited(board, four)

    print("WORDS", len(words))
    print("STROKE", STROKE_FRAC, "of cap, shadow 0.11, extrude 0.07")
    failed = 0
    for word, name, after, ratio, flag, ink, star_box in measures:
        if not after:
            continue
        times = ratio / BEFORE_FRAC if BEFORE_FRAC else 0
        ok = flag == "PAST" and 1.35 <= times <= 1.70
        if not ok:
            failed += 1
        print("AFTER", word, name, "word/star", round(ratio, 3), "times_before", round(times, 3), flag)
    before_out = 0
    for word, name, after, ratio, flag, ink, star_box in measures:
        if after:
            continue
        if flag != "INSIDE":
            before_out += 1
            print("BEFORE", word, name, "word/star", round(ratio, 3), flag)
    print("BEFORE_INSIDE", len(words) - before_out, "/", len(words))
    after_n = sum(1 for row in measures if row[2])
    past = sum(1 for row in measures if row[2] and row[4] == "PAST")
    print("OVERFLOW", past, "/", after_n, "failed", failed)
    if failed or before_out or past != after_n:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
