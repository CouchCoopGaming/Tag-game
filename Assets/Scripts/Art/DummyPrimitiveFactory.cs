using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Navy Spade–inspired crash dummy when FBX/prefabs are missing.
    /// Featureless head, colored polymer panels, black rubber joints/chest/hands/feet.
    /// Limb names match DummyLocomotor (UpperArm_L/R, UpperLeg_L/R, ...).
    /// </summary>
    public static class DummyPrimitiveFactory
    {
        static Material _matBody, _matItBody, _matJoint, _matSensor;

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

            var bodyMat = asIt ? _matItBody : _matBody;
            var joint = _matJoint;

            // Torso panels (colored) + black chest plate
            Prim(PrimitiveType.Capsule, root.transform, "Torso",
                new Vector3(0f, 1.15f, 0f), new Vector3(0.55f, 0.42f, 0.38f), bodyMat);
            Prim(PrimitiveType.Cube, root.transform, "ChestPlate",
                new Vector3(0f, 1.22f, 0.12f), new Vector3(0.42f, 0.38f, 0.08f), joint);
            Prim(PrimitiveType.Cube, root.transform, "Pelvis",
                new Vector3(0f, 0.88f, 0f), new Vector3(0.4f, 0.16f, 0.28f), joint);

            // Featureless head (no eyes) — hidden in FP via PlayerMotor
            Prim(PrimitiveType.Sphere, root.transform, "Head",
                new Vector3(0f, 1.62f, 0f), new Vector3(0.38f, 0.4f, 0.38f), bodyMat);
            Prim(PrimitiveType.Cylinder, root.transform, "Neck",
                new Vector3(0f, 1.44f, 0f), new Vector3(0.12f, 0.06f, 0.12f), joint);

            var hips = Empty(root.transform, "Hips", new Vector3(0f, 0.9f, 0f));
            var spine = Empty(root.transform, "Spine", new Vector3(0f, 1.2f, 0f));

            BuildArm(spine, "L", new Vector3(-0.32f, 0.08f, 0f), bodyMat, joint, left: true);
            BuildArm(spine, "R", new Vector3(0.32f, 0.08f, 0f), bodyMat, joint, left: false);
            BuildLeg(hips, "L", new Vector3(-0.12f, 0f, 0f), bodyMat, joint);
            BuildLeg(hips, "R", new Vector3(0.12f, 0f, 0f), bodyMat, joint);

            // Optional It sensor strip (glow) — still no face
            if (asIt)
            {
                Prim(PrimitiveType.Cube, root.transform, "Sensor",
                    new Vector3(0f, 1.78f, 0.1f), new Vector3(0.2f, 0.03f, 0.05f), _matSensor);
            }

            return root;
        }

        static void BuildArm(Transform parent, string side, Vector3 pos, Material body, Material joint, bool left)
        {
            var upper = Empty(parent, "UpperArm_" + side, pos);
            // black shoulder ball
            LimbMesh(upper, "Shoulder_" + side, new Vector3(0f, 0.02f, 0f), new Vector3(0.16f, 0.16f, 0.16f), joint, PrimitiveType.Sphere);
            LimbMesh(upper, "UpperArmMesh_" + side, new Vector3(0f, -0.16f, 0f), new Vector3(0.14f, 0.18f, 0.14f), body, PrimitiveType.Capsule);
            var lower = Empty(upper, "LowerArm_" + side, new Vector3(0f, -0.34f, 0f));
            LimbMesh(lower, "Elbow_" + side, Vector3.zero, new Vector3(0.12f, 0.12f, 0.12f), joint, PrimitiveType.Sphere);
            LimbMesh(lower, "LowerArmMesh_" + side, new Vector3(0f, -0.14f, 0f), new Vector3(0.12f, 0.15f, 0.12f), body, PrimitiveType.Capsule);
            var hand = Empty(lower, "Hand_" + side, new Vector3(0f, -0.28f, 0f));
            LimbMesh(hand, "HandMesh_" + side, Vector3.zero, new Vector3(0.11f, 0.08f, 0.1f), joint, PrimitiveType.Cube);
            upper.localRotation = Quaternion.Euler(0f, 0f, left ? 10f : -10f);
        }

        static void BuildLeg(Transform parent, string side, Vector3 pos, Material body, Material joint)
        {
            var upper = Empty(parent, "UpperLeg_" + side, pos);
            LimbMesh(upper, "Hip_" + side, Vector3.zero, new Vector3(0.16f, 0.16f, 0.16f), joint, PrimitiveType.Sphere);
            LimbMesh(upper, "UpperLegMesh_" + side, new Vector3(0f, -0.2f, 0f), new Vector3(0.18f, 0.2f, 0.18f), body, PrimitiveType.Capsule);
            var lower = Empty(upper, "LowerLeg_" + side, new Vector3(0f, -0.4f, 0f));
            LimbMesh(lower, "Knee_" + side, Vector3.zero, new Vector3(0.13f, 0.13f, 0.13f), joint, PrimitiveType.Sphere);
            LimbMesh(lower, "LowerLegMesh_" + side, new Vector3(0f, -0.18f, 0f), new Vector3(0.14f, 0.18f, 0.14f), body, PrimitiveType.Capsule);
            var foot = Empty(lower, "Foot_" + side, new Vector3(0f, -0.36f, 0.05f));
            LimbMesh(foot, "FootMesh_" + side, Vector3.zero, new Vector3(0.15f, 0.07f, 0.26f), joint, PrimitiveType.Cube);
        }

        static Transform Empty(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;
            return go.transform;
        }

        static void LimbMesh(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, PrimitiveType type)
        {
            Prim(type, parent, name, pos, scale, mat);
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
            // Runner default: tan polymer; It: orange — black rubber joints
            _matBody = MakeMat(new Color(0.92f, 0.78f, 0.55f));
            _matItBody = MakeMat(new Color(0.95f, 0.35f, 0.12f));
            _matJoint = MakeMat(new Color(0.06f, 0.06f, 0.07f));
            _matSensor = MakeMat(new Color(0.2f, 0.9f, 1f));
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
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
            return m;
        }
    }
}
