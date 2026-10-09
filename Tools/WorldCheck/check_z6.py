#!/usr/bin/env python3
"""Headless world check for the Z6 twin-fort district.

Gray crawls, spirals, decks, rims, hooks, and the yellow chutes stay where
MegaParkP1Layout put them. The dressed set is a harbor yard on the west
lawn: a container climb, a dock landing to the west, a harbor rail, and a
rope that crosses the empty z[46, 54] gap. The east spine x[130, 138] stays
empty. Prints the same world-check line as Z7. Stills are collider rasters.
"""

import math
import os
import sys

import check_z7 as z

# Closed dogleg around the forts. South leg is under the hopscotch. East leg
# threads the empty gap. North leg clears the shelters. West leg clears the rims.
CHASE = [(118.10, 13.5), (128.55, 13.5), (128.55, 86.8), (118.10, 86.8)]
CHASE_CLEAR = 0.80
STILL_DIR = os.path.join(z.ROOT, "Docs/WorldStills/pass7")
LANDMARKS = ((148.2, 28.8), (148.2, 70.4))
# South lip of the gap, east of Rim_GapS. The rope runs north from here.
GRAPPLE_FROM = (128.5, 1.6, 43.0)
REPEATED = (
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
)


def box_of(x, y, zc, sx, sy, sz):
    return (x - sx * 0.5, y - sy * 0.5, zc - sz * 0.5, x + sx * 0.5, y + sy * 0.5, zc + sz * 0.5)


def fort_gray():
    rows = []
    for fort, zc in (("Army", 28.2), ("Knight", 64.2)):
        rows.append((fort + "_CrawlL", box_of(123, 0.6, zc - 0.9, 7, 1.2, 0.22)))
        rows.append((fort + "_CrawlR", box_of(123, 0.6, zc + 0.9, 7, 1.2, 0.22)))
        rows.append((fort + "_CrawlRoof", box_of(123, 1.29, zc, 7, 0.18, 2.02)))
    for fort, x, zc in (("Army", 143.0, 28.0), ("Knight", 143.0, 70.0)):
        rows.append((fort + "_Core", box_of(x, 1.4, zc, 0.5, 2.8, 0.5)))
        rows.append((fort + "_L1", box_of(x - 1.5, 0.35, zc - 1.4, 1.5, 0.7, 1.5)))
        rows.append((fort + "_L2", box_of(x + 1.5, 0.7, zc - 1.4, 1.5, 1.4, 1.5)))
        rows.append((fort + "_L3", box_of(x + 1.5, 1.05, zc + 1.4, 1.5, 2.1, 1.5)))
        rows.append((fort + "_L4", box_of(x - 1.5, 1.4, zc + 1.4, 1.5, 2.8, 1.5)))
    rows.extend([
        ("Army_Lo", box_of(147.4, 1, 28, 3.6, 2, 3.4)),
        ("Army_Hi", box_of(148.2, 2.85, 28.8, 1.6, 1.7, 1.4)),
        ("Knight_Lo", box_of(147.4, 1, 71.2, 3.6, 2, 3.2)),
        ("Knight_Hi", box_of(148.2, 2.85, 70.4, 1.6, 1.7, 1.4)),
        ("Hook_Army_West", box_of(145.75, 3, 28.8, 0.3, 2, 1.6)),
        ("Hook_Knight_West", box_of(145.75, 3, 71.8, 0.3, 2, 1.6)),
        ("Rim_Corner", box_of(112, 1.5, 72.7, 8, 3, 7)),
        ("Hook_Rim_Corner", box_of(108.2, 4, 75.2, 0.30, 2, 2)),
        ("Rim_Knight", box_of(122, 1.4, 68.2, 6, 2.8, 5)),
        ("Hook_Rim_Knight", box_of(119.2, 3.8, 70, 0.30, 2, 1.2)),
        ("Rim_Ksouth", box_of(122, 1.1, 58.15, 5, 2.2, 8.3)),
        ("Hook_Rim_Ksouth", box_of(122, 3.2, 54.2, 3, 2, 0.30)),
        ("Rim_GapS", box_of(122, 1.1, 43.2, 5, 2.2, 5.6)),
        ("Hook_Rim_GapS", box_of(122, 3.2, 45.75, 3, 2, 0.30)),
        ("Rim_Army", box_of(122, 1, 34.85, 5, 2, 10.3)),
        ("Cover_N", box_of(124, 0.675, 73.2, 2.2, 1.35, 1.15)),
        ("Bar_EndDeck", box_of(122, 1, 24, 3.2, 2, 3.2)),
        ("Hook_Bar_End", box_of(120.55, 3, 24, 0.3, 2, 2.4)),
    ])
    for i in range(8):
        x = 124.0 + i * 3.1
        lip = 0.72 + (i % 4) * 0.22
        rows.append(("Hop_%d" % i, box_of(x, lip * 0.5, 15, 1.15, lip, 1.15)))
    return rows


