#!/usr/bin/env python3
"""Headless world check for the Z10 hopscotch.

Gray hops stay uncovered. The alley back wall jumps west onto the subway
roof. The rope crosses the hops. Barricades on the south chalk are the dash.
"""

import math
import os
import sys

import check_z6 as c6
import check_z7 as z

CHASE = [(128.65, 12.3), (149.6, 12.3), (149.6, 21.35), (128.65, 21.35)]
CHASE_CLEAR = 0.80
STILL_DIR = os.path.join(z.ROOT, "Docs/WorldStills/pass8")
LANDMARK = (150.4, 5.2)
BANNED = (
    "Restroom.prefab",
    "Playground.prefab",
    "Newsstand_Corner.prefab",
    "FireEscape.prefab",
    "Gazebo.prefab",
    "Container_20.prefab",
    "Dock_Straight.prefab",
    "Brick_Wall.prefab",
)


def box_of(x, y, zc, sx, sy, sz):
    return (x - sx * 0.5, y - sy * 0.5, zc - sz * 0.5, x + sx * 0.5, y + sy * 0.5, zc + sz * 0.5)


def hop_gray():
    rows = []
    for i in range(8):
        x = 124.0 + i * 3.1
        lip = 0.72 + (i % 4) * 0.22
        rows.append(("Hop_%d" % i, box_of(x, lip * 0.5, 15.0, 1.15, lip, 1.15)))
    return rows


def overlaps(a, b):
    return (
        z.overlap_1(a[0], a[3], b[0], b[3]) > 0.02
        and z.overlap_1(a[1], a[4], b[1], b[4]) > 0.02
        and z.overlap_1(a[2], a[5], b[2], b[5]) > 0.02
    )


def named_box(inst, name):
    for box in inst["boxes"]:
        if box.get("name") == name:
            return box["aabb"]
    return None


def roof_bottom(inst):
    best = None
    for box in inst["boxes"]:
        bot = box["aabb"][1]
        if bot > 1.0:
            best = bot if best is None else min(best, bot)
    return best if best is not None else inst["aabb"][4]


