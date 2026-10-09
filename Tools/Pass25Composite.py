"""Tile pass 25 panels with system PIL. No quantize."""
import os
from PIL import Image

PANEL = "/tmp/p25-panels"
HIP = "/tmp/p25-hip"
OUT = "/workspace/Docs/AnimStills/pass25"
os.makedirs(OUT, exist_ok=True)
LIMIT = 400 * 1024

names = [
    "exit-WallRun",
    "exit-WallJump",
    "exit-ClimbTopOut",
    "exit-ClingDrop",
    "exit-Vault",
    "exit-Mantle",
    "exit-Slide",
    "exit-AirDash",
    "exit-Punch",
    "exit-Lunge",
    "exit-ZipDrop",
    "exit-LaunchLand",
    "exit-GrappleArrive",
    "exit-GrappleRelease",
    "exit-Stagger",
    "exit-TagBackEnd",
    "exit-SoftLand",
    "exit-Roll",
    "exit-RollAbsorb",
    "exit-ClimbTopOut-side",
]


def save(im, dest):
    im.save(dest, "PNG", optimize=True, compress_level=9)
    size = os.path.getsize(dest)
    scale = 0.9
    while size >= LIMIT and scale > 0.45:
        small = im.resize((max(1, int(im.width * scale)), max(1, int(im.height * scale))), Image.Resampling.LANCZOS)
        small.save(dest, "PNG", optimize=True, compress_level=9)
        size = os.path.getsize(dest)
        scale -= 0.08
    return size


for name in names:
    panels = []
    for i in range(5):
        path = os.path.join(PANEL, "%s-%d.png" % (name, i))
        panels.append(Image.open(path).convert("RGB"))
    w, h = panels[0].size
    sheet = Image.new("RGB", (w * 5, h), (26, 28, 33))
    for i, im in enumerate(panels):
        sheet.paste(im, (i * w, 0))
    dest = os.path.join(OUT, name + ".png")
    print(name, save(sheet, dest))

for name in (
    "exit-ClimbTopOut-before",
    "exit-ClimbTopOut-after",
    "exit-Vault-before",
    "exit-Vault-after",
    "exit-Mantle-before",
    "exit-Mantle-after",
):
    src = os.path.join(HIP, name + ".png")
    im = Image.open(src).convert("RGB")
    dest = os.path.join(OUT, name + ".png")
    print(name, save(im, dest))
print("COMPOSITE_EXIT")
