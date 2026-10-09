"""Measure pelvis-behind-foot and knee on reference tracks, and draw side sticks.

Numbers come from MediaPipe world landmarks already in the repo. No video
frames are drawn. Output is Docs/Movement/hipref/.
"""
import json
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Docs", "Movement", "hipref")

# MediaPipe indices.
NOSE, LSH, RSH = 0, 11, 12
LEL, REL, LWR, RWR = 13, 14, 15, 16
LHP, RHP, LKN, RKN = 23, 24, 25, 26
LAN, RAN, LHE, RHE, LTO, RTO = 27, 28, 29, 30, 31, 32

BONES = (
    (LSH, LEL), (LEL, LWR), (RSH, REL), (REL, RWR),
    (LSH, RSH), (LSH, LHP), (RSH, RHP), (LHP, RHP),
    (LHP, LKN), (LKN, LAN), (LAN, LHE), (LAN, LTO), (LHE, LTO),
    (RHP, RKN), (RKN, RAN), (RAN, RHE), (RAN, RTO), (RHE, RTO),
)

YT = (
    "YouTube standard license, all rights reserved. "
    "REFERENCE-ONLY. Do not retarget into shipped animation. "
    "Raw video is not in the repo."
)
VIMEO = (
    "No Creative Commons license is stated on Internet Archive item "
    "vimeo-721018315 or on the Vimeo upload Sport-Freestyle-freerunning. "
    "REFERENCE-ONLY / DO-NOT-SHIP. Raw video is not in the repo."
)

# (path, category, ledger_clip, license, source_url, source_window)
CLIPS = (
    {
        "path": "Docs/Storror/out/json/02_drop_roll_gravel.json",
        "category": "roll-landing",
        "ledger_clip": "roll, exit-Roll, exit-RollAbsorb",
        "license": YT,
        "source_url": "https://www.youtube.com/watch?v=rq6XY_D_9M0&t=30s",
        "source_window": "30.80–33.30 s, Taking the Height Drop",
    },
    {
        "path": "Docs/Storror/out/json/01_roll_grass.json",
        "category": "roll-landing",
        "ledger_clip": "roll, exit-Roll, exit-RollAbsorb",
        "license": YT,
        "source_url": "https://www.youtube.com/watch?v=rq6XY_D_9M0&t=100s",
        "source_window": "100.00–101.95 s, Taking the Height Drop",
    },
    {
        "path": "Docs/Movement/pose/24_landing_roll.json",
        "category": "roll-landing",
        "ledger_clip": "roll, exit-Roll, exit-RollAbsorb",
        "license": VIMEO,
        "source_url": "https://vimeo.com/721018315",
        "source_window": "14.6–17.2 s of the unlicensed Vimeo film",
    },
    {
        "path": "Docs/Storror/out/json/09_run_vault_park.json",
        "category": "vault",
        "ledger_clip": "vault (played)",
        "license": YT,
        "source_url": "https://www.youtube.com/watch?v=zt7xP3_K7uQ&t=121s",
        "source_window": "121.50–126.00 s, Park built for parkour",
    },
    {
        "path": "Docs/Storror/out/json/08_vault_block_close.json",
        "category": "vault",
        "ledger_clip": "vault (played)",
        "license": YT,
        "source_url": "https://www.youtube.com/watch?v=zt7xP3_K7uQ&t=108s",
        "source_window": "108.30–110.40 s, Park built for parkour",
    },
    {
        "path": "Docs/Movement/pose/22_pike_vault.json",
        "category": "vault",
        "ledger_clip": "vault (played)",
        "license": VIMEO,
        "source_url": "https://vimeo.com/721018315",
        "source_window": "24.6–27.0 s of the unlicensed Vimeo film",
    },
    {
        "path": "Docs/Storror/out/json/15_cat_leap_wall.json",
        "category": "climb-plant",
        "ledger_clip": "climb",
        "license": YT,
        "source_url": "https://www.youtube.com/watch?v=QvvJ2t9iVyE&t=370s",
        "source_window": "370–378 s",
    },
    {
        "path": "Docs/Storror/out/json/07_wall_climb_traverse.json",
        "category": "climb-plant",
        "ledger_clip": "climb",
        "license": YT,
        "source_url": "https://www.youtube.com/watch?v=TCyhLQBBPmg&t=148s",
        "source_window": "148–154 s, Brighton wall run",
    },
    {
        "path": "Docs/Storror/out/json/06_run_walljump_climb_window.json",
        "category": "mantle",
        "ledger_clip": "exit-Mantle is the recovery; this is the climb onto the ledge",
        "license": YT,
        "source_url": "https://www.youtube.com/watch?v=TCyhLQBBPmg&t=173s",
        "source_window": "173.00–177.50 s, Brighton wall run",
    },
    {
        "path": "Docs/Storror/out/json/12_tictac_slanted_wall.json",
        "category": "wall-plant",
        "ledger_clip": "wall",
        "license": YT,
        "source_url": "https://www.youtube.com/watch?v=QvvJ2t9iVyE&t=290s",
        "source_window": "290.5–296.4 s",
    },
    {
        "path": "Docs/Storror/out/json/20_wallpop_180.json",
        "category": "wall-plant",
        "ledger_clip": "wall",
        "license": YT,
        "source_url": "https://www.youtube.com/watch?v=QvvJ2t9iVyE&t=62s",
        "source_window": "62.4–64.3 s",
    },
    {
        "path": "Docs/Storror/out/json/10_wallrun_slanted.json",
        "category": "wall-plant",
        "ledger_clip": "wall",
        "license": YT,
        "source_url": "https://www.youtube.com/watch?v=WGV_b8lPbyQ&t=136s",
        "source_window": "136.50–140.71 s, Brighton wallrun",
    },
    {
        "path": "Docs/Storror/out/json/17_slide_slope_crouch.json",
        "category": "slide",
        "ledger_clip": "slide (played). exit-Slide is the recovery, not this clip",
        "license": YT,
        "source_url": "https://www.youtube.com/watch?v=SqD-i_dTxws&t=13s",
        "source_window": "13.3–14.9 s, steep slope, not a flat slide",
    },
)

