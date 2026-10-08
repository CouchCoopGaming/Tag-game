#!/usr/bin/env python3
"""Pass 31. Float Gaussian title plate, Hier idle pair, loading and pause chips."""

import math
import os
import subprocess

import numpy as np
import render_pass27 as p27
import render_pass28 as p28
import render_screens2 as ui
from PIL import Image, ImageDraw, ImageStat

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass31")
p28.W, p28.H = 1920, 1080

INK = p28.INK
CREAM = p28.CREAM
GOLD = p28.GOLD
NAVY = p28.NAVY
PLATE = p28.PLATE
WELL = p28.WELL
FILL = [p27.FILL[0], p27.FILL[1], p27.FILL[2], (204, 184, 235)]
p27.FILL = FILL
BAND = p28.BAND
W, H = 1920, 1080
BLUR_PATH = os.path.join(ui.ROOT, "Assets", "Resources", "UI", "Menu", "MegaBlur.png")
SEAT = os.path.join(ui.ROOT, "Assets", "Resources", "UI", "Menu", "SeatIdle.png")
SIGMA = 24.0
# Sky above the logo, clear of the lockup. Two patches so a cloud in one
# does not hide a ring in the other. The reported step is the max of both.
SKY = ((80, 16, 560, 132), (1360, 16, 1840, 132))


def gaussian_kernel(sigma):
    radius = max(1, int(math.ceil(sigma * 3.0)))
    x = np.arange(-radius, radius + 1, dtype=np.float64)
    k = np.exp(-0.5 * (x / sigma) ** 2)
    k /= k.sum()
    return k.astype(np.float32)


def blur_axis(img, kernel, axis):
    radius = len(kernel) // 2
    pad = [(0, 0), (0, 0), (0, 0)]
    pad[axis] = (radius, radius)
    src = np.pad(img, pad, mode="reflect")
    out = np.zeros_like(img)
    for i, weight in enumerate(kernel):
        sl = [slice(None), slice(None), slice(None)]
        sl[axis] = slice(i, i + img.shape[axis])
        out += src[tuple(sl)] * float(weight)
    return out


def gaussian_blur(img, sigma):
    kernel = gaussian_kernel(sigma)
    return blur_axis(blur_axis(img, kernel, 1), kernel, 0)


def blue_noise(h, w, seed=31):
    """Unit-variance blue noise. Power rises with frequency."""
    rng = np.random.default_rng(seed)
    fy = np.fft.fftfreq(h).astype(np.float32)[:, None]
    fx = np.fft.fftfreq(w).astype(np.float32)[None, :]
    rad = np.sqrt(fx * fx + fy * fy)
    phase = rng.uniform(0.0, 2.0 * math.pi, (h, w)).astype(np.float32)
    spec = rad * (np.cos(phase) + 1j * np.sin(phase))
    noise = np.fft.ifft2(spec).real.astype(np.float32)
    noise -= noise.mean()
    noise /= np.std(noise) + 1e-8
    return noise


def vignette_alpha():
    """Matches MenuHost.TitleWashTex: smoothstep from r=0.20 over 1.20, peak 0.30.
    The live wash is a square stretched over the frame, so x and y are
    normalized independently."""
    yy, xx = np.mgrid[0:H, 0:W]
    cx = (W - 1) * 0.5
    cy = (H - 1) * 0.5
    dx = (xx - cx) / cx
    dy = (yy - cy) / cy
    radius = np.sqrt(dx * dx + dy * dy)
    t = (radius - 0.20) / 1.20
    np.clip(t, 0.0, 1.0, out=t)
    t = t * t * (3.0 - 2.0 * t)
    return (t * 0.30).astype(np.float32)


def quantize(img, noise, lsb=0.65):
    """Blue-noise dither, then round to 8-bit. img is float 0..1."""
    dither = noise * (lsb / 255.0)
    out = img + dither[..., None]
    return np.clip(np.round(out * 255.0), 0, 255).astype(np.uint8)


def row_step(rgb, box):
    x0, y0, x1, y1 = box
    crop = np.asarray(rgb.convert("RGB") if not isinstance(rgb, np.ndarray) else rgb)[y0:y1, x0:x1]
    if crop.dtype != np.float32 and crop.dtype != np.float64:
        crop = crop.astype(np.float32)
    lum = 0.2126 * crop[..., 0] + 0.7152 * crop[..., 1] + 0.0722 * crop[..., 2]
    rows = lum.mean(axis=1)
    return float(np.max(np.abs(np.diff(rows))))


