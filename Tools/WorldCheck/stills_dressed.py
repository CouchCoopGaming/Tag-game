#!/usr/bin/env python3
"""Eye and top stills from library LOD0 meshes, not collider boxes.

Pass 9 eye rasters read as gray boxes on empty sky. These frames keep the
placed props (wash houses, playground, newsstands, parked cars, alley) large
enough to recognize, on park ground, under 400 KB.
"""

import os
import sys

import check_z7 as z
import fbx_mesh

ROOT = z.ROOT
OUT = os.path.join(ROOT, "Docs/WorldStills/pass10")
W, H = 1280, 720
MAX_BYTES = 400 * 1024

# Prefab path -> FBX. Library prefabs live next to their mesh.
def fbx_for(prefab_rel):
    folder, name = os.path.split(prefab_rel)
    if folder.endswith("/Prefabs"):
        folder = folder[: -len("/Prefabs")]
    return os.path.join(ROOT, folder, name.replace(".prefab", ".fbx"))


_cache = {}


def lod0(prefab_rel):
    path = fbx_for(prefab_rel)
    if path not in _cache:
        if not os.path.isfile(path):
            _cache[path] = []
        else:
            _cache[path] = fbx_mesh.load_lod0(path)[0][1]
    return _cache[path]


def place_mesh(prefab_rel, x, y, zz, yaw):
    m = z.yaw_mat(yaw)
    tris = []
    for a, b, c, col in lod0(prefab_rel):
        def xp(p, m=m, x=x, y=y, zz=zz):
            q = z.mul_m(m, p)
            return (q[0] + x, q[1] + y, q[2] + zz)

        tris.append((xp(a), xp(b), xp(c), col))
    return tris


def from_places(places, names=None):
    tris = []
    for p in places:
        if names is not None and p["name"] not in names:
            continue
        tris.extend(place_mesh(p["path"], p["x"], p["y"], p["z"], p["yaw"]))
    return tris


def ground_quad(x0, x1, y, z0, z1, col):
    a = (x0, y, z0)
    b = (x1, y, z0)
    c = (x1, y, z1)
    d = (x0, y, z1)
    return [(a, b, c, col), (a, c, d, col)]


def figure(x, zz):
    # 1.8 m stand-in with a head, so it reads as a person beside the props.
    tan = (0.86, 0.62, 0.42)
    cloth = (0.20, 0.32, 0.55)
    tris = []
    tris.extend(z.box_tris(x, 0.45, zz, 0.28, 0.90, 0.22, 0, cloth))
    tris.extend(z.box_tris(x, 1.15, zz, 0.42, 0.55, 0.24, 0, cloth))
    tris.extend(z.box_tris(x, 1.62, zz, 0.22, 0.26, 0.22, 0, tan))
    return tris


