#!/usr/bin/env python3
"""Secondary-screen stills.

The runners are ArenaStill portraits of the posed Hier bake
(Docs/UiStills/hier-idle-N.tris), the same raster worker 1 uses for
the character cards. The menu chrome is composited from MenuHost's
colors, copy, and placement. Unity Editor is not installed here, so
MenuScreenCapture cannot enter play mode.
"""

import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageEnhance

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Docs", "UiStills", "screens2", "pass15")
FIG = os.path.join(OUT, "figures")
W, H = 1920, 1080

INK = (10, 18, 41)
NAVY = (15, 41, 102)
PANEL = (20, 82, 219)
HOT = (51, 148, 255)
GOLD = (255, 214, 31)
CREAM = (255, 250, 235)
MUTE = (199, 224, 255)
STROKE = (5, 10, 26)

BODY = {
    "Red": (224, 56, 61),
    "Blue": (107, 173, 235),
    "Orange": (240, 107, 36),
    "Lavender": (179, 148, 224),
    "Tan": (230, 194, 133),
    "Mint": (107, 209, 179),
}
SEAT = [(242, 41, 56), (41, 115, 255), (255, 219, 31), (41, 209, 71)]

FONT_D = os.path.join(ROOT, "Assets", "UI", "Fonts", "Bangers-Regular.ttf")
FONT_B = os.path.join(ROOT, "Assets", "UI", "Fonts", "LiberationSans-Bold.ttf")
PARK = os.path.join(ROOT, "Assets", "Resources", "UI", "Menu", "MegaGold.png")
LOCK = os.path.join(ROOT, "Assets", "Resources", "UI", "Menu", "TagLockup.png")
HERO = {
    "Mega Park": os.path.join(ROOT, "Assets", "UI", "ArenaThumbs", "Mega.png"),
    "Pocket Park": os.path.join(ROOT, "Assets", "UI", "ArenaThumbs", "Pocket.png"),
    "Stack Yard": os.path.join(ROOT, "Assets", "UI", "ArenaThumbs", "Stack.png"),
}
def font(path, size):
    return ImageFont.truetype(path, size)


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def lum(c):
    def f(u):
        u = u / 255.0
        return u / 12.92 if u <= 0.04045 else ((u + 0.055) / 1.055) ** 2.4
    r, g, b = f(c[0]), f(c[1]), f(c[2])
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def contrast(a, b):
    la, lb = lum(a), lum(b)
    hi, lo = (la, lb) if la > lb else (lb, la)
    return (hi + 0.05) / (lo + 0.05)


def rounded(draw, box, r, fill, outline=None, width=0):
    draw.rounded_rectangle(box, r, fill=fill, outline=outline, width=width)


def shadow(base, box, r=18, dy=8):
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x0, y0, x1, y1 = box
    d.rounded_rectangle((x0, y0 + dy, x1, y1 + dy), r, fill=(0, 0, 0, 90))
    layer = layer.filter(ImageFilter.GaussianBlur(6))
    base.alpha_composite(layer)


def fit_text(draw, text, face_path, max_w, max_size, min_size, fill):
    size = max_size
    while size > min_size:
        f = font(face_path, size)
        if draw.textlength(text, font=f) <= max_w:
            return f
        size -= 1
    return font(face_path, min_size)


def wrap(draw, text, f, max_w):
    words = text.split()
    lines, cur = [], ""
    for word in words:
        trial = word if not cur else cur + " " + word
        if draw.textlength(trial, font=f) <= max_w:
            cur = trial
        else:
            if cur:
                lines.append(cur)
            cur = word
    if cur:
        lines.append(cur)
    return lines


def button(base, box, title, sub, hot, bar=None, right=24, inset=0):
    x0, y0, x1, y1 = box
    shadow(base, box)
    d = ImageDraw.Draw(base)
    fill = HOT if hot else PANEL
    rounded(d, box, 22, fill, GOLD if hot else STROKE, 6 if hot else 3)
    d.rectangle((x0 + 16, y0 + 8, x1 - 16, y0 + 16), fill=GOLD if hot else (255, 255, 255, 70))
    if bar is not None:
        d.rounded_rectangle((x0 + 14, y0 + 28, x0 + 26, y1 - 14), 4, fill=bar if not hot else INK)
    title_c = INK if hot else CREAM
    sub_c = INK if hot else MUTE
    left = (40 if bar is not None else 28) + inset
    tw = x1 - x0 - left - right
    if tw < 80:
        tw = 80
    box_h = y1 - y0
    # Rows of 108–128 match UiFit: stripe, title at y+24 size 40, sub at y+70 size 30.
    if box_h >= 108:
        title_size = 40
        sub_size = 30
        title_y = y0 + 24
        sub_y = y0 + 70
    else:
        title_size = 32
        sub_size = 20
        title_y = y0 + 20
        sub_y = title_y + title_size + 8
    tf = fit_text(d, title, FONT_D, tw, title_size, 26, title_c)
    d.text((x0 + left, title_y), title, font=tf, fill=title_c)
    if sub:
        size = sub_size
        sf = font(FONT_B, size)
        while size > 18 and d.textlength(sub, font=sf) > tw:
            size -= 1
            sf = font(FONT_B, size)
        if sub_y + size <= y1 - 4:
            d.text((x0 + left, sub_y), sub, font=sf, fill=sub_c)
    return contrast(title_c, fill)


def plate(base, box, fill=(8, 22, 58, 230)):
    shadow(base, box, 16, 6)
    d = ImageDraw.Draw(base)
    rounded(d, box, 24, fill, GOLD, 3)


def backdrop(alpha):
    park = Image.open(PARK).convert("RGB").resize((W, H), Image.Resampling.LANCZOS)
    park = ImageEnhance.Brightness(park).enhance(0.72)
    veil = Image.new("RGB", (W, H), (8, 24, 72))
    img = Image.blend(park, veil, 1.0 - alpha)
    img = img.convert("RGBA")
    vig = Image.new("L", (W, H), 0)
    vd = ImageDraw.Draw(vig)
    vd.ellipse((-200, -80, W + 200, H + 220), fill=255)
    vig = vig.filter(ImageFilter.GaussianBlur(80))
    dark = Image.new("RGBA", (W, H), (2, 6, 18, 0))
    dd = ImageDraw.Draw(dark)
    dd.rectangle((0, 0, W, H), fill=(2, 6, 18, 150))
    mask = Image.eval(vig, lambda p: 255 - p)
    dark.putalpha(mask)
    img.alpha_composite(dark)
    return img


def header(img, title, banner):
    d = ImageDraw.Draw(img)
    lock = Image.open(LOCK).convert("RGBA")
    lock.thumbnail((220, 120), Image.Resampling.LANCZOS)
    img.alpha_composite(lock, (70, 18))
    tf = font(FONT_D, 64)
    d.text((310, 28), title, font=tf, fill=GOLD)
    if banner:
        bf = font(FONT_B, 28)
        tw = d.textlength(banner, font=bf)
        pad = 18
        marks = banner == "Everyone Ready? Press Start"
        extra = 140 if marks else 0
        bx0 = 300
        rounded(d, (bx0, 94, bx0 + tw + pad * 2 + extra, 132), 10, NAVY)
        d.text((bx0 + pad, 96), banner, font=bf, fill=GOLD)
        if marks:
            gx = bx0 + pad + int(tw) + 12
            draw_glyph(d, "space", gx, 98)
            draw_glyph(d, "start", gx + 70, 100)
    d.rectangle((64, 148, W - 64, 154), fill=GOLD)


def footer(img, words=None):
    # Keyboard glyph and pad glyph together. Words are PadGlyph.Line for each.
    d = ImageDraw.Draw(img)
    labels = [
        "Arrows / Stick   move",
        "Space / A   confirm",
        "Esc / B   back",
    ]
    pairs = [("arrows", "stick"), ("space", "a"), ("esc", "b")]
    chip_w = 500
    gap = 16
    total = 3 * chip_w + 2 * gap
    x = (W - total) // 2
    y = H - 92
    for (g0, g1), word in zip(pairs, labels):
        box = (x, y, x + chip_w, y + 64)
        rounded(d, box, 14, (20, 41, 92), GOLD, 2)
        draw_glyph(d, g0, x + 12, y + 8)
        draw_glyph(d, g1, x + 78, y + 8)
        d.text((x + 136, y + 16), word, font=font(FONT_B, 22), fill=CREAM)
        x += chip_w + gap


def draw_glyph(d, kind, x, y):
    if kind == "arrows":
        # Same mark as MenuIcons.ArrowKeys: a key cap and a four-way arrow.
        rounded(d, (x, y, x + 64, y + 48), 8, (15, 26, 46))
        rounded(d, (x + 4, y + 4, x + 60, y + 44), 6, (245, 247, 255))
        ink = (15, 26, 51)
        d.polygon([(x + 32, y + 8), (x + 24, y + 18), (x + 40, y + 18)], fill=ink)
        d.polygon([(x + 32, y + 40), (x + 24, y + 30), (x + 40, y + 30)], fill=ink)
        d.polygon([(x + 10, y + 24), (x + 20, y + 16), (x + 20, y + 32)], fill=ink)
        d.polygon([(x + 54, y + 24), (x + 44, y + 16), (x + 44, y + 32)], fill=ink)
        d.rectangle((x + 28, y + 20, x + 36, y + 28), fill=ink)
    elif kind == "space":
        rounded(d, (x, y + 10, x + 62, y + 36), 6, CREAM)
        d.text((x + 8, y + 12), "space", font=font(FONT_B, 14), fill=INK)
    elif kind == "esc":
        rounded(d, (x, y + 6, x + 48, y + 40), 6, CREAM)
        d.text((x + 8, y + 10), "esc", font=font(FONT_B, 18), fill=INK)
    elif kind == "stick":
        # Same mark as MenuIcons.StickCap: cap, well, and the stick nub.
        d.ellipse((x + 4, y + 6, x + 52, y + 54), fill=(26, 36, 56))
        d.ellipse((x + 10, y + 12, x + 46, y + 48), fill=(209, 224, 245))
        d.ellipse((x + 24, y + 26, x + 32, y + 34), fill=(26, 36, 56))
        d.ellipse((x + 36, y + 2, x + 58, y + 24), fill=(15, 26, 51))
        d.ellipse((x + 40, y + 6, x + 54, y + 20), fill=CREAM)
    elif kind == "a":
        d.ellipse((x, y + 2, x + 44, y + 46), fill=(30, 170, 70))
        d.text((x + 12, y + 6), "A", font=font(FONT_D, 28), fill=CREAM)
    elif kind == "b":
        d.ellipse((x, y + 2, x + 44, y + 46), fill=(210, 48, 52))
        d.text((x + 12, y + 6), "B", font=font(FONT_D, 28), fill=CREAM)
    elif kind == "keys":
        rounded(d, (x, y + 8, x + 46, y + 40), 6, CREAM, INK, 2)
        for row in range(2):
            for col in range(3):
                d.rectangle((x + 6 + col * 12, y + 14 + row * 10, x + 14 + col * 12, y + 20 + row * 10), fill=INK)
    elif kind == "pad":
        rounded(d, (x + 2, y + 10, x + 48, y + 40), 8, CREAM, INK, 2)
        d.ellipse((x + 28, y + 18, x + 36, y + 26), fill=(242, 56, 71))
        d.ellipse((x + 36, y + 26, x + 44, y + 34), fill=(41, 115, 255))
    elif kind == "start":
        rounded(d, (x, y + 4, x + 44, y + 36), 8, CREAM, INK, 2)
        d.rectangle((x + 8, y + 12, x + 36, y + 15), fill=INK)
        d.rectangle((x + 8, y + 19, x + 36, y + 22), fill=INK)
        d.rectangle((x + 8, y + 26, x + 36, y + 29), fill=INK)


