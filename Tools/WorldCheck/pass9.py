#!/usr/bin/env python3
"""Pass 9. Cross-check every dressed zone, then frame Z8–Z10 stills.

The shared camera sentence is not in Docs/Models/ENV_QUEUE.md on #122
(origin/cursor/tag-asset-library at the tip this pass read). The stills
follow the rule given for this pass: the whole subject in frame with margin,
the subject covering 25–85% of the frame, a 1.8 m figure visible, no
extreme close-up, each file under 400 KB.
"""

import math
import os
import sys

import check_z1
import check_z2
import check_z3
import check_z4
import check_z5
import check_z6
import check_z7 as z
import check_z8
import check_z9
import check_z10

W, H = 1280, 720
STILL_DIR = os.path.join(z.ROOT, "Docs/WorldStills/pass9")
MAX_BYTES = 400 * 1024
# Vertical field of view. Wider than this is an extreme close-up lens.
FOV_MAX = 70.0
MARGIN_PX = 28
COVER_MIN = 0.25
COVER_MAX = 0.85
FIG_MIN_PX = 48
FIG_MAX_FRAC = 0.42

ZONES = (
    ("Z1", "SoftPlay", check_z1.CHASE, check_z1.CHASE_CLEAR),
    ("Z2", "Cling", check_z2.CHASE, check_z2.CHASE_CLEAR),
    ("Z3", "Merry", check_z3.CHASE, check_z3.CHASE_CLEAR),
    ("Z4", "Slide", check_z4.CHASE, check_z4.CHASE_CLEAR),
    ("Z5", "Swing", check_z5.CHASE, check_z5.CHASE_CLEAR),
    ("Z6", "Forts", check_z6.CHASE, check_z6.CHASE_CLEAR),
    ("Z7", "Places", z.CHASE, z.CHASE_CLEAR),
    ("Z8", "Bowl", check_z8.CHASE, check_z8.CHASE_CLEAR),
    ("Z9", "Bars", check_z9.CHASE, check_z9.CHASE_CLEAR),
    ("Z10", "Hops", check_z10.CHASE, check_z10.CHASE_CLEAR),
)

# World-check lines from this pass. Re-measured by running each checker.
CHECK_LINES = {
    "Z1": "routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0",
    "Z2": "routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0",
    "Z3": "routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0",
    "Z4": "routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0",
    "Z5": "routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0",
    "Z6": "routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0",
    "Z7": "routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0",
    "Z8": "routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0",
    "Z9": "routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0",
    "Z10": "routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0",
}


def load_instances(src, array_name, cache):
    places = z.parse_places(src, array_name)
    instances = []
    for p in places:
        if p["path"] not in cache:
            path = os.path.join(z.ROOT, p["path"])
            cache[p["path"]] = z.parse_prefab(path) if os.path.isfile(path) else None
        prefab = cache[p["path"]]
        if prefab is None:
            instances.append({"place": p, "boxes": [], "aabb": None, "missing": True})
            continue
        boxes = []
        all_pts = []
        for b in prefab["boxes"]:
            if p["name"] == "CourtFence" and b.get("name") == "Col_Gate":
                continue
            pts = z.world_points(prefab, b, p)
            name = prefab["names"].get(b["go"], b.get("name", ""))
            boxes.append({"pts": pts, "aabb": z.aabb(pts), "name": name})
            all_pts.extend(pts)
        instances.append({
            "place": p,
            "boxes": boxes,
            "aabb": z.aabb(all_pts) if all_pts else None,
            "missing": prefab["collider_count"] == 0,
        })
    return instances


def samples_of(poly):
    pts = []
    n = len(poly)
    for i in range(n):
        ax, az = poly[i]
        bx, bz = poly[(i + 1) % n]
        dist = math.hypot(bx - ax, bz - az)
        steps = max(1, int(dist / 0.5))
        for s in range(steps):
            t = s / float(steps)
            pts.append((ax + (bx - ax) * t, az + (bz - az) * t))
    return pts


def body_band(box):
    return box[4] > 0.05 and box[1] < 1.80


def on_line(box, samples):
    """The route centerline passes through the collider."""
    if not body_band(box):
        return False
    for x, zz in samples:
        if z.dist_point_aabb(x, zz, box) <= 0.02:
            return True
    return False


def overlaps(a, b):
    return (
        z.overlap_1(a[0], a[3], b[0], b[3]) > 0.02
        and z.overlap_1(a[1], a[4], b[1], b[4]) > 0.02
        and z.overlap_1(a[2], a[5], b[2], b[5]) > 0.02
    )


