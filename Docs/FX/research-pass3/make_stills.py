#!/usr/bin/env python3
"""Labeled composites for FX research pass 3.

Each PNG is 1600x900 and contains one 960x540 cell at 1:1 pixels.
The body is drawn at 108 px, one fifth of the cell, as a stand-in.
Contact sprites are drawn large enough to judge shape. The labels carry
the real DustLook counts. These are not Unity captures.
"""

import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "research-pass1"))
import make_stills as p1

METAL = (255, 214, 64)
METAL_INK = (28, 20, 8)
WET = (36, 118, 198)
WET_INK = (8, 28, 48)
BRICK = (176, 78, 48)
BRICK_INK = (48, 16, 10)
WOOD = (210, 164, 96)
WOOD_INK = (48, 28, 12)
CONC = (214, 212, 206)
CONC_INK = (36, 34, 32)
DIRT = (156, 98, 48)
DIRT_INK = (48, 28, 12)
GRASS = (198, 204, 168)
GRASS_INK = (36, 42, 24)
CHIP = (16, 18, 22)


def outlined_line(draw, a, b, fill, ink, width):
    draw.line((a, b), fill=ink, width=width + 6)
    draw.line((a, b), fill=fill, width=width)


def outlined_disc(draw, xy, r, fill, ink):
    p1.disc(draw, xy, r + 3, ink)
    p1.disc(draw, xy, r, fill)


def chip_label(draw, x, y, text, w=None):
    if w is None:
        w = int(draw.textlength(text, font=p1.font(15, semi=True))) + 16
    draw.rounded_rectangle((x, y, x + w, y + 26), radius=4, fill=CHIP)
    draw.text((x + 8, y + 3), text, font=p1.font(15, semi=True), fill=p1.CREAM)


def streaks(draw, origin, scale, n=3):
    ox, oy = origin
    for i in range(n):
        x0 = ox + int((-10 + i * 8) * scale)
        y0 = oy + int((4 - i * 3) * scale)
        x1 = x0 + int((16 + i * 2) * scale)
        y1 = y0 - int((10 + i) * scale)
        outlined_line(draw, (x0, y0), (x1, y1), METAL, METAL_INK, max(3, int(3 * scale)))


def ticks(draw, origin, scale, n=5):
    ox, oy = origin
    for i in range(n):
        x = ox + int((-16 + i * 8) * scale)
        y = oy + int((i % 2) * 6 * scale)
        outlined_line(draw, (x, y - int(12 * scale)), (x, y), WET, WET_INK, max(3, int(3 * scale)))
        outlined_disc(draw, (x, y + int(2 * scale)), max(2, int(3 * scale)), WET, WET_INK)


def chips(draw, origin, scale):
    ox, oy = origin
    polys = [
        [(-14, 4), (-4, -8), (2, 6)],
        [(4, 2), (14, -6), (18, 8), (8, 10)],
        [(-6, 10), (2, 4), (8, 16)],
    ]
    for poly in polys:
        pts = [(ox + int(x * scale), oy + int(y * scale)) for x, y in poly]
        draw.polygon(pts, outline=BRICK_INK)
        draw.polygon(pts, fill=BRICK)


def splinters(draw, origin, scale):
    ox, oy = origin
    for i, (dx, length) in enumerate(((-12, 18), (-2, 22), (8, 14))):
        x = ox + int(dx * scale)
        y = oy + int((i - 1) * 4 * scale)
        outlined_line(
            draw,
            (x, y),
            (x + int(length * scale), y - int(3 * scale)),
            WOOD,
            WOOD_INK,
            max(2, int(2 * scale)),
        )


def sheet(draw, origin, scale):
    ox, oy = origin
    rw, rh = int(28 * scale), int(8 * scale)
    draw.ellipse((ox - rw - 3, oy - rh - 3, ox + rw + 3, oy + rh + 3), fill=CONC_INK)
    draw.ellipse((ox - rw, oy - rh, ox + rw, oy + rh), fill=CONC)


def cloud(draw, origin, scale):
    ox, oy = origin
    outlined_disc(draw, (ox, oy), int(16 * scale), DIRT, DIRT_INK)
    outlined_disc(draw, (ox - int(12 * scale), oy + int(4 * scale)), int(10 * scale), DIRT, DIRT_INK)
    outlined_disc(draw, (ox + int(12 * scale), oy + int(2 * scale)), int(9 * scale), DIRT, DIRT_INK)


