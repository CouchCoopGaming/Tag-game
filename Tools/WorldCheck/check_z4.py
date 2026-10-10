#!/usr/bin/env python3
"""Headless world check for the Z4 slide district.

Gray towers, chutes, rims, and the landmark stay where MegaParkP1Layout
put them. Library props sit on the north lawn, south of the z=92 loop.
Prints the same world-check line as Z7. Stills are collider rasters.
"""

import math
import os
import sys

import check_z7 as z

CHASE = [(22.4, 83.05), (51.2, 83.05), (51.2, 91.35), (22.4, 91.35)]
CHASE_CLEAR = 0.80
STILL_DIR = os.path.join(z.ROOT, "Docs/WorldStills/pass5")
LANDMARK = (48.0, 78.0)


def box_of(x, y, zc, sx, sy, sz):
    return (x - sx * 0.5, y - sy * 0.5, zc - sz * 0.5, x + sx * 0.5, y + sy * 0.5, zc + sz * 0.5)


def slide_gray():
    # Centers and full sizes copied from MegaParkP1Layout.BuildSolids.
    rows = [
        ("Slide_T1", box_of(28, 1, 78, 4, 2, 4)),
        ("Slide_T2", box_of(38, 1.75, 79, 4, 3.5, 4)),
        ("Slide_T2Step", box_of(40.8, 1, 79, 1.6, 2, 2.2)),
        ("Slide_T3", box_of(48, 2.5, 78, 4, 5, 4)),
        ("Slide_StepA", box_of(45.1, 1, 77.0, 1.8, 2, 1.8)),
        ("Slide_StepB", box_of(45.1, 1.75, 78.8, 1.8, 3.5, 1.8)),
        ("Slide_Deck2", box_of(24, 1, 78, 2.6, 2, 3)),
        ("Slide_Deck2Step", box_of(25.5, 0.45, 75.2, 1.6, 0.9, 1.4)),
        ("Slide_CrawlL", box_of(33.2, 0.6, 73.5, 7, 1.2, 0.22)),
        ("Slide_CrawlR", box_of(33.2, 0.6, 75.3, 7, 1.2, 0.22)),
        ("Slide_CrawlRoof", box_of(33.2, 1.29, 74.4, 7, 0.18, 2.02)),
        ("Slide_Spire", box_of(41.3, 1.4, 74.4, 0.4, 2.8, 0.4)),
        ("Slide_Sp1", box_of(40.05, 0.4, 73.15, 1.2, 0.8, 1.2)),
        ("Slide_Sp2", box_of(42.55, 0.8, 73.15, 1.2, 1.6, 1.2)),
        ("Slide_Sp3", box_of(42.55, 1.2, 75.65, 1.2, 2.4, 1.2)),
        ("Slide_Sp4", box_of(40.05, 1.25, 75.65, 1.2, 2.5, 1.2)),
        ("Rim_SlideIn", box_of(29, 1.75, 81.2, 13.6, 3.5, 2)),
        ("Rim_Link", box_of(42.10, 1.75, 77.125, 4.04, 3.5, 1.25)),
        ("Rim_N1", box_of(58, 2, 78, 8, 4, 4)),
        ("Landmark_Z4_Pole", box_of(48, 10.2, 78, 0.42, 10.4, 0.42)),
    ]
    return rows


def overlaps(a, b):
    return (
        z.overlap_1(a[0], a[3], b[0], b[3]) > 0.02
        and z.overlap_1(a[1], a[4], b[1], b[4]) > 0.02
        and z.overlap_1(a[2], a[5], b[2], b[5]) > 0.02
    )