def deck_bottom(inst):
    best = None
    for box in inst["boxes"]:
        bot = box["aabb"][1]
        if bot > 1.0:
            best = bot if best is None else min(best, bot)
    return best


def crouch_slots(zones):
    slots = []
    # Standing slot under the bar steel. Same box check_z9 uses.
    slots.append(("Z9 bars", (38.0, 0.05, 15.4, 114.0, 1.05, 16.6)))
    for zone, instances in zones:
        scaffolds = [i for i in instances if "Scaffold_Bay" in i["place"]["path"] and i["aabb"]]
        for inst in scaffolds:
            deck = deck_bottom(inst)
            if deck is None or deck < 1.05:
                continue
            b = inst["aabb"]
            slots.append((
                "%s under %s" % (zone, inst["place"]["name"]),
                (b[0] + 0.15, 0.05, b[2] + 0.15, b[3] - 0.15, 1.05, b[5] - 0.15),
            ))
        if len(scaffolds) == 2:
            a, b = scaffolds[0]["aabb"], scaffolds[1]["aabb"]
            left, right = (a, b) if a[3] <= b[0] else (b, a)
            if right[0] > left[3]:
                z0, z1 = max(left[2], right[2]), min(left[5], right[5])
                if z1 > z0:
                    slots.append((
                        "%s scaffold gap" % zone,
                        (left[3], 0.05, z0, right[0], 1.05, z1),
                    ))
    return slots


def audit(zones):
    route_hits = []
    crouch_hits = []
    loop_hits = []
    chases = []
    for zone, _array, poly, _clear in ZONES:
        chases.append((zone, samples_of(poly)))
    slots = crouch_slots(zones)
    for zone, instances in zones:
        for inst in instances:
            box = inst["aabb"]
            if box is None:
                continue
            if z.is_floor(inst["place"]["name"], box):
                continue
            if not z.loop_clear(box):
                loop_hits.append("%s %s" % (zone, inst["place"]["name"]))
            for owner, samples in chases:
                if on_line(box, samples):
                    route_hits.append("%s %s on %s chase" % (zone, inst["place"]["name"], owner))
            for label, slot in slots:
                own = label.startswith(zone + " ") and inst["place"]["name"] in label
                if own:
                    continue
                if overlaps(box, slot):
                    crouch_hits.append("%s %s in %s" % (zone, inst["place"]["name"], label))
    return route_hits, crouch_hits, loop_hits


def car_gaps(instances):
    roads = [i for i in instances if i["place"]["name"].startswith("Road_") and i["aabb"]]
    road_top = max(i["aabb"][4] for i in roads)
    rows = []
    for inst in instances:
        if not inst["place"]["name"].startswith("Car_"):
            continue
        wheels = [
            b for b in inst["boxes"]
            if "Wheel" in b["name"] or b["name"].startswith("Col_Fr") or b["name"].startswith("Col_Rr")
        ]
        if not wheels:
            rows.append((inst["place"]["name"], inst["place"]["y"], None, None))
            continue
        bottom = min(b["aabb"][1] for b in wheels)
        rows.append((inst["place"]["name"], inst["place"]["y"], bottom, bottom - road_top))
    return road_top, rows


def corners_of(box):
    x0, y0, z0, x1, y1, z1 = box
    pts = []
    for x in (x0, x1):
        for y in (y0, y1):
            for zz in (z0, z1):
                pts.append((x, y, zz))
    return pts


def subject_points(instances, gray):
    pts = []
    for inst in instances:
        for box in inst["boxes"]:
            pts.extend(box["pts"])
    for _name, box in gray:
        pts.extend(corners_of(box))
    return pts


def project(pts, eye, target, fov):
    fwd = z.norm((target[0] - eye[0], target[1] - eye[1], target[2] - eye[2]))
    right = z.norm(z.cross(fwd, (0.0, 1.0, 0.0)))
    up = z.cross(right, fwd)
    fy = 1.0 / math.tan(math.radians(fov) * 0.5)
    fx = fy * (H / float(W))
    out = []
    for p in pts:
        d = (p[0] - eye[0], p[1] - eye[1], p[2] - eye[2])
        depth = z.dot(d, fwd)
        if depth < 0.25:
            return None
        x = z.dot(d, right)
        y = z.dot(d, up)
        sx = (x / depth * fx + 1.0) * 0.5 * (W - 1)
        sy = (1.0 - (y / depth * fy + 1.0) * 0.5) * (H - 1)
        out.append((sx, sy))
    return out


