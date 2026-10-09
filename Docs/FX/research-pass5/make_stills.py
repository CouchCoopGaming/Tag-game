#!/usr/bin/env python3
"""Labeled composites for FX research pass 5.

Each page holds one 640x360 cell at 1:1. That is a 1280x720 quarter.
These are diagrams, not Unity captures. Emotes are not drawn.
"""

import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "research-pass1"))
import make_stills as p1

# DummyPrimitiveFactory foam. P1 red, P2 blue, P3 orange, P4 lavender.
RED = (224, 56, 61)
BLUE = (107, 173, 235)
ORANGE = (240, 107, 36)
LAVENDER = (179, 148, 224)
# VerbFxLook.PlayerColor seats 2 and 3. Drawn only as the current tint.
YELLOW = (255, 209, 38)
GREEN = (64, 230, 115)
INK = (20, 18, 15)
CHIP = (16, 18, 22)
CREAM = p1.CREAM


def save(img, path):
    img.save(path, "PNG", optimize=True, compress_level=9)
    print(path, img.size, os.path.getsize(path))


def chip(draw, x, y, text, w=None):
    font = p1.font(15, semi=True)
    if w is None:
        w = int(draw.textlength(text, font=font)) + 16
    draw.rounded_rectangle((x, y, x + w, y + 24), radius=4, fill=CHIP)
    draw.text((x + 8, y + 3), text, font=font, fill=CREAM)
    return w


def body(draw, foot, height, fill, outline=None, ow=0):
    """Simple side-on stand-in. `height` is the body in pixels."""
    fx, fy = foot
    s = height / 108.0
    hip = (fx, fy - int(46 * s))
    chest = (fx + int(4 * s), fy - int(78 * s))
    head = (chest[0] + int(2 * s), fy - int(100 * s))
    w = max(4, int(8 * s))
    knee = (fx + int(10 * s), fy - int(22 * s))
    toe = (fx + int(16 * s), fy)
    back = (fx - int(12 * s), fy)
    hand = (chest[0] + int(22 * s), chest[1] + int(10 * s))

    def stroke(color, width):
        p1.limb(draw, hip, knee, width, color)
        p1.limb(draw, knee, toe, width, color)
        p1.limb(draw, hip, back, width, color)
        p1.limb(draw, chest, hand, max(3, width - 1), color)
        torso = int(16 * s)
        draw.rounded_rectangle(
            (chest[0] - torso, chest[1] - int(8 * s), chest[0] + torso, hip[1] + int(6 * s)),
            radius=max(2, int(6 * s)),
            fill=color,
        )
        p1.disc(draw, head, int(12 * s), color)

    if outline and ow > 0:
        stroke(outline, w + ow * 2)
    stroke(fill, w)
    return head


def walls(draw, x0, x1):
    p1.brick_wall(draw, x0, x0 + 36, 40, 250)
    p1.brick_wall(draw, x1 - 36, x1, 40, 250)


def occlusion_cell():
    cell = Image.new("RGB", (640, 360))
    p1.sky(cell)
    d = ImageDraw.Draw(cell)
    p1.ground(d, 640, 360, 250)
    d.line((320, 40, 320, 320), fill=(28, 26, 24), width=4)
    walls(d, 8, 312)
    # Farther body first. Lavender, mostly behind the owner.
    body(d, (158, 248), 78, LAVENDER)
    body(d, (158, 274), 130, RED)
    chip(d, 16, 12, "now  ·  covered")

    walls(d, 328, 632)
    body(d, (488, 248), 78, LAVENDER, outline=INK, ow=8)
    body(d, (488, 248), 78, LAVENDER, outline=LAVENDER, ow=4)
    body(d, (448, 274), 130, RED)
    chip(d, 336, 12, "stroke on the covered body")
    chip(d, 16, 328, "640 x 360  ·  stand-in, not a capture")
    return cell


def streak(draw, x, y, length, fill, outlined=False):
    if outlined:
        draw.rectangle((x, y, x + length + 1, y + 5), fill=INK)
        draw.rectangle((x + 1, y + 1, x + length, y + 4), fill=fill)
    else:
        draw.rectangle((x, y, x + length - 1, y + 3), fill=fill)


