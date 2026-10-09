#!/usr/bin/env python3
"""Pass 22. Standings columns, a slim countdown, a comic-word slot, loading.

Same script as HudState. Unity Editor is not installed, so these are composites.
"""

import os

import render_screens2 as ui
from PIL import Image, ImageDraw

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass22")

W, H = ui.W, ui.H
INK = ui.INK
CREAM = ui.CREAM
GOLD = ui.GOLD

PLACE = (2, 3, 0, 1)
TIME = {0: "8.1", 1: "12.4", 2: "0.2", 3: "4.0"}
TAGS = {0: "1", 1: "1", 2: "0", 3: "0"}
TAGGED = {0: "0", 1: "1", 2: "1", 3: "0"}
WINS = {0: "0", 1: "0", 2: "1", 3: "0"}
TIPS = (
    "Space jumps.",
    "Sprint, then slide.",
    "Hold into a wall to climb.",
    "Hold into a wall to cling. Jump while clinging to wall jump.",
)
OPENING = 0
IT = 2
SAFE = 1


def slide(frame, row):
    if frame > row:
        return 0.0
    if frame < row:
        return 1.0
    return 0.42


def overlaps(a, b):
    return a[0] < b[2] and a[2] > b[0] and a[1] < b[3] and a[3] > b[1]


def assert_script():
    if slide(3, 0) != 0 or slide(0, 3) != 1 or slide(1, 1) != 0.42:
        raise SystemExit("slide")
    order = [float(TIME[s]) for s in PLACE]
    if order != sorted(order) or PLACE[0] != IT or WINS[IT] != "1":
        raise SystemExit("standings")
    it = (0.18, 0.62, 0.64, 0.16)
    word = (0.56, 0.30, 0.34, 0.18)
    a = (it[0], it[1], it[0] + it[2], it[1] + it[3])
    b = (word[0], word[1], word[0] + word[2], word[1] + word[3])
    if overlaps(a, b):
        raise SystemExit("word slot overlaps YOU'RE IT")
    for word in ("RESULTS", "NEXT ROUND", "LOCKED", "ROUND OVER", "LEAST IT TIME WINS"):
        if "podium" in word.lower():
            raise SystemExit("podium")


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
    n = acc = 0
    for i in range(len(bp)):
        if bp[i] == bg:
            continue
        n += 1
        acc += abs(ap[i][0] - bp[i][0]) + abs(ap[i][1] - bp[i][1]) + abs(ap[i][2] - bp[i][2])
    if n < 8:
        return 999
    return acc / (n * 3)


def slide_text(d, xy, word, face, fill, bg, box, checks, ratios, origin, limit):
    """A column sliding in may leave the card. Draw it; only measure the part still on the card."""
    bb = d.textbbox(xy, word, font=face)
    if bb[2] <= 4 or bb[0] >= limit - 4:
        return
    inside = (
        bb[0] >= box[0] - 3
        and bb[1] >= box[1] - 3
        and bb[2] <= min(box[2], limit - 2) + 3
        and bb[3] <= box[3] + 3
    )
    if not inside:
        d.text(xy, word, font=face, fill=fill)
        return
    text(d, xy, word, face, fill, bg, box, checks, ratios, origin)


def text(d, xy, word, face, fill, bg, box, checks, ratios, origin=(0, 0)):
    d.text(xy, word, font=face, fill=fill)
    ok, bb = ui.text_inside(d, xy, word, face, box)
    if not ok:
        raise SystemExit("clip %r %s" % (word, bb))
    ratios.append(ui.contrast(fill, bg))
    checks.append(((xy[0] + origin[0], xy[1] + origin[1]), word, face, fill, bg))


def shape(d, box, kind, fill):
    l, t, r, b = box
    cx, cy = (l + r) / 2, (t + b) / 2
    if kind == 0:
        d.ellipse(box, fill=fill)
    elif kind == 2:
        d.rectangle(box, fill=fill)
    elif kind == 3:
        d.polygon([(cx, t), (r, cy), (cx, b), (l, cy)], fill=fill)
    else:
        d.polygon([(cx, t), (l, b), (r, b)], fill=fill)