def flecks(draw, origin, scale):
    ox, oy = origin
    for dx, dy, r in ((-8, 2, 4), (0, -2, 5), (8, 3, 3), (4, 8, 3)):
        outlined_disc(draw, (ox + int(dx * scale), oy + int(dy * scale)), int(r * scale), GRASS, GRASS_INK)


def same_circle(draw, origin, scale, fill):
    outlined_disc(draw, origin, int(11 * scale), fill, (24, 20, 16))


def shape_cell():
    big = Image.new("RGB", (p1.CELL[0] * p1.SS, p1.CELL[1] * p1.SS))
    p1.sky(big)
    d = ImageDraw.Draw(big)
    p1.ground(d, big.width, big.height, int(250 * p1.SS))
    p1.runner(d, (int(120 * p1.SS), int(470 * p1.SS)), 108 * p1.SS, face=1, lean=int(4 * p1.SS))
    s = p1.SS
    # Tint-only row. Equal circles, so hue is the only difference.
    same_circle(d, (int(430 * s), int(150 * s)), s, BRICK)
    same_circle(d, (int(560 * s), int(150 * s)), s, CONC)
    same_circle(d, (int(690 * s), int(150 * s)), s, WOOD)
    # Shape row. Enlarged past metre scale so the outline can be judged.
    flecks(d, (int(300 * s), int(360 * s)), s)
    cloud(d, (int(420 * s), int(360 * s)), s)
    sheet(d, (int(560 * s), int(370 * s)), s)
    splinters(d, (int(680 * s), int(360 * s)), s)
    streaks(d, (int(300 * s), int(470 * s)), s)
    ticks(d, (int(460 * s), int(470 * s)), s)
    chips(d, (int(640 * s), int(460 * s)), s)
    cell = p1.down(big)
    cd = ImageDraw.Draw(cell)
    chip_label(cd, 8, 8, "tint only  ·  same circle")
    chip_label(cd, 392, 168, "brick", 64)
    chip_label(cd, 522, 168, "concrete", 96)
    chip_label(cd, 652, 168, "wood", 64)
    chip_label(cd, 250, 300, "grass 6", 88)
    chip_label(cd, 370, 292, "dirt 11", 80)
    chip_label(cd, 500, 300, "concrete 9", 112)
    chip_label(cd, 630, 300, "wood 8", 80)
    chip_label(cd, 250, 500, "metal 0", 88)
    chip_label(cd, 400, 500, "wet splash", 104)
    chip_label(cd, 580, 500, "brick chips", 112)
    p1.scale_bar(cd, 800, 500)
    return cell


def mark_cell():
    big = Image.new("RGB", (p1.CELL[0] * p1.SS, p1.CELL[1] * p1.SS))
    p1.sky(big)
    d = ImageDraw.Draw(big)
    p1.ground(d, big.width, big.height, int(280 * p1.SS))
    s = p1.SS
    p1.runner(d, (int(250 * s), int(460 * s)), 108 * s, face=1, lean=int(6 * s))
    # Fading puff: two pale discs, already broken up.
    outlined_disc(d, (int(270 * s), int(448 * s)), int(10 * s), (186, 184, 178), CONC_INK)
    outlined_disc(d, (int(292 * s), int(456 * s)), int(6 * s), (170, 168, 162), CONC_INK)
    # Later along the run: the puff is gone. The nick remains.
    ox, oy = int(640 * s), int(456 * s)
    rw, rh = int(36 * s), int(8 * s)
    d.ellipse((ox - rw - 5, oy - rh - 5, ox + rw + 5, oy + rh + 5), fill=p1.SHIRT)
    d.ellipse((ox - rw, oy - rh, ox + rw, oy + rh), fill=(42, 40, 38))
    cell = p1.down(big)
    cd = ImageDraw.Draw(cell)
    chip_label(cd, 8, 330, "puff, dying")
    chip_label(cd, 500, 400, "mark, still there")
    chip_label(cd, 8, 8, "one quad  ·  no extra dust")
    p1.scale_bar(cd, 800, 500)
    return cell


