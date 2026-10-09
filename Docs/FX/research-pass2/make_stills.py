#!/usr/bin/env python3
"""Labeled composites for FX research pass 2.

Each PNG is 1600x900 and contains one 960x540 cell at 1:1 pixels.
The body is drawn at 108 px, one fifth of the cell, as a stand-in.
These are not Unity captures.
"""

import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "research-pass1"))
import make_stills as p1

CROWN = (40, 210, 235)
BLUE = (90, 150, 220)


def chip(draw, x, y, text, fill=CROWN):
    draw.rounded_rectangle((x, y, x + 72, y + 36), radius=4, fill=(12, 16, 22))
    draw.rounded_rectangle((x + 4, y + 4, x + 68, y + 32), radius=3, fill=fill)
    draw.text((x + 22, y + 6), text, font=p1.font(18, bold=True), fill=(16, 18, 22))


def star(draw, cx, cy, r, fill, outline):
    # Five-point star, drawn as a fat cross plus a rotated hint so it reads at small size.
    draw.polygon(
        [
            (cx, cy - r),
            (cx + r * 0.28, cy - r * 0.28),
            (cx + r, cy - r * 0.18),
            (cx + r * 0.38, cy + r * 0.18),
            (cx + r * 0.62, cy + r),
            (cx, cy + r * 0.42),
            (cx - r * 0.62, cy + r),
            (cx - r * 0.38, cy + r * 0.18),
            (cx - r, cy - r * 0.18),
            (cx - r * 0.28, cy - r * 0.28),
        ],
        fill=fill,
        outline=outline,
    )


def plate(draw, head, scale):
    cx, cy = head
    cy -= int(22 * scale)
    w = int(28 * scale)
    h = int(22 * scale)
    draw.rounded_rectangle((cx - w, cy - h, cx + w, cy + h), radius=int(6 * scale), fill=(16, 18, 22))
    star(draw, cx, cy + int(2 * scale), int(14 * scale), CROWN, (255, 255, 255))


def wedge(draw, x, y):
    draw.polygon([(x, y - 28), (x + 36, y), (x, y + 28)], fill=CROWN)
    draw.polygon([(x + 4, y - 18), (x + 26, y), (x + 4, y + 18)], fill=(16, 18, 22))
    star(draw, x + 12, y, 10, CROWN, None)


def offscreen_cell():
    big = Image.new("RGB", (p1.CELL[0] * p1.SS, p1.CELL[1] * p1.SS))
    p1.sky(big)
    d = ImageDraw.Draw(big)
    p1.ground(d, big.width, big.height, int(320 * p1.SS))
    p1.brick_wall(d, int(40 * p1.SS), int(180 * p1.SS), int(140 * p1.SS), int(320 * p1.SS))
    p1.runner(d, (int(420 * p1.SS), int(460 * p1.SS)), 108 * p1.SS, face=1, lean=int(6 * p1.SS))
    cell = p1.down(big)
    cd = ImageDraw.Draw(cell)
    wedge(cd, 900, 250)
    cd.text((848, 286), "IT", font=p1.font(18, bold=True), fill=(16, 18, 22))
    cd.rounded_rectangle((8, 8, 430, 36), radius=4, fill=(247, 244, 238))
    cd.text((16, 12), "you are not It  ·  one wedge", font=p1.font(16, semi=True), fill=p1.EDGE)
    p1.scale_bar(cd, 790, 490)
    return cell


def crown_cell():
    big = Image.new("RGB", (p1.CELL[0] * p1.SS, p1.CELL[1] * p1.SS))
    p1.sky(big)
    d = ImageDraw.Draw(big)
    p1.ground(d, big.width, big.height, int(300 * p1.SS))
    p1.brick_wall(d, int(30 * p1.SS), int(160 * p1.SS), int(100 * p1.SS), int(300 * p1.SS))
    # Seat orange is It. Blue shirt is another runner, no plate.
    it = p1.runner(d, (int(560 * p1.SS), int(450 * p1.SS)), 108 * p1.SS, face=-1, lean=0)
    # Second runner in blue. runner() hardcodes the shirt, so paint a blue torso over it.
    other = p1.runner(d, (int(300 * p1.SS), int(450 * p1.SS)), 108 * p1.SS, face=1, lean=0)
    s = p1.SS
    chest = other["chest"]
    d.rounded_rectangle(
        (chest[0] - int(16 * s), chest[1] - int(6 * s), chest[0] + int(16 * s), other["hip"][1] + int(6 * s)),
        radius=int(6 * s),
        fill=BLUE,
    )
    # Halo under the It only.
    hx, hy = int(560 * s), int(448 * s)
    d.ellipse((hx - int(36 * s), hy - int(8 * s), hx + int(36 * s), hy + int(8 * s)), outline=CROWN, width=int(4 * s))
    plate(d, it["head"], s)
    cell = p1.down(big)
    cd = ImageDraw.Draw(cell)
    cd.rounded_rectangle((8, 8, 520, 36), radius=4, fill=(247, 244, 238))
    cd.text((16, 12), "star plate = role    shirt = seat", font=p1.font(16, semi=True), fill=p1.EDGE)
    p1.scale_bar(cd, 790, 490)
    return cell