def sky_step(src):
    if isinstance(src, str):
        img = Image.open(src).convert("RGB")
    else:
        img = src
    return max(row_step(img, box) for box in SKY)


def corner_mean(img):
    rgb = img.convert("RGB")
    boxes = ((0, 0, 70, 70), (W - 70, 0, W, 70), (0, H - 70, 70, H), (W - 70, H - 70, W, H))
    return [ImageStat.Stat(rgb.crop(box)).mean for box in boxes]


def save_bytes(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.convert("RGB").save(path, "PNG", optimize=True, compress_level=9)


def fit_png(img, path, sky_limit, size_limit):
    """pngquant only when the 8-bit file is over the cap. A result that
    reintroduces sky rings is rejected."""
    save_bytes(img, path)
    size = os.path.getsize(path)
    step = sky_step(path)
    print("plate", os.path.basename(path), size, "sky-step", round(step, 3))
    if step > sky_limit:
        raise SystemExit("banding %s step %.3f" % (path, step))
    if size <= size_limit:
        return step, size
    for quality in ("92-100", "80-98", "70-95", "60-92", "50-88", "45-82"):
        trial = path + ".q.png"
        proc = subprocess.run(
            ["pngquant", "--quality", quality, "--speed", "1", "--strip", "--force", "-o", trial, path],
            capture_output=True,
        )
        if proc.returncode != 0 or not os.path.isfile(trial):
            print("pngquant skip", quality, proc.returncode)
            continue
        qsize = os.path.getsize(trial)
        qstep = sky_step(trial)
        print("pngquant", quality, qsize, "sky-step", round(qstep, 3))
        if qstep <= sky_limit and qsize <= size_limit:
            os.replace(trial, path)
            return qstep, qsize
        os.remove(trial)
    raise SystemExit("size %s %d step %.3f" % (path, size, step))


def bake_plate(noise):
    src = p28.live().resize((W, H), Image.Resampling.LANCZOS).convert("RGB")
    sharp = np.asarray(src).astype(np.float32) / 255.0
    soft = gaussian_blur(sharp, SIGMA)
    blur_u8 = Image.fromarray(quantize(soft, noise, 0.65), "RGB")
    # The live plate is the blur only. Dim and vignette are overlays.
    step, size = fit_png(blur_u8, BLUR_PATH, 1.6, 750000)
    print("megablur", size, "sky-step", round(step, 3))
    dim = soft * 0.65
    washed = dim * (1.0 - vignette_alpha())[..., None]
    plate = Image.fromarray(quantize(washed, noise, 0.65), "RGB")
    return plate, Image.fromarray(quantize(dim, noise, 0.0), "RGB")


def assert_plate_corners(before, after):
    got = corner_mean(after)
    was = corner_mean(before)
    for i, mean in enumerate(got):
        if max(mean) < 28:
            raise SystemExit("title corner black %s" % (tuple(round(v) for v in mean),))
        if max(mean) > max(was[i]) + 1:
            raise SystemExit("title vignette did not fall off")
    print("title corners", tuple(tuple(round(v) for v in m) for m in got))


def scene_title(plate, before):
    p27.reset_layout()
    img = plate.convert("RGBA")
    assert_plate_corners(before, img)
    d = ImageDraw.Draw(img)
    checks, ratios = [], []
    p28.lockup(img, (560, 150, 1360, 760))
    d = ImageDraw.Draw(img)
    plate_box = (260, 800, 1660, 960)
    ui.rounded(d, plate_box, 28, PLATE)
    p27.ROWS.append(plate_box)
    line = "Press Space or Start"
    face = ui.font(ui.FONT_D, 84)
    tw = d.textlength(line, font=face)
    home = (280, 820, 1640, 940)
    p27.text(d, ((W - tw) / 2, 832), line, face, CREAM, PLATE, home, checks, ratios, home=home)
    return img, ratios


def assert_pair(path):
    im = Image.open(path).convert("RGBA")
    arr = np.asarray(im)
    opaque = arr[..., 3] > 180
    if int(opaque.sum()) < 8000:
        raise SystemExit("seat render empty")
    h, w = opaque.shape
    left = int(opaque[:, : w // 2].sum())
    right = int(opaque[:, w // 2 :].sum())
    if left < 2000 or right < 2000:
        raise SystemExit("seat render missing a side %d %d" % (left, right))
    rgb = arr[..., :3][opaque].astype(np.float32)
    lum = 0.2126 * rgb[:, 0] + 0.7152 * rgb[:, 1] + 0.0722 * rgb[:, 2]
    if float(lum.std()) < 10:
        raise SystemExit("seat render flat std %.1f" % float(lum.std()))
    print("seat opaque", int(opaque.sum()), "std", round(float(lum.std()), 1), "halves", left, right)


def paste_pair(img, box):
    fig = Image.open(SEAT).convert("RGBA")
    bbox = fig.getbbox()
    if bbox:
        fig = fig.crop(bbox)
    x0, y0, x1, y1 = box
    tw, th = x1 - x0, y1 - y0
    scale = min(tw / float(fig.size[0]), th / float(fig.size[1]))
    nw = max(1, int(round(fig.size[0] * scale)))
    nh = max(1, int(round(fig.size[1] * scale)))
    fig = fig.resize((nw, nh), Image.Resampling.LANCZOS)
    ox = x0 + (tw - nw) // 2
    oy = y1 - nh
    img.alpha_composite(fig, (ox, oy))
    return (ox, oy, ox + nw, oy + nh)


def scene_main():
    p27.reset_layout()
    img = p28.darken(0.30)
    d = ImageDraw.Draw(img)
    checks, ratios = [], []
    face = ui.font(ui.FONT_D, 48)
    label = "Menu"
    tw = d.textlength(label, font=face)
    header = (96, 16, int(96 + tw + 72), 100)
    if header[2] > 520:
        raise SystemExit("menu bar still wide")
    ui.rounded(d, header, 12, NAVY)
    p27.ROWS.append(header)
    p27.text(d, (96 + 36, 28), label, face, GOLD, NAVY, header, checks, ratios, home=header)
    bare = img.convert("RGB").getpixel((header[2] + 48, 50))
    if abs(bare[0] - NAVY[0]) < 8 and abs(bare[1] - NAVY[1]) < 8 and abs(bare[2] - NAVY[2]) < 12:
        raise SystemExit("menu bar still full width %s" % (bare,))
    p28.lockup(img, (140, 112, 520, 360))
    d = ImageDraw.Draw(img)
    blurb = (110, 368, 860, 416)
    ui.rounded(d, blurb, 10, NAVY)
    p27.ROWS.append(blurb)
    p27.text(d, (128, 376), "Local couch. One keyboard, four pads.", ui.font(ui.FONT_B, 22), CREAM, NAVY, blurb, checks, ratios, home=blurb)
    placed = paste_pair(img, (140, 428, 840, 868))
    if placed[1] < blurb[3]:
        raise SystemExit("figures cover the blurb")
    d = ImageDraw.Draw(img)
    tip = (110, 888, 860, 1048)
    if placed[3] > tip[1]:
        raise SystemExit("figures cover the tips")
    ui.rounded(d, tip, 16, NAVY)
    p27.ROWS.append(tip)
    tip_px = img.convert("RGB").getpixel((200, 930))
    if abs(tip_px[0] - NAVY[0]) > 30 or abs(tip_px[2] - NAVY[2]) > 40:
        raise SystemExit("tips covered %s" % (tip_px,))
    lines = (
        (896, "Tip of the day", p28.MUTE),
        (932, "Space jumps.", CREAM),
        (968, "Hold [WASD] against a wall to climb.", CREAM),
        (1004, "Double-click [RMB] to let go of the grapple.", CREAM),
    )
    for y, word, fill in lines:
        home = (128, y - 4, 844, y + 34)
        p27.text(d, (136, y), word, ui.font(ui.FONT_B, 22), fill, NAVY, home, checks, ratios, home=home)
    rows = (
        ("Play", "Local couch", True),
        ("Practice", "Free run any arena, no tagger", False),
        ("Options", "Sound, picture, access", False),
        ("Controls", "Binds. Space still jumps.", False),
    )
    if sum(1 for _, _, hot in rows if hot) != 1:
        raise SystemExit("two focus")
    y = 120
    play = None
    for title, sub, hot in rows:
        box = (920, y, 1760, y + 108)
        if hot:
            play = box
        p28.paint_row(d, box, title, sub, hot, checks, ratios)
        y += 144
    records = None
    for title, x in (("Credits", 920), ("Records", 1200), ("Quit", 1480)):
        box = (x, y, x + 260, y + 96)
        if title == "Records":
            records = box
        p28.paint_row(d, box, title, "", False, checks, ratios)
    one_focus(img, play, records)
    return img, ratios


def one_focus(img, play, records):
    rgb = img.convert("RGB")

    def edge(box):
        x0, y0, _, _ = box
        return rgb.getpixel((x0 - 10, y0 + 24))

    play_px = edge(play)
    rec_px = edge(records)
    print("play edge", play_px, "records edge", rec_px)
    if play_px[0] < 200 or play_px[1] < 160:
        raise SystemExit("play missing gold")
    if rec_px[0] > 180 and rec_px[1] > 140 and rec_px[2] < 90:
        raise SystemExit("records also focused")


def crop_cam_idle(seat, size):
    seat = seat % 4
    base = p27.crop_uv(p27.park_src(), p27.CAM[seat], size).convert("RGBA")
    if seat != 1:
        return base.convert("RGB")
    fig = Image.open(SEAT).convert("RGBA")
    w, _h = fig.size
    half = fig.crop((w // 2, 0, w, _h))
    bbox = half.getbbox()
    if bbox:
        half = half.crop(bbox)
    tw = max(8, int(size[0] * 0.62))
    th = max(8, int(size[1] * 0.70))
    scale = min(tw / float(half.size[0]), th / float(half.size[1]))
    nw = max(1, int(round(half.size[0] * scale)))
    nh = max(1, int(round(half.size[1] * scale)))
    half = half.resize((nw, nh), Image.Resampling.LANCZOS)
    x = max(0, (size[0] - nw) // 2)
    y = max(0, int(size[1] * 0.04))
    base.alpha_composite(half, (x, y))
    return base.convert("RGB")


def scene_load():
    p27.park_src = p28.live
    p27.reset_layout()
    old_w, old_h = p27.W, p27.H
    prev = p27.crop_cam
    p27.W, p27.H = W, H
    p27.crop_cam = crop_cam_idle
    try:
        img, checks, ratios, plates, shapes, tracks = p27.scene_load(4)
    finally:
        p27.W, p27.H = old_w, old_h
        p27.crop_cam = prev
    d = ImageDraw.Draw(img)
    face = ui.font(ui.FONT_D, 40)
    label = "Loading"
    tw = d.textlength(label, font=face)
    header = (28, 18, int(28 + tw + 64), 86)
    if header[3] > 92:
        raise SystemExit("loading chip hits the park sample")
    ui.rounded(d, header, 12, NAVY)
    p27.ROWS.append(header)
    p27.text(d, (header[0] + 32, 30), label, face, GOLD, NAVY, header, checks, ratios, home=header)
    return img, checks, ratios, plates, shapes, tracks


def scene_pause():
    p27.reset_layout()
    img = p28.darken(0.35)
    d = ImageDraw.Draw(img)
    checks = []
    ratios = [ui.contrast(GOLD, NAVY), ui.contrast(CREAM, INK), ui.contrast(INK, ui.HOT)]
    face = ui.font(ui.FONT_D, 40)
    label = "Paused by P1"
    tw = d.textlength(label, font=face)
    header = (96, 20, int(96 + 88 + tw + 28), 108)
    if header[2] > 860:
        raise SystemExit("pause chip still wide")
    ui.rounded(d, header, 12, NAVY)
    p27.ROWS.append(header)
    well = (header[0] + 14, 36, header[0] + 78, 92)
    ui.rounded(d, well, 8, WELL)
    shape = (well[0] + 8, well[1] + 8, well[2] - 8, well[3] - 8)
    p27.stamp(d, shape, 0)
    ratios.append(ui.contrast(FILL[0], WELL))
    title_home = (well[2] + 12, 36, header[2] - 16, 96)
    p27.text(d, (well[2] + 16, 40), label, face, GOLD, NAVY, title_home, checks, ratios, home=title_home)
    place = "Mega Park  ·  Least It"
    pface = ui.font(ui.FONT_B, 22)
    pw = d.textlength(place, font=pface)
    banner = (W - 36 - int(pw) - 48, 28, W - 36, 100)
    if banner[0] < header[2] + 40:
        raise SystemExit("pause chips meet")
    ui.rounded(d, banner, 12, NAVY)
    p27.ROWS.append(banner)
    p27.text(d, (banner[0] + 24, 46), place, pface, CREAM, NAVY, banner, checks, ratios, home=banner)
    gap_px = img.convert("RGB").getpixel(((header[2] + banner[0]) // 2, 60))
    if abs(gap_px[0] - NAVY[0]) < 10 and abs(gap_px[2] - NAVY[2]) < 14:
        raise SystemExit("pause bar still full width")
    items = (
        ("Resume", "Back into the match", True),
        ("Restart round", "Same arena, same rules", False),
        ("Options", "Sound, picture, and controls", False),
        ("Quit to menu", "Leave this match", False),
    )
    if sum(1 for _, _, hot in items if hot) != 1:
        raise SystemExit("pause two focus")
    bh, gap = 72, 8
    stack = len(items) * bh + (len(items) - 1) * gap
    card_w = 960
    card_h = stack + 48
    left = (W - card_w) // 2
    bottom = H - 24
    card = (left, bottom - card_h, left + card_w, bottom)
    ui.rounded(d, card, 16, INK)
    d.rectangle((card[0] + 12, card[1] + 6, card[2] - 12, card[1] + 14), fill=BAND[0])
    well2 = (card[0] + 20, card[1] + 18, card[0] + 52, card[1] + 50)
    ui.rounded(d, well2, 4, WELL)
    p27.stamp(d, (well2[0] + 4, well2[1] + 4, well2[2] - 4, well2[3] - 4), 0)
    y = card[1] + 18
    for title, sub, hot in items:
        box = (card[0] + 16, y, card[2] - 16, y + bh)
        p28.paint_row(d, box, title, sub, hot, checks, ratios)
        y += bh + gap
    return img, ratios, card


def finish(img, name, notes, ratios):
    if min(ratios) < 4.5:
        raise SystemExit("%s contrast %.2f" % (name, min(ratios)))
    raw = img.convert("RGB").tobytes()
    for bad in (b"podium", b"PODIUM", b"Starting", b"COMPOSITE"):
        if bad in raw:
            raise SystemExit("bad word in %s" % name)
    p27.layout_report(name)
    path = os.path.join(ui.OUT, name)
    os.makedirs(ui.OUT, exist_ok=True)
    img.convert("RGB").save(path, "PNG", optimize=True, compress_level=9)
    size = os.path.getsize(path)
    if size > 390000:
        for quality in ("80-98", "70-95", "60-92", "50-88", "40-78", "34-72"):
            trial = path + ".q.png"
            proc = subprocess.run(
                ["pngquant", "--quality", quality, "--speed", "1", "--strip", "--force", "-o", trial, path],
                capture_output=True,
            )
            if proc.returncode != 0 or not os.path.isfile(trial):
                continue
            qsize = os.path.getsize(trial)
            print("pngquant", name, quality, qsize)
            if qsize <= 390000:
                os.replace(trial, path)
                size = qsize
                break
            os.remove(trial)
    if size > 400000:
        raise SystemExit("size %s %d" % (name, size))
    notes.append((name, size))
    print("%s %d" % (name, size))
    return path


def main():
    if not os.path.isfile(SEAT):
        raise SystemExit("missing " + SEAT)
    assert_pair(SEAT)
    noise = blue_noise(H, W, 31)
    plate, before = bake_plate(noise)
    notes = []
    worst = 99.0
    img, ratios = scene_title(plate, before)
    path = finish(img, "31-title.png", notes, ratios)
    step = sky_step(path)
    print("title sky-step", round(step, 3))
    if step > 1.6:
        raise SystemExit("title banding %.3f" % step)
    worst = min(worst, min(ratios))
    print("  title %.2f" % min(ratios))
    img, ratios = scene_main()
    finish(img, "31-main.png", notes, ratios)
    worst = min(worst, min(ratios))
    print("  main %.2f" % min(ratios))
    img, _checks, ratios, plates, shapes, tracks = scene_load()
    path = finish(img, "31-load.png", notes, ratios)
    p27.probe(path, 4, plates, shapes, tracks, True)
    worst = min(worst, min(ratios))
    print("  load %.2f" % min(ratios))
    img, ratios, card = scene_pause()
    finish(img, "31-pause.png", notes, ratios)
    pause = Image.open(os.path.join(ui.OUT, "31-pause.png")).convert("RGB")
    park = pause.crop((80, 150, W - 80, card[1] - 16))
    st = ImageStat.Stat(park.convert("L"))
    frac = (card[3] - card[1]) / float(H)
    print("  pause %.2f card %.2f park std %.1f lum %.1f" % (min(ratios), frac, st.stddev[0], st.mean[0]))
    if st.stddev[0] < 8:
        raise SystemExit("pause park flat")
    if frac > 0.34:
        raise SystemExit("pause card %.2f" % frac)
    raw = p28.live()
    raw_lum = ImageStat.Stat(raw.crop((80, 150, W - 80, card[1] - 16)).convert("L")).mean[0]
    if st.mean[0] > raw_lum * 0.75:
        raise SystemExit("pause not darkened %.1f vs %.1f" % (st.mean[0], raw_lum))
    worst = min(worst, min(ratios))
    print("min contrast", round(worst, 2))
    print("SKY_STEP", round(step, 3))


if __name__ == "__main__":
    main()
