using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Crash-test dummy built from primitives when Dummy_Runner / Dummy_It prefabs
    /// are empty stubs (Editor hub setup not run). Third-person readable.
    /// </summary>
    public static class DummyPrimitiveFactory
    {
        static Material _matBody, _matItBody, _matAccent, _matItAccent, _matEye, _matSensor;

        public static bool PrefabHasRenderer(GameObject prefab)
        {
            return prefab != null && prefab.GetComponentInChildren<Renderer>(true) != null;
        }

        public static GameObject Build(Transform parent, bool asIt)
        {
            EnsureMaterials();
            var root = new GameObject(asIt ? "DummyVisual_It" : "DummyVisual_Runner");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            Color body = asIt ? new Color(1f, 0.42f, 0.05f) : new Color(0.91f, 0.85f, 0.75f);
            Color accent = asIt ? new Color(0.08f, 0.08f, 0.08f) : new Color(0.17f, 0.70f, 0.64f);

            Prim(PrimitiveType.Capsule, root.transform, "Body",
                new Vector3(0f, 0.9f, 0f), new Vector3(0.85f, 0.85f, 0.85f), asIt ? _matItBody : _matBody);
            Prim(PrimitiveType.Sphere, root.transform, "Head",
                new Vector3(0f, 1.62f, 0f), new Vector3(0.42f, 0.42f, 0.42f), asIt ? _matItBody : _matBody);
            Prim(PrimitiveType.Sphere, root.transform, "EyeL",
                new Vector3(-0.1f, 1.66f, 0.16f), new Vector3(0.07f, 0.07f, 0.07f), _matEye);
            Prim(PrimitiveType.Sphere, root.transform, "EyeR",
                new Vector3(0.1f, 1.66f, 0.16f), new Vector3(0.07f, 0.07f, 0.07f), _matEye);
            Prim(PrimitiveType.Cube, root.transform, "Band",
                new Vector3(0f, 1.15f, 0.12f), new Vector3(0.55f, 0.08f, 0.12f), asIt ? _matItAccent : _matAccent);
            Prim(PrimitiveType.Cube, root.transform, "Sensor",
                new Vector3(0f, 1.78f, 0.12f), new Vector3(0.22f, 0.04f, 0.06f), _matSensor);

            // Cache intended colors on the body mat instance so ItController tint still reads.
            var bodyRend = root.transform.Find("Body")?.GetComponent<Renderer>();
            if (bodyRend != null)
            {
                var block = new MaterialPropertyBlock();
                bodyRend.GetPropertyBlock(block);
                block.SetColor("_BaseColor", body);
                block.SetColor("_Color", body);
                bodyRend.SetPropertyBlock(block);
            }

            var bandRend = root.transform.Find("Band")?.GetComponent<Renderer>();
            if (bandRend != null)
            {
                var block = new MaterialPropertyBlock();
                bandRend.GetPropertyBlock(block);
                block.SetColor("_BaseColor", accent);
                block.SetColor("_Color", accent);
                bandRend.SetPropertyBlock(block);
            }

            return root;
        }

        static void Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        static void EnsureMaterials()
        {
            if (_matBody != null) return;
            _matBody = MakeMat(new Color(0.91f, 0.85f, 0.75f));
            _matItBody = MakeMat(new Color(1f, 0.42f, 0.05f));
            _matAccent = MakeMat(new Color(0.17f, 0.70f, 0.64f));
            _matItAccent = MakeMat(new Color(0.08f, 0.08f, 0.08f));
            _matEye = MakeMat(new Color(0.05f, 0.05f, 0.06f));
            _matSensor = MakeMat(new Color(0.2f, 0.85f, 1f));
        }

        public static Material MakeMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var m = new Material(shader) { name = "DummyPrim_" + ColorUtility.ToHtmlStringRGB(c) };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }
    }
}
