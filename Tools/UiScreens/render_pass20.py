#!/usr/bin/env python3
"""Pass 20 split HUD stills. One scripted tag, shared by every pane.

Sequence, same as HudState.Script: P1 tags P2, then P2 tags P3.
P3 is It. P2 is safe for 0.8 s of the 1.0 s window.
Unity Editor is not installed, so these are composites.
"""

import os

import render_screens2 as ui
from PIL import Image, ImageDraw

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass20")

W, H = ui.W, ui.H
INK = ui.INK
CREAM = ui.CREAM
GOLD = ui.GOLD

# HudState.Script
IT = 2
SAFE = 1
FEEDS = (("P2 tagged P3", 1), ("P1 tagged P2", 0))
# HudState.ViewOf
VIEW = {
    0: (0.36, 0.48, 1.0),
    1: (1.45, 0.22, 1.0),
    3: (0.50, -0.55, 1.0),
}
TIMES = {0: "8.1", 1: "12.4", 2: "0.2", 3: "4.0"}
TAGS = {0: "1", 1: "1", 2: "0", 3: "0"}


def project(vx, vy, vz):
    dx, dy = vx - 0.5, vy - 0.5
    if vz < 0.05:
        dx, dy = -dx, -dy
    on = vz >= 0.05 and 0.02 <= vx <= 0.98 and 0.04 <= vy <= 0.96
    if on:
        head = vy + 0.08
        if head > 0.92:
            head = 0.92
        return True, vx, head, dx, dy
    ax = abs(dx)
    ay = abs(dy)
    sx = 1000.0 if ax < 0.0001 else 0.5 / ax
    sy = 1000.0 if ay < 0.0001 else 0.5 / ay
    scale = sx if sx < sy else sy
    ex = 0.5 + dx * scale
    ey = 0.5 + dy * scale
    ex = 0.04 if ex < 0.04 else (0.96 if ex > 0.96 else ex)
    ey = 0.06 if ey < 0.06 else (0.94 if ey > 0.94 else ey)
    return False, ex, ey, dx, dy


def assert_aim():
    on, _, _, _, _ = project(*VIEW[0])
    if not on:
        raise SystemExit("seat 0 should see It")
    below = project(*VIEW[3])
    if below[0] or below[2] > 0.08 or below[4] >= 0:
        raise SystemExit("seat 3 arrow should sit on the bottom rim")
    side = project(*VIEW[1])
    if side[0] or side[1] < 0.90:
        raise SystemExit("seat 1 arrow should sit on the right rim")
    if IT == SAFE:
        raise SystemExit("It and safe are the same seat")


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


def arrow(d, cx, cy, dx, dy):
    mag = (dx * dx + dy * dy) ** 0.5 or 1
    ux, uy = dx / mag, dy / mag
    px, py = -uy, ux
    tip = (cx + ux * 26, cy + uy * 26)
    left = (cx - ux * 16 + px * 16, cy - uy * 16 + py * 16)
    right = (cx - ux * 16 - px * 16, cy - uy * 16 - py * 16)
    d.polygon([tip, left, right], fill=GOLD)


def ring(d, cx, cy, rad, fill_amt, color):
    d.ellipse((cx - rad, cy - rad, cx + rad, cy + rad), fill=(16, 22, 36))
    if fill_amt <= 0:
        return
    d.pieslice(
        (cx - rad + 3, cy - rad + 3, cx + rad - 3, cy + rad - 3),
        -90,
        -90 + 360 * fill_amt,
        fill=color,
    )


def figure(d, cx, cy, fw, fh, color):
    d.ellipse((cx - fw * 0.22, cy - fh * 0.55, cx + fw * 0.22, cy - fh * 0.28), fill=color)
    d.rounded_rectangle((cx - fw * 0.28, cy - fh * 0.26, cx + fw * 0.28, cy + fh * 0.45), 12, fill=color)