def draw_switch(d, x, y, on):
    # MenuWidgets.Toggle: 96x40 pill. On is gold with the knob on the right.
    track = GOLD if on else (13, 20, 41)
    rounded(d, (x, y, x + 96, y + 40), 20, track, STROKE, 2)
    kx = x + (60 if on else 4)
    rounded(d, (kx, y + 4, kx + 32, y + 36), 16, CREAM)


def footer_both(img):
    footer(img)


def save(img, name, notes):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    rgb = img.convert("RGB")
    rgb.save(path, "PNG", optimize=True, compress_level=9)
    size = os.path.getsize(path)
    if size > 390000:
        # Seat yellow sits next to the gold accent. A plain palette merge
        # turns that swatch gold, so those colors are painted back on.
        protect = list(SEAT) + list(PD_RGB) + list(TR_RGB) + [GOLD, CREAM, INK, HOT, PANEL, NAVY, MUTE, STROKE, (13, 20, 41)]
        pinned = None
        for colors in (256, 224, 192, 160):
            pinned = pin_colors(rgb, protect, colors)
            pinned.save(path, "PNG", optimize=True, compress_level=9)
            size = os.path.getsize(path)
            if size <= 390000:
                break
    notes.append((name, size, rgb.size))
    print(f"{name} {size} {rgb.size}")


def pin_colors(rgb, protect, colors):
    import numpy as np
    budget = colors - len(protect)
    if budget < 32:
        budget = 32
    q = rgb.quantize(colors=budget, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE)
    src = np.asarray(rgb)
    out = np.array(q.convert("RGB"))
    for color in protect:
        want = np.array(color, dtype=np.uint8)
        mask = np.all(src == want, axis=-1)
        out[mask] = want
    return Image.fromarray(out, "RGB")


def screen(alpha=0.5):
    return backdrop(alpha)


def mode_rules(focus_rule=False):
    img = screen(0.5)
    header(img, "Mode and rules", "Up and down move. Left and right change a rule. Left at the end returns to the modes.")
    modes = [
        ("Hot Potato", "First to 2. Fuse 45 / 40 / 35s.", False),
        ("Least It", "Least time as It. Next punch breaks a tie.", True),
        ("Trail Tag", "Ribbons eliminate. Last standing.", False),
        ("Free play", "Punch transfers It. No timer.", False),
    ]
    ratios = []
    for i, (name, blurb, sel) in enumerate(modes):
        col, row = i % 2, i // 2
        x = 80 + col * 400
        y = 176 + row * 156
        hot = (not focus_rule) and sel
        mark = "Selected" if sel else ""
        ratios.append(button(img, (x, y, x + 380, y + 144), name, (blurb + "  " + mark).strip(), hot))
    rules = [
        ("Round length", "120 s"),
        ("Rounds", "1"),
        ("Win target", "2"),
        ("Starting It", "Random"),
        ("Chosen seat", "P1"),
        ("P1 handicap", "Off"),
        ("P2 handicap", "Off"),
        ("P3 handicap", "Off"),
    ]
    if focus_rule:
        rules = [
            ("Launch pads", "On"),
            ("Zip lines", "On"),
            ("AI opponents", "1   (0-3, seats left over)"),
            ("Difficulty", "Normal"),
            ("Split", "Vertical"),
            ("Listener", "P1"),
            ("Arena select", "Next"),
            ("Back", "Characters"),
        ]
    for i, (title, detail) in enumerate(rules):
        y = 180 + i * 84
        hot = focus_rule and i == 0
        ratios.append(button(img, (880, y, 1760, y + 76), title, detail, hot))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((1772, 188, 1786, 820), 4, fill=(0, 0, 0, 140))
    if focus_rule:
        d.rounded_rectangle((1774, 620, 1784, 808), 3, fill=GOLD)
    else:
        d.rounded_rectangle((1774, 188, 1784, 420), 3, fill=GOLD)
    plate(img, (80, 500, 860, 900), (8, 28, 70, 235))
    d = ImageDraw.Draw(img)
    d.text((104, 516), "How to play", font=font(FONT_D, 36), fill=GOLD)
    d.text((104, 568), "Least time as It. Next punch breaks a tie.", font=font(FONT_B, 26), fill=CREAM)
    d.rounded_rectangle((104, 640, 520, 672), 8, fill=SEAT[0])
    d.ellipse((540, 628, 588, 676), fill=GOLD)
    footer_both(img)
    return img, min(ratios)


def arena(which):
    img = screen(0.42)
    names = ["Mega Park", "Pocket Park", "Stack Yard"]
    blurbs = [
        "160 x 100 m\nPads, zips, and the long loop.",
        "80 x 50 m\nA tight park on the same pieces.",
        "110 x 70 m\nDecks at 6 m and roofs at 12 m.",
    ]
    focus = names.index(which)
    header(img, "Arena", which + "   ·   " + blurbs[focus].split("\n")[0])
    gap = 24
    card_w = 500
    x0 = (W - (card_w * 3 + gap * 2)) // 2
    ratios = []
    for i, name in enumerate(names):
        x = x0 + i * (card_w + gap)
        y = 176
        hot = i == focus
        box = (x, y, x + card_w, y + 560)
        shadow(img, box)
        d = ImageDraw.Draw(img)
        rounded(d, box, 22, HOT if hot else PANEL, GOLD if hot else STROKE, 6 if hot else 3)
        hero = Image.open(HERO[name]).convert("RGB").resize((card_w - 36, 380), Image.Resampling.LANCZOS)
        mask = Image.new("L", hero.size, 0)
        ImageDraw.Draw(mask).rounded_rectangle((0, 0, hero.size[0] - 1, hero.size[1] - 1), 16, fill=255)
        img.paste(hero, (x + 18, y + 16), mask)
        title_c = INK if hot else CREAM
        sub_c = INK if hot else MUTE
        d.text((x + 24, y + 410), name, font=font(FONT_D, 40), fill=title_c)
        yy = y + 462
        for line in blurbs[i].split("\n"):
            d.text((x + 24, yy), line, font=font(FONT_B, 24), fill=sub_c)
            yy += 30
        ratios.append(contrast(title_c, HOT if hot else PANEL))
    ratios.append(button(img, (x0, 760, x0 + 760, 870), "Random", "One of Mega Park, Pocket Park, or Stack Yard.", False))
    ratios.append(button(img, (x0 + 784, 760, x0 + card_w * 3 + gap * 2, 870), "Back", "", False))
    footer_both(img)
    return img, min(ratios)


def unsky(path):
    im = Image.open(path).convert("RGBA")
    px = im.load()
    w, h = im.size
    for y in range(h):
        v = 1.0 - (y / float(h - 1))
        sky = 0.55 + v * 0.45
        br = int(255 * (0.95 * (1 - sky) + 0.45 * sky))
        bgc = int(255 * (0.62 * (1 - sky) + 0.68 * sky))
        bb = int(255 * (0.38 * (1 - sky) + 0.88 * sky))
        for x in range(w):
            r, g, b, a = px[x, y]
            if abs(r - br) <= 6 and abs(g - bgc) <= 6 and abs(b - bb) <= 6:
                px[x, y] = (r, g, b, 0)
    return im


def figure(seat):
    path = os.path.join(FIG, "place_%d-composite.png" % seat)
    im = unsky(path)
    px = im.load()
    w, h = im.size
    minx, miny, maxx, maxy = w, h, 0, 0
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 20:
                continue
            if r + g + b < 90:
                continue
            if x < minx:
                minx = x
            if y < miny:
                miny = y
            if x > maxx:
                maxx = x
            if y > maxy:
                maxy = y
    if maxx <= minx or maxy <= miny:
        return im
    pad = 4
    return im.crop((max(0, minx - pad), max(0, miny - pad), min(w, maxx + pad + 1), min(h, maxy + 1)))


def foot_row(im):
    px = im.load()
    w, h = im.size
    for y in range(h - 1, -1, -1):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a > 40 and r + g + b > 90:
                return y
    return h - 1


def badge_paint(seat_color):
    if contrast(INK, seat_color) >= 5.0:
        return seat_color, INK
    plate = seat_color
    for _ in range(8):
        plate = tuple(int(c * 0.88) for c in plate)
        if contrast(CREAM, plate) >= 5.0:
            return plate, CREAM
    return plate, CREAM


