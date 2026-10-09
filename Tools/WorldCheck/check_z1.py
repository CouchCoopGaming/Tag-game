#!/usr/bin/env python3
"""Headless world check for the Z1 soft-play district.

Decks stay where MegaParkP1Layout put them. This dresses around them and
prints the same world-check line as Z7. Stills are collider rasters.
"""

import os
import sys

import check_z7 as z

CHASE = [(23.0, 12.4), (36.4, 12.4), (36.4, 34.4), (22.2, 34.4), (22.2, 12.4)]
CHASE_CLEAR = 0.50
STILL_DIR = os.path.join(z.ROOT, "Docs/WorldStills/pass2")

# Centers and full sizes. Decks, step, and lip are not moved.
GRAY = [
    ("SoftPlay_DeckLow", 14, 1, 26, 10, 2, 8),
    ("SoftPlay_DeckHigh", 14, 2.75, 26, 6, 1.5, 5),
    ("SoftPlay_TubeL", 14, 1, 16.9, 8, 2, 0.2),
    ("SoftPlay_TubeR", 14, 1, 19.5, 8, 2, 0.2),
    ("SoftPlay_TubeRoof", 14, 2.1, 18.2, 8, 0.2, 2.9),
    ("SoftPlay_CubeA", 26, 0.55, 20, 1.4, 1.1, 1.4),
    ("SoftPlay_CubeB", 30, 0.4, 28, 1.6, 0.8, 1.6),
    ("SoftPlay_CubeC", 24, 0.85, 32, 1.2, 1.7, 1.2),
    ("SoftPlay_LipW", 9, 2.75, 26, 4, 1.5, 1.4),
    ("SoftPlay_StepW", 5.85, 1, 26, 2.3, 2, 1.4),
    ("Rim_SoftN", 14.3, 1, 33.2, 8.6, 2, 5.6),
    ("Landmark_Z1_Pole", 20, 7.2, 10, 0.42, 14.4, 0.42),
    ("Landmark_Z1_Flag", 20, 15.2, 10, 2.2, 1.6, 0.16),
]
LOCKED = {
    "SoftPlay_DeckLow", "SoftPlay_DeckHigh", "SoftPlay_LipW", "SoftPlay_StepW",
    "Landmark_Z1_Pole", "Landmark_Z1_Flag",
}


def gray_box(row):
    name, x, y, zc, sx, sy, sz = row
    return (x - sx * 0.5, y - sy * 0.5, zc - sz * 0.5, x + sx * 0.5, y + sy * 0.5, zc + sz * 0.5)


def overlaps(a, b):
    return z.overlap_1(a[0], a[3], b[0], b[3]) > 0.02 and z.overlap_1(a[1], a[4], b[1], b[4]) > 0.02 and z.overlap_1(a[2], a[5], b[2], b[5]) > 0.02


