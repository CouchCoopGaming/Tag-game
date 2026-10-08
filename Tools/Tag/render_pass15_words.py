"""Pass 15 comic sheets. Each word keeps its colour and gets its own tilt.

The tilt, skew, size, and arc match ComicWords.StyleOf. Contact stays bold.
Whiff is thin and streaky, land is low and wide, crash is split.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(__file__))
import bake_comic_layers as bake
import render_comic_sheet as comic_sheet

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Docs", "FxStills", "pass22")
FONT = comic_sheet.FONT_PATH


def category_of(i):
    if i in (6, 27, 28, 29):
        return "crash"
    if i in (15, 16, 17, 33, 34):
        return "land"
    if i in (12, 13, 14, 35) or 18 <= i <= 26:
        return "whiff"
    return "contact"


def style_of(i):
    mag = 8 + ((i * 17) % 18)
    sign = 1 if (i & 1) == 0 else -1
    tilt = sign * mag
    skew = ((i % 5) - 2) * 0.09
    size = 0.88 + (i % 7) * 0.04
    arc = ((i % 3) - 1) * 0.12
    cat = category_of(i)
    if cat == "whiff":
        wide, tall = 1.14, 0.82
        skew += 0.10 * sign
    elif cat == "land":
        wide, tall = 1.28, 0.74
    elif cat == "crash":
        wide, tall = 1.06, 1.08
        skew += 0.06 * -sign
    else:
        wide, tall = 1.0, 1.0
    return tilt, skew, size, arc, wide, tall


def shape(glyph, tilt, skew, size, arc, wide, tall):
    w, h = glyph.size
    skewed = glyph.transform(
        (w + int(abs(skew) * h) + 8, h),
        Image.Transform.AFFINE,
        (1, -skew, 0 if skew > 0 else skew * h, 0, 1, 0),
        resample=Image.Resampling.BICUBIC,
    )
    # Quadratic bow, sampled from the destination so the arc does not open holes.
    sw, sh = skewed.size
    bowed = Image.new("RGBA", (sw, sh + 48), (0, 0, 0, 0))
    src = skewed.load()
    dst = bowed.load()
    for y in range(bowed.height):
        for x in range(sw):
            nx = x / float(sw) - 0.5
            dy = nx * nx * arc * sh * 2.2
            sy = y - 24 + dy
            iy = int(round(sy))
            if 0 <= iy < sh:
                p = src[x, iy]
                if p[3] >= 8:
                    dst[x, y] = p
    nw = max(1, int(bowed.width * size * wide))
    nh = max(1, int(bowed.height * size * tall))
    bowed = bowed.resize((nw, nh), Image.Resampling.LANCZOS)
    if abs(tilt) > 0.05:
        bowed = bowed.rotate(tilt, expand=True, resample=Image.Resampling.BICUBIC)
    return bowed


# Atlas cell to the burst event. Same pools as ComicWords.
ATLAS_EVENT = {}
for _ev, _ids in (
    (0, (0, 1, 4, 5, 2)),
    (1, (3, 7, 8)),
    (2, (9, 10, 11)),
    (3, (12, 13, 14, 35)),
    (4, (15, 16, 17, 33, 34)),
    (5, (18, 19, 20)),
    (6, (21, 22, 23)),
    (7, (24, 25, 26)),
    (8, (27, 28, 29, 6)),
    (9, (30, 31, 32)),
):
    for _id in _ids:
        ATLAS_EVENT[_id] = _ev


def cell_of(index, box):
    text, kind, fill = bake.WORDS[index]
    word = bake.fit_word(text, fill, 280, kind)
    ev = ATLAS_EVENT.get(index, 0)
    color = comic_sheet.EVENTS[ev][1]
    burst = comic_sheet.burst(bake.CELL, bake.CELL, color, bake.star_radius())
    comic_sheet.knockout_dots(burst, word, color, pad=11)
    # Burst behind, word in front, then one tilt so they stay a pair.
    pair = Image.new("RGBA", (bake.CELL, bake.CELL), (0, 0, 0, 0))
    pair.alpha_composite(burst)
    pair.alpha_composite(word)
    bb = pair.getchannel("A").getbbox()
    if bb:
        pair = pair.crop(bb)
    tilt, skew, size, arc, wide, tall = style_of(index)
    posed = shape(pair, tilt, skew, size, arc, wide, tall)
    bg = Image.new("RGB", box, (32, 30, 28))
    scale = min((box[0] - 24) / float(posed.width), (box[1] - 36) / float(posed.height))
    if scale < 1:
        posed = posed.resize((max(1, int(posed.width * scale)), max(1, int(posed.height * scale))), Image.Resampling.LANCZOS)
    x = (box[0] - posed.width) // 2
    y = (box[1] - posed.height) // 2
    bg.paste(posed, (x, y), posed)
    return bg, text, tilt


def save_jpeg(im, name):
    path = os.path.join(OUT, name)
    os.makedirs(OUT, exist_ok=True)
    rgb = im.convert("RGB")
    for quality in (85, 75, 65, 55):
        rgb.save(path, format="JPEG", quality=quality, optimize=True)
        if os.path.getsize(path) <= 400 * 1024:
            print("SIZE", name, os.path.getsize(path), "q", quality, rgb.size)
            return
    print("SIZE", name, os.path.getsize(path), "over")


def sheet(title, indices, cols):
    font = ImageFont.truetype(FONT, 22)
    small = ImageFont.truetype(FONT, 16)
    box = (360, 280)
    gap = 8
    head = 40
    foot = 28
    rows = (len(indices) + cols - 1) // cols
    w = cols * box[0] + gap * (cols + 1)
    h = head + rows * (box[1] + foot) + gap * rows
    board = Image.new("RGB", (w, h), (22, 20, 18))
    draw = ImageDraw.Draw(board)
    draw.text((12, 8), title, font=font, fill=(255, 220, 120))
    for n, index in enumerate(indices):
        panel, text, tilt = cell_of(index, box)
        col = n % cols
        row = n // cols
        x = gap + col * (box[0] + gap)
        y = head + row * (box[1] + foot + gap)
        board.paste(panel, (x, y))
        draw.text((x + 8, y + box[1] + 4), "%s   %d deg" % (text, tilt), font=small, fill=(255, 244, 220))
    return board


def life_scale(age):
    """Matches ComicWords.Scale. Overshoot by 0.05 s, then shrink to nothing by 0.45 s."""
    pop = 0.05
    life = 0.45
    if age <= 0.0 or age >= life:
        return 0.0
    if age < pop:
        u = age / pop
        peak_at = 0.58
        peak = 1.25
        if u < peak_at:
            t = u / peak_at
            e = t * t * (3.0 - 2.0 * t)
            return peak * e
        settle = (u - peak_at) / (1.0 - peak_at)
        down = settle * settle * (3.0 - 2.0 * settle)
        return peak + (1.0 - peak) * down
    v = (age - pop) / (life - pop)
    ease = v * v * (3.0 - 2.0 * v)
    return 1.0 - ease


def word_punch(age):
    """Word and burst share Scale. The word overshoots a little more, then matches."""
    pop = 0.05
    peak_at = pop * 0.58
    extra = 0.18
    if age <= 0.0 or age >= pop:
        return 1.0
    if age < peak_at:
        t = age / peak_at
        e = t * t * (3.0 - 2.0 * t)
        return 1.0 + extra * e
    settle = (age - peak_at) / (pop - peak_at)
    down = settle * settle * (3.0 - 2.0 * settle)
    return 1.0 + extra * (1.0 - down)


def word_scale(age):
    start = 0.05
    span = 0.08
    if age <= start:
        return 0.0
    u = (age - start) / span
    if u >= 1.0:
        return 1.0
    peak_at = 0.55
    peak = 1.15
    if u < peak_at:
        t = u / peak_at
        e = t * t * (3.0 - 2.0 * t)
        return peak * e
    settle = (u - peak_at) / (1.0 - peak_at)
    down = settle * settle * (3.0 - 2.0 * settle)
    return peak + (1.0 - peak) * down


def life_alpha(age):
    pop = 0.05
    hold = 0.28
    life = 0.45
    if age < 0.0 or age >= life:
        return 0.0
    if age < pop:
        return age / pop
    if age < pop + hold:
        return 1.0
    span = life - pop - hold
    u = (age - pop - hold) / span
    if u < 0.0:
        u = 0.0
    if u > 1.0:
        u = 1.0
    return 1.0 - u


def posed_layers(index):
    text, kind, fill = bake.WORDS[index]
    word = bake.fit_word(text, fill, 280, kind)
    ev = ATLAS_EVENT.get(index, 0)
    color = comic_sheet.EVENTS[ev][1]
    burst = comic_sheet.burst(bake.CELL, bake.CELL, color, bake.star_radius())
    comic_sheet.knockout_dots(burst, word, color, pad=11)
    tilt, skew, size, arc, wide, tall = style_of(index)
    burst = shape(burst, tilt, skew, size, arc, wide, tall)
    word = shape(word, tilt, skew, size, arc, wide, tall)
    return text, burst, word


def frame_at(burst, word, age, box):
    """One moment. Burst and word share the tilt. The word is in front."""
    bg = Image.new("RGBA", box, (32, 30, 28, 255))
    bs = life_scale(age)
    ws = bs * word_punch(age)
    a = life_alpha(age)
    if bs < 0.02 and ws < 0.02 or a <= 0.001:
        return bg.convert("RGB")
    canvas = Image.new("RGBA", box, (0, 0, 0, 0))
    limit = min(box[0] - 16, box[1] - 16)

    def place(glyph, sc):
        if sc < 0.02:
            return
        nw = max(1, int(glyph.width * sc))
        nh = max(1, int(glyph.height * sc))
        fitted = min(limit / float(nw), limit / float(nh), 1.0)
        nw = max(1, int(nw * fitted))
        nh = max(1, int(nh * fitted))
        layer = glyph.resize((nw, nh), Image.Resampling.LANCZOS)
        if a < 0.999:
            band = layer.getchannel("A").point(lambda p: int(p * a))
            layer.putalpha(band)
        canvas.alpha_composite(layer, ((box[0] - nw) // 2, (box[1] - nh) // 2))

    # Fit the full-size pair into the panel, then apply the life scale on top.
    full = max(burst.width, word.width, 1)
    base = limit / float(full)
    place(burst, bs * base)
    place(word, ws * base)
    bg.alpha_composite(canvas)
    return bg.convert("RGB")


def life_strip():
    index = index_of("POW!")
    text, burst, word = posed_layers(index)
    ages = (0.00, 0.03, 0.09, 0.18, 0.33, 0.45)
    labels = ("0.00 born", "0.03 past full", "0.09 together", "0.18 shrinking", "0.33 smaller", "0.45 gone")
    font = ImageFont.truetype(FONT, 22)
    small = ImageFont.truetype(FONT, 16)
    box = (220, 220)
    gap = 8
    head = 40
    foot = 28
    w = len(ages) * box[0] + gap * (len(ages) + 1)
    h = head + box[1] + foot + gap
    board = Image.new("RGB", (w, h), (22, 20, 18))
    draw = ImageDraw.Draw(board)
    draw.text((12, 8), "POW!    word and burst born together    then gone by 0.45 s", font=font, fill=(255, 220, 120))
    for n, (age, label) in enumerate(zip(ages, labels)):
        panel = frame_at(burst, word, age, box)
        x = gap + n * (box[0] + gap)
        y = head
        board.paste(panel, (x, y))
        draw.text((x + 6, y + box[1] + 4), label, font=small, fill=(255, 244, 220))
    return board


def rel_lum(rgb):
    def lin(c):
        c = c / 255.0
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    r, g, b = (lin(c) for c in rgb)
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def wcag(word_fill, burst_fill):
    hi = max(rel_lum(word_fill), rel_lum(burst_fill))
    lo = min(rel_lum(word_fill), rel_lum(burst_fill))
    return (hi + 0.05) / (lo + 0.05)


def stroke_colors(fill):
    """Outermost stroke, then the stroke that touches the fill."""
    black = (0, 0, 0)
    return black, black


def contrast_report():
    """Outermost stroke against the burst, and the fill against its stroke."""
    stroke_worst = None
    stroke_name = ""
    fill_worst = None
    fill_name = ""
    failed = 0
    for i, (text, _kind, fill) in enumerate(bake.WORDS):
        burst = comic_sheet.EVENTS[ATLAS_EVENT[i]][1]
        outer, inner = stroke_colors(fill)
        stroke_score = wcag(outer, burst)
        fill_score = wcag(fill, inner)
        if stroke_worst is None or stroke_score < stroke_worst:
            stroke_worst = stroke_score
            stroke_name = text
        if fill_worst is None or fill_score < fill_worst:
            fill_worst = fill_score
            fill_name = text
        if stroke_score < 3.0 or fill_score < 3.0:
            failed += 1
            print("CONTRAST FAIL", text, "stroke-burst %.2f" % stroke_score, "fill-stroke %.2f" % fill_score)
    print("stroke-burst min=%.2f worst=%s" % (stroke_worst, stroke_name))
    print("fill-stroke min=%.2f worst=%s" % (fill_worst, fill_name))
    return failed


def live_pair(index, age, width):
    """Burst behind the word, both at the shared life scale, game tilt already applied."""
    _text, burst, word = posed_layers(index)
    bs = life_scale(age)
    ws = bs * word_punch(age)
    alpha = life_alpha(age)
    full = max(burst.width, word.width, 1)
    base = width / float(full)

    def layer(glyph, scale):
        nw = max(1, int(glyph.width * scale * base))
        nh = max(1, int(glyph.height * scale * base))
        im = glyph.resize((nw, nh), Image.Resampling.LANCZOS)
        if alpha < 0.999:
            im.putalpha(im.getchannel("A").point(lambda p: int(p * alpha)))
        return im

    back = layer(burst, bs)
    front = layer(word, ws)
    w = max(back.width, front.width)
    h = max(back.height, front.height)
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    canvas.alpha_composite(back, ((w - back.width) // 2, (h - back.height) // 2))
    canvas.alpha_composite(front, ((w - front.width) // 2, (h - front.height) // 2))
    return canvas


def read_runners():
    path = os.path.join(ROOT, "Docs", "FxStills", "pass20", "runners", "layout.txt")
    found = {}
    with open(path) as handle:
        for line in handle:
            parts = line.split()
            if not parts or parts[0] != "RUNNER":
                continue
            found[parts[1]] = {
                "foot": (float(parts[3]), float(parts[4])),
                "chest": (float(parts[6]), float(parts[7])),
                "head": (float(parts[9]), float(parts[10])),
                "body": float(parts[12]),
            }
    return found


def jagged_tail(draw, hit, burst_edge, color=(12, 10, 8)):
    """Short speed tail from the hit up toward the burst."""
    x0, y0 = hit
    x1, y1 = burst_edge
    stations = 6
    pts = []
    for i in range(stations):
        t = i / float(stations - 1)
        jag = (0.0, 7.0, -6.0, 5.0, -4.0, 0.0)[i]
        # Jag across the tail, in screen pixels.
        dx = x1 - x0
        dy = y1 - y0
        length = max((dx * dx + dy * dy) ** 0.5, 1.0)
        nx, ny = -dy / length, dx / length
        pts.append((x0 + dx * t + nx * jag, y0 + dy * t + ny * jag))
    draw.line(pts, fill=(255, 246, 220), width=9)
    draw.line(pts, fill=color, width=4)


def pane_fraction(life):
    """Visible burst-ink height as a fraction of the pane.

    Settle and the shrink stay on the 22% floor. The overshoot peak is 30%.
    The padded cell is larger than the spikes, so the fit uses the alpha box.
    """
    if life <= 1.0:
        return 0.22 * life
    u = (life - 1.0) / 0.25
    if u < 0.0:
        u = 0.0
    if u > 1.0:
        u = 1.0
    return 0.22 + (0.30 - 0.22) * u


def nudge_inside(cx, cy, width, height, pw, ph, margin=8.0):
    """Slide the burst back in so the glyph stays inside the pane."""
    dx = 0.0
    dy = 0.0
    left = cx - width * 0.5
    right = cx + width * 0.5
    top = cy - height * 0.5
    bot = cy + height * 0.5
    if left < margin:
        dx = margin - left
    if right > pw - margin:
        dx = (pw - margin) - right
    if top < margin:
        dy = margin - top
    if bot > ph - margin:
        dy = (ph - margin) - bot
    return cx + dx, cy + dy, dx, dy


def ink_box(glyph):
    bb = glyph.getchannel("A").point(lambda p: 255 if p > 16 else 0).getbbox()
    if bb is None:
        return 0, 0, glyph.width, glyph.height
    return bb


def couch_four():
    """1280x720. Arena plates, two runners, burst ink at the 30% peak."""
    names = ("SPROING!", "WHIZZ!", "POW!", "SMACK!")
    plates = (
        os.path.join(ROOT, "Docs", "ArenaStills", "MegaPark_Eye.png"),
        os.path.join(ROOT, "Docs", "ArenaStills", "MegaPark_Edge.png"),
        os.path.join(ROOT, "Docs", "FxStills", "pass21", "plates", "pocket-a.png"),
        os.path.join(ROOT, "Docs", "FxStills", "pass21", "plates", "pocket-b.png"),
    )
    # Two runners in the first two panes and the Pocket eye pane. The edge pane keeps both too.
    pair_panes = (True, True, True, True)
    runners = read_runners()
    attacker = Image.open(os.path.join(ROOT, "Docs", "FxStills", "pass20", "runners", "attacker.png")).convert("RGBA")
    victim = Image.open(os.path.join(ROOT, "Docs", "FxStills", "pass20", "runners", "victim.png")).convert("RGBA")
    board = Image.new("RGB", (1280, 720), (12, 12, 12))
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype(FONT, 22)
    meta = []
    pw, ph = 640, 360
    # Peak of the overshoot. Scale is 1.25, the word a little larger.
    age = 0.029
    life = life_scale(age)
    punch = word_punch(age)
    body = runners["attacker"]["body"]
    # Same offsets as ComicBurst: one body up, one body toward the open side.
    up_px = body * 1.0
    side_px = body * 1.0
    for n, text in enumerate(names):
        index = index_of(text)
        scene = Image.open(plates[n]).convert("RGBA").resize((pw, ph), Image.Resampling.LANCZOS)
        # Pair sits low in the pane. Even panes leave the open space on the right.
        pair_x = 230 if n % 2 == 0 else 400
        foot_y = 292
        gap = 52
        ax = int(round(pair_x - gap * 0.5 - runners["attacker"]["foot"][0]))
        ay = int(round(foot_y - runners["attacker"]["foot"][1]))
        vx = int(round(pair_x + gap * 0.5 - runners["victim"]["foot"][0]))
        vy = int(round(foot_y - runners["victim"]["foot"][1]))
        if pair_panes[n]:
            scene.alpha_composite(attacker, (ax, ay))
            scene.alpha_composite(victim, (vx, vy))
        hit_x = pair_x + (
            runners["attacker"]["chest"][0] - runners["attacker"]["foot"][0]
            + runners["victim"]["chest"][0] - runners["victim"]["foot"][0]
        ) * 0.5
        hit_y = foot_y + (
            runners["attacker"]["chest"][1] - runners["attacker"]["foot"][1]
            + runners["victim"]["chest"][1] - runners["victim"]["foot"][1]
        ) * 0.5
        # Open side is the half of the pane away from the pair.
        side = 1.0 if pair_x < pw * 0.5 else -1.0
        burst_cx = hit_x + side * side_px
        burst_cy = hit_y - up_px
        _text, burst, word = posed_layers(index)
        # Visible spike ink, not the padded cell. Peak life is 1.25, so this is 30%.
        burst_px = pane_fraction(life) * ph
        src_box = ink_box(burst)
        src_h = max(1, src_box[3] - src_box[1])
        scale = burst_px / float(src_h)

        def fit(glyph, extra):
            nw = max(1, int(round(glyph.width * scale * extra)))
            nh = max(1, int(round(glyph.height * scale * extra)))
            return glyph.resize((nw, nh), Image.Resampling.LANCZOS)

        back = fit(burst, 1.0)
        front = fit(word, punch)
        placed = ink_box(back)
        ink_w = placed[2] - placed[0]
        ink_h = placed[3] - placed[1]
        # Nudge the larger of the two glyphs so neither crosses the pane.
        span_w = max(front.width, back.width)
        span_h = max(front.height, back.height)
        burst_cx, burst_cy, nudge_x, nudge_y = nudge_inside(
            burst_cx, burst_cy, span_w, span_h, pw, ph,
        )
        # Tail stops at the near edge of the spikes so it points at the hit.
        edge_y = burst_cy - back.height * 0.5 + placed[3] - 4
        jagged_tail(ImageDraw.Draw(scene), (hit_x, hit_y), (burst_cx, edge_y))
        ref = scene.copy()
        back_xy = (int(round(burst_cx - back.width / 2)), int(round(burst_cy - back.height / 2)))
        front_xy = (int(round(burst_cx - front.width / 2)), int(round(burst_cy - front.height / 2)))
        scene.alpha_composite(back, back_xy)
        scene.alpha_composite(front, front_xy)
        ink_pane = (
            back_xy[0] + placed[0],
            back_xy[1] + placed[1],
            back_xy[0] + placed[2],
            back_xy[1] + placed[3],
        )
        ox = (n % 2) * pw
        oy = (n // 2) * ph
        board.paste(scene.convert("RGB"), (ox, oy))
        draw.text((ox + 16, oy + ph - 32), text, font=font, fill=(255, 244, 220))
        ev = ATLAS_EVENT.get(index, 0)
        color = comic_sheet.EVENTS[ev][1]
        meta.append({
            "text": text,
            "color": color,
            "ox": ox,
            "oy": oy,
            "ref": ref.convert("RGB"),
            "ink": ink_pane,
            "target": burst_px,
        })
        print(
            "COUCH", text,
            "life", round(life, 3),
            "punch", round(punch, 3),
            "ink", ink_w, ink_h,
            "frac", round(ink_h / float(ph), 3),
            "target", round(burst_px, 1),
            "body_px", round(body, 1),
            "up", round(up_px, 1),
            "side", round(side_px, 1),
            "nudge", round(nudge_x, 1), round(nudge_y, 1),
        )
    draw.line((640, 0, 640, 720), fill=(8, 8, 8), width=4)
    draw.line((0, 360, 1280, 360), fill=(8, 8, 8), width=4)
    return board, meta


def _near(mask, x, y, rad, w, h):
    x0 = max(0, x - rad)
    x1 = min(w - 1, x + rad)
    y0 = max(0, y - rad)
    y1 = min(h - 1, y + rad)
    for yy in range(y0, y1 + 1):
        row = mask[yy]
        for xx in range(x0, x1 + 1):
            if row[xx]:
                return True
    return False


def measure_saved_couch(meta):
    """Burst bounding box in the saved jpeg, one line per pane.

    The word is yellow and sits on the star. The box is the spike ink,
    including the black outline, and it has to land on 30% of the pane.
    """
    import io
    import numpy as np

    path = os.path.join(OUT, "comic-couch.jpg")
    saved = Image.open(path).convert("RGB")
    sw, sh = saved.size
    ref_board = Image.new("RGB", (sw, sh), (12, 12, 12))
    draw = ImageDraw.Draw(ref_board)
    font = ImageFont.truetype(FONT, 22)
    for row in meta:
        ref_board.paste(row["ref"], (row["ox"], row["oy"]))
        draw.text((row["ox"] + 16, row["oy"] + row["ref"].size[1] - 32), row["text"], font=font, fill=(255, 244, 220))
    draw.line((640, 0, 640, 720), fill=(8, 8, 8), width=4)
    draw.line((0, 360, 1280, 360), fill=(8, 8, 8), width=4)
    buf = io.BytesIO()
    ref_board.save(buf, format="JPEG", quality=85, optimize=True)
    buf.seek(0)
    ref_jpg = Image.open(buf).convert("RGB")
    saved_np = np.asarray(saved).astype(np.int16)
    ref_np = np.asarray(ref_jpg).astype(np.int16)
    fails = 0
    for row in meta:
        ox, oy = row["ox"], row["oy"]
        pw, ph = row["ref"].size
        color = np.array(row["color"], dtype=np.int16)
        dark = (color * 0.40).astype(np.int16)
        pane = saved_np[oy:oy + ph, ox:ox + pw]
        base = ref_np[oy:oy + ph, ox:ox + pw]
        delta = np.abs(pane - base).sum(axis=2)
        changed = delta > 36
        rgb = pane
        yellow = (
            (rgb[:, :, 0] > 190)
            & (rgb[:, :, 1] > 160)
            & (rgb[:, :, 2] < 150)
            & (rgb[:, :, 0] + rgb[:, :, 1] > rgb[:, :, 2] * 3)
        )
        fill = (np.abs(rgb - color).max(axis=2) < 42) & changed & ~yellow
        tone = (np.abs(rgb - dark).max(axis=2) < 34) & changed & ~yellow
        burstish = fill | tone
        # Black outline sits just outside the colour. Count it when it changed
        # and touches the colour, and skip the word's own stroke.
        dark_px = (rgb.max(axis=2) < 70) & changed & ~yellow
        ys_b, xs_b = np.where(burstish)
        if len(xs_b) == 0:
            print("COUCH-FILE", row["text"], "bbox NONE")
            fails += 1
            continue
        pad = 6
        outline = np.zeros_like(burstish)
        for y, x in zip(ys_b.tolist(), xs_b.tolist()):
            y0 = max(0, y - pad)
            y1 = min(ph, y + pad + 1)
            x0 = max(0, x - pad)
            x1 = min(pw, x + pad + 1)
            outline[y0:y1, x0:x1] = True
        outline &= dark_px
        # Drop outline pixels that only touch the yellow word.
        word = yellow
        keep = burstish | outline
        # A dark pixel beside yellow and not beside the fill is the word stroke.
        if word.any():
            near_word = np.zeros_like(word)
            wy, wx = np.where(word)
            for y, x in zip(wy.tolist(), wx.tolist()):
                y0 = max(0, y - 2)
                y1 = min(ph, y + 3)
                x0 = max(0, x - 2)
                x1 = min(pw, x + 3)
                near_word[y0:y1, x0:x1] = True
            keep &= ~((~burstish) & near_word & ~fill)
        ys, xs = np.where(keep)
        if len(xs) == 0:
            print("COUCH-FILE", row["text"], "bbox NONE")
            fails += 1
            continue
        x0, x1 = int(xs.min()), int(xs.max())
        y0, y1 = int(ys.min()), int(ys.max())
        bw = x1 - x0 + 1
        bh = y1 - y0 + 1
        frac = bh / float(ph)
        ink = row["ink"]
        print(
            "COUCH-FILE", row["text"],
            "bbox", x0, y0, x1, y1,
            "w", bw, "h", bh,
            "frac", round(frac, 3),
            "pane", ph,
            "file", sw, sh,
            "ink", ink[0], ink[1], ink[2], ink[3],
        )
        if frac < 0.29 or frac > 0.31:
            print("COUCH-FILE FAIL", row["text"], "frac", round(frac, 3))
            fails += 1
    return fails


def index_of(word):
    for i, (text, _kind, _fill) in enumerate(bake.WORDS):
        if text == word:
            return i
    raise KeyError(word)


def main():
    if contrast_report():
        return 1
    groups = (
        ("comic-contact.jpg", "Contact   punch or tag lands", ("POW!", "BAM!", "WHAM!", "SMACK!")),
        ("comic-whiff.jpg", "Whiff   missed punch or tag", ("WHIFF!", "SWISH!", "WHOOSH!", "MISS!")),
        ("comic-land.jpg", "Hard land   fall at the roll threshold", ("THUD!", "WHUMP!", "BOOM!", "KRUNCH!")),
        ("comic-crash.jpg", "Crash   wall or object at sprint", ("SLAM!", "KRAK!", "THWACK!", "SPLAT!")),
    )
    for name, title, words in groups:
        save_jpeg(sheet(title, [index_of(w) for w in words], 2), name)
    angles = ("POW!", "BAM!", "WHAM!", "ZIP!", "SWISH!", "THWACK!", "KRAK!", "WHUMP!")
    save_jpeg(sheet("Angle variety   each word has its own tilt", [index_of(w) for w in angles], 4), "comic-angles.jpg")
    save_jpeg(life_strip(), "comic-life.jpg")
    save_jpeg(sheet("Every word at full size", list(range(len(bake.WORDS))), 6), "comic-all.jpg")
    board, meta = couch_four()
    save_jpeg(board, "comic-couch.jpg")
    if measure_saved_couch(meta):
        return 1
    prev = None
    for i, (text, _k, _f) in enumerate(bake.WORDS):
        tilt = style_of(i)[0]
        if prev is not None and tilt == prev:
            print("TILT CLASH", text, tilt)
            return 1
        if abs(tilt) < 8 or abs(tilt) > 25:
            print("TILT RANGE", text, tilt)
            return 1
        prev = tilt
    print("TILTS", len(bake.WORDS), "ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