def results():
    img = screen(0.85)
    header(img, "RESULTS", "Least It  ·  Red / Tan")
    # Left to right matches the place order: 2nd, 1st, 3rd, 4th.
    seats = [1, 0, 2, 3]
    places = ["2nd  P2", "1st  P1", "3rd  P3", "4th  P4"]
    stats = [
        "5 tags\n14.7s as It\n1 round win",
        "WIN  6 tags\n8.5s as It\n2 round wins",
        "4 tags\n20.9s as It\n0 round wins",
        "3 tags\n27.1s as It\n0 round wins",
    ]
    card = (15, 31, 71)
    rank_w = 420
    x0 = 96
    floor = 520
    # Column order is 2nd, 1st, 3rd, 4th. The other steps descend.
    step_h = [58, 86, 36, 18]
    fig_box = [(200, 250), (230, 290), (180, 230), (160, 210)]
    step_fill = [(190, 198, 214), (232, 196, 92), (176, 112, 64), (28, 58, 110)]
    ratios = []
    for col, seat in enumerate(seats):
        winner = col == 1
        sx = x0 + col * (rank_w + 20)
        step_top = floor - step_h[col]
        d = ImageDraw.Draw(img)
        rounded(d, (sx + 70, step_top, sx + rank_w - 70, floor + 8), 8, step_fill[col], GOLD if winner else STROKE, 4 if winner else 2)
        crop = figure(seat)
        crop.thumbnail(fig_box[col], Image.Resampling.LANCZOS)
        feet = foot_row(crop)
        fx = sx + (rank_w - crop.size[0]) // 2
        fy = step_top - feet
        img.alpha_composite(crop, (fx, fy))
        if winner:
            d = ImageDraw.Draw(img)
            for k in range(8):
                cx = sx + 36 + (k * 47) % 340
                cy = fy + 8 + (k * 23) % 48
                d.rectangle((cx, cy, cx + 7, cy + 12), fill=GOLD if k % 2 == 0 else CREAM)
        box = (sx, 548, sx + rank_w, 760)
        shadow(img, box)
        d = ImageDraw.Draw(img)
        rounded(d, box, 18, card, GOLD if winner else STROKE, 5 if winner else 3)
        badge, ink = badge_paint(SEAT[seat])
        d.rounded_rectangle((box[0] + 28, box[1] + 16, box[0] + 96, box[1] + 52), 8, fill=badge)
        d.text((box[0] + 44, box[1] + 18), "P" + str(seat + 1), font=font(FONT_B, 24), fill=ink)
        d.text((box[0] + 112, box[1] + 16), places[col], font=font(FONT_D, 32), fill=CREAM)
        yy = box[1] + 72
        for line in stats[col].split("\n"):
            d.text((box[0] + 28, yy), line, font=font(FONT_B, 24), fill=CREAM)
            yy += 28
        ratios.append(contrast(CREAM, card))
        ratios.append(contrast(ink, badge))
    actions = [("Rematch", "Same setup", True), ("Change mode", "", False), ("Character select", "", False), ("Main menu", "", False)]
    for i, (title, sub, hot) in enumerate(actions):
        x = x0 + i * (rank_w + 20)
        ratios.append(button(img, (x, 772, x + rank_w, 900), title, sub, hot))
    footer_both(img)
    return img, min(ratios)


def loading():
    img = screen(0.62)
    header(img, "Loading", "")
    plate(img, (180, 168, 1740, 980))
    d = ImageDraw.Draw(img)
    d.text((220, 188), "Mega Park", font=font(FONT_D, 64), fill=GOLD)
    rows = [
        ("Length", "120 s"),
        ("Rounds", "1"),
        ("Starting It", "Random"),
        ("Handicaps", "none"),
        ("Pads", "On"),
        ("Zips", "On"),
    ]
    y = 280
    for label, value in rows:
        d.text((260, y), label, font=font(FONT_B, 30), fill=MUTE)
        d.text((620, y), value, font=font(FONT_B, 30), fill=CREAM)
        y += 52
    # Last rule ends near y. Tip and the bar sit below that row.
    tip = (400, y + 28, 1520, y + 100)
    rounded(d, tip, 16, GOLD, INK, 3)
    d.text((428, y + 46), "TIP", font=font(FONT_D, 32), fill=INK)
    tip_line = "Hold into a wall to cling. Jump while clinging to wall jump."
    tip_font = font(FONT_B, 26)
    d.text((540, y + 50), tip_line, font=tip_font, fill=INK)
    track_y = y + 124
    track = (400, track_y, 1520, track_y + 36)
    rounded(d, track, 10, (5, 13, 31), STROKE, 2)
    # AsyncOperation.progress of 0.6, left-aligned. Activation is still off until 0.9.
    span = 1520 - 400 - 12
    fill = int(span * 0.60)
    rounded(d, (406, track_y + 6, 406 + fill, track_y + 30), 6, GOLD)
    d.text((760, track_y + 48), "Loading  60%", font=font(FONT_B, 28), fill=CREAM)
    return img, min(contrast(CREAM, (8, 22, 58)), contrast(INK, GOLD), contrast(GOLD, (5, 13, 31)))


def pause():
    img = screen(0.28)
    header(img, "Paused by P1", "Mega Park  ·  Least It")
    items = [("Resume", "", True), ("Restart round", "Same arena, same rules", False), ("Options", "", False), ("Quit to menu", "", False)]
    ratios = []
    x = 520
    y = 220
    for title, sub, hot in items:
        ratios.append(button(img, (x, y, x + 880, y + 108), title, sub, hot))
        y += 120
    ratios.append(contrast(GOLD, NAVY))
    footer_both(img)
    return img, min(ratios)


def options(page, quality="Medium", hub="bar", access="tiles", mute=False, levels=True, seats="off"):
    if page == "access" and access != "strip":
        return access_page(seats)
    img = screen(0.5)
    picture_detail = "Low, Medium, High, Ultra" if levels else "Left / Right"
    sound_rows = [
        ("Master  0.00" if mute else "Master  0.80", "Left / Right", True),
        ("SFX  1.00", "Left / Right", False),
        ("UI  1.00", "Left / Right", False),
        ("Music  0.35", "Left / Right", False),
        ("Mute  On" if mute else "Mute  (Comma)", "Left / Right", False),
        ("Reset to defaults?", "Confirm to reset", False),
        ("Back", "", False),
    ]
    pages = {
        "hub": ("Options", "Sound, picture, accessibility, controls, look, and credits.", [
            ("Sound", "Music, effects, and UI.", True),
            ("Picture", "Resolution, fullscreen, and scale.", False),
            ("Accessibility", "Motion, text size, and colors.", False),
            ("Controls", "Keyboard and pad. Space jumps.", False),
            ("Look", "One sensitivity for the couch.", False),
            ("Credits", "Team, the font, and the tools.", False),
        ] + ([("Reset to defaults", "Sound, picture, and accessibility", False)] if hub == "before" else []), {}),
        "sound": ("Sound", "Sliders step the volumes you already have.", sound_rows,
                  {0: 0.0 if mute else 0.80, 1: 1.0, 2: 1.0, 3: 0.35}),
        "picture": ("Picture", "Resolution, fullscreen, vsync, and the couch UI scale.", [
            ("Resolution  1920 x 1080", "Left / Right", True),
            ("Fullscreen  On", "Left / Right", False),
            ("VSync  On", "Left / Right", False),
            ("Quality  " + quality, picture_detail, False),
            ("UI scale  100%", "80% to 130%, for a couch TV", False),
            ("Reset to defaults", "This page only", False),
            ("Back", "", False),
        ], {4: 0.40}),
        "access": ("Accessibility", "Reduce motion, text size, player colors, and comic words.", [
            ("Reduce motion  Off", "Menu slides and the title pulse only", True),
            ("Text size  1.00", "0.85, 1.00, 1.25, 1.50", False),
            ("Player  P1", "Left / Right", False),
            ("Colorblind palette  Default", "Left / Right", False),
            ("Comic words  On", "Verb words during a match.", False),
        ] + ([
            ("Reset to defaults", "This page only", False),
            ("Back", "", False),
        ] if access == "strip" else []), {1: 0.333}),
    }
    title, banner, rows, meters = pages[page]
    header(img, title, banner)
    ratios = []
    y = 148
    row_h = 108
    step = 108 if page == "access" and access != "strip" else 116
    for i, (name, sub, hot) in enumerate(rows):
        inset = 52 if page == "hub" and i < 6 else 0
        ratios.append(button(img, (280, y, 1640, y + row_h), name, sub, hot, inset=inset))
        if inset:
            d = ImageDraw.Draw(img)
            tints = ((64, 158, 255), GOLD, (242, 56, 71), (51, 209, 97), (255, 140, 31), GOLD)
            d.rounded_rectangle((294, y + 26, 350, y + 82), 12, fill=tints[i])
        if i in meters:
            d = ImageDraw.Draw(img)
            d.rounded_rectangle((1080, y + 74, 1500, y + 90), 4, fill=(0, 0, 0, 90))
            d.rounded_rectangle((1080, y + 74, 1080 + int(420 * meters[i]), y + 90), 4, fill=GOLD)
        if page == "access" and i in (0, 4):
            d = ImageDraw.Draw(img)
            draw_switch(d, 1640 - 96 - 28, y + 34, i == 4)
        y += step
    if page == "hub" and hub == "bar":
        ratios.append(prompt_bar(img, y + 4, 280, 1360, hot="back"))
    if page == "access" and access == "strip":
        d = ImageDraw.Draw(img)
        sy = y - step + row_h + 6
        sh = 32
        d.text((300, sy + 2), "Default", font=font(FONT_B, 22), fill=CREAM)
        ratios.append(contrast(CREAM, (8, 22, 58)))
        for i, c in enumerate(SEAT):
            x = 520 + i * 160
            rounded(d, (x, sy, x + 140, sy + sh), 10, c)
            ratios.append(contrast(CREAM, (8, 22, 58)))
    if page == "access" and access != "strip":
        ratios.extend(swatch_band(img, y + 8))
    ratios.append(contrast(GOLD, NAVY))
    footer_both(img)
    return img, min(ratios)


def prompt_bar(img, y, x, w, hot="back"):
    """Reset and Back on one row. Glyphs match the footer: space/A confirm, esc/B back."""
    d = ImageDraw.Draw(img)
    gap = 16
    chip_w = (w - gap) // 2
    h = 72
    chips = (("reset", "Reset", "space", "a"), ("back", "Back", "esc", "b"))
    ratio = 99
    for i, (key, word, g0, g1) in enumerate(chips):
        cx = x + i * (chip_w + gap)
        selected = key == hot
        fill = HOT if selected else NAVY
        ink = INK if selected else CREAM
        rounded(d, (cx, y, cx + chip_w, y + h), 14, fill, GOLD, 3 if selected else 2)
        draw_glyph(d, g0, cx + 12, y + 12)
        draw_glyph(d, g1, cx + 84, y + 12)
        d.text((cx + 148, y + 20), word, font=font(FONT_B, 28), fill=ink)
        ratio = min(ratio, contrast(ink, fill))
    return ratio


