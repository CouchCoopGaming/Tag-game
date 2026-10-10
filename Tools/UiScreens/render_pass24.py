#!/usr/bin/env python3
"""Pass 24. Bright per-seat Mega Park cameras, verb tips, and the real load states.

Same script as HudState. Unity Editor is not installed, so these are composites.
"""

import os

import render_screens2 as ui
from PIL import Image, ImageDraw, ImageEnhance

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass24")

W, H = ui.W, ui.H
INK = ui.INK
CREAM = ui.CREAM
GOLD = ui.GOLD
SEAT = [ui.BODY["Red"], ui.BODY["Blue"], ui.BODY["Orange"], ui.BODY["Lavender"]]
WELL = (5, 5, 10)
# Unity uvRect, origin bottom-left. Same windows as MenuHost.LoadCam.
# Seat 1 is the chase photograph. The others are different yards of MegaGold.
CAM = (
    (0.00, 0.19, 0.33, 0.44),
    (0.22, 0.12, 0.56, 0.70),
    (0.75, 0.26, 0.25, 0.59),
    (0.42, 0.04, 0.33, 0.44),
)
CHASE = os.path.join(ui.ROOT, "Assets", "Resources", "UI", "Menu", "Chase.png")
# P1 keyboard. P2-P4 pads. Opening rotation. Verbs come from ActionBinds.Name.
TIPS = (
    "Jump [Space].",
    "Sprint [LB], then slide [B].",
    "Cling hold [Left stick] against a wall to climb.",
    "Cling hold [Left stick] + jump [A] to wall jump.",
)
# Live plate strings. Waiting is the dash. Loading is scene progress. Ready fills the bar.
STATES = (
    ("Waiting  0%", 0.0, True),
    ("Loading  60%", 0.60, False),
    ("Ready  100%", 1.0, False),
)

_PARK = None


def park_src():
    global _PARK
    if _PARK is None:
        src = Image.open(ui.PARK).convert("RGB")
        _PARK = ImageEnhance.Brightness(src).enhance(1.08)
    return _PARK


def crop_gold(uv, size):
    uvx, uvy, uvw, uvh = uv
    src = park_src()
    sw, sh = src.size
    left = int(round(uvx * sw))
    right = int(round((uvx + uvw) * sw))
    top = int(round((1.0 - (uvy + uvh)) * sh))
    bottom = int(round((1.0 - uvy) * sh))
    if right <= left + 2:
        right = left + 2
    if bottom <= top + 2:
        bottom = top + 2
    return src.crop((left, top, right, bottom)).resize(size, Image.Resampling.LANCZOS)


