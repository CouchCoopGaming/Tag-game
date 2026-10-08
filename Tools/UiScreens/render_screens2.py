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
OUT = os.path.join(ROOT, "Docs", "UiStills", "screens2", "pass5")
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


def button(base, box, title, sub, hot, bar=None, right=24):
    x0, y0, x1, y1 = box
    shadow(base, box)
    d = ImageDraw.Draw(base)
    fill = HOT if hot else PANEL
    rounded(d, box, 22, fill, GOLD if hot else STROKE, 6 if hot else 3)
    d.rectangle((x0 + 16, y0 + 10, x1 - 16, y0 + 18), fill=GOLD if hot else (255, 255, 255, 70))
    if bar is not None:
        d.rounded_rectangle((x0 + 14, y0 + 28, x0 + 26, y1 - 14), 4, fill=bar if not hot else INK)
    title_c = INK if hot else CREAM
    sub_c = INK if hot else MUTE
    left = 40 if bar is not None else 28
    tw = x1 - x0 - left - right
    if tw < 80:
        tw = 80
    box_h = y1 - y0
    sub_size = 22 if box_h < 108 else 24
    title_top = 12 if box_h < 108 else 18
    title_max = 32 if sub and box_h < 112 else 40
    tf = fit_text(d, title, FONT_D, tw, title_max, 22, title_c)
    d.text((x0 + left, y0 + title_top), title, font=tf, fill=title_c)
    if sub:
        sf = font(FONT_B, sub_size)
        yy = y0 + title_top + int(tf.size * 0.78) + 4
        lines = wrap(d, sub, sf, tw)
        for line in lines[:2]:
            if yy + sub_size > y1 - 6:
                break
            d.text((x0 + left, yy), line, font=sf, fill=sub_c)
            yy += sub_size + 2
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
        bx0 = 300
        rounded(d, (bx0, 94, bx0 + tw + pad * 2, 132), 10, NAVY)
        d.text((bx0 + pad, 96), banner, font=bf, fill=CREAM)
    d.rectangle((64, 148, W - 64, 154), fill=GOLD)


def footer(img, words):
    d = ImageDraw.Draw(img)
    labels = words
    glyphs = ["arrows", "space", "esc"]
    total = 3 * 280 + 2 * 24
    x = (W - total) // 2
    y = H - 92
    for i, (g, word) in enumerate(zip(glyphs, labels)):
        box = (x, y, x + 280, y + 64)
        rounded(d, box, 14, (20, 46, 92), GOLD, 2)
        draw_glyph(d, g, x + 16, y + 10)
        d.text((x + 78, y + 16), word, font=font(FONT_B, 26), fill=CREAM)
        x += 304


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


def footer_both(img):
    footer(img, ["Move", "Space  confirm", "Esc  back"])


def save(img, name, notes):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    rgb = img.convert("RGB")
    rgb.save(path, "PNG", optimize=True, compress_level=9)
    size = os.path.getsize(path)
    if size > 390000:
        for colors in (256, 224, 192, 160):
            q = rgb.quantize(colors=colors, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE)
            q.save(path, "PNG", optimize=True)
            size = os.path.getsize(path)
            if size <= 390000:
                break
    notes.append((name, size, rgb.size))
    print(f"{name} {size} {rgb.size}")


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
    pad = 8
    return im.crop((max(0, minx - pad), max(0, miny - pad), min(w, maxx + pad), min(h, maxy + pad)))


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
    floor = 528
    # Winner stands higher and larger. The block is a step, not a label.
    step_h = [36, 72, 22, 12]
    fig_box = [(200, 260), (250, 320), (180, 230), (160, 210)]
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
        fx = sx + (rank_w - crop.size[0]) // 2
        fy = step_top - crop.size[1] + 8
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
        ratios.append(button(img, (x, 790, x + rank_w, 900), title, sub, hot))
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
    d.text((540, y + 50), "Jump again to leave the wall.", font=font(FONT_B, 28), fill=INK)
    track_y = y + 124
    track = (400, track_y, 1520, track_y + 36)
    rounded(d, track, 10, (5, 13, 31), STROKE, 2)
    span = 1520 - 400 - 12
    fill_w = int(span * 0.60)
    rounded(d, (406, track_y + 6, 406 + fill_w, track_y + 30), 6, GOLD)
    d.text((760, track_y + 48), "Loading  60%", font=font(FONT_B, 28), fill=CREAM)
    return img, min(contrast(CREAM, (8, 22, 58)), contrast(INK, GOLD), contrast(GOLD, (5, 13, 31)))


