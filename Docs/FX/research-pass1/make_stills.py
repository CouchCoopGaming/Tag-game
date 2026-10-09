#!/usr/bin/env python3
"""Labeled composites for FX research pass 1.

Each PNG is 1600x900 and contains one 960x540 cell at 1:1 pixels.
The body is drawn at 108 px, one fifth of the cell, as a stand-in.
These are not Unity captures.
"""

import math
import os

from PIL import Image, ImageDraw, ImageFont

W, H = 1600, 900
CELL = (960, 540)
CELL_XY = (36, 168)
SS = 2

FONT = "/usr/share/fonts/truetype/macos/Inter-Regular.ttf"
FONT_B = "/usr/share/fonts/truetype/macos/Inter-Bold.ttf"
FONT_M = "/usr/share/fonts/truetype/macos/Inter-SemiBold.ttf"

SHIRT = (240, 107, 36)
BODY = (244, 241, 234)
SHORTS = (36, 38, 46)
TEAL = (46, 196, 182)
INK = (22, 18, 16)
CREAM = (247, 244, 238)
EDGE = (28, 24, 22)


def font(size, bold=False, semi=False):
    path = FONT_B if bold else FONT_M if semi else FONT
    return ImageFont.truetype(path, size)


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def sky(img):
    px = img.load()
    w, h = img.size
    top = (126, 184, 214)
    bot = (196, 220, 228)
    for y in range(h):
        c = lerp(top, bot, y / max(1, h - 1))
        for x in range(w):
            px[x, y] = c


def ground(draw, w, h, horizon):
    draw.rectangle((0, horizon, w, h), fill=(132, 128, 120))
    for i in range(8):
        y = horizon + 18 + i * 28
        shade = 118 + (i % 2) * 10
        draw.rectangle((0, y, w, y + 10), fill=(shade, shade - 4, shade - 8))
    draw.rectangle((0, horizon, w, horizon + 6), fill=(98, 94, 88))