def overlaps(a, b):
    return (
        z.overlap_1(a[0], a[3], b[0], b[3]) > 0.02
        and z.overlap_1(a[1], a[4], b[1], b[4]) > 0.02
        and z.overlap_1(a[2], a[5], b[2], b[5]) > 0.02
    )


def hits_band(box, x0, x1, z0, z1):
    return box[0] < x1 and box[3] > x0 and box[2] < z1 and box[5] > z0


def off_wall_west(face_x, spans, deck, blockers, angles=z.OFF_WALL_DEGREES):
    """Wall jump toward -X. face_x is the west face. spans are (z0, z1)."""
    details = []
    all_ok = True
    deck_top = deck[4]
    for ang in angles:
        rad = math.radians(ang)
        perp = z.WALL_JUMP_OUT + z.WALL_JUMP_LOOK * math.sin(rad)
        along = z.WALL_JUMP_LOOK * math.cos(rad)
        found = None
        y = 0.05
        while y <= z.CLIMB_PRACTICAL + 1e-6 and found is None:
            t_land = z.t_descend_to(y, deck_top)
            if t_land is None or perp <= 0.05:
                y += 0.05
                continue
            for sign in (1.0, -1.0):
                if found is not None:
                    break
                for z0, z1 in spans:
                    z_launch = z0
                    while z_launch <= z1 + 1e-6:
                        hit = False
                        steps = max(8, int(t_land / 0.02))
                        for s in range(1, steps + 1):
                            t = t_land * s / float(steps)
                            x = face_x - z.PAWN_R - perp * t
                            zz = z_launch + sign * along * t
                            feet = z.y_wall_jump(t, y)
                            top = feet + z.PLAYER_H
                            if (
                                s == steps
                                and deck[0] + z.PAWN_R <= x <= deck[3] - z.PAWN_R
                                and deck[2] + z.PAWN_R <= zz <= deck[5] - z.PAWN_R
                            ):
                                found = (y, sign, z_launch, x, zz)
                                break
                            for box in blockers:
                                if feet >= box[4] - 0.02 or top <= box[1] + 0.02:
                                    continue
                                if z._circle_hits(x, zz, z.PAWN_R, box):
                                    hit = True
                                    break
                            if hit or found is not None:
                                break
                        if found is not None:
                            break
                        z_launch += 0.25
                    if found is not None:
                        break
            y += 0.05
        if found is None:
            all_ok = False
            details.append("%+.0f deg MISS (perp %.2f m)" % (ang, z.perp_range(ang)))
        else:
            details.append(
                "%+.0f deg launch %.2f m land (%.2f, %.2f) perp %.2f m"
                % (ang, found[0], found[3], found[4], z.perp_range(ang))
            )
    return all_ok, "; ".join(details)


def plank_deck(inst):
    planks = []
    for box in inst["boxes"]:
        if (box.get("name") or "").startswith("Col_Plank"):
            planks.append(box["aabb"])
    if not planks:
        return None
    return (
        min(b[0] for b in planks),
        min(b[1] for b in planks),
        min(b[2] for b in planks),
        max(b[3] for b in planks),
        max(b[4] for b in planks),
        max(b[5] for b in planks),
    )


