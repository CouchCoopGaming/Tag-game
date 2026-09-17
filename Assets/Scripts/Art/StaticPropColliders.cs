using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Runtime helper: ensure static HiPoly / PGK / landmark visuals have usable colliders.
    /// Prefer non-convex MeshCollider when mesh.isReadable; BoxCollider(bounds) fallback
    /// for non-readable, huge, or unusable meshes. Skips player mannequins / dummies.
    /// </summary>
    public static class StaticPropColliders
    {
        /// <summary>Meshes above this vertex count get a fitted BoxCollider instead of MeshCollider.</summary>
        const int HugeVertexCount = 25000;
        const int MinVertexCount = 3;

        public static void EnsureStaticColliders(GameObject root)
        {
            if (root == null) return;
            if (IsPlayerMannequin(root)) return;

            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf == null || mf.sharedMesh == null) continue;
                if (mf.GetComponent<Renderer>() == null) continue;
                if (IsPlayerMannequin(mf.gameObject)) continue;
                if (HasMovingRigidbody(mf.gameObject)) continue;

                // Already has a usable collider on this leaf — keep it.
                if (HasUsableLocalCollider(mf.gameObject)) continue;

                var mesh = mf.sharedMesh;
                if (mesh.vertexCount < MinVertexCount) continue;

                // Runtime MeshCollider needs Read/Write; HiPoly FBX imports ship isReadable=0.
                if (!mesh.isReadable || mesh.vertexCount >= HugeVertexCount)
                {
                    AddBoxFittedToRenderer(mf.gameObject);
                    continue;
                }

                if (!TryAddMeshCollider(mf.gameObject, mesh))
                    AddBoxFittedToRenderer(mf.gameObject);
            }

            // Rare skinned props (not mannequins): box from renderer bounds.
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr == null) continue;
                if (IsPlayerMannequin(smr.gameObject)) continue;
                if (HasMovingRigidbody(smr.gameObject)) continue;
                if (HasUsableLocalCollider(smr.gameObject)) continue;
                AddBoxFittedToRenderer(smr.gameObject);
            }
        }

        static bool TryAddMeshCollider(GameObject go, Mesh mesh)
        {
            try
            {
                var col = go.GetComponent<MeshCollider>();
                if (col == null) col = go.AddComponent<MeshCollider>();
                col.sharedMesh = null;
                // Static park props: non-convex is OK (no non-kinematic Rigidbody on this leaf).
                col.convex = false;
                col.sharedMesh = mesh;
                // If cooking failed, Unity clears sharedMesh or leaves collider unusable.
                if (col.sharedMesh == null)
                {
                    Object.Destroy(col);
                    return false;
                }
                return true;
            }
            catch
            {
                var bad = go.GetComponent<MeshCollider>();
                if (bad != null) Object.Destroy(bad);
                return false;
            }
        }

        static void AddBoxFittedToRenderer(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;

            var box = go.GetComponent<BoxCollider>();
            if (box == null) box = go.AddComponent<BoxCollider>();

            // Prefer mesh-local bounds (stable); else renderer localBounds; else world→local.
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var mb = mf.sharedMesh.bounds;
                box.center = mb.center;
                box.size = mb.size;
                return;
            }

            Bounds b = default;
            bool haveLocal = false;
            try
            {
                b = r.localBounds;
                haveLocal = b.size.sqrMagnitude > 1e-8f;
            }
            catch
            {
                haveLocal = false;
            }

            if (haveLocal)
            {
                box.center = b.center;
                box.size = b.size;
                return;
            }

            var wb = r.bounds;
            var lossy = go.transform.lossyScale;
            float sx = Mathf.Abs(lossy.x) < 1e-4f ? 1f : lossy.x;
            float sy = Mathf.Abs(lossy.y) < 1e-4f ? 1f : lossy.y;
            float sz = Mathf.Abs(lossy.z) < 1e-4f ? 1f : lossy.z;
            box.center = go.transform.InverseTransformPoint(wb.center);
            box.size = new Vector3(
                Mathf.Abs(wb.size.x / sx),
                Mathf.Abs(wb.size.y / sy),
                Mathf.Abs(wb.size.z / sz));
        }

        static bool HasUsableLocalCollider(GameObject go)
        {
            var cols = go.GetComponents<Collider>();
            for (int i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                if (c == null || !c.enabled) continue;
                if (c is MeshCollider mc)
                {
                    if (mc.sharedMesh != null) return true;
                    continue;
                }
                return true;
            }
            return false;
        }

        static bool HasMovingRigidbody(GameObject go)
        {
            var rb = go.GetComponentInParent<Rigidbody>();
            return rb != null && !rb.isKinematic;
        }

        static bool IsPlayerMannequin(GameObject go)
        {
            Transform t = go.transform;
            while (t != null)
            {
                var n = t.name;
                if (n.StartsWith("DummyVisual") ||
                    n.StartsWith("Dummy_It") ||
                    n.StartsWith("Dummy_Runner") ||
                    n.StartsWith("Player_") ||
                    n.Contains("Mannequin") ||
                    n == "DummyIt" ||
                    n == "DummyRunner")
                    return true;
                if (t.GetComponent<DummyAvatarBinder>() != null ||
                    t.GetComponent<DummyLocomotor>() != null)
                    return true;
                t = t.parent;
            }
            return false;
        }
    }
}