def pause():
    img = screen(0.28)
    header(img, "Paused by P1", "COMIC WORDS ON")
    items = [("Resume", "", True), ("Restart round", "Same arena, same rules", False), ("Options", "", False), ("Quit to menu", "", False)]
    ratios = []
    x = 520
    y = 220
    for title, sub, hot in items:
        ratios.append(button(img, (x, y, x + 880, y + 120), title, sub, hot))
        y += 140
    footer_both(img)
    return img, min(ratios)


def options(page):
    img = screen(0.5)
    pages = {
        "hub": ("Options", "Sound, picture, accessibility, controls, look, and credits.", [
            ("Sound", "Music, effects, and UI.", True),
            ("Picture", "Resolution, fullscreen, and scale.", False),
            ("Accessibility", "Motion, text size, and colors.", False),
            ("Controls", "Keyboard and pad. Space jumps.", False),
            ("Look", "One sensitivity for the couch.", False),
            ("Credits", "Team, the font, and the tools.", False),
            ("Back", "Main menu", False),
        ], []),
        "sound": ("Sound", "Sliders step the volumes you already have.", [
            ("Master  0.80", "Left / Right", True),
            ("SFX  1.00", "Left / Right", False),
            ("UI  1.00", "Left / Right", False),
            ("Music  0.35", "Left / Right", False),
            ("Mute  (Comma)", "Left / Right", False),
            ("Back", "", False),
        ], [0.80, 1.0, 1.0, 0.35]),
        "picture": ("Picture", "Resolution, fullscreen, vsync, and the couch UI scale.", [
            ("Resolution  1920 x 1080", "Left / Right", True),
            ("Fullscreen  On", "Left / Right", False),
            ("VSync  On", "Left / Right", False),
            ("Quality  Default", "Left / Right", False),
            ("UI scale  100%", "80% to 130%, for a couch TV", False),
            ("Back", "", False),
        ], []),
        "access": ("Accessibility", "Reduce motion, text size, player colors, and comic words.", [
            ("Reduce motion  Off", "Menu slides and the title pulse only", True),
            ("Text size  1.00", "Menu and HUD text", False),
            ("Player  P1", "Left / Right", False),
            ("Colorblind palette  Default", "Left / Right", False),
            ("Comic words  On", "Verb words during a match.", False),
            ("Back", "", False),
        ], []),
    }
    title, banner, rows, meters = pages[page]
    header(img, title, banner)
    ratios = []
    y = 180
    for i, (name, sub, hot) in enumerate(rows):
        ratios.append(button(img, (280, y, 1640, y + 88), name, sub, hot))
        if i < len(meters):
            d = ImageDraw.Draw(img)
            d.rounded_rectangle((980, y + 52, 1420, y + 68), 4, fill=(0, 0, 0, 90))
            d.rounded_rectangle((980, y + 52, 980 + int(440 * meters[i]), y + 68), 4, fill=GOLD)
        y += 96
    if page == "access":
        d = ImageDraw.Draw(img)
        sw = [(199, 199, 0), (145, 252, 115), (255, 255, 255), (0, 214, 191)]
        for i, c in enumerate(sw):
            x = 560 + i * 180
            rounded(d, (x, y + 8, x + 140, y + 86), 12, c)
            d.text((x + 48, y + 28), "P" + str(i + 1), font=font(FONT_B, 28), fill=INK)
            ratios.append(contrast(INK, c))
    footer_both(img)
    return img, min(ratios)


