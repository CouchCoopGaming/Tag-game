#!/usr/bin/env python3
"""Pass 25. Value-stepped costume hues, seat shapes, mid exposure, empty waiting track.

Same script as HudState. Unity Editor is not installed, so these are composites.
"""

import os

import render_screens2 as ui
from PIL import Image, ImageDraw, ImageEnhance, ImageStat

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass25")

INK = ui.INK
CREAM = ui.CREAM
GOLD = ui.GOLD
NAVY = ui.NAVY
# MenuMannequin.Swatch, pass 25. Darker red and blue, lighter orange and lavender.
SEAT = [(230, 46, 51), (51, 122, 224), (255, 158, 46), (230, 209, 255)]
WELL = (5, 5, 10)
TRACK = (8, 14, 28)
# Gold at 0.38 over the track. A short sliver, not a fill.
SHIMMER = (102, 90, 29)
# Unity uvRect, origin bottom-left. Mid band of MegaGold. Not the empty floor.
CAM = (
    (0.00, 0.30, 0.30, 0.28),
    (0.28, 0.30, 0.30, 0.28),
    (0.68, 0.36, 0.26, 0.28),
    (0.48, 0.32, 0.36, 0.30),
)
CHASE = os.path.join(ui.ROOT, "Assets", "Resources", "UI", "Menu", "Chase.png")
# P1 keyboard. P2-P4 pads. Cling uses Show(), because Chip() collides with Move.
TIPS = (
    "Jump [Space].",
    "Sprint [LB], then slide [B].",
    "Cling hold [Left stick hold] against a wall to climb.",
    "Cling hold [Left stick hold] + jump [A] to wall jump.",
)
STATES = (
    ("Waiting  0%", 0.0, True),
    ("Loading  60%", 0.60, False),
    ("Ready  100%", 1.0, False),
)

_PARK = None
W, H = 1920, 1080


def park_src():
    global _PARK
    if _PARK is None:
        src = Image.open(ui.PARK).convert("RGB")
        graded = ImageEnhance.Contrast(src).enhance(1.35)
        _PARK = ImageEnhance.Brightness(graded).enhance(0.75)
    return _PARK