# Target times are seconds from the first sample. The nearest measured frame is used.
KEYS = (
    ("02_drop_roll_gravel", 0.10, "early fold", "Already crouched at the start of the window. Pelvis behind, both knees deep."),
    ("02_drop_roll_gravel", 0.50, "unfold", "Pelvis nearly over the feet as the roll opens."),
    ("02_drop_roll_gravel", 0.70, "brief sit", "Short sit while the roll continues."),
    ("02_drop_roll_gravel", 1.23, "extreme behind", "Largest pelvis-behind reading. Shin points backward. Treat as an inverted or tucked frame, not a clean plant."),
    ("02_drop_roll_gravel", 1.43, "pelvis in front", "Most negative pelvisBack in this window. A heap, not a sit."),
    ("02_drop_roll_gravel", 2.34, "late crouch", "Late frame with the pelvis behind and the knee still deep."),
    ("01_roll_grass", 0.57, "roll contact", "Pelvis is in front of the feet during the roll."),
    ("01_roll_grass", 0.80, "stand-up sit", "Pelvis behind as the body comes up."),
    ("01_roll_grass", 1.74, "late sit", "Pelvis well behind. Shin points backward."),
    ("24_landing_roll", 0.64, "deep knee", "Sparse track. Only a handful of frames measured. Do not treat this as a dense landing."),
    ("09_run_vault_park", 0.07, "run", "Approach. Knee nearly straight, shin trailing."),
    ("09_run_vault_park", 1.03, "pre-vault", "Pelvis close to the feet before the hands go down."),
    ("09_run_vault_park", 2.54, "over the block", "Pelvis near the feet, not an 8 cm sit. The other knee is deeper."),
    ("09_run_vault_park", 3.47, "before the land", "Pelvis in front of the feet just before the absorb."),
    ("09_run_vault_park", 3.67, "landing absorb", "Deep knee and the pelvis behind. This is the landing sit in the track."),
    ("08_vault_block_close", 0.23, "vault, pelvis in front", "The pelvis stays in front through the vault."),
    ("08_vault_block_close", 1.40, "still in front", "Still short of an 8 cm sit."),
    ("08_vault_block_close", 1.84, "after the vault", "First clear sit, and it is after the cross."),
    ("22_pike_vault", 0.28, "pike", "Pelvis well in front while the hips pike. Sparse track."),
    ("22_pike_vault", 0.52, "after the pike", "Pelvis comes behind. Knee is modest."),
    ("15_cat_leap_wall", 0.00, "approach", "Pelvis slightly in front."),
    ("15_cat_leap_wall", 0.67, "into the wall", "Pelvis behind, knee bent."),
    ("15_cat_leap_wall", 2.37, "cling", "Deep knee, pelvis still behind."),
    ("15_cat_leap_wall", 3.04, "drive", "Pelvis in front. Not a sit."),
    ("15_cat_leap_wall", 3.67, "high reach", "Large pelvis-behind number. Shin points backward. Check the stick before calling it a plant."),
    ("07_wall_climb_traverse", 1.90, "facade plant", "Pelvis behind, knee past 45°. Small figure."),
    ("07_wall_climb_traverse", 5.84, "later plant", "Pelvis behind and the knee is deep. Feet on a small figure are unreliable."),
    ("06_run_walljump_climb_window", 0.00, "run", "Pelvis in front on the approach."),
    ("06_run_walljump_climb_window", 1.00, "deepest knee", "Knee is the deepest in the window. Pelvis is barely behind."),
    ("06_run_walljump_climb_window", 1.43, "wall reach", "Drive into the wall. Pelvis in front."),
    ("06_run_walljump_climb_window", 2.37, "on the ledge", "Pelvis behind, knee bent."),
    ("06_run_walljump_climb_window", 3.44, "hip over", "Mantle. Pelvis well behind the support foot."),
    ("06_run_walljump_climb_window", 4.37, "stood up", "Exit. Modest sit, knee opening."),
    ("12_tictac_slanted_wall", 0.40, "early sit", "Pelvis behind, knee deep."),
    ("12_tictac_slanted_wall", 1.23, "kick, pelvis in front", "Track puts the pelvis in front. Same moment the retarget labeled not-a-plant."),
    ("12_tictac_slanted_wall", 1.43, "one-foot sit", "Pelvis behind on one foot."),
    ("12_tictac_slanted_wall", 2.54, "second not-a-plant", "Track pelvis is still in front. Do not rewrite the tic-tac keys from this number."),
    ("12_tictac_slanted_wall", 3.70, "landing sit", "Pelvis behind after the drop."),
    ("12_tictac_slanted_wall", 4.67, "prone heap", "Extreme pelvis-behind with the shin backward. The ledger already calls the late track a prone heap."),
    ("20_wallpop_180", 0.27, "plant, pelvis in front", "Foot on the wall. Pelvis is in front. Matches the retired S2 note on frames 8 and 15."),
    ("20_wallpop_180", 0.43, "still in front", "The pop has not sat the hips yet."),
    ("20_wallpop_180", 0.77, "pop land", "Pelvis behind and the knee is deep."),
    ("10_wallrun_slanted", 0.50, "drive stride", "Pelvis in front. A push, not a sit."),
    ("10_wallrun_slanted", 1.07, "stride sit", "Pelvis behind and the knee is bent. One readable plant."),
    ("10_wallrun_slanted", 2.04, "later drive", "Pelvis in front again."),
    ("17_slide_slope_crouch", 0.00, "entry", "Already crouched. One knee much deeper than the support knee. Steep slope, not a flat slide."),
    ("17_slide_slope_crouch", 0.32, "into the slope", "Pelvis crosses to about even with the feet."),
    ("17_slide_slope_crouch", 0.60, "deepest knee", "Support knee is the deepest in the window."),
    ("17_slide_slope_crouch", 0.83, "most behind", "Largest pelvis-behind reading on this slope."),
    ("17_slide_slope_crouch", 1.45, "exit start", "Knee opening. Pelvis nearly over the feet."),
    ("17_slide_slope_crouch", 1.55, "exit", "Pelvis behind again as the knee opens further."),
)