# Seat colors from AccessibilityPalette.Default, then the same CVD matrices.
SEAT_F = (
    (0.95, 0.16, 0.22),
    (0.16, 0.45, 1.00),
    (1.00, 0.86, 0.12),
    (0.16, 0.82, 0.28),
)
# Okabe-Ito blue, vermillion, sky, yellow. Tritan is the second measured set.
PD_F = (
    (0 / 255, 114 / 255, 178 / 255),
    (213 / 255, 94 / 255, 0 / 255),
    (86 / 255, 180 / 255, 233 / 255),
    (240 / 255, 228 / 255, 66 / 255),
)
TR_F = (
    (0.90, 0.20, 0.25),
    (0.10, 0.78, 0.82),
    (0.98, 0.62, 0.12),
    (0.22, 0.12, 0.58),
)


def rgb_of(cols):
    return [tuple(int(round(c * 255)) for c in rgb) for rgb in cols]


PD_RGB = rgb_of(PD_F)
TR_RGB = rgb_of(TR_F)


def simulate_cvd(kind, rgb):
    r, g, b = rgb
    if kind == "protan":
        o = (0.152286 * r + 1.052583 * g - 0.204868 * b,
             0.114503 * r + 0.786281 * g + 0.099216 * b,
             -0.003882 * r - 0.048116 * g + 1.051998 * b)
    elif kind == "tritan":
        o = (1.255528 * r - 0.076749 * g - 0.178779 * b,
             -0.078411 * r + 0.930809 * g + 0.147602 * b,
             0.004733 * r + 0.691367 * g + 0.303900 * b)
    else:
        o = (0.367322 * r + 0.860646 * g - 0.227968 * b,
             0.280085 * r + 0.672501 * g + 0.047413 * b,
             -0.011820 * r + 0.042940 * g + 0.968881 * b)
    return tuple(max(0.0, min(1.0, c)) for c in o)


def pair_distance(kind, colors=None):
    src = SEAT_F if colors is None else colors
    cols = [simulate_cvd(kind, c) for c in src]
    worst = 99.0
    for i in range(4):
        for j in range(i + 1, 4):
            d = sum((cols[i][k] - cols[j][k]) ** 2 for k in range(3)) ** 0.5
            if d < worst:
                worst = d
    return worst


def seat_mark(d, kind, box, ink):
    x0, y0, x1, y1 = box
    if kind == 0:
        d.ellipse(box, fill=ink)
    elif kind == 1:
        d.polygon((((x0 + x1) / 2, y0 + 1), (x0 + 1, y1 - 1), (x1 - 1, y1 - 1)), fill=ink)
    elif kind == 2:
        d.rectangle((x0 + 1, y0 + 1, x1 - 1, y1 - 1), fill=ink)
    else:
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        d.polygon(((cx, y0), (x1, cy), (cx, y1), (x0, cy)), fill=ink)


def swatch_band(img, y, colors=None, title="Default", marks=False, sw_h=78, row_pitch=40):
    d = ImageDraw.Draw(img)
    ratios = []
    src = SEAT_F if colors is None else colors
    d.text((300, y), title, font=font(FONT_B, 28), fill=CREAM)
    ratios.append(contrast(CREAM, (8, 22, 58)))
    tiles = y + 32
    for i, rgb in enumerate(src):
        c = tuple(int(round(ch * 255)) for ch in rgb)
        x = 620 + i * 150
        rounded(d, (x, tiles, x + 120, tiles + sw_h), 12, c)
        if marks:
            luma = 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]
            ink = INK if luma > 140 else CREAM
            seat_mark(d, i, (x + 8, tiles + 22, x + 36, tiles + 54), ink)
        cap = (x, tiles + sw_h + 4, x + 120, tiles + sw_h + 40)
        rounded(d, cap, 8, NAVY)
        label = "P%d" % (i + 1)
        tw = d.textlength(label, font=font(FONT_B, 28))
        d.text((x + (120 - tw) / 2, tiles + sw_h + 6), label, font=font(FONT_B, 28), fill=CREAM)
        ratios.append(contrast(CREAM, NAVY))
    row_y = tiles + sw_h + 48
    for kind, name in (("protan", "Protan"), ("deutan", "Deutan"), ("tritan", "Tritan")):
        dist = pair_distance(kind, src)
        d.text((300, row_y + 2), "%s  %.2f" % (name, dist), font=font(FONT_B, 24), fill=CREAM)
        ratios.append(contrast(CREAM, (8, 22, 58)))
        cols = [simulate_cvd(kind, c) for c in src]
        for i, rgb in enumerate(cols):
            c = tuple(int(round(ch * 255)) for ch in rgb)
            x = 620 + i * 150
            rounded(d, (x, row_y, x + 120, row_y + 32), 8, c)
        row_y += row_pitch
    return ratios


def access_page(seats="off"):
    """Accessibility. Reset and Back stay on the bottom row, outside the scroll."""
    img = screen(0.5)
    header(img, "Accessibility", "Motion, text size, seat colours, and comic words.")
    if seats == "pd":
        seat_name, colors, head = "Protan/Deutan", PD_F, "Protan/Deutan"
    elif seats == "tritan":
        seat_name, colors, head = "Tritan", TR_F, "Tritan"
    else:
        seat_name, colors, head = "Off", SEAT_F, "Default"
    rows = [
        ("Player  P1", "Left / Right", False),
        ("Colorblind palette  Default", "Left / Right", False),
        ("Colour-blind seat palette  " + seat_name, "Off, Protan/Deutan, or Tritan", seats != "off"),
        ("Comic words  On", "Verb words during a match.", False),
    ]
    ratios = []
    y = 160
    row_h = 108
    for i, (name, sub, hot) in enumerate(rows):
        ratios.append(button(img, (280, y, 1640, y + row_h), name, sub, hot))
        if i == 3:
            d = ImageDraw.Draw(img)
            draw_switch(d, 1640 - 96 - 28, y + 34, True)
        y += 112
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((1660, 172, 1674, 560), 4, fill=(0, 0, 0, 140))
    d.rounded_rectangle((1662, 360, 1672, 548), 3, fill=GOLD)
    ratios.extend(swatch_band(img, y + 8, colors, head, marks=seats != "off", sw_h=64, row_pitch=34))
    ratios.append(prompt_bar(img, 900, 280, 1360, hot="back"))
    ratios.append(contrast(GOLD, NAVY))
    footer_both(img)
    return img, min(ratios)


# ActionBinds.Show, plus Xbox face names from ActionBinds.PadWord.
SHOW = {
    "wasd": "WASD",
    "mouse": "Mouse",
    "mouseLeft": "LMB",
    "space": "Space",
    "holdIntoWall": "Hold into wall",
    "leftCtrl": "Ctrl",
    "leftShift": "Shift",
    "leftAlt": "Alt",
    "q": "Q",
    "e": "E",
    "c": "C",
    "leftStick": "Left stick",
    "rightStick": "Right stick",
    "leftStickHold": "Left stick hold",
    "buttonSouth": "A",
    "buttonEast": "B",
    "buttonWest": "X",
    "buttonNorth": "Y",
    "leftShoulder": "LB",
    "rightShoulder": "RB",
    "leftTrigger": "LT",
    "rightTrigger": "RT",
}


def bind_line(keys, pads, cling=False):
    word = SHOW[keys[0]]
    if len(keys) > 1:
        word = word + " or " + SHOW[keys[1]]
    pad = SHOW[pads[0]]
    if cling:
        return word + " / " + pad + ". Wall climb and wall run need this hold. Wall jump is this hold plus Jump."
    return word + "    /    " + pad


def mark_width(token):
    if token in ("leftCtrl", "leftShift", "rightShift"):
        return 96
    if token in ("leftAlt", "mouseLeft", "mouseRight"):
        return 84
    if token in ("rightShoulder", "leftShoulder", "leftTrigger", "rightTrigger"):
        return 70
    if token in ("wasd", "arrows", "space", "leftStick", "rightStick", "leftStickHold"):
        return 72
    return 52


X_BLUE = (26, 97, 235)
Y_GOLD = (242, 199, 41)
KEY_INK = (15, 26, 51)
KEY_CAP = (245, 247, 255)
B_RED = (210, 48, 52)
A_GREEN = (30, 170, 70)


def draw_token(d, token, x, y):
    w = mark_width(token)
    h = 48
    rounded(d, (x, y, x + w, y + h), 8, NAVY)
    inset = 4
    ix, iy = x + inset, y + inset
    iw, ih = w - inset * 2, h - inset * 2
    if token in ("wasd", "arrows"):
        draw_glyph(d, "arrows", ix, iy - 2)
    elif token == "space":
        draw_glyph(d, "space", ix, iy)
    elif token == "escape":
        draw_glyph(d, "esc", ix, iy)
    elif token in ("leftStick", "rightStick", "leftStickHold"):
        draw_glyph(d, "stick", ix, iy - 4)
    elif token == "buttonSouth":
        draw_face(d, ix, iy, "A", A_GREEN, CREAM)
    elif token == "buttonEast":
        draw_face(d, ix, iy, "B", B_RED, CREAM)
    elif token == "buttonWest":
        draw_face(d, ix, iy, "X", X_BLUE, CREAM)
    elif token == "buttonNorth":
        draw_face(d, ix, iy, "Y", Y_GOLD, (20, 20, 26))
    elif token == "mouse":
        draw_mouse(d, ix, iy, False)
    elif token == "mouseLeft":
        draw_keycap(d, ix, iy, "LMB", iw, ih)
    elif token == "holdIntoWall":
        draw_wall(d, ix, iy)
    elif token in ("rightShoulder", "leftShoulder", "leftTrigger", "rightTrigger"):
        draw_bumper(d, ix, iy, SHOW[token], iw, ih)
    else:
        draw_keycap(d, ix, iy, SHOW.get(token, token), iw, ih)


def draw_keycap(d, x, y, label, w, h):
    rounded(d, (x, y, x + w, y + h), 6, KEY_CAP, KEY_INK, 2)
    size = 16 if len(label) >= 4 else 20
    face = font(FONT_B, size)
    tw = d.textlength(label, font=face)
    d.text((x + (w - tw) / 2, y + (h - size) / 2 - 2), label, font=face, fill=KEY_INK)


