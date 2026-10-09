#!/usr/bin/env python3
"""Headless world check for the Z9 bar highway.

Gray bars, posts, and vault lips stay. The crouch slot under the steel stays
empty. Newsstands occupy the north pockets. Prints the same world-check line.
"""

import math
import os
import sys

import check_z7 as z

CHASE = [(38.4, 20.35), (114.0, 20.35), (114.0, 21.3), (38.4, 21.3)]
CHASE_CLEAR = 0.80
STILL_DIR = os.path.join(z.ROOT, "Docs/WorldStills/pass8")
LANDMARK = (66.8, 16.0)
BANNED = (
    "Brick_Wall.prefab",
    "Gazebo.prefab",
    "RooftopAC.prefab",
    "Scaffold_Bay.prefab",
    "Container_20.prefab",
    "Dock_Straight.prefab",
    "Restroom.prefab",
    "Playground.prefab",
    "FireEscape.prefab",
)


def box_of(x, y, zc, sx, sy, sz):
    return (x - sx * 0.5, y - sy * 0.5, zc - sz * 0.5, x + sx * 0.5, y + sy * 0.5, zc + sz * 0.5)


def bar_gray():
    rows = []
    for i in range(12):
        x = 44.0 + i * 6.0
        rows.append(("BarPost_S%d" % i, box_of(x, 0.57, 12.7, 0.22, 1.14, 0.22)))
        rows.append(("BarPost_N%d" % i, box_of(x, 0.57, 19.3, 0.22, 1.14, 0.22)))
        rows.append(("Bar_%d" % i, box_of(x, 1.2, 16.0, 0.14, 0.12, 6.8)))
        rows.append(("BarVault_N%d" % i, box_of(x + 1.6, 0.48, 18.55, 1.30, 0.96, 0.90)))
        rows.append(("BarVault_S%d" % i, box_of(x + 3.0, 0.48, 13.55, 1.30, 0.96, 0.90)))
    # Arch at x=66.8, posts z=14.25 and 17.75. Crown is overhead; posts are the keep-out.
    rows.append(("Landmark_Z9_PostS", box_of(66.8, 1.6, 14.25, 0.4, 3.2, 0.4)))
    rows.append(("Landmark_Z9_PostN", box_of(66.8, 1.6, 17.75, 0.4, 3.2, 0.4)))
    return rows


def overlaps(a, b):
    return (
        z.overlap_1(a[0], a[3], b[0], b[3]) > 0.02
        and z.overlap_1(a[1], a[4], b[1], b[4]) > 0.02
        and z.overlap_1(a[2], a[5], b[2], b[5]) > 0.02
    )