def crop_uv(src, uv, size):
    uvx, uvy, uvw, uvh = uv
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
    base = crop_uv(park_src(), CAM[seat], size).convert("RGBA")
    if seat != 1:
        return base.convert("RGB")
    ch = Image.open(CHASE).convert("RGBA")
    # Bodies sit in the lower part of the chase plate. Keep that plate above the card.
    tw = max(8, int(size[0] * 0.84))
    th = max(8, int(size[1] * 0.48))
    runners = crop_uv(ch, (0.19, 0.22, 0.60, 0.66), (tw, th))
    x = max(0, (size[0] - tw) // 2)
    y = max(0, int(size[1] * 0.02))
    base.alpha_composite(runners, (x, y))
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


def cap_font(face):
    size = 18 if getattr(face, "size", 16) >= 20 else 14
    return ui.font(ui.FONT_B, size)


def line_width(d, line, face):
    cap = cap_font(face)
    width = 0
    rest = line
    while rest:
        cut = rest.find("[")
        if cut < 0:
            width += d.textlength(rest, font=face)
            break
        if cut > 0:
            width += d.textlength(rest[:cut], font=face)
        end = rest.find("]", cut)
        if end < 0:
            break
        word = rest[cut + 1:end]
        width += d.textlength(word, font=cap) + 14
        rest = rest[end + 1:]
    return width


def glyph_line(d, x, y, line, face, fill, bg, box, checks, ratios):
    rest = line
    cx = x
    cap = cap_font(face)
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
        ui.rounded(d, (cx, y - 2, cx + w, y + h), 6, NAVY)
        text(d, (cx + 7, y + 1), word, cap, CREAM, NAVY, (cx + 2, y - 4, cx + w - 2, y + h + 2), checks, ratios)
        cx += w
        rest = rest[end + 1:]
    return cx


def fit_face(d, line, max_w, start):
    size = start
    while size >= 13:
        face = ui.font(ui.FONT_B, size)
        if line_width(d, line, face) <= max_w:
            return face
        size -= 1
    return ui.font(ui.FONT_B, 13)


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


def stamp(d, box, seat):
    x0, y0, x1, y1 = box
    c = SEAT[seat % 4]
    kind = seat % 4
    if kind == 0:
        d.ellipse((x0, y0, x1 - 1, y1 - 1), fill=c)
    elif kind == 1:
        d.polygon((((x0 + x1) / 2, y0), (x0, y1 - 1), (x1 - 1, y1 - 1)), fill=c)
    elif kind == 2:
        d.rectangle((x0, y0, x1 - 1, y1 - 1), fill=c)
    else:
        mx = (x0 + x1) / 2
        my = (y0 + y1) / 2
        d.polygon(((mx, y0), (x1 - 1, my), (mx, y1 - 1), (x0, my)), fill=c)


def chip_size(pane_w):
    if pane_w < 800:
        return 64, 48
    if pane_w < 1400:
        return 56, 42
    return 72, 56


def load_pane(img, box, seat, checks, ratios, caption, fill, dash, arena_only=False):
    paint_park(img, box, seat)
    d = ImageDraw.Draw(img)
    l, t, r, b = box
    if not arena_only:
        borders(d, box, seat)
    pw = r - l
    ph = b - t
    well_s, shape_s = chip_size(pw)
    content = well_s + 118
    third = int(ph * 0.46)
    if content > third - 8:
        content = max(well_s + 96, third - 8)
    plate = (l + 18, b - 14 - content, r - 18, b - 14)
    shape_box = None
    if arena_only:
        face = ui.font(ui.FONT_D, 28)
        plate = (l + 20, b - 78, l + 280, b - 18)
        ui.rounded(d, plate, 10, INK)
        text(d, (plate[0] + 16, plate[1] + 12), "Mega Park", face, GOLD, INK, (plate[0] + 8, plate[1] + 6, plate[2] - 8, plate[3] - 6), checks, ratios)
        return plate, None, None
    ui.rounded(d, plate, 14, INK)
    name = ui.font(ui.FONT_D, 28 if pw > 1000 else 22)
    inset = (well_s - shape_s) // 2
    well = (plate[0] + 14, plate[1] + 12, plate[0] + 14 + well_s, plate[1] + 12 + well_s)
    ui.rounded(d, well, 8, WELL)
    shape_box = (well[0] + inset, well[1] + inset, well[2] - inset, well[3] - inset)
    stamp(d, shape_box, seat)
    ratios.append(ui.contrast(SEAT[seat % 4], WELL))
    name_x = well[2] + 12
    text(d, (name_x, plate[1] + 14), "Mega Park", name, GOLD, INK, (name_x - 2, plate[1] + 8, plate[2] - 72, plate[1] + 14 + well_s), checks, ratios)
    who = ui.font(ui.FONT_B, 20)
    text(d, (plate[2] - 64, plate[1] + 16), "P%d" % (seat + 1), who, CREAM, INK, (plate[2] - 72, plate[1] + 10, plate[2] - 12, plate[1] + 48), checks, ratios)
    tip = TIPS[seat % 4]
    tip_top = well[3] + 8
    tip_box = (plate[0] + 14, tip_top, plate[2] - 14, tip_top + 40)
    ui.rounded(d, tip_box, 8, GOLD)
    face = fit_face(d, tip, tip_box[2] - tip_box[0] - 24, 22 if pw > 1000 else 16)
    glyph_line(d, tip_box[0] + 12, tip_box[1] + 8, tip, face, INK, GOLD, (tip_box[0] + 6, tip_box[1] + 2, tip_box[2] - 6, tip_box[3] - 2), checks, ratios)
    track = (plate[0] + 16, plate[3] - 52, plate[2] - 16, plate[3] - 22)
    ui.rounded(d, track, 6, TRACK)
    span = track[2] - track[0]
    th = track[3] - track[1]
    if dash:
        # Indeterminate sliver: 10% wide, vertically inside the track, dim.
        x0 = track[0] + int(span * 0.28)
        sliver = max(4, int(th * 0.28))
        y0 = track[1] + (th - sliver) // 2
        d.rounded_rectangle((x0, y0, x0 + int(span * 0.10), y0 + sliver), 3, fill=SHIMMER)
    elif fill > 0:
        d.rounded_rectangle((track[0], track[1], track[0] + span * fill, track[3]), 6, fill=GOLD)
    cap = ui.font(ui.FONT_B, 18 if pw > 1000 else 15)
    text(d, (plate[0] + 16, plate[3] - 20), caption, cap, CREAM, INK, (plate[0] + 10, plate[3] - 22, plate[2] - 10, plate[3] - 2), checks, ratios)
    return plate, shape_box, track


def scene_load(humans):
    img = Image.new("RGBA", (W, H), (12, 18, 28, 255))
    checks = []
    ratios = [ui.contrast(GOLD, INK), ui.contrast(CREAM, INK), ui.contrast(INK, GOLD)]
    panes = 4 if humans >= 3 else humans
    plates = []
    shapes = []
    tracks = []
    for i in range(panes):
        box = pane_box(humans, i)
        if humans == 3 and i == 3:
            paint_park(img, box, 3)
            plate, shape, track = load_pane(img, box, 3, checks, ratios, "", 0, False, True)
        else:
            plate, shape, track = load_pane(img, box, i, checks, ratios, "Waiting  0%", 0.0, True, False)
        plates.append(plate)
        shapes.append(shape)
        tracks.append(track)
    return img, checks, ratios, plates, shapes, tracks


def scene_strip():
    img = Image.new("RGBA", (W, H), (12, 18, 28, 255))
    checks = []
    ratios = [ui.contrast(GOLD, INK), ui.contrast(CREAM, INK), ui.contrast(INK, GOLD)]
    plates = []
    shapes = []
    tracks = []
    for i, (caption, fill, dash) in enumerate(STATES):
        box = (i * W // 3, 0, (i + 1) * W // 3, H)
        plate, shape, track = load_pane(img, box, 0, checks, ratios, caption, fill, dash, False)
        plates.append(plate)
        shapes.append(shape)
        tracks.append(track)
    return img, checks, ratios, plates, shapes, tracks


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


def region_lum(img, box):
    crop = img.crop(box).convert("RGB")
    st = ImageStat.Stat(crop)
    return 0.2126 * st.mean[0] + 0.7152 * st.mean[1] + 0.0722 * st.mean[2]


def shape_reads(img, box, seat):
    if box is None:
        return
    x0, y0, x1, y1 = [int(v) for v in box]
    if x1 - x0 < 36 or y1 - y0 < 36:
        raise SystemExit("chip small %s %s" % (seat, box))
    c = SEAT[seat % 4]
    px = img.load()

    def is_seat(x, y):
        return near(px[x, y], c, 18)

    cx = (x0 + x1) // 2
    cy = (y0 + y1) // 2
    if not is_seat(cx, cy):
        raise SystemExit("chip center empty %s" % seat)
    corner = is_seat(x0 + 1, y0 + 1)
    top = is_seat(cx, y0 + 2)
    left = is_seat(x0 + 2, cy)
    if seat % 4 == 0 and corner:
        raise SystemExit("circle has corners")
    if seat % 4 == 1 and (corner or not top):
        raise SystemExit("triangle unreadable")
    if seat % 4 == 2 and not corner:
        raise SystemExit("square unreadable")
    if seat % 4 == 3 and (corner or not top or not left):
        raise SystemExit("diamond unreadable")


def probe(path, humans, plates, shapes, tracks, waiting):
    img = Image.open(path).convert("RGB")
    name = os.path.basename(path)
    iw, ih = img.size
    if humans == 4:
        seats = (
            img.getpixel((4, 4)),
            img.getpixel((iw - 6, 4)),
            img.getpixel((4, ih - 6)),
            img.getpixel((iw - 6, ih - 6)),
        )
        for i, px in enumerate(seats):
            if not near(px, SEAT[i], 18):
                raise SystemExit("%s border %d %s" % (name, i, px))
        for i, shape in enumerate(shapes):
            if shape is not None:
                shape_reads(img, shape, i)
    skies = []
    for i, plate in enumerate(plates):
        if plate is None:
            continue
        if humans:
            box = pane_box(humans, i)
        else:
            box = (i * iw // 3, 0, (i + 1) * iw // 3, ih)
        park_h = int((box[3] - box[1]) * 0.42)
        park = (box[0] + 16, box[1] + 16, box[2] - 16, box[1] + park_h)
        if park[3] <= park[1] + 8:
            raise SystemExit("%s no park %s" % (name, park))
        patch_l = region_lum(img, park)
        skies.append(patch_l)
        if patch_l < 115 or patch_l > 145:
            raise SystemExit("%s park lum %.1f" % (name, patch_l))
        st = ImageStat.Stat(img.crop(park).convert("L"))
        if st.stddev[0] < 12:
            raise SystemExit("%s park flat %.1f" % (name, st.stddev[0]))
        if tracks[i] is not None and waiting and (humans or i == 0):
            tr = tracks[i]
            bar = img.crop(tr)
            gold = shimmer = 0
            for px in bar.getdata():
                if near(px, GOLD, 28):
                    gold += 1
                if near(px, SHIMMER, 24):
                    shimmer += 1
            if gold > bar.size[0] * 2:
                raise SystemExit("%s waiting fill %d" % (name, gold))
            if shimmer < 12:
                raise SystemExit("%s shimmer missing %d" % (name, shimmer))
    if humans == 4 and len(skies) >= 4:
        # Chase on P2 must move that pane off the gold crop beside it.
        if abs(skies[0] - skies[1]) < 4 and abs(skies[1] - skies[3]) < 4:
            raise SystemExit("%s cameras flat %s" % (name, skies))
    raw = img.tobytes()
    if b"podium" in raw or b"PODIUM" in raw or b"Starting" in raw or b"COMPOSITE" in raw:
        raise SystemExit("bad word in %s" % name)
    return skies


def save(img, name, notes):
    old = ui.SEAT
    ui.SEAT = list(old) + list(SEAT) + [WELL, INK, GOLD, CREAM, NAVY, TRACK, SHIMMER]
    try:
        ui.save(img, name, notes)
    finally:
        ui.SEAT = old


def grade_upper():
    src = park_src()
    w, h = src.size
    upper = region_lum(src, (0, 0, w, h // 2))
    print("source upper lum %.1f" % upper)
    if upper < 120 or upper > 140:
        raise SystemExit("upper lum %.1f" % upper)
    for i, uv in enumerate(CAM):
        uvx, uvy, uvw, uvh = uv
        if (1.0 - uvy) > 0.72:
            raise SystemExit("floor crop %d" % i)
        sw, sh = src.size
        left = int(uvx * sw)
        right = int((uvx + uvw) * sw)
        top = int((1.0 - (uvy + uvh)) * sh)
        bottom = int((1.0 - uvy) * sh)
        crop = src.crop((left, top, right, bottom))
        st = ImageStat.Stat(crop.convert("L"))
        if st.stddev[0] < 12:
            raise SystemExit("flat crop %d %.1f" % (i, st.stddev[0]))


def main():
    global W, H
    grade_upper()
    notes = []
    worst = 99
    jobs = [
        ("25-load-1-composite.png", 1, (1920, 1080), scene_load),
        ("25-load-2-composite.png", 2, (1920, 1080), scene_load),
        ("25-load-3-composite.png", 3, (1920, 1080), scene_load),
        ("25-load-4-composite.png", 4, (1920, 1080), scene_load),
        ("25-load-4-720-composite.png", 4, (1280, 720), scene_load),
    ]
    for name, humans, size, build in jobs:
        W, H = size
        img, checks, ratios, plates, shapes, tracks = build(humans)
        if min(ratios) < 4.5:
            raise SystemExit("contrast %.2f" % min(ratios))
        miss = verify(img, checks, name)
        save(img, name, notes)
        skies = probe(os.path.join(ui.OUT, name), humans, plates, shapes, tracks, True)
        worst = min(worst, min(ratios))
        print("  contrast %.2f text %.1f park %s" % (min(ratios), miss, " ".join("%.0f" % s for s in skies)))
    W, H = 1920, 1080
    img, checks, ratios, plates, shapes, tracks = scene_strip()
    if min(ratios) < 4.5:
        raise SystemExit("contrast %.2f" % min(ratios))
    miss = verify(img, checks, "25-load-strip-composite.png")
    save(img, "25-load-strip-composite.png", notes)
    probe(os.path.join(ui.OUT, "25-load-strip-composite.png"), 0, plates, shapes, tracks, True)
    # Loading and Ready must show a real gold fill, not a sliver.
    strip = Image.open(os.path.join(ui.OUT, "25-load-strip-composite.png")).convert("RGB")
    for i, expect in ((1, 0.45), (2, 0.85)):
        tr = tracks[i]
        bar = strip.crop(tr)
        gold = sum(1 for px in bar.getdata() if near(px, GOLD, 28))
        if gold < bar.size[0] * bar.size[1] * expect:
            raise SystemExit("strip fill %d %d" % (i, gold))
    worst = min(worst, min(ratios))
    print("  contrast %.2f text %.1f" % (min(ratios), miss))
    print("min contrast", round(worst, 2))
    for name, size, _ in notes:
        if size > 400000:
            raise SystemExit("size %s %d" % (name, size))


if __name__ == "__main__":
    main()