def brick_wall(draw, x0, x1, y0, y1):
    draw.rectangle((x0, y0, x1, y1), fill=(146, 78, 62))
    brick_h = 22
    brick_w = 46
    y = y0
    row = 0
    while y < y1:
        off = 0 if row % 2 == 0 else brick_w // 2
        x = x0 - off
        while x < x1:
            draw.rectangle(
                (max(x0, x + 1), y + 1, min(x1, x + brick_w - 2), min(y1, y + brick_h - 2)),
                fill=(168, 92, 72) if (row + x // brick_w) % 3 else (132, 64, 52),
            )
            x += brick_w
        y += brick_h
        row += 1
    draw.rectangle((x1 - 8, y0, x1, y1), fill=(90, 48, 40))


def disc(draw, xy, r, fill):
    x, y = xy
    draw.ellipse((x - r, y - r, x + r, y + r), fill=fill)


def limb(draw, a, b, width, fill, outline=None):
    draw.line((a, b), fill=fill, width=width)
    r = width // 2
    disc(draw, a, r, fill)
    disc(draw, b, r, fill)
    if outline:
        draw.line((a, b), fill=outline, width=max(1, width // 5))


def runner(draw, foot, height, face=1, lean=0):
    """foot is (x, y) at the ground. height is body height in this image's pixels."""
    fx, fy = foot
    s = height / 108.0
    hip = (fx + lean, fy - int(46 * s))
    chest = (fx + lean + int(6 * face * s), fy - int(78 * s))
    head = (chest[0] + int(4 * face * s), fy - int(98 * s))
    hand_f = (chest[0] + int(28 * face * s), chest[1] + int(8 * s))
    hand_b = (chest[0] - int(16 * face * s), chest[1] + int(14 * s))
    knee_f = (fx + int(16 * face * s), fy - int(22 * s))
    knee_b = (fx - int(14 * face * s), fy - int(20 * s))
    foot_f = (fx + int(22 * face * s), fy)
    foot_b = (fx - int(18 * face * s), fy)
    w = max(4, int(7 * s))
    limb(draw, hip, knee_b, w, SHORTS)
    limb(draw, knee_b, foot_b, w, BODY)
    limb(draw, hip, knee_f, w, SHORTS)
    limb(draw, knee_f, foot_f, w, BODY)
    limb(draw, chest, hand_b, max(3, w - 1), BODY)
    limb(draw, chest, hand_f, max(3, w - 1), SHIRT)
    torso_w = int(16 * s)
    draw.rounded_rectangle(
        (chest[0] - torso_w, chest[1] - int(6 * s), chest[0] + torso_w, hip[1] + int(6 * s)),
        radius=int(6 * s),
        fill=SHIRT,
    )
    disc(draw, head, int(11 * s), BODY)
    disc(draw, (head[0] + int(6 * face * s), head[1] - int(2 * s)), int(3 * s), TEAL)
    disc(draw, chest, int(3 * s), TEAL)
    return {"head": head, "chest": chest, "hip": hip, "hand": hand_f, "foot": foot_f}


def starburst(draw, center, radius, fill, outline, spikes=12):
    cx, cy = center
    pts = []
    for i in range(spikes * 2):
        ang = -math.pi / 2 + i * math.pi / spikes
        r = radius if i % 2 == 0 else radius * 0.42
        pts.append((cx + math.cos(ang) * r, cy + math.sin(ang) * r))
    draw.polygon(pts, fill=fill, outline=outline)


def scale_bar(draw, x, y):
    draw.rectangle((x, y, x + 96, y + 6), fill=CREAM)
    draw.rectangle((x, y, x + 48, y + 6), fill=SHIRT)
    draw.text((x, y + 10), "48 px", font=font(16, semi=True), fill=EDGE)


def cell_frame(base):
    draw = ImageDraw.Draw(base)
    x, y = CELL_XY
    w, h = CELL
    draw.rectangle((x - 3, y - 3, x + w + 2, y + h + 2), outline=CREAM, width=3)
    draw.rectangle((x, y + h - 28, x + 168, y + h), fill=(20, 18, 16))
    draw.text((x + 8, y + h - 24), "960 x 540 cell, 1:1", font=font(16, semi=True), fill=CREAM)


def page(title, rank, ref, cost, risk):
    img = Image.new("RGB", (W, H), (28, 26, 24))
    draw = ImageDraw.Draw(img)
    draw.text((36, 28), "TAG  ·  FX RESEARCH  ·  PASS 1", font=font(18, semi=True), fill=(186, 168, 140))
    draw.text((36, 56), title, font=font(32, bold=True), fill=CREAM)
    draw.text((36, 100), "Rank %d for C2. Composite, not a Unity capture. Body drawn at 108 px." % rank, font=font(18), fill=(210, 198, 180))
    return img, draw


def wrap_text(draw, text, xy, fnt, fill, width):
    words = text.split()
    lines = []
    cur = ""
    for word in words:
        trial = word if not cur else cur + " " + word
        if draw.textlength(trial, font=fnt) <= width:
            cur = trial
        else:
            if cur:
                lines.append(cur)
            cur = word
    if cur:
        lines.append(cur)
    x, y = xy
    for line in lines:
        draw.text((x, y), line, font=fnt, fill=fill)
        y += fnt.size + 6
    return y


def down(big):
    return big.resize(CELL, Image.Resampling.LANCZOS)


def paste_cell(page_img, cell):
    page_img.paste(cell, CELL_XY)
    cell_frame(page_img)


def edge_cell():
    big = Image.new("RGB", (CELL[0] * SS, CELL[1] * SS))
    sky(big)
    d = ImageDraw.Draw(big)
    ground(d, big.width, big.height, int(300 * SS))
    brick_wall(d, int(40 * SS), int(220 * SS), int(120 * SS), int(300 * SS))
    # Margin streaks only. Center of the pane stays clear.
    def streak(x0, y, length, thick, alpha_rgb):
        d.line((x0, y, x0 + length, y), fill=alpha_rgb, width=thick)
        disc(d, (x0, y), thick // 2, alpha_rgb)
        disc(d, (x0 + length, y), max(1, thick // 3), alpha_rgb)

    margin = int(115 * SS)
    seat = SHIRT
    ink = (40, 18, 10)

    def seat_streak(x0, y, length, thick):
        streak(x0, y, length, thick + int(3 * SS), ink)
        streak(x0, y, length, thick, seat)

    for i, y in enumerate((150, 190, 240, 400, 450, 490)):
        length = int((70 + (i % 3) * 36) * SS)
        thick = int((6 if i % 2 == 0 else 4) * SS)
        seat_streak(int(18 * SS), int(y * SS), min(length, margin - int(28 * SS)), thick)
        seat_streak(big.width - margin + int(16 * SS), int((y + 16) * SS), int(length * 0.72), thick)
    runner(d, (int(430 * SS), int(430 * SS)), 108 * SS, face=1, lean=int(8 * SS))
    # Guide for the 12% margin. Dotted so it is not another streak.
    for y in range(0, big.height, int(14 * SS)):
        d.line((margin, y, margin, y + int(6 * SS)), fill=(255, 248, 230), width=2)
        d.line((big.width - margin, y, big.width - margin, y + int(6 * SS)), fill=(255, 248, 230), width=2)
    cell = down(big)
    cd = ImageDraw.Draw(cell)
    cd.text((8, 8), "owner pane only", font=font(18, semi=True), fill=EDGE)
    cd.text((122, 8), "12% margin", font=font(16), fill=EDGE)
    scale_bar(cd, 790, 490)
    return cell


def ribbon_cell():
    big = Image.new("RGB", (CELL[0] * SS, CELL[1] * SS))
    sky(big)
    d = ImageDraw.Draw(big)
    ground(d, big.width, big.height, int(430 * SS))
    brick_wall(d, int(70 * SS), int(250 * SS), int(40 * SS), int(430 * SS))
    # Ribbon path along the wall, fading toward the tail.
    pts = []
    for i in range(18):
        t = i / 17
        x = int((150 + t * 8) * SS)
        y = int((400 - t * 280) * SS)
        pts.append((x, y))
    # Draw tail first, thinner and darker, then the live end.
    for i in range(len(pts) - 1):
        t = i / (len(pts) - 2)
        thick = int((6 + t * 8) * SS)
        col = lerp((90, 40, 28), SHIRT, 0.35 + 0.65 * t)
        d.line((pts[i], pts[i + 1]), fill=(20, 12, 10), width=thick + int(4 * SS))
        d.line((pts[i], pts[i + 1]), fill=col, width=thick)
    # Existing kit ring, small, at the start of the run.
    ring_c = pts[2]
    r = int(14 * SS)
    d.ellipse((ring_c[0] - r, ring_c[1] - r, ring_c[0] + r, ring_c[1] + r), outline=(255, 236, 210), width=max(2, int(2 * SS)))
    runner(d, (int(168 * SS), int(150 * SS)), 108 * SS, face=1, lean=int(18 * SS))
    cell = down(big)
    cd = ImageDraw.Draw(cell)
    cd.rounded_rectangle((250, 250, 520, 278), radius=4, fill=(22, 18, 16))
    cd.text((258, 254), "already in the kit: small ring", font=font(16), fill=CREAM)
    cd.rounded_rectangle((250, 62, 490, 90), radius=4, fill=(247, 244, 238))
    cd.text((258, 66), "new: seat ribbon, 10 px", font=font(18, semi=True), fill=EDGE)
    scale_bar(cd, 790, 490)
    return cell


def ink_cell():
    big = Image.new("RGB", (CELL[0] * SS, CELL[1] * SS))
    sky(big)
    d = ImageDraw.Draw(big)
    ground(d, big.width, big.height, int(340 * SS))
    brick_wall(d, int(40 * SS), int(200 * SS), int(80 * SS), int(340 * SS))
    # Two bodies. Ink card is body-sized and sits behind the contact.
    a = runner(d, (int(380 * SS), int(470 * SS)), 108 * SS, face=1, lean=int(10 * SS))
    b = runner(d, (int(560 * SS), int(470 * SS)), 108 * SS, face=-1, lean=int(-8 * SS))
    # Rebuild a flat ink shape over the pair, then redraw the bodies on top
    # so the card reads as a plate behind them.
    # The runners were already drawn. Stamp the card first by redrawing the scene
    # order: we paint the card now, then redraw both runners.
    contact = (
        (a["hand"][0] + b["hand"][0]) // 2,
        (a["hand"][1] + b["hand"][1]) // 2,
    )
    card = [
        (int(340 * SS), int(250 * SS)),
        (int(430 * SS), int(220 * SS)),
        (int(560 * SS), int(230 * SS)),
        (int(650 * SS), int(280 * SS)),
        (int(670 * SS), int(390 * SS)),
        (int(600 * SS), int(470 * SS)),
        (int(470 * SS), int(490 * SS)),
        (int(340 * SS), int(450 * SS)),
        (int(310 * SS), int(340 * SS)),
    ]
    d.polygon(card, fill=INK)
    # Light rim by stroking the polygon edges thickly in cream, then refill.
    d.line(card + [card[0]], fill=CREAM, width=int(8 * SS))
    d.polygon(card, fill=INK)
    runner(d, (int(380 * SS), int(470 * SS)), 108 * SS, face=1, lean=int(10 * SS))
    runner(d, (int(560 * SS), int(470 * SS)), 108 * SS, face=-1, lean=int(-8 * SS))
    # Existing burst, smaller than the card, at the fists.
    starburst(d, contact, int(34 * SS), (255, 214, 64), INK, spikes=10)
    d.text((contact[0] - int(22 * SS), contact[1] - int(14 * SS)), "POW!", font=font(int(22 * SS), bold=True), fill=INK)
    cell = down(big)
    cd = ImageDraw.Draw(cell)
    cd.text((8, 8), "new: ink card, no freeze", font=font(18, semi=True), fill=EDGE)
    cd.text((8, 32), "small POW is the burst that already ships", font=font(16), fill=EDGE)
    scale_bar(cd, 790, 490)
    return cell


def finish(path, title, rank, ref, cost, risk, note, cell):
    img, draw = page(title, rank, ref, cost, risk)
    paste_cell(img, cell)
    draw = ImageDraw.Draw(img)
    y = 190
    y = wrap_text(draw, ref, (1028, y), font(20), CREAM, 530)
    draw.text((1028, y + 18), "COST", font=font(14, semi=True), fill=(186, 168, 140))
    y = wrap_text(draw, cost, (1028, y + 40), font(20), CREAM, 530)
    draw.text((1028, y + 18), "READABILITY", font=font(14, semi=True), fill=(186, 168, 140))
    y = wrap_text(draw, risk, (1028, y + 40), font(20), CREAM, 530)
    draw.text((1028, y + 18), "LEFT ALONE", font=font(14, semi=True), fill=(186, 168, 140))
    wrap_text(draw, note, (1028, y + 40), font(20), CREAM, 530)
    img.save(path, "PNG")
    print(path, img.size)


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    finish(
        os.path.join(here, "01-edge-streaks.png"),
        "1  ·  Owner-pane edge streaks",
        1,
        "Spider-Man sells speed with the camera. Tag cannot kick field of view. The streak stays in this pane.",
        "0 particles. 1 batched mesh on the owner camera. Estimated.",
        "Low in the margin. High if the same lines are drawn in the world, where all four panes see them.",
        "Fov pop, shake, and slow motion stay 0. Speed lines in the world stay off by default.",
        edge_cell(),
    )
    finish(
        os.path.join(here, "02-wall-ribbon.png"),
        "2  ·  Wall-run seat ribbon",
        2,
        "Jet Set Radio's mark on the path, and Splatoon's rule that the activity has to stay readable. No camera tilt.",
        "0 particles. 1 alpha-clipped strip. Estimated 1 draw per camera that can see it.",
        "The 10 px stroke is the test. A hairline disappears on brick. One ribbon per runner, then it fades.",
        "Wall-run speed stays 9.5. The small ring at the start is the effect that already ships.",
        ribbon_cell(),
    )
    finish(
        os.path.join(here, "03-contact-ink.png"),
        "3  ·  Contact ink card",
        3,
        "Smash and Rivals show one contact picture. Sifu poses the hit. Tag does not freeze time.",
        "1 quad. 0 particles. Estimated 1 draw per camera that sees the hit. Dies in a few frames.",
        "A full-pane flash would blind the split. The card is body-sized and Reduced flashing hides it.",
        "No hitstop. The POW burst and the comic toggle stay. A miss does not get this card.",
        ink_cell(),
    )


if __name__ == "__main__":
    main()