def crouch_blocked(box):
    """A solid in the standing slot under the bars."""
    slot = (38.0, 0.05, 15.4, 114.0, 1.05, 16.6)
    return overlaps(box, slot)


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
    places = z.parse_places(src, "Bars")
    z7 = z.parse_places(src, "Places")
    gray = bar_gray()
    cache = {}
    instances = []
    missing = 0
    scale_fails = 0
    floating = []
    open_hits = []
    loop_hits = []
    crouch_hits = []
    cover = []
    report = []
    repeated = []

    for p in places:
        if abs(p["scale"][0] - 1) > 0.001:
            scale_fails += 1
        base = os.path.basename(p["path"])
        if base in BANNED:
            repeated.append(p["name"] + " " + base)
        if p["path"] not in cache:
            path = os.path.join(z.ROOT, p["path"])
            cache[p["path"]] = z.parse_prefab(path) if os.path.isfile(path) else None
        prefab = cache[p["path"]]
        if prefab is None or prefab["collider_count"] == 0:
            missing += 1
            continue
        boxes = []
        all_pts = []
        for b in prefab["boxes"]:
            pts = z.world_points(prefab, b, p)
            boxes.append({"pts": pts, "aabb": z.aabb(pts), "name": prefab["names"].get(b["go"], "")})
            all_pts.extend(pts)
        instances.append({"place": p, "prefab": prefab, "boxes": boxes, "aabb": z.aabb(all_pts)})

    aabbs = [inst["aabb"] for inst in instances]
    for i, inst in enumerate(instances):
        rest = [aabbs[j] for j in range(len(aabbs)) if j != i]
        if not z.pivot_supported(inst, rest):
            floating.append(inst["place"]["name"])
        if z.hits_open(inst["aabb"]):
            open_hits.append(inst["place"]["name"])
        if not z.loop_clear(inst["aabb"]):
            loop_hits.append(inst["place"]["name"])
        if crouch_blocked(inst["aabb"]):
            crouch_hits.append(inst["place"]["name"])
        if z.dist_point_aabb(LANDMARK[0], LANDMARK[1], inst["aabb"]) < 1.2:
            cover.append(inst["place"]["name"] + " covers landmark")
        for name, box in gray:
            if overlaps(inst["aabb"], box):
                cover.append(inst["place"]["name"] + " overlaps " + name)

    by = {inst["place"]["name"]: inst for inst in instances}
    routes = []

    def add_route(name, ok, detail):
        routes.append((name, ok, detail))
        report.append(("OK  " if ok else "FAIL") + " " + name + " " + detail)

    bar = None
    for name, box in gray:
        if name == "Bar_0":
            bar = box
    under = (bar[1] - 0.0) if bar is not None else 0
    # Bar center y=1.2, size y=0.12, so the underside is 1.14.
    add_route(
        "BarCrouch",
        bar is not None and bar[1] >= z.CROUCH_H - 0.001 and not crouch_hits,
        "bar underside %.2f m (crouch %.2f) slot empty %s"
        % (bar[1] if bar else -1, z.CROUCH_H, "yes" if not crouch_hits else "NO"),
    )

    vault = None
    for name, box in gray:
        if name == "BarVault_N0":
            vault = box
    lip = vault[4] if vault is not None else 0
    approach = True
    if vault is not None:
        for inst in instances:
            b = inst["aabb"]
            if b[3] > vault[0] - 1.2 and b[0] < vault[0] and z.overlap_1(b[2], b[5], vault[2], vault[5]) > 0.2:
                approach = False
    add_route(
        "LipMantle",
        vault is not None and z.MANTLE_MIN <= lip <= z.MANTLE_MAX and z.VAULT_MIN <= lip <= z.VAULT_MAX and approach,
        "north lip %.2f m (vault band %.2f–%.2f) west approach %s"
        % (lip, z.VAULT_MIN, z.VAULT_MAX, "open" if approach else "blocked"),
    )

    if "Bar_StandA" in by and "Bar_StandC" in by:
        a = by["Bar_StandA"]["aabb"]
        c = by["Bar_StandC"]["aabb"]
        origin = ((a[0] + a[3]) * 0.5, a[4], (a[2] + a[5]) * 0.5)
        cornice = ((c[0] + c[3]) * 0.5, c[4], (c[2] + c[5]) * 0.5)
        gd = math.dist(origin, cornice)
        add_route(
            "CurbGrapple",
            z.GRAPPLE_MIN <= gd <= z.GRAPPLE_MAX and not crouch_hits,
            "rope %.2f m along the curb from the west stand to the third stand"
            % gd,
        )
    else:
        add_route("CurbGrapple", False, "missing stands")

    if "Bar_StandA" in by and "Bar_StandB" in by:
        a = by["Bar_StandA"]["aabb"]
        b = by["Bar_StandB"]["aabb"]
        gap = b[0] - a[3] if a[3] <= b[0] else a[0] - b[3]
        clear_a = roof_bottom(by["Bar_StandA"])
        clear_b = roof_bottom(by["Bar_StandB"])
        ok = clear_a >= z.CROUCH_H and clear_b >= z.CROUCH_H and 0.4 <= gap <= z.AIR_DASH + 0.02
        add_route(
            "StandDash",
            ok,
            "roof underside %.2f / %.2f m gap %.2f m (dash %.2f)"
            % (clear_a, clear_b, gap, z.AIR_DASH),
        )
    else:
        add_route("StandDash", False, "missing dash stands")

    named = [(inst["place"]["name"], inst["aabb"]) for inst in instances]
    named.extend(("gray:" + name, box) for name, box in gray)
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
        "CurbChase",
        blocked == 0 and length > 40.0 and worst >= CHASE_CLEAR,
        "length %.1f m clearance %.2f m at (%.1f, %.1f) vs %s blocked %d"
        % (length, worst, worst_at[0], worst_at[1], worst_at[2], blocked),
    )

    reachable = sum(1 for _, ok, _ in routes if ok)
    n = len(routes)
    z7_unique = len({p["path"] for p in z7})
    smaller = len(instances) < len(z7) and len(cache) < z7_unique
    boot = open(os.path.join(z.ROOT, "Assets/Scripts/Level/MegaParkP1Bootstrap.cs"), encoding="utf-8").read()
    batch_ok = 'BuildDistrict("WorldZ9", MegaParkWorldDistrict.Bars, table, true)' in boot
    line = "world-check routes=%d reachable=%d/%d floatingProps=%d missingColliders=%d scaleFails=%d" % (
        n, reachable, n, len(floating), missing, scale_fails,
    )
    print(line)
    print("instances %d unique %d" % (len(instances), len(cache)))
    print("smaller-than-z7 %s static-batch %s" % ("yes" if smaller else "NO", "yes" if batch_ok else "NO"))
    if floating:
        print("floating: " + ", ".join(floating))
    if loop_hits:
        print("472 m loop: " + ", ".join(loop_hits))
    if crouch_hits:
        print("crouch slot: " + ", ".join(crouch_hits))
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
        and not open_hits and not loop_hits and not crouch_hits and not cover
        and not repeated and smaller and batch_ok
    )
    return 0 if ok else 1


