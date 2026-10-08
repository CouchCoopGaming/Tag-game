"""Pack pass-6 stills under 400 KB. Court paint is snapped before the palette cut."""

import os
import sys

from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "Docs", "AssetStills", "pass6")
LIMIT = 400 * 1024


def _snap_court(im):
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            if r >= 150 and g >= 145 and b >= 135 and abs(r - g) < 25 and b < r + 18:
                px[x, y] = (242, 242, 236)
            elif r < 100 and g < 140 and b > r + 15 and 70 < b < 190 and g > 40:
                px[x, y] = (40, 78, 122)


def pack(folder):
    folder = os.path.abspath(folder)
    failed = False
    for name in sorted(os.listdir(folder)):
        if not name.endswith(".png"):
            continue
        path = os.path.join(folder, name)
        im = Image.open(path).convert("RGB")
        if name.startswith("court"):
            _snap_court(im)
        q = im.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
        q.save(path, optimize=True)
        size = os.path.getsize(path)
        print("PACK", name, size)
        if size >= LIMIT:
            failed = True
    return 1 if failed else 0


if __name__ == "__main__":
    target = sys.argv[1] if len(sys.argv) > 1 else ROOT
    raise SystemExit(pack(target))