def pane_box(humans, index):
    if humans <= 1:
        return (0, 0, W, H)
    if humans == 2:
        return (0, 0, W // 2, H) if index == 0 else (W // 2, 0, W, H)
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


def play_pane(img, box, seat, comic, shapes, big, checks, ratios):
    d = ImageDraw.Draw(img)
    l, t, r, b = box
    it = seat == IT
    safe = seat == SAFE
    seat_c = ui.SEAT[seat]
    # Border is the seat colour. It and safe do not recolor it.
    d.rectangle((l, t, r - 1, t + 8), fill=seat_c)
    d.rectangle((l, b - 8, r - 1, b - 1), fill=seat_c)
    d.rectangle((l, t, l + 8, b - 1), fill=seat_c)
    d.rectangle((r - 8, t, r - 1, b - 1), fill=seat_c)
    if safe:
        wash = Image.new("RGBA", img.size, (0, 0, 0, 0))
        wd = ImageDraw.Draw(wash)
        wd.rectangle((l + 18, t + 18, r - 18, b - 18), fill=(255, 214, 80, 48))
        img.alpha_composite(wash)
        d = ImageDraw.Draw(img)
    if it:
        d.rectangle((l + 18, t + 18, r - 18, t + 24), fill=GOLD)
        d.rectangle((l + 18, b - 24, r - 18, b - 18), fill=GOLD)
        d.rectangle((l + 18, t + 18, l + 24, b - 18), fill=GOLD)
        d.rectangle((r - 24, t + 18, r - 18, b - 18), fill=GOLD)
    il, itop, ir, ib = insets(box)
    face_clock = ui.font(ui.FONT_D if comic else ui.FONT_B, 46 if big else 32)
    name_face = ui.font(ui.FONT_D if comic else ui.FONT_B, 36 if big else 30)
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
    text(d, (cx0 + 16, cy0 + 8), clock, face_clock, CREAM, INK, (cx0 + 8, cy0 + 4, cx0 + cw - 70, cy0 + ch - 4), checks, ratios)
    rf = ui.font(ui.FONT_B, 24 if big else 20)
    text(d, (cx0 + cw - 92, cy0 + 16), rnd, rf, GOLD, INK, (cx0 + cw - 100, cy0 + 8, cx0 + cw - 8, cy0 + ch - 8), checks, ratios)
    right = l > W * 0.4
    nw, nh = (340 if big else 280), (108 if big else 96)
    nx = r - ir - nw if right else l + il
    ny = t + itop
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
    line = "TIME  " + TIMES[seat] + "    TAGS  " + TAGS[seat]
    text(d, (nx + 18, ny + nh - 36), line, sub, GOLD, INK, (nx + 12, ny + nh - 40, nx + nw - 8, ny + nh - 6), checks, ratios)
    fw, fh = (120, 200) if big else (72, 120)
    fx = (l + r) // 2
    fy = (t + b) // 2 + (40 if big else 16)
    figure(d, fx, fy, fw, fh, seat_c)
    if it:
        iw, ih = (280, 108) if big else (168, 76)
        ix = (l + r - iw) // 2
        iy = fy - int(fh * 0.55) - ih - 8
        if iy < cy0 + ch + 8:
            iy = cy0 + ch + 8
        ui.rounded(d, (ix, iy, ix + iw, iy + ih), 16, GOLD)
        it_face = ui.font(ui.FONT_D, 72 if big else 48)
        tw = d.textlength("IT", font=it_face)
        text(d, (ix + (iw - tw) / 2, iy + 10), "IT", it_face, INK, GOLD, (ix + 8, iy + 4, ix + iw - 8, iy + ih - 4), checks, ratios)
    if not it and seat in VIEW:
        on, ax, ay, dx, dy = project(*VIEW[seat])
        px = l + ax * (r - l)
        py = t + (1.0 - ay) * (b - t)
        if on:
            body_y = t + (1.0 - VIEW[seat][1]) * (b - t)
            figure(d, px, body_y, 64 if big else 48, 110 if big else 84, ui.SEAT[IT])
            ui.rounded(d, (px - 42, py - 36, px + 42, py + 4), 8, INK)
            text(d, (px - 18, py - 32), "IT", ui.font(ui.FONT_D, 28), GOLD, INK, (px - 40, py - 36, px + 40, py + 2), checks, ratios)
        else:
            arrow(d, px, py, dx, -dy)
    dx0 = l + il
    dy0 = b - ib - 78
    cooling = safe
    ring(d, dx0 + 28, dy0 + 24, 26, 0.4 if cooling else 1.0, (64, 158, 255))
    ui.rounded(d, (dx0 + 60, dy0 + 8, dx0 + 168, dy0 + 48), 8, INK)
    df = ui.font(ui.FONT_B, 22)
    dash_word = "18s" if cooling else "DASH"
    text(d, (dx0 + 72, dy0 + 12), dash_word, df, CREAM, INK, (dx0 + 64, dy0 + 8, dx0 + 164, dy0 + 46), checks, ratios)
    if safe:
        ring(d, dx0 + 28, dy0 - 62, 26, 0.8, GOLD)
        ui.rounded(d, (dx0 + 60, dy0 - 78, dx0 + 200, dy0 - 38), 8, INK)
        text(d, (dx0 + 72, dy0 - 72), "SAFE  0.8", df, CREAM, INK, (dx0 + 64, dy0 - 76, dx0 + 196, dy0 - 40), checks, ratios)
    fwrd = 360 if big else 300
    fhgt = 40
    gap = 6
    block = len(FEEDS) * fhgt + (len(FEEDS) - 1) * gap
    fx0 = r - ir - fwrd
    top_feed = b - ib - block
    ff = ui.font(ui.FONT_D if comic else ui.FONT_B, 26 if big else 22)
    for i, (sentence, tagger) in enumerate(FEEDS):
        fy0 = top_feed + i * (fhgt + gap)
        ui.rounded(d, (fx0, fy0, fx0 + fwrd, fy0 + fhgt), 10, INK)
        d.rectangle((fx0 + 8, fy0 + 8, fx0 + 28, fy0 + 32), fill=ui.SEAT[tagger])
        if shapes:
            shape(d, (fx0 + 36, fy0 + 6, fx0 + 60, fy0 + 30), tagger, CREAM)
            feed_x = fx0 + 68
        else:
            feed_x = fx0 + 40
        text(d, (feed_x, fy0 + 6), sentence, ff, CREAM, INK, (feed_x - 4, fy0 + 2, fx0 + fwrd - 8, fy0 + fhgt - 2), checks, ratios)


def score_pane(img, box, shapes, checks, ratios):
    d = ImageDraw.Draw(img)
    l, t, r, b = box
    d.rectangle((l + 24, t + 24, r - 24, b - 24), fill=INK)
    title = ui.font(ui.FONT_D, 36)
    text(d, (l + 48, t + 40), "SCORE", title, CREAM, INK, (l + 36, t + 32, r - 36, t + 88), checks, ratios)
    face = ui.font(ui.FONT_B, 28)
    lines = ("P1   8.1", "P2   12.4", "P3   0.2", "P4   4.0")
    for i, line in enumerate(lines):
        y = t + 110 + i * 72
        if shapes:
            shape(d, (l + 48, y, l + 84, y + 36), i, CREAM)
            x = l + 100
        else:
            x = l + 56
        d.rectangle((l + 36, y - 6, l + 46, y + 42), fill=ui.SEAT[i])
        text(d, (x, y), line, face, CREAM, INK, (l + 40, y - 4, r - 40, y + 44), checks, ratios)


def scene(humans, comic, shapes):
    img = ui.backdrop(0.45)
    checks = []
    ratios = [ui.contrast(GOLD, INK), ui.contrast(CREAM, INK), ui.contrast(INK, GOLD)]
    panes = 4 if humans >= 3 else humans
    seen_feed = None
    for i in range(panes):
        box = pane_box(humans, i)
        if humans == 3 and i == 3:
            score_pane(img, box, shapes, checks, ratios)
            continue
        if seen_feed is None:
            seen_feed = FEEDS
        elif seen_feed != FEEDS:
            raise SystemExit("feed split")
        play_pane(img, box, i, comic, shapes, humans <= 2, checks, ratios)
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
    assert_aim()
    jobs = [
        ("20-hud-1-composite.png", 1, True, False),
        ("20-hud-2-composite.png", 2, False, False),
        ("20-hud-3-composite.png", 3, True, True),
        ("20-hud-4-composite.png", 4, True, True),
    ]
    notes = []
    worst = 99
    for name, humans, comic, shapes in jobs:
        img, ratio, checks = scene(humans, comic, shapes)
        miss = verify(img, checks, name)
        ui.save(img, name, notes)
        worst = min(worst, ratio)
        print("  contrast %.2f text %.1f" % (ratio, miss))
    print("min contrast", round(worst, 2))


if __name__ == "__main__":
    main()
