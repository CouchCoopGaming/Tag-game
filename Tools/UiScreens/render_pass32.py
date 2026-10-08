#!/usr/bin/env python3
"""Pass 32. Four matching Hier idles on the loading panes, full tip sentences, a real bar."""

import os
import subprocess

import render_pass27 as p27
import render_pass28 as p28
import render_pass31 as p31
import render_screens2 as ui
from PIL import Image, ImageDraw, ImageStat

ui.OUT = os.path.join(ui.ROOT, "Docs", "UiStills", "screens2", "pass32")
p28.W, p28.H = 1920, 1080

INK = p27.INK
CREAM = p27.CREAM
GOLD = p27.GOLD
NAVY = p27.NAVY
WELL = p27.WELL
TRACK = p27.TRACK
FILL = [p27.FILL[0], p27.FILL[1], p27.FILL[2], (204, 184, 235)]
p27.FILL = FILL
BAND = p27.BAND
W, H = 1920, 1080
ATLAS = os.path.join(ui.ROOT, "Assets", "Resources", "UI", "Menu", "SeatLoad.png")
RAW = "/tmp/seatload"

# Same lines MenuTips.Shown paints for seats 0..3 at turn 0.
TIPS = (
    "Jump [Space] to leave the ground.",
    "Sprint [LB], then slide [B].",
    "Push the left stick into a wall to climb.",
    "Press [A] while pushing the left stick into a wall to wall jump.",
)
MAIN_CLIMB = "Push [WASD] or the left stick into a wall to climb."
MAIN_GRAPPLE = "Double-click [RMB] to let go of the grapple."


def crop_park(seat, size):
    return p27.crop_uv(p28.live(), p27.CAM[seat % 4], size).convert("RGB")


def glyph_line(d, x, y, line, face, fill, bg, box, checks, ratios, home, key_h=22):
    """Punctuation stays on the sentence. A lone period is not a second chip."""
    rest = line
    cx = x
    cap = ui.font(ui.FONT_B, max(11, key_h - 6))
    while rest:
        cut = rest.find("[")
        if cut < 0:
            if rest.strip():
                paint = p27.text if len(rest.strip()) > 1 else p27.draw_only
                paint(d, (cx, y), rest, face, fill, bg, box, checks, ratios, home=home)
            break
        if cut > 0:
            pre = rest[:cut]
            paint = p27.text if len(pre.strip()) > 1 else p27.draw_only
            paint(d, (cx, y), pre, face, fill, bg, box, checks, ratios, home=home)
            cx += d.textlength(pre, font=face)
        end = rest.find("]", cut)
        if end < 0:
            break
        word = rest[cut + 1:end]
        tw = d.textlength(word, font=cap)
        w = tw + 14
        key = (cx, y, cx + w, y + key_h)
        ui.rounded(d, key, 6, NAVY)
        p27.text(d, (cx + 7, y + 1), word, cap, CREAM, NAVY, (cx + 2, y, cx + w - 2, y + key_h), checks, ratios, home=key)
        cx += w
        rest = rest[end + 1:]
        while rest and rest[0] in ".,;:!?":
            punct = rest[0]
            p27.draw_only(d, (cx + 1, y), punct, face, fill, bg, box, checks, ratios, home=home)
            cx += d.textlength(punct, font=face) + 1
            rest = rest[1:]
        if rest and not rest.startswith(" "):
            cx += 4
    return cx


def pack_atlas():
    raws = []
    bb = None
    for i in range(4):
        im = Image.open(os.path.join(RAW, "seat%d.png" % i)).convert("RGBA")
        raws.append(im)
        got = im.split()[-1].getbbox()
        if bb is None:
            bb = got
        elif got != bb:
            raise SystemExit("seat bbox drift %s %s" % (got, bb))
    pad = 16
    box = (
        max(0, bb[0] - pad),
        max(0, bb[1] - pad),
        min(raws[0].size[0], bb[2] + pad),
        min(raws[0].size[1], bb[3] + pad),
    )
    cw, ch = box[2] - box[0], box[3] - box[1]
    side = max(cw, ch)
    atlas = Image.new("RGBA", (side * 4, side), (0, 0, 0, 0))
    cells = []
    ox = (side - cw) // 2
    oy = side - ch
    for i, im in enumerate(raws):
        cell = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        cell.paste(im.crop(box), (ox, oy))
        cells.append(cell)
        atlas.paste(cell, (i * side, 0))
    os.makedirs(os.path.dirname(ATLAS), exist_ok=True)
    atlas.save(ATLAS, "PNG", optimize=True)
    print("atlas", ATLAS, atlas.size, os.path.getsize(ATLAS))
    return cells