def wall_cell():
    big = Image.new("RGB", (p1.CELL[0] * p1.SS, p1.CELL[1] * p1.SS))
    p1.sky(big)
    d = ImageDraw.Draw(big)
    s = p1.SS
    # Metal plate on the left, concrete slab on the right.
    d.rectangle((0, int(120 * s), int(460 * s), int(540 * s)), fill=(118, 126, 136))
    for i in range(6):
        y = int((150 + i * 60) * s)
        d.rectangle((0, y, int(460 * s), y + int(4 * s)), fill=(88, 94, 102))
    d.rectangle((int(500 * s), int(200 * s), int(960 * s), int(540 * s)), fill=(168, 166, 160))
    d.rectangle((int(500 * s), int(200 * s), int(960 * s), int(214 * s)), fill=(120, 118, 112))
    me = p1.runner(d, (int(250 * s), int(470 * s)), 108 * s, face=1, lean=int(10 * s))
    # Same streak at the foot and at the hand. Origins are already in big-image pixels.
    streaks(d, me["foot"], s, n=3)
    streaks(d, me["hand"], s, n=3)
    # Concrete nicks. Darker than the slab, so they are not the metal streak.
    for dx in (-28, 0, 26):
        cx = int((720 + dx) * s)
        cy = int(400 * s)
        outlined_line(d, (cx, cy), (cx + int(22 * s), cy - int(5 * s)), (232, 230, 224), (42, 40, 38), int(5 * s))
    cell = p1.down(big)
    cd = ImageDraw.Draw(cell)
    chip_label(cd, 12, 12, "metal  ·  streak at foot and hand")
    chip_label(cd, 790, 360, "concrete nick")
    p1.scale_bar(cd, 800, 500)
    return cell


