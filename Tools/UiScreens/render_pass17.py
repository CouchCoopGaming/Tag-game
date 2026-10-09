#!/usr/bin/env python3
"""Pass 17 controls stills. Imports the pass 16 painter and does not rewrite those files."""

import os

import render_screens2 as ui
from PIL import Image, ImageDraw

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass17")

ACTIONS = [
    ("Move", ["wasd"], ["leftStick"], ""),
    ("Look", ["mouse"], ["rightStick"], ""),
    ("Jump", ["space"], ["buttonSouth"], ""),
    ("Cling hold", ["holdIntoWall"], ["leftStickHold"], "Wall climb and wall run need this hold. Wall jump is this hold plus Jump."),
    ("Slide", ["leftCtrl", "c"], ["buttonEast"], ""),
    ("Air dash", ["q", "leftAlt"], ["rightShoulder"], ""),
    ("Punch / tag", ["mouseLeft", "e"], ["buttonWest"], ""),
    ("Sprint", ["leftShift", "leftAlt"], ["leftShoulder"], ""),
    ("Pause", ["escape"], ["start"], ""),
    ("Minimap", ["m"], ["select"], ""),
    ("Arena 1", ["alpha1"], ["dpadLeft"], ""),
    ("Arena 2", ["alpha2"], ["dpadRight"], ""),
    ("Arena 3", ["alpha3"], ["dpadUp"], ""),
]
NOTES = [
    ("Grapple", ["mouseRight"], [], "RMB. Click pulls. Second click within 0.28 s releases. Left hand. No pad bind."),
    ("Zip", [], [], "Hold cling to grab. Jump to drop."),
    ("Launch pad", [], [], "Walk on. No button."),
]


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
    x = 120
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


def columns(d, y, h, ratios):
    key_box = (980, y, 1460, y + h)
    pad_box = (1488, y, 1760, y + h)
    ui.rounded(d, key_box, 12, ui.NAVY)
    ui.rounded(d, pad_box, 12, ui.NAVY)
    face = ui.font(ui.FONT_B, 28)
    key = "Keyboard / Mouse"
    pad = "Pad"
    d.text((996, y + 18), key, font=face, fill=ui.CREAM)
    d.text((1600, y + 18), pad, font=face, fill=ui.CREAM)
    for text, xy, box in ((key, (996, y + 18), key_box), (pad, (1600, y + 18), pad_box)):
        ok, bb = ui.text_inside(d, xy, text, face, box)
        if not ok:
            raise SystemExit("column clip %s %s" % (text, bb))
    ratios.append(ui.contrast(ui.CREAM, ui.NAVY))


def glyphs(d, tokens, right, y):
    gx = right
    for token in reversed(tokens):
        w = ui.mark_width(token)
        gx -= w
        ui.draw_token(d, token, gx, y)
        gx -= 8
    return gx


def row(img, y, h, title, sub, hot, keys, pads, ratios, checks):
    fill = ui.HOT if hot else ui.PANEL
    ink = ui.INK if hot else ui.CREAM
    sub_ink = ui.INK if hot else ui.MUTE
    ratios.append(ui.button(img, (120, y, 1680, y + h), title, sub, hot, right=560))
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
        while sub_size > 18 and ImageDraw.Draw(img).textlength(sub, font=sf) > 972:
            sub_size -= 1
            sf = ui.font(ui.FONT_B, sub_size)
        checks.append(((148, sub_y), sub, sf, sub_ink, fill))
    gy = y + (h - 48) // 2
    if gy < y + 8:
        gy = y + 8
    glyphs(d, keys, 1468, gy)
    glyphs(d, pads, 1748, gy)
    ratios.append(ui.contrast(ui.KEY_INK, ui.KEY_CAP))
    return y + h + 8