def tint_cell():
    cell = Image.new("RGB", (640, 360))
    p1.sky(cell)
    d = ImageDraw.Draw(cell)
    p1.ground(d, 640, 360, 250)
    d.line((320, 40, 320, 320), fill=(28, 26, 24), width=4)
    walls(d, 8, 312)
    body(d, (120, 268), 96, ORANGE)
    body(d, (210, 268), 96, LAVENDER)
    # PlayerColor seat 2 yellow, seat 3 green. No outline. They cross both bodies.
    streak(d, 70, 200, 90, YELLOW)
    streak(d, 150, 188, 90, GREEN)
    streak(d, 100, 230, 70, YELLOW)
    streak(d, 180, 218, 70, GREEN)
    chip(d, 16, 12, "PlayerColor 2 and 3")

    walls(d, 328, 632)
    body(d, (440, 268), 96, ORANGE)
    body(d, (530, 268), 96, LAVENDER)
    streak(d, 390, 200, 70, ORANGE, outlined=True)
    streak(d, 500, 188, 70, LAVENDER, outlined=True)
    streak(d, 410, 230, 56, ORANGE, outlined=True)
    streak(d, 520, 218, 56, LAVENDER, outlined=True)
    chip(d, 336, 12, "foam  ·  dark outline")
    chip(d, 16, 328, "640 x 360  ·  world strokes")
    return cell


def burst(draw, xy, text, fill):
    x, y = xy
    draw.ellipse((x, y, x + 150, y + 88), fill=(236, 228, 210))
    draw.ellipse((x + 8, y + 8, x + 142, y + 80), fill=fill)
    font = p1.font(28, bold=True)
    draw.text((x + 28, y + 26), text, font=font, fill=(255, 236, 210))


def word_cell():
    cell = Image.new("RGB", (640, 360), (168, 176, 186))
    d = ImageDraw.Draw(cell)
    d.rectangle((0, 250, 640, 360), fill=(92, 96, 90))
    # Two opaque words on the same pixels.
    burst(d, (40, 70), "THUD", (90, 54, 48))
    burst(d, (70, 96), "SLAM", (48, 64, 110))
    chip(d, 16, 16, "both at full alpha")

    burst(d, (400, 90), "SLAM", (48, 64, 110))
    chip(d, 360, 16, "newer word stays")
    chip(d, 16, 320, "640 x 360  ·  overlap retires the older burst")
    return cell


def finish(path, title, kicker, cell, lines):
    img = Image.new("RGB", (1180, 560), (28, 26, 24))
    draw = ImageDraw.Draw(img)
    draw.text((24, 16), "TAG  ·  FX RESEARCH  ·  PASS 5", font=p1.font(16, semi=True), fill=(186, 168, 140))
    draw.text((24, 40), title, font=p1.font(26, bold=True), fill=CREAM)
    draw.text((24, 76), kicker, font=p1.font(16), fill=(210, 198, 180))
    img.paste(cell, (24, 120))
    draw = ImageDraw.Draw(img)
    draw.rectangle((22, 118, 24 + 640 + 1, 120 + 360 + 1), outline=CREAM, width=2)
    y = 130
    font = p1.font(16)
    for line in lines:
        y = p1.wrap_text(draw, line, (690, y), font, CREAM, 466)
        y += 14
    save(img, path)


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    finish(
        os.path.join(here, "01-alley-occlusion.png"),
        "1  ·  Covered body in a tight gap",
        "For C2. This camera only. The larger body is a diagram of a shortened boom.",
        occlusion_cell(),
        [
            "STROKE  2 px at 640 x 360, 3 px at 960 x 540. Foam of the covered seat. Dark outline. Alpha 0.90, steady.",
            "GATE  Screen rects overlap in this pane. The owner does not get the stroke in their own pane.",
            "LEAVE  The immunity shell stays on tag-back time. Do not pulse this stroke.",
        ],
    )
    finish(
        os.path.join(here, "02-seat-tint-bleed.png"),
        "2  ·  Seat tint when two bodies share the pane",
        "For C2. World strokes use foam. PlayerColor seats 2 and 3 stay in the function.",
        tint_cell(),
        [
            "NOW  Seat 2 is yellow and seat 3 is green. Those strokes cross orange and lavender.",
            "FIX  P3 orange and P4 lavender, with the 1 px dark outline from pass 4.",
            "SCOPE  Immunity shell, tag-back rings, handoff flash, dash ghost. Speed lines stay off.",
        ],
    )
    finish(
        os.path.join(here, "03-word-overlap.png"),
        "3  ·  One word when the rects overlap",
        "For C2. The newer letters stay. Word count stays 36.",
        word_cell(),
        [
            "NOW  THUD and SLAM are both at alpha 1 through 0.30 s. They do not depth-test.",
            "FIX  Overlap retires the older burst. A word whose rect is clear of the other one stays.",
            "CARD  The 0.08 s ink card still stacks with the word that remains. A whiff still has no card.",
        ],
    )


if __name__ == "__main__":
    main()