def controls(bottom=False):
    img = screen(0.5)
    header(img, "Controls", "Keyboard and pad glyphs. Confirm changes one. Space still jumps.")
    if not bottom:
        rows = [
            ("Move", "WASD    /    Left stick"),
            ("Look", "Mouse    /    Right stick"),
            ("Jump", "Space    /    South"),
            ("Cling hold", "Hold into wall    /    Left stick hold. Wall climb and wall run need this hold."),
            ("Slide", "Ctrl or C    /    East"),
            ("Air dash", "Q or Alt    /    RB"),
            ("Punch / tag", "LMB or E    /    West"),
            ("Sprint", "Shift or Alt    /    LB"),
            ("Pause", "Esc    /    Start"),
        ]
    else:
        rows = [
            ("Stick outer deadzone  1.00", "Left / Right"),
            ("Stick response curve  1.00", "Left / Right"),
            ("Gamepad look accel  0.00", "Left / Right"),
            ("P1 confirm", "Auto"),
            ("P2 confirm", "Auto"),
            ("P3 confirm", "Auto"),
            ("P4 confirm", "Auto"),
            ("Reset bindings", "Back to the defaults. Jump is Space."),
            ("Back", ""),
        ]
    ratios = []
    y = 176
    d = ImageDraw.Draw(img)
    for i, (title, sub) in enumerate(rows):
        hot = i == (2 if not bottom else 0)
        ratios.append(button(img, (120, y, 1780, y + 84), title, sub, hot, right=300))
        if not bottom:
            kb = "space" if i == 2 else "esc" if i == 8 else "arrows"
            pad = "a" if i == 2 else "b" if i == 4 else "stick"
            draw_glyph(d, kb, 1648, y + 18)
            draw_glyph(d, pad, 1720, y + 16)
        y += 90
    d.rounded_rectangle((1740, 184, 1756, 960), 4, fill=(0, 0, 0, 140))
    if bottom:
        d.rounded_rectangle((1742, 700, 1754, 948), 3, fill=GOLD)
    else:
        d.rounded_rectangle((1742, 184, 1754, 460), 3, fill=GOLD)
    footer_both(img)
    return img, min(ratios)


def drop_in():
    img = screen(0.5)
    header(img, "Who's playing", "Seated players press Space to continue")
    cards = [
        ("P1", "P1  keyboard\nLeft / Right picks a profile", True, 0),
        ("P2", "P2  pad\nLeft / Right picks a profile", True, 1),
        ("P3", "Press Space or A to join", False, 2),
        ("P4", "Press Space or A to join", False, 3),
    ]
    ratios = []
    for i, (title, detail, human, seat) in enumerate(cards):
        x = 80 + i * 450
        fill = mix(INK, SEAT[seat], 0.4)
        box = (x, 200, x + 420, 860)
        shadow(img, box)
        d = ImageDraw.Draw(img)
        hot = i == 0
        rounded(d, box, 22, HOT if hot else fill, GOLD if hot else STROKE, 6 if hot else 3)
        d.rounded_rectangle((x + 18, 236, x + 30, 820), 4, fill=SEAT[seat] if not hot else INK)
        tc = INK if hot else CREAM
        d.text((x + 48, 230), title, font=font(FONT_D, 48), fill=tc)
        yy = 310
        for line in detail.split("\n"):
            d.text((x + 48, yy), line, font=font(FONT_B, 26), fill=INK if hot else MUTE)
            yy += 36
        if not human:
            draw_glyph(d, "space", x + 90, 520)
            draw_glyph(d, "a", x + 220, 512)
        elif i == 0:
            draw_glyph(d, "arrows", x + 160, 560)
        else:
            draw_glyph(d, "stick", x + 160, 560)
        ratios.append(contrast(tc, HOT if hot else fill))
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


def records(filled=False, end=False):
    img = screen(0.5)
    header(img, "Records", "Matches, wins, tags, and longest time not It")
    ratios = []
    rows = []
    if end:
        rows = [("Empty", "", False) for _ in range(5)]
        rows.append(("Back", "", True))
    elif filled:
        rows = [
            ("Red", "matches 4   wins 1   tags 6   live 6.2", True),
            ("Blue", "matches 2   wins 0   tags 1   live 3.0", False),
            ("Empty", "", False),
            ("Empty", "", False),
            ("Empty", "", False),
            ("Empty", "", False),
        ]
    else:
        rows = [("Empty", "", i == 0) for i in range(6)]
    y = 180
    for title, detail, hot in rows:
        sub = detail.replace("\n", "   ")
        ratios.append(button(img, (360, y, 1560, y + 100), title, sub, hot))
        y += 112
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
        ("09-sound-composite.png", options("sound")),
        ("09-picture-composite.png", options("picture")),
        ("09-access-composite.png", options("access")),
        ("10-controls-composite.png", controls(False)),
        ("10-controls-bottom-composite.png", controls(True)),
        ("11-drop-in-composite.png", drop_in()),
        ("12-credits-composite.png", credits()),
        ("14-records-composite.png", records(False)),
        ("14-records-card-composite.png", records(True)),
        ("14-records-end-composite.png", records(end=True)),
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
