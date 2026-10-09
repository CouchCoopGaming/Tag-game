#!/usr/bin/env python3
"""Headless world check for the Z8 crash bowl.

The open rect stays empty. Props sit on the ground lips north and south of
the sand. The climb is a wash-house wall onto the second wash roof. The rope
runs to the playground beam. Prints the same world-check line as Z7.
"""

import math
import os
import sys

import check_z7 as z

CHASE = [(48.8, 29.7), (70.5, 29.7), (70.5, 65.5), (48.8, 65.5)]
CHASE_CLEAR = 0.80
STILL_DIR = os.path.join(z.ROOT, "Docs/WorldStills/pass8")
LANDMARK = (54.0, 34.35)
BANNED = (
    "Brick_Wall.prefab",
    "Gazebo.prefab",
    "RooftopAC.prefab",
    "Scaffold_Bay.prefab",
    "Tree_Maple.prefab",
    "Planter.prefab",
    "Bench_Wood.prefab",
    "TrashCan_Lidded.prefab",
    "Shrub.prefab",
    "PicnicTable.prefab",
    "LightPost_Single.prefab",
    "Container_20.prefab",
    "Dock_Straight.prefab",
    "HarborRail.prefab",
    "BusShelter.prefab",
)


def box_of(x, y, zc, sx, sy, sz):
    return (x - sx * 0.5, y - sy * 0.5, zc - sz * 0.5, x + sx * 0.5, y + sy * 0.5, zc + sz * 0.5)


def bowl_gray():
    rows = [
        ("Toy_Bucket", box_of(51, -0.825, 42.5, 0.4, 0.35, 0.4)),
        ("Toy_Shovel", box_of(51, -0.97, 43.6, 0.55, 0.06, 0.08)),
        ("Toy_Mold", box_of(73, -0.94, 60.4, 0.5, 0.12, 0.5)),
        ("Toy_Sifter", box_of(73, -0.97, 61.2, 0.45, 0.06, 0.4)),
        ("Rim_DropN", box_of(58, 1.25, 71.1, 4, 2.5, 9.4)),
        ("Rim_N1", box_of(58, 2, 78, 8, 4, 4)),
        ("Rim_N2", box_of(72, 1.75, 77, 8, 3.5, 4)),
        ("Cover_S1", box_of(48, 0.675, 26.5, 2.4, 1.35, 1.15)),
    ]
    return rows


def overlaps(a, b):
    return (
        z.overlap_1(a[0], a[3], b[0], b[3]) > 0.02
        and z.overlap_1(a[1], a[4], b[1], b[4]) > 0.02
        and z.overlap_1(a[2], a[5], b[2], b[5]) > 0.02
    )


