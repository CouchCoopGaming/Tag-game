"""FIBA court markings scaled onto the 22 x 12 m street slab.

One texture, one decal. Line centers are 5 cm wide. Distances along the length
scale by 22/28 and distances across the width scale by 12/15, so the boundary
is the slab and the circles stay round. FIBA measures to the outer edge of a
line; the stroke here is centered 2.5 cm inside that edge.
"""

import math
import os
import struct
import zlib

W = 12.0
L = 22.0
LINE = 0.05
SX = W / 15.0
SZ = L / 28.0
# 100 px per meter. Not a power of two; the importer must not rescale it.
PX_PER_M = 100
WP = int(W * PX_PER_M)
HP = int(L * PX_PER_M)

# Inner edge of the boundary line (the edge measurements start from).
Z_IN = L * 0.5 - LINE
X_IN = W * 0.5 - LINE

ASPHALT = (38, 38, 40)
KEY = (28, 58, 92)
WHITE = (236, 236, 228)


def basket_z(sign):
    """sign +1 is the north (+Z) basket."""
    return sign * (Z_IN - 1.575 * SZ)


def ft_z(sign):
    # 5.80 m to the outer edge of the free-throw line, then back to the stroke center.
    return sign * (Z_IN - (5.80 - LINE * 0.5) * SZ)


def lane_half():
    """Distance from center court to the center of a lane line."""
    return (4.90 * SX) * 0.5 - LINE * 0.5


def three_radius():
    return (6.75 - LINE * 0.5) * SZ


def three_x():
    """Center of the straight corner line."""
    outer = X_IN - 0.90 * SX
    return outer - LINE * 0.5


def ft_radius():
    return (1.80 - LINE * 0.5) * SZ


def restricted_radius():
    return (1.25 - LINE * 0.5) * SZ


def center_radius():
    return (1.80 - LINE * 0.5) * SZ


def _png(path, w, h, buf):
    """buf row 0 is south (texture v = 0). PNG stores the north row first."""
    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    rows = []
    stride = w * 3
    for y in range(h - 1, -1, -1):
        rows.append(b"\x00" + bytes(buf[y * stride:(y + 1) * stride]))
    raw = zlib.compress(b"".join(rows), 9)
    ihdr = struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)
    data = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", raw) + chunk(b"IEND", b"")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as handle:
        handle.write(data)


def _px(x, z):
    return (x + W * 0.5) / W * WP, (z + L * 0.5) / L * HP


def _fill_rect(buf, x0, z0, x1, z1, color):
    if x1 < x0:
        x0, x1 = x1, x0
    if z1 < z0:
        z0, z1 = z1, z0
    ax, ay = _px(x0, z0)
    bx, by = _px(x1, z1)
    ix0 = max(0, int(math.floor(min(ax, bx))))
    ix1 = min(WP, int(math.ceil(max(ax, bx))))
    iy0 = max(0, int(math.floor(min(ay, by))))
    iy1 = min(HP, int(math.ceil(max(ay, by))))
    if ix1 <= ix0 or iy1 <= iy0:
        return
    row = bytes(color) * (ix1 - ix0)
    span = (ix1 - ix0) * 3
    for y in range(iy0, iy1):
        i = (y * WP + ix0) * 3
        buf[i:i + span] = row


def _disc(buf, x, z, radius, color):
    ax, ay = _px(x - radius, z - radius)
    bx, by = _px(x + radius, z + radius)
    ix0 = max(0, int(math.floor(min(ax, bx))))
    ix1 = min(WP, int(math.ceil(max(ax, bx))))
    iy0 = max(0, int(math.floor(min(ay, by))))
    iy1 = min(HP, int(math.ceil(max(ay, by))))
    r2 = radius * radius
    for iy in range(iy0, iy1):
        zc = (iy + 0.5) / HP * L - L * 0.5
        dz = zc - z
        for ix in range(ix0, ix1):
            xc = (ix + 0.5) / WP * W - W * 0.5
            if (xc - x) * (xc - x) + dz * dz <= r2:
                i = (iy * WP + ix) * 3
                buf[i:i + 3] = bytes(color)