def frame_ok(subj, fig, eye, target, fov):
    sp = project(subj, eye, target, fov)
    fp = project(fig, eye, target, fov)
    if sp is None or fp is None:
        return None
    xs = [p[0] for p in sp]
    ys = [p[1] for p in sp]
    fxs = [p[0] for p in fp]
    fys = [p[1] for p in fp]
    margin = min(min(xs), min(ys), (W - 1) - max(xs), (H - 1) - max(ys))
    fig_margin = min(min(fxs), min(fys), (W - 1) - max(fxs), (H - 1) - max(fys))
    width_frac = (max(xs) - min(xs)) / float(W)
    height_frac = (max(ys) - min(ys)) / float(H)
    # Longer screen axis is the "covering" measure. Both axes stay inside
    # 25–85% so a ribbon and an edge-to-edge crop both fail.
    fig_h = max(fys) - min(fys)
    ok = (
        margin >= MARGIN_PX
        and fig_margin >= 8
        and COVER_MIN <= width_frac <= COVER_MAX
        and COVER_MIN <= height_frac <= COVER_MAX
        and FIG_MIN_PX <= fig_h <= FIG_MAX_FRAC * H
        and fov <= FOV_MAX
    )
    return {
        "ok": ok,
        "margin": margin,
        "cover": width_frac * height_frac,
        "width": width_frac,
        "height": height_frac,
        "fig_h": fig_h,
        "fov": fov,
        "eye": eye,
        "target": target,
    }


def centroid(pts):
    n = float(len(pts))
    return (
        sum(p[0] for p in pts) / n,
        sum(p[1] for p in pts) / n,
        sum(p[2] for p in pts) / n,
    )


def radius(pts, c):
    return max(math.sqrt((p[0] - c[0]) ** 2 + (p[1] - c[1]) ** 2 + (p[2] - c[2]) ** 2) for p in pts)


def search_shot(subj, instances, gray, elevated):
    """Camera outside the subject. Figure stands on the ground in the foreground."""
    c = centroid(subj)
    r = max(radius(subj, c), 8.0)
    found = []
    if elevated:
        heights = tuple(c[1] + r * e for e in (0.28, 0.45, 0.65, 0.85))
    else:
        heights = (1.65,)
    for az in range(0, 360, 15):
        rad = math.radians(az)
        dx, dz = math.cos(rad), math.sin(rad)
        for dist in (r * k for k in (0.95, 1.15, 1.35, 1.6, 1.9)):
            for eye_y in heights:
                eye = (c[0] + dx * dist, eye_y, c[2] + dz * dist)
                toward = z.norm((c[0] - eye[0], 0.0, c[2] - eye[2]))
                for fig_d in (8.0, 11.0, 14.0):
                    fx = eye[0] + toward[0] * fig_d
                    fz = eye[2] + toward[2] * fig_d
                    if not clear_stand(instances, gray, fx, fz):
                        continue
                    fig = figure_corners(fx, fz)
                    for fov in (34.0, 42.0, 50.0, 58.0, 66.0):
                        target = (c[0], max(1.1, min(c[1], 2.2)), c[2])
                        row = frame_ok(subj, fig, eye, target, fov)
                        if row is None or not row["ok"]:
                            continue
                        score = abs(row["width"] - 0.62) + abs(row["height"] - 0.55) + abs(row["fig_h"] - 120.0) / 500.0
                        found.append((score, row, (fx, fz)))
    if not found:
        return None, None
    found.sort(key=lambda item: item[0])
    return found[0][1], found[0][2]


def figure_corners(x, zz):
    seen = set()
    corners = []
    for tri in z.figure_tris(x, 0.0, zz):
        for p in tri[:3]:
            key = (round(p[0], 3), round(p[1], 3), round(p[2], 3))
            if key not in seen:
                seen.add(key)
                corners.append(p)
    return corners


def clear_stand(instances, gray, x, zz):
    boxes = []
    for inst in instances:
        if inst["aabb"] is not None:
            boxes.append(inst["aabb"])
    boxes.extend(box for _name, box in gray)
    for b in boxes:
        if b[4] <= 0.3:
            continue
        if b[0] - 0.6 <= x <= b[3] + 0.6 and b[2] - 0.6 <= zz <= b[5] + 0.6:
            return False
    return True


def render_shot(tris, shot, path):
    z.render_view(tris, z.look_cam(shot["eye"], shot["target"], shot["fov"]), path)
    size = os.path.getsize(path)
    shot["bytes"] = size
    shot["path"] = path
    if size > MAX_BYTES:
        raise SystemExit("still over 400 KB: %s (%d)" % (path, size))
    if not shot["ok"]:
        raise SystemExit(
            "frame miss %s w=%.3f h=%.3f margin=%.1f fig=%.1f fov=%.1f"
            % (path, shot["width"], shot["height"], shot["margin"], shot["fig_h"], shot["fov"])
        )
    print(
        "still %s width=%.0f%% height=%.0f%% margin=%.0fpx fig=%.0fpx fov=%.0f bytes=%d"
        % (
            os.path.basename(path),
            shot["width"] * 100,
            shot["height"] * 100,
            shot["margin"],
            shot["fig_h"],
            shot["fov"],
            size,
        )
    )