def in_bowl(box):
    return box[0] < 78 and box[3] > 46 and box[2] < 66 and box[5] > 34


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
    places = z.parse_places(src, "Bowl")
    z7 = z.parse_places(src, "Places")
    others = []
    for array in ("Places", "SoftPlay", "Cling", "Merry", "Slide", "Swing", "Forts", "Bars", "Hops"):
        others.extend(z.parse_places(src, array))
    gray = bowl_gray()
    cache = {}
    instances = []
    missing = 0
    scale_fails = 0
    floating = []
    open_hits = []
    loop_hits = []
    bowl_hits = []
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
            report.append("missing collider or file: %s" % p["name"])
            continue
        instances.append(inst)

    other_boxes = []
    for p in others:
        inst = load(p)
        if inst is not None:
            other_boxes.append((p["name"], inst["aabb"]))

    aabbs = [inst["aabb"] for inst in instances]
    for i, inst in enumerate(instances):
        rest = [aabbs[j] for j in range(len(aabbs)) if j != i]
        if not z.pivot_supported(inst, rest):
            floating.append(inst["place"]["name"])
        if z.hits_open(inst["aabb"]):
            open_hits.append(inst["place"]["name"])
        if not z.loop_clear(inst["aabb"]):
            loop_hits.append(inst["place"]["name"])
        if in_bowl(inst["aabb"]):
            bowl_hits.append(inst["place"]["name"])
        if z.dist_point_aabb(LANDMARK[0], LANDMARK[1], inst["aabb"]) < 1.2:
            cover.append(inst["place"]["name"] + " covers landmark")
        for name, box in gray:
            if overlaps(inst["aabb"], box):
                cover.append(inst["place"]["name"] + " overlaps " + name)
        for name, box in other_boxes:
            if overlaps(inst["aabb"], box):
                cover.append(inst["place"]["name"] + " overlaps " + name)

    by = {inst["place"]["name"]: inst for inst in instances}
    routes = []

    def add_route(name, ok, detail):
        routes.append((name, ok, detail))
        report.append(("OK  " if ok else "FAIL") + " " + name + " " + detail)

    if "Bd_WashA" in by and "Bd_WashB" in by:
        wall = named_box(by["Bd_WashA"], "Climb_Right")
        deck = named_box(by["Bd_WashB"], "Col_Roof")
        blockers = []
        for inst in instances:
            if inst["place"]["name"] in ("Bd_WashA", "Bd_WashB"):
                for box in inst["boxes"]:
                    if box.get("name") in ("Climb_Right", "Col_Roof"):
                        continue
                    blockers.append(box["aabb"])
                continue
            for box in inst["boxes"]:
                blockers.append(box["aabb"])
        for name, box in gray:
            blockers.append(box)
        arc_ok, arc_detail = (False, "no deck")
        deck_gap = -1
        if wall is not None and deck is not None:
            face = wall[3]
            span = (wall[2], wall[5])
            arc_ok, arc_detail = z.off_wall_lands(face, [span], deck, blockers)
            deck_gap = deck[0] - face
        wall_h = (wall[4] - wall[1]) if wall is not None else 0
        run_len = (wall[5] - wall[2]) if wall is not None else 0
        ok = (
            wall is not None
            and by["Bd_WashA"]["prefab"]["climbable"]
            and wall_h >= 2.0
            and run_len >= 2.5
            and deck_gap > 0.5
            and abs(deck_gap - 4.10) > 0.15
            and abs(deck_gap - 3.60) > 0.15
            and arc_ok
            and not repeated
        )
        add_route(
            "WashClimb",
            ok,
            "east jump wall %.2f m on a %.2f m face deck gap %.2f m; %s"
            % (wall_h, run_len, deck_gap, arc_detail),
        )
    else:
        add_route("WashClimb", False, "missing wash houses")

    if "Bd_WashB" in by and "Bd_Play" in by:
        roof = named_box(by["Bd_WashB"], "Col_Roof")
        beam = named_box(by["Bd_Play"], "Col_Beam")
        if roof is None or beam is None:
            add_route("BeamGrapple", False, "missing roof or beam")
        else:
            origin = ((roof[0] + roof[3]) * 0.5, roof[4], (roof[2] + roof[5]) * 0.5)
            cornice = ((beam[0] + beam[3]) * 0.5, beam[4], (beam[2] + beam[5]) * 0.5)
            gd = math.dist(origin, cornice)
            add_route(
                "BeamGrapple",
                z.GRAPPLE_MIN <= gd <= z.GRAPPLE_MAX and not bowl_hits,
                "rope %.2f m from the wash roof to the playground beam (range %.1f–%.1f)"
                % (gd, z.GRAPPLE_MIN, z.GRAPPLE_MAX),
            )
    else:
        add_route("BeamGrapple", False, "missing wash or playground")

    if "Bd_Escape" in by:
        v = by["Bd_Escape"]["prefab"]["vault"]
        g = by["Bd_Escape"]["aabb"]
        west_clear = True
        for inst in instances:
            if inst["place"]["name"] == "Bd_Escape":
                continue
            b = inst["aabb"]
            if b[4] < 0.4:
                continue
            if b[3] > g[0] - 1.2 and b[0] < g[0] and z.overlap_1(b[2], b[5], g[2], g[5]) > 0.2:
                west_clear = False
        ok = z.VAULT_MIN <= v <= z.VAULT_MAX and west_clear
        add_route(
            "EscapeVault",
            ok,
            "fire-escape rail %.2f m (vault band %.2f–%.2f) west walk-up %s"
            % (v, z.VAULT_MIN, z.VAULT_MAX, "open" if west_clear else "blocked"),
        )
    else:
        add_route("EscapeVault", False, "missing escape")

    if "Bd_StandA" in by and "Bd_StandB" in by:
        a = by["Bd_StandA"]["aabb"]
        b = by["Bd_StandB"]["aabb"]
        clear_a = roof_bottom(by["Bd_StandA"])
        clear_b = roof_bottom(by["Bd_StandB"])
        gap = b[0] - a[3] if a[3] <= b[0] else a[0] - b[3]
        ok = clear_a >= z.CROUCH_H - 0.001 and clear_b >= z.CROUCH_H - 0.001 and 0.4 <= gap <= z.AIR_DASH + 0.02
        add_route(
            "StandDash",
            ok,
            "roof underside %.2f / %.2f m gap %.2f m (dash %.2f)"
            % (clear_a, clear_b, gap, z.AIR_DASH),
        )
    else:
        add_route("StandDash", False, "missing stands")

    named = []
    for inst in instances:
        named.append((inst["place"]["name"], inst["aabb"]))
    for name, box in gray:
        named.append(("gray:" + name, box))
    for name, box in other_boxes:
        named.append((name, box))
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
    length = sum(math.hypot(CHASE[(i + 1) % len(CHASE)][0] - CHASE[i][0], CHASE[(i + 1) % len(CHASE)][1] - CHASE[i][1]) for i in range(len(CHASE)))
    add_route(
        "LipChase",
        blocked == 0 and length > 40.0 and worst >= CHASE_CLEAR and not open_hits,
        "length %.1f m clearance %.2f m at (%.1f, %.1f) vs %s samples %d blocked %d"
        % (length, worst, worst_at[0], worst_at[1], worst_at[2], len(samples), blocked),
    )

    reachable = sum(1 for _, ok, _ in routes if ok)
    n = len(routes)
    z7_unique = len({p["path"] for p in z7})
    smaller = len(instances) < len(z7) and len({inst["place"]["path"] for inst in instances}) < z7_unique
    boot = open(os.path.join(z.ROOT, "Assets/Scripts/Level/MegaParkP1Bootstrap.cs"), encoding="utf-8").read()
    batch_ok = 'BuildDistrict("WorldZ8", MegaParkWorldDistrict.Bowl, table, true)' in boot
    line = "world-check routes=%d reachable=%d/%d floatingProps=%d missingColliders=%d scaleFails=%d" % (
        n, reachable, n, len(floating), missing, scale_fails,
    )
    print(line)
    print("instances %d unique %d (Z7 instances %d unique %d)" % (
        len(instances), len({inst["place"]["path"] for inst in instances}), len(z7), z7_unique))
    print("smaller-than-z7 %s static-batch %s" % ("yes" if smaller else "NO", "yes" if batch_ok else "NO"))
    if floating:
        print("floating: " + ", ".join(floating))
    if open_hits:
        print("open rect: " + ", ".join(open_hits))
    if loop_hits:
        print("472 m loop: " + ", ".join(loop_hits))
    if bowl_hits:
        print("bowl: " + ", ".join(bowl_hits))
    if repeated:
        print("repeated set: " + ", ".join(repeated))
    if cover:
        print("covers locked: " + ", ".join(cover))
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
        and not open_hits and not loop_hits and not bowl_hits and not cover
        and not repeated and smaller and batch_ok
    )
    return 0 if ok else 1


