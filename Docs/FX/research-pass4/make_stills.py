#!/usr/bin/env python3
"""Labeled composites for FX research pass 4.

01 and 02 contain one 640x360 cell at 1:1. That is a 1280x720 quarter.
03 uses the 108 px body from pass 3. These are not Unity captures.
"""

import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "research-pass1"))
import make_stills as p1

SEATS = (
    (242, 71, 82),
    (64, 140, 255),
    (255, 209, 38),
    (64, 230, 115),
)
INK = (20, 18, 16)
CHIP = (16, 18, 22)


def save(img, path):
    img.save(path, "PNG", optimize=True, compress_level=9)
    print(path, img.size, os.path.getsize(path))


def chip(draw, x, y, text, w=None):
    font = p1.font(15, semi=True)
    if w is None:
        w = int(draw.textlength(text, font=font)) + 16
    draw.rounded_rectangle((x, y, x + w, y + 24), radius=4, fill=CHIP)
    draw.text((x + 8, y + 3), text, font=font, fill=p1.CREAM)
    return w


def page(title, kicker):
    img = Image.new("RGB", (1280, 760), (28, 26, 24))
    draw = ImageDraw.Draw(img)
    draw.text((24, 18), "TAG  ·  FX RESEARCH  ·  PASS 4", font=p1.font(16, semi=True), fill=(186, 168, 140))
    draw.text((24, 42), title, font=p1.font(28, bold=True), fill=p1.CREAM)
    draw.text((24, 80), kicker, font=p1.font(16), fill=(210, 198, 180))
    return img, draw


def notes(img, lines):
    draw = ImageDraw.Draw(img)
    y = 140
    font = p1.font(16)
    for line in lines:
        y = p1.wrap_text(draw, line, (700, y), font, p1.CREAM, 550)
        y += 14
    return y


def streak(draw, x, y, length, fill):
    # Inclusive rects. Core is `length` by 4, with a 1 px outline.
    draw.rectangle((x, y, x + length + 1, y + 5), fill=INK)
    draw.rectangle((x + 1, y + 1, x + length, y + 4), fill=fill)


def dotted(draw, x0, y0, x1, y1):
    if x0 == x1:
        y = y0
        while y < y1:
            draw.line((x0, y, x0, min(y1, y + 4)), fill=(247, 244, 238), width=1)
            y += 8
    else:
        x = x0
        while x < x1:
            draw.line((x, y0, min(x1, x + 4), y0), fill=(247, 244, 238), width=1)
            x += 8


def edge_cell():
    cell = Image.new("RGB", (640, 360))
    p1.sky(cell)
    d = ImageDraw.Draw(cell)
    p1.ground(d, 640, 360, 210)
    # Outer 12%: 77 px left and right, 43 px top and bottom.
    dotted(d, 77, 0, 77, 360)
    dotted(d, 563, 0, 563, 360)
    dotted(d, 0, 43, 640, 43)
    dotted(d, 0, 317, 640, 317)
    p1.runner(d, (300, 300), 72, face=1, lean=4)
    # Four seat colors in the top band. One left, one right. Count is 6.
    for i, color in enumerate(SEATS):
        streak(d, 16 + i * 150, 16, 48, color)
    streak(d, 12, 168, 48, SEATS[0])
    streak(d, 580, 168, 48, SEATS[1])
    chip(d, 88, 52, "6 streaks  ·  center stays empty")
    chip(d, 16, 328, "640 x 360  ·  12% frame")
    return cell


def word(draw, xy, text, scale, fill):
    font = p1.font(max(12, int(18 * scale)), bold=True)
    draw.text(xy, text, font=font, fill=fill)


