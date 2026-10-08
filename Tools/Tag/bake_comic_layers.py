"""Bake the 33-word comic atlas and the separate burst atlas.

Words and bursts are different textures so the word can scale after the burst.
Tracking is the tightened advance in render_comic_sheet. Bangers is OFL.
"""
import base64
import os
import sys

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(__file__))
import render_comic_sheet as sheet

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CELL = 512
WORD_COLS = 6
WORD_ROWS = 6
BURST_COLS = 5
BURST_ROWS = 2
# Star spikes stay inside this fraction of the cell. The word is 1.17x that ink.
STAR_FRAC = 0.74
WORD_TIMES = 1.17

# Atlas order. The first four stay POP POW BAM WHAM so the old four-cell blit still matches.
WORDS = (
    ("POP!", 0), ("POW!", 0), ("BAM!", 0), ("WHAM!", 1),
    ("SMACK!", 0), ("WHACK!", 0), ("THWACK!", 0),
    ("BONK!", 1), ("KAPOW!", 1),
    ("TAG!", 2), ("GOTCHA!", 2), ("MINE!", 2),
    ("WHIFF!", 3), ("SWISH!", 3), ("WHOOSH!", 3),
    ("THUD!", 4), ("WHUMP!", 4), ("THUMP!", 4),
    ("BOING!", 5), ("SPROING!", 5), ("POING!", 5),
    ("ZING!", 6), ("ZIP!", 6), ("WHIZZ!", 6),
    ("THWIP!", 7), ("FWIP!", 7), ("ZWIP!", 7),
    ("KRAK!", 8), ("FWOOSH!", 8), ("THOK!", 8),
    ("OOF!", 9), ("UGH!", 9), ("OUCH!", 9),
)


def event_fill(ev):
    return sheet.EVENTS[ev][2]


def event_color(ev):
    return sheet.EVENTS[ev][1]


def star_radius():
    spike = STAR_FRAC * CELL * 0.5
    return spike / (max(sheet.OUTERS) * sheet.OUTLINE)


def measure_star(radius):
    side = CELL
    im = sheet.burst(side, side, (255, 118, 28), radius)
    box = sheet.opaque_bounds(im)
    return im, box


def fit_word(word, fill, target_w):
    glyph = sheet.render_after(word, target_w, fill)
    span = sheet.fill_span(glyph, fill)
    if span is None:
        return glyph
    # Keep a few pixels of cell margin. Shrink only if the ink would clip.
    limit = CELL - 16
    ink_w = span[1] - span[0] + 1
    ink_h = span[3] - span[2] + 1
    scale = 1.0
    if glyph.width > limit or glyph.height > limit or ink_w > limit or ink_h > limit:
        scale = min(limit / float(glyph.width), limit / float(glyph.height))
    if scale < 0.999:
        glyph = glyph.resize((max(1, int(glyph.width * scale)), max(1, int(glyph.height * scale))), Image.Resampling.LANCZOS)
    plate = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    x = (CELL - glyph.width) // 2
    y = (CELL - glyph.height) // 2
    plate.alpha_composite(glyph, (x, y))
    return plate


def pack(cells, cols, rows):
    sheet_im = Image.new("RGBA", (cols * CELL, rows * CELL), (0, 0, 0, 0))
    for i, im in enumerate(cells):
        col = i % cols
        row = i // cols
        sheet_im.paste(im, (col * CELL, row * CELL), im)
    return sheet_im


def write_cs(path, class_name, summary, cells, cols, rows, png_bytes):
    b64 = base64.b64encode(png_bytes).decode("ascii")
    lines = [
        "namespace Tag.FX",
        "{",
        "    /// <summary>",
        "    /// %s" % summary,
        "    /// </summary>",
        "    public static class %s" % class_name,
        "    {",
        "        public const int Cells = %d;" % cells,
        "        public const int Columns = %d;" % cols,
        "        public const int Rows = %d;" % rows,
        "        public const int CellWidth = %d;" % CELL,
        "        public const int CellHeight = %d;" % CELL,
        "",
        "        public static void Uv(int index, out float scaleX, out float scaleY, out float offX, out float offY)",
        "        {",
        "            if (index < 0) index = 0;",
        "            int col = index % Columns;",
        "            int row = index / Columns;",
        "            float du = 1f / Columns;",
        "            float dv = 1f / Rows;",
        "            float g = 1f / (Columns * CellWidth);",
        "            scaleX = du - g * 2f;",
        "            scaleY = dv - g * 2f;",
        "            offX = col * du + g;",
        "            offY = 1f - (row + 1f) * dv + g;",
        "        }",
        "",
        "        public static byte[] Png()",
        "        {",
        "            return System.Convert.FromBase64String(Data);",
        "        }",
        "",
        "        const string Data =",
    ]
    for i in range(0, len(b64), 120):
        lines.append('            "' + b64[i:i + 120] + '" +')
    lines[-1] = lines[-1][:-2] + ";"
    lines += ["    }", "}", ""]
    with open(path, "w") as f:
        f.write("\n".join(lines))
    print("CS", os.path.basename(path), len(png_bytes), "bytes png", cells, "cells")