def mp(v):
    return np.array((v[0], -v[2], -v[1]), dtype=np.float64)


def flat_fwd(hip, left_shoulder, right_shoulder, nose):
    up = (left_shoulder + right_shoulder) * 0.5 - hip
    up_n = np.linalg.norm(up)
    up = np.array((0.0, 0.0, 1.0)) if up_n < 1e-6 else up / up_n
    right = right_shoulder - left_shoulder
    right = right - up * np.dot(right, up)
    right_n = np.linalg.norm(right)
    right = np.array((1.0, 0.0, 0.0)) if right_n < 1e-6 else right / right_n
    fwd = np.cross(up, right)
    if nose is not None and np.dot(fwd, nose - (left_shoulder + right_shoulder) * 0.5) < 0.0:
        fwd = -fwd
    flat = np.array((fwd[0], fwd[1], 0.0))
    flat_n = np.linalg.norm(flat)
    flat = np.array((0.0, -1.0, 0.0)) if flat_n < 1e-6 else flat / flat_n
    return flat


def angle_deg(a, b, c):
    u = a - b
    v = c - b
    n = np.linalg.norm(u) * np.linalg.norm(v)
    if n < 1e-8:
        return float("nan")
    coss = float(np.dot(u, v) / n)
    return math.degrees(math.acos(max(-1.0, min(1.0, coss))))