def ink_cell():
    cell = Image.new("RGB", (640, 360), (168, 176, 186))
    d = ImageDraw.Draw(cell)
    # 0.00 s. Card is 0.42 by 0.28 of this pane: 269 x 101. Opacity is painted
    # as a mix of black at 0.72 over the gray field.
    card = tuple(int(168 * 0.28 + 8 * 0.72) for _ in range(3))
    d.rounded_rectangle((16, 70, 16 + 269, 70 + 101), radius=14, fill=(236, 232, 224))
    d.rounded_rectangle((22, 76, 16 + 263, 76 + 89), radius=10, fill=card)
    word(d, (78, 100), "THUD!", 1.6, (255, 236, 210))
    chip(d, 16, 40, "0.00 s  ·  card 0.72")
    # 0.08 s. Card is gone. The word remains.
    word(d, (330, 90), "THUD!", 2.2, (255, 214, 160))
    chip(d, 320, 40, "0.08 s  ·  card gone")
    # Hold. Still no card.
    word(d, (470, 200), "SLAM!", 2.0, (255, 120, 80))
    chip(d, 430, 168, "wall stacks")
    d.text((400, 270), "WHIFF!", font=p1.font(28, bold=True), fill=(120, 128, 136))
    chip(d, 520, 274, "no card")
    chip(d, 16, 310, "640 x 360  ·  seconds, not frames")
    return cell


