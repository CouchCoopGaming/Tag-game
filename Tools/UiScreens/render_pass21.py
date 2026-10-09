#!/usr/bin/env python3
"""Pass 21 in-match moments. Same script as HudState.

Countdown, hot clock, round over, standings, results, tag flash,
solo, and a mid-round drop. Unity Editor is not installed, so these
are composites.
"""

import os

import render_screens2 as ui
from PIL import Image, ImageDraw

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass21")

W, H = ui.W, ui.H
INK = ui.INK
CREAM = ui.CREAM
GOLD = ui.GOLD

IT = 2
SAFE = 1
OPENING = 0
DROPPED = 3
FEEDS = (("P2 tagged P3", 1), ("P1 tagged P2", 0))
DROP_FEEDS = (("P4 left", 3),) + FEEDS
VIEW = {
    0: (0.36, 0.48, 1.0),
    1: (1.45, 0.22, 1.0),
    3: (0.50, -0.55, 1.0),
}
TIMES = {0: "8.1", 1: "12.4", 2: "0.2", 3: "4.0"}
TAGS = {0: "1", 1: "1", 2: "0", 3: "0"}
PLACE = (2, 3, 0, 1)


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


def assert_script():
    on, _, _, _, _ = project(*VIEW[0])
    if not on:
        raise SystemExit("seat 0 should see It")
    order = [float(TIMES[s]) for s in PLACE]
    if order != sorted(order):
        raise SystemExit("standings are not least-it order")
    if PLACE[0] != IT:
        raise SystemExit("least time is not the current It")
    if OPENING == IT:
        raise SystemExit("opening It and the later It are the same seat")
    for word in ("ROUND OVER", "LOCKED", "RESULTS", "NEXT ROUND", "LEAST IT TIME WINS", "P4 left", "YOU'RE IT!", "GO", "AI"):
        if "podium" in word.lower():
            raise SystemExit("podium in copy")


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