def _stroke(buf, x, z, color):
    _disc(buf, x, z, LINE * 0.5, color)


def _segment(buf, x0, z0, x1, z1, color):
    length = math.hypot(x1 - x0, z1 - z0)
    steps = max(2, int(length * PX_PER_M * 0.5))
    for i in range(steps + 1):
        t = i / float(steps)
        _stroke(buf, x0 + (x1 - x0) * t, z0 + (z1 - z0) * t, color)


def _arc(buf, cx, cz, radius, a0, a1, color, forward=1.0):
    """Angle 0 points toward center court. forward is +1 on the south basket."""
    span = a1 - a0
    length = abs(radius * span)
    steps = max(8, int(length * PX_PER_M * 0.5))
    for i in range(steps + 1):
        a = a0 + span * (i / float(steps))
        _stroke(buf, cx + math.sin(a) * radius, cz + forward * math.cos(a) * radius, color)


def _dashed_free_throw(buf, cx, cz, radius, forward):
    """Lane half of the free-throw circle. Dashes mirror across the lane center."""
    a0, a1 = math.pi * 0.5, math.pi * 1.5
    span = a1 - a0
    steps = max(8, int(abs(radius * span) * PX_PER_M * 0.5))
    dash, gap = 0.36, 0.16
    period = dash + gap
    for i in range(steps + 1):
        a = a0 + span * (i / float(steps))
        end = a0 if a <= math.pi else a1
        dist = abs(a - end) * radius
        if dist % period > dash:
            continue
        _stroke(buf, cx + math.sin(a) * radius, cz + forward * math.cos(a) * radius, color=WHITE)


def _noise(x, z):
    ix = int(abs(x) * 8.0)
    iz = int(abs(z) * 8.0)
    n = (ix * 374761393 + iz * 668265263) & 0xFFFFFFFF
    n = (n ^ (n >> 13)) * 1274126177 & 0xFFFFFFFF
    return (n & 255) / 255.0


def _end(buf, sign):
    bz = basket_z(sign)
    fz = ft_z(sign)
    half = lane_half()
    forward = -sign
    # Key, inside the lane lines, from the inner baseline to the inner free-throw line.
    base = sign * Z_IN
    inner_ft = fz - sign * LINE * 0.5
    _fill_rect(buf, -(half - LINE * 0.5), base, half - LINE * 0.5, inner_ft, KEY)
    # Lane sides and the free-throw line. The baseline is the fourth side.
    _segment(buf, -half, base, -half, fz, WHITE)
    _segment(buf, half, base, half, fz, WHITE)
    _segment(buf, -(half + LINE * 0.5), fz, half + LINE * 0.5, fz, WHITE)
    # Free-throw circle. Solid toward center court, dashed back into the lane.
    _arc(buf, 0.0, fz, ft_radius(), -math.pi * 0.5, math.pi * 0.5, WHITE, forward)
    _dashed_free_throw(buf, 0.0, fz, ft_radius(), forward)
    # Restricted arc, toward center court only.
    _arc(buf, 0.0, bz, restricted_radius(), -math.pi * 0.5, math.pi * 0.5, WHITE, forward)
    # Three-point arc plus the two straight corner lines. They share the meet point.
    r3 = three_radius()
    x3 = three_x()
    meet = math.asin(max(-1.0, min(1.0, x3 / r3)))
    _arc(buf, 0.0, bz, r3, -meet, meet, WHITE, forward)
    meet_z = bz + forward * math.cos(meet) * r3
    _segment(buf, -x3, base, -x3, meet_z, WHITE)
    _segment(buf, x3, base, x3, meet_z, WHITE)