def handoff_cell():
    big = Image.new("RGB", (p1.CELL[0] * p1.SS, p1.CELL[1] * p1.SS))
    p1.sky(big)
    d = ImageDraw.Draw(big)
    p1.ground(d, big.width, big.height, int(280 * p1.SS))
    me = p1.runner(d, (int(470 * p1.SS), int(470 * p1.SS)), 108 * p1.SS, face=1, lean=int(4 * p1.SS))
    # Short ring at the chest. Not a full-pane frame.
    cx, cy = me["chest"]
    r = int(34 * p1.SS)
    d.ellipse((cx - r, cy - r, cx + r, cy + r), outline=p1.SHIRT, width=int(5 * p1.SS))
    plate(d, me["head"], p1.SS * 1.15)
    cell = p1.down(big)
    cd = ImageDraw.Draw(cell)
    chip(cd, 24, 24, "IT")
    cd.rounded_rectangle((110, 24, 560, 56), radius=4, fill=(247, 244, 238))
    cd.text((118, 28), "you became It  ·  no full-pane flash", font=p1.font(16, semi=True), fill=p1.EDGE)
    p1.scale_bar(cd, 790, 490)
    return cell


def finish(path, title, rank, ref, cost, risk, note, cell):
    img = Image.new("RGB", (p1.W, p1.H), (28, 26, 24))
    draw = ImageDraw.Draw(img)
    draw.text((36, 28), "TAG  ·  FX RESEARCH  ·  PASS 2", font=p1.font(18, semi=True), fill=(186, 168, 140))
    draw.text((36, 56), title, font=p1.font(32, bold=True), fill=p1.CREAM)
    draw.text((36, 100), "Rank %d for C2. Composite, not a Unity capture. Body drawn at 108 px." % rank, font=p1.font(18), fill=(210, 198, 180))
    img.paste(cell, p1.CELL_XY)
    p1.cell_frame(img)
    draw = ImageDraw.Draw(img)
    y = 190
    y = p1.wrap_text(draw, ref, (1028, y), p1.font(20), p1.CREAM, 530)
    draw.text((1028, y + 18), "COST", font=p1.font(14, semi=True), fill=(186, 168, 140))
    y = p1.wrap_text(draw, cost, (1028, y + 40), p1.font(20), p1.CREAM, 530)
    draw.text((1028, y + 18), "READABILITY", font=p1.font(14, semi=True), fill=(186, 168, 140))
    y = p1.wrap_text(draw, risk, (1028, y + 40), p1.font(20), p1.CREAM, 530)
    draw.text((1028, y + 18), "LEFT ALONE", font=p1.font(14, semi=True), fill=(186, 168, 140))
    p1.wrap_text(draw, note, (1028, y + 40), p1.font(20), p1.CREAM, 530)
    img.save(path, "PNG")
    print(path, img.size)


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    finish(
        os.path.join(here, "01-offscreen-wedge.png"),
        "1  ·  One off-screen It wedge",
        1,
        "Smash shows a port-colored mark when a fighter leaves the camera. Tag needs that for one body, the It.",
        "0 particles. One screen quad on that pane, only while the It is outside it. Estimated.",
        "One wedge. Four arrows would fill a 960x540 pane. Hold it steady when Reduced flashing is on.",
        "The world hat stays. This pane does not draw a wedge at your own feet when you are It.",
        offscreen_cell(),
    )
    finish(
        os.path.join(here, "02-crown-plate.png"),
        "2  ·  Crown plate, not a recolor",
        2,
        "Mario Kart puts a crown on the leader. Fall Guys puts a tail on the holder. The shirt stays the seat.",
        "1 billboard. 0 particles. Estimated 1 draw per camera that can see the It. No new light.",
        "The plate has to stay off the shirt color. ItAgainst already picks that crown color.",
        "The tall beacon stays hidden in your own view. The plate does not.",
        crown_cell(),
    )
    finish(
        os.path.join(here, "03-handoff-receiver.png"),
        "3  ·  Handoff, receiver's pane",
        3,
        "The crown changes heads. The swell that already plays is the beat. Time does not stop.",
        "0 new particles. The 0.40 s swell, the HUD chip, and the plate from idea 2. Estimated.",
        "Do not give the receiver the tagger's edge bars. Those mean the tag landed, not that you are It.",
        "The tagger's pane is the hat leaving and the cool-white swell. This cell is the other half.",
        handoff_cell(),
    )


if __name__ == "__main__":
    main()
