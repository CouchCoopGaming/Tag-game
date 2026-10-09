"""Read Blender FBX 7400 meshes. LOD0 triangles and diffuse colors. Y-up."""

import struct
import zlib


def _u32(data, pos):
    return struct.unpack_from("<I", data, pos)[0], pos + 4


def _props(data, pos, count):
    out = []
    for _ in range(count):
        code = data[pos]
        pos += 1
        if code == ord("Y"):
            out.append(struct.unpack_from("<h", data, pos)[0])
            pos += 2
        elif code == ord("C"):
            out.append(data[pos] != 0)
            pos += 1
        elif code == ord("I"):
            out.append(struct.unpack_from("<i", data, pos)[0])
            pos += 4
        elif code == ord("F"):
            out.append(struct.unpack_from("<f", data, pos)[0])
            pos += 4
        elif code == ord("D"):
            out.append(struct.unpack_from("<d", data, pos)[0])
            pos += 8
        elif code == ord("L"):
            out.append(struct.unpack_from("<q", data, pos)[0])
            pos += 8
        elif code in (ord("S"), ord("R")):
            n = struct.unpack_from("<I", data, pos)[0]
            pos += 4
            raw = data[pos:pos + n]
            pos += n
            out.append(raw.decode("utf-8", "replace") if code == ord("S") else raw)
        elif code in (ord("f"), ord("d"), ord("i"), ord("l"), ord("b")):
            n, enc, comp = struct.unpack_from("<III", data, pos)
            pos += 12
            raw = data[pos:pos + comp]
            pos += comp
            if enc == 1:
                raw = zlib.decompress(raw)
            fmt = {ord("f"): "f", ord("d"): "d", ord("i"): "i", ord("l"): "q", ord("b"): "B"}[code]
            size = struct.calcsize("<" + fmt)
            out.append(list(struct.unpack_from("<%d%s" % (n, fmt), raw)))
        else:
            raise ValueError("fbx prop %s" % chr(code))
    return out, pos


def _node(data, pos, end):
    nodes = []
    while pos < end:
        end_off, num, _plen, name_len = struct.unpack_from("<IIIB", data, pos)
        pos += 13
        if end_off == 0:
            break
        name = data[pos:pos + name_len].decode("ascii", "replace")
        pos += name_len
        props, pos = _props(data, pos, num)
        children = []
        if pos < end_off:
            children, pos = _node(data, pos, end_off)
        pos = end_off
        nodes.append((name, props, children))
    return nodes, pos


def _find(nodes, name):
    for n, props, kids in nodes:
        if n == name:
            return props, kids
    return None, None


def _child(nodes, name):
    props, kids = _find(nodes, name)
    return kids or []


def _prop(nodes, name, default=None):
    props, _kids = _find(nodes, name)
    if not props:
        return default
    return props[0]


def _vec3(nodes, name, default):
    props, _kids = _find(nodes, name)
    if not props:
        return default
    value = props[0]
    if isinstance(value, list) and len(value) >= 3:
        return (value[0], value[1], value[2])
    if len(props) >= 3 and all(isinstance(v, (int, float)) for v in props[:3]):
        return (props[0], props[1], props[2])
    return default


def _mat(t, r, s):
    # Euler XYZ degrees, then scale, matching FBX Lcl Rotation.
    import math
    ax, ay, az = [math.radians(v) for v in r]
    cx, sx = math.cos(ax), math.sin(ax)
    cy, sy = math.cos(ay), math.sin(ay)
    cz, sz = math.cos(az), math.sin(az)
    # R = Rz * Ry * Rx
    r00 = cy * cz
    r01 = cz * sx * sy - cx * sz
    r02 = sx * sz + cx * cz * sy
    r10 = cy * sz
    r11 = cx * cz + sx * sy * sz
    r12 = cx * sy * sz - cz * sx
    r20 = -sy
    r21 = cy * sx
    r22 = cx * cy
    return (
        (r00 * s[0], r01 * s[1], r02 * s[2], t[0]),
        (r10 * s[0], r11 * s[1], r12 * s[2], t[1]),
        (r20 * s[0], r21 * s[1], r22 * s[2], t[2]),
    )


def _mul(a, b):
    out = []
    for i in range(3):
        row = []
        for j in range(3):
            row.append(a[i][0] * b[0][j] + a[i][1] * b[1][j] + a[i][2] * b[2][j])
        row.append(a[i][0] * b[0][3] + a[i][1] * b[1][3] + a[i][2] * b[2][3] + a[i][3])
        out.append(tuple(row))
    return tuple(out)


def _apply(m, p):
    return (
        m[0][0] * p[0] + m[0][1] * p[1] + m[0][2] * p[2] + m[0][3],
        m[1][0] * p[0] + m[1][1] * p[1] + m[1][2] * p[2] + m[1][3],
        m[2][0] * p[0] + m[2][1] * p[1] + m[2][2] * p[2] + m[2][3],
    )


