#!/usr/bin/env python3
"""Headless world check for the Z3 merry district.

Gray merry toys stay where MegaParkP1Layout put them. Crossing B stays empty.
Library props sit on the south lawn and the north strip. Prints the same
world-check line as Z7. Stills are collider rasters and use a z3_ prefix so
the Z2 pass-3 set stays put.
"""

import math
import os
import sys

import check_z7 as z

CHASE = [(22.30, 34.10), (45.05, 34.10), (45.05, 54.35), (22.30, 54.35), (22.30, 34.10)]
CHASE_CLEAR = 0.80
STILL_DIR = os.path.join(z.ROOT, "Docs/WorldStills/pass3")
CROSS_B = (22.0, 46.0, 44.0, 52.0)
LANDMARK = (40.0, 36.6)


def box_of(x, y, zc, sx, sy, sz):
    return (x - sx * 0.5, y - sy * 0.5, zc - sz * 0.5, x + sx * 0.5, y + sy * 0.5, zc + sz * 0.5)


def merry_rows():
    # Centers and sizes copied from MegaParkP1Layout.BuildSolids / AddMerryGrapple / AddLandmarks.
    rows = [
        ("Merry_Podium", box_of(34.0, 0.15, 39.0, 5.0, 0.3, 5.0)),
        ("Merry_Post_SW", box_of(31.2, 1.2, 36.6, 0.25, 2.4, 0.25)),
        ("Merry_Post_SE", box_of(36.8, 1.2, 36.6, 0.25, 2.4, 0.25)),
        ("Merry_Post_NW", box_of(31.2, 1.2, 41.4, 0.25, 2.4, 0.25)),
        ("Merry_Post_NE", box_of(36.8, 1.2, 41.4, 0.25, 2.4, 0.25)),
        ("Merry_TableW", box_of(28.0, 0.42, 56.0, 2.4, 0.84, 1.2)),
        ("Merry_TableE", box_of(40.0, 0.42, 56.0, 2.4, 0.84, 1.2)),
        ("Merry_A", box_of(25.6, 1.0, 58.2, 3.0, 2.0, 2.2)),
        ("Merry_B", box_of(33.4, 1.15, 58.2, 3.0, 2.3, 2.2)),
        ("Hook_Merry_B", box_of(32.05, 3.3, 58.2, 0.3, 2.0, 1.8)),
        ("Landmark_Z3_Pole", box_of(40.0, 7.2, 36.6, 0.42, 14.4, 0.42)),
        ("Landmark_Z3_Flag", box_of(40.0, 15.2, 36.6, 2.2, 1.6, 0.16)),
    ]
    return rows


def overlaps(a, b):
    return (
        z.overlap_1(a[0], a[3], b[0], b[3]) > 0.02
        and z.overlap_1(a[1], a[4], b[1], b[4]) > 0.02
        and z.overlap_1(a[2], a[5], b[2], b[5]) > 0.02
    )


def hits_cross(box):
    x0, x1, z0, z1 = CROSS_B
    return box[0] < x1 and box[3] > x0 and box[2] < z1 and box[5] > z0