def draw_bumper(d, x, y, label, w, h):
    rounded(d, (x, y + 4, x + w, y + h - 2), 12, KEY_CAP, KEY_INK, 2)
    face = font(FONT_B, 18)
    tw = d.textlength(label, font=face)
    d.text((x + (w - tw) / 2, y + 8), label, font=face, fill=KEY_INK)


def draw_face(d, x, y, letter, fill, ink):
    d.ellipse((x + 2, y + 2, x + 42, y + 42), fill=fill)
    face = font(FONT_D, 26)
    tw = d.textlength(letter, font=face)
    d.text((x + 2 + (40 - tw) / 2, y + 4), letter, font=face, fill=ink)


def draw_mouse(d, x, y, left):
    rounded(d, (x + 6, y + 2, x + 40, y + 42), 12, KEY_CAP, KEY_INK, 2)
    d.line((x + 23, y + 8, x + 23, y + 36), fill=KEY_INK, width=2)
    if left:
        d.rectangle((x + 10, y + 8, x + 21, y + 22), fill=X_BLUE)
    d.ellipse((x + 20, y + 18, x + 26, y + 24), fill=KEY_INK)


def draw_wall(d, x, y):
    d.rectangle((x + 30, y + 4, x + 42, y + 42), fill=(158, 178, 214))
    d.rectangle((x + 26, y + 4, x + 30, y + 42), fill=CREAM)
    d.rectangle((x + 2, y + 18, x + 14, y + 28), fill=CREAM)
    d.polygon([(x + 26, y + 23), (x + 12, y + 10), (x + 12, y + 36)], fill=CREAM)


def controls(bottom=False):
    img = screen(0.5)
    header(img, "Controls", "Keyboard and pad glyphs. Confirm changes one. Space still jumps.")
    # Same 108 px rows as options. At 100% the window is 7, so the rest scroll.
    # Tokens are ActionBinds.Fill. The second key is the one PlayerInputReader ORs in.
    if not bottom:
        rows = [
            ("Move", ["wasd"], ["leftStick"], False),
            ("Look", ["mouse"], ["rightStick"], False),
            ("Jump", ["space"], ["buttonSouth"], False),
            ("Cling hold", ["holdIntoWall"], ["leftStickHold"], True),
            ("Slide", ["leftCtrl", "c"], ["buttonEast"], False),
            ("Air dash", ["q", "leftAlt"], ["rightShoulder"], False),
            ("Punch / tag", ["mouseLeft", "e"], ["buttonWest"], False),
        ]
    else:
        rows = []
    ratios = []
    y = 156
    row_h = 108
    step = 116
    d = ImageDraw.Draw(img)
    if bottom:
        plain = [
            ("Gamepad look accel  0.00", "Left / Right"),
            ("P1 confirm", "Auto"),
            ("P2 confirm", "Auto"),
            ("P3 confirm", "Auto"),
            ("P4 confirm", "Auto"),
            ("Reset bindings", "Back to the defaults. Jump is Space."),
            ("Back", ""),
        ]
        for i, (title, sub) in enumerate(plain):
            ratios.append(button(img, (120, y, 1760, y + row_h), title, sub, i == 0, right=24))
            y += step
    for i, (title, keys, pads, cling) in enumerate(rows):
        tokens = list(keys) + list(pads)
        reserve = 20
        for token in tokens:
            reserve += mark_width(token) + 8
        sub = bind_line(keys, pads, cling)
        ratios.append(button(img, (120, y, 1760, y + row_h), title, sub, i == 0, right=reserve))
        gx = 1740
        for token in reversed(tokens):
            w = mark_width(token)
            gx -= w
            draw_token(d, token, gx, y + 30)
            gx -= 8
        y += step
    ratios.append(contrast(CREAM, X_BLUE))
    ratios.append(contrast((20, 20, 26), Y_GOLD))
    ratios.append(contrast(KEY_INK, KEY_CAP))
    ratios.append(contrast(CREAM, B_RED))
    track_top = 168
    track_bot = 156 + 6 * step + row_h
    d.rounded_rectangle((1784, track_top, 1798, track_bot), 4, fill=(0, 0, 0, 140))
    thumb = int((track_bot - track_top) * 7 / 26)
    if thumb < 56:
        thumb = 56
    if bottom:
        d.rounded_rectangle((1786, track_bot - thumb, 1796, track_bot - 2), 3, fill=GOLD)
    else:
        d.rounded_rectangle((1786, track_top + 2, 1796, track_top + thumb), 3, fill=GOLD)
    ratios.append(contrast(GOLD, NAVY))
    footer_both(img)
    return img, min(ratios)


_BUST = None


def bust_mask():
    # Front projection of hier-idle-0, costume mats only. Same outline as MenuIcons.HierBust.
    global _BUST
    if _BUST is not None:
        return _BUST
    import struct
    path = os.path.join(ROOT, "Docs", "UiStills", "hier-idle-0.tris")
    xyzs = []
    with open(path, "rb") as f:
        magic, n = struct.unpack("<ii", f.read(8))
        if magic != 0x52454948:
            raise SystemExit("hier idle")
        for _ in range(n):
            mat = struct.unpack("<B", f.read(1))[0]
            xyz = struct.unpack("<9f", f.read(36))
            if mat < 2:
                xyzs.append(xyz)
    xs = [xyz[k * 3] for xyz in xyzs for k in range(3)]
    ys = [xyz[k * 3 + 1] for xyz in xyzs for k in range(3)]
    xmin, xmax = min(xs), max(xs)
    ymin, ymax = min(ys), max(ys)
    mw, mh = 96, 192
    mask = Image.new("L", (mw, mh), 0)
    pix = mask.load()

    def mx(v):
        return int((v - xmin) / (xmax - xmin) * (mw - 1))

    def my(v):
        return int((ymax - v) / (ymax - ymin) * (mh - 1))

    def fill_tri(p0, p1, p2):
        minx = max(min(p0[0], p1[0], p2[0]), 0)
        maxx = min(max(p0[0], p1[0], p2[0]), mw - 1)
        miny = max(min(p0[1], p1[1], p2[1]), 0)
        maxy = min(max(p0[1], p1[1], p2[1]), mh - 1)
        x0, y0 = p0
        x1, y1 = p1
        x2, y2 = p2
        den = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
        if abs(den) < 1e-8:
            return
        for y in range(miny, maxy + 1):
            for x in range(minx, maxx + 1):
                a = ((y1 - y2) * (x - x2) + (x2 - x1) * (y - y2)) / den
                b = ((y2 - y0) * (x - x2) + (x0 - x2) * (y - y2)) / den
                c = 1 - a - b
                if a >= -0.02 and b >= -0.02 and c >= -0.02:
                    pix[x, y] = 255

    for xyz in xyzs:
        fill_tri(*[(mx(xyz[k * 3]), my(xyz[k * 3 + 1])) for k in range(3)])
    _BUST = mask
    return mask


def draw_bust(base, x, y, w, h, color):
    mask = bust_mask()
    mw, mh = mask.size
    scale = min(w / mw, h / mh)
    dw, dh = max(1, int(mw * scale)), max(1, int(mh * scale))
    sized = mask.resize((dw, dh), Image.Resampling.NEAREST)
    sprite = Image.new("RGBA", (dw, dh), color + (0,))
    sprite.putalpha(sized)
    ox = x + (w - dw) // 2
    oy = y + (h - dh) // 2
    base.alpha_composite(sprite, (ox, oy))


def card_y(top, card_h, runtime_y, runtime_h=420.0):
    return top + int(runtime_y / runtime_h * card_h)


def draw_cup(d, x, y, s):
    # MenuWidgets.EmptyMark: rim, bowl, two handles, stem, base.
    rounded(d, (x + int(s * 0.22), y + int(s * 0.10), x + int(s * 0.78), y + int(s * 0.48)), int(s * 0.08), CREAM)
    rounded(d, (x + int(s * 0.14), y, x + int(s * 0.86), y + int(s * 0.16)), int(s * 0.05), GOLD)
    width = max(8, int(s * 0.07))
    d.arc((x, y + int(s * 0.04), x + int(s * 0.36), y + int(s * 0.52)), 80, 280, fill=GOLD, width=width)
    d.arc((x + int(s * 0.64), y + int(s * 0.04), x + s, y + int(s * 0.52)), 260, 100, fill=GOLD, width=width)
    rounded(d, (x + int(s * 0.44), y + int(s * 0.46), x + int(s * 0.56), y + int(s * 0.74)), 4, GOLD)
    rounded(d, (x + int(s * 0.16), y + int(s * 0.72), x + int(s * 0.84), y + int(s * 0.92)), 8, GOLD)


def draw_join_glyph(d, kind, x, y):
    s = 64
    if kind == "space":
        rounded(d, (x, y + 8, x + s + 28, y + s - 8), 10, CREAM, INK, 3)
        d.text((x + 10, y + 18), "space", font=font(FONT_B, 22), fill=INK)
    else:
        d.ellipse((x, y, x + s, y + s), fill=(30, 170, 70))
        d.text((x + 18, y + 10), "A", font=font(FONT_D, 40), fill=CREAM)


