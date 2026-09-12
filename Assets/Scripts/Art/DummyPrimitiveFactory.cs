using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Crash-test dummy built from primitives when Dummy_Runner / Dummy_It
    /// FBX/prefabs are missing. Limb names match Dummy_*.fbx so DummyLocomotor
    /// can drive either path. Paint lock: Runner #E8D9C0/#2BB3A3, It #FF6A00/black.
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

            Color body = asIt ? new Color(1f, 0.416f, 0f) : new Color(0.91f, 0.851f, 0.753f);
            Color accent = asIt ? new Color(0.04f, 0.04f, 0.04f) : new Color(0.169f, 0.702f, 0.639f);
            var bodyMat = asIt ? _matItBody : _matBody;
            var accentMat = asIt ? _matItAccent : _matAccent;

            Prim(PrimitiveType.Capsule, root.transform, "Body",
                new Vector3(0f, 1.05f, 0f), new Vector3(0.72f, 0.55f, 0.55f), bodyMat);
            Prim(PrimitiveType.Sphere, root.transform, "Head",
                new Vector3(0f, 1.62f, 0f), new Vector3(0.42f, 0.42f, 0.42f), bodyMat);
            Prim(PrimitiveType.Sphere, root.transform, "EyeL",
                new Vector3(-0.1f, 1.66f, 0.16f), new Vector3(0.07f, 0.07f, 0.07f), _matEye);
            Prim(PrimitiveType.Sphere, root.transform, "EyeR",
                new Vector3(0.1f, 1.66f, 0.16f), new Vector3(0.07f, 0.07f, 0.07f), _matEye);
            Prim(PrimitiveType.Cube, root.transform, "Band",
                new Vector3(0f, 1.22f, 0.12f), new Vector3(0.5f, 0.08f, 0.12f), accentMat);
            Prim(PrimitiveType.Cube, root.transform, "Sensor",
                new Vector3(0f, 1.78f, 0.12f), new Vector3(0.22f, 0.04f, 0.06f), _matSensor);

            var hips = Empty(root.transform, "Hips", new Vector3(0f, 0.92f, 0f));
            var spine = Empty(root.transform, "Spine", new Vector3(0f, 1.22f, 0f));

            BuildArm(spine, "L", new Vector3(-0.34f, 0.12f, 0f), bodyMat, accentMat, left: true);
            BuildArm(spine, "R", new Vector3(0.34f, 0.12f, 0f), bodyMat, accentMat, left: false);
            BuildLeg(hips, "L", new Vector3(-0.13f, 0f, 0f), bodyMat, accentMat);
            BuildLeg(hips, "R", new Vector3(0.13f, 0f, 0f), bodyMat, accentMat);

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

        static void BuildArm(Transform parent, string side, Vector3 pos, Material body, Material accent, bool left)
        {
            var upper = Empty(parent, "UpperArm_" + side, pos);
            LimbMesh(upper, "UpperArmMesh_" + side, new Vector3(0f, -0.16f, 0f), new Vector3(0.16f, 0.2f, 0.16f), body);
            var lower = Empty(upper, "LowerArm_" + side, new Vector3(0f, -0.34f, 0f));
            LimbMesh(lower, "LowerArmMesh_" + side, new Vector3(0f, -0.14f, 0f), new Vector3(0.13f, 0.16f, 0.13f), body);
            var hand = Empty(lower, "Hand_" + side, new Vector3(0f, -0.28f, 0f));
            LimbMesh(hand, "HandMesh_" + side, Vector3.zero, new Vector3(0.12f, 0.08f, 0.1f), accent);
            upper.localRotation = Quaternion.Euler(0f, 0f, left ? 8f : -8f);
        }

        static void BuildLeg(Transform parent, string side, Vector3 pos, Material body, Material accent)
        {
            var upper = Empty(parent, "UpperLeg_" + side, pos);
            LimbMesh(upper, "UpperLegMesh_" + side, new Vector3(0f, -0.2f, 0f), new Vector3(0.2f, 0.22f, 0.2f), body);
            var lower = Empty(upper, "LowerLeg_" + side, new Vector3(0f, -0.4f, 0f));
            LimbMesh(lower, "LowerLegMesh_" + side, new Vector3(0f, -0.18f, 0f), new Vector3(0.16f, 0.2f, 0.16f), body);
            var foot = Empty(lower, "Foot_" + side, new Vector3(0f, -0.36f, 0.06f));
            LimbMesh(foot, "FootMesh_" + side, Vector3.zero, new Vector3(0.16f, 0.07f, 0.26f), accent);
        }

        static Transform Empty(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;
            return go.transform;
        }

        static void LimbMesh(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            Prim(PrimitiveType.Capsule, parent, name, pos, scale, mat);
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
            _matBody = MakeMat(new Color(0.91f, 0.851f, 0.753f));
            _matItBody = MakeMat(new Color(1f, 0.416f, 0f));
            _matAccent = MakeMat(new Color(0.169f, 0.702f, 0.639f));
            _matItAccent = MakeMat(new Color(0.04f, 0.04f, 0.04f));
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