def gray_tris(gray):
    tris = []
    for name, box in gray:
        sx = box[3] - box[0]
        sy = box[4] - box[1]
        sz = box[5] - box[2]
        cx = (box[0] + box[3]) * 0.5
        cy = (box[1] + box[4]) * 0.5
        cz = (box[2] + box[5]) * 0.5
        col = (0.72, 0.62, 0.38) if name.startswith("Toy") else (0.42, 0.48, 0.32)
        tris.extend(z.box_tris(cx, cy, cz, sx, sy, sz, 0, col))
    sand = z.box_tris(62, -1.05, 50, 32, 0.1, 32, 0, (0.76, 0.68, 0.42))
    return sand + tris


def prop_tris(instances):
    tris = []
    for inst in instances:
        path = inst["place"]["path"]
        if "Restroom" in path:
            col = (0.62, 0.64, 0.60)
        elif "Playground" in path:
            col = (0.20, 0.46, 0.72)
        elif "FireEscape" in path:
            col = (0.45, 0.48, 0.52)
        elif "Newsstand" in path:
            col = (0.55, 0.32, 0.22)
        else:
            col = (0.58, 0.50, 0.40)
        for box in inst["boxes"]:
            tris.extend(z.pts_box(box["pts"], col))
    return tris


def write_stills(instances, gray):
    os.makedirs(STILL_DIR, exist_ok=True)
    before = gray_tris(gray)
    after = gray_tris(gray) + prop_tris(instances)
    ground = z.box_tris(62, -0.05, 52, 40, 0.08, 56, 0, (0.34, 0.46, 0.28))
    north = ("ortho", 48.0, 80.0, 64.0, 80.0)
    south = ("ortho", 46.0, 80.0, 26.0, 36.0)
    eye = z.look_cam((58.5, 1.7, 66.5), (66.5, 1.4, 69.4), 55.0)
    fig = z.figure_tris(66.8, 0.0, 69.2)
    z.render_view(ground + before, north, os.path.join(STILL_DIR, "z8_before_top_north.png"))
    z.render_view(ground + after, north, os.path.join(STILL_DIR, "z8_after_top_north.png"))
    z.render_view(ground + before, south, os.path.join(STILL_DIR, "z8_before_top_south.png"))
    z.render_view(ground + after, south, os.path.join(STILL_DIR, "z8_after_top_south.png"))
    z.render_view(ground + before + fig, eye, os.path.join(STILL_DIR, "z8_before_eye.png"))
    z.render_view(ground + after + fig, eye, os.path.join(STILL_DIR, "z8_after_eye.png"))
    print("stills " + STILL_DIR)


if __name__ == "__main__":
    raise SystemExit(main())