def pane_box(humans, index, bounds=None):
    x0, y0, x1, y1 = (0, 0, W, H) if bounds is None else bounds
    pw, ph = x1 - x0, y1 - y0
    if humans <= 1:
        return (x0, y0, x1, y1)
    if humans == 2:
        mid = x0 + pw // 2
        return (x0, y0, mid, y1) if index == 0 else (mid, y0, x1, y1)
    col, row = index % 2, index // 2
    return (
        x0 + col * pw // 2,
        y0 + row * ph // 2,
        x0 + (col + 1) * pw // 2,
        y0 + (row + 1) * ph // 2,
    )


def borders(d, box, seat):
    l, t, r, b = box
    c = ui.SEAT[seat]
    d.rectangle((l, t, r - 1, t + 8), fill=c)
    d.rectangle((l, b - 8, r - 1, b - 1), fill=c)
    d.rectangle((l, t, l + 8, b - 1), fill=c)
    d.rectangle((r - 8, t, r - 1, b - 1), fill=c)


def name_row(d, box, seat, checks, ratios, stats):
    l, t, r, b = box
    tall = 108 if stats else 56
    plate = (l + 28, t + 36, l + 28 + (320 if r - l > 1000 else 250), t + 36 + tall)
    ui.rounded(d, plate, 12, INK)
    d.rectangle((plate[0], plate[1], plate[0] + 10, plate[3]), fill=ui.SEAT[seat])
    face = ui.font(ui.FONT_D, 32 if r - l > 1000 else 28)
    text(d, (plate[0] + 22, plate[1] + 8), "P%d" % (seat + 1), face, CREAM, INK, (plate[0] + 16, plate[1] + 4, plate[2] - 8, plate[1] + 48), checks, ratios)
    if stats:
        sub = ui.font(ui.FONT_B, 20)
        line = "TIME  %s    TAGS  %s" % (TIME[seat], TAGS[seat])
        text(d, (plate[0] + 18, plate[3] - 34), line, sub, GOLD, INK, (plate[0] + 12, plate[3] - 38, plate[2] - 8, plate[3] - 4), checks, ratios)
    return plate


def count_pane(img, box, seat, go, checks, ratios):
    d = ImageDraw.Draw(img)
    borders(d, box, seat)
    if seat == OPENING:
        l, t, r, b = box
        d.rectangle((l + 18, t + 18, r - 18, t + 24), fill=GOLD)
        d.rectangle((l + 18, b - 24, r - 18, b - 18), fill=GOLD)
        d.rectangle((l + 18, t + 18, l + 24, b - 18), fill=GOLD)
        d.rectangle((r - 24, t + 18, r - 18, b - 18), fill=GOLD)
    name_row(d, box, seat, checks, ratios, go)
    l, t, r, b = box
    word = "GO" if go else "3"
    face = ui.font(ui.FONT_D, 92 if r - l > 1000 else 64)
    tw = d.textlength(word, font=face)
    pw, ph = tw + 48, (120 if r - l > 1000 else 84)
    px = (l + r - pw) / 2
    py = (t + b) / 2 - ph
    ui.rounded(d, (px, py, px + pw, py + ph), 14, INK)
    text(d, (px + 24, py + 8), word, face, CREAM, INK, (px + 8, py + 4, px + pw - 8, py + ph - 4), checks, ratios)
    if go:
        return
    lock = ui.font(ui.FONT_B, 22)
    lw = d.textlength("LOCKED", font=lock)
    lx = (l + r - lw) / 2 - 16
    ly = py + ph + 10
    ui.rounded(d, (lx, ly, lx + lw + 32, ly + 40), 8, INK)
    text(d, (lx + 16, ly + 6), "LOCKED", lock, CREAM, INK, (lx + 8, ly + 2, lx + lw + 24, ly + 36), checks, ratios)