_IDENT = ((1, 0, 0, 0), (0, 1, 0, 0), (0, 0, 1, 0))


def load_lod0(path):
    data = open(path, "rb").read()
    if not data.startswith(b"Kaydara FBX Binary"):
        raise ValueError("not binary fbx: " + path)
    version = struct.unpack_from("<I", data, 23)[0]
    if version >= 7500:
        raise ValueError("fbx %d not supported" % version)
    nodes, _pos = _node(data, 27, len(data))
    objects = _child(nodes, "Objects")
    connections = _child(nodes, "Connections")

    geos = {}
    models = {}
    materials = {}
    for name, props, kids in objects:
        if name == "Geometry" and props:
            geos[props[0]] = (props[1] if len(props) > 1 else "", kids)
        elif name == "Model" and props:
            models[props[0]] = (props[1] if len(props) > 1 else "", kids)
        elif name == "Material" and props:
            materials[props[0]] = (props[1] if len(props) > 1 else "", kids)

    # child id -> parent id, and OO links model->geometry, model->material
    parent = {}
    geo_of = {}
    mats_of = {}
    for name, props, _kids in connections:
        if name != "C" or len(props) < 3:
            continue
        kind, child, parent_id = props[0], props[1], props[2]
        if kind == "OO":
            parent.setdefault(child, parent_id)
            if child in geos and parent_id in models:
                geo_of.setdefault(parent_id, child)
            if child in materials and parent_id in models:
                mats_of.setdefault(parent_id, []).append(child)

    def xform_of(mid):
        chain = []
        seen = set()
        cur = mid
        while cur in models and cur not in seen:
            seen.add(cur)
            _label, kids = models[cur]
            t = (0.0, 0.0, 0.0)
            r = (0.0, 0.0, 0.0)
            s = (1.0, 1.0, 1.0)
            for pn, pp, _pk in _child(kids, "Properties70"):
                if pn != "P" or not pp:
                    continue
                if pp[0] == "Lcl Translation" and len(pp) >= 7:
                    t = (float(pp[4]), float(pp[5]), float(pp[6]))
                elif pp[0] == "Lcl Rotation" and len(pp) >= 7:
                    r = (float(pp[4]), float(pp[5]), float(pp[6]))
                elif pp[0] == "Lcl Scaling" and len(pp) >= 7:
                    s = (float(pp[4]), float(pp[5]), float(pp[6]))
            chain.append(_mat(t, r, s))
            cur = parent.get(cur)
        m = _IDENT
        for step in reversed(chain):
            m = _mul(m, step)
        return m

    def diffuse(mid):
        cols = []
        for mat_id in mats_of.get(mid, []):
            _label, kids = materials[mat_id]
            color = _vec3(kids, "DiffuseColor", None)
            label = _label.split("\x00")[0]
            if color is None:
                for pn, pp, _pk in _child(kids, "Properties70"):
                    if pn == "P" and pp and pp[0] in ("Diffuse", "DiffuseColor") and len(pp) >= 7:
                        color = (float(pp[4]), float(pp[5]), float(pp[6]))
            cols.append((label, color or (0.6, 0.6, 0.6)))
        return cols or [("gray", (0.6, 0.6, 0.6))]

    meshes = []
    for mid, (label, _kids) in models.items():
        short = label.split("\x00")[0]
        if "LOD0" not in short:
            continue
        gid = geo_of.get(mid)
        if gid is None:
            continue
        _glabel, kids = geos[gid]
        verts = _prop(kids, "Vertices", [])
        idx = _prop(kids, "PolygonVertexIndex", [])
        if not verts or not idx:
            continue
        # Blender's FBX export writes centimeters. The park is meters.
        points = [(verts[i] * 0.01, verts[i + 1] * 0.01, verts[i + 2] * 0.01) for i in range(0, len(verts), 3)]
        _lp, layer_kids = _find(kids, "LayerElementMaterial")
        mat_index = _prop(layer_kids or [], "Materials", None)
        palette = diffuse(mid)
        m = xform_of(mid)
        world = [_apply(m, p) for p in points]
        poly = 0
        face = []
        tris = []
        for raw in idx:
            end = raw < 0
            vi = ~raw if end else raw
            face.append(world[vi])
            if end:
                col = palette[0][1]
                if mat_index is not None and poly < len(mat_index):
                    slot = mat_index[poly]
                    if 0 <= slot < len(palette):
                        col = palette[slot][1]
                for k in range(1, len(face) - 1):
                    tris.append((face[0], face[k], face[k + 1], col))
                face = []
                poly += 1
        meshes.append((short, tris))
    return meshes