def main():
    os.chdir(z.ROOT)
    src = open(z.DISTRICT_CS, encoding="utf-8").read()
    layout = open(os.path.join(z.ROOT, "Assets/Scripts/Level/MegaParkP1Layout.cs"), encoding="utf-8").read()
    merry_lock = (
        'Add(list, "Merry_Podium"' in layout
        and 'Add(list, "Merry_TableW"' in layout
        and 'Add(list, "Merry_TableE"' in layout
        and 'Add(list, "Merry_A"' in layout
        and 'Add(list, "Merry_B"' in layout
        and 'Mast(list, "Landmark_Z3"' in layout
    )
    places = z.parse_places(src, "Merry")
    z7 = z.parse_places(src, "Places")
    cache = {}
    instances = []
    missing = 0
    scale_fails = 0
    floating = []
    open_hits = []
    loop_hits = []
    bowl_hits = []
    cross_hits = []
    cover = []
    report = []
    gray = merry_rows()

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
        if hits_cross(b):
            cross_hits.append(inst["place"]["name"])
        if z.dist_point_aabb(LANDMARK[0], LANDMARK[1], b) < 1.2:
            cover.append(inst["place"]["name"])
        for name, box in gray:
            if overlaps(b, box):
                cover.append(inst["place"]["name"] + " overlaps " + name)

    by = {inst["place"]["name"]: inst for inst in instances}
    routes = []

    def add_route(name, ok, detail):
        routes.append((name, ok, detail))
        report.append(("OK  " if ok else "FAIL") + " " + name + " " + detail)

    if "My_ClimbA" in by and "My_ClimbB" in by and "My_Gazebo" in by:
        wa = by["My_ClimbA"]["aabb"]
        wb = by["My_ClimbB"]["aabb"]
        wall_east = max(wa[3], wb[3])
        wall_h = max(wa[4], wb[4]) - min(wa[1], wb[1])
        run_len = max(wa[5], wb[5]) - min(wa[2], wb[2])
        deck_box = None
        for box in by["My_Gazebo"]["boxes"]:
            if box.get("name") == "Col_Deck":
                deck_box = box["aabb"]
        blockers = []
        for inst in instances:
            if inst["place"]["name"] in ("My_ClimbA", "My_ClimbB"):
                continue
            for box in inst["boxes"]:
                if inst["place"]["name"] == "My_Gazebo" and box.get("name") == "Col_Deck":
                    continue
                blockers.append(box["aabb"])
        for name, box in gray:
            if name.endswith("_Flag"):
                continue
            blockers.append(box)
        arc_ok, arc_detail = (False, "no deck")
        if deck_box is not None:
            arc_ok, arc_detail = z.off_wall_lands(wall_east, [(wa[2], wa[5]), (wb[2], wb[5])], deck_box, blockers)
        deck_gap = (deck_box[0] - wall_east) if deck_box else -1
        ok = (
            by["My_ClimbA"]["prefab"]["climbable"]
            and wall_h + 0.05 >= 0.32
            and run_len >= 4.0
            and 4.0 <= z.WALL_RUN_DIST + 0.02
            and deck_gap > 0.5
            and arc_ok
            and merry_lock
        )
        add_route(
            "WestClimb",
            ok,
            "wall %.2f m wall-run 4.00 m on a %.2f m face (max %.2f) deck gap %.2f m gray locked %s; %s"
            % (wall_h, run_len, z.WALL_RUN_DIST, deck_gap, "yes" if merry_lock else "NO", arc_detail),
        )
        cornice = (wall_east, max(wa[4], wb[4]), (min(wa[2], wb[2]) + max(wa[5], wb[5])) * 0.5)
        origin = (40.5, 1.6, 53.5)
        gd = math.sqrt(sum((cornice[k] - origin[k]) ** 2 for k in range(3)))
        add_route(
            "NorthGrapple",
            z.GRAPPLE_MIN <= gd <= z.GRAPPLE_MAX,
            "rope %.2f m from the north strip to the climb cornice (range %.1f–%.1f)"
            % (gd, z.GRAPPLE_MIN, z.GRAPPLE_MAX),
        )
    else:
        add_route("WestClimb", False, "missing climb or gazebo")
        add_route("NorthGrapple", False, "missing climb")

    if "My_Gazebo" in by:
        v = by["My_Gazebo"]["prefab"]["vault"]
        g = by["My_Gazebo"]["aabb"]
        east_clear = True
        for inst in instances:
            if inst["place"]["name"] == "My_Gazebo":
                continue
            b = inst["aabb"]
            if b[4] < 0.5:
                continue
            if b[0] < g[3] + 1.2 and b[3] > g[3] and z.overlap_1(b[2], b[5], g[2], g[5]) > 0.4:
                east_clear = False
        for name, box in gray:
            if name.endswith("_Flag"):
                continue
            if box[4] < 0.5:
                continue
            if box[0] < g[3] + 1.2 and box[3] > g[3] and z.overlap_1(box[2], box[5], g[2], g[5]) > 0.4:
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

    if "My_ScaffoldA" in by and "My_ScaffoldB" in by:
        a = by["My_ScaffoldA"]["aabb"]
        b = by["My_ScaffoldB"]["aabb"]

        def deck_bottom(inst):
            best = None
            for box in inst["boxes"]:
                bot = box["aabb"][1]
                if bot > 1.0:
                    best = bot if best is None else min(best, bot)
            return best if best is not None else inst["aabb"][4]

        clear_a = deck_bottom(by["My_ScaffoldA"])
        clear_b = deck_bottom(by["My_ScaffoldB"])
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
        if name.endswith("_Flag"):
            continue
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
    smaller = len(instances) < len(z7) and len(cache) < 23
    boot = open(os.path.join(z.ROOT, "Assets/Scripts/Level/MegaParkP1Bootstrap.cs"), encoding="utf-8").read()
    batch_ok = 'BuildDistrict("WorldZ3", MegaParkWorldDistrict.Merry, table, true)' in boot
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
    print("instances %d unique %d (Z7 instances %d unique %d)" % (len(instances), len(cache), len(z7), len({p["path"] for p in z7})))
    print("smaller-than-z7 %s static-batch %s crossing-empty %s" % (
        "yes" if smaller else "NO",
        "yes" if batch_ok else "NO",
        "yes" if not cross_hits else "NO",
    ))
    if floating:
        print("floating: " + ", ".join(floating))
    if open_hits:
        print("open rect: " + ", ".join(open_hits))
    if loop_hits:
        print("472 m loop: " + ", ".join(loop_hits))
    if bowl_hits:
        print("bowl: " + ", ".join(bowl_hits))
    if cross_hits:
        print("crossing B: " + ", ".join(cross_hits))
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
        and not open_hits and not loop_hits and not bowl_hits and not cross_hits and not cover
        and smaller and batch_ok and merry_lock
    )
    return 0 if ok else 1


