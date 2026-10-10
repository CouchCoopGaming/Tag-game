#!/usr/bin/env python3
"""Pass 27. A short load card, a sunny neutral park, and a lavender diamond.

Same script as HudState. Unity Editor is not installed, so these are composites.
Shapes follow MenuMannequin: P1 circle, P2 triangle, P3 square, P4 diamond.
The diamond fill is the base lavender. The light step is the pane band.
"""

import math
import os

import render_screens2 as ui
from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageStat

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass27")

INK = ui.INK
CREAM = ui.CREAM
GOLD = ui.GOLD
NAVY = ui.NAVY
# Shape fills. Lavender is the base swatch (0.70, 0.58, 0.88), not the light step.
FILL = [(230, 46, 51), (51, 122, 224), (255, 158, 46), (179, 148, 224)]
# Pane bands. Lavender's light step lives here.
BAND = [(230, 46, 51), (51, 122, 224), (255, 158, 46), (209, 179, 250)]
WELL = (5, 5, 10)
TRACK = (8, 14, 28)
# Gold at 0.38 over the track. A short sliver, not a fill.
SHIMMER = (102, 90, 29)
# Unity uv, origin bottom-left. Landmark band, so the upper pane is the dock,
# the court, or the gazebo. The card covers only the bottom third.
CAM = (
    (0.00, 0.22, 0.50, 0.40),
    (0.16, 0.22, 0.48, 0.44),
    (0.52, 0.22, 0.48, 0.40),
    (0.26, 0.22, 0.50, 0.44),
)
# Chase plate with room above the heads. Unity uv on Chase.png.
CHASE_UV = (0.16, 0.19, 0.67, 0.74)
SIZE_LINE = "160 x 100 m"
CHASE = os.path.join(ui.ROOT, "Assets", "Resources", "UI", "Menu", "Chase.png")
# Verb once, input once. Pad cling is the stick. Keyboard cling is WASD.
TIPS = (
    "Jump [Space].",
    "Sprint [LB], then slide [B].",
    "Hold [Left stick] into a wall to climb.",
    "[Left stick] into a wall + [A] to wall jump.",
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
        _PARK = neutralize(src)
    return _PARK


def _curve(v):
    x = v / 255.0
    y = x * x * (3.0 - 2.0 * x)
    y = 0.42 * x + 0.58 * y
    y = y + 0.10 * math.sin((y - 0.5) * math.pi)
    if y < 0.0:
        y = 0.0
    if y > 1.0:
        y = 1.0
    return int(round(y * 255.0))


def neutralize(src):
    """Neutral white balance, then an S-curve and saturation so the park stays sunny."""
    sample = src.resize((240, 135))
    st = ImageStat.Stat(sample)
    target = sum(st.mean) / 3.0
    scales = []
    for mean in st.mean:
        aim = target / max(1.0, mean)
        scales.append(1.0 + (aim - 1.0) * 0.92)
    balanced = Image.merge(
        "RGB",
        [ch.point(lambda v, s=s: max(0, min(255, int(round(v * s))))) for ch, s in zip(src.split(), scales)],
    )
    graded = ImageEnhance.Contrast(balanced).enhance(1.18)
    graded = ImageEnhance.Color(graded).enhance(1.48)
    graded = Image.merge("RGB", [c.point(_curve) for c in graded.split()])
    px = graded.load()
    w, h = graded.size
    sky = int(h * 0.28)
    for y in range(sky):
        for x in range(w):
            r, g, b = px[x, y]
            if max(r, g, b) - min(r, g, b) < 36 and (r + g + b) > 420:
                px[x, y] = (max(0, int(r * 0.86)), max(0, int(g * 0.94)), min(255, int(b * 1.08 + 16)))
    # The curve lifts the mids. Ease the exposure so the park is sunny, not blown out.
    return ImageEnhance.Brightness(graded).enhance(0.82)


def crop_uv(src, uv, size):
    uvx, uvy, uvw, uvh = uv
    sw, sh = src.size
    need_w = size[0] / float(sw)
    need_h = size[1] / float(sh)
    if uvw < need_w:
        extra = need_w - uvw
        uvx = max(0.0, uvx - extra * 0.5)
        uvw = min(1.0 - uvx, need_w)
    if uvh < need_h:
        extra = need_h - uvh
        # Grow toward the floor. The landmark stays in the upper pane, and the card covers the extra.
        uvy = max(0.0, uvy - extra)
        uvh = min(1.0 - uvy, uvh + extra)
    left = int(round(uvx * sw))
    right = int(round((uvx + uvw) * sw))
    top = int(round((1.0 - (uvy + uvh)) * sh))
    bottom = int(round((1.0 - uvy) * sh))
    if right <= left + 2:
        right = left + 2
    if bottom <= top + 2:
        bottom = top + 2
    shot = src.crop((left, top, right, bottom))
    upscale = shot.size[0] < size[0] or shot.size[1] < size[1]
    shot = shot.resize(size, Image.Resampling.LANCZOS)
    if upscale:
        shot = shot.filter(ImageFilter.UnsharpMask(radius=1.3, percent=90, threshold=2))
    return shot.filter(ImageFilter.UnsharpMask(radius=0.7, percent=50, threshold=3))


def crop_cam(seat, size):
    seat = seat % 4
    base = crop_uv(park_src(), CAM[seat], size).convert("RGBA")
    if seat != 1:
        return base.convert("RGB")
    ch = Image.open(CHASE).convert("RGBA")
    tw = max(8, int(size[0] * 0.84))
    th = max(8, int(size[1] * 0.52))
    runners = crop_uv(ch, CHASE_UV, (tw, th))
    x = max(0, (size[0] - tw) // 2)
    y = max(0, int(size[1] * 0.08))
    base.alpha_composite(runners, (x, y))
    return base.convert("RGB")


TEXTS = []
ROWS = []


def reset_layout():
    TEXTS.clear()
    ROWS.clear()


def text(d, xy, word, face, fill, bg, box, checks, ratios, origin=(0, 0), home=None):
    d.text(xy, word, font=face, fill=fill)
    ok, bb = ui.text_inside(d, xy, word, face, box)
    if not ok:
        raise SystemExit("clip %r %s" % (word, bb))
    ratios.append(ui.contrast(fill, bg))
    checks.append(((xy[0] + origin[0], xy[1] + origin[1]), word, face, fill, bg))
    if home is not None:
        TEXTS.append((bb, home, word))


def draw_only(d, xy, word, face, fill, bg, box, checks, ratios, home=None):
    d.text(xy, word, font=face, fill=fill)
    ok, bb = ui.text_inside(d, xy, word, face, box)
    if not ok:
        raise SystemExit("clip %r %s" % (word, bb))
    ratios.append(ui.contrast(fill, bg))
    if home is not None:
        TEXTS.append((bb, home, word))


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


def glyph_line(d, x, y, line, face, fill, bg, box, checks, ratios, home, key_h=22):
    rest = line
    cx = x
    cap = ui.font(ui.FONT_B, max(11, key_h - 6))
    while rest:
        cut = rest.find("[")
        if cut < 0:
            paint = text if len(rest.strip()) > 1 else draw_only
            paint(d, (cx, y), rest, face, fill, bg, box, checks, ratios, home=home)
            break
        if cut > 0:
            pre = rest[:cut]
            paint = text if len(pre.strip()) > 1 else draw_only
            paint(d, (cx, y), pre, face, fill, bg, box, checks, ratios, home=home)
            cx += d.textlength(pre, font=face)
        end = rest.find("]", cut)
        if end < 0:
            break
        word = rest[cut + 1:end]
        tw = d.textlength(word, font=cap)
        w = tw + 14
        h = key_h
        key = (cx, y, cx + w, y + h)
        ui.rounded(d, key, 6, NAVY)
        text(d, (cx + 7, y + 1), word, cap, CREAM, NAVY, (cx + 2, y, cx + w - 2, y + h), checks, ratios, home=key)
        cx += w + 4
        rest = rest[end + 1:]
    return cx


def overlap(a, b):
    x0 = max(a[0], b[0])
    y0 = max(a[1], b[1])
    x1 = min(a[2], b[2])
    y1 = min(a[3], b[3])
    if x1 - x0 < 1 or y1 - y0 < 1:
        return 0
    return (x1 - x0) * (y1 - y0)


def inside(bb, home, slop=2):
    return bb[0] >= home[0] - slop and bb[1] >= home[1] - slop and bb[2] <= home[2] + slop and bb[3] <= home[3] + slop


def layout_report(label):
    hits = []
    for i in range(len(ROWS)):
        for j in range(i + 1, len(ROWS)):
            if overlap(ROWS[i], ROWS[j]) > 0:
                hits.append("rows")
    for bb, home, word in TEXTS:
        if not inside(bb, home):
            hits.append("outside %r" % word)
    for i in range(len(TEXTS)):
        for j in range(i + 1, len(TEXTS)):
            if overlap(TEXTS[i][0], TEXTS[j][0]) > 2:
                hits.append("%r x %r" % (TEXTS[i][2], TEXTS[j][2]))
    if hits:
        raise SystemExit("layout fail %s %s" % (label, hits[:8]))
    print("layout=ok texts=%d rows=%d hits=0" % (len(TEXTS), len(ROWS)))


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
    c = BAND[seat]
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
    c = FILL[seat % 4]
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


def chip_size(pane_w, pane_h):
    cap = int(pane_h * 0.32)
    well = 42 if pane_w < 1400 else 48
    while well > 34 and (8 + well + 4 + 24 + 4 + 12 + 4 + 16 + 6) > cap:
        well -= 2
    shape = well - 6
    if shape < 30:
        shape = 30
    return well, shape


def load_pane(img, box, seat, checks, ratios, caption, fill, dash, arena_only=False):
    paint_park(img, box, seat)
    d = ImageDraw.Draw(img)
    l, t, r, b = box
    if not arena_only:
        borders(d, box, seat)
    pw = r - l
    ph = b - t
    well_s, shape_s = chip_size(pw, ph)
    tip_h = 26 if ph < 500 else 32
    track_h = 12
    cap_h = 16
    gap = 4
    pad = 6
    content = pad + well_s + gap + tip_h + gap + track_h + gap + cap_h + pad
    limit = int(ph * 0.32)
    if content > limit:
        content = limit
    plate = (l + 10, b - 8 - content, r - 10, b - 8)
    if (plate[3] - plate[1]) > limit:
        plate = (plate[0], plate[3] - limit, plate[2], plate[3])
    shape_box = None
    if arena_only:
        face = ui.font(ui.FONT_D, 22)
        plate = (l + 16, b - 64, l + 250, b - 14)
        ui.rounded(d, plate, 10, INK)
        text(d, (plate[0] + 12, plate[1] + 10), "Mega Park", face, GOLD, INK, (plate[0] + 8, plate[1] + 4, plate[2] - 8, plate[3] - 4), checks, ratios, home=plate)
        ROWS.append(plate)
        return plate, None, None
    ui.rounded(d, plate, 12, INK)
    y = plate[1] + pad
    header = (plate[0] + 8, y, plate[2] - 8, y + well_s)
    y = header[3] + gap
    tip_box = (plate[0] + 8, y, plate[2] - 8, y + tip_h)
    y = tip_box[3] + gap
    track = (plate[0] + 10, y, plate[2] - 10, y + track_h)
    y = track[3] + gap
    cap_row = (plate[0] + 8, y, plate[2] - 8, min(plate[3] - 4, y + cap_h))
    for row in (header, tip_box, track, cap_row):
        ROWS.append(row)
    name_px = 18 if pw > 1000 else 15
    sub_px = 13 if pw > 1000 else 12
    name = ui.font(ui.FONT_D, name_px)
    sub = ui.font(ui.FONT_B, sub_px)
    inset = (well_s - shape_s) // 2
    well = (header[0], header[1], header[0] + well_s, header[1] + well_s)
    ui.rounded(d, well, 6, WELL)
    shape_box = (well[0] + inset, well[1] + inset, well[2] - inset, well[3] - inset)
    stamp(d, shape_box, seat)
    ratios.append(ui.contrast(FILL[seat % 4], WELL))
    name_x = well[2] + 8
    title_home = (name_x, header[1], plate[2] - 52, header[1] + name_px + 4)
    text(d, (name_x, header[1]), "Mega Park", name, GOLD, INK, title_home, checks, ratios, home=title_home)
    size_home = (name_x, header[1] + name_px + 2, plate[2] - 52, header[3])
    text(d, (name_x, header[1] + name_px + 2), SIZE_LINE, sub, CREAM, INK, size_home, checks, ratios, home=size_home)
    who = ui.font(ui.FONT_B, 14)
    who_home = (plate[2] - 44, header[1] + 4, plate[2] - 4, header[1] + 24)
    text(d, (who_home[0], who_home[1]), "P%d" % (seat + 1), who, CREAM, INK, who_home, checks, ratios, home=who_home)
    tip = TIPS[seat % 4]
    ui.rounded(d, tip_box, 6, GOLD)
    face = fit_face(d, tip, tip_box[2] - tip_box[0] - 20, 15 if pw > 1000 else 13)
    key_h = max(16, tip_h - 8)
    glyph_line(d, tip_box[0] + 8, tip_box[1] + (tip_h - key_h) // 2, tip, face, INK, GOLD, tip_box, checks, ratios, tip_box, key_h)
    ui.rounded(d, track, 4, TRACK)
    span = track[2] - track[0]
    th = track[3] - track[1]
    if dash:
        x0 = track[0] + int(span * 0.28)
        sliver = max(3, int(th * 0.34))
        y0 = track[1] + (th - sliver) // 2
        d.rounded_rectangle((x0, y0, x0 + max(8, int(span * 0.10)), y0 + sliver), 2, fill=SHIMMER)
    elif fill > 0:
        d.rounded_rectangle((track[0], track[1], track[0] + span * fill, track[3]), 4, fill=GOLD)
    cap = ui.font(ui.FONT_B, 13 if pw > 1000 else 12)
    text(d, (cap_row[0] + 4, cap_row[1]), caption, cap, CREAM, INK, cap_row, checks, ratios, home=cap_row)
    return plate, shape_box, track


def scene_load(humans):
    reset_layout()
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
    reset_layout()
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
    if x1 - x0 < 28 or y1 - y0 < 28:
        raise SystemExit("chip small %s %s" % (seat, box))
    c = FILL[seat % 4]
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
            if not near(px, BAND[i], 18):
                raise SystemExit("%s border %d %s" % (name, i, px))
        for i, shape in enumerate(shapes):
            if shape is not None:
                shape_reads(img, shape, i)
    skies = []
    patches = []
    for i, plate in enumerate(plates):
        if plate is None:
            continue
        if humans:
            box = pane_box(humans, i)
        else:
            box = (i * iw // 3, 0, (i + 1) * iw // 3, ih)
        visible_top = box[1] + 16
        visible_bot = plate[1] - 8
        mid = (visible_top + visible_bot) // 2
        park = (box[0] + 16, max(visible_top, mid - 90), box[2] - 16, min(visible_bot, mid + 90))
        if park[3] <= park[1] + 8:
            raise SystemExit("%s no park %s" % (name, park))
        patch_l = region_lum(img, park)
        skies.append(patch_l)
        if patch_l < 100 or patch_l > 170:
            raise SystemExit("%s park lum %.1f" % (name, patch_l))
        st = ImageStat.Stat(img.crop(park).convert("L"))
        if st.stddev[0] < 12:
            raise SystemExit("%s park flat %.1f" % (name, st.stddev[0]))
        frac = (plate[3] - plate[1]) / float(box[3] - box[1])
        if humans and frac > 0.34:
            raise SystemExit("%s card %.2f" % (name, frac))
        patches.append(img.crop(park).resize((80, 40)))
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
    if humans == 4 and len(patches) >= 4:
        for a in range(4):
            for b in range(a + 1, 4):
                mad = sum(ImageStat.Stat(ImageChops.difference(patches[a], patches[b])).mean) / 3.0
                if mad < 8:
                    raise SystemExit("%s cameras %d %d %.1f" % (name, a, b, mad))
    raw = img.tobytes()
    if b"podium" in raw or b"PODIUM" in raw or b"Starting" in raw or b"COMPOSITE" in raw:
        raise SystemExit("bad word in %s" % name)
    return skies


def save(img, name, notes):
    old = ui.SEAT
    ui.SEAT = list(old) + list(FILL) + list(BAND) + [WELL, INK, GOLD, CREAM, NAVY, TRACK, SHIMMER]
    try:
        ui.save(img, name, notes)
        path = os.path.join(ui.OUT, name)
        if os.path.getsize(path) > 390000:
            rgb = Image.open(path).convert("RGB")
            pinned = ui.pin_colors(rgb, list(ui.SEAT), 96)
            pinned.save(path, "PNG", optimize=True, compress_level=9)
            size = os.path.getsize(path)
            notes[-1] = (name, size, rgb.size)
            print("requant %s %d" % (name, size))
    finally:
        ui.SEAT = old


def grade_upper():
    src = park_src()
    w, h = src.size
    sky = src.crop((40, 20, 400, 160))
    st = ImageStat.Stat(sky)
    print("sky rgb %.0f %.0f %.0f" % (st.mean[0], st.mean[1], st.mean[2]))
    if st.mean[2] + 8 < st.mean[0]:
        raise SystemExit("sky still warm")
    ground = src.crop((600, 632, 680, 672))
    gst = ImageStat.Stat(ground)
    spread = max(gst.mean) - min(gst.mean)
    print("concrete rgb %.0f %.0f %.0f spread %.1f" % (gst.mean[0], gst.mean[1], gst.mean[2], spread))
    if spread > 22:
        raise SystemExit("concrete still cast %.1f" % spread)
    px = src.load()
    best = 0
    for y in range(0, h, 8):
        for x in range(0, w, 8):
            r, g, b = px[x, y]
            gap = g - max(r, b)
            if gap > best:
                best = gap
    print("grass gap %d" % best)
    if best < 24:
        raise SystemExit("grass flat %d" % best)
    upper = region_lum(src, (0, 0, w, h // 2))
    print("source upper lum %.1f" % upper)
    for i, uv in enumerate(CAM):
        uvx, uvy, uvw, uvh = uv
        top = int((1.0 - (uvy + uvh)) * h)
        if top > int(h * 0.50):
            raise SystemExit("crop starts in the floor %d" % i)
        sw = w
        left = int(uvx * sw)
        right = int((uvx + uvw) * sw)
        bottom = int((1.0 - uvy) * h)
        crop = src.crop((left, top, right, bottom))
        band = crop.crop((0, 0, crop.size[0], max(8, crop.size[1] // 2)))
        std = ImageStat.Stat(band.convert("L")).stddev[0]
        if std < 8:
            raise SystemExit("flat landmark %d %.1f" % (i, std))


def scene_pause():
    """Pause was the weak screen: a dead band under four sparse buttons."""
    reset_layout()
    img = park_src().resize((W, H), Image.Resampling.LANCZOS).convert("RGBA")
    veil = Image.new("RGBA", (W, H), (6, 12, 24, 64))
    img.alpha_composite(veil)
    d = ImageDraw.Draw(img)
    checks = []
    ratios = [ui.contrast(GOLD, NAVY), ui.contrast(CREAM, INK), ui.contrast(INK, ui.HOT)]
    header = (40, 24, W - 40, 128)
    ui.rounded(d, header, 16, NAVY)
    ROWS.append(header)
    well = (64, 44, 120, 100)
    ui.rounded(d, well, 8, WELL)
    shape = (well[0] + 8, well[1] + 8, well[2] - 8, well[3] - 8)
    stamp(d, shape, 0)
    ratios.append(ui.contrast(FILL[0], WELL))
    title_home = (140, 40, 980, 108)
    text(d, (148, 48), "Paused by P1", ui.font(ui.FONT_D, 44), GOLD, NAVY, title_home, checks, ratios, home=title_home)
    banner_home = (1000, 52, W - 64, 96)
    text(d, (1008, 58), "Mega Park  ·  Least It", ui.font(ui.FONT_B, 22), CREAM, NAVY, banner_home, checks, ratios, home=banner_home)
    items = (
        ("Resume", "Back into the match", True),
        ("Restart round", "Same arena, same rules", False),
        ("Options", "Sound, picture, and controls", False),
        ("Quit to menu", "Leave this match", False),
    )
    bh = 68
    gap = 8
    stack = len(items) * bh + (len(items) - 1) * gap
    card_w = 920
    card_h = stack + 56
    left = (W - card_w) // 2
    bottom = H - 28
    card = (left, bottom - card_h, left + card_w, bottom)
    ui.rounded(d, card, 16, INK)
    d.rectangle((card[0] + 12, card[1] + 8, card[2] - 12, card[1] + 16), fill=BAND[0])
    y = card[1] + 24
    for title, sub, hot in items:
        box = (card[0] + 16, y, card[2] - 16, y + bh)
        fill = ui.HOT if hot else ui.PANEL
        ui.rounded(d, box, 14, fill, ui.GOLD if hot else ui.STROKE, 4 if hot else 2)
        ROWS.append(box)
        title_c = INK if hot else CREAM
        sub_c = INK if hot else ui.MUTE
        title_home = (box[0] + 20, box[1] + 6, box[2] - 16, box[1] + 34)
        sub_home = (box[0] + 20, box[1] + 36, box[2] - 16, box[3] - 6)
        text(d, (title_home[0], title_home[1]), title, ui.font(ui.FONT_D, 26), title_c, fill, title_home, checks, ratios, home=title_home)
        text(d, (sub_home[0], sub_home[1]), sub, ui.font(ui.FONT_B, 16), sub_c, fill, sub_home, checks, ratios, home=sub_home)
        ratios.append(ui.contrast(title_c, fill))
        ratios.append(ui.contrast(sub_c, fill))
        y += bh + gap
    hint = (card[0] + 16, card[3] - 28, card[2] - 16, card[3] - 8)
    # Hint shares the card, so it is not a separate row. It must stay inside the card and clear of the last button.
    text(d, (hint[0], hint[1]), "Esc / B   back", ui.font(ui.FONT_B, 14), CREAM, INK, hint, checks, ratios, home=hint)
    return img, checks, ratios, card, shape


def main():
    global W, H
    grade_upper()
    notes = []
    worst = 99
    jobs = [
        ("27-load-1-composite.png", 1, (1920, 1080), scene_load),
        ("27-load-2-composite.png", 2, (1920, 1080), scene_load),
        ("27-load-3-composite.png", 3, (1920, 1080), scene_load),
        ("27-load-4-composite.png", 4, (1920, 1080), scene_load),
        ("27-load-4-720-composite.png", 4, (1280, 720), scene_load),
    ]
    for name, humans, size, build in jobs:
        W, H = size
        img, checks, ratios, plates, shapes, tracks = build(humans)
        layout_report(name)
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
    layout_report("27-load-strip-composite.png")
    miss = verify(img, checks, "27-load-strip-composite.png")
    save(img, "27-load-strip-composite.png", notes)
    probe(os.path.join(ui.OUT, "27-load-strip-composite.png"), 0, plates, shapes, tracks, True)
    # Loading and Ready must show a real gold fill, not a sliver.
    strip = Image.open(os.path.join(ui.OUT, "27-load-strip-composite.png")).convert("RGB")
    for i, expect in ((1, 0.45), (2, 0.85)):
        tr = tracks[i]
        bar = strip.crop(tr)
        gold = sum(1 for px in bar.getdata() if near(px, GOLD, 28))
        if gold < bar.size[0] * bar.size[1] * expect:
            raise SystemExit("strip fill %d %d" % (i, gold))
    worst = min(worst, min(ratios))
    print("  contrast %.2f text %.1f" % (min(ratios), miss))
    W, H = 1920, 1080
    ui.W, ui.H = W, H
    img, checks, ratios, card, shape = scene_pause()
    if min(ratios) < 4.5:
        raise SystemExit("pause contrast %.2f" % min(ratios))
    layout_report("27-pause-composite.png")
    miss = verify(img, checks, "27-pause-composite.png")
    save(img, "27-pause-composite.png", notes)
    shape_reads(img.convert("RGB"), shape, 0)
    pause = Image.open(os.path.join(ui.OUT, "27-pause-composite.png")).convert("RGB")
    park = (80, 150, W - 80, card[1] - 12)
    st = ImageStat.Stat(pause.crop(park).convert("L"))
    if st.stddev[0] < 12:
        raise SystemExit("pause park flat %.1f" % st.stddev[0])
    frac = (card[3] - card[1]) / float(H)
    if frac > 0.34:
        raise SystemExit("pause card %.2f" % frac)
    print("  pause contrast %.2f text %.1f card %.2f park std %.1f" % (min(ratios), miss, frac, st.stddev[0]))
    worst = min(worst, min(ratios))
    print("min contrast", round(worst, 2))
    for name, size, _ in notes:
        if size > 400000:
            raise SystemExit("size %s %d" % (name, size))


if __name__ == "__main__":
    main()
