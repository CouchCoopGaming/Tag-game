#!/usr/bin/env python3
"""Headless pose stills for the Hier proportion rig.

Blender is not required. Bone lengths match Tools/Tag/build_mannequin_hier.py
(hip 1.05, thigh 0.54, shin 0.49, upper arm 0.37, forearm 0.33, shoulder 0.235).
Joint angles are the runtime clips: VerbPoseClips slide, LaunchPose apex,
ZipPose hang, PunchStaggerPose stumble. Y-up, face +Z. No root motion.
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = "/workspace/Docs/AnimStills"
W, H = 1200, 1500

HIP_Y = 1.05
HIP_X = 0.118
UL, LL = 0.54, 0.49
UA, LA = 0.37, 0.33
SH_X, SH_Z = 0.235, -0.06
SLIDE_DROP = 0.525

CREAM = np.array([0.910, 0.851, 0.753])
HAND = np.array([0.78, 0.62, 0.48])
JOINT = np.array([0.16, 0.15, 0.16])
EYE = np.array([0.04, 0.04, 0.045])
CABLE = np.array([0.22, 0.23, 0.25])
SOLE = np.array([0.45, 0.32, 0.24])


def rx(v, deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    x, y, z = v
    return np.array([x, y * c - z * s, y * s + z * c], dtype=np.float64)


def ry(v, deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    x, y, z = v
    return np.array([x * c + z * s, y, -x * s + z * c], dtype=np.float64)


def norm(v):
    n = np.linalg.norm(v)
    if n < 1e-8:
        return v
    return v / n


def limb(sx, thigh, yaw_out, knee, drop=0.0):
    hip = np.array([sx * HIP_X, HIP_Y - drop, -0.02], dtype=np.float64)
    down = np.array([0.0, -1.0, 0.0])
    d = norm(ry(rx(down, -thigh), yaw_out * sx))
    knee_p = hip + d * UL
    ds = norm(ry(rx(down, -(thigh + knee)), yaw_out * sx))
    ankle = knee_p + ds * LL
    return hip, knee_p, ankle, ds


def arm(sx, chest_pitch, pitch, yaw_out, elbow, drop=0.0):
    up = rx(np.array([0.0, 1.0, 0.0]), chest_pitch)
    basis = np.array([0.0, HIP_Y - drop, 0.0]) + up * (1.40 - HIP_Y)
    shoulder = basis + rx(np.array([sx * SH_X, 0.0, SH_Z]), chest_pitch)
    out_a = math.radians(24.0)
    fwd_a = math.radians(10.0)
    rest = np.array([sx * math.sin(out_a), -math.cos(out_a), math.sin(fwd_a)])
    d = norm(ry(rx(rest, pitch), yaw_out * sx))
    elbow_p = shoulder + d * UA
    fd = norm(rx(d, elbow))
    hand = elbow_p + fd * LA
    return shoulder, elbow_p, hand


def torso(chest_pitch, head_pitch):
    origin = np.array([0.0, HIP_Y, 0.0])
    up = rx(np.array([0.0, 1.0, 0.0]), chest_pitch)
    chest = origin + up * 0.30
    head_dir = rx(np.array([0.0, 1.0, 0.0]), chest_pitch + head_pitch)
    face = rx(np.array([0.0, 0.0, 1.0]), chest_pitch + head_pitch)
    head = origin + up * 0.36 + head_dir * 0.16
    return origin, chest, head, face, head_dir


class Rig:
    def __init__(self):
        self.spheres = []
        self.caps = []
        self.drop = 0.0
        self.ground = True
        self.frame_caps = []

    def s(self, c, r, color):
        self.spheres.append((np.asarray(c, dtype=np.float64), float(r), np.asarray(color, dtype=np.float64)))

    def c(self, a, b, r, color, frame=True):
        cap = (np.asarray(a, dtype=np.float64), np.asarray(b, dtype=np.float64), float(r), np.asarray(color, dtype=np.float64))
        self.caps.append(cap)
        if frame:
            self.frame_caps.append(cap)

    def apply_drop(self):
        if self.drop == 0.0:
            return
        d = np.array([0.0, self.drop, 0.0])
        self.spheres = [(c - d, r, col) for c, r, col in self.spheres]
        self.caps = [(a - d, b - d, r, col) for a, b, r, col in self.caps]
        self.frame_caps = [(a - d, b - d, r, col) for a, b, r, col in self.frame_caps]

    def roll(self, deg):
        if abs(deg) < 0.01:
            return
        a = math.radians(deg)
        c, s = math.cos(a), math.sin(a)
        pivot = np.array([0.0, HIP_Y - self.drop, 0.0])

        def rz(p):
            q = p - pivot
            return pivot + np.array([q[0] * c - q[1] * s, q[0] * s + q[1] * c, q[2]])

        self.spheres = [(rz(c), r, col) for c, r, col in self.spheres]
        self.caps = [(rz(a), rz(b), r, col) for a, b, r, col in self.caps]
        self.frame_caps = [(rz(a), rz(b), r, col) for a, b, r, col in self.frame_caps]


def add_leg(rig, sx, thigh, yaw, knee, foot_mode):
    hip, knee_p, ankle, shin = limb(sx, thigh, yaw, knee)
    rig.c(hip, knee_p, 0.072, CREAM)
    rig.c(knee_p, ankle, 0.048, CREAM)
    rig.s(knee_p, 0.058, CREAM)
    rig.s(hip, 0.06, CREAM)
    if foot_mode == "plant":
        heel = ankle + np.array([sx * 0.02, -0.012, -0.055])
        toe = ankle + np.array([sx * 0.015, -0.008, 0.155])
        rig.c(heel, toe, 0.028, SOLE)
    else:
        toe = ankle + shin * 0.12 + np.array([0.0, 0.02, 0.0])
        rig.c(ankle, toe, 0.026, SOLE)
    return ankle


def add_arm(rig, sx, chest, pitch, yaw, elbow, hand_r=0.046):
    sh, el, hand = arm(sx, chest, pitch, yaw, elbow)
    rig.c(sh, el, 0.048, CREAM)
    rig.c(el, hand, 0.034, CREAM)
    rig.s(el, 0.04, CREAM)
    rig.s(hand, hand_r, HAND)
    return sh, hand


def add_body(rig, chest_pitch, head_pitch, hip_yaw=0.0):
    pelvis, chest, head, face, head_up = torso(chest_pitch, head_pitch)
    if abs(hip_yaw) > 0.01:
        pelvis_p = pelvis.copy()

        def yaw_pt(p):
            return pelvis_p + ry(p - pelvis_p, hip_yaw)

        chest, head = yaw_pt(chest), yaw_pt(head)
        face, head_up = ry(face, hip_yaw), ry(head_up, hip_yaw)
    neck = chest + (head - chest) * 0.55
    rig.s(pelvis, 0.115, CREAM)
    rig.c(pelvis, chest, 0.10, CREAM)
    rig.s(chest, 0.125, CREAM)
    rig.c(chest, neck, 0.07, CREAM)
    rig.c(neck, head, 0.05, CREAM)
    rig.s(head, 0.112, CREAM)
    right = norm(np.cross(face, np.array([0.0, 1.0, 0.0])))
    if np.linalg.norm(right) < 1e-4:
        right = np.array([1.0, 0.0, 0.0])
    gaze = norm(face * 1.0 + head_up * 0.08)
    for sgn in (-1.0, 1.0):
        eye = head + gaze * 0.104 + right * (0.034 * sgn) + head_up * 0.012
        rig.s(eye, 0.020, EYE)
    return pelvis, chest, head


def build_slide():
    rig = Rig()
    rig.drop = SLIDE_DROP
    chest = -22.0 + -14.0
    add_body(rig, chest, 50.0)
    # Clearance model: lead leg is -X, lead (free) arm is +X, trail hand is -X.
    add_leg(rig, -1.0, 68.0, 8.0, -10.0, "plant")
    add_leg(rig, 1.0, 40.0, 24.0, -130.0, "tuck")
    add_arm(rig, 1.0, chest, -36.0, 22.0, -28.0)
    add_arm(rig, -1.0, chest, 48.0, 22.0, -36.0, hand_r=0.05)
    rig.apply_drop()
    return rig


def build_launch():
    """Apex tuck. vy = 0 selects the tuck beat."""
    rig = Rig()
    rig.ground = False
    chest = 10.0 + -14.0
    add_body(rig, chest, -16.0)
    add_leg(rig, -1.0, 96.0, 0.0, -128.0, "tuck")
    add_leg(rig, 1.0, 92.0, 0.0, -122.0, "tuck")
    add_arm(rig, -1.0, chest, -168.0, 8.0, -10.0)
    add_arm(rig, 1.0, chest, -168.0, 8.0, -10.0)
    return rig


def build_zip():
    rig = Rig()
    rig.ground = False
    chest = -8.0 + -6.0
    add_body(rig, chest, -4.0)
    add_leg(rig, -1.0, 34.0, 0.0, -18.0, "plant")
    add_leg(rig, 1.0, 30.0, 0.0, -14.0, "plant")
    _, hand_l = add_arm(rig, -1.0, chest, -138.0, 8.0, -8.0, hand_r=0.05)
    _, hand_r = add_arm(rig, 1.0, chest, -138.0, 8.0, -8.0, hand_r=0.05)
    mid = (hand_l + hand_r) * 0.5
    axis = np.array([1.0, 0.0, 0.02])
    rig.c(mid - axis * 1.55, mid + axis * 1.55, 0.016, CABLE, frame=False)
    rig.roll(7.0)
    return rig


def build_stagger():
    rig = Rig()
    chest = -18.0 + -16.0
    add_body(rig, chest, -24.0, hip_yaw=8.0 + -6.0)
    add_leg(rig, -1.0, 38.0, 0.0, -34.0, "plant")
    add_leg(rig, 1.0, -16.0, 0.0, -8.0, "plant")
    add_arm(rig, -1.0, chest, 46.0, 28.0, -30.0)
    add_arm(rig, 1.0, chest, 34.0, 26.0, -24.0)
    return rig


def slide_checks():
    chest = -36.0
    _, _, ankle_l, _ = limb(-1.0, 68.0, 8.0, -10.0)
    _, _, ankle_t, _ = limb(1.0, 40.0, 24.0, -130.0)
    _, _, hand = arm(-1.0, chest, 48.0, 22.0, -36.0)
    sole = ankle_l[1] - 0.035 - SLIDE_DROP
    trail = ankle_t[1] - 0.03 - SLIDE_DROP
    pelvis = HIP_Y - SLIDE_DROP
    hand_d = hand - np.array([0.0, SLIDE_DROP, 0.0])
    print(f"slide sole={sole:.3f} trail={trail:.3f} pelvis={pelvis:.3f} hand={hand_d[0]:.3f},{hand_d[1]:.3f},{hand_d[2]:.3f}")
    ok = (
        abs(sole - 0.028) < 0.02
        and abs(trail - 0.08) < 0.03
        and abs(pelvis - 0.525) < 0.001
        and abs(hand_d[0] + 0.35) < 0.04
        and abs(hand_d[1] - 0.18) < 0.03
        and hand_d[2] < -0.40
    )
    if not ok:
        raise SystemExit("slide kinematic check failed")


def ndot(a, b):
    return np.sum(a * b, axis=-1)


def normalize_img(v):
    n = np.linalg.norm(v, axis=-1, keepdims=True)
    return v / np.maximum(n, 1e-8)


def ray_sphere(ro, rd, c, r):
    oc = ro - c
    b = ndot(rd, oc)
    c0 = ndot(oc, oc) - r * r
    disc = b * b - c0
    t = np.full(disc.shape, np.inf)
    hit = disc >= 0.0
    if not np.any(hit):
        return t, None
    s = np.sqrt(np.maximum(disc, 0.0))
    t0 = -b - s
    t1 = -b + s
    tp = np.where(t0 > 1e-3, t0, np.where(t1 > 1e-3, t1, np.inf))
    t = np.where(hit, tp, np.inf)
    return t, None


def ray_capsule(ro, rd, pa, pb, ra):
    ba = pb - pa
    baba = float(np.dot(ba, ba))
    oc = ro - pa
    bard = ndot(rd, ba)
    baoc = ndot(oc, ba)
    k2 = baba - bard * bard
    k1 = baba * ndot(oc, rd) - baoc * bard
    k0 = baba * ndot(oc, oc) - baoc * baoc - ra * ra * baba
    h = k1 * k1 - k2 * k0
    t = np.full(h.shape, np.inf)
    safe = np.abs(k2) > 1e-8
    hpos = (h >= 0.0) & safe
    root = np.sqrt(np.maximum(h, 0.0))
    tc = (-k1 - root) / np.where(safe, k2, 1.0)
    y = baoc + tc * bard
    cyl = hpos & (y > 0.0) & (y < baba) & (tc > 1e-3)
    t = np.where(cyl, tc, t)
    ts, _ = ray_sphere(ro, rd, pa, ra)
    te, _ = ray_sphere(ro, rd, pb, ra)
    t = np.minimum(t, np.minimum(ts, te))
    return t


def shade(n, rd, albedo, light):
    n = normalize_img(n)
    ndl = np.clip(ndot(n, light), 0.0, 1.0)
    fill = np.array([-0.45, 0.25, 0.55], dtype=np.float64)
    fill = fill / np.linalg.norm(fill)
    ndf = np.clip(ndot(n, fill), 0.0, 1.0)
    view = normalize_img(-rd)
    rim = np.clip(1.0 - ndot(n, view), 0.0, 1.0) ** 2
    col = albedo * (0.20 + 0.78 * ndl[..., None] + 0.22 * ndf[..., None])
    col = col + rim[..., None] * 0.18
    return np.clip(col, 0.0, 1.0)


def dilate(mask, radius):
    out = mask.copy()
    for dy in range(-radius, radius + 1):
        for dx in range(-radius, radius + 1):
            if dx * dx + dy * dy > radius * radius:
                continue
            shifted = np.roll(np.roll(mask, dy, axis=0), dx, axis=1)
            if dy > 0:
                shifted[:dy, :] = False
            elif dy < 0:
                shifted[dy:, :] = False
            if dx > 0:
                shifted[:, :dx] = False
            elif dx < 0:
                shifted[:, dx:] = False
            out |= shifted
    return out


def render(rig, view):
    pts = [c for c, _, _ in rig.spheres]
    for a, b, _, _ in rig.frame_caps:
        pts.append(a)
        pts.append(b)
    pts = np.stack(pts, axis=0)
    lo = pts.min(axis=0).copy()
    hi = pts.max(axis=0).copy()
    if not rig.ground:
        lo[1] -= 0.55
    center = (lo + hi) * 0.5
    radius = max(float(np.linalg.norm(hi - lo)) * 0.55, 0.85)
    if view == "front":
        eye_dir = norm(np.array([0.08, 0.12, 1.0]))
    else:
        # Left-front, so the slide's trail hand and the stagger's step leg face the camera.
        eye_dir = norm(np.array([-1.05, 0.22, 0.50]))
    dist = radius / math.tan(math.radians(16.0)) * 1.08
    eye = center + eye_dir * dist
    target = center.copy()
    target[1] *= 0.92

    forward = norm(target - eye)
    right = norm(np.cross(forward, np.array([0.0, 1.0, 0.0])))
    up = np.cross(right, forward)
    fov = math.radians(32.0)
    aspect = W / H
    ys, xs = np.mgrid[0:H, 0:W]
    sx = (2.0 * (xs + 0.5) / W - 1.0) * math.tan(fov * 0.5) * aspect
    sy = (1.0 - 2.0 * (ys + 0.5) / H) * math.tan(fov * 0.5)
    rd = normalize_img(forward + right * sx[..., None] + up * sy[..., None])
    ro = np.broadcast_to(eye, rd.shape).copy()

    t_best = np.full((H, W), np.inf)
    n_best = np.zeros((H, W, 3))
    c_best = np.zeros((H, W, 3))

    def consider(t, n, col):
        nonlocal t_best, n_best, c_best
        better = t < t_best
        t_best = np.where(better, t, t_best)
        n_best = np.where(better[..., None], n, n_best)
        c_best = np.where(better[..., None], col, c_best)

    for c, r, col in rig.spheres:
        t, _ = ray_sphere(ro, rd, c, r)
        hit = np.isfinite(t)
        pt = ro + rd * t[..., None]
        n = pt - c
        n = np.where(hit[..., None], n, 0.0)
        consider(np.where(hit, t, np.inf), n, col)

    for a, b, r, col in rig.caps:
        t = ray_capsule(ro, rd, a, b, r)
        hit = np.isfinite(t)
        pt = ro + rd * t[..., None]
        ba = b - a
        baba = float(np.dot(ba, ba))
        y = ndot(pt - a, ba) / baba
        y = np.clip(y, 0.0, 1.0)
        axis = a + ba * y[..., None]
        n = pt - axis
        n = np.where(hit[..., None], n, 0.0)
        consider(np.where(hit, t, np.inf), n, col)

    # Ground y = 0. Airborne poses leave it out so the feet are not planted.
    if rig.ground:
        denom = rd[..., 1]
        tg = np.where(denom < -1e-4, -ro[..., 1] / denom, np.inf)
        tg = np.where(tg > 1e-3, tg, np.inf)
        ground = tg < t_best
        gp = ro + rd * tg[..., None]
    else:
        ground = np.zeros((H, W), dtype=bool)
        gp = ro

    light = norm(np.array([0.28, 0.86, 0.42]))
    img = np.zeros((H, W, 3))
    sky_t = np.linspace(0.0, 1.0, H)[:, None, None]
    sky = (1.0 - sky_t) * np.array([0.55, 0.60, 0.66]) + sky_t * np.array([0.16, 0.18, 0.22])
    img[:] = sky

    body = np.isfinite(t_best) & ~ground
    if np.any(body):
        shaded = shade(n_best, rd, c_best, light)
        img = np.where(body[..., None], shaded, img)

    if rig.ground and np.any(ground):
        gcol = np.array([0.46, 0.47, 0.45])
        xz = gp[..., [0, 2]]
        grid = (np.abs(np.mod(xz[..., 0] + 0.25, 0.5) - 0.25) < 0.012) | (
            np.abs(np.mod(xz[..., 1] + 0.25, 0.5) - 0.25) < 0.012
        )
        base = np.broadcast_to(gcol, img.shape).copy()
        base = np.where(grid[..., None], base * 0.86, base)
        # Soft contact from the nearest body point in XZ.
        shadow = np.ones((H, W))
        samples = []
        for c, r, _ in rig.spheres:
            samples.append(c)
        for a, b, _, _ in rig.caps:
            samples.append((a + b) * 0.5)
        samples = np.stack(samples, axis=0)
        dmin = np.full((H, W), 1e3)
        for p in samples:
            d = np.hypot(gp[..., 0] - p[0], gp[..., 2] - p[2])
            dmin = np.minimum(dmin, d + max(0.0, p[1]) * 0.15)
        k = np.clip(1.0 - np.exp(-dmin * 1.6), 0.45, 1.0)
        shadow = np.where(ground, k, 1.0)
        base = base * shadow[..., None]
        img = np.where(ground[..., None], base, img)

    if np.any(body):
        edge = dilate(body, 5) & ~body & ~ground
        img = np.where(edge[..., None], np.array([0.05, 0.05, 0.06]), img)

    img = np.nan_to_num(img, nan=0.55, posinf=1.0, neginf=0.0)
    img = np.clip(img, 0.0, 1.0) ** (1.0 / 2.2)
    return (img * 255.0 + 0.5).astype(np.uint8)


def caption(im, title, subtitle):
    bar_h = 92
    canvas = Image.new("RGB", (im.width, im.height + bar_h), (28, 30, 32))
    canvas.paste(im, (0, 0))
    draw = ImageDraw.Draw(canvas)
    try:
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 28)
        small = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 20)
    except OSError:
        font = ImageFont.load_default()
        small = font
    draw.text((28, im.height + 14), title, fill=(236, 232, 224), font=font)
    draw.text((28, im.height + 52), subtitle, fill=(176, 180, 184), font=small)
    return canvas


def save(rig, view, name, title, subtitle):
    print(f"render {name} {view}")
    rgb = render(rig, view)
    im = Image.fromarray(rgb, "RGB")
    im = caption(im, title, subtitle)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, f"{name}_{view}.png")
    im.save(path, optimize=True)
    print(f"wrote {path} {im.size}")


def add_wall_x(rig, x):
    col = np.array([0.62, 0.64, 0.66])
    for z in (-0.35, 0.05, 0.45):
        rig.c(np.array([x, 0.15, z]), np.array([x, 2.15, z]), 0.045, col, frame=False)


def add_wall_z(rig, z):
    col = np.array([0.62, 0.64, 0.66])
    for x in (-0.28, 0.0, 0.28):
        rig.c(np.array([x, 0.2, z]), np.array([x, 2.2, z]), 0.04, col, frame=False)


def build_wallrun():
    rig = Rig()
    chest = 18.0
    add_body(rig, chest, -4.0)
    add_leg(rig, -1.0, 11.6, 0.0, -87.0, "tuck")
    add_leg(rig, 1.0, -10.0, 0.0, -5.0, "plant")
    add_arm(rig, -1.0, chest, 5.0, 36.0, -18.0)
    add_arm(rig, 1.0, chest, -61.0, 12.0, -16.0)
    rig.roll(-20.0)
    add_wall_x(rig, -0.59)
    return rig


def build_climb():
    rig = Rig()
    rig.ground = False
    chest = 22.0
    add_body(rig, chest, -26.0)
    add_leg(rig, -1.0, 56.0, 0.0, -60.0, "plant")
    add_leg(rig, 1.0, 76.0, 0.0, -88.0, "plant")
    add_arm(rig, -1.0, chest, -120.0, -16.0, -16.0)
    add_arm(rig, 1.0, chest, -70.0, 16.0, -48.0)
    add_wall_z(rig, 0.60)
    return rig


def build_slip():
    rig = Rig()
    rig.drop = 0.18
    chest = 16.0
    add_body(rig, chest, -6.0)
    add_leg(rig, -1.0, 46.0, 0.0, -90.0, "tuck")
    add_leg(rig, 1.0, 34.0, 0.0, -90.0, "tuck")
    add_arm(rig, -1.0, chest, -106.0, 8.0, -14.0)
    add_arm(rig, 1.0, chest, -118.0, -8.0, -14.0)
    rig.apply_drop()
    add_wall_z(rig, 0.57)
    return rig


def build_walljump():
    rig = Rig()
    rig.ground = False
    chest = -32.0
    add_body(rig, chest, -16.0)
    add_leg(rig, -1.0, 44.0, 0.0, -64.0, "tuck")
    add_leg(rig, 1.0, 82.0, 0.0, -110.0, "tuck")
    add_arm(rig, -1.0, chest, 46.0, 20.0, -16.0)
    add_arm(rig, 1.0, chest, 30.0, 14.0, -20.0)
    rig.roll(-18.0)
    add_wall_z(rig, 0.62)
    return rig


def build_airdash():
    rig = Rig()
    rig.ground = False
    chest = 82.0
    add_body(rig, chest, 6.0)
    add_leg(rig, -1.0, -16.0, 0.0, -12.0, "tuck")
    add_leg(rig, 1.0, -16.0, 0.0, -12.0, "tuck")
    add_arm(rig, -1.0, chest, 70.0, 10.0, -18.0)
    add_arm(rig, 1.0, chest, 70.0, 10.0, -18.0)
    return rig


def build_land_soft():
    rig = Rig()
    rig.drop = 0.02
    chest = 14.0
    add_body(rig, chest, -4.0)
    add_leg(rig, -1.0, 18.0, 0.0, -26.0, "plant")
    add_leg(rig, 1.0, 18.0, 0.0, -26.0, "plant")
    add_arm(rig, -1.0, chest, -16.0, 12.0, -14.0)
    add_arm(rig, 1.0, chest, -16.0, 12.0, -14.0)
    rig.apply_drop()
    return rig


def build_land_hard():
    rig = Rig()
    rig.drop = 0.50
    chest = 74.0
    add_body(rig, chest, 8.0)
    add_leg(rig, -1.0, 74.0, 0.0, -125.0, "plant")
    add_leg(rig, 1.0, 74.0, 0.0, -125.0, "plant")
    add_arm(rig, -1.0, chest, 18.0, 16.0, -16.0)
    add_arm(rig, 1.0, chest, -30.0, 12.0, -40.0)
    rig.apply_drop()
    return rig


def mech_checks():
    hand = roll(arm(-1.0, 18.0, 5.0, 36.0, -18.0)[2], -20.0)
    ankle = roll(limb(1.0, -10.0, 0.0, -5.0)[2], -20.0)
    print(f"wallrun hand={hand[0]:.3f},{hand[1]:.3f} ankle={ankle[1]:.3f}")
    if hand[0] > -0.50 or ankle[1] > 0.12 or ankle[1] < -0.02:
        raise SystemExit("wall-run contact failed")

    high = arm(-1.0, 22.0, -120.0, -16.0, -16.0)[2]
    low = arm(1.0, 22.0, -70.0, 16.0, -48.0)[2]
    foot_lo = limb(-1.0, 56.0, 0.0, -60.0)[2]
    foot_hi = limb(1.0, 76.0, 0.0, -88.0)[2]
    print(f"climb high={high[2]:.3f} low={low[2]:.3f} feet={foot_lo[2]:.3f},{foot_hi[2]:.3f}")
    if high[2] < 0.45 or low[2] < 0.48 or foot_lo[2] < 0.30 or foot_hi[2] < 0.30:
        raise SystemExit("climb contact failed")
    if high[1] < low[1] + 0.25:
        raise SystemExit("climb hands do not alternate")

    slip_l = arm(-1.0, 16.0, -106.0, 8.0, -14.0, drop=0.18)[2]
    slip_r = arm(1.0, 16.0, -118.0, -8.0, -14.0, drop=0.18)[2]
    sag = limb(-1.0, 46.0, 0.0, -90.0, drop=0.18)[2]
    print(f"slip hands={slip_l[1]:.3f},{slip_r[1]:.3f} foot={sag[1]:.3f} pelvis={HIP_Y - 0.18:.3f}")
    if slip_l[1] < 1.4 or slip_r[1] < 1.4 or sag[1] < 0.05:
        raise SystemExit("slip pose failed")
    if slip_l[2] < 0.48 or slip_r[2] < 0.48:
        raise SystemExit("slip hands left the wall")

    kick_l = limb(-1.0, 44.0, 0.0, -64.0)[2]
    kick_r = limb(1.0, 82.0, 0.0, -110.0)[2]
    away_l = arm(-1.0, -32.0, 46.0, 20.0, -16.0)[2]
    away_r = arm(1.0, -32.0, 30.0, 14.0, -20.0)[2]
    print(f"walljump feet={kick_l[2]:.3f},{kick_r[2]:.3f} arms={away_l[2]:.3f},{away_r[2]:.3f}")
    if kick_l[2] + 0.12 > 0.55 or kick_r[2] + 0.12 > 0.55:
        raise SystemExit("wall-jump feet still on the wall")
    if kick_l[1] < 0.10 or kick_r[1] < 0.10:
        raise SystemExit("wall-jump feet on the ground")
    if away_l[2] > -0.15 or away_r[2] > -0.15:
        raise SystemExit("wall-jump arms not away")

    dash_arm = arm(-1.0, 82.0, 70.0, 10.0, -18.0)[2]
    dash_foot = limb(-1.0, -16.0, 0.0, -12.0)[2]
    print(f"airdash arm={dash_arm[2]:.3f} foot={dash_foot[2]:.3f}")
    if dash_arm[2] > -0.10 or dash_foot[2] > -0.20:
        raise SystemExit("air-dash pose failed")

    soft_hand = arm(-1.0, 14.0, -16.0, 12.0, -14.0, drop=0.02)[2]
    soft_foot = limb(-1.0, 18.0, 0.0, -26.0, drop=0.02)[2]
    hard_hand = arm(-1.0, 74.0, 18.0, 16.0, -16.0, drop=0.50)[2]
    hard_free = arm(1.0, 74.0, -30.0, 12.0, -40.0, drop=0.50)[2]
    hard_foot = limb(-1.0, 74.0, 0.0, -125.0, drop=0.50)[2]
    print(f"soft hand={soft_hand[1]:.3f} foot={soft_foot[1]:.3f} hard hand={hard_hand[1]:.3f} free={hard_free[1]:.3f} foot={hard_foot[1]:.3f}")
    if soft_hand[1] < 0.5 or soft_foot[1] > 0.12:
        raise SystemExit("soft land failed")
    if hard_hand[1] > 0.10 or hard_foot[1] > 0.16 or hard_free[1] < 0.25:
        raise SystemExit("hard land failed")


def roll(p, deg, drop=0.0):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    pivot = np.array([0.0, HIP_Y - drop, 0.0])
    q = p - pivot
    return pivot + np.array([q[0] * c - q[1] * s, q[0] * s + q[1] * c, q[2]])


def main():
    slide_checks()
    mech_checks()
    shots = [
        (build_slide, "slide", "Slide — baseball / parkour", "Lead extended, trail tucked, chest back, trail hand down. Pelvis at crouch center."),
        (build_launch, "launch_apex", "Launch pad — apex tuck", "Arms up, knees tucked. Not the jump rise. Opens again on the way down."),
        (build_zip, "zip_hang", "Zip line — two-hand hang", "Hands under the cable, legs forward, sway at the 14 m/s ride."),
        (build_stagger, "stagger", "Punch stagger — 0.25 s", "Head and chest snap back. One leg steps. Window matches the motor."),
        (build_wallrun, "wallrun", "Wall run — lean 20°", "Chest off the wall, inner hand brushing, outer foot planted. Cadence matches 9.5."),
        (build_climb, "climb", "Cling climb — hand over hand", "High hand reaches, low hand pulls. Feet on the wall. Synced to climb 6.0."),
        (build_slip, "slip", "Cling slip — hands dragging", "Hands stay high and slide. Hips sag. Feet come off the floor. Slip speed 3.7."),
        (build_walljump, "walljump", "Wall jump — both feet off", "Kick leaves the wall. Arms swing away, then the jump pose takes over."),
        (build_airdash, "airdash", "Air dash — 0.10 s", "Body horizontal, arms back, legs trailing. Returns on the handoff."),
        (build_land_soft, "land_soft", "Soft land", "Small knee bend. Hands stay up. Impact at the soft audio floor, 5."),
        (build_land_hard, "land_hard", "Hard land", "Deep crouch, one hand down. Impact at land-stun speed, 28. Control is not delayed."),
    ]
    for build, name, title, subtitle in shots:
        rig = build()
        for view in ("front", "three_quarter"):
            save(rig, view, name, f"{title}  ·  {view.replace('_', ' ')}", subtitle)


if __name__ == "__main__":
    main()
