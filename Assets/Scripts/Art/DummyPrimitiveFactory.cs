using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Navy Spade–inspired crash dummy when FBX/prefabs lack a hierarchical limb rig.
    /// Featureless head, colored polymer panels, black rubber joints/chest/hands/feet.
    /// Limb names match DummyLocomotor (UpperArm_L/R, UpperLeg_L/R, ...).
    /// Colors aligned with Tools/Tag/build_mannequin_hier.py COLORS / PANELS / JOINT.
    /// </summary>
    public static class DummyPrimitiveFactory
    {
        static Material _matBody, _matItBody, _matJoint, _matPanel, _matItPanel, _matSensor;

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
            var panelMat = asIt ? _matItPanel : _matPanel;
            var joint = _matJoint;

            // Black rubber chest / pelvis core
            Prim(PrimitiveType.Cube, root.transform, "ChestPlate",
                new Vector3(0f, 1.24f, 0.02f), new Vector3(0.46f, 0.44f, 0.26f), joint);
            Prim(PrimitiveType.Cube, root.transform, "Pelvis",
                new Vector3(0f, 0.90f, 0f), new Vector3(0.42f, 0.16f, 0.28f), joint);

            // Colored polymer torso panels (Navy Spade crash-dummy look)
            Prim(PrimitiveType.Cube, root.transform, "Panel_Chest",
                new Vector3(0f, 1.30f, 0.155f), new Vector3(0.38f, 0.26f, 0.045f), panelMat);
            Prim(PrimitiveType.Cube, root.transform, "Panel_Abs",
                new Vector3(0f, 1.06f, 0.145f), new Vector3(0.32f, 0.16f, 0.04f), panelMat);
            Prim(PrimitiveType.Cube, root.transform, "Panel_Back",
                new Vector3(0f, 1.22f, -0.15f), new Vector3(0.42f, 0.36f, 0.05f), bodyMat);
            Prim(PrimitiveType.Cube, root.transform, "Panel_Side_L",
                new Vector3(-0.26f, 1.18f, 0f), new Vector3(0.06f, 0.32f, 0.22f), panelMat);
            Prim(PrimitiveType.Cube, root.transform, "Panel_Side_R",
                new Vector3(0.26f, 1.18f, 0f), new Vector3(0.06f, 0.32f, 0.22f), panelMat);

            // Featureless head (sphere) + black rubber neck collar — no face/eyes
            Prim(PrimitiveType.Sphere, root.transform, "Head",
                new Vector3(0f, 1.66f, 0f), new Vector3(0.34f, 0.37f, 0.34f), bodyMat);
            Prim(PrimitiveType.Cylinder, root.transform, "Neck",
                new Vector3(0f, 1.47f, 0f), new Vector3(0.12f, 0.045f, 0.12f), joint);
            Prim(PrimitiveType.Cylinder, root.transform, "NeckCollar",
                new Vector3(0f, 1.50f, 0f), new Vector3(0.22f, 0.03f, 0.22f), joint);

            var hips = Empty(root.transform, "Hips", new Vector3(0f, 0.92f, 0f));
            var spine = Empty(root.transform, "Spine", new Vector3(0f, 1.22f, 0f));

            BuildArm(spine, "L", new Vector3(-0.33f, 0.1f, 0f), bodyMat, panelMat, joint, left: true);
            BuildArm(spine, "R", new Vector3(0.33f, 0.1f, 0f), bodyMat, panelMat, joint, left: false);
            BuildLeg(hips, "L", new Vector3(-0.12f, 0f, 0f), bodyMat, panelMat, joint);
            BuildLeg(hips, "R", new Vector3(0.12f, 0f, 0f), bodyMat, panelMat, joint);

            // Optional It sensor strip (glow) — still no face
            if (asIt)
            {
                Prim(PrimitiveType.Cube, root.transform, "Sensor",
                    new Vector3(0f, 1.78f, 0.12f), new Vector3(0.22f, 0.035f, 0.05f), _matSensor);
            }

            return root;
        }

        static void BuildArm(Transform parent, string side, Vector3 pos, Material body, Material panel, Material joint, bool left)
        {
            var upper = Empty(parent, "UpperArm_" + side, pos);
            // black shoulder ball
            LimbMesh(upper, "Shoulder_" + side, new Vector3(0f, 0.02f, 0f), new Vector3(0.18f, 0.18f, 0.18f), joint, PrimitiveType.Sphere);
            LimbMesh(upper, "UpperArmMesh_" + side, new Vector3(0f, -0.16f, 0f), new Vector3(0.15f, 0.18f, 0.15f), body, PrimitiveType.Capsule);
            LimbMesh(upper, "UpperArmPanel_" + side, new Vector3(0f, -0.16f, 0.06f), new Vector3(0.12f, 0.14f, 0.04f), panel, PrimitiveType.Cube);
            var lower = Empty(upper, "LowerArm_" + side, new Vector3(0f, -0.34f, 0f));
            LimbMesh(lower, "Elbow_" + side, Vector3.zero, new Vector3(0.13f, 0.13f, 0.13f), joint, PrimitiveType.Sphere);
            LimbMesh(lower, "LowerArmMesh_" + side, new Vector3(0f, -0.14f, 0f), new Vector3(0.12f, 0.15f, 0.12f), body, PrimitiveType.Capsule);
            LimbMesh(lower, "LowerArmPanel_" + side, new Vector3(0f, -0.14f, 0.05f), new Vector3(0.10f, 0.12f, 0.035f), panel, PrimitiveType.Cube);
            var hand = Empty(lower, "Hand_" + side, new Vector3(0f, -0.28f, 0f));
            LimbMesh(hand, "HandMesh_" + side, Vector3.zero, new Vector3(0.12f, 0.09f, 0.11f), joint, PrimitiveType.Cube);
            upper.localRotation = Quaternion.Euler(0f, 0f, left ? 12f : -12f);
        }

        static void BuildLeg(Transform parent, string side, Vector3 pos, Material body, Material panel, Material joint)
        {
            var upper = Empty(parent, "UpperLeg_" + side, pos);
            LimbMesh(upper, "Hip_" + side, Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f), joint, PrimitiveType.Sphere);
            LimbMesh(upper, "UpperLegMesh_" + side, new Vector3(0f, -0.2f, 0f), new Vector3(0.18f, 0.2f, 0.18f), body, PrimitiveType.Capsule);
            LimbMesh(upper, "ThighPanel_" + side, new Vector3(0f, -0.2f, 0.07f), new Vector3(0.14f, 0.16f, 0.04f), panel, PrimitiveType.Cube);
            var lower = Empty(upper, "LowerLeg_" + side, new Vector3(0f, -0.4f, 0f));
            LimbMesh(lower, "Knee_" + side, Vector3.zero, new Vector3(0.14f, 0.14f, 0.14f), joint, PrimitiveType.Sphere);
            LimbMesh(lower, "LowerLegMesh_" + side, new Vector3(0f, -0.18f, 0f), new Vector3(0.14f, 0.18f, 0.14f), body, PrimitiveType.Capsule);
            LimbMesh(lower, "ShinPanel_" + side, new Vector3(0f, -0.18f, 0.06f), new Vector3(0.11f, 0.14f, 0.035f), panel, PrimitiveType.Cube);
            var foot = Empty(lower, "Foot_" + side, new Vector3(0f, -0.36f, 0.05f));
            LimbMesh(foot, "FootMesh_" + side, Vector3.zero, new Vector3(0.16f, 0.08f, 0.28f), joint, PrimitiveType.Cube);
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
            // Hier Navy Spade palette (build_mannequin_hier.py COLORS/PANELS/JOINT/SENSOR).
            // Runner = Tan foam + Tan polymer panels; It = Orange foam + Orange panels.
            // Smoothness ~= 1 - Blender roughness (body 0.40, panel 0.28, joint 0.90, sensor 0.22).
            _matBody = MakeMat(new Color(0.90f, 0.76f, 0.52f), 0.60f);
            _matItBody = MakeMat(new Color(0.94f, 0.42f, 0.14f), 0.60f);
            _matPanel = MakeMat(new Color(0.10f, 0.48f, 0.68f), 0.72f);
            _matItPanel = MakeMat(new Color(1.00f, 0.48f, 0.05f), 0.72f);
            _matJoint = MakeMat(new Color(0.02f, 0.02f, 0.025f), 0.10f);
            _matSensor = MakeMat(new Color(0.20f, 0.90f, 1.0f), 0.78f);
        }

        public static Material MakeMat(Color c) => MakeMat(c, 0.42f);

        public static Material MakeMat(Color c, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var m = new Material(shader) { name = "DummyPrim_" + ColorUtility.ToHtmlStringRGB(c) };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
        }
    }
}