def main():
    os.chdir(z.ROOT)
    src = open(z.DISTRICT_CS, encoding="utf-8").read()
    layout = open(os.path.join(z.ROOT, "Assets/Scripts/Level/MegaParkP1Layout.cs"), encoding="utf-8").read()
    deck_lock = (
        'Add(list, "SoftPlay_DeckLow", "Z1", "block", "soft", 14f, 1f, 26f, 10f, 2f, 8f, 0f);' in layout
        and 'Add(list, "SoftPlay_DeckHigh", "Z1", "cap", "soft", 14f, 2.75f, 26f, 6f, 1.5f, 5f, 2f);' in layout
    )
    places = z.parse_places(src, "SoftPlay")
    z7 = z.parse_places(src, "Places")
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
        all_pts = []
        boxes = []
        for b in prefab["boxes"]:
            pts = z.world_points(prefab, b, p)
            boxes.append({"pts": pts, "aabb": z.aabb(pts)})
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
        # Landmark pole footprint, plus a metre so a prop does not swallow it.
        if z.dist_point_aabb(20.0, 10.0, b) < 1.2:
            cover.append(inst["place"]["name"])
        for row in GRAY:
            if row[0] not in LOCKED:
                continue
            if overlaps(b, gray_box(row)):
                cover.append(inst["place"]["name"] + " overlaps " + row[0])

    by = {inst["place"]["name"]: inst for inst in instances}
    routes = []

    def add_route(name, ok, detail):
        routes.append((name, ok, detail))
        report.append(("OK  " if ok else "FAIL") + " " + name + " " + detail)

    deck_west = 9.0
    deck_top = 2.0
    if "Sp_ClimbA" in by and "Sp_ClimbB" in by:
        wa = by["Sp_ClimbA"]["aabb"]
        wb = by["Sp_ClimbB"]["aabb"]
        wall_east = max(wa[3], wb[3])
        wall_h = max(wa[4], wb[4]) - min(wa[1], wb[1])
        face = max(wa[5] - wa[2], wb[5] - wb[2])
        gap = deck_west - wall_east
        step = gray_box(GRAY[9])
        lip = gray_box(GRAY[8])
        decks = gray_box(GRAY[0])
        high = gray_box(GRAY[1])
        clear_toys = not overlaps(wa, step) and not overlaps(wb, step) and not overlaps(wa, lip) and not overlaps(wb, lip) and not overlaps(wa, decks) and not overlaps(wb, decks)
        blockers = [step, lip, high]
        for inst in instances:
            if inst["place"]["name"] in ("Sp_ClimbA", "Sp_ClimbB"):
                continue
            for box in inst["boxes"]:
                blockers.append(box["aabb"])
        for row in GRAY:
            if row[0] in ("SoftPlay_DeckLow", "Landmark_Z1_Flag"):
                continue
            if row[0] in ("SoftPlay_LipW", "SoftPlay_StepW", "SoftPlay_DeckHigh"):
                continue
            blockers.append(gray_box(row))
        arc_ok, arc_detail = z.off_wall_lands(wall_east, [(wa[2], wa[5]), (wb[2], wb[5])], decks, blockers)
        ok = (
            by["Sp_ClimbA"]["prefab"]["climbable"]
            and deck_top <= z.CLIMB_PRACTICAL
            and wall_h + 0.05 >= deck_top
            and face >= 4.0
            and gap > 0.5
            and clear_toys
            and deck_lock
            and arc_ok
        )
        add_route(
            "WestClimb",
            ok,
            "climb %.2f m onto DeckLow (wall %.2f m) wall-run 4.00 m on a %.2f m face gap %.2f m decks locked %s; %s"
            % (deck_top, wall_h, face, gap, "yes" if deck_lock else "NO", arc_detail),
        )
        cornice = (wall_east, max(wa[4], wb[4]), (wa[2] + wa[5]) * 0.5)
        origin = (28.0, 1.6, 18.0)
        gd = ((cornice[0] - origin[0]) ** 2 + (cornice[1] - origin[1]) ** 2 + (cornice[2] - origin[2]) ** 2) ** 0.5
        add_route(
            "WestGrapple",
            z.GRAPPLE_MIN <= gd <= z.GRAPPLE_MAX,
            "rope %.2f m from the east lawn to the climb cornice (range %.1f–%.1f)"
            % (gd, z.GRAPPLE_MIN, z.GRAPPLE_MAX),
        )
    else:
        add_route("WestClimb", False, "missing climb")
        add_route("WestGrapple", False, "missing climb")

    if "Sp_Gazebo" in by:
        v = by["Sp_Gazebo"]["prefab"]["vault"]
        g = by["Sp_Gazebo"]["aabb"]
        east_clear = True
        for inst in instances:
            if inst["place"]["name"] == "Sp_Gazebo":
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

    if "Sp_ScaffoldA" in by and "Sp_ScaffoldB" in by:
        a = by["Sp_ScaffoldA"]["aabb"]
        b = by["Sp_ScaffoldB"]["aabb"]

        def deck_bottom(inst):
            best = None
            for box in inst["boxes"]:
                bot = box["aabb"][1]
                if bot > 1.0:
                    best = bot if best is None else min(best, bot)
            return best if best is not None else inst["aabb"][4]

        clear_a = deck_bottom(by["Sp_ScaffoldA"])
        clear_b = deck_bottom(by["Sp_ScaffoldB"])
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
    for row in GRAY:
        box = gray_box(row)
        if box[4] - box[1] < z.STEP and box[4] < 0.45:
            continue
        # The flag is overhead. The pole is the ground blocker.
        if row[0].endswith("_Flag"):
            continue
        named.append(("gray:" + row[0], box))

    samples = []
    for i in range(len(CHASE)):
        ax, az = CHASE[i]
        bx, bz = CHASE[(i + 1) % len(CHASE)]
        dist = ( (bx - ax) ** 2 + (bz - az) ** 2 ) ** 0.5
        steps = max(1, int(dist / 0.5))
        for s in range(steps):
            t = s / float(steps)
            samples.append((ax + (bx - ax) * t, az + (bz - az) * t))
    worst = 99.0
    worst_at = (0, 0, "")
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
        length += ((bx - ax) ** 2 + (bz - az) ** 2) ** 0.5
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
    print("instances %d unique %d (Z7 instances %d)" % (len(instances), len(cache), len(z7)))
    print("smaller-than-z7 %s batched-in-bootstrap %s" % (
        "yes" if smaller else "NO",
        "yes" if 'BuildDistrict("WorldZ1"' in open(os.path.join(z.ROOT, "Assets/Scripts/Level/MegaParkP1Bootstrap.cs"), encoding="utf-8").read() and "BatchDistrictMeshes" in open(os.path.join(z.ROOT, "Assets/Scripts/Level/MegaParkP1Bootstrap.cs"), encoding="utf-8").read() else "NO",
    ))
    floating.extend(z.gray_route_floats("Z1"))
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
        write_stills(instances)

    ok = (
        reachable == n and not floating and missing == 0 and scale_fails == 0
        and not open_hits and not loop_hits and not bowl_hits and not cover
        and smaller and deck_lock
    )
    return 0 if ok else 1