def main():
    os.chdir(z.ROOT)
    src = open(z.DISTRICT_CS, encoding="utf-8").read()
    layout = open(os.path.join(z.ROOT, "Assets/Scripts/Level/MegaParkP1Layout.cs"), encoding="utf-8").read()
    fort_lock = (
        'AddCrawl(list, "Army"' in layout
        and 'AddCrawl(list, "Knight"' in layout
        and 'AddSpiral(list, "Army"' in layout
        and 'AddSpiral(list, "Knight"' in layout
        and "static void AddFortDecks(" in layout
        and 'Mast(list, "Landmark_Z6_Army"' in layout
        and 'Mast(list, "Landmark_Z6_Knight"' in layout
        and 'RampOf("Slide_ArmyHi"' in layout
        and 'RampOf("Slide_KnightHi"' in layout
    )
    places = z.parse_places(src, "Forts")
    z7 = z.parse_places(src, "Places")
    gray = fort_gray()
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

    for p in places:
        if abs(p["scale"][0] - 1) > 0.001 or abs(p["scale"][1] - 1) > 0.001 or abs(p["scale"][2] - 1) > 0.001:
            scale_fails += 1
        base = os.path.basename(p["path"])
        if base in REPEATED:
            repeated.append(p["name"] + " " + base)
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
    spine_hits = []
    gap_hits = []
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
        if hits_band(b, 130.0, 138.0, 10.0, 90.0):
            spine_hits.append(inst["place"]["name"])
        if hits_band(b, 118.0, 158.0, 46.0, 54.0):
            gap_hits.append(inst["place"]["name"])
        for mx, mz in LANDMARKS:
            if z.dist_point_aabb(mx, mz, b) < 1.2:
                cover.append(inst["place"]["name"] + " covers landmark")
        for name, box in gray:
            if overlaps(b, box):
                cover.append(inst["place"]["name"] + " overlaps " + name)

    by = {inst["place"]["name"]: inst for inst in instances}
    routes = []

    def add_route(name, ok, detail):
        routes.append((name, ok, detail))
        report.append(("OK  " if ok else "FAIL") + " " + name + " " + detail)

    if "Ft_Climb" in by and "Ft_Dock" in by:
        climb = by["Ft_Climb"]
        body = None
        for box in climb["boxes"]:
            if box.get("name") == "Climb_Body":
                body = box["aabb"]
        deck = plank_deck(by["Ft_Dock"])
        face = body[0] if body is not None else climb["aabb"][0]
        span = (body[2], body[5]) if body is not None else (climb["aabb"][2], climb["aabb"][5])
        wall_h = climb["aabb"][4] - climb["aabb"][1]
        run_len = span[1] - span[0]
        blockers = []
        for inst in instances:
            if inst["place"]["name"] == "Ft_Climb":
                for box in inst["boxes"]:
                    if box.get("name") == "Climb_Body":
                        continue
                    blockers.append(box["aabb"])
                continue
            if inst["place"]["name"] == "Ft_Dock":
                for box in inst["boxes"]:
                    if (box.get("name") or "").startswith("Col_Plank"):
                        continue
                    blockers.append(box["aabb"])
                continue
            for box in inst["boxes"]:
                blockers.append(box["aabb"])
        for name, box in gray:
            blockers.append(box)
        arc_ok, arc_detail = (False, "no deck")
        if deck is not None:
            arc_ok, arc_detail = off_wall_west(face, [span], deck, blockers)
        deck_gap = (face - deck[3]) if deck is not None else -1
        ok = (
            climb["prefab"]["climbable"]
            and wall_h + 0.05 >= 0.32
            and run_len >= 4.0
            and deck_gap > 0.5
            and abs(deck_gap - 4.10) > 0.15
            and arc_ok
            and fort_lock
            and not repeated
        )
        add_route(
            "YardClimb",
            ok,
            "west jump wall %.2f m on a %.2f m face (max %.2f) deck gap %.2f m gray locked %s; %s"
            % (wall_h, run_len, z.WALL_RUN_DIST, deck_gap, "yes" if fort_lock else "NO", arc_detail),
        )
    else:
        add_route("YardClimb", False, "missing climb or dock")

    if "Ft_Anchor" in by:
        anchor = by["Ft_Anchor"]
        body = None
        for box in anchor["boxes"]:
            if box.get("name") == "Climb_Body":
                body = box["aabb"]
        if body is None:
            body = anchor["aabb"]
        cornice = ((body[0] + body[3]) * 0.5, body[4], body[2])
        origin = GRAPPLE_FROM
        gd = math.sqrt(sum((cornice[k] - origin[k]) ** 2 for k in range(3)))
        crosses = origin[2] < 46.0 and cornice[2] > 54.0 and not gap_hits
        add_route(
            "GapGrapple",
            z.GRAPPLE_MIN <= gd <= z.GRAPPLE_MAX and crosses and fort_lock,
            "rope %.2f m from the south lip across the empty gap to the blue container (range %.1f–%.1f) crosses %s"
            % (gd, z.GRAPPLE_MIN, z.GRAPPLE_MAX, "yes" if crosses else "NO"),
        )
    else:
        add_route("GapGrapple", False, "missing anchor")

    if "Ft_Rail" in by:
        v = by["Ft_Rail"]["prefab"]["vault"]
        g = by["Ft_Rail"]["aabb"]
        west_clear = True
        for inst in instances:
            if inst["place"]["name"] == "Ft_Rail":
                continue
            b = inst["aabb"]
            if b[4] < 0.5:
                continue
            if b[3] > g[0] - 1.2 and b[0] < g[0] and z.overlap_1(b[2], b[5], g[2], g[5]) > 0.2:
                west_clear = False
        ok = z.MANTLE_MIN <= v <= z.MANTLE_MAX and z.VAULT_MIN <= v <= z.VAULT_MAX and west_clear
        add_route(
            "RailVault",
            ok,
            "rail %.2f m (mantle %.2f–%.2f, vault band %.2f–%.2f) west walk-up %s"
            % (v, z.MANTLE_MIN, z.MANTLE_MAX, z.VAULT_MIN, z.VAULT_MAX, "open" if west_clear else "blocked"),
        )
    else:
        add_route("RailVault", False, "missing rail")

    if "Ft_ShelterA" in by and "Ft_ShelterB" in by:
        a = by["Ft_ShelterA"]["aabb"]
        b = by["Ft_ShelterB"]["aabb"]

        def roof_bottom(inst):
            best = None
            for box in inst["boxes"]:
                bot = box["aabb"][1]
                if bot > 1.0:
                    best = bot if best is None else min(best, bot)
            return best if best is not None else inst["aabb"][4]

        clear_a = roof_bottom(by["Ft_ShelterA"])
        clear_b = roof_bottom(by["Ft_ShelterB"])
        gap = b[0] - a[3] if a[3] <= b[0] else a[0] - b[3]
        ok = clear_a >= z.CROUCH_H - 0.001 and clear_b >= z.CROUCH_H - 0.001 and 0.4 <= gap <= z.AIR_DASH + 0.02
        add_route(
            "ShelterDash",
            ok,
            "roof underside %.2f / %.2f m (crouch %.2f) gap between shelters %.2f m (dash %.2f)"
            % (clear_a, clear_b, z.CROUCH_H, gap, z.AIR_DASH),
        )
    else:
        add_route("ShelterDash", False, "missing shelters")

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
    chase_ok = blocked == 0 and length > 40.0 and worst >= CHASE_CLEAR and not spine_hits
    add_route(
        "FortChase",
        chase_ok,
        "length %.1f m clearance %.2f m at (%.1f, %.1f) vs %s samples %d blocked %d"
        % (length, worst, worst_at[0], worst_at[1], worst_at[2], len(samples), blocked),
    )

    reachable = sum(1 for _, ok, _ in routes if ok)
    n = len(routes)
    z7_unique = len({p["path"] for p in z7})
    smaller = len(instances) < len(z7) and len(cache) < z7_unique
    boot = open(os.path.join(z.ROOT, "Assets/Scripts/Level/MegaParkP1Bootstrap.cs"), encoding="utf-8").read()
    batch_ok = 'BuildDistrict("WorldZ6", MegaParkWorldDistrict.Forts, table, true)' in boot
    line = "world-check routes=%d reachable=%d/%d floatingProps=%d missingColliders=%d scaleFails=%d" % (
        n, reachable, n, len(floating), missing, scale_fails,
    )
    print(line)
    print("instances %d unique %d (Z7 instances %d unique %d)" % (len(instances), len(cache), len(z7), z7_unique))
    print("smaller-than-z7 %s static-batch %s gray-locked %s" % (
        "yes" if smaller else "NO", "yes" if batch_ok else "NO", "yes" if fort_lock else "NO"))
    if floating:
        print("floating: " + ", ".join(floating))
    if open_hits:
        print("open rect: " + ", ".join(open_hits))
    if loop_hits:
        print("472 m loop: " + ", ".join(loop_hits))
    if bowl_hits:
        print("bowl: " + ", ".join(bowl_hits))
    if spine_hits:
        print("spine: " + ", ".join(spine_hits))
    if gap_hits:
        print("gap: " + ", ".join(gap_hits))
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
        and not spine_hits and not gap_hits and not repeated
        and smaller and batch_ok and fort_lock
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
        if name.startswith("Hop"):
            col = (0.35, 0.48, 0.72)
        elif name.startswith("Slide"):
            col = (0.85, 0.75, 0.20)
        elif "Knight" in name or name.startswith("Rim_K"):
            col = (0.45, 0.32, 0.48)
        else:
            col = (0.42, 0.48, 0.32)
        tris.extend(z.box_tris(cx, cy, cz, sx, sy, sz, 0, col))
    return tris


