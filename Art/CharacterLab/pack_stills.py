"""Pack costume stills under 400 KB each."""
import json
import os

from PIL import Image, ImageDraw, ImageFont

RAW = "/tmp/charlab/pass1"
DOCS = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Docs", "Characters", "pass1"))
LIMIT = 400 * 1024
HOOKS = {
    "Reed": "Leaves before the count.",
    "Bram": "Stays in the doorway.",
    "Pip": "Fits the gap you missed.",
    "Sol": "The one you spot first.",
}
SINGLES = (
    "lineup-front.png",
    "lineup-three-quarter.png",
    "lineup-side.png",
    "readability-30px.png",
)


def font(size):
    path = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
    if os.path.exists(path):
        return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def crunch(im, dest):
    im = im.convert("RGB")
    for colors in (160, 128, 96, 72, 48, 32):
        packed = im.quantize(colors=colors, method=Image.Quantize.FASTOCTREE).convert("RGB")
        packed.save(dest, optimize=True)
        size = os.path.getsize(dest)
        if size <= LIMIT:
            return size, colors, packed.size
    scale = 0.85
    cur = im
    while scale >= 0.45:
        resized = cur.resize(
            (max(1, int(cur.width * scale)), max(1, int(cur.height * scale))),
            Image.Resampling.LANCZOS,
        )
        packed = resized.quantize(colors=48, method=Image.Quantize.FASTOCTREE).convert("RGB")
        packed.save(dest, optimize=True)
        size = os.path.getsize(dest)
        if size <= LIMIT:
            return size, 48, packed.size
        scale -= 0.1
    raise SystemExit("still over 400KB: " + dest)


def stitch_variants():
    rows = json.load(open(os.path.join(RAW, "rows.json"), encoding="utf-8"))
    title = font(22)
    small = font(18)
    header_h = 54
    gap = 10
    images = [Image.open(row["file"]).convert("RGB") for row in rows]
    width = max(im.width for im in images)
    height = header_h * len(rows) + sum(im.height for im in images) + gap * (len(rows) - 1)
    sheet = Image.new("RGB", (width, height), (236, 234, 230))
    draw = ImageDraw.Draw(sheet)
    y = 0
    for row, im in zip(rows, images):
        color = tuple(row["color"])
        draw.rectangle((0, y, width, y + header_h), fill=color)
        ink = (255, 255, 255) if sum(color) < 420 else (28, 24, 22)
        draw.text((16, y + 6), "%s  —  %s" % (row["who"], HOOKS[row["who"]]), fill=ink, font=title)
        labels = "          ".join("%d  %s" % (i + 1, label) for i, label in enumerate(row["labels"]))
        draw.text((16, y + 30), labels, fill=ink, font=small)
        y += header_h
        sheet.paste(im, (0, y))
        y += im.height + gap
    return sheet


def main():
    os.makedirs(DOCS, exist_ok=True)
    for name in SINGLES:
        dest = os.path.join(DOCS, name)
        size, colors, wh = crunch(Image.open(os.path.join(RAW, name)), dest)
        print("PACK %s bytes=%d colors=%d %dx%d" % (name, size, colors, wh[0], wh[1]), flush=True)
    dest = os.path.join(DOCS, "variants.png")
    size, colors, wh = crunch(stitch_variants(), dest)
    print("PACK variants.png bytes=%d colors=%d %dx%d" % (size, colors, wh[0], wh[1]), flush=True)


if __name__ == "__main__":
    main()