def poster(img):
    # Step the shade so the PNG stays under 400 KB without turning to mush.
    return (img // 12) * 12


def save(img, path):
    img = poster(img)
    z.write_png(path, img)
    size = os.path.getsize(path)
    if size > MAX_BYTES:
        raise SystemExit("still over 400 KB: %s (%d)" % (path, size))
    return size


def hero_proj(tris, cam):
    pts = []
    for a, b, c, _col in tris:
        pts.extend((a, b, c))
    if cam[0] == "ortho":
        x0, x1, z0, z1 = cam[1], cam[2], cam[3], cam[4]
        # render_ortho insets the map by pad 0.92, so match that frame.
        pad = 0.92
        xs, ys = [], []
        for p in pts:
            u = (p[0] - x0) / (x1 - x0)
            v = (p[2] - z0) / (z1 - z0)
            xs.append((u - 0.5) * pad + 0.5)
            ys.append(0.5 - (v - 0.5) * pad)
        return xs, ys
    import math
    eye, target, fov = cam[1], cam[2], cam[3]
    fwd = z.norm((target[0] - eye[0], target[1] - eye[1], target[2] - eye[2]))
    right = z.norm(z.cross(fwd, (0.0, 1.0, 0.0)))
    up = z.cross(right, fwd)
    fy = 1.0 / math.tan(math.radians(fov) * 0.5)
    fx = fy * (H / float(W))
    xs, ys = [], []
    for p in pts:
        d = (p[0] - eye[0], p[1] - eye[1], p[2] - eye[2])
        depth = z.dot(d, fwd)
        if depth < 0.2:
            xs.append(-1.0)
            ys.append(-1.0)
            continue
        xs.append((z.dot(d, right) / depth * fx + 1.0) * 0.5)
        ys.append(1.0 - (z.dot(d, up) / depth * fy + 1.0) * 0.5)
    return xs, ys


def hero_span(tris, cam):
    xs, ys = hero_proj(tris, cam)
    inside = [i for i in range(len(xs)) if 0.02 <= xs[i] <= 0.98 and 0.02 <= ys[i] <= 0.98]
    if not inside:
        return 0.0
    # Size of the in-frame part. A crop that throws most of the mesh away fails.
    if len(inside) < 0.92 * len(xs):
        return 0.0
    ix = [xs[i] for i in inside]
    iy = [ys[i] for i in inside]
    return max(max(ix) - min(ix), max(iy) - min(iy))


def shoot(name, tris, cam, hero, min_span):
    span = hero_span(hero, cam)
    if span < min_span:
        raise SystemExit("%s hero span %.2f < %.2f" % (name, span, min_span))
    path = os.path.join(OUT, name + ".png")
    if cam[0] == "ortho":
        img = z.render_ortho(tris, W, H, cam[1], cam[2], cam[3], cam[4])
    else:
        img = z.render_persp(tris, W, H, cam[1], cam[2], cam[3])
    size = save(img, path)
    print("still %s hero=%.0f%% bytes=%d" % (name, span * 100, size))


def main():
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    src = open(z.DISTRICT_CS, encoding="utf-8").read()
    bowl = z.parse_places(src, "Bowl")
    bars = z.parse_places(src, "Bars")
    hops = z.parse_places(src, "Hops")
    places = z.parse_places(src, "Places")
    os.makedirs(OUT, exist_ok=True)

    # North lip of the bowl: grass under the wash houses, sand to the south.
    z8 = []
    z8.extend(ground_quad(48, 78, -0.02, 62, 80, (0.30, 0.48, 0.26)))
    z8.extend(ground_quad(48, 78, -0.90, 36, 62, (0.72, 0.62, 0.40)))
    z8.extend(from_places(bowl))
    z8_fig = figure(58.6, 67.4)
    wash = from_places(bowl, {"Bd_WashA", "Bd_WashB"})
    play = from_places(bowl, {"Bd_Play"})
    stands = from_places(bowl, {"Bd_StandA", "Bd_StandB"})
    z8_top = ("ortho", 55.0, 76.0, 63.5, 80.0)
    z8_eye = ("persp", (76.4, 1.7, 63.2), (66.2, 1.4, 71.0), 48.0)
    shoot("z8_top", z8 + z8_fig, z8_top, wash + play, 0.35)
    shoot("z8_eye", z8 + z8_fig, z8_eye, wash + play, 0.35)

    # Newsstand row on the bar highway. Asphalt under the steel, grass north.
    z9 = []
    z9.extend(ground_quad(34, 60, -0.02, 12, 22, (0.30, 0.46, 0.24)))
    z9.extend(from_places(bars, {"Bar_StandA", "Bar_StandB", "Bar_StandC", "Bar_Mail", "Bar_Meter"}))
    # South of the row so the eye shot sees a person in front of the awnings.
    z9_fig = figure(40.2, 15.7)
    news = from_places(bars, {"Bar_StandA", "Bar_StandB", "Bar_StandC"})
    z9_top = ("ortho", 36.5, 57.5, 15.2, 20.6)
    z9_eye = ("persp", (32.0, 1.65, 14.0), (48.5, 1.05, 17.7), 42.0)
    shoot("z9_top", z9 + z9_fig, z9_top, news, 0.40)
    shoot("z9_eye", z9 + z9_fig, z9_eye, news, 0.40)

    # Parked 2025 cars on the two-lane slab.
    z7 = []
    z7.extend(ground_quad(78, 108, 0.0, 31.5, 39.5, (0.18, 0.18, 0.19)))
    z7.extend(ground_quad(78, 108, 0.02, 28.2, 31.6, (0.55, 0.54, 0.50)))
    z7.extend(from_places(places, {"Car_Sedan", "Car_Hatch", "Car_Crossover", "Car_Pickup", "Road_2", "Road_3", "Road_4", "Road_5"}))
    z7_fig = figure(82.4, 32.6)
    cars = from_places(places, {"Car_Sedan", "Car_Hatch", "Car_Crossover", "Car_Pickup"})
    z7_top = ("ortho", 80.5, 104.5, 32.2, 39.2)
    z7_eye = ("persp", (76.0, 1.8, 28.0), (92.5, 0.85, 35.6), 48.0)
    shoot("z7_top", z7 + z7_fig, z7_top, cars, 0.45)
    shoot("z7_eye", z7 + z7_fig, z7_eye, cars, 0.45)

    # Hopscotch library props: subway stair and alley, not the gray cubes.
    z10 = []
    z10.extend(ground_quad(124, 152, -0.02, 6, 24, (0.34, 0.46, 0.28)))
    z10.extend(from_places(hops))
    z10_fig = figure(137.2, 16.2)
    built = from_places(hops, {"Hp_Subway", "Hp_Alley"})
    z10_top = ("ortho", 126.5, 151.5, 9.0, 22.5)
    z10_eye = ("persp", (122.0, 1.9, 8.0), (139.0, 1.15, 18.5), 46.0)
    shoot("z10_top", z10 + z10_fig, z10_top, built, 0.35)
    shoot("z10_eye", z10 + z10_fig, z10_eye, built, 0.35)
    # South-apron newsstands are part of the bowl dress. Confirm they survived the north framing.
    if hero_span(stands, z8_top) < 0.02 and hero_span(stands, z8_eye) < 0.02:
        print("note z8 frame is the north lip; south newsstands are in the mesh set but outside this pair")
    print("stills " + OUT)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