def limb(draw, a, b, width, fill):
    draw.line((a, b), fill=INK, width=width + 6)
    draw.line((a, b), fill=fill, width=width)
    r = max(4, width // 2)
    p1.disc(draw, a, r + 2, INK)
    p1.disc(draw, b, r + 2, INK)
    p1.disc(draw, a, r, fill)
    p1.disc(draw, b, r, fill)


def figure(draw, foot, height, kind):
    fx, fy = foot
    s = height / 108.0
    hip = (fx, fy - int(46 * s))
    chest = (fx, fy - int(78 * s))
    head = (fx, fy - int(98 * s))
    w = max(5, int(8 * s))
    if kind == "v":
        hands = ((fx - int(46 * s), chest[1] - int(36 * s)), (fx + int(46 * s), chest[1] - int(36 * s)))
        knees = ((fx - int(18 * s), fy - int(22 * s)), (fx + int(18 * s), fy - int(22 * s)))
        feet = ((fx - int(32 * s), fy), (fx + int(32 * s), fy))
    elif kind == "star":
        hands = ((fx - int(70 * s), chest[1] - int(50 * s)), (fx + int(70 * s), chest[1] - int(50 * s)))
        knees = ((fx - int(40 * s), fy - int(52 * s)), (fx + int(40 * s), fy - int(52 * s)))
        feet = ((fx - int(58 * s), fy - int(34 * s)), (fx + int(58 * s), fy - int(34 * s)))
    elif kind == "spin":
        hands = ((fx - int(50 * s), chest[1] - int(20 * s)), (fx + int(44 * s), chest[1] - int(34 * s)))
        knees = ((fx - int(10 * s), fy - int(24 * s)), (fx + int(14 * s), fy - int(20 * s)))
        feet = ((fx - int(16 * s), fy), (fx + int(22 * s), fy))
    else:
        hands = ((fx - int(8 * s), chest[1] + int(6 * s)), (fx + int(8 * s), chest[1] + int(6 * s)))
        knees = ((fx - int(8 * s), fy - int(22 * s)), (fx + int(8 * s), fy - int(22 * s)))
        feet = ((fx - int(10 * s), fy), (fx + int(10 * s), fy))
    limb(draw, hip, knees[0], w, p1.SHORTS)
    limb(draw, knees[0], feet[0], w, p1.BODY)
    limb(draw, hip, knees[1], w, p1.SHORTS)
    limb(draw, knees[1], feet[1], w, p1.BODY)
    limb(draw, chest, hands[0], max(4, w - 1), p1.BODY)
    limb(draw, chest, hands[1], max(4, w - 1), p1.SHIRT)
    torso = int(14 * s)
    draw.rounded_rectangle(
        (chest[0] - torso, chest[1] - int(8 * s), chest[0] + torso, hip[1] + int(6 * s)),
        radius=int(6 * s),
        fill=p1.SHIRT,
        outline=INK,
        width=2,
    )
    p1.disc(draw, head, int(11 * s) + 2, INK)
    p1.disc(draw, head, int(11 * s), p1.BODY)
    if kind == "spin":
        # Arc above the V so the turn reads without a face.
        box = (fx - int(58 * s), head[1] - int(28 * s), fx + int(58 * s), head[1] + int(36 * s))
        draw.arc(box, start=200, end=340, fill=INK, width=max(4, int(5 * s)))


def dance_cell():
    cell = Image.new("RGB", (960, 420))
    p1.sky(cell)
    d = ImageDraw.Draw(cell)
    p1.ground(d, 960, 420, 250)
    figure(d, (140, 360), 108, "v")
    figure(d, (380, 360), 108, "star")
    figure(d, (620, 360), 108, "spin")
    figure(d, (860, 360), 108, "clap")
    chip(d, 70, 16, "wide V")
    chip(d, 310, 16, "star hop")
    chip(d, 540, 16, "spin stop")
    chip(d, 790, 16, "clap fails")
    return cell


def finish(path, title, kicker, cell, xy, lines, note_xy=(700, 130), size=(1280, 760)):
    img = Image.new("RGB", size, (28, 26, 24))
    draw = ImageDraw.Draw(img)
    draw.text((24, 18), "TAG  ·  FX RESEARCH  ·  PASS 4", font=p1.font(16, semi=True), fill=(186, 168, 140))
    draw.text((24, 42), title, font=p1.font(28, bold=True), fill=p1.CREAM)
    draw.text((24, 80), kicker, font=p1.font(16), fill=(210, 198, 180))
    img.paste(cell, xy)
    draw = ImageDraw.Draw(img)
    x, y = xy
    w, h = cell.size
    draw.rectangle((x - 2, y - 2, x + w + 1, y + h + 1), outline=p1.CREAM, width=2)
    ny = note_xy[1]
    font = p1.font(16)
    for line in lines:
        ny = p1.wrap_text(draw, line, (note_xy[0], ny), font, p1.CREAM, size[0] - note_xy[0] - 24)
        ny += 12
    save(img, path)


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    finish(
        os.path.join(here, "01-edge-budget.png"),
        "1  ·  Edge streaks at 640 x 360",
        "For C2. One mesh on the owner camera. Body drawn at 72 px, one fifth of 360.",
        edge_cell(),
        (24, 140),
        [
            "COUNT  6 full, 3 on low effects, 0 when effects are off or Reduced flashing is on.",
            "LIFE  0.12 s. Opacity peaks at 0.55. Do not turn the world speed lines on.",
            "MARGIN  Outer 12%. At this pane that is 77 px on the sides and 43 px on the top and bottom.",
            "TINT  VerbFxLook.PlayerColor, plus a 1 px dark outline on every seat.",
            "DRAW  1 mesh. The other three cameras do not draw it.",
        ],
    )
    finish(
        os.path.join(here, "02-ink-card-timing.png"),
        "2  ·  Ink card, then the word",
        "For C2. Seconds, not frames. The card is 269 x 101 px in this pane.",
        ink_cell(),
        (24, 200),
        [
            "LIFE  0.08 s. Opacity 0.72 while the comic toggle is on, 0.88 when that toggle is off.",
            "HOLD  From 0.16 s to 0.30 s the word is alone. Alpha stays 1 until 0.30 s.",
            "STACK  Hard-land and wall-slam words keep their own text. One card beside them.",
            "WHIFF  No card. One live card per pawn. A new contact restarts it.",
        ],
    )
    finish(
        os.path.join(here, "03-dance-outlines.png"),
        "3  ·  Three short dances",
        "For the motion worker. Not a movement lock. Body drawn at 108 px.",
        dance_cell(),
        (24, 120),
        [
            "WIDE V  0.9 s. Arms in a V and feet apart for the first 0.40 s.",
            "STAR HOP  1.0 s. The star is the hold. The legs may close after 0.45 s. The arms stay out.",
            "SPIN STOP  1.2 s. Turn once with the arms kept in the V, then stop in the star.",
            "CLAP  The hands sit on the chest. From behind that is the run pose.",
        ],
        note_xy=(24, 560),
        size=(1040, 760),
    )


if __name__ == "__main__":
    main()