def visible(frame, vis, index, thresh):
    if frame is None or index >= len(frame) or frame[index] is None:
        return False
    if any(c is None for c in frame[index]):
        return False
    if vis is None or index >= len(vis) or vis[index] is None:
        return True
    return vis[index] >= thresh


def measure(frame, vis):
    need = (NOSE, LSH, RSH, LHP, RHP, LKN, RKN, LAN, RAN)
    if not all(visible(frame, vis, i, 0.25) for i in need):
        return None
    pts = []
    for i in range(len(frame)):
        if visible(frame, vis, i, 0.0):
            pts.append(mp(frame[i]))
        else:
            pts.append(None)
    hip = (pts[LHP] + pts[RHP]) * 0.5
    fwd = flat_fwd(hip, pts[LSH], pts[RSH], pts[NOSE])
    feet = {}
    for side, ankle, heel, toe in (("L", LAN, LHE, LTO), ("R", RAN, RHE, RTO)):
        parts = [pts[j] for j in (ankle, heel, toe) if pts[j] is not None and visible(frame, vis, j, 0.2)]
        if parts:
            feet[side] = sum(parts) / len(parts)
    if not feet:
        return None
    order = sorted(feet, key=lambda s: feet[s][2])
    low = feet[order[0]][2]
    support = [s for s in order if feet[s][2] <= low + 0.12]
    foot = sum((feet[s] for s in support), np.zeros(3)) / len(support)
    back = float(np.dot(foot - hip, fwd)) * 100.0
    knees = {}
    shins = {}
    for side, hip_i, knee_i, ankle_i in (("L", LHP, LKN, LAN), ("R", RHP, RKN, RAN)):
        knees[side] = 180.0 - angle_deg(pts[hip_i], pts[knee_i], pts[ankle_i])
        shins[side] = float(np.dot(pts[knee_i] - pts[ankle_i], fwd)) * 100.0
    support_knees = [knees[s] for s in support]
    support_shins = [shins[s] for s in support]
    return {
        "back": back,
        "knee": min(support_knees),
        "kneeMax": max(knees.values()),
        "shin": min(support_shins),
        "support": "".join(support),
        "pts": pts,
        "fwd": fwd,
        "foot": foot,
        "hip": hip,
    }


def r1(value):
    return round(float(value), 1)


def load_rows(spec):
    path = os.path.join(ROOT, spec["path"])
    data = json.load(open(path))
    t0 = data["times_s"][0]
    rows = []
    for i, usable in enumerate(data["usable"]):
        if not usable:
            continue
        measured = measure(data["world_xyz_m"][i], data["visibility"][i])
        if measured is None:
            continue
        measured["i"] = i
        measured["t"] = data["times_s"][i] - t0
        rows.append(measured)
    return data, rows


def nearest(rows, target_t):
    hit = min(rows, key=lambda row: abs(row["t"] - target_t))
    if abs(hit["t"] - target_t) > 0.08:
        raise SystemExit(f"no measured frame near t={target_t:.2f}, closest {hit['t']:.2f}")
    return hit