def limb_pose(draw, a, b, width, fill):
    outlined_line(draw, a, b, fill, (20, 16, 14), width)
    r = max(3, width // 2)
    outlined_disc(draw, a, r, fill, (20, 16, 14))
    outlined_disc(draw, b, r, fill, (20, 16, 14))


def pose_figure(draw, foot, height, kind):
    """Rear three-quarter. height is pixels in this image. No face."""
    fx, fy = foot
    s = height / 108.0
    hip = (fx, fy - int(46 * s))
    chest = (fx, fy - int(78 * s))
    head = (fx, fy - int(98 * s))
    w = max(5, int(8 * s))
    if kind == "shrug":
        hands = (
            (fx - int(16 * s), chest[1] + int(2 * s)),
            (fx + int(16 * s), chest[1] + int(2 * s)),
        )
        feet = (
            (fx - int(10 * s), fy),
            (fx + int(10 * s), fy),
        )
        knees = (
            (fx - int(8 * s), fy - int(22 * s)),
            (fx + int(8 * s), fy - int(22 * s)),
        )
    elif kind == "up":
        hands = (
            (fx - int(40 * s), chest[1] - int(6 * s)),
            (fx + int(6 * s), chest[1] - int(56 * s)),
        )
        feet = (
            (fx - int(22 * s), fy),
            (fx + int(18 * s), fy),
        )
        knees = (
            (fx - int(14 * s), fy - int(24 * s)),
            (fx + int(12 * s), fy - int(24 * s)),
        )
    else:
        hands = (
            (fx - int(46 * s), chest[1] - int(40 * s)),
            (fx + int(46 * s), chest[1] - int(40 * s)),
        )
        feet = (
            (fx - int(28 * s), fy),
            (fx + int(28 * s), fy),
        )
        knees = (
            (fx - int(16 * s), fy - int(22 * s)),
            (fx + int(16 * s), fy - int(22 * s)),
        )
    limb_pose(draw, hip, knees[0], w, p1.SHORTS)
    limb_pose(draw, knees[0], feet[0], w, p1.BODY)
    limb_pose(draw, hip, knees[1], w, p1.SHORTS)
    limb_pose(draw, knees[1], feet[1], w, p1.BODY)
    limb_pose(draw, chest, hands[0], max(4, w - 1), p1.BODY)
    limb_pose(draw, chest, hands[1], max(4, w - 1), p1.SHIRT)
    torso_w = int(16 * s)
    draw.rounded_rectangle(
        (chest[0] - torso_w, chest[1] - int(8 * s), chest[0] + torso_w, hip[1] + int(6 * s)),
        radius=int(6 * s),
        fill=p1.SHIRT,
        outline=(20, 16, 14),
        width=max(2, int(2 * s)),
    )
    outlined_disc(draw, head, int(11 * s), p1.BODY, (20, 16, 14))
    # Back of the neck sensor. Not a face.
    outlined_disc(draw, (head[0], head[1] + int(4 * s)), int(3 * s), p1.TEAL, (12, 40, 36))


def emote_cell():
    big = Image.new("RGB", (p1.CELL[0] * p1.SS, p1.CELL[1] * p1.SS))
    p1.sky(big)
    d = ImageDraw.Draw(big)
    p1.ground(d, big.width, big.height, int(300 * p1.SS))
    s = p1.SS
    h = 108 * s
    pose_figure(d, (int(180 * s), int(470 * s)), h, "v")
    pose_figure(d, (int(480 * s), int(470 * s)), h, "up")
    pose_figure(d, (int(760 * s), int(470 * s)), h, "shrug")
    cell = p1.down(big)
    cd = ImageDraw.Draw(cell)
    chip_label(cd, 8, 8, "first second  ·  hold the extreme")
    chip_label(cd, 24, 200, "V reads", 80)
    chip_label(cd, 400, 140, "arm up reads", 130)
    chip_label(cd, 780, 160, "shrug fails", 128)
    return cell


def finish(path, title, kicker, ref, cost, risk, note, cell):
    img = Image.new("RGB", (p1.W, p1.H), (28, 26, 24))
    draw = ImageDraw.Draw(img)
    draw.text((36, 28), "TAG  ·  FX RESEARCH  ·  PASS 3", font=p1.font(18, semi=True), fill=(186, 168, 140))
    draw.text((36, 56), title, font=p1.font(32, bold=True), fill=p1.CREAM)
    draw.text((36, 100), kicker, font=p1.font(18), fill=(210, 198, 180))
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
    kicker = "For C2. Composite, not a Unity capture. Body drawn at 108 px."
    finish(
        os.path.join(here, "01-shape-not-tint.png"),
        "1  ·  Sprite shape, counts stay",
        kicker,
        "Glass, metal, and concrete are different materials in Mirror's Edge. Tag already changes size and color. The circle is still one picture.",
        "0 extra particles. Emit counts stay, including metal at 0. Estimated 0 extra draws if the shapes share one texture.",
        "A red circle and a gray circle match at this size. A streak, a tick, and a chip do not.",
        "SurfaceCount stays 6. Brick keeps the concrete size curve. Do not add a puff to a quiet metal plant.",
        shape_cell(),
    )
    finish(
        os.path.join(here, "02-contact-mark.png"),
        "2  ·  A mark after the puff",
        kicker,
        "The scrape is the evidence after the foot has moved on. The wall scuff already outlasts its puffs. The foot plant does not.",
        "1 quad, inside the scuff slots that already exist. 0 extra dust. DustRoll stays 16.",
        "Grass gets no stain. A white mark on concrete blows out. Keep the nick darker than the sheet.",
        "LandDust counts stay. This is not a second landing burst.",
        mark_cell(),
    )
    finish(
        os.path.join(here, "03-wall-matches-foot.png"),
        "3  ·  Wall matches the foot",
        kicker,
        "A scrape and a footstep are the same material. Concrete may still spark. It should not use the metal streak.",
        "0 new particles. ScrapeSpark and ScrapeDrop stay. Estimated cost is a texture frame.",
        "If every hard wall uses the bright streak, metal stops meaning metal.",
        "The seat ribbon from pass 1 stays the seat color. This streak is the material.",
        wall_cell(),
    )
    finish(
        os.path.join(here, "04-emote-silhouette.png"),
        "4  ·  Emote silhouette",
        "For the motion worker. Not a movement lock. Body drawn at 108 px.",
        "Smash taunts move the whole body. Neon White's victory is a full-body flip. A face does not fit in a 22 px head.",
        "No particles. No new settings row. The capsule stays free, the way the roll is a pose.",
        "Hold the V or the raised arm for about 0.4 s in the first second. A shrug matches a run.",
        "Do not copy Smash's frame-50 cancel. Do not write coyote, speed, or a stun.",
        emote_cell(),
    )


if __name__ == "__main__":
    main()