def write_stills(instances, gray):
    os.makedirs(STILL_DIR, exist_ok=True)
    tris_g = []
    for name, box in gray:
        sx, sy, sz = box[3] - box[0], box[4] - box[1], box[5] - box[2]
        cx, cy, cz = (box[0] + box[3]) * 0.5, (box[1] + box[4]) * 0.5, (box[2] + box[5]) * 0.5
        col = (0.55, 0.58, 0.62) if "Vault" not in name else (0.62, 0.62, 0.58)
        tris_g.extend(z.box_tris(cx, cy, cz, sx, sy, sz, 0, col))
    tris_p = []
    for inst in instances:
        col = (0.55, 0.32, 0.22) if "Newsstand" in inst["place"]["path"] else (0.45, 0.48, 0.52)
        for box in inst["boxes"]:
            tris_p.extend(z.pts_box(box["pts"], col))
    ground = z.box_tris(78, -0.05, 16, 90, 0.08, 14, 0, (0.34, 0.46, 0.28))
    view = ("ortho", 36.0, 116.0, 10.0, 23.0)
    eye = z.look_cam((36.0, 1.7, 14.5), (42.0, 1.2, 17.6), 50.0)
    fig = z.figure_tris(40.6, 0.0, 16.0)
    z.render_view(ground + tris_g, view, os.path.join(STILL_DIR, "z9_before_top.png"))
    z.render_view(ground + tris_g + tris_p, view, os.path.join(STILL_DIR, "z9_after_top.png"))
    z.render_view(ground + tris_g + fig, eye, os.path.join(STILL_DIR, "z9_before_eye.png"))
    z.render_view(ground + tris_g + tris_p + fig, eye, os.path.join(STILL_DIR, "z9_after_eye.png"))
    print("stills " + STILL_DIR)


if __name__ == "__main__":
    raise SystemExit(main())
