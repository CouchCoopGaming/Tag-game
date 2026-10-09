#!/usr/bin/env python3
"""Pass 29. The graded plate covers the 16:9 frame. One focus. Grapple from Show."""

import os

import render_pass27 as p27
import render_pass28 as p28
import render_screens2 as ui
from PIL import Image, ImageDraw, ImageStat

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass29")
p28.W, p28.H = 1920, 1080

INK = p28.INK
CREAM = p28.CREAM
GOLD = p28.GOLD
NAVY = p28.NAVY
PLATE = p28.PLATE
WELL = p28.WELL
FILL = p28.FILL
BAND = p28.BAND
W, H = 1920, 1080


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


def assert_cover(img, name):
    rgb = img.convert("RGB")
    boxes = ((0, 0, 70, 70), (W - 70, 0, W, 70), (0, H - 70, 70, H), (W - 70, H - 70, W, H))
    for box in boxes:
        st = ImageStat.Stat(rgb.crop(box))
        if max(st.mean) < 12:
            raise SystemExit("%s letterbox %s %s" % (name, box, tuple(round(v) for v in st.mean)))
    sky = ImageStat.Stat(rgb.crop((30, 12, 400, 120)))
    print("%s sky %.0f %.0f %.0f" % (name, sky.mean[0], sky.mean[1], sky.mean[2]))
    if sky.mean[2] < sky.mean[0] + 6:
        raise SystemExit("%s sky not blue" % name)


def scene_title():
    p27.reset_layout()
    img = p28.darken(0.35)
    assert_cover(img, "title-plate")
    d = ImageDraw.Draw(img)
    checks, ratios = [], []
    p28.lockup(img, (560, 150, 1360, 760))
    plate = (260, 800, 1660, 960)
    ui.rounded(d, plate, 28, PLATE)
    p27.ROWS.append(plate)
    line = "Press Space or Start"
    face = ui.font(ui.FONT_D, 84)
    tw = d.textlength(line, font=face)
    home = (280, 820, 1640, 940)
    p27.text(d, ((W - tw) / 2, 832), line, face, CREAM, PLATE, home, checks, ratios, home=home)
    return img, ratios


def scene_main():
    p27.reset_layout()
    img = p28.darken(0.30)
    assert_cover(img, "main-plate")
    d = ImageDraw.Draw(img)
    checks, ratios = [], []
    header = (96, 16, 1824, 100)
    ui.rounded(d, header, 12, NAVY)
    p27.ROWS.append(header)
    p27.text(d, (120, 28), "Menu", ui.font(ui.FONT_D, 48), GOLD, NAVY, (110, 20, 500, 92), checks, ratios, home=(110, 20, 500, 92))
    p28.lockup(img, (120, 118, 620, 500))
    blurb = (110, 512, 860, 568)
    ui.rounded(d, blurb, 10, NAVY)
    p27.ROWS.append(blurb)
    p27.text(d, (128, 520), "Local couch. One keyboard, four pads.", ui.font(ui.FONT_B, 26), CREAM, NAVY, blurb, checks, ratios, home=blurb)
    hero = (110, 580, 860, 748)
    chase = Image.open(p28.CHASE_PATH).convert("RGBA")
    shot = p27.crop_uv(chase, p27.CHASE_UV, (hero[2] - hero[0], hero[3] - hero[1]))
    img.alpha_composite(shot, (hero[0], hero[1]))
    d = ImageDraw.Draw(img)
    tip = (110, 760, 860, 1020)
    ui.rounded(d, tip, 16, NAVY)
    p27.ROWS.append(tip)
    lines = (
        (772, "Tip of the day", p28.MUTE),
        (826, "Space jumps.", CREAM),
        (880, "Hold [WASD] against a wall to climb.", CREAM),
        (934, "Double-click [RMB] to let go of the grapple.", CREAM),
    )
    for y, word, fill in lines:
        home = (128, y - 4, 844, y + 40)
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
        p27.stamp(d, (well[0] + 12, well[1] + 12, well[2] - 12, well[3] - 12), i)
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
            p27.stamp(d, (mark[0] + 10, mark[1] + 10, mark[2] - 10, mark[3] - 10), i)
            prompt = "Press Space or A to join"
            face = ui.font(ui.FONT_B, 24)
            tw = d.textlength(prompt, font=face)
            home = (x + 20, top + 470, x + card_w - 20, top + 530)
            p27.text(d, (x + (card_w - tw) / 2, top + 484), prompt, face, p28.MUTE, fill, home, checks, ratios, home=home)
    return img, ratios


def finish(img, name, notes, ratios):
    """Keep enough colors that the park stays a picture. 48 flattens the sky."""
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
    for colors in (96, 80, 64, 56):
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
    notes = []
    worst = 99.0
    img, ratios = scene_title()
    finish(img, "29-title.png", notes, ratios)
    worst = min(worst, min(ratios))
    print("  title %.2f" % min(ratios))
    img, ratios = scene_main()
    finish(img, "29-main.png", notes, ratios)
    worst = min(worst, min(ratios))
    print("  main %.2f" % min(ratios))
    img, ratios = scene_join()
    finish(img, "29-join.png", notes, ratios)
    worst = min(worst, min(ratios))
    print("  join %.2f" % min(ratios))
    print("min contrast", round(worst, 2))


if __name__ == "__main__":
    main()