def write_stills(instances):
    os.makedirs(STILL_DIR, exist_ok=True)
    before = gray_tris()
    after = gray_tris() + z.prop_tris(instances)
    ground = []
    ground.extend(z.box_tris(20, -0.08, 20, 44, 0.08, 40, 0, (0.45, 0.36, 0.24)))
    ground.extend(z.box_tris(14, -0.02, 26, 16, 0.04, 14, 0, (0.72, 0.45, 0.38)))
    fig = z.figure_tris(22.0, 0.0, 12.0)
    top = ("ortho", 0.0, 40.0, 4.0, 40.0)
    eye_south = z.look_cam((24.0, 1.6, 6.5), (18.0, 1.8, 24.0), 58.0)
    eye_west = z.look_cam((1.2, 1.6, 26.0), (12.0, 1.6, 26.0), 62.0)
    z.render_view(ground + before, top, os.path.join(STILL_DIR, "before_top.png"))
    z.render_view(ground + before + fig, eye_south, os.path.join(STILL_DIR, "before_eye_south.png"))
    z.render_view(ground + before + z.figure_tris(4.0, 0.0, 24.0), eye_west, os.path.join(STILL_DIR, "before_eye_west.png"))
    z.render_view(ground + after, top, os.path.join(STILL_DIR, "after_top.png"))
    z.render_view(ground + after + fig, eye_south, os.path.join(STILL_DIR, "after_eye_south.png"))
    z.render_view(ground + after + z.figure_tris(4.0, 0.0, 24.0), eye_west, os.path.join(STILL_DIR, "after_eye_west.png"))
    write_split(ground + after, os.path.join(STILL_DIR, "split4.png"))
    print("stills " + STILL_DIR)


def gray_tris():
    tris = []
    for row in GRAY:
        name, x, y, zc, sx, sy, sz = row
        if name.startswith("Landmark"):
            col = (0.85, 0.55, 0.35)
        elif name.startswith("SoftPlay_Cube"):
            col = (0.55, 0.62, 0.70)
        else:
            col = (0.62, 0.42, 0.36)
        tris.extend(z.box_tris(x, y, zc, sx, sy, sz, 0, col))
    return tris


def write_split(tris, path):
    w, h = 1280, 720
    img = z.sky_image(w, h)
    cams = [
        z.look_cam((8.0, 1.6, 12.0), (16.0, 1.5, 24.0), 60.0),
        z.look_cam((36.0, 1.6, 12.0), (30.0, 1.4, 18.0), 60.0),
        z.look_cam((20.0, 1.6, 36.0), (16.0, 1.4, 28.0), 60.0),
        z.look_cam((4.0, 1.6, 32.0), (10.0, 1.6, 26.0), 60.0),
    ]
    vw, vh = w // 2, h // 2
    for i, cam in enumerate(cams):
        sub = z.render_persp(tris + z.figure_tris(12 + (i % 2) * 10, 0, 16 + (i // 2) * 8), vw, vh, cam[1], cam[2], cam[3])
        ox, oy = (i % 2) * vw, (i // 2) * vh
        img[oy:oy + vh, ox:ox + vw] = sub
    img[vh - 1:vh + 1, :] = (230, 230, 230)
    img[:, vw - 1:vw + 1] = (230, 230, 230)
    z.write_png(path, img)


if __name__ == "__main__":
    raise SystemExit(main())