def main():
    os.chdir(z.ROOT)
    src = open(z.DISTRICT_CS, encoding="utf-8").read()
    layout = open(os.path.join(z.ROOT, "Assets/Scripts/Level/MegaParkP1Layout.cs"), encoding="utf-8").read()
    slide_lock = (
        'Add(list, "Slide_T1"' in layout
        and 'Add(list, "Slide_T3"' in layout
        and 'Add(list, "Rim_SlideIn"' in layout
        and 'Mast(list, "Landmark_Z4"' in layout
        and 'list.Add(RampOf("Slide_Chute1"' in layout
    )
    places = z.parse_places(src, "Slide")
    z7 = z.parse_places(src, "Places")
    gray = slide_gray()
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

    for p in places:
        if abs(p["scale"][0] - 1) > 0.001 or abs(p["scale"][1] - 1) > 0.001 or abs(p["scale"][2] - 1) > 0.001:
            scale_fails += 1
        path = os.path.join(z.ROOT, p["path"])
        if p["path"] not in cache:
            cache[p["path"]] = z.parse_prefab(path) if os.path.isfile(path) else None
        prefab = cache[p["path"]]
        if prefab is None or prefab["collider_count"] == 0:
            missing += 1
            report.append("missing collider or file: %s" % p["name"])
            continue
        boxes = []
        all_pts = []
        for b in prefab["boxes"]:
            pts = z.world_points(prefab, b, p)
            boxes.append({"pts": pts, "aabb": z.aabb(pts), "name": b.get("name", "")})
            all_pts.extend(pts)
        inst = {"place": p, "prefab": prefab, "boxes": boxes, "aabb": z.aabb(all_pts)}
        instances.append(inst)

    aabbs = [inst["aabb"] for inst in instances]
    for i, inst in enumerate(instances):
        others = [aabbs[j] for j in range(len(aabbs)) if j != i]
        if not z.pivot_supported(inst, others):
            floating.append(inst["place"]["name"])
        if z.hits_open(inst["aabb"]):
            open_hits.append(inst["place"]["name"])
        if not z.loop_clear(inst["aabb"]):
            loop_hits.append(inst["place"]["name"])
        b = inst["aabb"]
        if b[0] < 78 and b[3] > 46 and b[2] < 66 and b[5] > 34:
            bowl_hits.append(inst["place"]["name"])
        if z.dist_point_aabb(LANDMARK[0], LANDMARK[1], b) < 1.2:
            cover.append(inst["place"]["name"] + " covers landmark")
        for name, box in gray:
            if overlaps(b, box):
                cover.append(inst["place"]["name"] + " overlaps " + name)

    by = {inst["place"]["name"]: inst for inst in instances}
    routes = []

    def add_route(name, ok, detail):
        routes.append((name, ok, detail))
        report.append(("OK  " if ok else "FAIL") + " " + name + " " + detail)

    if "Sl_ClimbA" in by and "Sl_ClimbB" in by and "Sl_Gazebo" in by:
        wa = by["Sl_ClimbA"]["aabb"]
        wb = by["Sl_ClimbB"]["aabb"]
        wall_east = max(wa[3], wb[3])
        wall_h = max(wa[4], wb[4]) - min(wa[1], wb[1])
        run_len = max(wa[5], wb[5]) - min(wa[2], wb[2])
        deck_box = None
        for box in by["Sl_Gazebo"]["boxes"]:
            if box.get("name") == "Col_Deck":
                deck_box = box["aabb"]
        blockers = []
        for inst in instances:
            if inst["place"]["name"] in ("Sl_ClimbA", "Sl_ClimbB"):
                continue
            for box in inst["boxes"]:
                if inst["place"]["name"] == "Sl_Gazebo" and box.get("name") == "Col_Deck":
                    continue
                blockers.append(box["aabb"])
        for name, box in gray:
            if name.endswith("_Flag"):
                continue
            blockers.append(box)
        arc_ok, arc_detail = (False, "no deck")
        if deck_box is not None:
            arc_ok, arc_detail = z.off_wall_lands(
                wall_east, [(wa[2], wa[5]), (wb[2], wb[5])], deck_box, blockers
            )
        deck_gap = (deck_box[0] - wall_east) if deck_box else -1
        ok = (
            by["Sl_ClimbA"]["prefab"]["climbable"]
            and wall_h + 0.05 >= 0.32
            and run_len >= 4.0
            and deck_gap > 0.5
            and arc_ok
            and slide_lock
        )
        add_route(
            "WestClimb",
            ok,
            "wall %.2f m wall-run 4.00 m on a %.2f m face (max %.2f) deck gap %.2f m gray locked %s; %s"
            % (wall_h, run_len, z.WALL_RUN_DIST, deck_gap, "yes" if slide_lock else "NO", arc_detail),
        )
        cornice = (wall_east, max(wa[4], wb[4]), (min(wa[2], wb[2]) + max(wa[5], wb[5])) * 0.5)
        origin = (50.0, 1.6, 86.0)
        gd = math.sqrt(sum((cornice[k] - origin[k]) ** 2 for k in range(3)))
        add_route(
            "EastGrapple",
            z.GRAPPLE_MIN <= gd <= z.GRAPPLE_MAX,
            "rope %.2f m from the east lawn to the climb cornice (range %.1f–%.1f)"
            % (gd, z.GRAPPLE_MIN, z.GRAPPLE_MAX),
        )
    else:
        add_route("WestClimb", False, "missing climb or gazebo")
        add_route("EastGrapple", False, "missing climb")

    if "Sl_Gazebo" in by:
        v = by["Sl_Gazebo"]["prefab"]["vault"]
        g = by["Sl_Gazebo"]["aabb"]
        east_clear = True
        for inst in instances:
            if inst["place"]["name"] == "Sl_Gazebo":
                continue
            b = inst["aabb"]
            if b[4] < 0.5:
                continue
            if b[0] < g[3] + 1.2 and b[3] > g[3] and z.overlap_1(b[2], b[5], g[2], g[5]) > 0.4:
                east_clear = False
        ok = z.MANTLE_MIN <= v <= z.MANTLE_MAX and z.VAULT_MIN <= v <= z.VAULT_MAX and east_clear
        add_route(
            "GazeboVault",
            ok,
            "rail %.2f m (mantle %.2f–%.2f, vault band %.2f–%.2f) east walk-around %s"
            % (v, z.MANTLE_MIN, z.MANTLE_MAX, z.VAULT_MIN, z.VAULT_MAX, "open" if east_clear else "blocked"),
        )
    else:
        add_route("GazeboVault", False, "missing gazebo")

    if "Sl_ScaffoldA" in by and "Sl_ScaffoldB" in by:
        a = by["Sl_ScaffoldA"]["aabb"]
        b = by["Sl_ScaffoldB"]["aabb"]

        def deck_bottom(inst):
            best = None
            for box in inst["boxes"]:
                bot = box["aabb"][1]
                if bot > 1.0:
                    best = bot if best is None else min(best, bot)
            return best if best is not None else inst["aabb"][4]

        clear_a = deck_bottom(by["Sl_ScaffoldA"])
        clear_b = deck_bottom(by["Sl_ScaffoldB"])
        gap = b[0] - a[3] if a[3] <= b[0] else a[0] - b[3]
        ok = clear_a >= z.CROUCH_H - 0.001 and clear_b >= z.CROUCH_H - 0.001 and 0.4 <= gap <= z.AIR_DASH + 0.02
        add_route(
            "SlideDash",
            ok,
            "under-clear %.2f / %.2f m (crouch %.2f) air-dash gap %.2f m (dash %.2f)"
            % (clear_a, clear_b, z.CROUCH_H, gap, z.AIR_DASH),
        )
    else:
        add_route("SlideDash", False, "missing scaffolds")

    named = []
    for inst in instances:
        if z.is_floor(inst["place"]["name"], inst["aabb"]):
            continue
        if inst["aabb"][4] - inst["aabb"][1] < z.STEP and inst["aabb"][4] < 0.45:
            continue
        named.append((inst["place"]["name"], inst["aabb"]))
    for name, box in gray:
        if box[4] - box[1] < z.STEP and box[4] < 0.45:
            continue
        named.append(("gray:" + name, box))

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
    length = 0.0
    for i in range(len(CHASE)):
        ax, az = CHASE[i]
        bx, bz = CHASE[(i + 1) % len(CHASE)]
        length += math.hypot(bx - ax, bz - az)
    chase_ok = blocked == 0 and length > 40.0 and worst >= CHASE_CLEAR
    add_route(
        "ChaseLoop",
        chase_ok,
        "length %.1f m clearance %.2f m at (%.1f, %.1f) vs %s samples %d blocked %d"
        % (length, worst, worst_at[0], worst_at[1], worst_at[2], len(samples), blocked),
    )

    reachable = sum(1 for _, ok, _ in routes if ok)
    n = len(routes)
    z7_unique = len({p["path"] for p in z7})
    smaller = len(instances) < len(z7) and len(cache) < z7_unique
    boot = open(os.path.join(z.ROOT, "Assets/Scripts/Level/MegaParkP1Bootstrap.cs"), encoding="utf-8").read()
    batch_ok = 'BuildDistrict("WorldZ4", MegaParkWorldDistrict.Slide, table, true)' in boot
    line = "world-check routes=%d reachable=%d/%d floatingProps=%d missingColliders=%d scaleFails=%d" % (
        n, reachable, n, len(floating), missing, scale_fails,
    )
    print(line)
    print("jump apex %.3f m  wall-jump flat %.3f m  wall-run %.2f m  air dash %.2f m  grapple %.1f m" % (
        z.JUMP_APEX, z.WALL_JUMP_FLAT, z.WALL_RUN_DIST, z.AIR_DASH, z.GRAPPLE_MAX))
    print(
        "wall-jump perp 0 deg %.3f m  off-wall 30 deg %.3f m  60 deg %.3f m  into-wall -30 deg %.3f m  -60 deg %.3f m"
        % (z.perp_range(0), z.perp_range(30), z.perp_range(60), z.perp_range(-30), z.perp_range(-60))
    )
    print("instances %d unique %d (Z7 instances %d unique %d)" % (len(instances), len(cache), len(z7), z7_unique))
    print("smaller-than-z7 %s static-batch %s gray-locked %s" % (
        "yes" if smaller else "NO", "yes" if batch_ok else "NO", "yes" if slide_lock else "NO"))
    floating.extend(z.gray_route_floats("Z4"))
    if floating:
        print("floating: " + ", ".join(floating))
    if open_hits:
        print("open rect: " + ", ".join(open_hits))
    if loop_hits:
        print("472 m loop: " + ", ".join(loop_hits))
    if bowl_hits:
        print("bowl: " + ", ".join(bowl_hits))
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
        and smaller and batch_ok and slide_lock
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
        col = (0.72, 0.55, 0.22) if name.startswith("Slide") else (0.55, 0.42, 0.28)
        tris.extend(z.box_tris(cx, cy, cz, sx, sy, sz, 0, col))
    return tris


def write_stills(instances, gray):
    os.makedirs(STILL_DIR, exist_ok=True)
    before = gray_tris(gray)
    after = gray_tris(gray) + z.prop_tris(instances)
    ground = z.box_tris(39, -0.05, 86, 40, 0.08, 20, 0, (0.34, 0.46, 0.28))
    top = ("ortho", 20.0, 56.0, 74.0, 96.0)
    eye = z.look_cam((36.0, 1.6, 78.5), (30.0, 1.5, 87.0), 58.0)
    z.render_view(ground + before, top, os.path.join(STILL_DIR, "z4_before_top.png"))
    z.render_view(ground + before + z.figure_tris(32.0, 0.0, 83.5), eye, os.path.join(STILL_DIR, "z4_before_eye.png"))
    z.render_view(ground + after, top, os.path.join(STILL_DIR, "z4_after_top.png"))
    z.render_view(ground + after + z.figure_tris(32.0, 0.0, 83.5), eye, os.path.join(STILL_DIR, "z4_after_eye.png"))
    print("stills " + STILL_DIR)


if __name__ == "__main__":
    raise SystemExit(main())
