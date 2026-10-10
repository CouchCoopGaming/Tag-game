#!/usr/bin/env python3
"""Pass 55 headless frames. These are not Unity captures."""

import os

import render_pass27 as p27
import render_pass32 as p32
import render_screens2 as ui
from PIL import Image, ImageDraw

ROOT = ui.ROOT
OUT = os.path.join(ROOT, "Docs", "UiStills", "pass55")
W, H = 1280, 720


def fit(img):
    rgb = img.convert("RGB")
    if rgb.size != (W, H):
        rgb = rgb.resize((W, H), Image.Resampling.LANCZOS)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, "tmp.png")
    rgb.save(path, "PNG", optimize=True, compress_level=9)
    if os.path.getsize(path) > 400000:
        q = rgb.quantize(colors=128, method=Image.Quantize.MEDIANCUT).convert("RGB")
        q.save(path, "PNG", optimize=True, compress_level=9)
    return path


def save_as(img, name):
    path = fit(img)
    dest = os.path.join(OUT, name)
    os.replace(path, dest)
    print(name, os.path.getsize(dest), Image.open(dest).size)
    if os.path.getsize(dest) > 400000:
        raise SystemExit("size %s" % name)


def loading(cells):
    p32.TIPS = (
        "Tag-back is 1 second.",
        "Double-click [LT] to let go of the grapple.",
        "Least It: least time as It. Next punch breaks a tie.",
        "Hot Potato: first to 2.",
    )
    img, _checks, ratios, _plates, _shapes, _tracks, rects, _header = p32.scene_load(cells)
    p32.figure_gate(img, rects)
    if min(ratios) < 4.5:
        raise SystemExit("contrast")
    save_as(img, "55-load.png")


def it_marker():
    img = Image.new("RGB", (W, H), (18, 24, 32))
    d = ImageDraw.Draw(img)
    cx, ground = 640, 560
    d.ellipse((cx - 210, ground - 28, cx + 210, ground + 28), fill=(255, 120, 40))
    d.rounded_rectangle((cx - 70, ground - 280, cx + 70, ground - 20), 28, fill=(214, 92, 48))
    d.ellipse((cx - 48, ground - 330, cx + 48, ground - 250), fill=(230, 200, 170))
    d.ellipse((cx - 90, ground - 250, cx + 90, ground - 228), fill=(255, 70, 16))
    d.rounded_rectangle((cx - 36, ground - 360, cx + 36, ground - 250), 10, fill=(255, 51, 5))
    d.ellipse((cx - 22, ground - 392, cx + 22, ground - 348), fill=(255, 217, 38))
    d.rounded_rectangle((cx - 8, ground - 520, cx + 8, ground - 370), 4, fill=(255, 115, 13))
    face = ui.font(ui.FONT_B, 22)
    d.text((80, 40), "It marker meshes", font=ui.font(ui.FONT_D, 36), fill=(255, 214, 120))
    d.text((80, 96), "Hat, brim, tip, beacon, and halo. Not a Unity capture.", font=face, fill=(236, 228, 210))
    save_as(img, "55-it.png")


def wins():
    img = Image.new("RGB", (W, H), (12, 16, 28))
    d = ImageDraw.Draw(img)
    d.text((64, 36), "RESULTS", font=ui.font(ui.FONT_D, 42), fill=(255, 214, 120))
    d.text((64, 92), "Least It. Round wins, then time as It.", font=ui.font(ui.FONT_B, 22), fill=(236, 228, 210))
    rows = (
        (True, "1", "P1", "12.4s", "1"),
        (False, "2", "P2", "40.1s", "0"),
        (False, "3", "P3", "51.0s", "0"),
        (False, "4", "P4", "66.2s", "0"),
    )
    y = 160
    head = ui.font(ui.FONT_B, 18)
    d.text((120, y), "Place", font=head, fill=(180, 190, 200))
    d.text((280, y), "Seat", font=head, fill=(180, 190, 200))
    d.text((520, y), "Time as It", font=head, fill=(180, 190, 200))
    d.text((860, y), "Wins", font=head, fill=(180, 190, 200))
    y += 36
    body = ui.font(ui.FONT_D, 28)
    for win, place, seat, time, wins_n in rows:
        ink = (18, 16, 12) if win else (236, 228, 210)
        plate = (255, 214, 120) if win else (28, 36, 52)
        d.rounded_rectangle((80, y, 1100, y + 72), 12, fill=plate)
        d.text((120, y + 18), place, font=body, fill=ink)
        d.text((280, y + 18), seat, font=body, fill=ink)
        d.text((520, y + 18), time, font=body, fill=ink)
        d.text((860, y + 18), wins_n, font=body, fill=ink)
        y += 88
    d.text((80, 660), "Headless drawing. Not a Unity capture.", font=ui.font(ui.FONT_B, 18), fill=(180, 190, 200))
    save_as(img, "55-wins.png")


def main():
    cells = p32.pack_atlas()
    loading(cells)
    it_marker()
    wins()


if __name__ == "__main__":
    main()