def write_stills(instances, gray):
    os.makedirs(STILL_DIR, exist_ok=True)
    before = gray_tris(gray)
    after = gray_tris(gray) + z.prop_tris(instances)
    ground = []
    ground.extend(z.box_tris(34, -0.08, 47, 28, 0.08, 30, 0, (0.40, 0.52, 0.32)))
    ground.extend(z.box_tris(34, -0.02, 48, 24, 0.02, 8, 0, (0.55, 0.58, 0.48)))
    top = ("ortho", 18.0, 50.0, 30.0, 64.0)
    eye_south = z.look_cam((32.0, 2.4, 30.5), (29.0, 1.0, 39.0), 55.0)
    eye_west = z.look_cam((18.0, 2.6, 33.0), (28.0, 1.0, 39.0), 55.0)
    z.render_view(ground + before, top, os.path.join(STILL_DIR, "z3_before_top.png"))
    z.render_view(ground + before + z.figure_tris(32.0, 0.0, 34.5), eye_south, os.path.join(STILL_DIR, "z3_before_eye_south.png"))
    z.render_view(ground + before + z.figure_tris(24.0, 0.0, 39.0), eye_west, os.path.join(STILL_DIR, "z3_before_eye_west.png"))
    z.render_view(ground + after, top, os.path.join(STILL_DIR, "z3_after_top.png"))
    z.render_view(ground + after + z.figure_tris(32.0, 0.0, 34.5), eye_south, os.path.join(STILL_DIR, "z3_after_eye_south.png"))
    z.render_view(ground + after + z.figure_tris(24.0, 0.0, 39.0), eye_west, os.path.join(STILL_DIR, "z3_after_eye_west.png"))
    write_split(ground + after, os.path.join(STILL_DIR, "z3_split4.png"))
    print("stills " + STILL_DIR)


def gray_tris(gray):
    tris = []
    for name, box in gray:
        cx = (box[0] + box[3]) * 0.5
        cy = (box[1] + box[4]) * 0.5
        cz = (box[2] + box[5]) * 0.5
        sx = box[3] - box[0]
        sy = box[4] - box[1]
        sz = box[5] - box[2]
        if name.startswith("Landmark"):
            col = (0.85, 0.45, 0.55)
        elif name.startswith("Merry_Post") or name.startswith("Hook"):
            col = (0.62, 0.64, 0.68)
        else:
            col = (0.72, 0.45, 0.55)
        tris.extend(z.box_tris(cx, cy, cz, sx, sy, sz, 0, col))
    return tris


def write_split(tris, path):
    w, h = 1280, 720
    img = z.sky_image(w, h)
    cams = [
        z.look_cam((26.0, 1.6, 33.0), (29.0, 1.3, 39.0), 60.0),
        z.look_cam((36.0, 1.6, 33.5), (34.0, 1.2, 39.0), 60.0),
        z.look_cam((22.0, 1.6, 50.0), (26.0, 1.3, 53.0), 60.0),
        z.look_cam((40.0, 1.6, 54.0), (33.0, 1.2, 58.0), 60.0),
    ]
    vw, vh = w // 2, h // 2
    for i, cam in enumerate(cams):
        sub = z.render_persp(
            tris + z.figure_tris(28 + (i % 2) * 8, 0, 36 + (i // 2) * 16),
            vw, vh, cam[1], cam[2], cam[3],
        )
        ox, oy = (i % 2) * vw, (i // 2) * vh
        img[oy:oy + vh, ox:ox + vw] = sub
    img[vh - 1:vh + 1, :] = (230, 230, 230)
    img[:, vw - 1:vw + 1] = (230, 230, 230)
    z.write_png(path, img)


if __name__ == "__main__":
    sys.exit(main())
