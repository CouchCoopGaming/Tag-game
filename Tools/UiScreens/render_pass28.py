#!/usr/bin/env python3
"""Pass 28. The live Mega Park plate is the graded yard.

MegaGrade.png is neutralize() from pass 27, baked once. These frames load that
file. They do not grade it again. Unity is not installed, so the menu is drawn
on the texture the game loads.
"""

import os

import render_pass27 as p27
import render_screens2 as ui
from PIL import Image, ImageChops, ImageDraw, ImageStat

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass28")
GOLD_PATH = os.path.join(ui.ROOT, "Assets", "Resources", "UI", "Menu", "MegaGold.png")
GRADE_PATH = os.path.join(ui.ROOT, "Assets", "Resources", "UI", "Menu", "MegaGrade.png")
LOCK_PATH = os.path.join(ui.ROOT, "Assets", "Resources", "UI", "Menu", "TagLockup.png")
CHASE_PATH = os.path.join(ui.ROOT, "Assets", "Resources", "UI", "Menu", "Chase.png")

INK = (10, 18, 41)
CREAM = ui.CREAM
GOLD = ui.GOLD
NAVY = (15, 41, 102)
PLATE = (10, 20, 46)
WELL = p27.WELL
FILL = p27.FILL
BAND = p27.BAND
HOT = ui.HOT
PANEL = ui.PANEL
MUTE = ui.MUTE
STROKE = ui.STROKE

W, H = 1920, 1080
GRADE = None


def bake():
    if os.path.isfile(GRADE_PATH):
        got = Image.open(GRADE_PATH).convert("RGB")
        if got.size == (1920, 1080):
            print("grade", GRADE_PATH, os.path.getsize(GRADE_PATH), got.size)
            return
    src = Image.open(GOLD_PATH).convert("RGB")
    graded = p27.neutralize(src)
    os.makedirs(os.path.dirname(GRADE_PATH), exist_ok=True)
    graded.save(GRADE_PATH, "PNG", optimize=True)
    got = Image.open(GRADE_PATH).convert("RGB")
    diff = ImageChops.difference(graded, got)
    stat = ImageStat.Stat(diff)
    if max(stat.extrema[c][1] for c in range(3)) != 0:
        raise SystemExit("grade roundtrip")
    print("baked", GRADE_PATH, os.path.getsize(GRADE_PATH), got.size)


def live():
    global GRADE
    if GRADE is None:
        GRADE = Image.open(GRADE_PATH).convert("RGB")
    return GRADE


def darken(alpha):
    img = live().resize((W, H), Image.Resampling.LANCZOS).convert("RGBA")
    dim = Image.new("RGBA", (W, H), (0, 0, 0, int(round(alpha * 255))))
    img.alpha_composite(dim)
    return img


def vignette(img):
    d = ImageDraw.Draw(img)
    edge = (0, 0, 0, 140)
    d.rectangle((0, 0, W, int(H * 0.14)), fill=edge)
    d.rectangle((0, int(H * 0.84), W, H), fill=edge)
    d.rectangle((0, 0, int(W * 0.08), H), fill=edge)
    d.rectangle((int(W * 0.92), 0, W, H), fill=edge)


def lockup(img, box):
    mark = Image.open(LOCK_PATH).convert("RGBA")
    x0, y0, x1, y1 = box
    tw, th = x1 - x0, y1 - y0
    scale = min(tw / float(mark.size[0]), th / float(mark.size[1]))
    nw = max(1, int(round(mark.size[0] * scale)))
    nh = max(1, int(round(mark.size[1] * scale)))
    mark = mark.resize((nw, nh), Image.Resampling.LANCZOS)
    ox = x0 + (tw - nw) // 2
    oy = y0 + (th - nh) // 2
    img.alpha_composite(mark, (ox, oy))