def prop_tris(instances):
    tris = []
    for inst in instances:
        name = inst["place"]["name"]
        path = inst["place"]["path"]
        if "Container_20_Blue" in path:
            col = (0.22, 0.38, 0.62)
        elif "Container" in path:
            col = (0.62, 0.28, 0.18)
        elif "Dock" in path:
            col = (0.48, 0.38, 0.26)
        elif "HarborRail" in path:
            col = (0.62, 0.64, 0.66)
        elif "BusShelter" in path:
            col = (0.40, 0.58, 0.64)
        elif "Tree" in path:
            col = (0.16, 0.40, 0.22)
        elif "Dumpster" in path:
            col = (0.22, 0.42, 0.28)
        elif "Seesaw" in path:
            col = (0.72, 0.46, 0.22)
        elif "Fountain" in path:
            col = (0.55, 0.64, 0.70)
        elif "Sign" in name:
            col = (0.36, 0.48, 0.28)
        elif "Light" in name:
            col = (0.72, 0.68, 0.48)
        elif "Bollard" in name or "Barrel" in name:
            col = (0.78, 0.55, 0.16)
        else:
            col = (0.58, 0.50, 0.40)
        for box in inst["boxes"]:
            tris.extend(z.pts_box(box["pts"], col))
    return tris


