using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Marks the dust a collider kicks up. Put this on the ground or the prop.
    /// Kind 0 grass, 1 dirt/sand, 2 concrete/asphalt, 3 wood, 4 metal, 5 wet.
    /// If the tag is missing, the physics material name is used, then the
    /// renderer material name, then the object name. Grass, mulch, sand,
    /// concrete, wood, and metal on the parks are stamped when the arena builds.
    /// Name a material "wet", or set Kind to 5, for splash instead of dust.
    /// The stamp does not change slide friction. The motor still owns the slide.
    /// </summary>
    public sealed class SurfaceTag : MonoBehaviour
    {
        public int Kind = DustLook.TagNone;

        public static void Apply(GameObject go, string token, string objectName)
        {
            if (go == null) return;
            DustLook.Surface surface = DustLook.Classify(token, objectName, DustLook.TagNone);
            SurfaceTag tag = go.GetComponent<SurfaceTag>();
            if (tag == null) tag = go.AddComponent<SurfaceTag>();
            tag.Kind = (int)surface;
            Collider col = go.GetComponent<Collider>();
            if (col == null) return;
            col.sharedMaterial = Shared(surface);
        }

        static PhysicsMaterial[] _shared;

        static PhysicsMaterial Shared(DustLook.Surface surface)
        {
            int i = (int)surface;
            if (i < 0 || i >= DustLook.SurfaceCount) i = (int)DustLook.Surface.Concrete;
            if (_shared == null) _shared = new PhysicsMaterial[DustLook.SurfaceCount];
            if (_shared[i] != null) return _shared[i];
            var mat = new PhysicsMaterial(DustLook.PhysicsName(i));
            mat.dynamicFriction = 0.6f;
            mat.staticFriction = 0.6f;
            mat.bounciness = 0f;
            mat.frictionCombine = PhysicsMaterialCombine.Average;
            mat.bounceCombine = PhysicsMaterialCombine.Average;
            _shared[i] = mat;
            return mat;
        }
    }
}
