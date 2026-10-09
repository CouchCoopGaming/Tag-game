#!/usr/bin/env python3
"""Labeled composites for FX research pass 6.

Each page holds one 640x360 cell at 1:1. That is a 1280x720 quarter.
These are diagrams, not Unity captures. Emotes are not drawn.
"""

import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "research-pass1"))
import make_stills as p1

RED = (224, 56, 61)
ORANGE = (240, 107, 36)
LAVENDER = (179, 148, 224)
YELLOW = (255, 209, 38)
INK = (20, 18, 15)
SCUFF = (51, 51, 48)
BROWN = (115, 97, 77)
STAIN = (115, 112, 107)
PUFF = (199, 199, 194)
CHIP = (16, 18, 22)
CREAM = p1.CREAM


def save(img, path):
    img.save(path, "PNG", optimize=True, compress_level=9)
    print(path, img.size, os.path.getsize(path))


def chip(draw, x, y, text):
    font = p1.font(15, semi=True)
    w = int(draw.textlength(text, font=font)) + 16
    draw.rounded_rectangle((x, y, x + w, y + 24), radius=4, fill=CHIP)
    draw.text((x + 8, y + 3), text, font=font, fill=CREAM)
    return w


def body(draw, foot, height, fill):
    fx, fy = foot
    s = height / 108.0
    hip = (fx, fy - int(46 * s))
    chest = (fx + int(4 * s), fy - int(78 * s))
    head = (chest[0] + int(2 * s), fy - int(100 * s))
    w = max(4, int(8 * s))
    knee = (fx + int(8 * s), fy - int(22 * s))
    toe = (fx + int(14 * s), fy)
    back = (fx - int(10 * s), fy)
    hand = (chest[0] + int(18 * s), chest[1] + int(6 * s))
    p1.limb(draw, hip, knee, w, fill)
    p1.limb(draw, knee, toe, w, fill)
    p1.limb(draw, hip, back, w, fill)
    p1.limb(draw, chest, hand, max(3, w - 1), fill)
    torso = int(14 * s)
    draw.rounded_rectangle(
        (chest[0] - torso, chest[1] - int(8 * s), chest[0] + torso, hip[1] + int(6 * s)),
        radius=max(2, int(6 * s)),
        fill=fill,
    )
    p1.disc(draw, head, int(11 * s), fill)


def frame(draw, x0, y0, x1, y1, color):
    """0.08 of the width on the sides, 0.11 of the height on the top and bottom."""
    w = x1 - x0
    h = y1 - y0
    sw = max(8, int(w * 0.08))
    sh = max(8, int(h * 0.11))
    draw.rectangle((x0, y0, x0 + sw, y1), fill=color)
    draw.rectangle((x1 - sw, y0, x1, y1), fill=color)
    draw.rectangle((x0, y0, x1, y0 + sh), fill=color)
    draw.rectangle((x0, y1 - sh, x1, y1), fill=color)


def wall_cell():
    cell = Image.new("RGB", (640, 360), (168, 166, 160))
    d = ImageDraw.Draw(cell)
    for y in range(0, 360, 28):
        d.line((0, y, 318, y), fill=(140, 138, 132), width=2)
    p1.brick_wall(d, 324, 640, 0, 360)
    d.line((320, 0, 320, 360), fill=(28, 26, 24), width=4)
    # Now: one gray scuff, brown kit line, yellow PlayerColor frame.
    frame(d, 0, 0, 320, 360, YELLOW)
    d.ellipse((70, 150, 250, 250), fill=SCUFF)
    d.line((80, 200, 240, 214), fill=BROWN, width=4)
    body(d, (130, 230), 78, ORANGE)
    body(d, (190, 250), 78, LAVENDER)
    chip(d, 48, 48, "surface ink  ·  yellow frame")
    # Fix: ribbons offset, foam frame, tagger ribbon ducked (drawn shorter).
    frame(d, 320, 0, 640, 360, ORANGE)
    d.rounded_rectangle((360, 168, 560, 182), radius=3, fill=INK)
    d.rounded_rectangle((364, 171, 556, 179), radius=2, fill=ORANGE)
    d.rounded_rectangle((360, 214, 590, 230), radius=3, fill=INK)
    d.rounded_rectangle((364, 217, 586, 227), radius=2, fill=LAVENDER)
    body(d, (450, 210), 78, ORANGE)
    body(d, (510, 268), 78, LAVENDER)
    chip(d, 360, 48, "offset ribbons  ·  foam frame")
    chip(d, 24, 292, "640 x 360  ·  stand-in")
    return cell