def bar(img, y, reset_word, hot_reset, ratios, checks):
    d = ImageDraw.Draw(img)
    gap = 16
    w = (1680 - 120 - gap) // 2
    chips = [(120, reset_word, hot_reset), (120 + w + gap, "Back", not hot_reset and False)]
    face = ui.font(ui.FONT_D, 36)
    h = 72
    for x, word, hot in ((120, reset_word, hot_reset), (120 + w + gap, "Back", False)):
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
    w = (1680 - 120 - gap) // 2
    face = ui.font(ui.FONT_D, 40)
    h = 72
    for i, word in enumerate(("Swap", "Cancel")):
        x = 120 + i * (w + gap)
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
    # Compare only pixels the probe painted away from the flat background.
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
    for title, keys, pads, sub in ACTIONS:
        if title == "Jump":
            keys = ["space", "f"]
            sub = "Space always jumps."
        h = 96 if sub else 64
        heights.append(h)
        rows.append((title, sub, keys, pads, h, title == "Jump"))
    for title, keys, pads, sub in NOTES:
        h = 96
        heights.append(h)
        rows.append((title, sub, keys, pads, h, False))
    body = 168 + 72 + 12 + 64
    for h in heights:
        body += h + 8
    body += 16 + 72 + 110
    img = canvas(body)
    banner = "Space always jumps. Added F as a second Jump key."
    ui.header(img, "Controls", banner)
    ratios.append(ui.contrast(ui.GOLD, ui.NAVY))
    checks.append(((318, 96), banner, ui.font(ui.FONT_B, 28), ui.GOLD, ui.NAVY))
    y = 168
    y = seats(img, y, 0, ratios) + 12
    columns(ImageDraw.Draw(img), y, 56, ratios)
    y += 64
    for title, sub, keys, pads, h, hot in rows:
        y = row(img, y, h, title, sub, hot, keys, pads, ratios, checks)
    y += 8
    bar(img, y, "Reset bindings?", True, ratios, checks)
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
    columns(ImageDraw.Draw(img), y, 56, ratios)
    y += 64
    shown = [
        ("Move", "", ["wasd"], ["leftStick"], False),
        ("Look", "", ["mouse"], ["rightStick"], False),
        ("Jump", "Press any button to bind", ["space"], ["buttonSouth"], True),
        ("Cling hold", "Wall climb and wall run need this hold. Wall jump is this hold plus Jump.", ["holdIntoWall"], ["leftStickHold"], False),
    ]
    for title, sub, keys, pads, hot in shown:
        y = row(img, y, 108, title, sub, hot, keys, pads, ratios, checks)
    bar(img, 900, "Reset", False, ratios, checks)
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
        ("Air dash", ["q", "leftAlt"], ["rightShoulder"], False),
        ("Punch / tag", ["mouseLeft", "e"], ["buttonWest"], True),
    ]
    for title, keys, pads, hot in shown:
        y = row(img, y, 96, title, "", hot, keys, pads, ratios, checks)
    bar(img, 900, "Reset", False, ratios, checks)
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
    columns(ImageDraw.Draw(img), y, 56, ratios)
    y += 64
    # P3's pad jump is North. The keyboard column stays Space.
    shown = [
        ("Move", ["wasd"], ["leftStick"], False),
        ("Jump", ["space"], ["buttonNorth"], True),
        ("Slide", ["leftCtrl", "c"], ["buttonEast"], False),
        ("Punch / tag", ["mouseLeft", "e"], ["buttonWest"], False),
        ("Sprint", ["leftShift", "leftAlt"], ["leftShoulder"], False),
    ]
    for title, keys, pads, hot in shown:
        y = row(img, y, 96, title, "", hot, keys, pads, ratios, checks)
    bar(img, 900, "Reset", False, ratios, checks)
    footer_at(img)
    return img, min(ratios), checks


def main():
    jobs = [
        ("17-binds-composite.png", full_list),
        ("17-capture-composite.png", capture),
        ("17-conflict-composite.png", conflict),
        ("17-seats-composite.png", seats_still),
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