def main():
    os.chdir(z.ROOT)
    src = open(z.DISTRICT_CS, encoding="utf-8").read()
    places = z.parse_places(src, "Hops")
    z7 = z.parse_places(src, "Places")
    forts = z.parse_places(src, "Forts")
    gray = hop_gray()
    cache = {}
    instances = []
    missing = 0
    scale_fails = 0
    floating = []
    open_hits = []
    loop_hits = []
    hop_hits = []
    cover = []
    report = []
    repeated = []

    def load(p):
        if p["path"] not in cache:
            path = os.path.join(z.ROOT, p["path"])
            cache[p["path"]] = z.parse_prefab(path) if os.path.isfile(path) else None
        prefab = cache[p["path"]]
        if prefab is None or prefab["collider_count"] == 0:
            return None
        boxes = []
        all_pts = []
        for b in prefab["boxes"]:
            pts = z.world_points(prefab, b, p)
            boxes.append({"pts": pts, "aabb": z.aabb(pts), "name": prefab["names"].get(b["go"], "")})
            all_pts.extend(pts)
        return {"place": p, "prefab": prefab, "boxes": boxes, "aabb": z.aabb(all_pts)}

    for p in places:
        if abs(p["scale"][0] - 1) > 0.001:
            scale_fails += 1
        base = os.path.basename(p["path"])
        if base in BANNED:
            repeated.append(p["name"] + " " + base)
        inst = load(p)
        if inst is None:
            missing += 1
            continue
        instances.append(inst)

    fort_boxes = []
    for p in forts:
        inst = load(p)
        if inst is not None:
            fort_boxes.append((p["name"], inst["aabb"]))

    aabbs = [inst["aabb"] for inst in instances]
    for i, inst in enumerate(instances):
        rest = [aabbs[j] for j in range(len(aabbs)) if j != i]
        if not z.pivot_supported(inst, rest):
            floating.append(inst["place"]["name"])
        if z.hits_open(inst["aabb"]):
            open_hits.append(inst["place"]["name"])
        if not z.loop_clear(inst["aabb"]):
            loop_hits.append(inst["place"]["name"])
        if z.dist_point_aabb(LANDMARK[0], LANDMARK[1], inst["aabb"]) < 1.2:
            cover.append(inst["place"]["name"] + " covers landmark")
        for name, box in gray:
            if overlaps(inst["aabb"], box):
                hop_hits.append(inst["place"]["name"] + " overlaps " + name)
        for name, box in fort_boxes:
            if overlaps(inst["aabb"], box):
                cover.append(inst["place"]["name"] + " overlaps " + name)

    by = {inst["place"]["name"]: inst for inst in instances}
    routes = []

    def add_route(name, ok, detail):
        routes.append((name, ok, detail))
        report.append(("OK  " if ok else "FAIL") + " " + name + " " + detail)

    if "Hp_Alley" in by and "Hp_Subway" in by:
        backs = [named_box(by["Hp_Alley"], n) for n in ("Climb_BackL", "Climb_BackR")]
        backs = [b for b in backs if b is not None]
        roof = named_box(by["Hp_Subway"], "Col_Roof")
        blockers = []
        for inst in instances:
            for box in inst["boxes"]:
                if inst["place"]["name"] == "Hp_Alley" and box.get("name") in ("Climb_BackL", "Climb_BackR"):
                    continue
                if inst["place"]["name"] == "Hp_Subway" and box.get("name") == "Col_Roof":
                    continue
                blockers.append(box["aabb"])
        for name, box in gray:
            blockers.append(box)
        arc_ok, arc_detail = (False, "no roof")
        deck_gap = -1
        wall_h = 0
        run_len = 0
        if backs and roof is not None:
            face = min(b[0] for b in backs)
            spans = [(b[2], b[5]) for b in backs]
            wall_h = max(b[4] for b in backs) - min(b[1] for b in backs)
            run_len = sum(b[5] - b[2] for b in backs)
            arc_ok, arc_detail = c6.off_wall_west(face, spans, roof, blockers)
            deck_gap = face - roof[3]
        ok = (
            backs
            and by["Hp_Alley"]["prefab"]["climbable"]
            and wall_h >= 2.0
            and deck_gap > 0.5
            and abs(deck_gap - 4.10) > 0.15
            and abs(deck_gap - 3.60) > 0.15
            and arc_ok
            and not repeated
        )
        add_route(
            "StairClimb",
            ok,
            "west jump wall %.2f m deck gap %.2f m; %s" % (wall_h, deck_gap, arc_detail),
        )
    else:
        add_route("StairClimb", False, "missing alley or subway")

    hop = None
    for name, box in gray:
        if name == "Hop_5":
            hop = box
    lip = hop[4] if hop is not None else 0
    south_open = True
    if hop is not None:
        for inst in instances:
            b = inst["aabb"]
            if b[5] > hop[2] - 1.2 and b[2] < hop[2] and z.overlap_1(b[0], b[3], hop[0], hop[3]) > 0.2:
                south_open = False
    add_route(
        "HopVault",
        hop is not None and z.VAULT_MIN <= lip <= z.VAULT_MAX and south_open and not hop_hits,
        "hop lip %.2f m (vault band %.2f–%.2f) south approach %s"
        % (lip, z.VAULT_MIN, z.VAULT_MAX, "open" if south_open else "blocked"),
    )

    if "Hp_BarA" in by and "Hp_Alley" in by:
        bar = by["Hp_BarA"]["aabb"]
        header = named_box(by["Hp_Alley"], "Col_Header")
        if header is None:
            add_route("HopGrapple", False, "missing header")
        else:
            origin = ((bar[0] + bar[3]) * 0.5, 1.4, (bar[2] + bar[5]) * 0.5)
            cornice = ((header[0] + header[3]) * 0.5, header[4], (header[2] + header[5]) * 0.5)
            gd = math.dist(origin, cornice)
            crosses = origin[2] < 14.4 and cornice[2] > 15.6
            add_route(
                "HopGrapple",
                z.GRAPPLE_MIN <= gd <= z.GRAPPLE_MAX and crosses,
                "rope %.2f m from the south chalk over the hops to the alley header crosses %s"
                % (gd, "yes" if crosses else "NO"),
            )
    else:
        add_route("HopGrapple", False, "missing barricade or alley")

    if "Hp_BarA" in by and "Hp_BarB" in by:
        a = by["Hp_BarA"]["aabb"]
        b = by["Hp_BarB"]["aabb"]
        gap = b[0] - a[3] if a[3] <= b[0] else a[0] - b[3]
        clear_a = roof_bottom(by["Hp_BarA"])
        clear_b = roof_bottom(by["Hp_BarB"])
        ok = clear_a >= z.CROUCH_H and clear_b >= z.CROUCH_H and 0.4 <= gap <= z.AIR_DASH + 0.02
        add_route(
            "ChalkDash",
            ok,
            "rail underside %.2f / %.2f m gap %.2f m (dash %.2f)"
            % (clear_a, clear_b, gap, z.AIR_DASH),
        )
    else:
        add_route("ChalkDash", False, "missing barricades")

    named = [(inst["place"]["name"], inst["aabb"]) for inst in instances]
    named.extend(("gray:" + name, box) for name, box in gray)
    named.extend(fort_boxes)
    samples = []
    for i in range(len(CHASE)):
        ax, az = CHASE[i]
        bx, bz = CHASE[(i + 1) % len(CHASE)]
        dist = math.hypot(bx - ax, bz - az)
        steps = max(1, int(dist / 0.5))
        for s in range(steps):
            t = s / float(steps)
            samples.append((ax + (bx - ax) * t, az + (bz - az) * t))
    worst = 99.0
    worst_at = (0.0, 0.0, "")
    blocked = 0
    for x, zz in samples:
        for name, box in named:
            d = z.dist_point_aabb(x, zz, box)
            if d < worst:
                worst = d
                worst_at = (x, zz, name)
            if d < CHASE_CLEAR:
                blocked += 1
                break
    length = sum(
        math.hypot(CHASE[(i + 1) % len(CHASE)][0] - CHASE[i][0], CHASE[(i + 1) % len(CHASE)][1] - CHASE[i][1])
        for i in range(len(CHASE))
    )
    add_route(
        "HopChase",
        blocked == 0 and length > 40.0 and worst >= CHASE_CLEAR and not hop_hits,
        "length %.1f m clearance %.2f m at (%.1f, %.1f) vs %s blocked %d"
        % (length, worst, worst_at[0], worst_at[1], worst_at[2], blocked),
    )

    reachable = sum(1 for _, ok, _ in routes if ok)
    n = len(routes)
    z7_unique = len({p["path"] for p in z7})
    smaller = len(instances) < len(z7) and len({inst["place"]["path"] for inst in instances}) < z7_unique
    boot = open(os.path.join(z.ROOT, "Assets/Scripts/Level/MegaParkP1Bootstrap.cs"), encoding="utf-8").read()
    batch_ok = 'BuildDistrict("WorldZ10", MegaParkWorldDistrict.Hops, table, true)' in boot
    line = "world-check routes=%d reachable=%d/%d floatingProps=%d missingColliders=%d scaleFails=%d" % (
        n, reachable, n, len(floating), missing, scale_fails,
    )
    print(line)
    print("instances %d unique %d" % (len(instances), len({inst["place"]["path"] for inst in instances})))
    print("smaller-than-z7 %s static-batch %s" % ("yes" if smaller else "NO", "yes" if batch_ok else "NO"))
    if floating:
        print("floating: " + ", ".join(floating))
    if loop_hits:
        print("472 m loop: " + ", ".join(loop_hits))
    if hop_hits:
        print("hops: " + ", ".join(hop_hits))
    if cover:
        print("covers locked: " + ", ".join(cover))
    if repeated:
        print("repeated set: " + ", ".join(repeated))
    for row in report:
        print(row)
    print("--- bounds ---")
    for inst in instances:
        b = inst["aabb"]
        print("%-16s x[%.2f, %.2f] y[%.2f, %.2f] z[%.2f, %.2f]" % (
            inst["place"]["name"], b[0], b[3], b[1], b[4], b[2], b[5]))
    if "--stills" in sys.argv:
        write_stills(instances, gray)
    ok = (
        reachable == n and not floating and missing == 0 and scale_fails == 0
        and not open_hits and not loop_hits and not hop_hits and not cover
        and not repeated and smaller and batch_ok
    )
    return 0 if ok else 1


