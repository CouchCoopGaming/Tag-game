#!/usr/bin/env python3
"""Pass 18 controls stills. Player list: no arena rows, no Alt on dash or sprint."""

import os

import render_screens2 as ui
from PIL import Image, ImageDraw

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass18")

ROW_L = 120
ROW_R = 1680
PAD_R = ROW_R - 12
PAD_W = 200
KEY_R = PAD_R - 16 - PAD_W
KEY_W = 280
KEY_L = KEY_R - KEY_W

ACTIONS = [
    ("Move", ["wasd"], ["leftStick"], ""),
    ("Look", ["mouse"], ["rightStick"], ""),
    ("Jump", ["space"], ["buttonSouth"], ""),
    ("Cling hold", ["holdIntoWall"], ["leftStickHold"], "Wall climb and wall run need this hold. Wall jump is this hold plus Jump."),
    ("Slide", ["leftCtrl", "c"], ["buttonEast"], ""),
    ("Air dash", ["q"], ["rightShoulder"], ""),
    ("Punch / tag", ["mouseLeft", "e"], ["buttonWest"], ""),
    ("Sprint", ["leftShift"], ["leftShoulder"], ""),
    ("Pause", ["escape"], ["start"], ""),
    ("Minimap", ["m"], ["select"], ""),
]
NOTES = [
    ("Grapple", ["mouseRight"], ["leftTrigger"], "RMB. Press pulls. Second press within 0.28 s releases. Left hand."),
    ("Zip", [], [], "Hold cling to grab. Jump to drop."),
    ("Launch pad", [], [], "Walk on. No button."),
]

WORDS = {
    "mouse": "Mouse",
    "rightStick": "Right stick",
    "leftStickHold": "Left stick",
    "holdIntoWall": "WASD",
}


def canvas(height):
    base = ui.backdrop(0.5)
    if height <= ui.H:
        return base
    img = Image.new("RGBA", (ui.W, height), (8, 24, 72, 255))
    img.paste(base, (0, 0))
    return img


def footer_at(img):
    old = ui.H
    ui.H = img.size[1]
    try:
        ui.footer(img)
    finally:
        ui.H = old


def seats(img, y, selected, ratios):
    d = ImageDraw.Draw(img)
    x = ROW_L
    chip_w, chip_h, gap = 140, 72, 12
    face = ui.font(ui.FONT_D, 36)
    for i, name in enumerate(("P1", "P2", "P3", "P4")):
        color = ui.SEAT[i]
        stroke = ui.GOLD if i == selected else ui.STROKE
        ui.rounded(d, (x, y, x + chip_w, y + chip_h), 16, stroke)
        ui.rounded(d, (x + 6, y + 6, x + chip_w - 6, y + chip_h - 6), 12, color)
        tw = d.textlength(name, font=face)
        plate_w = max(64, int(tw) + 28)
        px = x + (chip_w - plate_w) // 2
        ui.rounded(d, (px, y + 16, px + plate_w, y + chip_h - 16), 10, ui.INK)
        tx = x + (chip_w - tw) / 2
        ty = y + 18
        d.text((tx, ty), name, font=face, fill=ui.CREAM)
        ok, bb = ui.text_inside(d, (tx, ty), name, face, (px + 4, y + 16, px + plate_w - 4, y + chip_h - 16))
        if not ok:
            raise SystemExit("seat clip %s %s" % (name, bb))
        ratios.append(ui.contrast(ui.CREAM, ui.INK))
        x += chip_w + gap
    return y + chip_h


def columns(d, y, h, ratios, checks):
    if PAD_R > ROW_R - 12 or KEY_R > PAD_R - 16 or KEY_L < ROW_L:
        raise SystemExit("headers outside the row")
    key_box = (KEY_L, y, KEY_R, y + h)
    pad_box = (PAD_R - PAD_W, y, PAD_R, y + h)
    ui.rounded(d, key_box, 12, ui.NAVY)
    ui.rounded(d, pad_box, 12, ui.NAVY)
    face = ui.font(ui.FONT_B, 26)
    key = "Keyboard / Mouse"
    pad = "Pad"
    while face.size > 18 and d.textlength(key, font=face) > KEY_W - 16:
        face = ui.font(ui.FONT_B, face.size - 1)
    kw = d.textlength(key, font=face)
    pw = d.textlength(pad, font=face)
    kxy = (KEY_R - 12 - kw, y + 14)
    pxy = (PAD_R - 12 - pw, y + 14)
    d.text(kxy, key, font=face, fill=ui.CREAM)
    d.text(pxy, pad, font=face, fill=ui.CREAM)
    for text, xy, box in ((key, kxy, key_box), (pad, pxy, pad_box)):
        ok, bb = ui.text_inside(d, xy, text, face, (box[0] + 4, box[1] + 2, box[2] - 4, box[3] - 2))
        if not ok:
            raise SystemExit("column clip %s %s" % (text, bb))
        checks.append((xy, text, face, ui.CREAM, ui.NAVY))
    ratios.append(ui.contrast(ui.CREAM, ui.NAVY))