def play_pane(img, box, seat, opt, checks, ratios):
    d = ImageDraw.Draw(img)
    l, t, r, b = box
    it = seat == opt["it"]
    safe = seat == opt["safe"]
    big = opt["big"]
    comic = opt["comic"]
    shapes = opt["shapes"]
    seat_c = ui.SEAT[seat]
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
    if opt.get("flash") and seat == opt["it"]:
        d.rectangle((l + 8, t + 8, r - 9, t + 26), fill=GOLD)
        d.rectangle((l + 8, b - 26, r - 9, b - 9), fill=GOLD)
        d.rectangle((l + 8, t + 8, l + 26, b - 9), fill=GOLD)
        d.rectangle((r - 26, t + 8, r - 9, b - 9), fill=GOLD)
    il, itop, ir, ib = insets(box)
    clock = opt["clock"]
    clock_fill = opt["clock_fill"]
    face_clock = ui.font(ui.FONT_D, 46 if big else 32)
    name_face = ui.font(ui.FONT_D if comic else ui.FONT_B, 36 if big else 30)
    rnd = opt["round"]
    scale = opt.get("pulse", 1.0)
    cw = int((250 if big else 210) * scale)
    ch = int((64 if big else 52) * scale)
    cx0 = (l + r - cw) // 2
    cy0 = t + itop
    if cx0 < l + il:
        cx0 = l + il
    if cx0 + cw > r - ir:
        cx0 = r - ir - cw
    ui.rounded(d, (cx0, cy0, cx0 + cw, cy0 + ch), 12, INK)
    text(d, (cx0 + 16, cy0 + 8), clock, face_clock, clock_fill, INK, (cx0 + 8, cy0 + 4, cx0 + cw - 70, cy0 + ch - 4), checks, ratios)
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
    if it and not opt.get("call"):
        ui.rounded(d, (nx + nw - 78, ny + 10, nx + nw - 10, ny + 50), 8, GOLD)
        bf = ui.font(ui.FONT_D, 28)
        text(d, (nx + nw - 64, ny + 12), "IT", bf, INK, GOLD, (nx + nw - 76, ny + 8, nx + nw - 12, ny + 48), checks, ratios)
    sub = ui.font(ui.FONT_B, 22 if big else 20)
    line = "TIME  " + TIMES[seat] + "    TAGS  " + TAGS[seat]
    if opt.get("show_stats", True):
        text(d, (nx + 18, ny + nh - 36), line, sub, GOLD, INK, (nx + 12, ny + nh - 40, nx + nw - 8, ny + nh - 6), checks, ratios)
    fw, fh = (120, 200) if big else (72, 120)
    fx = (l + r) // 2
    fy = (t + b) // 2 + (40 if big else 16)
    figure(d, fx, fy, fw, fh, seat_c)
    call = opt.get("call")
    if call:
        cf = ui.font(ui.FONT_D, 108 if big else 64)
        tw = d.textlength(call, font=cf)
        pw, ph = tw + 56, (132 if big else 84)
        px = (l + r - pw) / 2
        py = fy - int(fh * 0.55) - ph - 12
        if py < cy0 + ch + 6:
            py = cy0 + ch + 6
        ui.rounded(d, (px, py, px + pw, py + ph), 16, INK)
        text(d, (px + 28, py + 8), call, cf, CREAM, INK, (px + 12, py + 4, px + pw - 12, py + ph - 4), checks, ratios)
    elif it:
        iw, ih = (280, 108) if big else (168, 76)
        ix = (l + r - iw) // 2
        iy = fy - int(fh * 0.55) - ih - 8
        if iy < cy0 + ch + 8:
            iy = cy0 + ch + 8
        ui.rounded(d, (ix, iy, ix + iw, iy + ih), 16, GOLD)
        it_face = ui.font(ui.FONT_D, 72 if big else 48)
        tw = d.textlength("IT", font=it_face)
        text(d, (ix + (iw - tw) / 2, iy + 10), "IT", it_face, INK, GOLD, (ix + 8, iy + 4, ix + iw - 8, iy + ih - 4), checks, ratios)
    comic_word = opt.get("comic_word")
    if comic and comic_word and it:
        wf = ui.font(ui.FONT_D, 42 if big else 28)
        tw = d.textlength(comic_word, font=wf)
        ww, wh = tw + 36, (64 if big else 48)
        wx = (l + r - ww) / 2
        wy = fy + int(fh * 0.2)
        ui.rounded(d, (wx, wy, wx + ww, wy + wh), 12, INK)
        text(d, (wx + 18, wy + 6), comic_word, wf, GOLD, INK, (wx + 8, wy + 2, wx + ww - 8, wy + wh - 2), checks, ratios)
    if opt.get("aim") and not it and seat in VIEW:
        on, ax, ay, dx, dy = project(*VIEW[seat])
        px = l + ax * (r - l)
        py = t + (1.0 - ay) * (b - t)
        if on:
            body_y = t + (1.0 - VIEW[seat][1]) * (b - t)
            figure(d, px, body_y, 64 if big else 48, 110 if big else 84, ui.SEAT[IT])
            ui.rounded(d, (px - 42, py - 36, px + 42, py + 4), 8, INK)
            text(d, (px - 18, py - 32), "IT", ui.font(ui.FONT_D, 28), GOLD, INK, (px - 40, py - 36, px + 40, py + 2), checks, ratios)
            if opt.get("ai"):
                af = ui.font(ui.FONT_B, 22)
                ui.rounded(d, (px - 28, body_y + 36, px + 28, body_y + 68), 8, INK)
                text(d, (px - 14, body_y + 40), "AI", af, CREAM, INK, (px - 26, body_y + 36, px + 26, body_y + 66), checks, ratios)
        else:
            arrow(d, px, py, dx, -dy)
    if opt.get("verbs", True):
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
    feeds = opt.get("feeds") or ()
    if feeds:
        fwrd = 360 if big else 300
        fhgt = 34 if len(feeds) > 2 else 40
        gap = 6
        block = len(feeds) * fhgt + (len(feeds) - 1) * gap
        fx0 = r - ir - fwrd
        top_feed = b - ib - block
        ff = ui.font(ui.FONT_D if comic else ui.FONT_B, 22 if big else 18)
        for i, (sentence, tagger) in enumerate(feeds):
            fy0 = top_feed + i * (fhgt + gap)
            ui.rounded(d, (fx0, fy0, fx0 + fwrd, fy0 + fhgt), 10, INK)
            d.rectangle((fx0 + 8, fy0 + 8, fx0 + 28, fy0 + fhgt - 8), fill=ui.SEAT[tagger])
            feed_x = fx0 + 40
            text(d, (feed_x, fy0 + 4), sentence, ff, CREAM, INK, (feed_x - 4, fy0 + 2, fx0 + fwrd - 8, fy0 + fhgt - 2), checks, ratios)


def score_pane(img, box, checks, ratios):
    d = ImageDraw.Draw(img)
    l, t, r, b = box
    d.rectangle((l + 24, t + 24, r - 24, b - 24), fill=INK)
    title = ui.font(ui.FONT_D, 36)
    text(d, (l + 48, t + 40), "SCORE", title, CREAM, INK, (l + 36, t + 32, r - 36, t + 88), checks, ratios)
    face = ui.font(ui.FONT_B, 28)
    lines = ("P1   8.1", "P2   12.4", "P3   0.2")
    for i, line in enumerate(lines):
        y = t + 120 + i * 80
        d.rectangle((l + 48, y, l + 62, y + 40), fill=ui.SEAT[i])
        text(d, (l + 80, y), line, face, CREAM, INK, (l + 70, y - 4, r - 40, y + 44), checks, ratios)


