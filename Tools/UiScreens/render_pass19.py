#!/usr/bin/env python3
"""Pass 19 split HUD stills. Unity Editor is not installed, so these are composites."""

import os

import render_screens2 as ui
from PIL import Image, ImageDraw

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass19")

W, H = ui.W, ui.H
INK = ui.INK
CREAM = ui.CREAM
GOLD = ui.GOLD
NAVY = ui.NAVY


def score_text(img, xy, text, face, fill, bg):
    x, y = xy
    probe = Image.new("RGB", (img.size[0], img.size[1]), bg)
    pd = ImageDraw.Draw(probe)
    pd.text((x, y), text, font=face, fill=fill)
    crop = pd.textbbox((x, y), text, font=face)
    box = (
        max(0, crop[0] - 1),
        max(0, crop[1] - 1),
        min(img.size[0], crop[2] + 1),
        min(img.size[1], crop[3] + 1),
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


def text(d, xy, word, face, fill, bg, box, checks, ratios):
    d.text(xy, word, font=face, fill=fill)
    ok, bb = ui.text_inside(d, xy, word, face, box)
    if not ok:
        raise SystemExit("clip %r %s" % (word, bb))
    ratios.append(ui.contrast(fill, bg))
    checks.append((xy, word, face, fill, bg))


def shape(d, box, kind, fill):
    l, t, r, b = box
    cx = (l + r) / 2
    cy = (t + b) / 2
    if kind == 0:
        d.ellipse(box, fill=fill)
    elif kind == 2:
        d.rectangle(box, fill=fill)
    elif kind == 3:
        d.polygon([(cx, t), (r, cy), (cx, b), (l, cy)], fill=fill)
    else:
        d.polygon([(cx, t), (l, b), (r, b)], fill=fill)


def arrow(d, box, toward):
    l, t, r, b = box
    cx, cy = (l + r) / 2, (t + b) / 2
    dx, dy = toward
    if dx == 0 and dy == 0:
        dx = 1
    # Point the triangle along (dx, dy).
    mag = (dx * dx + dy * dy) ** 0.5
    ux, uy = dx / mag, dy / mag
    px, py = -uy, ux
    tip = (cx + ux * 28, cy + uy * 28)
    left = (cx - ux * 16 + px * 16, cy - uy * 16 + py * 16)
    right = (cx - ux * 16 - px * 16, cy - uy * 16 - py * 16)
    d.polygon([tip, left, right], fill=GOLD)


def ring(d, cx, cy, rad, fill_amt, color):
    d.ellipse((cx - rad, cy - rad, cx + rad, cy + rad), fill=(16, 22, 36))
    if fill_amt <= 0:
        return
    d.pieslice((cx - rad + 3, cy - rad + 3, cx + rad - 3, cy + rad - 3), -90, -90 + 360 * fill_amt, fill=color)


def pane_box(humans, index, split_h=False):
    if humans <= 1:
        return (0, 0, W, H)
    if humans == 2 and not split_h:
        return (0, 0, W // 2, H) if index == 0 else (W // 2, 0, W, H)
    if humans == 2 and split_h:
        return (0, 0, W, H // 2) if index == 0 else (0, H // 2, W, H)
    # Quadrants. Index 3 is the score pane at 3-up.
    col = index % 2
    row = index // 2
    return (col * W // 2, row * H // 2, (col + 1) * W // 2, (row + 1) * H // 2)


def insets(box):
    l, t, r, b = box
    left = 48 if l <= 1 else 22
    top = 48 if t <= 1 else 22
    right = 48 if r >= W - 1 else 22
    bottom = 48 if b >= H - 1 else 22
    return left, top, right, bottom


def play_pane(img, box, seat, it, safe, dash_amt, dash_word, metric, value, tags, tagger, feed, comic, shapes, big, checks, ratios, arrow_at=None):
    d = ImageDraw.Draw(img)
    l, t, r, b = box
    seat_c = ui.SEAT[seat]
    frame = GOLD if it else seat_c
    d.rectangle((l, t, r - 1, t + 8), fill=frame)
    d.rectangle((l, b - 8, r - 1, b - 1), fill=frame)
    d.rectangle((l, t, l + 8, b - 1), fill=frame)
    d.rectangle((r - 8, t, r - 1, b - 1), fill=frame)
    if safe:
        d.rectangle((l + 14, t + 14, r - 14, b - 14), outline=(255, 236, 160))
        for k in range(6):
            d.rectangle((l + 16 + k, t + 16 + k, r - 16 - k, b - 16 - k), outline=(255, 214, 80))
    il, itop, ir, ib = insets(box)
    face_d = ui.font(ui.FONT_D, 64 if big else 48)
    face_b = ui.font(ui.FONT_B, 30 if big else 26)
    clock_px = 46 if big else 32
    clock_face = ui.font(ui.FONT_D if comic else ui.FONT_B, clock_px)
    name_face = ui.font(ui.FONT_D if comic else ui.FONT_B, 36 if big else 30)
    # Timer, top center of the pane, inside the safe inset.
    clock = "1:24"
    rnd = "R2 / 3" if not big else "R1"
    cw = 250 if big else 210
    ch = 64 if big else 52
    cx0 = (l + r - cw) // 2
    cy0 = t + itop
    if cx0 < l + il:
        cx0 = l + il
    if cx0 + cw > r - ir:
        cx0 = r - ir - cw
    ui.rounded(d, (cx0, cy0, cx0 + cw, cy0 + ch), 12, INK)
    text(d, (cx0 + 16, cy0 + 8), clock, clock_face, CREAM, INK, (cx0 + 8, cy0 + 4, cx0 + cw - 70, cy0 + ch - 4), checks, ratios)
    rf = ui.font(ui.FONT_B, 24 if big else 20)
    text(d, (cx0 + cw - 92, cy0 + 16), rnd, rf, GOLD, INK, (cx0 + cw - 100, cy0 + 8, cx0 + cw - 8, cy0 + ch - 8), checks, ratios)
    # Identity, outer top corner.
    right = l > W * 0.4
    nw, nh = (340 if big else 280), (108 if big else 96)
    if right:
        nx = r - ir - nw
    else:
        nx = l + il
    ny = t + itop
    # Keep the name plate off the timer.
    if nx < cx0 + cw and nx + nw > cx0 and ny < cy0 + ch:
        ny = cy0 + ch + 8
    ui.rounded(d, (nx, ny, nx + nw, ny + nh), 12, INK)
    d.rectangle((nx, ny, nx + 10, ny + nh), fill=seat_c)
    if shapes:
        shape(d, (nx + 18, ny + 14, nx + 50, ny + 46), seat, CREAM)
        name_x = nx + 58
    else:
        name_x = nx + 20
    text(d, (name_x, ny + 8), "P%d" % (seat + 1), name_face, CREAM, INK, (nx + 12, ny + 4, nx + nw - 80, ny + 48), checks, ratios)
    if it:
        ui.rounded(d, (nx + nw - 78, ny + 10, nx + nw - 10, ny + 50), 8, GOLD)
        bf = ui.font(ui.FONT_D, 28)
        text(d, (nx + nw - 64, ny + 12), "IT", bf, INK, GOLD, (nx + nw - 76, ny + 8, nx + nw - 12, ny + 48), checks, ratios)
    sub = ui.font(ui.FONT_B, 22 if big else 20)
    line = metric + "  " + value + "    TAGS  " + tags
    text(d, (nx + 18, ny + nh - 36), line, sub, GOLD, INK, (nx + 12, ny + nh - 40, nx + nw - 8, ny + nh - 6), checks, ratios)
    # Figure.
    fw, fh = (120, 200) if big else (72, 120)
    fx = (l + r - fw) // 2
    fy = (t + b - fh) // 2 + (20 if big else 8)
    d.ellipse((fx + fw * 0.28, fy, fx + fw * 0.72, fy + fh * 0.28), fill=seat_c)
    d.rounded_rectangle((fx + fw * 0.22, fy + fh * 0.26, fx + fw * 0.78, fy + fh), 16, fill=seat_c)
    if it:
        iw, ih = (280, 108) if big else (168, 76)
        ix = (l + r - iw) // 2
        iy = fy - ih - 12
        if iy < cy0 + ch + 8:
            iy = cy0 + ch + 8
        ui.rounded(d, (ix, iy, ix + iw, iy + ih), 16, GOLD)
        it_face = ui.font(ui.FONT_D, 72 if big else 48)
        tw = d.textlength("IT", font=it_face)
        text(d, (ix + (iw - tw) / 2, iy + 10), "IT", it_face, INK, GOLD, (ix + 8, iy + 4, ix + iw - 8, iy + ih - 4), checks, ratios)
    if arrow_at is not None and not it:
        al, at, ar, ab = arrow_at
        acx, acy = (al + ar) / 2, (at + ab) / 2
        pcx, pcy = (l + r) / 2, (t + b) / 2
        dx, dy = acx - pcx, acy - pcy
        mag = (dx * dx + dy * dy) ** 0.5 or 1
        ux, uy = dx / mag, dy / mag
        ax = pcx + ux * ((r - l) * 0.36)
        ay = pcy + uy * ((b - t) * 0.30)
        margin = 56
        if ax < l + margin:
            ax = l + margin
        if ax > r - margin:
            ax = r - margin
        if ay < t + margin:
            ay = t + margin
        if ay > b - margin:
            ay = b - margin
        arrow(d, (ax - 30, ay - 30, ax + 30, ay + 30), (dx, dy))
        chip_l, chip_t, chip_r, chip_b = ax + 24, ay - 22, ax + 96, ay + 18
        room_r = r - 36
        room_l = l + 36
        if chip_r > room_r and (ax - 96) >= room_l:
            chip_l, chip_r = ax - 96, ax - 24
        if chip_r > room_r:
            chip_r = room_r
            chip_l = chip_r - 72
        if chip_l < room_l:
            chip_l = room_l
            chip_r = chip_l + 72
        if chip_t < t + 36:
            chip_b += (t + 36) - chip_t
            chip_t = t + 36
        if chip_b > b - 36:
            chip_t -= chip_b - (b - 36)
            chip_b = b - 36
        ui.rounded(d, (chip_l, chip_t, chip_r, chip_b), 8, INK)
        text(d, (chip_l + 16, chip_t + 6), "IT", ui.font(ui.FONT_D, 28), GOLD, INK, (chip_l + 4, chip_t + 2, chip_r - 4, chip_b - 2), checks, ratios)
    # Dash, bottom left.
    dx0 = l + il
    dy0 = b - ib - 78
    ring(d, dx0 + 28, dy0 + 24, 26, dash_amt, (64, 158, 255))
    ui.rounded(d, (dx0 + 60, dy0 + 8, dx0 + 168, dy0 + 48), 8, INK)
    df = ui.font(ui.FONT_B, 22)
    text(d, (dx0 + 72, dy0 + 12), dash_word, df, CREAM, INK, (dx0 + 64, dy0 + 8, dx0 + 164, dy0 + 46), checks, ratios)
    if safe:
        ring(d, dx0 + 28, dy0 - 62, 26, 0.8, GOLD)
        ui.rounded(d, (dx0 + 60, dy0 - 78, dx0 + 188, dy0 - 38), 8, INK)
        text(d, (dx0 + 72, dy0 - 72), "SAFE  0.8", df, CREAM, INK, (dx0 + 64, dy0 - 76, dx0 + 184, dy0 - 40), checks, ratios)
    # Feed.
    fwrd = 360 if big else 300
    fhgt = 44
    fx0 = r - ir - fwrd
    fy0 = b - ib - fhgt
    ui.rounded(d, (fx0, fy0, fx0 + fwrd, fy0 + fhgt), 10, INK)
    d.rectangle((fx0 + 8, fy0 + 8, fx0 + 28, fy0 + 36), fill=ui.SEAT[tagger])
    if shapes:
        shape(d, (fx0 + 36, fy0 + 8, fx0 + 64, fy0 + 36), tagger, CREAM)
        feed_x = fx0 + 72
    else:
        feed_x = fx0 + 40
    ff = ui.font(ui.FONT_D if comic else ui.FONT_B, 26 if big else 22)
    text(d, (feed_x, fy0 + 8), feed, ff, CREAM, INK, (feed_x - 4, fy0 + 4, fx0 + fwrd - 8, fy0 + fhgt - 4), checks, ratios)


def score_pane(img, box, shapes, checks, ratios):
    d = ImageDraw.Draw(img)
    l, t, r, b = box
    d.rectangle((l + 24, t + 24, r - 24, b - 24), fill=INK)
    title = ui.font(ui.FONT_D, 36)
    text(d, (l + 48, t + 40), "SCORE", title, CREAM, INK, (l + 36, t + 32, r - 36, t + 88), checks, ratios)
    face = ui.font(ui.FONT_B, 28)
    for i, line in enumerate(("P1   12.4", "P2   8.1", "P3   4.0", "P4   1.2")):
        y = t + 110 + i * 72
        if shapes:
            shape(d, (l + 48, y, l + 84, y + 36), i, CREAM)
            x = l + 100
        else:
            x = l + 56
        d.rectangle((l + 36, y - 6, l + 46, y + 42), fill=ui.SEAT[i])
        text(d, (x, y), line, face, CREAM, INK, (l + 40, y - 4, r - 40, y + 44), checks, ratios)


def scene(humans, comic, shapes, it_seat):
    img = ui.backdrop(0.45)
    checks = []
    ratios = [ui.contrast(GOLD, INK), ui.contrast(CREAM, INK), ui.contrast(INK, GOLD)]
    panes = 4 if humans >= 3 else humans
    it_box = pane_box(humans, it_seat)
    for i in range(panes):
        box = pane_box(humans, i)
        if humans == 3 and i == 3:
            score_pane(img, box, shapes, checks, ratios)
            continue
        it = i == it_seat
        play_pane(
            img, box, i, it,
            safe=(i == 0 and humans > 1),
            dash_amt=1.0 if it else 0.4,
            dash_word="DASH" if it else "18s",
            metric="TIME",
            value="12.4" if i == 0 else "4.0",
            tags=str(i + 1),
            tagger=(1 if i == 0 else 0) if it else it_seat,
            feed=("P%d tagged P%d" % ((2 if i == 0 else 1) if it else (it_seat + 1), i + 1)),
            comic=comic,
            shapes=shapes,
            big=humans <= 2,
            checks=checks,
            ratios=ratios,
            arrow_at=None if it or humans <= 1 else it_box,
        )
    if min(ratios) < 4.5:
        raise SystemExit("contrast %.2f" % min(ratios))
    return img, min(ratios), checks


def verify(img, checks, label):
    worst = 0
    for item in checks:
        score = score_text(img, item[0], item[1], item[2], item[3], item[4])
        if score > 12:
            raise SystemExit("text miss %s %r %.1f" % (label, item[1], score))
        worst = max(worst, score)
    return worst


def main():
    jobs = [
        ("19-hud-1-composite.png", 1, True, False, 0),
        ("19-hud-2-composite.png", 2, False, False, 1),
        ("19-hud-3-composite.png", 3, True, True, 2),
        ("19-hud-4-composite.png", 4, True, True, 2),
    ]
    notes = []
    worst = 99
    for name, humans, comic, shapes, it_seat in jobs:
        img, ratio, checks = scene(humans, comic, shapes, it_seat)
        miss = verify(img, checks, name)
        ui.save(img, name, notes)
        worst = min(worst, ratio)
        print("  contrast %.2f text %.1f" % (ratio, miss))
    print("min contrast", round(worst, 2))


if __name__ == "__main__":
    main()