def drop_in():
    img = screen(0.5)
    header(img, "Who's playing", "Everyone Ready? Press Start")
    # Joined seats: keyboard shows Space / Enter, a pad shows A. Both show Y Ready.
    cards = [
        (True, 0, True, "P1", True),
        (True, 1, False, "P2", False),
        (False, 2, False, "", False),
        (False, 3, False, "", False),
    ]
    ratios = []
    top, card_h, card_w = 200, 700, 420
    for i, (human, seat, ready, profile, device) in enumerate(cards):
        x = 80 + i * 450
        fill = mix(INK, SEAT[seat], 0.4)
        box = (x, top, x + card_w, top + card_h)
        shadow(img, box)
        d = ImageDraw.Draw(img)
        selected = i == 0
        rounded(d, box, 22, fill, GOLD if selected else STROKE, 6 if selected else 3)
        d.rounded_rectangle((x + 18, card_y(top, card_h, 19), x + 30, card_y(top, card_h, 412)), 4, fill=SEAT[seat])
        d.text((x + 48, card_y(top, card_h, 18)), "P%d" % (seat + 1), font=font(FONT_D, 44), fill=CREAM)
        if human:
            bust_h = int(card_h * 0.40)
            bust_w = int(bust_h * 0.72)
            if bust_w > int(card_w * 0.78):
                bust_w = int(card_w * 0.78)
            draw_bust(img, x + (card_w - bust_w) // 2, card_y(top, card_h, 62), bust_w, bust_h, SEAT[seat])
            d = ImageDraw.Draw(img)
            name = "<  %s  >" % profile
            nf = font(FONT_B, 32)
            nw = d.textlength(name, font=nf)
            d.text((x + (card_w - nw) / 2, card_y(top, card_h, 236)), name, font=nf, fill=CREAM)
            hint = "Y  Ready"
            hf = font(FONT_B, 24)
            hw = d.textlength(hint, font=hf)
            d.text((x + (card_w - hw) / 2, card_y(top, card_h, 298)), hint, font=hf, fill=MUTE)
            word = "Keyboard" if device else "Gamepad"
            df = font(FONT_B, 26)
            dw = d.textlength(word, font=df)
            group = 40 + 8 + dw
            gx = x + (card_w - group) / 2
            gy = card_y(top, card_h, 328)
            draw_glyph(d, "keys" if device else "pad", int(gx), gy)
            d.text((gx + 44, gy + 6), word, font=df, fill=CREAM)
            chip_h = int(40 / 420.0 * card_h)
            chip_w = 176
            chip_y = card_y(top, card_h, 366)
            chip = (x + (card_w - chip_w) // 2, chip_y, x + (card_w + chip_w) // 2, chip_y + chip_h)
            rounded(d, chip, 12, GOLD if ready else NAVY, INK, 2)
            word = "Ready" if ready else "Joined"
            ink = INK if ready else CREAM
            wf = font(FONT_D, 32)
            ww = d.textlength(word, font=wf)
            d.text((x + (card_w - ww) / 2, chip_y + 12), word, font=wf, fill=ink)
            ratios.append(contrast(ink, GOLD if ready else NAVY))
        else:
            d.text((x + 48, card_y(top, card_h, 64)), "Press Space or A to join", font=font(FONT_B, 26), fill=MUTE)
            gy = card_y(top, card_h, 193)
            draw_join_glyph(d, "space", x + 100, gy)
            draw_join_glyph(d, "a", x + 240, gy)
        ratios.append(contrast(CREAM, fill))
        ratios.append(contrast(MUTE, fill))
    ratios.append(contrast(GOLD, NAVY))
    footer_both(img)
    return img, min(ratios)


def credits():
    img = screen(0.5)
    header(img, "Credits", "")
    plate(img, (120, 180, 1800, 900))
    text = (
        "TAG\n"
        "A couch tag game for one keyboard and up to four pads.\n\n"
        "Team\n"
        "Couch Co-op. This build is local tag.\n\n"
        "Type\n"
        "Headings are Bangers. Body letters are Liberation Sans Bold.\n"
        "Bangers copyright 2010 The Bangers Project Authors.\n"
        "Liberation Sans copyright 2010-2012 Red Hat, Inc.\n"
        "SIL Open Font License, Version 1.1.\n"
        "The license files sit next to the fonts. Nothing paid.\n\n"
        "Tools\n"
        "Unity, the input system already in the project, and the audio bus.\n"
        "Menu sounds are clips that were already here.\n"
        "One-shots under Assets/Audio are original synthesis.\n\n"
        "Space still jumps. Online play is not in this build."
    )
    d = ImageDraw.Draw(img)
    y = 200
    for line in text.split("\n"):
        face = font(FONT_D, 36) if line in ("TAG", "Team", "Type", "Tools") else font(FONT_B, 28)
        fill = GOLD if line in ("TAG", "Team", "Type", "Tools") else CREAM
        d.text((160, y), line, font=face, fill=fill)
        y += 36 if line else 16
    button(img, (720, 920, 1200, 1010), "Back", "", True)
    footer_both(img)
    return img, contrast(CREAM, (8, 22, 58))


def records_empty():
    img = screen(0.5)
    header(img, "Records", "Matches, wins, tags, and longest time not It")
    d = ImageDraw.Draw(img)
    box = (360, 200, 1560, 500)
    shadow(img, box)
    rounded(d, box, 22, NAVY, GOLD, 4)
    draw_cup(d, 400, 230, 220)
    d.text((680, 280), "No records yet.", font=font(FONT_D, 40), fill=CREAM)
    d.text((680, 340), "Play a match to set one.", font=font(FONT_B, 30), fill=MUTE)
    ratios = [
        contrast(CREAM, NAVY),
        contrast(MUTE, NAVY),
        button(img, (360, 524, 1560, 632), "Back", "", False),
    ]
    footer_both(img)
    return img, min(ratios)


def records_sample():
    img = screen(0.5)
    header(img, "Records", "Sample data. Not a saved profile.")
    d = ImageDraw.Draw(img)
    rounded(d, (360, 158, 530, 198), 8, GOLD, INK, 2)
    d.text((378, 160), "SAMPLE", font=font(FONT_D, 28), fill=INK)
    rows = [
        ("Sample  Red", "matches 12    wins 4    tags 9    live 18.4 s", True),
        ("Sample  Blue", "matches 9    wins 2    tags 7    live 11.0 s", False),
        ("Sample  Yellow", "matches 6    wins 1    tags 3    live 8.5 s", False),
        ("Back", "", False),
    ]
    ratios = [contrast(INK, GOLD)]
    y = 200
    for title, detail, hot in rows:
        ratios.append(button(img, (360, y, 1560, y + 120), title, detail, hot))
        y += 128
    footer_both(img)
    return img, min(ratios)


def bars(img):
    d = ImageDraw.Draw(img)
    for i, color in enumerate((GOLD, CREAM, HOT)):
        x = 280 + i * 220
        d.polygon([(x, -40), (x + 180, -40), (x + 40, H + 40), (x - 140, H + 40)], fill=color + (230,))
    return img


def wipe():
    img, ratio = mode_rules(False)
    return bars(img), ratio


def wipe_load():
    img, ratio = loading()
    return bars(img), ratio


def wipe_results():
    img, ratio = results()
    return bars(img), ratio


def text_inside(draw, xy, text, face, box, slop=3):
    bbox = draw.textbbox(xy, text, font=face)
    x0, y0, x1, y1 = box
    ok = bbox[0] >= x0 - slop and bbox[1] >= y0 - slop and bbox[2] <= x1 + slop and bbox[3] <= y1 + slop
    return ok, bbox


def menu_text(scale):
    """Accessibility rows at a text size. The type stays inside each button."""
    img = screen(0.5)
    header(img, "Accessibility", "Text size  %.2f" % scale)
    title_px = int(round(40 * scale))
    sub_px = int(round(30 * scale))
    row_h = 24 + title_px + 6 + sub_px + 12
    if row_h < 108:
        row_h = 108
    step = row_h + 8
    rows = [
        ("Reduce motion  Off", "Menu slides and the title pulse only", True),
        ("Text size  %.2f" % scale, "0.85, 1.00, 1.25, 1.50", False),
        ("Comic words  On", "Verb words during a match.", False),
    ]
    y = 168
    ratios = []
    d = ImageDraw.Draw(img)
    for title, sub, hot in rows:
        box = (280, y, 1640, y + row_h)
        shadow(img, box)
        d = ImageDraw.Draw(img)
        fill = HOT if hot else PANEL
        rounded(d, box, 22, fill, GOLD if hot else STROKE, 6 if hot else 3)
        d.rectangle((296, y + 8, 1624, y + 16), fill=GOLD if hot else (255, 255, 255, 70))
        title_c = INK if hot else CREAM
        sub_c = INK if hot else MUTE
        tf = font(FONT_D, title_px)
        sf = font(FONT_B, sub_px)
        title_xy = (308, y + 24)
        sub_xy = (308, y + 24 + title_px + 6)
        d.text(title_xy, title, font=tf, fill=title_c)
        d.text(sub_xy, sub, font=sf, fill=sub_c)
        well = (292, y + 18, 1628, y + row_h - 6)
        ok_t, tb = text_inside(d, title_xy, title, tf, well)
        ok_s, sb = text_inside(d, sub_xy, sub, sf, well)
        if not ok_t or not ok_s:
            raise SystemExit("menu clip scale=%s title=%s sub=%s well=%s" % (scale, tb, sb, well))
        ratios.append(contrast(title_c, fill))
        ratios.append(contrast(sub_c, fill))
        y += step
    ratios.append(contrast(GOLD, NAVY))
    footer_both(img)
    return img, min(ratios)


def match_hud(scale):
    """Two-player match HUD. Plates grow with the text size so nothing clips."""
    img = screen(0.28)
    d = ImageDraw.Draw(img)
    name_px = int(round(36 * scale)) if scale < 1.2 else int(round(36 * scale))
    if abs(scale - 1.0) < 0.01:
        name_px, floor_px, call_px, clock_px = 36, 30, 68, 46
        name_h, badge_h, clock_h, clock_w, chip_w, chip_h = 132, 52, 70, 260, 52, 72
    else:
        floor_px = int(round(30 * scale))
        call_px = int(round(68 * scale))
        clock_px = int(round(46 * scale))
        name_px = int(round(36 * scale))
        name_h = max(name_px / 0.34, floor_px / 0.32, floor_px / 0.38) + 4
        badge_h = max(52, floor_px + 8)
        clock_h = max(70, clock_px / 0.84 + 8)
        clock_w = min(420, max(260, clock_px * 3.6))
        chip_w = max(52, floor_px * 3.0)
        chip_h = (floor_px + 8) / 0.42
    # Seam and a quiet plate per half.
    d.rectangle((W // 2 - 2, 0, W // 2 + 2, H), fill=GOLD)
    seats = ((0, SEAT[0], "P1", "Keyboard", True), (W // 2, SEAT[1], "P2", "Pad", False))
    ratios = [contrast(GOLD, NAVY)]
    nf = font(FONT_D, name_px)
    ff = font(FONT_B, floor_px)
    for origin, seat, name, profile, it in seats:
        plate = (origin + 28, 28, origin + 28 + 112 + 16 + 460, 28 + int(name_h))
        # Identity sits in the corner. The badge is the seat, the name is beside it.
        badge = (origin + 36, 36, origin + 36 + 112, 36 + int(badge_h))
        rounded(d, badge, 10, GOLD if it else NAVY, INK, 2)
        it_word = "IT"
        itf = font(FONT_D, floor_px)
        tw = d.textlength(it_word, font=itf)
        it_xy = (badge[0] + (112 - tw) / 2, badge[1] + (badge_h - floor_px) / 2 - 2)
        d.text(it_xy, it_word, font=itf, fill=INK if it else CREAM)
        ok, bb = text_inside(d, it_xy, it_word, itf, (badge[0] + 4, badge[1] + 2, badge[2] - 4, badge[3] - 2))
        if not ok:
            raise SystemExit("badge clip %s %s" % (scale, bb))
        ratios.append(contrast(INK if it else CREAM, GOLD if it else NAVY))
        nx = badge[2] + 16
        ny = 36
        d.text((nx, ny), name, font=nf, fill=CREAM)
        ok, bb = text_inside(d, (nx, ny), name, nf, (nx, ny, nx + 420, ny + name_px + 4))
        if not ok:
            raise SystemExit("name clip %s %s" % (scale, bb))
        mid = ny + int(name_h * 0.34)
        d.text((nx, mid), profile, font=ff, fill=MUTE)
        d.text((nx + 220, mid), "TAGS  1" if it else "TAGS  0", font=ff, fill=MUTE)
        low = ny + int(name_h * 0.66)
        d.text((nx, low), "8.5s as It" if it else "Live", font=ff, fill=GOLD)
        for word, xy in (
            (profile, (nx, mid)),
            ("TAGS  1" if it else "TAGS  0", (nx + 220, mid)),
            ("8.5s as It" if it else "Live", (nx, low)),
        ):
            ok, bb = text_inside(d, xy, word, ff, (origin + 28, 28, origin + W // 2 - 24, 36 + int(name_h)))
            if not ok:
                raise SystemExit("hud line clip %s %s %s" % (scale, word, bb))
        ratios.append(contrast(CREAM, (8, 22, 58)))
        # Verb words along the bottom. The chip is wide enough for the word.
        verbs = ("GO", "IT")
        base_y = H - 36 - int(chip_h)
        for i, word in enumerate(verbs):
            cx = origin + 36 + i * (int(chip_w) + 8)
            chip = (cx, base_y, cx + int(chip_w), base_y + int(chip_h))
            rounded(d, chip, 12, NAVY, STROKE, 2)
            vf = font(FONT_B, floor_px)
            vw = d.textlength(word, font=vf)
            word_top = chip[3] - int(chip_h * 0.42)
            vxy = (cx + (chip_w - vw) / 2, word_top + (chip_h * 0.42 - floor_px) / 2)
            d.text(vxy, word, font=vf, fill=CREAM)
            band = (chip[0] + 2, word_top, chip[2] - 2, chip[3] - 2)
            ok, bb = text_inside(d, vxy, word, vf, band)
            if not ok:
                raise SystemExit("verb clip %s %s %s band %s" % (scale, word, bb, band))
            ratios.append(contrast(CREAM, NAVY))
        if it:
            cf = font(FONT_D, call_px)
            call = "YOU'RE IT!"
            cw = d.textlength(call, font=cf)
            band_l = origin + 80
            band_r = origin + W // 2 - 80
            band_t = 360
            band_b = 360 + call_px + 16
            if cw <= (band_r - band_l):
                cxy = (band_l + (band_r - band_l - cw) / 2, band_t)
                d.text(cxy, call, font=cf, fill=GOLD)
                ok, bb = text_inside(d, cxy, call, cf, (band_l, band_t, band_r, band_b))
            else:
                cxy = (band_l, band_t)
                d.text(cxy, call, font=cf, fill=GOLD)
                ok, bb = text_inside(d, cxy, call, cf, (band_l, band_t, band_r, band_t + call_px + 8))
            if not ok:
                raise SystemExit("call clip %s %s" % (scale, bb))
            ratios.append(contrast(GOLD, (8, 22, 58)))
    # Clock on the seam.
    cx0 = (W - int(clock_w)) / 2
    clock = (cx0, 28, cx0 + clock_w, 28 + clock_h)
    rounded(d, clock, 12, INK, GOLD, 3)
    clf = font(FONT_D, clock_px)
    clock_word = "1:30"
    ctw = d.textlength(clock_word, font=clf)
    # The clock text uses the left 62% of the plate, same as the runtime anchors.
    text_r = clock[0] + clock_w * 0.62
    cxy = (clock[0] + 12, clock[1] + (clock_h - clock_px) / 2 - 2)
    if cxy[0] + ctw > text_r:
        cxy = (text_r - ctw - 4, cxy[1])
    d.text(cxy, clock_word, font=clf, fill=CREAM)
    ok, bb = text_inside(d, cxy, clock_word, clf, (clock[0] + 8, clock[1] + 4, text_r, clock[3] - 4))
    if not ok:
        raise SystemExit("clock clip %s %s plate %s" % (scale, bb, clock))
    round_word = "1/3"
    rf = font(FONT_B, floor_px)
    rxy = (text_r + 8, clock[1] + (clock_h - floor_px) / 2)
    d.text(rxy, round_word, font=rf, fill=GOLD)
    ok, bb = text_inside(d, rxy, round_word, rf, (text_r, clock[1] + 4, clock[2] - 8, clock[3] - 4))
    if not ok:
        raise SystemExit("round clip %s %s" % (scale, bb))
    ratios.append(contrast(CREAM, INK))
    ratios.append(contrast(GOLD, INK))
    return img, min(ratios)


def type_px(base, scale):
    if abs(scale - 1.0) < 0.02:
        return base
    return int(round(base * scale))


def wrap_words(d, text, face, width):
    if not text:
        return []
    words = text.split()
    lines = []
    cur = ""
    for word in words:
        trial = word if not cur else cur + " " + word
        if d.textlength(trial, font=face) <= width or not cur:
            cur = trial
        else:
            lines.append(cur)
            cur = word
    if cur:
        lines.append(cur)
    return lines


def shrink_into(d, title, lines, title_px, sub_px, width, height):
    while title_px > 16 or sub_px > 14:
        tf = font(FONT_D, title_px)
        sf = font(FONT_B, sub_px)
        title_lines = wrap_words(d, title, tf, width)
        need = len(title_lines) * (title_px + 2) + 4
        wide = False
        for line in title_lines:
            if d.textlength(line, font=tf) > width + 1:
                wide = True
        for line in lines:
            if not line:
                continue
            need += sub_px + 2
            if d.textlength(line, font=sf) > width + 1:
                wide = True
        if need <= height and not wide:
            return title_px, sub_px
        if title_px > 16:
            title_px -= 2
        if sub_px > 14:
            sub_px -= 2
    return max(16, title_px), max(14, sub_px)


def paint_lines(img, box, title, lines, scale, hot, grown):
    """Grown rows keep TextPx. The before pane shrinks into the old height."""
    x0, y0, x1, y1 = box
    shadow(img, box)
    d = ImageDraw.Draw(img)
    fill = HOT if hot else PANEL
    rounded(d, box, 18, fill, GOLD if hot else STROKE, 5 if hot else 3)
    d.rectangle((x0 + 12, y0 + 6, x1 - 12, y0 + 12), fill=GOLD if hot else (255, 255, 255, 70))
    title_c = INK if hot else CREAM
    sub_c = INK if hot else MUTE
    title_px = type_px(40, scale)
    sub_px = type_px(30, scale)
    left = x0 + 18
    right = x1 - 14
    width = right - left
    height = (y1 - 10) - (y0 + 16)
    if not grown:
        title_px, sub_px = shrink_into(d, title, lines, title_px, sub_px, width, height)
    tf = font(FONT_D, title_px)
    sf = font(FONT_B, sub_px)
    title_lines = wrap_words(d, title, tf, width)
    yy = y0 + 18
    well = (left - 2, y0 + 14, right + 2, y1 - 6)
    for line in title_lines:
        d.text((left, yy), line, font=tf, fill=title_c)
        ok, bb = text_inside(d, (left, yy), line, tf, well)
        if grown and not ok:
            raise SystemExit("row title clip %s %s well %s" % (line, bb, well))
        yy += title_px + 2
    for line in lines:
        if not line:
            continue
        d.text((left, yy), line, font=sf, fill=sub_c)
        ok, bb = text_inside(d, (left, yy), line, sf, well)
        if grown and not ok:
            raise SystemExit("row sub clip %s %s well %s" % (line, bb, well))
        yy += sub_px + 2
    return contrast(title_c, fill), title_px, sub_px


def board_main(scale, grown):
    img = screen(0.45)
    header(img, "Menu", "Text size  %.2f" % scale)
    plate(img, (70, 168, 920, 900), (8, 28, 70, 230))
    d = ImageDraw.Draw(img)
    d.text((96, 190), "Local couch. One keyboard, four pads.", font=font(FONT_B, type_px(30, scale) if grown else 30), fill=CREAM)
    rows = [
        ("Play", ["Local couch"], True),
        ("Practice", ["Free run any arena, no tagger"], False),
        ("Options", ["Sound, picture, access"], False),
        ("Controls", ["Binds. Space still jumps."], False),
    ]
    title_px = type_px(40, scale)
    sub_px = type_px(30, scale)
    h = 96 if not grown else max(96, 24 + title_px + 6 + sub_px + 12)
    step = 108 if not grown else h + 12
    y = 168
    ratios = []
    locked = []
    for title, lines, hot in rows:
        ratio, tp, sp = paint_lines(img, (980, y, 1800, y + h), title, lines, scale, hot, grown)
        ratios.append(ratio)
        locked.append((tp, sp))
        y += step
    bw = (1800 - 980 - 24) // 3
    x = 980
    for title, lines in (("Credits", []), ("Records", ["Profiles"]), ("Quit", [])):
        ratio, tp, sp = paint_lines(img, (x, y, x + bw, y + h), title, lines, scale, False, grown)
        ratios.append(ratio)
        locked.append((tp, sp))
        x += bw + 12
    return img, min(ratios), locked


def board_cast(scale, grown):
    img = screen(0.5)
    header(img, "Characters", "Text size  %.2f" % scale)
    name_px = type_px(30, scale)
    name_h = 78 if not grown else max(78, 12 + name_px * 2 + 8)
    status_h = 52 if not grown else max(52, 16 + name_px)
    card_w = 420
    card_h = 400 if not grown else 430
    ratios = []
    y0 = 168
    for s in range(4):
        x = 48 + s * (card_w + 16)
        d = ImageDraw.Draw(img)
        rounded(d, (x, y0, x + card_w, y0 + card_h), 18, SEAT[s], STROKE, 3)
        rounded(d, (x + 36, y0 + 16, x + card_w - 36, y0 + card_h - name_h - status_h - 24), 12, NAVY)
        name_box = (x + 12, y0 + card_h - name_h - status_h - 8, x + card_w - 12, y0 + card_h - status_h - 8)
        stat_box = (x + 12, name_box[3], x + card_w - 12, name_box[3] + status_h)
        rounded(d, name_box, 8, NAVY)
        rounded(d, stat_box, 8, NAVY)
        face_px = name_px if grown else 28
        nf = font(FONT_B, face_px)
        d.text((name_box[0] + 12, name_box[1] + 6), "P%d" % (s + 1), font=nf, fill=CREAM)
        d.text((name_box[0] + 12, name_box[1] + 8 + face_px), "Red / Red", font=nf, fill=CREAM)
        d.text((stat_box[0] + 12, stat_box[1] + 8), "Hat off    Not ready", font=nf, fill=CREAM)
        if grown:
            for word, xy, box in (
                ("P%d" % (s + 1), (name_box[0] + 12, name_box[1] + 6), name_box),
                ("Red / Red", (name_box[0] + 12, name_box[1] + 8 + face_px), name_box),
                ("Hat off    Not ready", (stat_box[0] + 12, stat_box[1] + 8), stat_box),
            ):
                ok, bb = text_inside(d, xy, word, nf, (box[0] + 4, box[1] + 2, box[2] - 4, box[3] - 2))
                if not ok:
                    raise SystemExit("cast clip %s %s" % (word, bb))
        ratios.append(contrast(CREAM, NAVY))
    title_px = type_px(40, scale)
    sub_px = type_px(30, scale)
    grid_h = 100 if not grown else max(100, 24 + title_px + 6 + sub_px + 12)
    grid_step = 108 if not grown else grid_h + 8
    names = ["RED", "BLUE", "ORANGE", "LAVENDER", "TAN", "MINT"]
    grid_y = y0 + card_h + 16
    locked = []
    col_w = 280
    x0 = (W - (col_w * 3 + 16)) // 2
    for i, name in enumerate(names):
        col, row = i % 3, i // 3
        x = x0 + col * (col_w + 8)
        y = grid_y + row * grid_step
        ratio, tp, sp = paint_lines(img, (x, y, x + col_w, y + grid_h), name, [" "], scale, i == 0, grown)
        ratios.append(ratio)
        locked.append((tp, sp))
    return img, min(ratios), locked


def board_rules(scale, grown):
    img = screen(0.5)
    header(img, "Mode and rules", "Text size  %.2f" % scale)
    title_px = type_px(40, scale)
    sub_px = type_px(30, scale)
    mode_h = 152 if not grown else max(152, 24 + title_px + 6 + sub_px * 2 + 12)
    mode_step = 168 if not grown else mode_h + 16
    rule_h = 80 if not grown else max(80, 24 + title_px + 6 + sub_px + 12)
    rule_step = 84 if not grown else rule_h + 8
    modes = [
        ("Hot Potato", ["First to 2.", "Fuse 45 / 40 / 35s."], False),
        ("Least It", ["Least time as It.", "Selected"], True),
        ("Trail Tag", ["Ribbons eliminate.", "Last standing."], False),
        ("Free play", ["Punch transfers It.", "No timer."], False),
    ]
    ratios = []
    locked = []
    for i, (name, lines, hot) in enumerate(modes):
        col, row = i % 2, i // 2
        x = 64 + col * 400
        y = 168 + row * mode_step
        ratio, tp, sp = paint_lines(img, (x, y, x + 380, y + mode_h), name, lines, scale, hot, grown)
        ratios.append(ratio)
        locked.append((tp, sp))
    rules = [
        ("Round length", ["120 s"]),
        ("Rounds", ["1"]),
        ("Win target", ["2"]),
        ("Starting It", ["Random"]),
    ]
    for i, (title, lines) in enumerate(rules):
        y = 168 + i * rule_step
        ratio, tp, sp = paint_lines(img, (900, y, 1800, y + rule_h), title, lines, scale, False, grown)
        ratios.append(ratio)
        locked.append((tp, sp))
    return img, min(ratios), locked


def board_results(scale, grown):
    img = screen(0.85)
    header(img, "RESULTS", "Least It  ·  Red / Tan")
    title_px = type_px(40, scale)
    sub_px = type_px(30, scale)
    rank_h = 180 if not grown else max(180, 24 + title_px + 6 + sub_px * 3 + 12)
    btn_h = 128 if not grown else max(128, 24 + title_px + 6 + sub_px + 12)
    places = [
        ("2nd  P2", ["5 tags", "14.7s as It", "1 round win"], 1),
        ("1st  P1", ["WIN  6 tags", "8.5s as It", "2 round wins"], 0),
        ("3rd  P3", ["4 tags", "20.9s as It", "0 round wins"], 2),
        ("4th  P4", ["3 tags", "27.1s as It", "0 round wins"], 3),
    ]
    rank_w = 430
    x0 = 70
    rank_y = 200
    ratios = []
    locked = []
    for i, (title, lines, seat) in enumerate(places):
        x = x0 + i * (rank_w + 16)
        ratio, tp, sp = paint_lines(img, (x, rank_y, x + rank_w, rank_y + rank_h), title, lines, scale, i == 1, grown)
        ratios.append(ratio)
        locked.append((tp, sp))
        d = ImageDraw.Draw(img)
        badge, ink = badge_paint(SEAT[seat])
        d.rounded_rectangle((x + rank_w - 108, rank_y + 18, x + rank_w - 22, rank_y + 56), 8, fill=badge)
        d.text((x + rank_w - 88, rank_y + 24), "P%d" % (seat + 1), font=font(FONT_B, 22), fill=ink)
        ratios.append(contrast(ink, badge))
    actions = [("Rematch", ["Same setup"], True), ("Change mode", [], False), ("Character select", [], False), ("Main menu", [], False)]
    btn_y = rank_y + rank_h + 16
    for i, (title, lines, hot) in enumerate(actions):
        x = x0 + i * (rank_w + 16)
        ratio, tp, sp = paint_lines(img, (x, btn_y, x + rank_w, btn_y + btn_h), title, lines, scale, hot, grown)
        ratios.append(ratio)
        locked.append((tp, sp))
    return img, min(ratios), locked


def quad_split(scale, grown):
    boards = [
        ("Main menu", board_main),
        ("Character select", board_cast),
        ("Mode and rules", board_rules),
        ("RESULTS", board_results),
    ]
    full = type_px(40, scale)
    floor = type_px(30, scale)
    shots = []
    worst = 99.0
    for label, fn in boards:
        img, ratio, locked = fn(scale, grown)
        worst = min(worst, ratio)
        if grown:
            for tp, sp in locked:
                if tp != full or sp != floor:
                    raise SystemExit("best-fit shrink %s title %s sub %s" % (label, tp, sp))
        shots.append((label, img))
    canvas = Image.new("RGB", (W, H), (8, 14, 32))
    d = ImageDraw.Draw(canvas)
    note = "Text size 1.50, rows grown" if grown else "Text size 1.50, old row height"
    d.text((24, 6), note, font=font(FONT_B, 22), fill=GOLD)
    pw, ph = W // 2 - 16, H // 2 - 40
    for i, (label, img) in enumerate(shots):
        thumb = img.convert("RGB").resize((pw, ph), Image.Resampling.LANCZOS)
        x = 8 + (i % 2) * (W // 2)
        y = 40 + (i // 2) * (H // 2)
        canvas.paste(thumb, (x, y))
        d.rectangle((x, y, x + 320, y + 28), fill=NAVY)
        d.text((x + 8, y + 3), "%s  1.50" % label, font=font(FONT_B, 20), fill=CREAM)
    return canvas, worst


def main():
    notes = []
    jobs = [
        ("05-mode-rules-composite.png", mode_rules(False)),
        ("05-rules-end-composite.png", mode_rules(True)),
        ("04-arena-mega-composite.png", arena("Mega Park")),
        ("04-arena-pocket-composite.png", arena("Pocket Park")),
        ("04-arena-stack-composite.png", arena("Stack Yard")),
        ("06-results-composite.png", results()),
        ("07-loading-composite.png", loading()),
        ("08-pause-composite.png", pause()),
        ("09-options-composite.png", options("hub")),
        ("09-hub-before-composite.png", options("hub", hub="before")),
        ("09-sound-composite.png", options("sound")),
        ("09-sound-muted-composite.png", options("sound", mute=True)),
        ("09-picture-composite.png", options("picture")),
        ("09-quality-before-composite.png", options("picture", levels=False)),
        ("09-quality-ultra-composite.png", options("picture", quality="Ultra")),
        ("09-access-composite.png", options("access")),
        ("09-access-before-composite.png", options("access", access="strip")),
        ("15-seats-pd-composite.png", options("access", seats="pd")),
        ("15-seats-tritan-composite.png", options("access", seats="tritan")),
        ("15-rows-before-composite.png", quad_split(1.5, False)),
        ("15-rows-after-composite.png", quad_split(1.5, True)),
        ("09-menu-text-100-composite.png", menu_text(1.0)),
        ("09-menu-text-150-composite.png", menu_text(1.5)),
        ("03-hud-text-100-composite.png", match_hud(1.0)),
        ("03-hud-text-150-composite.png", match_hud(1.5)),
        ("10-controls-composite.png", controls(False)),
        ("10-controls-bottom-composite.png", controls(True)),
        ("11-drop-in-composite.png", drop_in()),
        ("12-credits-composite.png", credits()),
        ("14-records-composite.png", records_empty()),
        ("14-records-sample-composite.png", records_sample()),
        ("16-trans-rules-composite.png", wipe()),
        ("16-trans-load-composite.png", wipe_load()),
        ("16-trans-results-composite.png", wipe_results()),
    ]
    worst = 99
    for name, (img, ratio) in jobs:
        save(img, name, notes)
        worst = min(worst, ratio)
        print(f"  contrast {ratio:.2f}")
    print("min contrast", round(worst, 2))


if __name__ == "__main__":
    main()
