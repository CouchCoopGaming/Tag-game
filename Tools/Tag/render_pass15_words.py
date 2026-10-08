"""Pass 15 comic sheets. Each word keeps its colour and gets its own tilt.

The tilt, skew, size, and arc match ComicWords.StyleOf. Contact stays bold.
Whiff is thin and streaky, land is low and wide, crash is split.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(__file__))
import bake_comic_layers as bake
import render_comic_sheet as comic_sheet

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Docs", "FxStills", "pass19")
FONT = comic_sheet.FONT_PATH


def category_of(i):
    if i in (6, 27, 28, 29):
        return "crash"
    if i in (15, 16, 17, 33, 34):
        return "land"
    if i in (12, 13, 14, 35) or 18 <= i <= 26:
        return "whiff"
    return "contact"


def style_of(i):
    mag = 8 + ((i * 17) % 18)
    sign = 1 if (i & 1) == 0 else -1
    tilt = sign * mag
    skew = ((i % 5) - 2) * 0.09
    size = 0.88 + (i % 7) * 0.04
    arc = ((i % 3) - 1) * 0.12
    cat = category_of(i)
    if cat == "whiff":
        wide, tall = 1.14, 0.82
        skew += 0.10 * sign
    elif cat == "land":
        wide, tall = 1.28, 0.74
    elif cat == "crash":
        wide, tall = 1.06, 1.08
        skew += 0.06 * -sign
    else:
        wide, tall = 1.0, 1.0
    return tilt, skew, size, arc, wide, tall


def shape(glyph, tilt, skew, size, arc, wide, tall):
    w, h = glyph.size
    skewed = glyph.transform(
        (w + int(abs(skew) * h) + 8, h),
        Image.Transform.AFFINE,
        (1, -skew, 0 if skew > 0 else skew * h, 0, 1, 0),
        resample=Image.Resampling.BICUBIC,
    )
    # Quadratic bow, sampled from the destination so the arc does not open holes.
    sw, sh = skewed.size
    bowed = Image.new("RGBA", (sw, sh + 48), (0, 0, 0, 0))
    src = skewed.load()
    dst = bowed.load()
    for y in range(bowed.height):
        for x in range(sw):
            nx = x / float(sw) - 0.5
            dy = nx * nx * arc * sh * 2.2
            sy = y - 24 + dy
            iy = int(round(sy))
            if 0 <= iy < sh:
                p = src[x, iy]
                if p[3] >= 8:
                    dst[x, y] = p
    nw = max(1, int(bowed.width * size * wide))
    nh = max(1, int(bowed.height * size * tall))
    bowed = bowed.resize((nw, nh), Image.Resampling.LANCZOS)
    if abs(tilt) > 0.05:
        bowed = bowed.rotate(tilt, expand=True, resample=Image.Resampling.BICUBIC)
    return bowed


# Atlas cell to the burst event. Same pools as ComicWords.
ATLAS_EVENT = {}
for _ev, _ids in (
    (0, (0, 1, 4, 5, 2)),
    (1, (3, 7, 8)),
    (2, (9, 10, 11)),
    (3, (12, 13, 14, 35)),
    (4, (15, 16, 17, 33, 34)),
    (5, (18, 19, 20)),
    (6, (21, 22, 23)),
    (7, (24, 25, 26)),
    (8, (27, 28, 29, 6)),
    (9, (30, 31, 32)),
):
    for _id in _ids:
        ATLAS_EVENT[_id] = _ev


def cell_of(index, box):
    text, kind, fill = bake.WORDS[index]
    word = bake.fit_word(text, fill, 280, kind)
    ev = ATLAS_EVENT.get(index, 0)
    color = comic_sheet.EVENTS[ev][1]
    burst = comic_sheet.burst(bake.CELL, bake.CELL, color, bake.star_radius())
    comic_sheet.knockout_dots(burst, word, color, pad=11)
    # Burst behind, word in front, then one tilt so they stay a pair.
    pair = Image.new("RGBA", (bake.CELL, bake.CELL), (0, 0, 0, 0))
    pair.alpha_composite(burst)
    pair.alpha_composite(word)
    bb = pair.getchannel("A").getbbox()
    if bb:
        pair = pair.crop(bb)
    tilt, skew, size, arc, wide, tall = style_of(index)
    posed = shape(pair, tilt, skew, size, arc, wide, tall)
    bg = Image.new("RGB", box, (32, 30, 28))
    scale = min((box[0] - 24) / float(posed.width), (box[1] - 36) / float(posed.height))
    if scale < 1:
        posed = posed.resize((max(1, int(posed.width * scale)), max(1, int(posed.height * scale))), Image.Resampling.LANCZOS)
    x = (box[0] - posed.width) // 2
    y = (box[1] - posed.height) // 2
    bg.paste(posed, (x, y), posed)
    return bg, text, tilt


def save_jpeg(im, name):
    path = os.path.join(OUT, name)
    os.makedirs(OUT, exist_ok=True)
    rgb = im.convert("RGB")
    for quality in (85, 75, 65, 55):
        rgb.save(path, format="JPEG", quality=quality, optimize=True)
        if os.path.getsize(path) <= 400 * 1024:
            print("SIZE", name, os.path.getsize(path), "q", quality, rgb.size)
            return
    print("SIZE", name, os.path.getsize(path), "over")


def sheet(title, indices, cols):
    font = ImageFont.truetype(FONT, 22)
    small = ImageFont.truetype(FONT, 16)
    box = (360, 280)
    gap = 8
    head = 40
    foot = 28
    rows = (len(indices) + cols - 1) // cols
    w = cols * box[0] + gap * (cols + 1)
    h = head + rows * (box[1] + foot) + gap * rows
    board = Image.new("RGB", (w, h), (22, 20, 18))
    draw = ImageDraw.Draw(board)
    draw.text((12, 8), title, font=font, fill=(255, 220, 120))
    for n, index in enumerate(indices):
        panel, text, tilt = cell_of(index, box)
        col = n % cols
        row = n // cols
        x = gap + col * (box[0] + gap)
        y = head + row * (box[1] + foot + gap)
        board.paste(panel, (x, y))
        draw.text((x + 8, y + box[1] + 4), "%s   %d deg" % (text, tilt), font=small, fill=(255, 244, 220))
    return board


def life_scale(age):
    """Matches ComicWords.Scale. Overshoot by 0.05 s, then shrink to nothing by 0.45 s."""
    pop = 0.05
    life = 0.45
    if age <= 0.0 or age >= life:
        return 0.0
    if age < pop:
        u = age / pop
        peak_at = 0.58
        peak = 1.25
        if u < peak_at:
            t = u / peak_at
            e = t * t * (3.0 - 2.0 * t)
            return peak * e
        settle = (u - peak_at) / (1.0 - peak_at)
        down = settle * settle * (3.0 - 2.0 * settle)
        return peak + (1.0 - peak) * down
    v = (age - pop) / (life - pop)
    ease = v * v * (3.0 - 2.0 * v)
    return 1.0 - ease


def word_punch(age):
    """Word and burst share Scale. The word overshoots a little more, then matches."""
    pop = 0.05
    peak_at = pop * 0.58
    extra = 0.18
    if age <= 0.0 or age >= pop:
        return 1.0
    if age < peak_at:
        t = age / peak_at
        e = t * t * (3.0 - 2.0 * t)
        return 1.0 + extra * e
    settle = (age - peak_at) / (pop - peak_at)
    down = settle * settle * (3.0 - 2.0 * settle)
    return 1.0 + extra * (1.0 - down)


def word_scale(age):
    start = 0.05
    span = 0.08
    if age <= start:
        return 0.0
    u = (age - start) / span
    if u >= 1.0:
        return 1.0
    peak_at = 0.55
    peak = 1.15
    if u < peak_at:
        t = u / peak_at
        e = t * t * (3.0 - 2.0 * t)
        return peak * e
    settle = (u - peak_at) / (1.0 - peak_at)
    down = settle * settle * (3.0 - 2.0 * settle)
    return peak + (1.0 - peak) * down


def life_alpha(age):
    pop = 0.05
    hold = 0.28
    life = 0.45
    if age < 0.0 or age >= life:
        return 0.0
    if age < pop:
        return age / pop
    if age < pop + hold:
        return 1.0
    span = life - pop - hold
    u = (age - pop - hold) / span
    if u < 0.0:
        u = 0.0
    if u > 1.0:
        u = 1.0
    return 1.0 - u


def posed_layers(index):
    text, kind, fill = bake.WORDS[index]
    word = bake.fit_word(text, fill, 280, kind)
    ev = ATLAS_EVENT.get(index, 0)
    color = comic_sheet.EVENTS[ev][1]
    burst = comic_sheet.burst(bake.CELL, bake.CELL, color, bake.star_radius())
    comic_sheet.knockout_dots(burst, word, color, pad=11)
    tilt, skew, size, arc, wide, tall = style_of(index)
    burst = shape(burst, tilt, skew, size, arc, wide, tall)
    word = shape(word, tilt, skew, size, arc, wide, tall)
    return text, burst, word


def frame_at(burst, word, age, box):
    """One moment. Burst and word share the tilt. The word is in front."""
    bg = Image.new("RGBA", box, (32, 30, 28, 255))
    bs = life_scale(age)
    ws = bs * word_punch(age)
    a = life_alpha(age)
    if bs < 0.02 and ws < 0.02 or a <= 0.001:
        return bg.convert("RGB")
    canvas = Image.new("RGBA", box, (0, 0, 0, 0))
    limit = min(box[0] - 16, box[1] - 16)

    def place(glyph, sc):
        if sc < 0.02:
            return
        nw = max(1, int(glyph.width * sc))
        nh = max(1, int(glyph.height * sc))
        fitted = min(limit / float(nw), limit / float(nh), 1.0)
        nw = max(1, int(nw * fitted))
        nh = max(1, int(nh * fitted))
        layer = glyph.resize((nw, nh), Image.Resampling.LANCZOS)
        if a < 0.999:
            band = layer.getchannel("A").point(lambda p: int(p * a))
            layer.putalpha(band)
        canvas.alpha_composite(layer, ((box[0] - nw) // 2, (box[1] - nh) // 2))

    # Fit the full-size pair into the panel, then apply the life scale on top.
    full = max(burst.width, word.width, 1)
    base = limit / float(full)
    place(burst, bs * base)
    place(word, ws * base)
    bg.alpha_composite(canvas)
    return bg.convert("RGB")


def life_strip():
    index = index_of("POW!")
    text, burst, word = posed_layers(index)
    ages = (0.00, 0.03, 0.09, 0.18, 0.33, 0.45)
    labels = ("0.00 born", "0.03 past full", "0.09 together", "0.18 shrinking", "0.33 smaller", "0.45 gone")
    font = ImageFont.truetype(FONT, 22)
    small = ImageFont.truetype(FONT, 16)
    box = (220, 220)
    gap = 8
    head = 40
    foot = 28
    w = len(ages) * box[0] + gap * (len(ages) + 1)
    h = head + box[1] + foot + gap
    board = Image.new("RGB", (w, h), (22, 20, 18))
    draw = ImageDraw.Draw(board)
    draw.text((12, 8), "POW!    word and burst born together    then gone by 0.45 s", font=font, fill=(255, 220, 120))
    for n, (age, label) in enumerate(zip(ages, labels)):
        panel = frame_at(burst, word, age, box)
        x = gap + n * (box[0] + gap)
        y = head
        board.paste(panel, (x, y))
        draw.text((x + 6, y + box[1] + 4), label, font=small, fill=(255, 244, 220))
    return board


def rel_lum(rgb):
    def lin(c):
        c = c / 255.0
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    r, g, b = (lin(c) for c in rgb)
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def wcag(word_fill, burst_fill):
    hi = max(rel_lum(word_fill), rel_lum(burst_fill))
    lo = min(rel_lum(word_fill), rel_lum(burst_fill))
    return (hi + 0.05) / (lo + 0.05)


def stroke_colors(fill):
    """Outermost stroke, then the stroke that touches the fill."""
    black = (0, 0, 0)
    return black, black


def contrast_report():
    """Outermost stroke against the burst, and the fill against its stroke."""
    stroke_worst = None
    stroke_name = ""
    fill_worst = None
    fill_name = ""
    failed = 0
    for i, (text, _kind, fill) in enumerate(bake.WORDS):
        burst = comic_sheet.EVENTS[ATLAS_EVENT[i]][1]
        outer, inner = stroke_colors(fill)
        stroke_score = wcag(outer, burst)
        fill_score = wcag(fill, inner)
        if stroke_worst is None or stroke_score < stroke_worst:
            stroke_worst = stroke_score
            stroke_name = text
        if fill_worst is None or fill_score < fill_worst:
            fill_worst = fill_score
            fill_name = text
        if stroke_score < 3.0 or fill_score < 3.0:
            failed += 1
            print("CONTRAST FAIL", text, "stroke-burst %.2f" % stroke_score, "fill-stroke %.2f" % fill_score)
    print("stroke-burst min=%.2f worst=%s" % (stroke_worst, stroke_name))
    print("fill-stroke min=%.2f worst=%s" % (fill_worst, fill_name))
    return failed


def live_pair(index, age, width):
    """Burst behind the word, both at the shared life scale, game tilt already applied."""
    _text, burst, word = posed_layers(index)
    bs = life_scale(age)
    ws = bs * word_punch(age)
    alpha = life_alpha(age)
    full = max(burst.width, word.width, 1)
    base = width / float(full)

    def layer(glyph, scale):
        nw = max(1, int(glyph.width * scale * base))
        nh = max(1, int(glyph.height * scale * base))
        im = glyph.resize((nw, nh), Image.Resampling.LANCZOS)
        if alpha < 0.999:
            im.putalpha(im.getchannel("A").point(lambda p: int(p * alpha)))
        return im

    back = layer(burst, bs)
    front = layer(word, ws)
    w = max(back.width, front.width)
    h = max(back.height, front.height)
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    canvas.alpha_composite(back, ((w - back.width) // 2, (h - back.height) // 2))
    canvas.alpha_composite(front, ((w - front.width) // 2, (h - front.height) // 2))
    return canvas


def couch_four():
    """1280x720. Chase-cam plates, words at the measured quad size in front of the runner."""
    names = ("SPROING!", "WHIZZ!", "POW!", "SMACK!")
    plates = ("sproing", "whizz", "pow", "smack")
    # 1.65 m quad at the contact, from the pass 19 chase render.
    base_px = 55.9
    anchor = (323.7, 165.6)
    board = Image.new("RGB", (1280, 720), (12, 12, 12))
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype(FONT, 22)
    pw, ph = 640, 360
    age = 0.12
    life = life_scale(age)
    plate_dir = os.path.join(ROOT, "Docs", "FxStills", "pass19", "plates")
    for n, text in enumerate(names):
        index = index_of(text)
        scene = Image.open(os.path.join(plate_dir, plates[n] + ".png")).convert("RGB").resize((pw, ph), Image.Resampling.LANCZOS)
        _text, burst, word = posed_layers(index)
        _tilt, _skew, size, _arc, _wide, tall = style_of(index)
        px = base_px * life * size * tall
        meters = 1.65 * life * size * tall
        pre_h = (bake.CELL + 48) * size * tall
        scale = px / float(pre_h)

        def layer(glyph):
            nw = max(1, int(round(glyph.width * scale)))
            nh = max(1, int(round(glyph.height * scale)))
            return glyph.resize((nw, nh), Image.Resampling.LANCZOS)

        back = layer(burst)
        front = layer(word)
        w = max(back.width, front.width)
        h = max(back.height, front.height)
        pair = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        pair.alpha_composite(back, ((w - back.width) // 2, (h - back.height) // 2))
        pair.alpha_composite(front, ((w - front.width) // 2, (h - front.height) // 2))
        x = int(round(anchor[0] - pair.width / 2.0))
        y = int(round(anchor[1] - pair.height / 2.0))
        scene.paste(pair, (x, y), pair)
        ox = (n % 2) * pw
        oy = (n // 2) * ph
        board.paste(scene, (ox, oy))
        draw.text((ox + 16, oy + ph - 32), text, font=font, fill=(255, 244, 220))
        print("COUCH", text, "quad_m", round(meters, 3), "px", round(px, 1), "paste", pair.size)
    draw.line((640, 0, 640, 720), fill=(8, 8, 8), width=4)
    draw.line((0, 360, 1280, 360), fill=(8, 8, 8), width=4)
    return board


def index_of(word):
    for i, (text, _kind, _fill) in enumerate(bake.WORDS):
        if text == word:
            return i
    raise KeyError(word)


def main():
    if contrast_report():
        return 1
    groups = (
        ("comic-contact.jpg", "Contact   punch or tag lands", ("POW!", "BAM!", "WHAM!", "SMACK!")),
        ("comic-whiff.jpg", "Whiff   missed punch or tag", ("WHIFF!", "SWISH!", "WHOOSH!", "MISS!")),
        ("comic-land.jpg", "Hard land   fall at the roll threshold", ("THUD!", "WHUMP!", "BOOM!", "KRUNCH!")),
        ("comic-crash.jpg", "Crash   wall or object at sprint", ("SLAM!", "KRAK!", "THWACK!", "SPLAT!")),
    )
    for name, title, words in groups:
        save_jpeg(sheet(title, [index_of(w) for w in words], 2), name)
    angles = ("POW!", "BAM!", "WHAM!", "ZIP!", "SWISH!", "THWACK!", "KRAK!", "WHUMP!")
    save_jpeg(sheet("Angle variety   each word has its own tilt", [index_of(w) for w in angles], 4), "comic-angles.jpg")
    save_jpeg(life_strip(), "comic-life.jpg")
    save_jpeg(sheet("Every word at full size", list(range(len(bake.WORDS))), 6), "comic-all.jpg")
    save_jpeg(couch_four(), "comic-couch.jpg")
    prev = None
    for i, (text, _k, _f) in enumerate(bake.WORDS):
        tilt = style_of(i)[0]
        if prev is not None and tilt == prev:
            print("TILT CLASH", text, tilt)
            return 1
        if abs(tilt) < 8 or abs(tilt) > 25:
            print("TILT RANGE", text, tilt)
            return 1
        prev = tilt
    print("TILTS", len(bake.WORDS), "ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
