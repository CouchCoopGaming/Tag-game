"""Shift a finished asset along Unity Z so the ground footprint is centered."""


def shift_z(asset, dz):
    if abs(dz) < 1e-9:
        return
    for geo in asset.lods.values():
        for vert in geo.bm.verts:
            # Blender Y is the negated Unity Z.
            vert.co.y -= dz
    for col in asset.colliders:
        center = col["center"]
        center[2] = center[2] + dz
