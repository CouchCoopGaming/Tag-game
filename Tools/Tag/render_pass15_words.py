"""Pass 15 comic sheets. Each word keeps its colour and gets its own tilt.

The tilt, skew, size, and arc match ComicWords.StyleOf. Contact stays bold.
Whiff is thin and streaky, land is low and wide, crash is split.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(__file__))
import bake_comic_layers as bake
import render_comic_sheet as sheet

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Docs", "FxStills", "pass15")
FONT = sheet.FONT_PATH


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
    # Quadratic bow. p.y += p.x^2 * arc, with p in about -0.5..0.5.
    sw, sh = skewed.size
    bowed = Image.new("RGBA", (sw, sh + 48), (0, 0, 0, 0))
    src = skewed.load()
    dst = bowed.load()
    for y in range(sh):
        for x in range(sw):
            p = src[x, y]
            if p[3] < 8:
                continue
            nx = x / float(sw) - 0.5
            dy = int(nx * nx * arc * sh * 2.2)
            yy = y + 24 - dy
            if 0 <= yy < bowed.height:
                dst[x, yy] = p
    nw = max(1, int(bowed.width * size * wide))
    nh = max(1, int(bowed.height * size * tall))
    bowed = bowed.resize((nw, nh), Image.Resampling.LANCZOS)
    if abs(tilt) > 0.05:
        bowed = bowed.rotate(tilt, expand=True, resample=Image.Resampling.BICUBIC)
    return bowed


def cell_of(index, box):
    text, kind, fill = bake.WORDS[index]
    glyph = bake.fit_word(text, fill, 280, kind)
    # fit_word returns a 512 plate. Crop to the ink, then pose it.
    bb = glyph.getchannel("A").getbbox()
    if bb:
        glyph = glyph.crop(bb)
    tilt, skew, size, arc, wide, tall = style_of(index)
    posed = shape(glyph, tilt, skew, size, arc, wide, tall)
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


def index_of(word):
    for i, (text, _kind, _fill) in enumerate(bake.WORDS):
        if text == word:
            return i
    raise KeyError(word)


def main():
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