def write_stills(instances, gray):
    os.makedirs(STILL_DIR, exist_ok=True)
    before = gray_tris(gray)
    after = gray_tris(gray) + prop_tris(instances)
    ground = z.box_tris(124, -0.05, 50, 20, 0.08, 90, 0, (0.36, 0.42, 0.30))
    south = ("ortho", 116.0, 132.0, 12.0, 32.0)
    north = ("ortho", 116.0, 132.0, 40.0, 90.0)
    eye = z.look_cam((117.2, 1.7, 14.2), (123.4, 1.1, 19.0), 55.0)
    fig = z.figure_tris(121.2, 0.0, 17.5)
    z.render_view(ground + before, south, os.path.join(STILL_DIR, "z6_before_top_south.png"))
    z.render_view(ground + after, south, os.path.join(STILL_DIR, "z6_after_top_south.png"))
    z.render_view(ground + before, north, os.path.join(STILL_DIR, "z6_before_top_north.png"))
    z.render_view(ground + after, north, os.path.join(STILL_DIR, "z6_after_top_north.png"))
    z.render_view(ground + before + fig, eye, os.path.join(STILL_DIR, "z6_before_eye.png"))
    z.render_view(ground + after + fig, eye, os.path.join(STILL_DIR, "z6_after_eye.png"))
    print("stills " + STILL_DIR)


if __name__ == "__main__":
    raise SystemExit(main())
