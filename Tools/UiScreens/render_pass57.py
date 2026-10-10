#!/usr/bin/env python3
"""Pass 57 headless plates. These are not Unity captures.

The dark keyline is the same colour MenuWidgets.BandKeyline draws,
(0.02, 0.02, 0.04). Load, pause, and results each show it. The orange
seat stays (0.94, 0.42, 0.14). This script slices the existing atlases.
It does not rewrite them.
"""

import os

import render_pass27 as p27
import render_pass28 as p28
import render_pass31 as p31
import render_pass32 as p32
import render_screens2 as ui
from PIL import Image, ImageDraw

ROOT = ui.ROOT
OUT = os.path.join(ROOT, "Docs", "UiStills", "pass57")
W, H = 1280, 720
KEY = (5, 5, 10)
ORANGE = (240, 107, 36)
RED = (230, 46, 51)
BLUE = (51, 122, 224)
LAVENDER = (209, 179, 250)
SEAT = (RED, BLUE, ORANGE, LAVENDER)
CAP = "Headless. Not a Unity capture."
LOAD_ATLAS = os.path.join(ROOT, "Assets", "Resources", "UI", "Menu", "SeatLoad.png")
RESULT_ATLAS = os.path.join(ROOT, "Assets", "Resources", "UI", "Menu", "SeatResult.png")


def slice_row(path, cols, row, rows):
    atlas = Image.open(path).convert("RGBA")
    aw, ah = atlas.size
    cw, ch = aw // cols, ah // rows
    cells = []
    for i in range(cols):
        cells.append(atlas.crop((i * cw, row * ch, (i + 1) * cw, (row + 1) * ch)))
    return cells


def frame(d, box, color, px):
    l, t, r, b = [int(v) for v in box]
    if r <= l or b <= t:
        return
    d.rectangle((l, t, r - 1, t + px - 1), fill=color)
    d.rectangle((l, b - px, r - 1, b - 1), fill=color)
    d.rectangle((l, t, l + px - 1, b - 1), fill=color)
    d.rectangle((r - px, t, r - 1, b - 1), fill=color)


def ring(d, box, px=2):
    l, t, r, b = [int(v) for v in box]
    if r <= l or b <= t:
        return
    d.rectangle((l, t, r - 1, t + px - 1), fill=KEY)
    d.rectangle((l, b - px, r - 1, b - 1), fill=KEY)
    d.rectangle((l, t, l + px - 1, b - 1), fill=KEY)
    d.rectangle((r - px, t, r - 1, b - 1), fill=KEY)


def keyed_bar(d, box, fill, px=2):
    l, t, r, b = [int(v) for v in box]
    d.rectangle((l - px, t - px, r - 1 + px, b - 1 + px), fill=KEY)
    d.rectangle((l, t, r - 1, b - 1), fill=fill)


def caption(img, x, y):
    d = ImageDraw.Draw(img)
    face = ui.font(ui.FONT_B, 18)
    tw = d.textlength(CAP, font=face)
    box = (int(x), int(y), int(x + tw + 20), int(y + 28))
    ui.rounded(d, box, 6, (15, 41, 102))
    d.text((box[0] + 10, box[1] + 4), CAP, font=face, fill=(236, 228, 210))
    return box


def near(px, want, tol=8):
    return all(abs(int(px[i]) - want[i]) <= tol for i in range(3))


def save_as(img, name, restamp=None):
    rgb = img.convert("RGB")
    if rgb.size != (W, H):
        rgb = rgb.resize((W, H), Image.Resampling.LANCZOS)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    rgb.save(path, "PNG", optimize=True, compress_level=9)
    if os.path.getsize(path) > 400000:
        saved = False
        for colors in (128, 96, 64, 48):
            q = rgb.quantize(colors=colors, method=Image.Quantize.MEDIANCUT).convert("RGB")
            if restamp:
                restamp(q)
            q.save(path, "PNG", optimize=True, compress_level=9)
            if os.path.getsize(path) <= 400000:
                saved = True
                break
        if not saved:
            raise SystemExit("size %s" % name)
    size = os.path.getsize(path)
    print(name, size, Image.open(path).size)
    raw = Image.open(path).convert("RGB").tobytes()
    if b"COMPOSITE" in raw:
        raise SystemExit("bad word")
    return path


def down(img, boxes):
    """1920x1080 plate to 1280x720, then the caller draws a crisp keyline."""
    rgb = img.convert("RGB")
    if rgb.size == (1920, 1080):
        rgb = rgb.resize((W, H), Image.Resampling.LANCZOS)
    sx = W / 1920.0
    sy = H / 1080.0
    scaled = []
    for box in boxes:
        scaled.append((
            int(round(box[0] * sx)),
            int(round(box[1] * sy)),
            int(round(box[2] * sx)),
            int(round(box[3] * sy)),
        ))
    return rgb, scaled