def cell_w(token):
    if token in WORDS:
        return {"mouse": 132, "rightStick": 176, "leftStickHold": 168, "holdIntoWall": 108}[token]
    return ui.mark_width(token)


def draw_word(d, x, y, w, label):
    h = 48
    ui.rounded(d, (x, y, x + w, y + h), 8, ui.NAVY)
    size = 22
    face = ui.font(ui.FONT_B, size)
    while size > 14 and d.textlength(label, font=face) > w - 16:
        size -= 1
        face = ui.font(ui.FONT_B, size)
    ui.rounded(d, (x + 3, y + 3, x + w - 3, y + h - 3), 6, ui.KEY_CAP, ui.KEY_INK, 2)
    tw = d.textlength(label, font=face)
    d.text((x + (w - tw) / 2, y + (h - size) / 2 - 1), label, font=face, fill=ui.KEY_INK)
    return (x + (w - tw) / 2, y + (h - size) / 2 - 1), face


def glyphs(d, tokens, right, left, y, checks):
    if right > ROW_R - 12:
        raise SystemExit("glyph column %s past the row %s" % (right, ROW_R))
    gx = right
    for token in reversed(tokens):
        w = cell_w(token)
        gx -= w
        if gx < left:
            raise SystemExit("glyph %s left of its column" % token)
        if token in WORDS:
            xy, face = draw_word(d, gx, y, w, WORDS[token])
            checks.append((xy, WORDS[token], face, ui.KEY_INK, ui.KEY_CAP))
        else:
            ui.draw_token(d, token, gx, y)
        gx -= 8


def row(img, y, h, title, sub, hot, keys, pads, ratios, checks):
    fill = ui.HOT if hot else ui.PANEL
    ink = ui.INK if hot else ui.CREAM
    sub_ink = ui.INK if hot else ui.MUTE
    reserve = ROW_R - KEY_L + 8
    text_w = (ROW_R - ROW_L) - 28 - reserve
    ratios.append(ui.button(img, (ROW_L, y, ROW_R, y + h), title, sub, hot, right=reserve))
    if sub:
        ratios.append(ui.contrast(sub_ink, fill))
    d = ImageDraw.Draw(img)
    title_size = 40 if h >= 108 else 32
    title_y = y + (24 if h >= 108 else 20)
    face = ui.font(ui.FONT_D, title_size)
    checks.append(((148, title_y), title, face, ink, fill))
    if sub:
        if h >= 108:
            sub_y, sub_size = y + 70, 30
        else:
            sub_y, sub_size = y + 60, 20
        sf = ui.font(ui.FONT_B, sub_size)
        while sub_size > 18 and d.textlength(sub, font=sf) > text_w:
            sub_size -= 1
            sf = ui.font(ui.FONT_B, sub_size)
        checks.append(((148, sub_y), sub, sf, sub_ink, fill))
    gy = y + (h - 48) // 2
    if gy < y + 8:
        gy = y + 8
    glyphs(d, keys, KEY_R, KEY_L, gy, checks)
    glyphs(d, pads, PAD_R, PAD_R - PAD_W, gy, checks)
    ratios.append(ui.contrast(ui.KEY_INK, ui.KEY_CAP))
    return y + h + 8


