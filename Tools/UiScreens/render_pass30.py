#!/usr/bin/env python3
"""Pass 30. Waiting banner, idle seat pair, soft title plate, lighter lavender fill."""

import os

import render_pass27 as p27
import render_pass28 as p28
import render_screens2 as ui
from PIL import Image, ImageDraw, ImageFilter, ImageStat

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass30")
p28.W, p28.H = 1920, 1080

INK = p28.INK
CREAM = p28.CREAM
GOLD = p28.GOLD
NAVY = p28.NAVY
PLATE = p28.PLATE
WELL = p28.WELL
# Lavender fill lifted to (0.80, 0.72, 0.92). Hue stays lavender. Diamond stays.
FILL = [p27.FILL[0], p27.FILL[1], p27.FILL[2], (204, 184, 235)]
BAND = p28.BAND
W, H = 1920, 1080
BLUR_PATH = os.path.join(ui.ROOT, "Assets", "Resources", "UI", "Menu", "MegaBlur.png")
READY = "Everyone Ready? Press Start"


def old_banner(humans):
    return READY if humans > 0 else "Anyone can join"


def new_banner(joined, ready):
    if joined >= 2 and ready == joined:
        return READY
    if joined <= 0:
        return "Anyone can join"
    waiting = joined - ready
    if waiting < 1:
        waiting = 2 - joined
    if waiting < 1:
        waiting = 1
    if waiting == 1:
        return "Waiting for 1 player to ready up"
    return "Waiting for %d players to ready up" % waiting


def prove_banner():
    old = old_banner(2)
    now = new_banner(2, 1)
    if old != READY or now == old or now != "Waiting for 1 player to ready up":
        raise SystemExit("banner still uses the old rule: %s" % now)
    if new_banner(2, 2) != READY:
        raise SystemExit("ready pair lost the start line")
    print("banner old=%s now=%s" % (old, now))


def blurred_plate():
    src = p28.live().resize((W, H), Image.Resampling.LANCZOS).convert("RGB")
    return src.filter(ImageFilter.GaussianBlur(radius=12))


def bake_blur(plate):
    os.makedirs(os.path.dirname(BLUR_PATH), exist_ok=True)
    plate.save(BLUR_PATH, "PNG", optimize=True, compress_level=9)
    print("blur", BLUR_PATH, os.path.getsize(BLUR_PATH))


def soft_vignette(img, strength=0.32):
    n = 128
    small = Image.new("L", (n, n))
    pix = small.load()
    cx = (n - 1) / 2.0
    for y in range(n):
        for x in range(n):
            dx = (x - cx) / cx
            dy = (y - cx) / cx
            radius = (dx * dx + dy * dy) ** 0.5
            t = (radius - 0.35) / 0.95
            if t < 0:
                t = 0
            if t > 1:
                t = 1
            t = t * t
            pix[x, y] = int(round(t * strength * 255))
    alpha = small.resize(img.size, Image.Resampling.BICUBIC)
    black = Image.new("RGBA", img.size, (0, 0, 0, 255))
    black.putalpha(alpha)
    img.alpha_composite(black)


def stamp(d, box, seat):
    x0, y0, x1, y1 = box
    c = FILL[seat % 4]
    kind = seat % 4
    if kind == 0:
        d.ellipse((x0, y0, x1 - 1, y1 - 1), fill=c)
    elif kind == 1:
        d.polygon((((x0 + x1) / 2, y0), (x0, y1 - 1), (x1 - 1, y1 - 1)), fill=c)
    elif kind == 2:
        d.rectangle((x0, y0, x1 - 1, y1 - 1), fill=c)
    else:
        mx = (x0 + x1) / 2
        my = (y0 + y1) / 2
        d.polygon(((mx, y0), (x1 - 1, my), (mx, y1 - 1), (x0, my)), fill=c)


def corner_mean(img):
    rgb = img.convert("RGB")
    boxes = ((0, 0, 70, 70), (W - 70, 0, W, 70), (0, H - 70, 70, H), (W - 70, H - 70, W, H))
    means = []
    for box in boxes:
        means.append(ImageStat.Stat(rgb.crop(box)).mean)
    return means