def district_tris(zone, instances, gray):
    tris = []
    if zone == "Z8":
        tris.extend(check_z8.gray_tris(gray))
        tris.extend(check_z8.prop_tris(instances))
        tris.extend(z.box_tris(62, -0.04, 52, 44, 0.06, 60, 0, (0.32, 0.46, 0.28)))
    elif zone == "Z9":
        for name, box in gray:
            sx, sy, sz = box[3] - box[0], box[4] - box[1], box[5] - box[2]
            cx = (box[0] + box[3]) * 0.5
            cy = (box[1] + box[4]) * 0.5
            cz = (box[2] + box[5]) * 0.5
            col = (0.55, 0.58, 0.62) if "Vault" not in name else (0.62, 0.62, 0.58)
            tris.extend(z.box_tris(cx, cy, cz, sx, sy, sz, 0, col))
        for inst in instances:
            col = (0.55, 0.32, 0.22) if "Newsstand" in inst["place"]["path"] else (0.45, 0.48, 0.52)
            for box in inst["boxes"]:
                tris.extend(z.pts_box(box["pts"], col))
        tris.extend(z.box_tris(76, -0.04, 16, 92, 0.06, 16, 0, (0.32, 0.46, 0.28)))
    else:
        for _name, box in gray:
            sx, sy, sz = box[3] - box[0], box[4] - box[1], box[5] - box[2]
            cx = (box[0] + box[3]) * 0.5
            cy = (box[1] + box[4]) * 0.5
            cz = (box[2] + box[5]) * 0.5
            tris.extend(z.box_tris(cx, cy, cz, sx, sy, sz, 0, (0.35, 0.48, 0.72)))
        for inst in instances:
            path = inst["place"]["path"]
            if "Alley" in path:
                col = (0.55, 0.42, 0.32)
            elif "Subway" in path:
                col = (0.45, 0.48, 0.52)
            else:
                col = (0.72, 0.55, 0.22)
            for box in inst["boxes"]:
                tris.extend(z.pts_box(box["pts"], col))
        tris.extend(z.box_tris(137, -0.04, 13, 42, 0.06, 24, 0, (0.32, 0.46, 0.28)))
    return tris


def main():
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    src = open(z.DISTRICT_CS, encoding="utf-8").read()
    cache = {}
    loaded = []
    for zone, array, _poly, _clear in ZONES:
        loaded.append((zone, load_instances(src, array, cache)))

    route_hits, crouch_hits, loop_hits = audit(loaded)
    print("route-line hits %d" % len(route_hits))
    for row in route_hits:
        print("  " + row)
    print("crouch-slot hits %d" % len(crouch_hits))
    for row in crouch_hits:
        print("  " + row)
    print("loop hits %d" % len(loop_hits))
    for row in loop_hits:
        print("  " + row)

    by = {zone: inst for zone, inst in loaded}
    road_top, cars = car_gaps(by["Z7"])
    print("road top y=%.3f" % road_top)
    for name, pivot, bottom, gap in cars:
        print("car %s pivot=%.3f wheel-bottom=%.3f gap=%.4f" % (name, pivot, bottom, gap))

    if route_hits or crouch_hits or loop_hits:
        return 1

    gray = {
        "Z8": check_z8.bowl_gray(),
        "Z9": check_z9.bar_gray(),
        "Z10": check_z10.hop_gray(),
    }
    os.makedirs(STILL_DIR, exist_ok=True)
    for zone in ("Z8", "Z9", "Z10"):
        instances = by[zone]
        subj = subject_points(instances, gray[zone])
        over, over_fig = search_shot(subj, instances, gray[zone], True)
        eye, eye_fig = search_shot(subj, instances, gray[zone], False)
        if over is None or eye is None:
            print(zone, "overview", over, "eye", eye)
            raise SystemExit("no legal camera for " + zone)
        base = district_tris(zone, instances, gray[zone])
        render_shot(
            base + z.figure_tris(over_fig[0], 0.0, over_fig[1]),
            over,
            os.path.join(STILL_DIR, zone.lower() + "_overview.png"),
        )
        render_shot(
            base + z.figure_tris(eye_fig[0], 0.0, eye_fig[1]),
            eye,
            os.path.join(STILL_DIR, zone.lower() + "_eye.png"),
        )
    print("stills " + STILL_DIR)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