def png_bytes(im):
    import io
    buf = io.BytesIO()
    im.save(buf, format="PNG")
    return buf.getvalue()


def save_limited(im, path, limit=400 * 1024):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.convert("RGB").save(path, optimize=True)
    if os.path.getsize(path) <= limit:
        print("SIZE", os.path.basename(path), os.path.getsize(path), im.size)
        return
    rgb = im.convert("RGB")
    for colors in (192, 160, 128, 96):
        q = rgb.quantize(colors=colors, method=Image.Quantize.MEDIANCUT)
        q.save(path, optimize=True)
        if os.path.getsize(path) <= limit:
            print("SIZE", os.path.basename(path), os.path.getsize(path), "colors", colors)
            return
    w, h = rgb.size
    while w > 800 and os.path.getsize(path) > limit:
        w = int(w * 0.92)
        h = int(h * 0.92)
        rgb = rgb.resize((w, h), Image.Resampling.LANCZOS)
        rgb.save(path, optimize=True)
    print("SIZE", os.path.basename(path), os.path.getsize(path), "scaled", rgb.size)


def compose_pair(word_im, burst_im):
    out = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    out.alpha_composite(burst_im)
    out.alpha_composite(word_im)
    return out


def clamp_center(cx, cy, half_w, half_h, pane):
    x0, y0, x1, y1 = pane
    min_x = x0 + half_w
    max_x = x1 - half_w
    min_y = y0 + half_h
    max_y = y1 - half_h
    if min_x > max_x:
        cx = (x0 + x1) * 0.5
    else:
        cx = min(max(cx, min_x), max_x)
    if min_y > max_y:
        cy = (y0 + y1) * 0.5
    else:
        cy = min(max(cy, min_y), max_y)
    return cx, cy


def ink_box(im):
    bb = im.getchannel("A").getbbox()
    if bb is None:
        return None
    return bb


