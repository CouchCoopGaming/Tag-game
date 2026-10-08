"""Turn manifest.json into URP materials, prefabs, metas, the showcase scene, and the doc.

Pure Python. Does not import bpy.

  python3 Tools/Blender/AssetLibrary/write_unity.py
"""

import ast
import hashlib
import json
import math
import os
import re

ROOT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(ROOT, "..", "..", ".."))
LIB = os.path.join(REPO, "Assets", "Art", "Props", "Library")
MANIFEST = os.path.join(ROOT, "manifest.json")
COMMON = os.path.join(ROOT, "_common.py")
RUNNER_MAT = os.path.join(REPO, "Assets", "Art", "Characters", "Mat_Runner_Base.mat")
BENCH_META = os.path.join(REPO, "Assets", "Art", "Props", "Playground", "Toy_Bench.fbx.meta")
BOOT = os.path.join(REPO, "Assets", "Scenes", "Boot.unity")

ORDER = ["Showcase", "StreetFurniture", "Roads", "Buildings", "Utility", "Park", "Harbor"]


def guid(*parts):
    return hashlib.md5(("tag-asset-library-v1|" + "|".join(parts)).encode("utf-8")).hexdigest()


def load_palette():
    text = open(COMMON, encoding="utf-8").read()
    block = re.search(r"PALETTE = (\{.*?\n\})", text, re.S).group(1)
    textured = re.search(r"TEXTURED = (\([^)]*\))", text, re.S).group(1)
    normals = re.search(r"NORMALS = (\([^)]*\))", text, re.S)
    normal_names = set(ast.literal_eval(normals.group(1))) if normals else set()
    ao = re.search(r"AO = (\([^)]*\))", text, re.S)
    ao_names = set(ast.literal_eval(ao.group(1))) if ao else set()
    emissive = re.search(r"EMISSIVE = (\{.*?\n\})", text, re.S)
    emissive_map = ast.literal_eval(emissive.group(1)) if emissive else {}
    return ast.literal_eval(block), set(ast.literal_eval(textured)), normal_names, ao_names, emissive_map


def q_from_matrix(m):
    t = m[0][0] + m[1][1] + m[2][2]
    if t > 0:
        s = math.sqrt(t + 1.0) * 2.0
        w = 0.25 * s
        x = (m[2][1] - m[1][2]) / s
        y = (m[0][2] - m[2][0]) / s
        z = (m[1][0] - m[0][1]) / s
    elif m[0][0] > m[1][1] and m[0][0] > m[2][2]:
        s = math.sqrt(1.0 + m[0][0] - m[1][1] - m[2][2]) * 2.0
        w = (m[2][1] - m[1][2]) / s
        x = 0.25 * s
        y = (m[0][1] + m[1][0]) / s
        z = (m[0][2] + m[2][0]) / s
    elif m[1][1] > m[2][2]:
        s = math.sqrt(1.0 + m[1][1] - m[0][0] - m[2][2]) * 2.0
        w = (m[0][2] - m[2][0]) / s
        x = (m[0][1] + m[1][0]) / s
        y = 0.25 * s
        z = (m[1][2] + m[2][1]) / s
    else:
        s = math.sqrt(1.0 + m[2][2] - m[0][0] - m[1][1]) * 2.0
        w = (m[1][0] - m[0][1]) / s
        x = (m[0][2] + m[2][0]) / s
        y = (m[1][2] + m[2][1]) / s
        z = 0.25 * s
    n = math.sqrt(x * x + y * y + z * z + w * w) or 1.0
    return (x / n, y / n, z / n, w / n)


def unity_euler_quat(euler_deg):
    x, y, z = [math.radians(v) * 0.5 for v in euler_deg]
    cx, sx = math.cos(x), math.sin(x)
    cy, sy = math.cos(y), math.sin(y)
    cz, sz = math.cos(z), math.sin(z)
    ax, ay, az, aw = sx * cz, sx * sz, cx * sz, cx * cz
    qx = cy * ax + sy * az
    qy = sy * aw + cy * ay
    qz = cy * az - sy * ax
    qw = cy * aw - sy * ay
    return (qx, qy, qz, qw)


def look_quat(eye, target):
    fx, fy, fz = target[0] - eye[0], target[1] - eye[1], target[2] - eye[2]
    fl = math.sqrt(fx * fx + fy * fy + fz * fz) or 1.0
    zx, zy, zz = fx / fl, fy / fl, fz / fl
    xx, xy, xz = zy * 0 - zz * 1, zz * 0 - zx * 0, zx * 1 - zy * 0
    # cross(up=(0,1,0), forward)
    xx = 1 * zz - 0 * zy
    xy = 0 * zx - 0 * zz
    xz = 0 * zy - 1 * zx
    xl = math.sqrt(xx * xx + xy * xy + xz * xz) or 1.0
    xx, xy, xz = xx / xl, xy / xl, xz / xl
    yx = zy * xz - zz * xy
    yy = zz * xx - zx * xz
    yz = zx * xy - zy * xx
    m = [
        [xx, yx, zx],
        [xy, yy, zy],
        [xz, yz, zz],
    ]
    return q_from_matrix(m)


def num(v):
    return ("%.5f" % float(v)).rstrip("0").rstrip(".") or "0"


def vec3(x, y, z):
    return "{x: %s, y: %s, z: %s}" % (num(x), num(y), num(z))


def yml_str(value):
    text = "" if value is None else str(value).replace("\n", " ")
    return '"' + text.replace("\\", "\\\\").replace('"', '\\"') + '"'