def finish(img, name, notes, ratios):
    if min(ratios) < 4.5:
        raise SystemExit("%s contrast %.2f" % (name, min(ratios)))
    raw = img.convert("RGB").tobytes()
    for bad in (b"podium", b"PODIUM", b"Starting", b"COMPOSITE"):
        if bad in raw:
            raise SystemExit("bad word in %s" % name)
    p27.layout_report(name)
    p27.save(img, name, notes)
    path = os.path.join(ui.OUT, name)
    size = os.path.getsize(path)
    protect = list(FILL) + list(BAND) + [WELL, INK, GOLD, CREAM, NAVY, PLATE, HOT, PANEL, MUTE, STROKE]
    for colors in (80, 64, 48, 40):
        if size <= 390000:
            break
        rgb = Image.open(path).convert("RGB")
        pinned = ui.pin_colors(rgb, protect, colors)
        pinned.save(path, "PNG", optimize=True, compress_level=9)
        size = os.path.getsize(path)
        print("requant %s %d colors %d" % (name, colors, size))
    if size > 400000:
        raise SystemExit("size %s %d" % (name, size))
    return size


def scene_title():
    p27.reset_layout()
    img = darken(0.18)
    vignette(img)
    d = ImageDraw.Draw(img)
    checks = []
    ratios = []
    lock = (560, 160, 1360, 770)
    lockup(img, lock)
    plate = (260, 800, 1660, 960)
    ui.rounded(d, plate, 28, PLATE)
    p27.ROWS.append(plate)
    line = "Press Space or Start"
    face = ui.font(ui.FONT_D, 84)
    tw = d.textlength(line, font=face)
    home = (280, 820, 1640, 940)
    xy = ((W - tw) / 2, 832)
    p27.text(d, xy, line, face, CREAM, PLATE, home, checks, ratios, home=home)
    return img, ratios


def scene_main():
    p27.reset_layout()
    img = darken(0.25)
    vignette(img)
    d = ImageDraw.Draw(img)
    checks = []
    ratios = []
    header = (96, 16, 1824, 100)
    ui.rounded(d, header, 12, NAVY)
    p27.ROWS.append(header)
    p27.text(d, (120, 28), "Menu", ui.font(ui.FONT_D, 48), GOLD, NAVY, (110, 20, 500, 92), checks, ratios, home=(110, 20, 500, 92))
    lockup(img, (120, 118, 620, 500))
    blurb = (110, 512, 860, 568)
    ui.rounded(d, blurb, 10, NAVY)
    p27.ROWS.append(blurb)
    p27.text(d, (128, 520), "Local couch. One keyboard, four pads.", ui.font(ui.FONT_B, 26), CREAM, NAVY, blurb, checks, ratios, home=blurb)
    hero = (110, 580, 860, 748)
    chase = Image.open(CHASE_PATH).convert("RGBA")
    shot = p27.crop_uv(chase, p27.CHASE_UV, (hero[2] - hero[0], hero[3] - hero[1]))
    img.alpha_composite(shot, (hero[0], hero[1]))
    tip = (110, 760, 860, 1020)
    ui.rounded(d, tip, 16, NAVY)
    p27.ROWS.append(tip)
    lines = (
        (772, "Tip of the day", MUTE),
        (826, "Space jumps.", CREAM),
        (880, "Hold [WASD] against a wall to climb.", CREAM),
        (934, "Double-click RMB to let go of the grapple.", CREAM),
    )
    for y, word, fill in lines:
        home = (128, y - 4, 844, y + 40)
        face = ui.font(ui.FONT_B, 22 if y > 740 else 20)
        p27.text(d, (136, y), word, face, fill, NAVY, home, checks, ratios, home=home)
    rows = (
        ("Play", "Local couch", True),
        ("Practice", "Free run any arena, no tagger", False),
        ("Options", "Sound, picture, access", False),
        ("Controls", "Binds. Space still jumps.", False),
    )
    y = 120
    for title, sub, hot in rows:
        box = (920, y, 1760, y + 108)
        paint_row(d, box, title, sub, hot, checks, ratios)
        y += 144
    small = (("Credits", 920), ("Records", 1200), ("Quit", 1480))
    for i, (title, x) in enumerate(small):
        box = (x, y, x + 260, y + 96)
        paint_row(d, box, title, "", i == 1, checks, ratios)
    return img, ratios