def figure_rect(box):
    """Lower-right square on a 16:9 pane. Feet sit just above the 32% card."""
    l, _t, _r, b = box
    pw, ph = _r - l, b - _t
    fh = 0.38
    fw = fh * (ph / float(pw))
    if fw > 0.34:
        fw = 0.34
        fh = fw * (pw / float(ph))
    fig_h = int(round(ph * fh))
    fig_w = fig_h
    right = l + int(round(pw * 0.97))
    bottom = b - int(round(ph * 0.34))
    return (right - fig_w, bottom - fig_h, right, bottom)


def load_pane(img, box, seat, cell, checks, ratios):
    l, t, r, b = box
    shot = crop_park(seat, (r - l, b - t)).convert("RGBA")
    img.alpha_composite(shot, (l, t))
    rect = figure_rect(box)
    fig = cell.resize((rect[2] - rect[0], rect[3] - rect[1]), Image.Resampling.LANCZOS)
    img.alpha_composite(fig, (rect[0], rect[1]))
    d = ImageDraw.Draw(img)
    p27.borders(d, box, seat)
    pw, ph = r - l, b - t
    well_s, shape_s = p27.chip_size(pw, ph)
    tip_h = 32
    cap_h = 16
    gap = 4
    pad = 8
    limit = int(ph * 0.32)
    track_h = 26
    content = pad + well_s + gap + tip_h + gap + track_h + gap + cap_h + pad
    if content > limit:
        track_h -= content - limit
        if track_h < 18:
            raise SystemExit("bar collapsed")
        content = limit
    plate = (l + 10, b - 8 - content, r - 10, b - 8)
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
        p27.ROWS.append(row)
    name_px = 18
    name = ui.font(ui.FONT_D, name_px)
    sub = ui.font(ui.FONT_B, 13)
    inset = (well_s - shape_s) // 2
    well = (header[0], header[1], header[0] + well_s, header[1] + well_s)
    ui.rounded(d, well, 6, WELL)
    shape_box = (well[0] + inset, well[1] + inset, well[2] - inset, well[3] - inset)
    p27.stamp(d, shape_box, seat)
    ratios.append(ui.contrast(FILL[seat], WELL))
    name_x = well[2] + 8
    title_home = (name_x, header[1], plate[2] - 52, header[1] + name_px + 4)
    p27.text(d, (name_x, header[1]), "Mega Park", name, GOLD, INK, title_home, checks, ratios, home=title_home)
    size_home = (name_x, header[1] + name_px + 2, plate[2] - 52, header[3])
    p27.text(d, (name_x, header[1] + name_px + 2), p27.SIZE_LINE, sub, CREAM, INK, size_home, checks, ratios, home=size_home)
    who = ui.font(ui.FONT_B, 14)
    who_home = (plate[2] - 44, header[1] + 4, plate[2] - 4, header[1] + 24)
    p27.text(d, (who_home[0], who_home[1]), "P%d" % (seat + 1), who, CREAM, INK, who_home, checks, ratios, home=who_home)
    tip = TIPS[seat]
    ui.rounded(d, tip_box, 6, GOLD)
    face = p27.fit_face(d, tip, tip_box[2] - tip_box[0] - 16, 15)
    key_h = tip_h - 8
    glyph_line(d, tip_box[0] + 8, tip_box[1] + (tip_h - key_h) // 2, tip, face, INK, GOLD, tip_box, checks, ratios, tip_box, key_h)
    ui.rounded(d, track, 5, TRACK)
    span = track[2] - track[0]
    cap_w = max(10, int(span * 0.04))
    d.rounded_rectangle(
        (track[0] + 3, track[1] + 3, track[0] + 3 + cap_w, track[3] - 3),
        4,
        fill=GOLD,
    )
    cap = ui.font(ui.FONT_B, 13)
    p27.text(d, (cap_row[0] + 4, cap_row[1]), "Waiting  0%", cap, CREAM, INK, cap_row, checks, ratios, home=cap_row)
    if rect[3] > plate[1] - 2:
        raise SystemExit("figure on the card %s" % (rect,))
    if rect[1] < t + 4:
        raise SystemExit("figure cropped %s" % (rect,))
    return plate, shape_box, track, rect


def scene_load(cells):
    p27.reset_layout()
    p27.W, p27.H = W, H
    img = Image.new("RGBA", (W, H), (12, 18, 28, 255))
    checks = []
    ratios = [ui.contrast(GOLD, INK), ui.contrast(CREAM, INK), ui.contrast(INK, GOLD)]
    plates, shapes, tracks, rects = [], [], [], []
    for i in range(4):
        box = p27.pane_box(4, i)
        plate, shape, track, rect = load_pane(img, box, i, cells[i], checks, ratios)
        plates.append(plate)
        shapes.append(shape)
        tracks.append(track)
        rects.append(rect)
    d = ImageDraw.Draw(img)
    face = ui.font(ui.FONT_D, 40)
    label = "Loading"
    tw = d.textlength(label, font=face)
    header = (28, 18, int(28 + tw + 64), 86)
    ui.rounded(d, header, 12, NAVY)
    p27.ROWS.append(header)
    p27.text(d, (header[0] + 32, 30), label, face, GOLD, NAVY, header, checks, ratios, home=header)
    return img, checks, ratios, plates, shapes, tracks, rects, header


def figure_gate(img, rects):
    heights, rights, feet = [], [], []
    for i, rect in enumerate(rects):
        pane = p27.pane_box(4, i)
        heights.append(rect[3] - rect[1])
        rights.append(pane[2] - rect[2])
        feet.append(pane[3] - rect[3])
        if rect[0] < pane[0] + (pane[2] - pane[0]) * 0.55:
            raise SystemExit("figure not in the right third %s" % (rect,))
        x0, y0, x1, y1 = rect
        tw, th = x1 - x0, y1 - y0
        torso = (x0 + int(tw * 0.40), y0 + int(th * 0.32), x0 + int(tw * 0.60), y0 + int(th * 0.48))
        mean = ImageStat.Stat(img.crop(torso)).mean
        r, g, b = mean[0], mean[1], mean[2]
        ok = (
            (i == 0 and r > g + 20 and r > b + 20),
            (i == 1 and b > r + 20 and b > g + 10),
            (i == 2 and r > b + 20 and g > b + 10),
            (i == 3 and max(r, g, b) - min(r, g, b) < 28 and b + 4 >= r and b + 4 >= g),
        )[i]
        if not ok:
            raise SystemExit("seat color %s %.0f %.0f %.0f" % (i, r, g, b))
    if max(heights) - min(heights) > 1:
        raise SystemExit("scale drift %s" % (heights,))
    if max(rights) - min(rights) > 1 or max(feet) - min(feet) > 1:
        raise SystemExit("placement drift %s %s" % (rights, feet))
    print("figures", heights[0], "right", rights[0], "above-pane-bottom", feet[0])


def probe(path, plates, shapes, tracks, rects):
    img = Image.open(path).convert("RGB")
    iw, ih = img.size
    seats = (
        img.getpixel((4, 4)),
        img.getpixel((iw - 6, 4)),
        img.getpixel((4, ih - 6)),
        img.getpixel((iw - 6, ih - 6)),
    )
    for i, px in enumerate(seats):
        if not p27.near(px, BAND[i], 18):
            raise SystemExit("border %d %s" % (i, px))
    for i, shape in enumerate(shapes):
        p27.shape_reads(img, shape, i)
    for i, plate in enumerate(plates):
        box = p27.pane_box(4, i)
        pw = box[2] - box[0]
        visible_top = box[1] + 16
        visible_bot = plate[1] - 8
        mid = (visible_top + visible_bot) // 2
        park = (box[0] + 16, max(visible_top, mid - 70), box[0] + int(pw * 0.52), min(visible_bot, mid + 70))
        if park[3] <= park[1] + 8 or park[2] <= park[0] + 8:
            raise SystemExit("no park %s" % (park,))
        if rects[i][0] < park[2]:
            raise SystemExit("figure in the park sample")
        lum = p27.region_lum(img, park)
        if lum < 100 or lum > 170:
            raise SystemExit("park lum %.1f" % lum)
        st = ImageStat.Stat(img.crop(park).convert("L"))
        if st.stddev[0] < 12:
            raise SystemExit("park flat %.1f" % st.stddev[0])
        frac = (plate[3] - plate[1]) / float(box[3] - box[1])
        if frac > 0.34:
            raise SystemExit("card %.2f" % frac)
        tr = tracks[i]
        if tr[3] - tr[1] < 18:
            raise SystemExit("thin bar %s" % (tr,))
        bar = img.crop(tr).convert("RGB")
        bw, bh = bar.size
        cap = bar.crop((0, 0, max(4, int(bw * 0.08)), bh))
        trough = bar.crop((int(bw * 0.35), 0, bw, bh))
        cap_l = p27.lum(tuple(int(v) for v in ImageStat.Stat(cap).mean))
        trough_l = p27.lum(tuple(int(v) for v in ImageStat.Stat(trough).mean))
        if cap_l < trough_l + 25:
            raise SystemExit("no progress cap %.1f vs %.1f" % (cap_l, trough_l))
        if trough_l > 80:
            raise SystemExit("bar trough washed %.1f" % trough_l)
        if cap_l > 220:
            raise SystemExit("waiting bar looks full %.1f" % cap_l)
    raw = img.tobytes()
    for bad in (b"podium", b"PODIUM", b"Starting", b"COMPOSITE"):
        if bad in raw:
            raise SystemExit("bad word")
    print("probe=ok")


def scene_main():
    """Accepted main menu. Only the climb sentence changes."""
    p27.reset_layout()
    img = p28.darken(0.30)
    d = ImageDraw.Draw(img)
    checks, ratios = [], []
    face = ui.font(ui.FONT_D, 48)
    label = "Menu"
    tw = d.textlength(label, font=face)
    header = (96, 16, int(96 + tw + 72), 100)
    ui.rounded(d, header, 12, NAVY)
    p27.ROWS.append(header)
    p27.text(d, (96 + 36, 28), label, face, GOLD, NAVY, header, checks, ratios, home=header)
    p28.lockup(img, (140, 112, 520, 360))
    d = ImageDraw.Draw(img)
    blurb = (110, 368, 860, 416)
    ui.rounded(d, blurb, 10, NAVY)
    p27.ROWS.append(blurb)
    p27.text(d, (128, 376), "Local couch. One keyboard, four pads.", ui.font(ui.FONT_B, 22), CREAM, NAVY, blurb, checks, ratios, home=blurb)
    placed = p31.paste_pair(img, (140, 428, 840, 868))
    if placed[1] < blurb[3]:
        raise SystemExit("figures cover the blurb")
    d = ImageDraw.Draw(img)
    tip = (110, 888, 860, 1048)
    if placed[3] > tip[1]:
        raise SystemExit("figures cover the tips")
    ui.rounded(d, tip, 16, NAVY)
    p27.ROWS.append(tip)
    lines = (
        (896, "Tip of the day", p28.MUTE),
        (932, "Space jumps.", CREAM),
        (968, MAIN_CLIMB, CREAM),
        (1004, MAIN_GRAPPLE, CREAM),
    )
    tip_face = ui.font(ui.FONT_B, 22)
    if d.textlength(MAIN_CLIMB, font=tip_face) > 700:
        tip_face = ui.font(ui.FONT_B, 18)
    for y, word, fill in lines:
        home = (128, y - 4, 844, y + 34)
        p27.text(d, (136, y), word, tip_face, fill, NAVY, home, checks, ratios, home=home)
    rows = (
        ("Play", "Local couch", True),
        ("Practice", "Free run any arena, no tagger", False),
        ("Options", "Sound, picture, access", False),
        ("Controls", "Binds. Space still jumps.", False),
    )
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
    p31.one_focus(img, play, records)
    if MAIN_CLIMB.find("against a wall") >= 0 or MAIN_CLIMB.find("Hold [WASD]") >= 0:
        raise SystemExit("old climb tip")
    return img, checks, ratios


def finish(img, name, ratios):
    if min(ratios) < 4.5:
        raise SystemExit("contrast %.2f" % min(ratios))
    p27.layout_report(name)
    os.makedirs(ui.OUT, exist_ok=True)
    path = os.path.join(ui.OUT, name)
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
            print("pngquant", quality, qsize)
            if qsize <= 399000:
                os.replace(trial, path)
                size = qsize
                break
            os.remove(trial)
    if size > 400000:
        raise SystemExit("size %d" % size)
    print(name, size)
    return path


def scene_join(cells):
    """Drop-in cards. Same Hier idle on every seat, waiting banner unchanged."""
    p27.reset_layout()
    img = p28.darken(0.28)
    d = ImageDraw.Draw(img)
    checks, ratios = [], []
    header = (80, 24, 900, 100)
    ui.rounded(d, header, 12, NAVY)
    p27.ROWS.append(header)
    p27.text(d, (100, 36), "Who's playing", ui.font(ui.FONT_D, 44), GOLD, NAVY, header, checks, ratios, home=header)
    banner_line = "Waiting for 1 player to ready up"
    banner = (940, 28, 1840, 96)
    ui.rounded(d, banner, 12, NAVY)
    p27.ROWS.append(banner)
    p27.text(d, (banner[0] + 24, 44), banner_line, ui.font(ui.FONT_B, 26), GOLD, NAVY, banner, checks, ratios, home=banner)
    cards = (
        (True, "Red", True, "Keyboard"),
        (True, "Blue", False, "Gamepad"),
        (False, "", False, ""),
        (False, "", False, ""),
    )
    card_w, card_h, gap, top = 420, 760, 24, 140
    left = (W - (card_w * 4 + gap * 3)) // 2
    rects = []
    for i, (human, profile, ready, device) in enumerate(cards):
        x = left + i * (card_w + gap)
        box = (x, top, x + card_w, top + card_h)
        fill = ui.mix(INK, BAND[i], 0.40)
        pad = 16 if i == 0 else 5
        frame = (x - pad, top - pad, x + card_w + pad, top + card_h + pad)
        ui.rounded(d, frame, 22, GOLD if i == 0 else p28.STROKE)
        ui.rounded(d, box, 18, fill)
        p27.ROWS.append(box)
        d.rectangle((x + 16, top + 18, x + 28, top + card_h - 18), fill=BAND[i])
        name_home = (x + 44, top + 16, x + 250, top + 78)
        p27.text(d, (x + 48, top + 22), "P%d" % (i + 1), ui.font(ui.FONT_D, 40), CREAM, fill, name_home, checks, ratios, home=name_home)
        well = (x + card_w - 104, top + 18, x + card_w - 16, top + 106)
        ui.rounded(d, well, 10, WELL)
        p27.stamp(d, (well[0] + 12, well[1] + 12, well[2] - 12, well[3] - 12), i)
        ratios.append(ui.contrast(FILL[i], WELL))
        side = 250
        fig = (x + (card_w - side) // 2, top + 130, x + (card_w - side) // 2 + side, top + 130 + side)
        pasted = cells[i].resize((side, side), Image.Resampling.LANCZOS)
        img.alpha_composite(pasted, (fig[0], fig[1]))
        rects.append(fig)
        d = ImageDraw.Draw(img)
        if human:
            who = "<  %s  >" % profile
            who_home = (x + 40, top + 400, x + card_w - 40, top + 450)
            p27.text(d, (x + 70, top + 408), who, ui.font(ui.FONT_B, 28), CREAM, fill, who_home, checks, ratios, home=who_home)
            dev_home = (x + 80, top + 470, x + card_w - 40, top + 516)
            p27.text(d, (x + 120, top + 476), device, ui.font(ui.FONT_B, 26), CREAM, fill, dev_home, checks, ratios, home=dev_home)
            chip = (x + 120, top + 640, x + 300, top + 700)
            chip_fill = GOLD if ready else NAVY
            ink = INK if ready else CREAM
            ui.rounded(d, chip, 12, chip_fill)
            word = "Ready" if ready else "Joined"
            p27.text(d, (x + 150, top + 652), word, ui.font(ui.FONT_D, 32), ink, chip_fill, chip, checks, ratios, home=chip)
        else:
            prompt = "Press Space or A to join"
            face = ui.font(ui.FONT_B, 22)
            tw = d.textlength(prompt, font=face)
            home = (x + 16, top + 420, x + card_w - 16, top + 470)
            p27.text(d, (x + (card_w - tw) / 2, top + 430), prompt, face, p28.MUTE, fill, home, checks, ratios, home=home)
        if fig[3] > (top + 400 if human else top + 410):
            raise SystemExit("figure covers the label")
    if len(rects) != 4:
        raise SystemExit("join seats")
    heights = [r[3] - r[1] for r in rects]
    if max(heights) - min(heights) > 1:
        raise SystemExit("join scale")
    return img, checks, ratios


def main():
    cells = pack_atlas()
    img, checks, ratios, plates, shapes, tracks, rects, _header = scene_load(cells)
    figure_gate(img, rects)
    worst = p27.verify(img, checks, "32-load")
    print("text", round(worst, 2), "contrast", round(min(ratios), 2))
    path = finish(img, "32-load.png", ratios)
    probe(path, plates, shapes, tracks, rects)
    img, checks, ratios = scene_main()
    worst = p27.verify(img, checks, "32-main")
    print("main text", round(worst, 2), "contrast", round(min(ratios), 2))
    finish(img, "32-main.png", ratios)
    img, checks, ratios = scene_join(cells)
    worst = p27.verify(img, checks, "32-join")
    print("join text", round(worst, 2), "contrast", round(min(ratios), 2))
    finish(img, "32-join.png", ratios)


if __name__ == "__main__":
    main()