def bar(img, y, reset_word, hot_reset, ratios, checks):
    d = ImageDraw.Draw(img)
    gap = 16
    w = (ROW_R - ROW_L - gap) // 2
    face = ui.font(ui.FONT_D, 36)
    h = 72
    for x, word, hot in ((ROW_L, reset_word, hot_reset), (ROW_L + w + gap, "Back", False)):
        fill = ui.HOT if hot else ui.NAVY
        ink = ui.INK if hot else ui.CREAM
        ui.rounded(d, (x, y, x + w, y + h), 16, fill, ui.GOLD if hot else ui.STROKE, 4 if hot else 2)
        ui.draw_glyph(d, "space" if word != "Back" else "esc", x + 16, y + 14)
        ui.draw_glyph(d, "a" if word != "Back" else "b", x + 88, y + 14)
        d.text((x + 150, y + 16), word, font=face, fill=ink)
        box = (x + 140, y + 4, x + w - 8, y + h - 4)
        ok, bb = ui.text_inside(d, (x + 150, y + 16), word, face, box)
        if not ok:
            raise SystemExit("bar clip %s %s" % (word, bb))
        ratios.append(ui.contrast(ink, fill))
        checks.append(((x + 150, y + 16), word, face, ink, fill))
    return y + h


def choice(img, y, pick, ratios, checks):
    d = ImageDraw.Draw(img)
    gap = 16
    w = (ROW_R - ROW_L - gap) // 2
    face = ui.font(ui.FONT_D, 40)
    h = 72
    for i, word in enumerate(("Swap", "Cancel")):
        x = ROW_L + i * (w + gap)
        hot = i == pick
        fill = ui.HOT if hot else ui.NAVY
        ink = ui.INK if hot else ui.CREAM
        ui.rounded(d, (x, y, x + w, y + h), 16, fill, ui.GOLD if hot else ui.STROKE, 4 if hot else 2)
        tw = d.textlength(word, font=face)
        tx = x + (w - tw) / 2
        d.text((tx, y + 12), word, font=face, fill=ink)
        ok, bb = ui.text_inside(d, (tx, y + 12), word, face, (x + 8, y + 4, x + w - 8, y + h - 4))
        if not ok:
            raise SystemExit("choice clip %s %s" % (word, bb))
        ratios.append(ui.contrast(ink, fill))
        checks.append(((tx, y + 12), word, face, ink, fill))
    return y + h


def score_text(img, xy, text, face, fill, bg):
    x, y = xy
    probe = Image.new("RGB", (img.size[0], img.size[1]), bg)
    pd = ImageDraw.Draw(probe)
    pd.text((x, y), text, font=face, fill=fill)
    crop_box = pd.textbbox((x, y), text, font=face)
    pad = 1
    box = (
        max(0, crop_box[0] - pad),
        max(0, crop_box[1] - pad),
        min(img.size[0], crop_box[2] + pad),
        min(img.size[1], crop_box[3] + pad),
    )
    a = img.crop(box).convert("RGB")
    b = probe.crop(box)
    if a.size != b.size or a.size[0] < 2:
        return 999
    ap = list(a.getdata())
    bp = list(b.getdata())
    n = 0
    acc = 0
    for i in range(len(bp)):
        if bp[i] == bg:
            continue
        n += 1
        acc += abs(ap[i][0] - bp[i][0]) + abs(ap[i][1] - bp[i][1]) + abs(ap[i][2] - bp[i][2])
    if n < 8:
        return 999
    return acc / (n * 3)


def verify(img, checks, label):
    worst = 0
    for xy, text, face, fill, bg in checks:
        score = score_text(img, xy, text, face, fill, bg)
        if score > 12:
            raise SystemExit("text miss %s %r score %.1f" % (label, text, score))
        worst = max(worst, score)
    return worst


def full_list():
    ratios = []
    checks = []
    heights = []
    rows = []
    for title, keys, pads, sub in ACTIONS + NOTES:
        h = 96 if sub else 64
        heights.append(h)
        rows.append((title, sub, keys, pads, h, False))
    body = 168 + 72 + 12 + 64
    for h in heights:
        body += h + 8
    body += 16 + 72 + 110
    img = canvas(body)
    banner = "Keyboard and pad glyphs. Space always jumps."
    ui.header(img, "Controls", banner)
    ratios.append(ui.contrast(ui.GOLD, ui.NAVY))
    checks.append(((318, 96), banner, ui.font(ui.FONT_B, 28), ui.GOLD, ui.NAVY))
    y = 168
    y = seats(img, y, 0, ratios) + 12
    columns(ImageDraw.Draw(img), y, 56, ratios, checks)
    y += 64
    for title, sub, keys, pads, h, hot in rows:
        y = row(img, y, h, title, sub, hot, keys, pads, ratios, checks)
    y += 8
    bar(img, y, "Reset bindings", False, ratios, checks)
    footer_at(img)
    ratios.append(ui.contrast(ui.CREAM, (20, 41, 92)))
    return img, min(ratios), checks