def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)


def folder_meta(path, key):
    write(path + ".meta", "fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % guid("folder", key))


def script_meta(path, key):
    write(path + ".meta", """fileFormatVersion: 2
guid: %s
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""" % guid("script", key))


def prefab_meta(path, key):
    write(path + ".meta", "fileFormatVersion: 2\nguid: %s\nPrefabImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % guid("prefab", key))


def scene_meta(path):
    write(path + ".meta", "fileFormatVersion: 2\nguid: %s\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % guid("scene", "AssetShowcase"))


def texture_meta(path, key, normal=False, linear=False, npot=False, size=256):
    text = """fileFormatVersion: 2
guid: %s
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: %d
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 0
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: %d
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""" % (guid("tex", key), size, size)
    if normal or linear:
        text = text.replace("sRGBTexture: 1", "sRGBTexture: 0", 1)
    if normal:
        text = text.replace("textureType: 0", "textureType: 1", 1)
    if npot:
        text = text.replace("nPOTScale: 1", "nPOTScale: 0", 1)
    write(path + ".meta", text)
    return guid("tex", key)


def write_materials(palette, textured, normals, ao_names, emissive):
    src = open(RUNNER_MAT, encoding="utf-8").read()
    # Drop the editor version sidecar; the material itself ends at the first document.
    src = src.split("--- !u!114")[0].rstrip() + "\n"
    out = {}
    for name, (color, metal, smooth) in palette.items():
        body = src.replace("m_Name: Mat_Runner_Base", "m_Name: " + name)
        col = "{r: %s, g: %s, b: %s, a: 1}" % (num(color[0]), num(color[1]), num(color[2]))
        body = re.sub(r"- _BaseColor: \{[^}]+\}", "- _BaseColor: " + col, body)
        body = re.sub(r"- _Color: \{[^}]+\}", "- _Color: " + col, body)
        body = re.sub(r"- _Metallic: [0-9.]+", "- _Metallic: %s" % num(metal), body)
        body = re.sub(r"- _Smoothness: [0-9.]+", "- _Smoothness: %s" % num(smooth), body)
        g = guid("mat", name)
        if name in textured:
            tex = "{fileID: 2800000, guid: %s, type: 3}" % guid("tex", name)
            body = body.replace("_BaseMap:\n        m_Texture: {fileID: 0}", "_BaseMap:\n        m_Texture: " + tex)
            body = body.replace("_MainTex:\n        m_Texture: {fileID: 0}", "_MainTex:\n        m_Texture: " + tex)
        keywords = []
        if name in normals:
            bump = "{fileID: 2800000, guid: %s, type: 3}" % guid("tex", name + "_N")
            body = body.replace("_BumpMap:\n        m_Texture: {fileID: 0}", "_BumpMap:\n        m_Texture: " + bump)
            keywords.append("_NORMALMAP")
            scale = "0.35" if name == "Lib_Water" else "0.6"
            body = body.replace("- _BumpScale: 1", "- _BumpScale: %s" % scale)
        if name in ao_names:
            occ = "{fileID: 2800000, guid: %s, type: 3}" % guid("tex", name + "_AO")
            body = body.replace("_OcclusionMap:\n        m_Texture: {fileID: 0}", "_OcclusionMap:\n        m_Texture: " + occ)
            keywords.append("_OCCLUSIONMAP")
        if name in emissive:
            emit, _strength = emissive[name]
            body = body.replace(
                "- _EmissionColor: {r: 0, g: 0, b: 0, a: 1}",
                "- _EmissionColor: {r: %s, g: %s, b: %s, a: 1}" % (num(emit[0]), num(emit[1]), num(emit[2])),
            )
            keywords.append("_EMISSION")
        if keywords:
            body = body.replace(
                "m_ValidKeywords: []",
                "m_ValidKeywords:\n" + "\n".join("  - " + key for key in keywords),
            )
        path = os.path.join(LIB, "Materials", name + ".mat")
        write(path, body if body.startswith("%YAML") else "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n" + body)
        write(path + ".meta", "fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % g)
        out[name] = g
    return out


def write_fbx_meta(entry):
    rel = entry["fbx"]
    path = os.path.join(REPO, rel)
    text = open(BENCH_META, encoding="utf-8").read()
    text = re.sub(r"^guid: [0-9a-f]+", "guid: " + guid("fbx", entry["category"], entry["name"]), text, count=1, flags=re.M)
    text = text.replace("materialImportMode: 1", "materialImportMode: 0")
    write(path + ".meta", text)
    return guid("fbx", entry["category"], entry["name"])


RENDERER = """--- !u!23 &{fid}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
{mats}  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
"""


def transform_block(fid, go, father, children, pos=(0, 0, 0), rot=(0, 0, 0, 1)):
    kids = "".join("  - {fileID: %d}\n" % c for c in children)
    return """--- !u!4 &%d
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: %d}
  serializedVersion: 2
  m_LocalRotation: {x: %s, y: %s, z: %s, w: %s}
  m_LocalPosition: %s
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
%s  m_Father: {fileID: %d}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
""" % (fid, go, num(rot[0]), num(rot[1]), num(rot[2]), num(rot[3]), vec3(*pos), kids, father)


def go_block(fid, name, components):
    comps = "".join("  - component: {fileID: %d}\n" % c for c in components)
    return """--- !u!1 &%d
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
%s  m_Layer: 0
  m_Name: %s
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
""" % (fid, comps, name)


def collider_block(col, go, fid):
    common = """  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: %d}
  m_Material: {fileID: 0}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
""" % go
    if col["type"] == "box":
        return "--- !u!65 &%d\nBoxCollider:\n%s  serializedVersion: 3\n  m_Size: %s\n  m_Center: {x: 0, y: 0, z: 0}\n" % (
            fid, common, vec3(*col["size"]))
    if col["type"] == "sphere":
        return "--- !u!135 &%d\nSphereCollider:\n%s  m_Radius: %s\n  m_Center: {x: 0, y: 0, z: 0}\n" % (
            fid, common, num(col["radius"]))
    return "--- !u!136 &%d\nCapsuleCollider:\n%s  m_Radius: %s\n  m_Height: %s\n  m_Direction: %d\n  m_Center: {x: 0, y: 0, z: 0}\n" % (
        fid, common, num(col["radius"]), num(col["height"]), int(col["direction"]))


def write_prefab(entry, fbx_guid, mat_guids, script_guid):
    parts = []
    lods = entry["lods"]
    child_transforms = []
    lod_renderers = []
    for lod in lods:
        idx = lod["lod"]
        go = 200000 + idx * 10
        tr = go + 1
        mf = go + 2
        mr = go + 3
        child_transforms.append(tr)
        lod_renderers.append(mr)
        mats = "".join("  - {fileID: 2100000, guid: %s, type: 2}\n" % mat_guids[name] for name in lod["materials"])
        parts.append(go_block(go, "LOD%d" % idx, [tr, mf, mr]))
        parts.append(transform_block(tr, go, 100001, []))
        parts.append("""--- !u!33 &%d
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: %d}
  m_Mesh: {fileID: %d, guid: %s, type: 3}
""" % (mf, go, lod["fileID"], fbx_guid))
        parts.append(RENDERER.format(fid=mr, go=go, mats=mats))

    for i, col in enumerate(entry["colliders"]):
        go = 300000 + i * 10
        tr = go + 1
        cf = go + 2
        child_transforms.append(tr)
        euler = col.get("euler") or (0, 0, 0)
        rot = unity_euler_quat(euler) if any(abs(v) > 1e-6 for v in euler) else (0, 0, 0, 1)
        parts.append(go_block(go, col["name"], [tr, cf]))
        parts.append(transform_block(tr, go, 100001, [], pos=tuple(col["center"]), rot=rot))
        parts.append(collider_block(col, go, cf))

    size = entry["size"]
    center = [(entry["min"][i] + entry["max"][i]) * 0.5 for i in range(3)]
    extent = max(size[0], size[1], size[2], 0.5)
    heights = [0.55, 0.2, 0.07][:len(lods)]
    lod_yaml = ""
    for height, renderer in zip(heights, lod_renderers):
        lod_yaml += "  - screenRelativeHeight: %s\n    fadeTransitionWidth: 0\n    renderers:\n    - renderer: {fileID: %d}\n" % (num(height), renderer)

    root = []
    root.append(go_block(100000, entry["name"], [100001, 100002, 100004]))
    root.append(transform_block(100001, 100000, 0, child_transforms))
    root.append("""--- !u!205 &100002
LODGroup:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000}
  serializedVersion: 2
  m_LocalReferencePoint: %s
  m_Size: %s
  m_FadeMode: 0
  m_AnimateCrossFading: 0
  m_LastLODIsBillboard: 0
  m_LODs:
%s  m_Enabled: 1
""" % (vec3(*center), num(extent), lod_yaml))
    root.append("""--- !u!114 &100004
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: %s, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::Tag.Art.Library.LibraryPropMeta
  category: %s
  summary: %s
  climbable: %d
  vaultable: %d
  vaultHeightMeters: %s
  climbNote: %s
  vaultNote: %s
""" % (
        script_guid,
        yml_str(entry["category"]),
        yml_str(entry["blurb"]),
        1 if entry["climbable"] else 0,
        1 if entry["vaultable"] else 0,
        num(entry["vaultHeight"]),
        yml_str(entry["climbNote"]),
        yml_str(entry["vaultNote"]),
    ))
    text = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n" + "".join(root + parts)
    path = os.path.join(LIB, entry["category"], "Prefabs", entry["name"] + ".prefab")
    write(path, text)
    prefab_meta(path, entry["category"] + "/" + entry["name"])
    return guid("prefab", entry["category"] + "/" + entry["name"])


def collider_summary(cols):
    grouped = []
    seen = {}
    for col in cols:
        base = re.sub(r"_\d+$", "", col["name"])
        seen.setdefault(base, 0)
        seen[base] += 1
    for base, count in seen.items():
        grouped.append("%s x%d" % (base, count) if count > 1 else base)
    text = ", ".join(grouped)
    return text if len(text) < 180 else text[:177] + "..."


def write_doc(entries):
    lines = []
    lines.append("# Asset library, pass 29")
    lines.append("")
    lines.append("Procedural props for the couch tag arenas. Real meters, +Y up, pivot at the ground contact (or the module origin called out in the notes). Players are about 1.8 m. Vault rails in the park kit sit at 0.90–1.05 m. Every mesh is rebuilt from `Tools/Blender/AssetLibrary/<asset>.py`.")
    lines.append("")
    lines.append("No third-party textures. Brick, concrete, wood, bark, asphalt, and the worn metals are Blender node trees baked to albedo, roughness, normal, and occlusion, one meter per tile. Wood is straight plank seams about every 14 cm, with fine grain along the plank. Siding, roof, soil, and hydrant paint stay on the small procedural tiles. The court paint is one decal, not rescaled by the importer. Window glass and street-light lenses emit. Some shop windows use a warmer night glass.")
    lines.append("")
    lines.append("## Rebuild")
    lines.append("")
    lines.append("```")
    lines.append("blender --background --python Tools/Blender/AssetLibrary/build_all.py")
    lines.append("python3 Tools/Blender/AssetLibrary/write_unity.py")
    lines.append("blender --background --python Tools/Blender/AssetLibrary/render_pass29.py")
    lines.append("```")
    lines.append("")
    lines.append("Blender 4.2 LTS is enough. `write_unity.py` does not need Blender. The showcase scene is `Assets/Scenes/AssetShowcase.unity`. It is not in the build settings and it does not touch the three arenas. `Tag/Asset Showcase` rebuilds that scene from the prefabs.")
    lines.append("")
    lines.append("## Colliders")
    lines.append("")
    lines.append("`Col_*` matches one solid piece. `Climb_*` is the cling face and is the wall, not a shell around it. `Vault_*` is the rail; `LibraryPropMeta.vaultHeightMeters` is the height of that rail above the deck you take off from. Chain-link `Col_Fabric` is a slab the thickness of the wire: the diamonds are not a passage. Storm-drain slots and crate slat gaps are the same idea, a walkable or solid skin with openings too small to move through. Road paint and the asphalt patch have no collider. Utility-pole wires have no collider.")
    lines.append("")
    lines.append("## Assets")
    lines.append("")
    lines.append("| Name | Category | Dimensions (m) | Tris LOD0 | Colliders | Climb / vault | Status |")
    lines.append("| --- | --- | --- | --- | --- | --- | --- |")
    for entry in entries:
        sx, sy, sz = entry["size"]
        dims = "%s × %s × %s" % (num(sx), num(sy), num(sz))
        tris = str(entry["lods"][0]["tris"])
        if len(entry["lods"]) > 1:
            tris += " (" + "/".join(str(item["tris"]) for item in entry["lods"]) + ")"
        notes = []
        if entry["climbable"]:
            notes.append("climb")
        if entry["vaultable"]:
            notes.append("vault %.2f m" % entry["vaultHeight"])
        if entry["climbNote"]:
            notes.append(entry["climbNote"])
        if entry["vaultNote"]:
            notes.append(entry["vaultNote"])
        if entry["warnings"]:
            notes.append("Check: " + "; ".join(entry["warnings"]))
        status = "shipped" if not entry["warnings"] else "shipped, review"
        climb = " ".join(notes).replace("|", "/")
        lines.append("| %s | %s | %s | %s | %s | %s | %s |" % (
            entry["name"], entry["category"], dims, tris, collider_summary(entry["colliders"]).replace("|", "/"), climb, status))
    lines.append("")
    lines.append("## Modules")
    lines.append("")
    lines.append("- Roads are 6 m wide and 4 m long. Top of asphalt is 0.12 m. Each straight tile is one continuous asphalt surface: no lane seam and no crack-seal ticks. One white edge line per side runs the full 4 m, with a double yellow of two 10 cm lines and a 10 cm gap. Sidewalk top is 0.27 m (15 cm curb) and the curb faces -X. `Gutter` is one concrete pan, 0.38 m wide and a full 4 m long, top at 0.09 m. Its +X face meets that curb and its -X face sits on the road edge. On the shop street the road edge is x = 3, the gutter center is x = 3.19, the walk center is x = 4.38, and the shop footing is x = 5.38. `Road_Junction` is a flush plus of asphalt, top at 0.12 m, with arms out to 11 m. The box is empty. Each corner is one sidewalk with a 5 m curb return into straight walks along both arms. The zebra runs curb to curb on the straight run. A white stop bar sits just before it, on the inbound lanes, and the double yellow ends at that bar. Each ramp is about 1.70 m long and carries a detectable-warning panel at the toe. A `Road_Straight` tile butts each arm and carries the same double yellow, a flush concrete gutter, and sidewalks on both sides. The gutter meets the asphalt flush. Ramps, sidewalks, and the full lane width, including the outer edge, have colliders.")
    lines.append("- Brick bays are 4.0 m wide, 3.2 m tall, 0.30 m thick, exterior +Z. Stack a second row at y = 3.2 for two stories. `Store_Corner`, `Store_Diner`, and `Store_Laundromat` are closed volumes. The ground floor is a shopfront: the kickplate and the display glass sit about 14 cm behind the brick face, mullions fill that reveal, a recessed door has a transom, and a tall sign band sits on a dark plate. The cornice is a bed, a concrete corona that projects about 26 cm, and a smaller cap, and it mitres around the corners instead of stopping short. A sign-band moulding does the same. Letters are about 0.62 m. The awning is sloped fabric with a valance, and each steel frame ends at the pier on its own bracket. Corner stones alternate header and stretcher so the brick wraps. Signs read MARKET, DINER, and WASH. `House_Gable` and `House_Hip` have a porch, steps, a paneled door, trimmed windows, eave gutters, and a chimney. `Ranch_House` is a single-story hip-roof house, 10.8 × 7.2 m, plate at 2.50 m, ridge about 3.70 m. The attached garage is the +X 3.6 m, with a closed overhead door whose panels fill the opening. The porch on the living wing has a top rail, a bottom rail, and balusters about 10 cm apart, tied into the posts, plus steps and a concrete walk to the driveway. All four hip slopes are landings. The shingles are a short staggered course on Lib_RanchRoof. A brick chimney stands on the living-wing ridge. `WalkUp` is a three-storey brick block, 8.0 × 6.4 m, brick top at 9.30 m. Brick is 13 courses per metre. Windows are recessed about 13 cm with brick returns, a stone sill, and a lintel. A thin concrete coping sits on that parapet. A concrete corona projects past each wall under the coping and mitres at the corners. The entry has a stoop and a canopy. The fire escape on +Z hangs one landing per upper floor, each spanning two windows, with a stair along the wall between them (a stringer on both sides, treads with depth), rails on the street edge, brackets into the brick under each landing, and a drop ladder. `GasCanopy` is a fuel island: pad, four columns, a fascia around the roof, two pumps with bollards, a MART shop behind the canopy, a price sign, and an ICE machine. `PicketFence` is a 2 m bay of pointed pickets about 7 cm wide with a similar gap. `WoodFence` is an 8 ft privacy bay of 6 ft dog-ear pickets with one post at the origin, so the next bay brings the far post. Three rails sit on the back. `WoodFence_Gate` is a closed leaf between two posts, with a back brace, a hinge, and a latch. `WoodFence_Corner` turns from +X to +Z with the rails on the inside. `WoodFence_End` is a post and a short run. Chain-link stays `ChainFence` and `ChainGate`. `Pond` is a sunken basin: grass and dirt banks, a stone lip, reeds, and a water surface below the surrounding grade. `PondEdge` stays the 4 m straight coping.")
    lines.append("- Dock modules share a deck at 0.62 m. `Dock_Straight` is 6 × 3 m. Piles are 30 cm rounds on a 2.4 m grid along both edges and at mid-span, from about 1.22 m below the pivot up to the stringer. The shaft is darker under the waterline and dry timber above it, with a thin green line at the water. There is no side skirt and no fascia. From the side, top to bottom: flat deck planks (a few sit up to 1.5 mm lower, none crown up) with about 5 cm gaps, one 5 × 20 cm stringer, flat steel straps bolted at each pile above the water, then the piles, then water. A ladder of dark galvanised 4 cm round rails and round rungs on -X is bolted to the stringer, curves over the deck edge as grab handles, and runs down into the water. Cleats, two bollards, and a rope that lies on the deck. `Dock_Corner` is an L inside a 4 m square; its pivot is the center of that square.")
    lines.append("- Containers are external ISO sizes: 20 ft is 6.06 × 2.44 × 2.59 m, 40 ft is 12.19 × 2.44 × 2.59 m. Doors face +Z. The shell is untextured enamel with a rolled trapezoid corrugation (one closed sheet per face, not raised battens). Corner castings are steel boxes with an oval hole on each exposed face. Four locking bars stay on the door end. Container_20 is rust red, Container_20_Blue and Container_40 are blue, Container_20_Green is green.")
    lines.append("- The court is a 22 × 15 m street full court. Paint is one even acrylic decal measured from the rims at z = ±9.425: a 4.90 m key, a free-throw circle solid on the outer half and dashed inside the lane, a restricted arc, and a 6.75 m three-point arc centred on the rim. The corner line is 0.90 m in from the sideline. Lines are 5 cm. Wear is only a slight darkening in the keys and at center court. `CourtFence` shares that pivot, with posts 0.45 m outside the slab (baselines 3.05 m, sidelines 1.80 m, gate on +X). `Hoop` rim is 3.05 m and 0.81 m in front of the pole. Place the pole so the backboard face is 1.20 m inside the baseline: south z = -10.235 (yaw 0), north z = 10.235 (yaw 180). The rim then sits on the painted basket. The face-to-rim gap is the real 0.375 m.")
    lines.append("- `DockRamp` is 4 × 2 m and falls from 0.62 m at -Z to 0.05 m at +Z. A dock centered at the origin meets a ramp centered at z = 4.")
    lines.append("- `LaneArrow` and `StopBar` are paint. Place them on a road top (y = 0.12). They have no collider. `RaisedCrosswalk` replaces a 6 × 4 m road tile; the crown is 8 cm above the road and the collider follows that hump.")
    lines.append("- `HarborWater` is a dark rippled sheet with no collider. `Quay_Edge` is 18 m long with an 8.2 m apron, deck at 0.90 m. The water face is a vertical concrete wall from about 1.2 m below the pivot up to the deck, with a foundation under the apron. Edge stone, four bollards, a ladder, and five fenders are on -Z. `Container_20` stays the ISO 20 ft box (6.06 × 2.44 × 2.59 m) and sits on that apron. `HarborCrane` is a steel jib crane: tubular mast, cab, level jib toward +Z, counterweight on -Z. `Boat` is a work boat about 6.4 m long with a cabin, bow to -Z, gunwale near 1.0 m. `FishingBoat` is about 5.1 m, bow to -Z. The hull is a hard-chine V with a flared bow, plus a wheelhouse, rails, cleats, and an outboard. The red stripe is the 0.34 m waterline. `Rowboat` is about 2.9 m, bow to -Z. The section is 21 segments around, with overlapping lapstrake planks, a beveled transom, a gunwale, two thwarts, and oars seated in the oarlocks. `Boathouse` is a wood building on piles, 6.6 × 8.4 m, eave 2.90 m, ridge about 4.25 m. The +Z gable is an open slip 3.20 m clear so a 3 m dock can enter. Side walks use the dock board: 20 cm planks, 5 cm gaps, top at 0.62 m. A steel lift beam, cables, and two bunks sit in the slip. The gable uses the ranch shingle tile. `HarborShed` is a wood shed. Board-and-batten siding, corner trim seated on the boards, a butt-jointed frame-and-panel door with a beveled recess, a trimmed window on +Z, and a window on +X. The gable still sits on a top plate with a closed eave. `Buoy` is a can buoy: ballast bulb, red body, white band, and a cage with a light. `Gangway` is an aluminum plate from 0.90 m at -Z to 0.72 m at +Z. Rail posts are welded into the side stringers, cleats sit on the plate, and the wheels at +Z rest at 0.62 m. `HarborWarehouse` is a brick shed, 10 × 6.2 m, with two roll-up doors on +Z and a corrugated roof. `QuayDavit` is a short post and a jib toward +Z; set it on the quay deck. `MooringLine` is a sagged rope along Z for a cleat-to-boat line. `FuelDock` is a hose pump. `LifeRing` faces +Z. `FishCrate` and `LobsterTrap` sit on a deck. `Piling` continues about 1.4 m below its pivot. `Mooring` is a 3.2 m finger at 0.55 m.")
    lines.append("- `Pavilion` is the square park shelter: 4.6 m across, rail 0.95 m above the deck, pyramid roof. `Gazebo` is a hexagon: six posts on the deck corners, roof corners on those same rays with a short overhang. Every side except the entry has a top rail, a bottom rail, and even balusters. The deck is 0.32 m thick with vertical skirt boards, and one side is a step. Top rail center is 0.95 m above the deck. `Playground` is an A-frame swing with two chains on each red belt seat. The seat has a cream rim. The slide has rails and a curled lip, square posts down to the mulch, guard panels, and stairs in the same blue as the deck. The spring rider seat is about 0.6 m and the climbing dome is a geodesic of bars, on a rubber border with a wood-chip patch. `Seesaw` is a separate park plank. `Tree_Grate` is a street tree standing in a square steel grate.")
    lines.append("- `Mannequin` is a 1.80 m scale figure for the showcase. It is not a gameplay character. Harbor and street stills use the Hier mannequin at the same height.")
    lines.append("- `RecyclingBin` is a blue street bin with a white rim and a green lid, about 0.95 m tall. `TrashCan_Slat` stays the slatted can. `TrashCan_Lidded` is a dark green powder-coated can: slats about 2 cm apart, hoops, a black liner, a lip, a shallow lid, and a dark steel flap on a hinge. `Bench_Wood` is a 1.80 m board bench. Each end is the same dark cast profile, with a wood arm seated on the iron. Seat and back slats overhang the frames by about 6 cm, and the cross rails enter the casting through a small boss. Seat height is 0.45 m. `PicnicTable` is a board top on two tapered A-frame trestles with a cross brace, benches tied into the legs. `ParkLamp` is a post on a 36 cm tapered cast base and a four-pane glass lantern. `LightPost_Single` is a cobra head on a tapered pole with a 36 cm cast base. `Median_Planter` is a 4 m concrete median with coping on all four sides and a clipped box hedge. `Brick_Wall` is a 4 m brick bay. The door and window sit in brick reveals, with a concrete lintel and a stone sill. A concrete corona projects about 23 cm on the street face under the cap. The door is five vertical planks in a frame. `Climb_Wall` is still the solid 0.30 m wall, so the recess does not move the cling surface. `Cabin` is a log cabin. Side logs sit half a course above the front logs so the corners read as saddle notches, with end grain on the tails. Each course is a slightly different tan, with grey-tan chinking and a light checking normal. The door is vertical boards about 13 cm wide, with an iron Z-brace, strap hinges, and a latch. Each side wall has one framed window with a sill, and the gables are board-and-batten. The porch shed sits on a ledger outside the logs, under the eave, on a beam over the posts. The porch has a top rail and one mid rail between the posts. Log ends carry two growth rings. The roof is cedar shakes about 45–60 cm long and 8–18 cm wide, with about 25 cm of exposure, vertical grain, and a thick tapered butt that throws a shadow on each course. The ridge and the courses run continuously up to the flashing on both sides of the chimney. The brick chimney comes up through the roof at the ridge: a concrete footing, a brick shaft, a stepped worn-metal flashing ring around the penetration, a concrete cap, and a flue opening. The footing and the shaft each have their own collider. The window glass does not emit.")
    lines.append("")
    lines.append("## TODO")
    lines.append("")
    lines.append("- Interior dressing once a shell is placed in an arena.")
    lines.append("- Lighthouse fresnel, animated water, and night light cookies if a harbor becomes a landmark.")
    lines.append("- A second dock length is two `Dock_Straight` modules. The quay is the working edge; the dock sits in the water in front of it.")
    lines.append("")
    write(os.path.join(REPO, "Docs", "AssetLibrary.md"), "\n".join(lines) + "\n")


def boot_header():
    text = open(BOOT, encoding="utf-8").read()
    marker = "--- !u!1 &100000"
    return text.split(marker)[0]


def write_scene(entries, prefab_guids, mat_guids):
    buckets = {}
    for entry in entries:
        buckets.setdefault(entry["category"], []).append(entry)
    row_z = 0.0
    placed = []
    labels = []
    for cat in ORDER:
        items = sorted(buckets.get(cat, []), key=lambda item: item["name"])
        if not items:
            continue
        cursor = 0.0
        depth = 2.0
        for entry in items:
            px = cursor - entry["min"][0]
            placed.append((entry, px, row_z))
            cursor += entry["size"][0] + 1.8
            depth = max(depth, entry["size"][2])
        labels.append((cat, row_z))
        row_z -= depth + 6.0

    xs, zs = [], []
    for entry, px, pz in placed:
        xs.extend((px + entry["min"][0], px + entry["max"][0]))
        zs.extend((pz + entry["min"][2], pz + entry["max"][2]))
    min_x, max_x = min(xs) - 8, max(xs) + 6
    min_z, max_z = min(zs) - 6, max(zs) + 8
    ground_center = ((min_x + max_x) * 0.5, -0.05, (min_z + max_z) * 0.5)
    ground_scale = (max_x - min_x, 0.1, max_z - min_z)
    target = ((min_x + max_x) * 0.5, 1.5, (min_z + max_z) * 0.5)
    eye = (target[0] + (max_x - min_x) * 0.15, max(10.0, (max_z - min_z) * 0.45), max_z + (max_z - min_z) * 0.15)
    rot = look_quat(eye, target)

    chunks = [boot_header()]
    roots = []

    def add_go(name, comps_extra, pos, rot_q=(0, 0, 0, 1), scale=(1, 1, 1)):
        go = next_id[0]
        next_id[0] += 1
        tr = next_id[0]
        next_id[0] += 1
        extra_ids = []
        blocks = []
        for _ in comps_extra:
            extra_ids.append(next_id[0])
            next_id[0] += 1
        blocks.append(go_block(go, name, [tr] + extra_ids))
        kids = ""
        blocks.append("""--- !u!4 &%d
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: %d}
  serializedVersion: 2
  m_LocalRotation: {x: %s, y: %s, z: %s, w: %s}
  m_LocalPosition: %s
  m_LocalScale: %s
  m_ConstrainProportionsScale: 0
  m_Children:
%s  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
""" % (tr, go, num(rot_q[0]), num(rot_q[1]), num(rot_q[2]), num(rot_q[3]), vec3(*pos), vec3(*scale), kids))
        for fid, block in zip(extra_ids, comps_extra):
            blocks.append(block.format(fid=fid, go=go))
        chunks.extend(blocks)
        roots.append(tr)
        return go

    next_id = [100000]
    # Camera
    add_go("Main Camera", [
        """--- !u!20 &{fid}
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 1
  m_BackGroundColor: {{r: 0.62, g: 0.72, b: 0.82, a: 0}}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_Iso: 200
  m_ShutterSpeed: 0.005
  m_Aperture: 16
  m_FocusDistance: 10
  m_FocalLength: 50
  m_BladeCount: 5
  m_Curvature: {{x: 2, y: 11}}
  m_BarrelClipping: 0.25
  m_Anamorphism: 0
  m_SensorSize: {{x: 36, y: 24}}
  m_LensShift: {{x: 0, y: 0}}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 500
  field of view: 40
  orthographic: 0
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {{fileID: 0}}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 0
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
""",
        """--- !u!81 &{fid}
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
"""
    ], eye, rot, (1, 1, 1))

    add_go("Directional Light", [
        """--- !u!108 &{fid}
Light:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  serializedVersion: 10
  m_Type: 1
  m_Shape: 0
  m_Color: {{r: 1, g: 0.96, b: 0.9, a: 1}}
  m_Intensity: 1.1
  m_Range: 10
  m_SpotAngle: 30
  m_InnerSpotAngle: 21.8
  m_CookieSize: 10
  m_Shadows:
    m_Type: 2
    m_Resolution: -1
    m_CustomResolution: -1
    m_Strength: 1
    m_Bias: 0.05
    m_NormalBias: 0.4
    m_NearPlane: 0.2
    m_CullingMatrixOverride:
      e00: 1
      e01: 0
      e02: 0
      e03: 0
      e10: 0
      e11: 1
      e12: 0
      e13: 0
      e20: 0
      e21: 0
      e22: 1
      e23: 0
      e30: 0
      e31: 0
      e32: 0
      e33: 1
    m_UseCullingMatrixOverride: 0
  m_Cookie: {{fileID: 0}}
  m_DrawHalo: 0
  m_Flare: {{fileID: 0}}
  m_RenderMode: 0
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingLayerMask: 1
  m_Lightmapping: 4
  m_LightShadowCasterMode: 0
  m_AreaSize: {{x: 1, y: 1}}
  m_BounceIntensity: 1
  m_ColorTemperature: 6570
  m_UseColorTemperature: 0
  m_BoundingSphereOverride: {{x: 0, y: 0, z: 0, w: 0}}
  m_UseBoundingSphereOverride: 0
  m_UseViewFrustumForShadowCasterCull: 1
  m_ShadowRadius: 0
  m_ShadowAngle: 0
"""
    ], (0, 12, 0), (0.40821788, -0.23456968, 0.10938163, 0.8754261))

    concrete = mat_guids["Lib_Concrete"]
    add_go("Ground", [
        """--- !u!33 &{fid}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Mesh: {{fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}}
""",
        """--- !u!23 &{fid}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: %s, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
""" % concrete,
        """--- !u!65 &{fid}
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {{x: 1, y: 1, z: 1}}
  m_Center: {{x: 0, y: 0, z: 0}}
"""
    ], ground_center, (0, 0, 0, 1), ground_scale)

    for cat, z in labels:
        go = next_id[0]
        next_id[0] += 1
        tr = next_id[0]
        next_id[0] += 1
        tm = next_id[0]
        next_id[0] += 1
        mr = next_id[0]
        next_id[0] += 1
        chunks.append(go_block(go, "Label_" + cat, [tr, tm, mr]))
        chunks.append(transform_block(tr, go, 0, [], pos=(-2.4, 0.2, z)))
        chunks.append("""--- !u!102 &%d
TextMesh:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: %d}
  serializedVersion: 3
  m_Text: %s
  m_OffsetZ: 0
  m_CharacterSize: 0.16
  m_LineSpacing: 1
  m_Anchor: 5
  m_Alignment: 2
  m_TabSize: 4
  m_FontSize: 64
  m_FontStyle: 0
  m_RichText: 1
  m_Color: {r: 0.12, g: 0.13, b: 0.16, a: 1}
  m_Font: {fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}
""" % (tm, go, cat))
        chunks.append("""--- !u!23 &%d
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: %d}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 0
  m_ReflectionProbeUsage: 0
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 10108, guid: 0000000000000000e000000000000000, type: 0}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {fileID: 0}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_AdditionalVertexStreams: {fileID: 0}
""" % (mr, go))
        roots.append(tr)

    for entry, px, pz in placed:
        pid = next_id[0]
        next_id[0] += 1
        tr = next_id[0]
        next_id[0] += 1
        pguid = prefab_guids[entry["name"]]
        mods = []
        for prop, value in (
            ("m_LocalPosition.x", px),
            ("m_LocalPosition.y", 0),
            ("m_LocalPosition.z", pz),
            ("m_LocalRotation.x", 0),
            ("m_LocalRotation.y", 0),
            ("m_LocalRotation.z", 0),
            ("m_LocalRotation.w", 1),
            ("m_LocalEulerAnglesHint.x", 0),
            ("m_LocalEulerAnglesHint.y", 0),
            ("m_LocalEulerAnglesHint.z", 0),
        ):
            mods.append("    - target: {fileID: 100001, guid: %s, type: 3}\n      propertyPath: %s\n      value: %s\n      objectReference: {fileID: 0}\n" % (pguid, prop, num(value)))
        mods.append("    - target: {fileID: 100000, guid: %s, type: 3}\n      propertyPath: m_Name\n      value: %s\n      objectReference: {fileID: 0}\n" % (pguid, entry["name"]))
        chunks.append("""--- !u!1001 &%d
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {fileID: 0}
    m_Modifications:
%s    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {fileID: 100100000, guid: %s, type: 3}
--- !u!4 &%d stripped
Transform:
  m_CorrespondingSourceObject: {fileID: 100001, guid: %s, type: 3}
  m_PrefabInstance: {fileID: %d}
  m_PrefabAsset: {fileID: 0}
""" % (pid, "".join(mods), pguid, tr, pguid, pid))
        roots.append(tr)

    chunks.append("""--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
%s""" % "".join("  - {fileID: %d}\n" % r for r in roots))
    path = os.path.join(REPO, "Assets", "Scenes", "AssetShowcase.unity")
    write(path, "".join(chunks))
    scene_meta(path)


def ensure_folders():
    rels = ["", "Textures", "Materials", "Scripts"]
    cats = set()
    for entry in json.load(open(MANIFEST, encoding="utf-8")):
        cats.add(entry["category"])
    for rel in rels:
        folder_meta(os.path.join(LIB, rel) if rel else LIB, "Library/" + rel)
    for cat in cats:
        folder_meta(os.path.join(LIB, cat), "Library/" + cat)
        folder_meta(os.path.join(LIB, cat, "Prefabs"), "Library/" + cat + "/Prefabs")


def main():
    palette, textured, normals, ao_names, emissive = load_palette()
    entries = json.load(open(MANIFEST, encoding="utf-8"))
    ensure_folders()
    tex_dir = os.path.join(LIB, "Textures")
    if os.path.isdir(tex_dir):
        for fn in sorted(os.listdir(tex_dir)):
            if not fn.endswith(".png"):
                continue
            png = os.path.join(tex_dir, fn)
            key = fn[:-4]
            normal = key.endswith("_N")
            linear = normal or key.endswith("_R") or key.endswith("_AO")
            npot = key == "Lib_CourtDecal"
            size = 2048 if key == "Lib_CourtDecal" else 512 if key.startswith("Lib_") and not key.endswith(("_R", "_N", "_AO")) else 512
            # Court paint is 1500 x 2200 and must not be rescaled onto a power of two.
            if key == "Lib_CourtDecal":
                size = 2048
            texture_meta(png, key, normal=normal, linear=linear, npot=npot, size=size)
    mat_guids = write_materials(palette, textured, normals, ao_names, emissive)
    script_guid = guid("script", "LibraryPropMeta")
    script_meta(os.path.join(LIB, "Scripts", "LibraryPropMeta.cs"), "LibraryPropMeta")
    script_meta(os.path.join(REPO, "Assets", "Editor", "AssetLibraryShowcase.cs"), "AssetLibraryShowcase")
    prefab_guids = {}
    for entry in entries:
        fbx_guid = write_fbx_meta(entry)
        prefab_guids[entry["name"]] = write_prefab(entry, fbx_guid, mat_guids, script_guid)
    write_scene(entries, prefab_guids, mat_guids)
    write_doc(entries)
    print("unity assets", len(entries), "materials", len(mat_guids))


if __name__ == "__main__":
    main()