def glow(draw, foot, height, fill, pad):
    fx, fy = foot
    s = height / 108.0
    top = fy - int(112 * s) - pad
    draw.ellipse((fx - int(36 * s) - pad, top, fx + int(40 * s) + pad, fy + pad), fill=fill)


def yield_cell():
    cell = Image.new("RGB", (640, 360), (168, 176, 186))
    d = ImageDraw.Draw(cell)
    d.rectangle((0, 250, 640, 360), fill=(92, 96, 90))
    d.line((320, 16, 320, 300), fill=(28, 26, 24), width=4)
    # Both: yellow shell glow, then a hard lavender stroke.
    glow(d, (150, 240), 120, (255, 220, 80), 18)
    glow(d, (150, 240), 120, YELLOW, 8)
    body(d, (150, 240), 120, INK)
    body(d, (150, 240), 108, LAVENDER)
    chip(d, 24, 16, "stroke and shell")
    # Shell only, foam, no hard stroke.
    glow(d, (470, 240), 120, (210, 180, 230), 16)
    glow(d, (470, 240), 120, LAVENDER, 6)
    body(d, (470, 240), 108, LAVENDER)
    chip(d, 360, 16, "stroke yields")
    chip(d, 16, 320, "640 x 360  ·  shell owns the edge")
    return cell


def puff(draw, cx, cy, scale, alpha_rgb):
    r = int(46 * scale)
    draw.ellipse((cx - r, cy - int(r * 0.7), cx + r, cy + int(r * 0.45)), fill=alpha_rgb)


def mark(draw, x, y, length, strong):
    color = STAIN if strong else (168, 166, 162)
    h = 8 if strong else 4
    draw.rectangle((x, y, x + length, y + h), fill=color)


def roll_cell():
    cell = Image.new("RGB", (640, 360), (186, 188, 184))
    d = ImageDraw.Draw(cell)
    d.rectangle((0, 230, 640, 360), fill=(150, 148, 144))
    # 0.00 s. Puff is the read. Stain is dim.
    puff(d, 100, 180, 1.35, PUFF)
    mark(d, 60, 196, 80, False)
    chip(d, 24, 24, "0.00 s  ·  puff")
    # 0.50 s. Puff life is over at the threshold. Stain still dim until 0.56.
    mark(d, 276, 196, 80, False)
    chip(d, 230, 24, "0.50 s  ·  life ends")
    # After 0.56 s. Stain alone.
    mark(d, 470, 190, 124, True)
    chip(d, 450, 24, "0.70 s  ·  stain")
    chip(d, 16, 320, "640 x 360  ·  puff life unchanged")
    return cell


def finish(path, title, kicker, cell, lines):
    img = Image.new("RGB", (1180, 560), (28, 26, 24))
    draw = ImageDraw.Draw(img)
    draw.text((24, 16), "TAG  ·  FX RESEARCH  ·  PASS 6", font=p1.font(16, semi=True), fill=(186, 168, 140))
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
        os.path.join(here, "01-wall-tag-layers.png"),
        "1  ·  Two seats on one wall",
        "For C2. The frame is the tag. The ribbon is the seat. Scuff gray stays.",
        wall_cell(),
        [
            "NOW  Surface ink and a brown line. The tag frame uses PlayerColor yellow.",
            "FIX  Foam ribbons, shifted by seat. The frame is foam and ducks only the tagger’s ribbon.",
            "RAIL  Zip core stays purple. A 1 px foam edge separates two riders.",
        ],
    )
    finish(
        os.path.join(here, "02-stroke-yields.png"),
        "2  ·  Stroke yields to the immunity shell",
        "For C2. The shell is the edge for that 1.0 s. The stroke returns after.",
        yield_cell(),
        [
            "FIGHT  A 0.90 stroke sits on the same edge as a shell that peaks near 0.85 and pulses.",
            "YIELD  While the shell is enabled, this camera draws no stroke on that body.",
            "LEAVE  RimAlpha, the 0.012 m push, and the 1.0 s timer stay.",
        ],
    )
    finish(
        os.path.join(here, "03-roll-mark.png"),
        "3  ·  Stain after the concrete puff",
        "For C2. concreteLife stays 0.50. The stain does not use that timer.",
        roll_cell(),
        [
            "PUFF  Sprint concrete life is 0.50 s. Land dust at the roll threshold is 0.50 s. The plume is 0.50 s.",
            "STAIN  Opacity 0.22 until 0.56 s, then 0.55 until 1.10 s. One quad. Count stays 9.",
            "SKIP  Impact under 12 draws no stain. Grass still gets none.",
        ],
    )


if __name__ == "__main__":
    main()