def write_stills(instances, gray):
    os.makedirs(STILL_DIR, exist_ok=True)
    tris_g = []
    for name, box in gray:
        sx, sy, sz = box[3] - box[0], box[4] - box[1], box[5] - box[2]
        cx, cy, cz = (box[0] + box[3]) * 0.5, (box[1] + box[4]) * 0.5, (box[2] + box[5]) * 0.5
        tris_g.extend(z.box_tris(cx, cy, cz, sx, sy, sz, 0, (0.35, 0.48, 0.72)))
    tris_p = []
    for inst in instances:
        path = inst["place"]["path"]
        if "Alley" in path:
            col = (0.55, 0.42, 0.32)
        elif "Subway" in path:
            col = (0.45, 0.48, 0.52)
        else:
            col = (0.72, 0.55, 0.22)
        for box in inst["boxes"]:
            tris_p.extend(z.pts_box(box["pts"], col))
    ground = z.box_tris(138, -0.05, 14, 40, 0.08, 24, 0, (0.34, 0.46, 0.28))
    view = ("ortho", 124.0, 156.0, 8.0, 23.0)
    eye = z.look_cam((128.0, 1.7, 16.0), (138.0, 1.3, 19.0), 50.0)
    fig = z.figure_tris(138.5, 0.0, 17.2)
    z.render_view(ground + tris_g, view, os.path.join(STILL_DIR, "z10_before_top.png"))
    z.render_view(ground + tris_g + tris_p, view, os.path.join(STILL_DIR, "z10_after_top.png"))
    z.render_view(ground + tris_g + fig, eye, os.path.join(STILL_DIR, "z10_before_eye.png"))
    z.render_view(ground + tris_g + tris_p + fig, eye, os.path.join(STILL_DIR, "z10_after_eye.png"))
    print("stills " + STILL_DIR)


if __name__ == "__main__":
    raise SystemExit(main())