def paint_row(d, box, title, sub, hot, checks, ratios):
    x0, y0, x1, y1 = box
    short = (y1 - y0) < 96
    pad = 4 if short else (16 if hot else 5)
    ui.rounded(d, (x0 - pad, y0 - pad, x1 + pad, y1 + pad), 18, GOLD if hot else STROKE)
    fill = HOT if hot else PANEL
    ui.rounded(d, box, 16, fill)
    p27.ROWS.append(box)
    title_c = INK if hot else CREAM
    sub_c = INK if hot else MUTE
    if short:
        title_home = (x0 + 20, y0 + 4, x1 - 12, y0 + 34)
        p27.text(d, (x0 + 24, y0 + 6), title, ui.font(ui.FONT_D, 26), title_c, fill, title_home, checks, ratios, home=title_home)
        if sub:
            sub_home = (x0 + 20, y0 + 34, x1 - 12, y1 - 4)
            p27.text(d, (x0 + 24, y0 + 36), sub, ui.font(ui.FONT_B, 16), sub_c, fill, sub_home, checks, ratios, home=sub_home)
        return
    title_home = (x0 + 28, y0 + 12, x1 - 16, y0 + 56)
    p27.text(d, (x0 + 32, y0 + 16), title, ui.font(ui.FONT_D, 36), title_c, fill, title_home, checks, ratios, home=title_home)
    if sub:
        sub_home = (x0 + 28, y0 + 60, x1 - 16, y1 - 8)
        p27.text(d, (x0 + 32, y0 + 64), sub, ui.font(ui.FONT_B, 22), sub_c, fill, sub_home, checks, ratios, home=sub_home)


def scene_join():
    p27.reset_layout()
    img = darken(0.28)
    d = ImageDraw.Draw(img)
    checks = []
    ratios = []
    header = (80, 24, 900, 100)
    ui.rounded(d, header, 12, NAVY)
    p27.ROWS.append(header)
    p27.text(d, (100, 36), "Who's playing", ui.font(ui.FONT_D, 44), GOLD, NAVY, header, checks, ratios, home=header)
    banner = (980, 28, 1760, 96)
    ui.rounded(d, banner, 12, NAVY)
    p27.ROWS.append(banner)
    p27.text(d, (1000, 44), "Everyone Ready? Press Start", ui.font(ui.FONT_B, 26), GOLD, NAVY, banner, checks, ratios, home=banner)
    cards = (
        (True, "Red", True, "Keyboard"),
        (True, "Blue", False, "Gamepad"),
        (False, "", False, ""),
        (False, "", False, ""),
    )
    card_w, card_h = 420, 760
    gap = 24
    left = (W - (card_w * 4 + gap * 3)) // 2
    top = 140
    for i, (human, profile, ready, device) in enumerate(cards):
        x = left + i * (card_w + gap)
        box = (x, top, x + card_w, top + card_h)
        fill = ui.mix(INK, BAND[i], 0.40)
        pad = 16 if i == 0 else 5
        frame = (x - pad, top - pad, x + card_w + pad, top + card_h + pad)
        ui.rounded(d, frame, 22, GOLD if i == 0 else STROKE)
        ui.rounded(d, box, 18, fill)
        p27.ROWS.append(frame)
        d.rectangle((x + 16, top + 18, x + 28, top + card_h - 18), fill=BAND[i])
        name_home = (x + 44, top + 16, x + 250, top + 78)
        p27.text(d, (x + 48, top + 22), "P%d" % (i + 1), ui.font(ui.FONT_D, 40), CREAM, fill, name_home, checks, ratios, home=name_home)
        well = (x + card_w - 104, top + 18, x + card_w - 16, top + 106)
        ui.rounded(d, well, 10, WELL)
        shape = (well[0] + 12, well[1] + 12, well[2] - 12, well[3] - 12)
        p27.stamp(d, shape, i)
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
            prompt = "Press Space or A to join"
            home = (x + 36, top + 160, x + card_w - 24, top + 230)
            p27.text(d, (x + 44, top + 176), prompt, ui.font(ui.FONT_B, 26), MUTE, fill, home, checks, ratios, home=home)
    return img, ratios