def draw_standings(img, area, frame, checks, ratios):
    """frame is None for the settled card, or 0..3 for a slide frame.

    The card is its own image so a row sliding in is clipped at the card edge
    and cannot paint into the next frame of the strip.
    """
    ax0, ay0, ax1, ay1 = area
    tall = ay1 - ay0 > 700
    rows = 4 if frame is None else frame + 1
    row_h = 64 if tall else 46
    head = 92 if tall else 70
    foot = 48 if tall else 36
    height = head + rows * row_h + foot + 16
    width = min(1180, ax1 - ax0 - 48)
    if not tall:
        width = min(width, ax1 - ax0 - 24)
    width = int(width)
    height = int(height)
    x0 = int(round((ax0 + ax1 - width) / 2))
    y0 = int(round((ay0 + ay1 - height) / 2))
    card = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    d = ImageDraw.Draw(card)
    ui.rounded(d, (0, 0, width - 1, height - 1), 16, INK)
    title_px = 40 if tall else 26
    title = ui.font(ui.FONT_D, title_px)
    text(d, (24, 12), "LEAST IT TIME WINS", title, CREAM, INK, (12, 6, width - 12, head - 28), checks, ratios, (x0, y0))
    head_face = ui.font(ui.FONT_B, 18 if tall else 14)
    # Same fractions as MatchHudView score columns: rank, time, tags, tagged, wins.
    heads = (("#", 0.02), ("TIME", 0.42), ("TAGS", 0.56), ("TAGGED", 0.68), ("WINS", 0.82))
    hy = 52 if tall else 40
    span = width - 32
    for word, frac in heads:
        text(d, (16 + span * frac, hy), word, head_face, GOLD, INK, (8, hy - 2, width - 8, hy + 24), checks, ratios, (x0, y0))
    body = ui.font(ui.FONT_B, 26 if tall else 18)
    for i in range(rows):
        off = 0.0 if frame is None else slide(frame, i)
        if off >= 1:
            continue
        seat = PLACE[i]
        win = i == 0
        y = head + i * row_h
        shift = off * (width * 0.72)
        row = (16 + shift, y, width - 16, y + row_h - 8)
        if row[0] >= width - 20:
            continue
        bg = GOLD if win else (16, 28, 56)
        fg = INK if win else CREAM
        ui.rounded(d, row, 8, bg)
        label = "%d" % (i + 1)
        text(d, (16 + span * 0.02 + shift, row[1] + 8), label, body, fg, bg, (row[0] + 4, row[1] + 2, min(row[0] + 48, width - 8), row[3] - 2), checks, ratios, (x0, y0))
        mark = 16 + span * 0.18 + shift
        shape(d, (mark, row[1] + 10, mark + 26, row[1] + 36), seat, fg)
        chip = 16 + span * 0.12 + shift
        d.rectangle((chip, row[1] + 12, chip + 16, row[1] + 38), fill=ui.SEAT[seat])
        name = "P%d" % (seat + 1)
        text(d, (16 + span * 0.28 + shift, row[1] + 8), name, body, fg, bg, (8, row[1] + 2, width - 8, row[3] - 2), checks, ratios, (x0, y0))
        cols = ((TIME[seat], 0.42), (TAGS[seat], 0.56), (TAGGED[seat], 0.68), (WINS[seat], 0.82))
        for value, frac in cols:
            slide_text(d, (16 + span * frac + shift, row[1] + 8), value, body, fg, bg, (8, row[1] + 2, width - 8, row[3] - 2), checks, ratios, (x0, y0), width)
    foot_face = ui.font(ui.FONT_D, 28 if tall else 18)
    text(d, (24, height - foot), "NEXT ROUND", foot_face, GOLD, INK, (12, height - foot - 4, width - 12, height - 8), checks, ratios, (x0, y0))
    img.alpha_composite(card, (x0, y0))


