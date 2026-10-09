"""Pack a still quartet. Each frame stays 1280x720 or larger and under 400 KB."""
import os

from PIL import Image

RAW = os.environ.get("COSTUME_RAW", "/tmp/charlab/pass5")
DOCS = os.environ.get(
    "COSTUME_DOCS",
    os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Docs", "Characters", "pass5")),
)
LIMIT = 400 * 1024
MIN_SIZE = (1280, 720)
NAMES = (
    "lineup-three-quarter.png",
    "lineup-side.png",
    "joint-close.png",
    "scale-figure.png",
)


def crunch(im, dest):
    im = im.convert("RGB")
    if im.size[0] < MIN_SIZE[0] or im.size[1] < MIN_SIZE[1]:
        raise SystemExit("still under 1280x720: %s %s" % (dest, im.size))
    for colors in (128, 96, 64, 48, 32, 24, 16):
        packed = im.quantize(colors=colors, method=Image.Quantize.FASTOCTREE).convert("RGB")
        packed.save(dest, optimize=True)
        size = os.path.getsize(dest)
        if size <= LIMIT:
            return size, colors, packed.size
    raise SystemExit("still over 400KB: " + dest)


def main():
    os.makedirs(DOCS, exist_ok=True)
    for name in NAMES:
        dest = os.path.join(DOCS, name)
        size, colors, wh = crunch(Image.open(os.path.join(RAW, name)), dest)
        print("PACK %s bytes=%d colors=%d %dx%d" % (name, size, colors, wh[0], wh[1]), flush=True)


if __name__ == "__main__":
    main()