def center_card(img, word, checks, ratios, y=None):
    d = ImageDraw.Draw(img)
    face = ui.font(ui.FONT_D, 84)
    tw = d.textlength(word, font=face)
    bw = tw + 96
    bh = 148
    x = (W - bw) / 2
    yy = (H - bh) / 2 if y is None else y
    veil = Image.new("RGBA", img.size, (0, 0, 0, 0))
    vd = ImageDraw.Draw(veil)
    vd.rectangle((x - 24, yy - 24, x + bw + 24, yy + bh + 24), fill=(2, 6, 18, 90))
    img.alpha_composite(veil)
    d = ImageDraw.Draw(img)
    ui.rounded(d, (x, yy, x + bw, yy + bh), 18, INK)
    text(d, (x + 48, yy + 24), word, face, CREAM, INK, (x + 16, yy + 12, x + bw - 16, yy + bh - 12), checks, ratios)


def standings(img, checks, ratios):
    d = ImageDraw.Draw(img)
    plate = (W * 0.22, H * 0.12, W * 0.78, H * 0.88)
    ui.rounded(d, plate, 20, INK)
    title = ui.font(ui.FONT_D, 54)
    text(d, (plate[0] + 48, plate[1] + 28), "LEAST IT TIME WINS", title, CREAM, INK, (plate[0] + 24, plate[1] + 16, plate[2] - 24, plate[1] + 100), checks, ratios)
    face = ui.font(ui.FONT_B, 40)
    for i, seat in enumerate(PLACE):
        y = plate[1] + 140 + i * 110
        d.rectangle((plate[0] + 48, y, plate[0] + 72, y + 56), fill=ui.SEAT[seat])
        line = "P%d   %s" % (seat + 1, TIMES[seat])
        text(d, (plate[0] + 96, y + 4), line, face, CREAM, INK, (plate[0] + 84, y, plate[2] - 40, y + 60), checks, ratios)
    foot = ui.font(ui.FONT_D, 42)
    text(d, (plate[0] + 48, plate[3] - 80), "NEXT ROUND", foot, GOLD, INK, (plate[0] + 36, plate[3] - 88, plate[2] - 36, plate[3] - 16), checks, ratios)


def results_card(img, checks, ratios):
    d = ImageDraw.Draw(img)
    plate = (W * 0.18, H * 0.28, W * 0.82, H * 0.72)
    ui.rounded(d, plate, 20, INK)
    face = ui.font(ui.FONT_D, 96)
    word = "RESULTS"
    tw = d.textlength(word, font=face)
    text(d, ((W - tw) / 2, plate[1] + 48), word, face, CREAM, INK, (plate[0] + 24, plate[1] + 36, plate[2] - 24, plate[1] + 170), checks, ratios)
    sub = ui.font(ui.FONT_B, 32)
    line = "LEAST IT TIME WINS"
    tw = d.textlength(line, font=sub)
    text(d, ((W - tw) / 2, plate[1] + 190), line, sub, GOLD, INK, (plate[0] + 24, plate[1] + 180, plate[2] - 24, plate[3] - 24), checks, ratios)


def base_opt(**extra):
    opt = {
        "it": IT,
        "safe": SAFE,
        "feeds": FEEDS,
        "comic": True,
        "shapes": False,
        "big": False,
        "clock": "1:24",
        "clock_fill": CREAM,
        "round": "R2 / 3",
        "aim": True,
        "verbs": True,
        "show_stats": True,
        "pulse": 1.0,
    }
    opt.update(extra)
    return opt


def scene_panes(humans, opt):
    img = ui.backdrop(0.45)
    checks = []
    ratios = [ui.contrast(GOLD, INK), ui.contrast(CREAM, INK), ui.contrast(INK, GOLD)]
    panes = 4 if humans >= 3 else humans
    feeds = opt.get("feeds")
    for i in range(panes):
        box = pane_box(humans, i)
        if humans == 3 and i == 3:
            score_pane(img, box, checks, ratios)
            continue
        play_pane(img, box, i, opt, checks, ratios)
    if feeds and humans >= 2:
        if opt.get("feeds") != feeds:
            raise SystemExit("feed split")
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


def near(px, rgb, tol):
    return abs(px[0] - rgb[0]) <= tol and abs(px[1] - rgb[1]) <= tol and abs(px[2] - rgb[2]) <= tol


def region_has(img, box, rgb, tol):
    l, t, r, b = box
    for y in range(t, b, 2):
        for x in range(l, r, 2):
            if near(img.getpixel((x, y)), rgb, tol):
                return True
    return False