def assert_plate_corners(before, after, name):
    got = corner_mean(after)
    was = corner_mean(before)
    for i, mean in enumerate(got):
        if max(mean) < 28:
            raise SystemExit("%s corner black %s" % (name, tuple(round(v) for v in mean)))
        if max(mean) > max(was[i]) + 1:
            raise SystemExit("%s vignette did not fall off" % name)
    sky = ImageStat.Stat(after.convert("RGB").crop((220, 70, 620, 200)))
    print("%s sky %.0f %.0f %.0f corners %s" % (
        name, sky.mean[0], sky.mean[1], sky.mean[2],
        tuple(tuple(round(v) for v in m) for m in got)))
    if sky.mean[2] < sky.mean[0] + 4:
        raise SystemExit("%s sky not blue" % name)


def scene_title(plate):
    p27.reset_layout()
    img = plate.convert("RGBA")
    dim = Image.new("RGBA", (W, H), (0, 0, 0, int(round(0.35 * 255))))
    img.alpha_composite(dim)
    before = img.copy()
    soft_vignette(img)
    assert_plate_corners(before, img, "title")
    d = ImageDraw.Draw(img)
    checks, ratios = [], []
    p28.lockup(img, (560, 150, 1360, 760))
    d = ImageDraw.Draw(img)
    plate_box = (260, 800, 1660, 960)
    ui.rounded(d, plate_box, 28, PLATE)
    p27.ROWS.append(plate_box)
    line = "Press Space or Start"
    face = ui.font(ui.FONT_D, 84)
    tw = d.textlength(line, font=face)
    home = (280, 820, 1640, 940)
    p27.text(d, ((W - tw) / 2, 832), line, face, CREAM, PLATE, home, checks, ratios, home=home)
    return img, ratios


