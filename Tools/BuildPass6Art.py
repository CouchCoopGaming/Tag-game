#!/usr/bin/env python3
"""Project-owned flipbooks for the verb FX this pass owns.

Soft dust (8 frames), a ground ring with a cracked variant for each
SurfaceTag, and a wet drip. No paid art. Written for Assets/Resources/FX.
"""
import os

import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "FX")

SURFACES = (
    ("grass", (92, 118, 48)),
    ("dirt", (168, 124, 72)),
    ("concrete", (168, 166, 158)),
    ("wood", (140, 92, 48)),
    ("metal", (176, 186, 196)),
    ("wet", (48, 92, 128)),
)


def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name)
    img.save(path, "PNG")
    print(name, img.size)


def gauss(h, w, cx, cy, rx, ry):
    ys = np.arange(h)[:, None]
    xs = np.arange(w)[None, :]
    return np.exp(-(((xs - cx) / rx) ** 2 + ((ys - cy) / ry) ** 2))


def dust_frame(i, n=8):
    size = 128
    u = i / float(n - 1)
    rng = np.random.RandomState(20 + i * 17)
    alpha = np.zeros((size, size), np.float32)
    spread = 16 + u * 38
    blobs = 4 + (i % 3)
    for k in range(blobs):
        ang = rng.uniform(0, np.pi * 2)
        rad = rng.uniform(0, spread * 0.55)
        cx = size * 0.5 + np.cos(ang) * rad
        cy = size * 0.5 + np.sin(ang) * rad * 0.72
        rx = rng.uniform(10, 18) + u * 16
        ry = rx * rng.uniform(0.55, 0.9)
        alpha = np.maximum(alpha, gauss(size, size, cx, cy, rx, ry) * rng.uniform(0.55, 1.0))
    alpha *= 1.0 - u * 0.72
    # A soft core so the puff reads even on the last frames.
    alpha = np.maximum(alpha, gauss(size, size, size * 0.5, size * 0.52, 12 + u * 8, 8 + u * 5) * (0.35 - u * 0.2))
    alpha = np.clip(alpha, 0, 1)
    rgb = np.zeros((size, size, 3), np.float32)
    rgb[:] = (0.92, 0.90, 0.84)
    shade = gauss(size, size, size * 0.42, size * 0.40, 22, 16)
    rgb *= 0.72 + 0.28 * shade[..., None]
    img = np.zeros((size, size, 4), np.uint8)
    img[:, :, :3] = np.clip(rgb * 255, 0, 255).astype(np.uint8)
    img[:, :, 3] = np.clip(alpha * 255, 0, 255).astype(np.uint8)
    # Two clear pixels so bilinear does not bleed into the next frame.
    img[:2, :, 3] = 0
    img[-2:, :, 3] = 0
    img[:, :2, 3] = 0
    img[:, -2:, 3] = 0
    return Image.fromarray(img, "RGBA")


def build_dust():
    frames = [dust_frame(i) for i in range(8)]
    sheet = Image.new("RGBA", (128 * 8, 128), (0, 0, 0, 0))
    for i, fr in enumerate(frames):
        sheet.paste(fr, (i * 128, 0))
    save(sheet, "DustPuff.png")


def soft_ring(color):
    size = 256
    ys = np.arange(size)[:, None]
    xs = np.arange(size)[None, :]
    r = np.sqrt((xs - size * 0.5) ** 2 + (ys - size * 0.5) ** 2) / (size * 0.5)
    band = np.exp(-((r - 0.62) / 0.10) ** 2)
    inner = np.exp(-((r - 0.48) / 0.16) ** 2) * 0.22
    alpha = np.clip(band + inner, 0, 1)
    alpha *= np.clip((0.96 - r) / 0.08, 0, 1)
    rgb = np.zeros((size, size, 3), np.float32)
    rgb[:] = np.array(color) / 255.0
    rgb += (1 - rgb) * np.clip(band, 0, 1)[..., None] * 0.35
    img = np.zeros((size, size, 4), np.uint8)
    img[:, :, :3] = np.clip(rgb * 255, 0, 255).astype(np.uint8)
    img[:, :, 3] = np.clip(alpha * 255, 0, 255).astype(np.uint8)
    img[:3, :, 3] = 0
    img[-3:, :, 3] = 0
    img[:, :3, 3] = 0
    img[:, -3:, 3] = 0
    return img