def probe(path):
    img = Image.open(path).convert("RGB")
    name = os.path.basename(path)
    four = name.startswith("21-count") or name.startswith("21-go") or name.startswith("21-hot") or name.startswith("21-tag") or name.startswith("21-over")
    if four:
        seats = (img.getpixel((4, 4)), img.getpixel((1914, 4)), img.getpixel((4, 1074)), img.getpixel((1914, 1074)))
        for i, px in enumerate(seats):
            if not near(px, ui.SEAT[i], 8):
                raise SystemExit("%s border %d %s" % (name, i, px))
    if name.startswith("21-count"):
        if not near(img.getpixel((24, 24)), GOLD, 18):
            raise SystemExit("opening It frame missing %s" % (img.getpixel((24, 24),)))
        if near(img.getpixel((984, 24)), GOLD, 12):
            raise SystemExit("It frame leaked onto P2")
    if name.startswith("21-tag"):
        if not near(img.getpixel((16, 556)), GOLD, 20):
            raise SystemExit("tag flash missing on P3 %s" % (img.getpixel((16, 556)),))
        if near(img.getpixel((16, 16)), GOLD, 12):
            raise SystemExit("tag flash leaked onto P1")
    if name.startswith("21-drop"):
        corner = img.getpixel((964, 544))
        if near(corner, ui.SEAT[3], 12):
            raise SystemExit("dropped pane still has a P4 border")
    if name.startswith("21-hot"):
        if not region_has(img, (400, 48, 560, 120), GOLD, 8):
            raise SystemExit("hot clock is not gold")
    if b"podium" in img.tobytes() or b"PODIUM" in img.tobytes():
        raise SystemExit("podium pixels in %s" % name)


def main():
    assert_script()
    built = []
    count_img, count_ratio, count_checks = scene_panes(4, base_opt(
        it=OPENING, safe=-1, feeds=(), call="3", aim=False, show_stats=False, verbs=False, round="R1 / 3", clock="1:30",
    ))
    center_card(count_img, "LOCKED", count_checks, [])
    built.append(("21-count-composite.png", count_img, count_checks, count_ratio))

    go_img, go_ratio, go_checks = scene_panes(4, base_opt(
        it=OPENING, safe=-1, feeds=(), call="GO", aim=False, show_stats=False, verbs=False, round="R1 / 3", clock="1:30",
    ))
    built.append(("21-go-composite.png", go_img, go_checks, go_ratio))

    hot_img, hot_ratio, hot_checks = scene_panes(4, base_opt(
        clock="0:08", clock_fill=GOLD, pulse=1.12, round="R2 / 3",
    ))
    built.append(("21-hot-composite.png", hot_img, hot_checks, hot_ratio))

    over_img, over_ratio, over_checks = scene_panes(4, base_opt(round="R2 / 3"))
    built.append(("21-over-composite.png", over_img, over_checks, over_ratio))

    stand_img = ui.backdrop(0.35)
    stand_checks = []
    stand_ratios = [ui.contrast(GOLD, INK), ui.contrast(CREAM, INK)]
    standings(stand_img, stand_checks, stand_ratios)
    if min(stand_ratios) < 4.5:
        raise SystemExit("standings contrast")
    built.append(("21-standings-composite.png", stand_img, stand_checks, min(stand_ratios)))

    res_img = ui.backdrop(0.35)
    res_checks = []
    res_ratios = [ui.contrast(GOLD, INK), ui.contrast(CREAM, INK)]
    results_card(res_img, res_checks, res_ratios)
    if min(res_ratios) < 4.5:
        raise SystemExit("results contrast")
    built.append(("21-results-composite.png", res_img, res_checks, min(res_ratios)))

    tag_img, tag_ratio, tag_checks = scene_panes(4, base_opt(
        flash=True, comic_word="YOU'RE IT!", round="R2 / 3",
    ))
    built.append(("21-tag-composite.png", tag_img, tag_checks, tag_ratio))

    solo_img, solo_ratio, solo_checks = scene_panes(1, base_opt(
        big=True, round="R1", clock="1:24", ai=True, comic=True,
    ))
    built.append(("21-solo-composite.png", solo_img, solo_checks, solo_ratio))

    drop_img, drop_ratio, drop_checks = scene_panes(3, base_opt(
        feeds=DROP_FEEDS, round="R2 / 3", comic=True,
    ))
    built.append(("21-drop-composite.png", drop_img, drop_checks, drop_ratio))

    notes = []
    worst = 99
    for name, img, checks, ratio in built:
        miss = verify(img, checks, name)
        if name.startswith("21-over"):
            extra = []
            center_card(img, "ROUND OVER", extra, [])
            miss = max(miss, verify(img, extra, name))
        ui.save(img, name, notes)
        probe(os.path.join(ui.OUT, name))
        worst = min(worst, ratio)
        print("  contrast %.2f text %.1f" % (ratio, miss))
    print("min contrast", round(worst, 2))


if __name__ == "__main__":
    main()