def scene_pause():
    p27.reset_layout()
    img = darken(0.35)
    d = ImageDraw.Draw(img)
    checks = []
    ratios = [ui.contrast(GOLD, NAVY), ui.contrast(CREAM, INK), ui.contrast(INK, HOT)]
    header = (40, 24, W - 40, 120)
    ui.rounded(d, header, 16, NAVY)
    p27.ROWS.append(header)
    well = (64, 40, 120, 96)
    ui.rounded(d, well, 8, WELL)
    shape = (well[0] + 8, well[1] + 8, well[2] - 8, well[3] - 8)
    p27.stamp(d, shape, 0)
    ratios.append(ui.contrast(FILL[0], WELL))
    title_home = (140, 36, 900, 100)
    p27.text(d, (148, 44), "Paused by P1", ui.font(ui.FONT_D, 44), GOLD, NAVY, title_home, checks, ratios, home=title_home)
    banner_home = (980, 48, W - 64, 96)
    p27.text(d, (988, 54), "Mega Park  ·  Least It", ui.font(ui.FONT_B, 22), CREAM, NAVY, banner_home, checks, ratios, home=banner_home)
    items = (
        ("Resume", "Back into the match", True),
        ("Restart round", "Same arena, same rules", False),
        ("Options", "Sound, picture, and controls", False),
        ("Quit to menu", "Leave this match", False),
    )
    bh, gap = 72, 8
    stack = len(items) * bh + (len(items) - 1) * gap
    card_w = 960
    card_h = stack + 48
    left = (W - card_w) // 2
    bottom = H - 24
    card = (left, bottom - card_h, left + card_w, bottom)
    ui.rounded(d, card, 16, INK)
    d.rectangle((card[0] + 12, card[1] + 6, card[2] - 12, card[1] + 14), fill=BAND[0])
    y = card[1] + 18
    for title, sub, hot in items:
        box = (card[0] + 16, y, card[2] - 16, y + bh)
        paint_row(d, box, title, sub, hot, checks, ratios)
        y += bh + gap
    return img, ratios, card


def scene_load():
    p27.park_src = live
    p27.reset_layout()
    old_w, old_h = p27.W, p27.H
    p27.W, p27.H = 1280, 720
    try:
        img, checks, ratios, plates, shapes, tracks = p27.scene_load(4)
    finally:
        p27.W, p27.H = old_w, old_h
    return img, ratios


def grade_stats():
    src = live()
    sky = ImageStat.Stat(src.crop((40, 20, 400, 160)))
    ground = ImageStat.Stat(src.crop((600, 632, 680, 672)))
    spread = max(ground.mean) - min(ground.mean)
    print("live sky %.0f %.0f %.0f" % tuple(sky.mean))
    print("live concrete %.0f %.0f %.0f spread %.1f" % (ground.mean[0], ground.mean[1], ground.mean[2], spread))
    if sky.mean[2] + 8 < sky.mean[0]:
        raise SystemExit("live sky still warm")
    if spread > 22:
        raise SystemExit("live concrete still cast")


def main():
    bake()
    grade_stats()
    notes = []
    worst = 99.0
    img, ratios = scene_title()
    finish(img, "28-title.png", notes, ratios)
    worst = min(worst, min(ratios))
    print("  title %.2f" % min(ratios))
    img, ratios = scene_main()
    finish(img, "28-main.png", notes, ratios)
    worst = min(worst, min(ratios))
    print("  main %.2f" % min(ratios))
    img, ratios = scene_join()
    finish(img, "28-join.png", notes, ratios)
    worst = min(worst, min(ratios))
    print("  join %.2f" % min(ratios))
    img, ratios, card = scene_pause()
    finish(img, "28-pause.png", notes, ratios)
    pause = Image.open(os.path.join(ui.OUT, "28-pause.png")).convert("RGB")
    park = pause.crop((80, 150, W - 80, card[1] - 16))
    st = ImageStat.Stat(park.convert("L"))
    frac = (card[3] - card[1]) / float(H)
    print("  pause %.2f card %.2f park std %.1f lum %.1f" % (min(ratios), frac, st.stddev[0], st.mean[0]))
    if st.stddev[0] < 8:
        raise SystemExit("pause park flat")
    if frac > 0.34:
        raise SystemExit("pause card %.2f" % frac)
    raw = live()
    raw_lum = ImageStat.Stat(raw.crop((80, 150, W - 80, card[1] - 16)).convert("L")).mean[0]
    if st.mean[0] > raw_lum * 0.75:
        raise SystemExit("pause not darkened %.1f vs %.1f" % (st.mean[0], raw_lum))
    worst = min(worst, min(ratios))
    img, ratios = scene_load()
    finish(img, "28-load-4-720.png", notes, ratios)
    worst = min(worst, min(ratios))
    print("  load %.2f" % min(ratios))
    print("min contrast", round(worst, 2))


if __name__ == "__main__":
    main()