def crop_cam(seat, size):
    seat = seat % 4
    if seat != 1:
        return crop_gold(CAM[seat], size)
    base = crop_gold(CAM[1], size).convert("RGBA")
    ch = Image.open(CHASE).convert("RGBA")
    box = ch.split()[-1].getbbox()
    if box is None:
        return base.convert("RGB")
    ch = ch.crop(box)
    tw = max(8, int(size[0] * 0.78))
    th = max(8, int(ch.size[1] * tw / max(1, ch.size[0])))
    if th > int(size[1] * 0.72):
        th = max(8, int(size[1] * 0.72))
        tw = max(8, int(ch.size[0] * th / max(1, ch.size[1])))
    ch = ch.resize((tw, th), Image.Resampling.LANCZOS)
    x = max(0, (size[0] - tw) // 2)
    y = max(0, (size[1] - th) // 2 - int(size[1] * 0.04))
    base.alpha_composite(ch, (x, y))
    return base.convert("RGB")


def text(d, xy, word, face, fill, bg, box, checks, ratios, origin=(0, 0)):
    d.text(xy, word, font=face, fill=fill)
    ok, bb = ui.text_inside(d, xy, word, face, box)
    if not ok:
        raise SystemExit("clip %r %s" % (word, bb))
    ratios.append(ui.contrast(fill, bg))
    checks.append(((xy[0] + origin[0], xy[1] + origin[1]), word, face, fill, bg))


def draw_only(d, xy, word, face, fill, bg, box, checks, ratios):
    d.text(xy, word, font=face, fill=fill)
    ok, bb = ui.text_inside(d, xy, word, face, box)
    if not ok:
        raise SystemExit("clip %r %s" % (word, bb))
    ratios.append(ui.contrast(fill, bg))


def r_wide(face):
    return getattr(face, "size", 16) >= 20


def glyph_line(d, x, y, line, face, fill, bg, box, checks, ratios):
    rest = line
    cx = x
    cap = ui.font(ui.FONT_B, 18 if r_wide(face) else 14)
    while rest:
        cut = rest.find("[")
        if cut < 0:
            paint = text if len(rest.strip()) > 1 else draw_only
            paint(d, (cx, y), rest, face, fill, bg, box, checks, ratios)
            break
        if cut > 0:
            pre = rest[:cut]
            paint = text if len(pre.strip()) > 1 else draw_only
            paint(d, (cx, y), pre, face, fill, bg, box, checks, ratios)
            cx += d.textlength(pre, font=face)
        end = rest.find("]", cut)
        if end < 0:
            break
        word = rest[cut + 1:end]
        tw = d.textlength(word, font=cap)
        w = tw + 14
        h = 26
        ui.rounded(d, (cx, y - 2, cx + w, y + h), 6, ui.NAVY)
        text(d, (cx + 7, y + 1), word, cap, CREAM, ui.NAVY, (cx + 2, y - 4, cx + w - 2, y + h + 2), checks, ratios)
        cx += w
        rest = rest[end + 1:]
    return cx


def pane_box(humans, index):
    if humans <= 1:
        return (0, 0, W, H)
    if humans == 2:
        mid = W // 2
        return (0, 0, mid, H) if index == 0 else (mid, 0, W, H)
    col, row = index % 2, index // 2
    return (col * W // 2, row * H // 2, (col + 1) * W // 2, (row + 1) * H // 2)


def borders(d, box, seat):
    l, t, r, b = box
    c = SEAT[seat]
    d.rectangle((l, t, r - 1, t + 8), fill=c)
    d.rectangle((l, b - 8, r - 1, b - 1), fill=c)
    d.rectangle((l, t, l + 8, b - 1), fill=c)
    d.rectangle((r - 8, t, r - 1, b - 1), fill=c)


def paint_park(img, box, seat):
    l, t, r, b = [int(v) for v in box]
    if r - l < 4 or b - t < 4:
        return
    shot = crop_cam(seat, (r - l, b - t)).convert("RGBA")
    img.alpha_composite(shot, (l, t))


def load_pane(img, box, seat, checks, ratios, caption, fill, dash, arena_only=False):
    paint_park(img, box, seat)
    d = ImageDraw.Draw(img)
    l, t, r, b = box
    if not arena_only:
        borders(d, box, seat)
    ph = b - t
    content = 188 if r - l > 1000 else 156
    third = int(ph * 0.34)
    if content > third - 8:
        content = third - 8
    plate = (l + 18, b - 14 - content, r - 18, b - 14)
    if arena_only:
        face = ui.font(ui.FONT_D, 28)
        plate = (l + 20, b - 78, l + 280, b - 18)
        ui.rounded(d, plate, 10, INK)
        text(d, (plate[0] + 16, plate[1] + 12), "Mega Park", face, GOLD, INK, (plate[0] + 8, plate[1] + 6, plate[2] - 8, plate[3] - 6), checks, ratios)
        return plate
    ui.rounded(d, plate, 14, INK)
    name = ui.font(ui.FONT_D, 28 if r - l > 1000 else 22)
    well = (plate[0] + 16, plate[1] + 12, plate[0] + 48, plate[1] + 44)
    ui.rounded(d, well, 6, WELL)
    d.rectangle((well[0] + 6, well[1] + 6, well[2] - 6, well[3] - 6), fill=SEAT[seat])
    text(d, (plate[0] + 58, plate[1] + 10), "Mega Park", name, GOLD, INK, (plate[0] + 54, plate[1] + 6, plate[2] - 90, plate[1] + 46), checks, ratios)
    who = ui.font(ui.FONT_B, 20)
    text(d, (plate[2] - 64, plate[1] + 12), "P%d" % (seat + 1), who, CREAM, INK, (plate[2] - 72, plate[1] + 8, plate[2] - 12, plate[1] + 44), checks, ratios)
    tip = TIPS[seat % 4]
    face = ui.font(ui.FONT_B, 22 if r - l > 1000 else 16)
    tip_box = (plate[0] + 14, plate[1] + 52, plate[2] - 14, plate[1] + 96)
    ui.rounded(d, tip_box, 8, GOLD)
    glyph_line(d, tip_box[0] + 12, tip_box[1] + 10, tip, face, INK, GOLD, (tip_box[0] + 6, tip_box[1] + 4, tip_box[2] - 6, tip_box[3] - 4), checks, ratios)
    track = (plate[0] + 16, plate[3] - 52, plate[2] - 16, plate[3] - 30)
    ui.rounded(d, track, 6, (8, 14, 28))
    span = track[2] - track[0]
    if dash:
        x0 = track[0] + span * 0.36
        d.rounded_rectangle((x0, track[1], x0 + span * 0.28, track[3]), 6, fill=GOLD)
    elif fill > 0:
        d.rounded_rectangle((track[0], track[1], track[0] + span * fill, track[3]), 6, fill=GOLD)
    cap = ui.font(ui.FONT_B, 18 if r - l > 1000 else 15)
    text(d, (plate[0] + 16, plate[3] - 26), caption, cap, CREAM, INK, (plate[0] + 10, plate[3] - 28, plate[2] - 10, plate[3] - 4), checks, ratios)
    return plate


def scene_load(humans):
    img = Image.new("RGBA", (W, H), (12, 18, 28, 255))
    checks = []
    ratios = [ui.contrast(GOLD, INK), ui.contrast(CREAM, INK), ui.contrast(INK, GOLD)]
    panes = 4 if humans >= 3 else humans
    plates = []
    for i in range(panes):
        box = pane_box(humans, i)
        if humans == 3 and i == 3:
            paint_park(img, box, 3)
            plates.append(load_pane(img, box, 3, checks, ratios, "", 0, False, True))
        else:
            plates.append(load_pane(img, box, i, checks, ratios, "Waiting  0%", 0.0, True, False))
    return img, checks, ratios, plates


def scene_strip():
    img = Image.new("RGBA", (W, H), (12, 18, 28, 255))
    checks = []
    ratios = [ui.contrast(GOLD, INK), ui.contrast(CREAM, INK), ui.contrast(INK, GOLD)]
    plates = []
    for i, (caption, fill, dash) in enumerate(STATES):
        box = (i * W // 3, 0, (i + 1) * W // 3, H)
        plates.append(load_pane(img, box, 0, checks, ratios, caption, fill, dash, False))
    return img, checks, ratios, plates


def score_text(img, xy, word, face, fill, bg):
    x, y = xy
    probe = Image.new("RGB", (img.size[0], img.size[1]), bg)
    pd = ImageDraw.Draw(probe)
    pd.text((x, y), word, font=face, fill=fill)
    crop = pd.textbbox((x, y), word, font=face)
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


def mean_patch(img, x, y, n=24):
    x = max(0, min(img.size[0] - n - 1, int(x)))
    y = max(0, min(img.size[1] - n - 1, int(y)))
    crop = img.crop((x, y, x + n, y + n)).convert("RGB")
    px = list(crop.getdata())
    k = len(px)
    return tuple(sum(p[i] for p in px) // k for i in range(3))


def lum(c):
    return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]


def probe(path, humans, plates):
    img = Image.open(path).convert("RGB")
    name = os.path.basename(path)
    if humans == 4 and name.startswith("24-load-4"):
        seats = (img.getpixel((4, 4)), img.getpixel((1914, 4)), img.getpixel((4, 1074)), img.getpixel((1914, 1074)))
        for i, px in enumerate(seats):
            if not near(px, SEAT[i], 18):
                raise SystemExit("%s border %d %s" % (name, i, px))
    skies = []
    for i, plate in enumerate(plates):
        if plate is None:
            continue
        box = pane_box(humans, i) if humans else (i * W // 3, 0, (i + 1) * W // 3, H)
        cx = (box[0] + box[2]) / 2
        cy = (box[1] + plate[1]) / 2
        if cy >= plate[1] - 30:
            cy = box[1] + 40
        patch = mean_patch(img, cx - 12, cy)
        skies.append(patch)
        if lum(patch) < 90:
            raise SystemExit("%s park dim %s lum %.1f" % (name, patch, lum(patch)))
        card = mean_patch(img, plate[0] + 30, plate[1] + 8)
        if lum(card) > lum(patch) - 30:
            raise SystemExit("%s card not darker %s vs %s" % (name, card, patch))
    if humans == 4 and len(skies) >= 4:
        for a in range(4):
            for b in range(a + 1, 4):
                far = max(abs(skies[a][c] - skies[b][c]) for c in range(3))
                if far < 8:
                    raise SystemExit("%s cameras %d %d match %s" % (name, a, b, skies))
    raw = img.tobytes()
    if b"podium" in raw or b"PODIUM" in raw or b"Starting" in raw:
        raise SystemExit("bad word in %s" % name)


def save(img, name, notes):
    old = ui.SEAT
    ui.SEAT = list(old) + list(SEAT) + [WELL, INK, GOLD, CREAM]
    try:
        ui.save(img, name, notes)
    finally:
        ui.SEAT = old


def main():
    notes = []
    worst = 99
    jobs = [
        ("24-load-1-composite.png", 1, scene_load),
        ("24-load-2-composite.png", 2, scene_load),
        ("24-load-3-composite.png", 3, scene_load),
        ("24-load-4-composite.png", 4, scene_load),
    ]
    for name, humans, build in jobs:
        img, checks, ratios, plates = build(humans)
        if min(ratios) < 4.5:
            raise SystemExit("contrast %.2f" % min(ratios))
        miss = verify(img, checks, name)
        save(img, name, notes)
        probe(os.path.join(ui.OUT, name), humans, plates)
        worst = min(worst, min(ratios))
        print("  contrast %.2f text %.1f" % (min(ratios), miss))
    img, checks, ratios, plates = scene_strip()
    if min(ratios) < 4.5:
        raise SystemExit("contrast %.2f" % min(ratios))
    miss = verify(img, checks, "24-load-strip-composite.png")
    save(img, "24-load-strip-composite.png", notes)
    probe(os.path.join(ui.OUT, "24-load-strip-composite.png"), 0, plates)
    worst = min(worst, min(ratios))
    print("  contrast %.2f text %.1f" % (min(ratios), miss))
    print("min contrast", round(worst, 2))
    for name, size, _ in notes:
        if size > 400000:
            raise SystemExit("size %s %d" % (name, size))


if __name__ == "__main__":
    main()