def seat_idle(img, cx, sole, color, kind):
    """Upright idle. Legs are vertical, feet sit on the disc, shape on the chest."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    disc = tuple(max(0, c // 3) for c in color) + (235,)
    d.ellipse((cx - 92, sole - 12, cx + 92, sole + 26), fill=disc)
    body = color + (255,)
    d.ellipse((cx - 40, sole - 10, cx - 8, sole + 6), fill=body)
    d.ellipse((cx + 8, sole - 10, cx + 40, sole + 6), fill=body)
    d.rounded_rectangle((cx - 36, sole - 176, cx - 12, sole - 2), 12, fill=body)
    d.rounded_rectangle((cx + 12, sole - 176, cx + 36, sole - 2), 12, fill=body)
    d.rounded_rectangle((cx - 48, sole - 206, cx + 48, sole - 162), 14, fill=body)
    d.rounded_rectangle((cx - 54, sole - 348, cx + 54, sole - 192), 22, fill=body)
    d.rounded_rectangle((cx - 82, sole - 328, cx - 56, sole - 196), 12, fill=body)
    d.rounded_rectangle((cx + 56, sole - 328, cx + 82, sole - 196), 12, fill=body)
    d.ellipse((cx - 36, sole - 422, cx + 36, sole - 350), fill=body)
    well = (cx - 30, sole - 308, cx + 30, sole - 248)
    d.rounded_rectangle(well, 8, fill=WELL + (255,))
    img.alpha_composite(layer)
    d2 = ImageDraw.Draw(img)
    stamp(d2, (well[0] + 8, well[1] + 8, well[2] - 8, well[3] - 8), kind)


def scene_main():
    p27.reset_layout()
    img = p28.darken(0.30)
    d = ImageDraw.Draw(img)
    checks, ratios = [], []
    face = ui.font(ui.FONT_D, 48)
    label = "Menu"
    tw = d.textlength(label, font=face)
    header = (96, 16, int(96 + tw + 72), 100)
    if header[2] > 520:
        raise SystemExit("menu bar still wide")
    ui.rounded(d, header, 12, NAVY)
    p27.ROWS.append(header)
    p27.text(d, (96 + 36, 28), label, face, GOLD, NAVY, header, checks, ratios, home=header)
    bare = img.convert("RGB").getpixel((header[2] + 48, 50))
    if abs(bare[0] - NAVY[0]) < 8 and abs(bare[1] - NAVY[1]) < 8 and abs(bare[2] - NAVY[2]) < 12:
        raise SystemExit("menu bar still full width %s" % (bare,))
    p28.lockup(img, (140, 112, 520, 360))
    d = ImageDraw.Draw(img)
    blurb = (110, 368, 860, 416)
    ui.rounded(d, blurb, 10, NAVY)
    p27.ROWS.append(blurb)
    p27.text(d, (128, 376), "Local couch. One keyboard, four pads.", ui.font(ui.FONT_B, 22), CREAM, NAVY, blurb, checks, ratios, home=blurb)
    seat_idle(img, 280, 852, FILL[0], 0)
    seat_idle(img, 560, 852, FILL[1], 1)
    d = ImageDraw.Draw(img)
    tip = (110, 888, 860, 1048)
    ui.rounded(d, tip, 16, NAVY)
    p27.ROWS.append(tip)
    tip_px = img.convert("RGB").getpixel((200, 930))
    if abs(tip_px[0] - NAVY[0]) > 30 or abs(tip_px[2] - NAVY[2]) > 40:
        raise SystemExit("tips covered %s" % (tip_px,))
    lines = (
        (896, "Tip of the day", p28.MUTE),
        (932, "Space jumps.", CREAM),
        (968, "Hold [WASD] against a wall to climb.", CREAM),
        (1004, "Double-click [RMB] to let go of the grapple.", CREAM),
    )
    for y, word, fill in lines:
        home = (128, y - 4, 844, y + 34)
        p27.text(d, (136, y), word, ui.font(ui.FONT_B, 22), fill, NAVY, home, checks, ratios, home=home)
    rows = (
        ("Play", "Local couch", True),
        ("Practice", "Free run any arena, no tagger", False),
        ("Options", "Sound, picture, access", False),
        ("Controls", "Binds. Space still jumps.", False),
    )
    if sum(1 for _, _, hot in rows if hot) != 1:
        raise SystemExit("two focus")
    y = 120
    play = None
    for title, sub, hot in rows:
        box = (920, y, 1760, y + 108)
        if hot:
            play = box
        p28.paint_row(d, box, title, sub, hot, checks, ratios)
        y += 144
    records = None
    for title, x in (("Credits", 920), ("Records", 1200), ("Quit", 1480)):
        box = (x, y, x + 260, y + 96)
        if title == "Records":
            records = box
        p28.paint_row(d, box, title, "", False, checks, ratios)
    one_focus(img, play, records)
    return img, ratios


def one_focus(img, play, records):
    rgb = img.convert("RGB")

    def edge(box):
        x0, y0, _, _ = box
        return rgb.getpixel((x0 - 10, y0 + 24))

    play_px = edge(play)
    rec_px = edge(records)
    print("play edge", play_px, "records edge", rec_px)
    if play_px[0] < 200 or play_px[1] < 160:
        raise SystemExit("play missing gold")
    if rec_px[0] > 180 and rec_px[1] > 140 and rec_px[2] < 90:
        raise SystemExit("records also focused")


def scene_join():
    p27.reset_layout()
    img = p28.darken(0.28)
    d = ImageDraw.Draw(img)
    checks, ratios = [], []
    header = (80, 24, 900, 100)
    ui.rounded(d, header, 12, NAVY)
    p27.ROWS.append(header)
    p27.text(d, (100, 36), "Who's playing", ui.font(ui.FONT_D, 44), GOLD, NAVY, header, checks, ratios, home=header)
    banner_line = new_banner(2, 1)
    if banner_line == READY:
        raise SystemExit("join still shows the start line")
    banner = (940, 28, 1840, 96)
    ui.rounded(d, banner, 12, NAVY)
    p27.ROWS.append(banner)
    p27.text(d, (banner[0] + 24, 44), banner_line, ui.font(ui.FONT_B, 26), GOLD, NAVY, banner, checks, ratios, home=banner)
    cards = (
        (True, "Red", True, "Keyboard"),
        (True, "Blue", False, "Gamepad"),
        (False, "", False, ""),
        (False, "", False, ""),
    )
    card_w, card_h, gap, top = 420, 760, 24, 140
    left = (W - (card_w * 4 + gap * 3)) // 2
    for i, (human, profile, ready, device) in enumerate(cards):
        x = left + i * (card_w + gap)
        box = (x, top, x + card_w, top + card_h)
        fill = ui.mix(INK, BAND[i], 0.40)
        pad = 16 if i == 0 else 5
        frame = (x - pad, top - pad, x + card_w + pad, top + card_h + pad)
        ui.rounded(d, frame, 22, GOLD if i == 0 else p28.STROKE)
        ui.rounded(d, box, 18, fill)
        p27.ROWS.append(box)
        d.rectangle((x + 16, top + 18, x + 28, top + card_h - 18), fill=BAND[i])
        name_home = (x + 44, top + 16, x + 250, top + 78)
        p27.text(d, (x + 48, top + 22), "P%d" % (i + 1), ui.font(ui.FONT_D, 40), CREAM, fill, name_home, checks, ratios, home=name_home)
        well = (x + card_w - 104, top + 18, x + card_w - 16, top + 106)
        ui.rounded(d, well, 10, WELL)
        stamp(d, (well[0] + 12, well[1] + 12, well[2] - 12, well[3] - 12), i)
        ratios.append(ui.contrast(FILL[i], WELL))
        if human:
            ui.draw_bust(img, x + 90, top + 140, 240, 280, BAND[i])
            d = ImageDraw.Draw(img)
            who = "<  %s  >" % profile
            who_home = (x + 40, top + 440, x + card_w - 40, top + 490)
            p27.text(d, (x + 70, top + 448), who, ui.font(ui.FONT_B, 28), CREAM, fill, who_home, checks, ratios, home=who_home)
            dev_home = (x + 80, top + 510, x + card_w - 40, top + 556)
            p27.text(d, (x + 120, top + 516), device, ui.font(ui.FONT_B, 26), CREAM, fill, dev_home, checks, ratios, home=dev_home)
            chip = (x + 120, top + 640, x + 300, top + 700)
            chip_fill = GOLD if ready else NAVY
            ink = INK if ready else CREAM
            ui.rounded(d, chip, 12, chip_fill)
            word = "Ready" if ready else "Joined"
            p27.text(d, (x + 150, top + 652), word, ui.font(ui.FONT_D, 32), ink, chip_fill, chip, checks, ratios, home=chip)
        else:
            ghost(img, x + 110, top + 170, 200, 280, BAND[i])
            d = ImageDraw.Draw(img)
            mark = (x + 170, top + 300, x + 250, top + 380)
            ui.rounded(d, mark, 10, WELL)
            stamp(d, (mark[0] + 10, mark[1] + 10, mark[2] - 10, mark[3] - 10), i)
            prompt = "Press Space or A to join"
            face = ui.font(ui.FONT_B, 24)
            tw = d.textlength(prompt, font=face)
            home = (x + 20, top + 470, x + card_w - 20, top + 530)
            p27.text(d, (x + (card_w - tw) / 2, top + 484), prompt, face, p28.MUTE, fill, home, checks, ratios, home=home)
    return img, ratios


def ghost(img, x, y, w, h, color):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    line = tuple(min(255, int(c * 0.45 + 140)) for c in color) + (190,)
    fill = tuple(color) + (46,)
    body = (x + int(w * 0.16), y + int(h * 0.24), x + int(w * 0.84), y + int(h * 0.98))
    head = (x + int(w * 0.30), y, x + int(w * 0.70), y + int(h * 0.30))
    d.rounded_rectangle(body, 28, fill=fill, outline=line, width=5)
    d.ellipse(head, fill=fill, outline=line, width=5)
    img.alpha_composite(layer)


def finish(img, name, notes, ratios):
    if min(ratios) < 4.5:
        raise SystemExit("%s contrast %.2f" % (name, min(ratios)))
    raw = img.convert("RGB").tobytes()
    for bad in (b"podium", b"PODIUM", b"Starting", b"COMPOSITE"):
        if bad in raw:
            raise SystemExit("bad word in %s" % name)
    p27.layout_report(name)
    path = os.path.join(ui.OUT, name)
    os.makedirs(ui.OUT, exist_ok=True)
    rgb = img.convert("RGB")
    rgb.save(path, "PNG", optimize=True, compress_level=9)
    size = os.path.getsize(path)
    protect = list(FILL) + list(BAND) + [WELL, INK, GOLD, CREAM, NAVY, PLATE, ui.HOT, ui.PANEL, ui.MUTE, ui.STROKE]
    for colors in (220, 192, 160, 128, 112, 96):
        if size <= 390000:
            break
        pinned = ui.pin_colors(rgb, protect, colors)
        pinned.save(path, "PNG", optimize=True, compress_level=9)
        size = os.path.getsize(path)
        print("requant %s %d colors %d" % (name, colors, size))
    if size > 400000:
        raise SystemExit("size %s %d" % (name, size))
    notes.append((name, size))
    print("%s %d" % (name, size))


def main():
    prove_banner()
    plate = blurred_plate()
    bake_blur(plate)
    notes = []
    worst = 99.0
    img, ratios = scene_title(plate)
    finish(img, "30-title.png", notes, ratios)
    worst = min(worst, min(ratios))
    print("  title %.2f" % min(ratios))
    img, ratios = scene_main()
    finish(img, "30-main.png", notes, ratios)
    worst = min(worst, min(ratios))
    print("  main %.2f" % min(ratios))
    img, ratios = scene_join()
    finish(img, "30-join.png", notes, ratios)
    worst = min(worst, min(ratios))
    print("  join %.2f" % min(ratios))
    print("min contrast", round(worst, 2))


if __name__ == "__main__":
    main()