def main():
    radius = star_radius()
    _probe, star_box = measure_star(radius)
    outer_w = (star_box[1] - star_box[0] + 1) if star_box else CELL * STAR_FRAC
    target = outer_w * WORD_TIMES
    print("RADIUS", round(radius, 2), "STAR", round(outer_w, 1), "WORD", round(target, 1))

    bursts = []
    for ev in range(10):
        bursts.append(sheet.burst(CELL, CELL, event_color(ev), radius))

    words = []
    for text, ev in WORDS:
        words.append(fit_word(text, event_fill(ev), target))

    # Overflow: word fill wider than the star, and inside the cell.
    failed = 0
    for (text, ev), word_im in zip(WORDS, words):
        fill = event_fill(ev)
        ink = sheet.fill_span(word_im, fill)
        if ink is None or star_box is None:
            print("MISSING", text)
            failed += 1
            continue
        word_w = ink[1] - ink[0] + 1
        past = ink[0] < star_box[0] - 1 and ink[1] > star_box[1] + 1
        inside = ink[0] > 2 and ink[1] < CELL - 3 and ink[2] > 2 and ink[3] < CELL - 3
        if not past or not inside:
            failed += 1
            print("CELL", text, "word", word_w, "star", outer_w, "past", past, "inside", inside, ink)
    print("CELLS", len(words), "failed", failed)
    if failed:
        return 1

    word_atlas = pack(words, WORD_COLS, WORD_ROWS)
    burst_atlas = pack(bursts, BURST_COLS, BURST_ROWS)
    art = os.path.join(ROOT, "Assets", "Art", "FX")
    word_png = png_bytes(word_atlas)
    burst_png = png_bytes(burst_atlas)
    word_atlas.save(os.path.join(art, "ComicAtlas.png"))
    burst_atlas.save(os.path.join(art, "ComicBurstAtlas.png"))
    write_cs(
        os.path.join(ROOT, "Assets", "Scripts", "FX", "ComicAtlas.cs"),
        "ComicAtlas",
        "Bangers comic words. Built by Tools/Tag/bake_comic_layers.py. Burst is separate.",
        len(words), WORD_COLS, WORD_ROWS, word_png)
    write_cs(
        os.path.join(ROOT, "Assets", "Scripts", "FX", "ComicBurstAtlas.cs"),
        "ComicBurstAtlas",
        "Comic burst layer, one cell per event. Built by Tools/Tag/bake_comic_layers.py.",
        10, BURST_COLS, BURST_ROWS, burst_png)

    out_dir = os.path.join(ROOT, "Docs", "FxStills", "pass13")
    label = ImageFont.truetype(sheet.FONT_PATH, 22)
    small = ImageFont.truetype(sheet.FONT_PATH, 16)

    # All 33, settled, word in front of its burst.
    cols = 6
    cell = (180, 150)
    gap = 6
    rows = 6
    head = 36
    board_w = cols * cell[0] + (cols + 1) * gap
    board_h = head + rows * cell[1] + (rows + 1) * gap
    board = Image.new("RGB", (board_w, board_h), (22, 20, 18))
    draw = ImageDraw.Draw(board)
    draw.text((12, 8), "In game    word in front    tracking tightened", font=label, fill=(255, 220, 120))
    for i, ((text, ev), word_im) in enumerate(zip(WORDS, words)):
        pair = compose_pair(word_im, bursts[ev])
        col = i % cols
        row = i // cols
        x = gap + col * (cell[0] + gap)
        y = head + gap + row * (cell[1] + gap)
        thumb = pair.resize(cell, Image.Resampling.LANCZOS)
        bg = Image.new("RGB", cell, (28, 26, 24))
        bg.paste(thumb, mask=thumb.getchannel("A"))
        board.paste(bg, (x, y))
        draw.text((x + 4, y + 2), text, font=small, fill=(255, 244, 220))
    save_limited(board, os.path.join(out_dir, "comic-words.png"))

    # Pop: burst alone, then word at 0.55, 1.15, 1.0. POW is atlas 1, event 0.
    pop = Image.new("RGB", (860, 230), (22, 20, 18))
    pd = ImageDraw.Draw(pop)
    pd.text((12, 8), "POW!    burst in, then the word", font=label, fill=(255, 220, 120))
    scales = (0.0, 0.55, 1.15, 1.0)
    titles = ("burst in", "word 0.55", "overshoot 1.15", "settle 1.0")
    pow_word = words[1]
    pow_burst = bursts[0]
    for i, (sc, title) in enumerate(zip(scales, titles)):
        frame = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
        frame.alpha_composite(pow_burst)
        if sc > 0.02:
            sized = pow_word.resize((max(1, int(CELL * sc)), max(1, int(CELL * sc))), Image.Resampling.LANCZOS)
            frame.alpha_composite(sized, ((CELL - sized.width) // 2, (CELL - sized.height) // 2))
        thumb = frame.resize((180, 180), Image.Resampling.LANCZOS)
        bg = Image.new("RGB", (180, 180), (28, 26, 24))
        bg.paste(thumb, mask=thumb.getchannel("A"))
        x = 16 + i * 210
        pop.paste(bg, (x, 40))
        pd.text((x, 224), title, font=small, fill=(255, 236, 200))
    save_limited(pop, os.path.join(out_dir, "comic-pop.png"))

    # 4-up panes. Each word is inset so the ink stays off the pane edge.
    quads = (
        (6, "THWACK!", "PUNCH"),
        (8, "KAPOW!", "TAG"),
        (19, "SPROING!", "LAUNCH"),
        (10, "GOTCHA!", "TRANSFER"),
    )
    pane = 320
    pad = 28
    four = Image.new("RGB", (pane * 2, pane * 2 + 28), (18, 16, 14))
    fd = ImageDraw.Draw(four)
    fd.text((8, 4), "Split-screen 4-up    word inside the pane", font=small, fill=(255, 220, 120))
    edge_fail = 0
    for i, (idx, text, name) in enumerate(quads):
        ev = WORDS[idx][1]
        pair = compose_pair(words[idx], bursts[ev])
        inner = pane - pad * 2
        thumb = pair.resize((inner, inner), Image.Resampling.LANCZOS)
        x = (i % 2) * pane
        y = 28 + (i // 2) * pane
        bg = Image.new("RGB", (pane, pane), (28, 26, 24))
        bg.paste(thumb, (pad, pad), thumb.getchannel("A"))
        four.paste(bg, (x, y))
        fd.rectangle((x, y, x + pane - 1, y + pane - 1), outline=(255, 210, 80))
        fd.text((x + 8, y + 6), "%s  %s" % (name, text), font=small, fill=(255, 244, 210))
        # Ink must stay inside the pane, clear of the gold edge.
        crop = four.crop((x + 2, y + 2, x + pane - 2, y + pane - 2))
        bb = crop.getchannel("A") if crop.mode == "RGBA" else None
        # Measure non-background pixels against the pane.
        px = four.load()
        xs = []
        ys = []
        # Skip the caption band. The word itself has to clear the pane edge.
        for yy in range(y + 24, y + pane - 1):
            for xx in range(x + 1, x + pane - 1):
                r, g, b = px[xx, yy]
                if r < 40 and g < 40 and b < 40:
                    continue
                if abs(r - 28) < 6 and abs(g - 26) < 6:
                    continue
                xs.append(xx)
                ys.append(yy)
        if not xs:
            edge_fail += 1
            print("FOUR empty", text)
            continue
        mL = min(xs) - x
        mR = x + pane - 1 - max(xs)
        mT = min(ys) - y
        mB = y + pane - 1 - max(ys)
        print("FOUR", text, "margin", mL, mR, mT, mB)
        if mL < 8 or mR < 8 or mT < 8 or mB < 8:
            edge_fail += 1
    save_limited(four, os.path.join(out_dir, "comic-4up.png"))

    # Pane corner: the contact would sit on the corner. The clamp shifts it in.
    corner = Image.new("RGB", (pane * 2, pane * 2 + 28), (16, 14, 12))
    cd = ImageDraw.Draw(corner)
    cd.text((8, 4), "Pane corner    THWACK! shifted inside", font=small, fill=(255, 220, 120))
    for i in range(4):
        x = (i % 2) * pane
        y = 28 + (i // 2) * pane
        cd.rectangle((x, y, x + pane - 1, y + pane - 1), outline=(255, 210, 80))
    # Top-left pane. Natural center is the top-right corner of that pane.
    idx = 6
    ev = WORDS[idx][1]
    pair = compose_pair(words[idx], bursts[ev])
    shown = pair.resize((150, 150), Image.Resampling.LANCZOS)
    half_w = shown.width * 0.5
    half_h = shown.height * 0.5
    # Extra inset past the quad, matching the shader's NDC sliver.
    inset = 10
    pane_box = (inset, 28 + inset, pane - inset, 28 + pane - inset)
    natural = (pane - 4, 28 + 4)
    cx, cy = clamp_center(natural[0], natural[1], half_w, half_h, pane_box)
    paste_x = int(round(cx - half_w))
    paste_y = int(round(cy - half_h))
    corner.paste(shown, (paste_x, paste_y), shown.getchannel("A"))
    print("CORNER natural", natural, "clamped", round(cx, 1), round(cy, 1), "paste", paste_x, paste_y)
    # Confirm ink is inside the top-left pane.
    px = corner.load()
    xs = []
    ys = []
    for yy in range(28, 28 + pane):
        for xx in range(0, pane):
            r, g, b = px[xx, yy]
            # THWACK fill is yellow. The pane border is gold and is not the word.
            if r > 230 and g > 220 and b < 120:
                xs.append(xx)
                ys.append(yy)
    if xs:
        print("CORNER ink", min(xs), max(xs), min(ys), max(ys), "pane", 0, 28, pane, 28 + pane)
        if min(xs) < 8 or max(xs) > pane - 8 or min(ys) < 28 + 8 or max(ys) > 28 + pane - 8:
            print("CORNER CLIP")
            return 1
    else:
        print("CORNER no ink")
        return 1
    save_limited(corner, os.path.join(out_dir, "comic-corner.png"))
    if edge_fail:
        print("EDGE", edge_fail)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