def tag_pane(img, box, seat, checks, ratios):
    d = ImageDraw.Draw(img)
    l, t, r, b = box
    borders(d, box, seat)
    if seat == IT:
        d.rectangle((l + 8, t + 8, r - 9, t + 22), fill=GOLD)
        d.rectangle((l + 8, b - 22, r - 9, b - 9), fill=GOLD)
        d.rectangle((l + 8, t + 8, l + 22, b - 9), fill=GOLD)
        d.rectangle((r - 22, t + 8, r - 9, b - 9), fill=GOLD)
        ix, iy, iw, ih = 0.18, 0.62, 0.64, 0.16
        px0 = l + ix * (r - l)
        py0 = t + (1 - (iy + ih)) * (b - t)
        px1 = l + (ix + iw) * (r - l)
        py1 = t + (1 - iy) * (b - t)
        ui.rounded(d, (px0, py0, px1, py1), 12, INK)
        face = ui.font(ui.FONT_D, 36 if r - l > 1000 else 26)
        word = "YOU'RE IT!"
        tw = d.textlength(word, font=face)
        bb = d.textbbox((0, 0), word, font=face)
        th = bb[3] - bb[1]
        tx = px0 + ((px1 - px0) - tw) / 2
        ty = py0 + ((py1 - py0) - th) / 2 - bb[1]
        text(d, (tx, ty), word, face, GOLD, INK, (px0 + 6, py0 + 4, px1 - 6, py1 - 4), checks, ratios)
        wx, wy, ww, wh = 0.56, 0.30, 0.34, 0.18
        sx0 = l + wx * (r - l)
        sy0 = t + (1 - (wy + wh)) * (b - t)
        sx1 = l + (wx + ww) * (r - l)
        sy1 = t + (1 - wy) * (b - t)
        d.rounded_rectangle((sx0, sy0, sx1, sy1), 12, outline=GOLD, width=4)
        call = (px0, py0, px1, py1)
        slot = (sx0, sy0, sx1, sy1)
        if overlaps(call, slot):
            raise SystemExit("slot overlap")
    if seat == SAFE:
        wash = Image.new("RGBA", img.size, (0, 0, 0, 0))
        wd = ImageDraw.Draw(wash)
        wd.rectangle((l + 18, t + 18, r - 18, b - 18), fill=(255, 214, 80, 40))
        img.alpha_composite(wash)
    name_row(ImageDraw.Draw(img), box, seat, checks, ratios, True)


def load_pane(img, box, seat, checks, ratios, arena_only=False):
    d = ImageDraw.Draw(img)
    l, t, r, b = box
    if not arena_only:
        borders(d, box, seat)
    plate = (l + 28, t + 48, r - 28, b - 36)
    ui.rounded(d, plate, 16, INK)
    name = ui.font(ui.FONT_D, 40 if r - l > 1000 else 28)
    text(d, (plate[0] + 24, plate[1] + 16), "Mega Park", name, GOLD, INK, (plate[0] + 12, plate[1] + 8, plate[2] - 12, plate[1] + 70), checks, ratios)
    if arena_only:
        return
    who = ui.font(ui.FONT_B, 24)
    d.rectangle((plate[0] + 24, plate[1] + 78, plate[0] + 40, plate[1] + 106), fill=ui.SEAT[seat])
    text(d, (plate[0] + 52, plate[1] + 72), "P%d" % (seat + 1), who, CREAM, INK, (plate[0] + 48, plate[1] + 66, plate[0] + 160, plate[1] + 108), checks, ratios)
    tip = TIPS[seat]
    face = ui.font(ui.FONT_B, 28 if r - l > 1000 else 18)
    tw = d.textlength(tip, font=face)
    room = plate[2] - plate[0] - 48
    if tw > room:
        face = ui.font(ui.FONT_B, 16)
    tip_box = (plate[0] + 20, plate[1] + 120, plate[2] - 20, plate[1] + 188)
    ui.rounded(d, tip_box, 10, GOLD)
    text(d, (tip_box[0] + 16, tip_box[1] + 16), tip, face, INK, GOLD, (tip_box[0] + 8, tip_box[1] + 8, tip_box[2] - 8, tip_box[3] - 8), checks, ratios)
    track = (plate[0] + 24, plate[3] - 92, plate[2] - 24, plate[3] - 60)
    ui.rounded(d, track, 8, (8, 14, 28))
    span = track[2] - track[0]
    ui.rounded(d, (track[0], track[1], track[0] + span * 0.5, track[3]), 8, GOLD)
    cap = ui.font(ui.FONT_B, 22 if r - l > 1000 else 18)
    text(d, (plate[0] + 24, plate[3] - 48), "Starting  50%", cap, CREAM, INK, (plate[0] + 16, plate[3] - 52, plate[2] - 16, plate[3] - 12), checks, ratios)