def render(path):
    buf = bytearray(WP * HP * 3)
    for y in range(HP):
        z = (y + 0.5) / HP * L - L * 0.5
        row = bytearray()
        for x in range(WP):
            xm = (x + 0.5) / WP * W - W * 0.5
            n = _noise(xm, z)
            row.extend((
                int(ASPHALT[0] + n * 10),
                int(ASPHALT[1] + n * 10),
                int(ASPHALT[2] + n * 8),
            ))
        buf[y * WP * 3:(y + 1) * WP * 3] = row
    _end(buf, -1.0)
    _end(buf, 1.0)
    # Boundary. Strips overlap at the corners so the rectangle is closed.
    _fill_rect(buf, -W * 0.5, -L * 0.5, -X_IN, L * 0.5, WHITE)
    _fill_rect(buf, X_IN, -L * 0.5, W * 0.5, L * 0.5, WHITE)
    _fill_rect(buf, -W * 0.5, -L * 0.5, W * 0.5, -Z_IN, WHITE)
    _fill_rect(buf, -W * 0.5, Z_IN, W * 0.5, L * 0.5, WHITE)
    # Center line between the inner sidelines, and the center circle.
    _segment(buf, -X_IN, 0.0, X_IN, 0.0, WHITE)
    _arc(buf, 0.0, 0.0, center_radius(), 0.0, math.tau, WHITE, 1.0)
    _png(path, WP, HP, buf)
    return buf


def _sample(buf, x, z):
    px, py = _px(x, z)
    ix = min(WP - 1, max(0, int(px)))
    iy = min(HP - 1, max(0, int(py)))
    i = (iy * WP + ix) * 3
    return tuple(buf[i:i + 3])


def _white(rgb):
    return rgb[0] > 200 and rgb[1] > 200


def _at(buf, ix, iy):
    i = (iy * WP + ix) * 3
    return tuple(buf[i:i + 3])


def check(buf):
    """Return a list of problems. Empty means the paint is symmetric and joined."""
    problems = []
    # Mirror symmetry on the pixel grid. Step so the test stays cheap.
    for iy in range(0, HP, 7):
        for ix in range(0, WP // 2, 7):
            a = _at(buf, ix, iy)
            b = _at(buf, WP - 1 - ix, iy)
            c = _at(buf, ix, HP - 1 - iy)
            if a != b or a != c:
                problems.append("asymmetric pixel (%d, %d)" % (ix, iy))
                if len(problems) > 6:
                    return problems
    # Boundary and center line read as paint. A point 8 cm inside does not.
    if not _white(_sample(buf, 0.0, L * 0.5 - LINE * 0.5)):
        problems.append("baseline missing")
    if not _white(_sample(buf, W * 0.5 - LINE * 0.5, 0.0)):
        problems.append("sideline missing")
    if not _white(_sample(buf, 0.0, 0.0)):
        problems.append("center line missing")
    if _white(_sample(buf, 0.4, 0.4)):
        problems.append("paint spilled off the center lines")
    r3 = three_radius()
    x3 = three_x()
    meet = math.asin(x3 / r3)
    bz = basket_z(-1.0)
    mz = bz + math.cos(meet) * r3
    mx = math.sin(meet) * r3
    for px, pz, label in (
        (mx, mz, "3pt joint"),
        (x3, (mz - Z_IN) * 0.5, "3pt straight"),
        (math.sin(meet * 0.5) * r3, bz + math.cos(meet * 0.5) * r3, "3pt arc"),
        (-mx, -mz, "3pt joint north"),
    ):
        if not _white(_sample(buf, px, pz)):
            problems.append(label + " gap")
    # A point 8 cm beside the joint should not all be paint in a fat blob,
    # but the miter can be a little wide. Just require the open court to stay clear.
    if _white(_sample(buf, 0.0, 0.8)):
        problems.append("center circle filled solid")
    return problems


if __name__ == "__main__":
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_court_preview.png")
    image = render(out)
    issues = check(image)
    print("court", WP, HP, "issues", len(issues))
    for item in issues:
        print(" ", item)
    print("basket", round(basket_z(1), 3), "ft", round(ft_z(1), 3), "r3", round(three_radius(), 3), "x3", round(three_x(), 3))