def loading(cells):
    p27.BAND[2] = ORANGE
    p32.TIPS = (
        "Tag-back is 1 second.",
        "Double-click [LT] to let go of the grapple.",
        "Least It: least time as It. Next punch breaks a tie.",
        "Hot Potato: first to 2.",
    )
    img, _checks, ratios, _plates, _shapes, _tracks, rects, header = p32.scene_load(cells)
    p32.figure_gate(img, rects)
    if min(ratios) < 4.5:
        raise SystemExit("contrast")
    panes = [p27.pane_box(4, i) for i in range(4)]
    img, panes = down(img, panes)
    _hx0, hy0, hx1, _hy1 = header
    cap_at = (int(round(hx1 * W / 1920.0)) + 12, int(round(hy0 * H / 1080.0)))

    def paint(plate):
        draw = ImageDraw.Draw(plate)
        for i, box in enumerate(panes):
            frame(draw, box, SEAT[i], 8)
            ring(draw, box, 2)
        caption(plate, cap_at[0], cap_at[1])

    paint(img)
    path = save_as(img, "57-load.png", restamp=paint)
    got = Image.open(path).convert("RGB")
    # Outer 2 px is the keyline. The seat colour sits just inside that ring.
    if got.getpixel((0, 0)) != KEY:
        raise SystemExit("load keyline %s" % (got.getpixel((0, 0)),))
    pane = panes[2]
    orange_px = got.getpixel((pane[0] + 4, pane[1] + 4))
    if orange_px != ORANGE:
        raise SystemExit("load orange %s" % (orange_px,))
    print("load keyline", got.getpixel((0, 0)), "orange", orange_px)


def pause():
    img, ratios, card = p31.scene_pause()
    if min(ratios) < 4.5:
        raise SystemExit("pause contrast")
    bar = (card[0] + 12, card[1] + 6, card[2] - 12, card[1] + 14)
    img, bars = down(img, [bar])
    bar = bars[0]

    def paint(plate):
        draw = ImageDraw.Draw(plate)
        keyed_bar(draw, bar, RED, 2)
        caption(plate, 64, 88)

    paint(img)
    path = save_as(img, "57-pause.png", restamp=paint)
    got = Image.open(path).convert("RGB")
    inner = got.getpixel((bar[0] + 8, (bar[1] + bar[3]) // 2))
    edge = got.getpixel((bar[0] + 8, bar[1] - 2))
    if not near(inner, RED, 12):
        raise SystemExit("pause fill %s" % (inner,))
    if edge != KEY:
        raise SystemExit("pause keyline %s" % (edge,))
    print("pause keyline", edge, "fill", inner)


def results(cells):
    img = Image.new("RGB", (W, H), (12, 16, 28))
    d = ImageDraw.Draw(img)
    d.text((64, 28), "RESULTS", font=ui.font(ui.FONT_D, 42), fill=(255, 214, 120))
    d.text((64, 84), "Least It. Round wins, then time as It.", font=ui.font(ui.FONT_B, 20), fill=(236, 228, 210))
    places = ("1st", "2nd", "3rd", "4th")
    times = ("12.4s", "40.1s", "51.0s", "66.2s")
    wins = ("1", "0", "0", "0")
    gap = 16
    left = 40
    top = 132
    card_w = (W - left * 2 - gap * 3) // 4
    card_h = 500
    side = min(180, card_w - 36)
    bars = []
    for i in range(4):
        x = left + i * (card_w + gap)
        y = top
        plate = (18, 16, 12) if i == 0 else (28, 36, 52)
        ink = (18, 16, 12) if i == 0 else (236, 228, 210)
        if i == 0:
            plate = (255, 214, 120)
        d.rounded_rectangle((x, y, x + card_w, y + card_h), 12, fill=plate)
        bar = (x + 10, y + 12, x + card_w - 10, y + 22)
        bars.append(bar)
        keyed_bar(d, bar, SEAT[i], 2)
        fig = cells[i].resize((side, side), Image.Resampling.LANCZOS)
        fx = x + (card_w - side) // 2
        fy = y + 36
        img.paste(fig, (fx, fy), fig)
        d = ImageDraw.Draw(img)
        body = ui.font(ui.FONT_D, 26)
        sub = ui.font(ui.FONT_B, 16)
        d.text((x + 16, y + 36 + side + 8), places[i] + "  P%d" % (i + 1), font=body, fill=ink)
        d.text((x + 16, y + 36 + side + 42), times[i] + "   wins " + wins[i], font=sub, fill=ink)
    caption(img, 64, 656)
    path = save_as(img, "57-results.png")
    got = Image.open(path).convert("RGB")
    bar = bars[2]
    inner = got.getpixel(((bar[0] + bar[2]) // 2, (bar[1] + bar[3]) // 2))
    edge = got.getpixel(((bar[0] + bar[2]) // 2, bar[1] - 2))
    if not near(inner, ORANGE, 12):
        raise SystemExit("results orange %s" % (inner,))
    if edge != KEY:
        raise SystemExit("results keyline %s" % (edge,))
    print("results keyline", edge, "orange", inner)


def main():
    load_cells = slice_row(LOAD_ATLAS, 4, 0, 1)
    # File rows, top to bottom: celebrate, fist, weight, slump. Rank i uses that row.
    result_cells = []
    for rank in range(4):
        result_cells.append(slice_row(RESULT_ATLAS, 4, rank, 4)[rank])
    loading(load_cells)
    pause()
    results(result_cells)


if __name__ == "__main__":
    main()