def scene(kind, humans=4):
    img = ui.backdrop(0.45)
    checks = []
    ratios = [ui.contrast(GOLD, INK), ui.contrast(CREAM, INK), ui.contrast(INK, GOLD)]
    if kind == "count" or kind == "go":
        for i in range(4):
            count_pane(img, pane_box(4, i), i, kind == "go", checks, ratios)
    elif kind == "stand":
        draw_standings(img, (0, 0, W, H), None, checks, ratios)
    elif kind == "strip":
        cells = ((0, 0), (1, 0), (0, 1), (1, 1))
        for frame, (cx, cy) in enumerate(cells):
            area = (cx * W // 2, cy * H // 2, (cx + 1) * W // 2, (cy + 1) * H // 2)
            draw_standings(img, area, frame, checks, ratios)
    elif kind == "tag":
        for i in range(4):
            tag_pane(img, pane_box(4, i), i, checks, ratios)
    elif kind == "load":
        panes = 4 if humans >= 3 else humans
        for i in range(panes):
            box = pane_box(humans, i)
            if humans == 3 and i == 3:
                load_pane(img, box, i, checks, ratios, True)
            else:
                load_pane(img, box, i, checks, ratios, False)
    if min(ratios) < 4.5:
        raise SystemExit("contrast %.2f" % min(ratios))
    return img, min(ratios), checks


def verify(img, checks, label):
    worst = 0
    for item in checks:
        score = score_text(img, item[0], item[1], item[2], item[3], item[4])
        if score > 18:
            raise SystemExit("text miss %s %r %.1f" % (label, item[1], score))
        worst = max(worst, score)
    return worst


def near(px, rgb, tol):
    return abs(px[0] - rgb[0]) <= tol and abs(px[1] - rgb[1]) <= tol and abs(px[2] - rgb[2]) <= tol


def probe(path):
    img = Image.open(path).convert("RGB")
    name = os.path.basename(path)
    if name.startswith("22-count") or name.startswith("22-tag") or name.startswith("22-load-4"):
        seats = (img.getpixel((4, 4)), img.getpixel((1914, 4)), img.getpixel((4, 1074)), img.getpixel((1914, 1074)))
        for i, px in enumerate(seats):
            if not near(px, ui.SEAT[i], 12):
                raise SystemExit("%s border %d %s" % (name, i, px))
    if name.startswith("22-count"):
        if not near(img.getpixel((24, 24)), GOLD, 18):
            raise SystemExit("opening frame missing")
        if near(img.getpixel((984, 24)), GOLD, 12):
            raise SystemExit("frame leaked")
    if b"podium" in img.tobytes() or b"PODIUM" in img.tobytes():
        raise SystemExit("podium in %s" % name)


def main():
    assert_script()
    jobs = [
        ("22-count-composite.png", "count", 4),
        ("22-go-composite.png", "go", 4),
        ("22-standings-composite.png", "stand", 4),
        ("22-stand-strip-composite.png", "strip", 4),
        ("22-tag-composite.png", "tag", 4),
        ("22-load-1-composite.png", "load", 1),
        ("22-load-2-composite.png", "load", 2),
        ("22-load-3-composite.png", "load", 3),
        ("22-load-4-composite.png", "load", 4),
    ]
    notes = []
    worst = 99
    for name, kind, humans in jobs:
        img, ratio, checks = scene(kind, humans)
        miss = verify(img, checks, name)
        ui.save(img, name, notes)
        probe(os.path.join(ui.OUT, name))
        worst = min(worst, ratio)
        print("  contrast %.2f text %.1f" % (ratio, miss))
    print("min contrast", round(worst, 2))


if __name__ == "__main__":
    main()
