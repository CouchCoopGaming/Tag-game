using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Reads a collider once per contact, then reuses that surface until the collider changes.
    /// </summary>
    public static class DustContact
    {
        public static int Read(Collider col, ref int cachedId, ref int cached)
        {
            int id = col != null ? col.GetInstanceID() : 0;
            if (id != 0 && id == cachedId) return cached;
            cachedId = id;
            cached = (int)Resolve(col);
            return cached;
        }

        public static DustLook.Surface Resolve(Collider col)
        {
            if (col == null) return DustLook.Surface.Concrete;
            SurfaceTag tag = col.GetComponent<SurfaceTag>();
            if (tag != null && tag.Kind == (int)DustLook.Surface.Brick)
                return DustLook.Surface.Brick;
            if (tag != null && tag.Kind >= 0 && tag.Kind < DustLook.SurfaceCount)
                return DustLook.FromTag(tag.Kind);
            PhysicsMaterial phys = col.sharedMaterial;
            if (phys != null && DustLook.Named(phys.name))
                return DustLook.Classify(phys.name, col.name, DustLook.TagNone);
            string rend = "";
            MeshRenderer renderer = col.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
                rend = renderer.sharedMaterial.name;
            string mat = rend.Length > 0 ? rend : col.name;
            return DustLook.Classify(mat, col.name, DustLook.TagNone);
        }
    }
}
