#!/usr/bin/env python3
"""Headless world check for the Z5 swing district.

Gray swing frames, the vault line, the north rims, and the landmark stay
where MegaParkP1Layout put them. Library props sit on the north lawn, south
of the z=92 loop. Prints the same world-check line as Z7. Stills are
collider rasters.
"""

import math
import os
import sys

import check_z7 as z

CHASE = [(60.4, 83.25), (92.0, 83.25), (92.0, 91.35), (60.4, 91.35)]
CHASE_CLEAR = 0.80
STILL_DIR = os.path.join(z.ROOT, "Docs/WorldStills/pass6")
LANDMARK = (84.0, 95.0)


def box_of(x, y, zc, sx, sy, sz):
    return (x - sx * 0.5, y - sy * 0.5, zc - sz * 0.5, x + sx * 0.5, y + sy * 0.5, zc + sz * 0.5)


def swing_gray():
    rows = []
    for x in (66.0, 78.0, 90.0):
        rows.append(("Swing_PostL_%.0f" % x, box_of(x - 1.2, 1.035, 81, 0.2, 2.07, 0.2)))
        rows.append(("Swing_PostR_%.0f" % x, box_of(x + 1.2, 1.035, 81, 0.2, 2.07, 0.2)))
        rows.append(("Swing_Beam_%.0f" % x, box_of(x, 2.15, 81, 2.6, 0.16, 0.2)))
        rows.append(("Swing_Rail_%.0f" % x, box_of(x, 0.45, 82.3, 2.2, 0.9, 0.16)))
    for i in range(5):
        x = 64.0 + i * 3.2
        rows.append(("Swing_Line%d" % i, box_of(x, 0.48, 79.95, 1.3, 0.96, 0.5)))
    rows.extend([
        ("Rim_N2", box_of(72, 1.75, 77, 8, 3.5, 4)),
        ("Hook_Rim_N2", box_of(68.2, 4.5, 77, 0.30, 2, 3)),
        ("Rim_N3", box_of(86, 1.75, 77, 8, 3.5, 4)),
        ("Hook_Rim_N3", box_of(82.2, 4.5, 77, 0.30, 2, 3)),
        ("Rim_N4", box_of(100, 1.6, 77, 8, 3.2, 4)),
        ("Hook_Rim_N4", box_of(96.2, 4.2, 77, 0.30, 2, 3)),
        ("Landmark_Z5_Pole", box_of(84, 7.2, 95, 0.42, 14.4, 0.42)),
    ])
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
    swing_lock = (
        "static void AddSwing(" in layout
        and "static void AddSwingLine(" in layout
        and 'Mast(list, "Landmark_Z5"' in layout
        and 'Add(list, "Rim_N2"' in layout
    )
    places = z.parse_places(src, "Swing")
    z7 = z.parse_places(src, "Places")
    gray = swing_gray()
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

    if "Sw_ClimbA" in by and "Sw_ClimbB" in by and "Sw_Gazebo" in by:
        wa = by["Sw_ClimbA"]["aabb"]
        wb = by["Sw_ClimbB"]["aabb"]
        wall_east = max(wa[3], wb[3])
        wall_h = max(wa[4], wb[4]) - min(wa[1], wb[1])
        run_len = max(wa[5], wb[5]) - min(wa[2], wb[2])
        deck_box = None
        for box in by["Sw_Gazebo"]["boxes"]:
            if box.get("name") == "Col_Deck":
                deck_box = box["aabb"]
        blockers = []
        for inst in instances:
            if inst["place"]["name"] in ("Sw_ClimbA", "Sw_ClimbB"):
                continue
            for box in inst["boxes"]:
                if inst["place"]["name"] == "Sw_Gazebo" and box.get("name") == "Col_Deck":
                    continue
                blockers.append(box["aabb"])
        for name, box in gray:
            blockers.append(box)
        arc_ok, arc_detail = (False, "no deck")
        if deck_box is not None:
            arc_ok, arc_detail = z.off_wall_lands(
                wall_east, [(wa[2], wa[5]), (wb[2], wb[5])], deck_box, blockers
            )
        deck_gap = (deck_box[0] - wall_east) if deck_box else -1
        ok = (
            by["Sw_ClimbA"]["prefab"]["climbable"]
            and wall_h + 0.05 >= 0.32
            and run_len >= 4.0
            and deck_gap > 0.5
            and arc_ok
            and swing_lock
        )
        add_route(
            "WestClimb",
            ok,
            "wall %.2f m wall-run 4.00 m on a %.2f m face (max %.2f) deck gap %.2f m gray locked %s; %s"
            % (wall_h, run_len, z.WALL_RUN_DIST, deck_gap, "yes" if swing_lock else "NO", arc_detail),
        )
        cornice = (wall_east, max(wa[4], wb[4]), (min(wa[2], wb[2]) + max(wa[5], wb[5])) * 0.5)
        origin = (90.0, 1.6, 86.0)
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

    if "Sw_Gazebo" in by:
        v = by["Sw_Gazebo"]["prefab"]["vault"]
        g = by["Sw_Gazebo"]["aabb"]
        east_clear = True
        for inst in instances:
            if inst["place"]["name"] == "Sw_Gazebo":
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

    if "Sw_ScaffoldA" in by and "Sw_ScaffoldB" in by:
        a = by["Sw_ScaffoldA"]["aabb"]
        b = by["Sw_ScaffoldB"]["aabb"]

        def deck_bottom(inst):
            best = None
            for box in inst["boxes"]:
                bot = box["aabb"][1]
                if bot > 1.0:
                    best = bot if best is None else min(best, bot)
            return best if best is not None else inst["aabb"][4]

        clear_a = deck_bottom(by["Sw_ScaffoldA"])
        clear_b = deck_bottom(by["Sw_ScaffoldB"])
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
    batch_ok = 'BuildDistrict("WorldZ5", MegaParkWorldDistrict.Swing, table, true)' in boot
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
        "yes" if smaller else "NO", "yes" if batch_ok else "NO", "yes" if swing_lock else "NO"))
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
        and smaller and batch_ok and swing_lock
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
        col = (0.45, 0.50, 0.55) if name.startswith("Swing") else (0.55, 0.42, 0.28)
        tris.extend(z.box_tris(cx, cy, cz, sx, sy, sz, 0, col))
    return tris


def write_stills(instances, gray):
    os.makedirs(STILL_DIR, exist_ok=True)
    before = gray_tris(gray)
    after = gray_tris(gray) + z.prop_tris(instances)
    ground = z.box_tris(79, -0.05, 86, 50, 0.08, 22, 0, (0.32, 0.46, 0.30))
    top = ("ortho", 56.0, 102.0, 74.0, 98.0)
    eye = z.look_cam((74.0, 1.6, 78.2), (68.0, 1.5, 87.0), 58.0)
    z.render_view(ground + before, top, os.path.join(STILL_DIR, "z5_before_top.png"))
    z.render_view(ground + before + z.figure_tris(70.0, 0.0, 83.4), eye, os.path.join(STILL_DIR, "z5_before_eye.png"))
    z.render_view(ground + after, top, os.path.join(STILL_DIR, "z5_after_top.png"))
    z.render_view(ground + after + z.figure_tris(70.0, 0.0, 83.4), eye, os.path.join(STILL_DIR, "z5_after_eye.png"))
    print("stills " + STILL_DIR)


if __name__ == "__main__":
    raise SystemExit(main())