def capture():
    ratios = []
    checks = []
    img = canvas(ui.H)
    banner = "Esc or B cancels. This waits 5 seconds."
    ui.header(img, "Controls", banner)
    ratios.append(ui.contrast(ui.GOLD, ui.NAVY))
    checks.append(((318, 96), banner, ui.font(ui.FONT_B, 28), ui.GOLD, ui.NAVY))
    y = 168
    y = seats(img, y, 0, ratios) + 12
    columns(ImageDraw.Draw(img), y, 56, ratios, checks)
    y += 64
    shown = [
        ("Sprint", "", ["leftShift"], ["leftShoulder"], False),
        ("Pause", "", ["escape"], ["start"], False),
        ("Minimap", "", ["m"], ["select"], False),
        ("Grapple", "Press any button to bind", ["mouseRight"], ["leftTrigger"], True),
    ]
    for title, sub, keys, pads, hot in shown:
        y = row(img, y, 108, title, sub, hot, keys, pads, ratios, checks)
    bar(img, 900, "Reset bindings", False, ratios, checks)
    footer_at(img)
    return img, min(ratios), checks


def conflict():
    ratios = []
    checks = []
    img = canvas(ui.H)
    banner = "Slide and Punch / tag use that button."
    ui.header(img, "Controls", banner)
    ratios.append(ui.contrast(ui.GOLD, ui.NAVY))
    checks.append(((318, 96), banner, ui.font(ui.FONT_B, 28), ui.GOLD, ui.NAVY))
    y = 168
    y = seats(img, y, 0, ratios) + 12
    y = choice(img, y, 0, ratios, checks) + 12
    shown = [
        ("Cling hold", ["holdIntoWall"], ["leftStickHold"], False),
        ("Slide", ["leftCtrl", "c"], ["buttonEast"], True),
        ("Air dash", ["q"], ["rightShoulder"], False),
        ("Punch / tag", ["mouseLeft", "e"], ["buttonWest"], True),
    ]
    for title, keys, pads, hot in shown:
        y = row(img, y, 96, title, "", hot, keys, pads, ratios, checks)
    bar(img, 900, "Reset bindings", False, ratios, checks)
    footer_at(img)
    return img, min(ratios), checks


def seats_still():
    ratios = []
    checks = []
    img = canvas(ui.H)
    banner = "Keyboard and pad glyphs. Space always jumps."
    ui.header(img, "Controls", banner)
    ratios.append(ui.contrast(ui.GOLD, ui.NAVY))
    checks.append(((318, 96), banner, ui.font(ui.FONT_B, 28), ui.GOLD, ui.NAVY))
    y = 168
    y = seats(img, y, 2, ratios) + 12
    columns(ImageDraw.Draw(img), y, 56, ratios, checks)
    y += 64
    shown = [
        ("Look", ["mouse"], ["rightStick"], False),
        ("Jump", ["space"], ["buttonNorth"], True),
        ("Cling hold", ["holdIntoWall"], ["leftStickHold"], False),
        ("Air dash", ["q"], ["rightShoulder"], False),
        ("Sprint", ["leftShift"], ["leftShoulder"], False),
    ]
    for title, keys, pads, hot in shown:
        y = row(img, y, 96, title, "", hot, keys, pads, ratios, checks)
    bar(img, 900, "Reset bindings", False, ratios, checks)
    footer_at(img)
    return img, min(ratios), checks


def main():
    jobs = [
        ("18-binds-composite.png", full_list),
        ("18-capture-composite.png", capture),
        ("18-conflict-composite.png", conflict),
        ("18-seats-composite.png", seats_still),
    ]
    notes = []
    worst = 99
    for name, fn in jobs:
        img, ratio, checks = fn()
        if ratio < 4.5:
            raise SystemExit("%s contrast %.2f" % (name, ratio))
        miss = verify(img, checks, name)
        ui.save(img, name, notes)
        worst = min(worst, ratio)
        print("  contrast %.2f text %.1f" % (ratio, miss))
    print("min contrast", round(worst, 2))


if __name__ == "__main__":
    main()