def cracked_ring(color, kind):
    base = soft_ring(color).astype(np.float32)
    size = 256
    ys = np.arange(size)[:, None]
    xs = np.arange(size)[None, :]
    ang = np.arctan2(ys - size * 0.5, xs - size * 0.5)
    r = np.sqrt((xs - size * 0.5) ** 2 + (ys - size * 0.5) ** 2) / (size * 0.5)
    cracks = np.sin(ang * (5 + kind) + kind) * np.sin(ang * (9 + kind * 2))
    crack = (np.abs(cracks) < 0.18) & (r > 0.40) & (r < 0.78)
    base[:, :, 3] *= np.where(crack, 0.15, 1.0)
    rng = np.random.RandomState(100 + kind * 13)
    chips = 10 + kind * 2
    for k in range(chips):
        a = rng.uniform(0, np.pi * 2)
        rad = rng.uniform(0.48, 0.78) * size * 0.5
        cx = size * 0.5 + np.cos(a) * rad
        cy = size * 0.5 + np.sin(a) * rad
        rx = rng.uniform(3, 8 + kind)
        ry = rng.uniform(2, 5)
        blob = gauss(size, size, cx, cy, rx, ry)
        tint = np.array(color) / 255.0
        if kind == 4:
            tint = np.clip(tint + 0.35, 0, 1)
        if kind == 5:
            tint = np.array((0.55, 0.75, 0.9))
        base[:, :, :3] = np.maximum(base[:, :, :3], (blob * 255)[..., None] * tint)
        base[:, :, 3] = np.maximum(base[:, :, 3], blob * 220)
    img = np.clip(base, 0, 255).astype(np.uint8)
    img[:3, :, 3] = 0
    img[-3:, :, 3] = 0
    img[:, :3, 3] = 0
    img[:, -3:, 3] = 0
    return img


def build_rings():
    sheet = Image.new("RGBA", (256 * 6, 256 * 2), (0, 0, 0, 0))
    for i, (name, color) in enumerate(SURFACES):
        sheet.paste(Image.fromarray(soft_ring(color), "RGBA"), (i * 256, 256))
        sheet.paste(Image.fromarray(cracked_ring(color, i), "RGBA"), (i * 256, 0))
    save(sheet, "RingAtlas.png")


def build_drip():
    w, h = 128, 256
    ys = np.arange(h)[:, None]
    xs = np.arange(w)[None, :]
    # Teardrop: round belly, pinched top.
    t = ys / float(h - 1)
    cx = w * 0.5
    half = (0.10 + 0.28 * np.sin(np.clip(t, 0, 1) * np.pi) ** 0.85) * w
    dx = (xs - cx) / np.maximum(half, 1.0)
    dy = (t - 0.62) / 0.34
    body = np.exp(-(dx ** 2 + dy ** 2))
    body = np.clip((body - 0.22) / 0.78, 0, 1)
    alpha = np.clip(body, 0, 1)
    rgb = np.zeros((h, w, 3), np.float32)
    rgb[:] = (0.45, 0.72, 0.88)
    hi = gauss(h, w, w * 0.42, h * 0.38, 10, 18)
    rgb = np.clip(rgb + hi[..., None] * 0.45, 0, 1)
    img = np.zeros((h, w, 4), np.uint8)
    img[:, :, :3] = (rgb * 255).astype(np.uint8)
    img[:, :, 3] = (alpha * 230).astype(np.uint8)
    soft = Image.fromarray(img, "RGBA").filter(ImageFilter.GaussianBlur(radius=0.6))
    save(soft, "Drip.png")


def main():
    build_dust()
    build_rings()
    build_drip()


if __name__ == "__main__":
    main()