def draw_strip(clip_id, keys, path):
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 13)
    font_b = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 14)
    cell_w, cell_h = 250, 320
    header, footer = 28, 78
    img = Image.new("RGB", (cell_w * len(keys), cell_h), (22, 24, 28))
    draw = ImageDraw.Draw(img)
    for n, key in enumerate(keys):
        x0 = n * cell_w
        draw.rectangle((x0, 0, x0 + cell_w - 1, cell_h - 1), outline=(60, 64, 72))
        title = f"{key['label']}"
        draw.text((x0 + 8, 6), title, fill=(242, 242, 242), font=font_b)
        box = (x0 + 8, header, x0 + cell_w - 8, cell_h - footer)
        _draw_pose(draw, key, box)
        lines = (
            f"t={key['t_s']:.2f}s  i={key['frame']}",
            f"pelvisBack {key['pelvis_back_cm']:+.1f} cm",
            f"knee {key['knee_deg']:.0f}°  max {key['knee_max_deg']:.0f}°",
            f"shin {key['shin_forward_cm']:+.1f} cm  {key['support']}",
            "REFERENCE-ONLY",
        )
        y = cell_h - footer + 4
        for line in lines:
            draw.text((x0 + 8, y), line, fill=(210, 214, 220), font=font)
            y += 14
    img.save(path, optimize=True)
    return os.path.getsize(path)


def _draw_pose(draw, key, box):
    pts = key["_pts"]
    fwd = key["_fwd"]
    foot = key["_foot"]
    hip = key["_hip"]
    x0, y0, x1, y1 = box

    def project(p):
        return float(np.dot(p - foot, fwd)), float(p[2] - foot[2])

    samples = [project(p) for p in pts if p is not None]
    samples.append(project(hip))
    samples.append((0.0, 0.0))
    xs = [s[0] for s in samples]
    ys = [s[1] for s in samples]
    pad = 0.15
    min_x, max_x = min(xs) - pad, max(xs) + pad
    min_y, max_y = min(ys) - pad, max(ys) + pad
    span = max(max_x - min_x, max_y - min_y, 0.4)
    cx = (min_x + max_x) * 0.5
    cy = (min_y + max_y) * 0.5
    w, h = x1 - x0, y1 - y0
    scale = min(w, h) / span

    def pix(p):
        sx, sy = project(p)
        return (
            x0 + w * 0.5 + (sx - cx) * scale,
            y0 + h * 0.5 - (sy - cy) * scale,
        )

    # Vertical through the support foot. Camera-up, not gravity.
    foot_x = x0 + w * 0.5 + (0.0 - cx) * scale
    draw.line((foot_x, y0 + 4, foot_x, y1 - 4), fill=(90, 170, 210), width=1)
    left = {LSH, LEL, LWR, LHP, LKN, LAN, LHE, LTO}
    for a, b in BONES:
        if pts[a] is None or pts[b] is None:
            continue
        color = (232, 148, 74) if a in left or b in left else (120, 170, 230)
        if a in (LSH, RSH, LHP, RHP) and b in (LSH, RSH, LHP, RHP):
            color = (236, 236, 236)
        draw.line((*pix(pts[a]), *pix(pts[b])), fill=color, width=3)
    if pts[NOSE] is not None and pts[LSH] is not None and pts[RSH] is not None:
        mid = (pts[LSH] + pts[RSH]) * 0.5
        draw.line((*pix(mid), *pix(pts[NOSE])), fill=(236, 236, 236), width=2)
        nx, ny = pix(pts[NOSE])
        draw.ellipse((nx - 4, ny - 4, nx + 4, ny + 4), outline=(236, 236, 236))
    hx, hy = pix(hip)
    draw.ellipse((hx - 5, hy - 5, hx + 5, hy + 5), fill=(255, 122, 24))


def main():
    os.makedirs(OUT, exist_ok=True)
    by_id = {}
    catalog = []
    for spec in CLIPS:
        data, rows = load_rows(spec)
        clip_id = data["clip_id"]
        by_id[clip_id] = rows
        entry = {
            "clip_id": clip_id,
            "category": spec["category"],
            "ledger_clip": spec["ledger_clip"],
            "ship": "REFERENCE-ONLY",
            "license": spec["license"],
            "source_url": spec["source_url"],
            "source_window": spec["source_window"],
            "track": spec["path"],
            "fps": data.get("fps"),
            "frame_count": len(data["times_s"]),
            "measured_count": len(rows),
            "frame": [row["i"] for row in rows],
            "time_s": [round(row["t"], 3) for row in rows],
            "pelvis_back_cm": [r1(row["back"]) for row in rows],
            "knee_deg": [r1(row["knee"]) for row in rows],
            "knee_max_deg": [r1(row["kneeMax"]) for row in rows],
            "shin_forward_cm": [r1(row["shin"]) for row in rows],
            "support": [row["support"] for row in rows],
            "keys": [],
        }
        catalog.append(entry)
        print(
            f"{clip_id} measured={len(rows)}/{entry['frame_count']} "
            f"back {min(entry['pelvis_back_cm']):+.1f}..{max(entry['pelvis_back_cm']):+.1f}"
        )

    grouped = {entry["clip_id"]: entry for entry in catalog}
    for clip_id, target_t, label, note in KEYS:
        row = nearest(by_id[clip_id], target_t)
        key = {
            "label": label,
            "note": note,
            "frame": row["i"],
            "t_s": round(row["t"], 3),
            "pelvis_back_cm": r1(row["back"]),
            "knee_deg": r1(row["knee"]),
            "knee_max_deg": r1(row["kneeMax"]),
            "shin_forward_cm": r1(row["shin"]),
            "support": row["support"],
            "_pts": row["pts"],
            "_fwd": row["fwd"],
            "_foot": row["foot"],
            "_hip": row["hip"],
        }
        grouped[clip_id]["keys"].append(key)
        print(
            f"  {clip_id} {label} i={row['i']} t={row['t']:.2f} "
            f"back={row['back']:+.1f} knee={row['knee']:.1f} shin={row['shin']:+.1f} {row['support']}"
        )

    for entry in catalog:
        still = f"{entry['clip_id']}_side.png"
        size = draw_strip(entry["clip_id"], entry["keys"], os.path.join(OUT, still))
        entry["side_still"] = f"Docs/Movement/hipref/{still}"
        entry["side_still_bytes"] = size
        for key in entry["keys"]:
            del key["_pts"]
            del key["_fwd"]
            del key["_foot"]
            del key["_hip"]

    payload = {
        "purpose": (
            "Reference measurements for the movement lead on cursor/tag-movement. "
            "The ledger was read and was not edited."
        ),
        "ledger_source": "origin/cursor/tag-movement:Docs/Movement/LEDGER.md",
        "terminal_note": (
            "The ledger lists terminal 56.16 and a roll at 65% of terminal. "
            "That threshold is about 36.5 in game units. These tracks are monocular. "
            "They do not measure that speed. The roll windows are high drops into a roll, not a verified 36.5 fall."
        ),
        "method": (
            "MediaPipe world (x right, y down) is converted to (x, -z, -y) so the third axis is camera-up, not gravity. "
            "Facing is the flat shoulder-nose direction. Support is the lower foot, plus any foot within 12 cm. "
            "pelvisBack_cm is (foot - hip) dot facing, in centimetres. Positive means the pelvis is behind the support foot. "
            "Knee is 180 minus the angle at the support knee. A frame needs visibility at least 0.25 on nose, shoulders, hips, knees, and ankles, and the usable flag."
        ),
        "hip_rule": (
            "Plants and landings want pelvisBack at least 8 cm, or 12 cm in a crouch. "
            "Knee flex at least 25° on a plant and 45° on a landing. "
            "A negative pelvisBack in the footage is labeled not-a-plant. It is not a retarget error."
        ),
        "ship": "REFERENCE-ONLY",
        "cc0_or_public_domain": (
            "No CC0 or public-domain live parkour clip was verified for these moves. "
            "The PD-self dive-roll GIF in REFERENCE.md is a 128x96 animation, not one of these tracks."
        ),
        "clips": catalog,
    }
    out_path = os.path.join(OUT, "measurements.json")
    with open(out_path, "w") as handle:
        json.dump(payload, handle, indent=2)
        handle.write("\n")
    print("wrote", out_path)


if __name__ == "__main__":
    main()
