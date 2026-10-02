using System.Collections.Generic;
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
        // Soft body foam — same keys as DummyAvatarBinder.MannequinColors / Hier COLORS.
        static readonly Dictionary<string, Color> BodyColors = new Dictionary<string, Color>
        {
            ["Blue"] = new Color(0.42f, 0.68f, 0.92f),
            ["Mint"] = new Color(0.42f, 0.82f, 0.70f),
            ["Orange"] = new Color(0.94f, 0.42f, 0.14f),
            ["Lavender"] = new Color(0.70f, 0.58f, 0.88f),
            ["Tan"] = new Color(0.90f, 0.76f, 0.52f),
            ["Red"] = new Color(0.88f, 0.22f, 0.24f),
        };

        // Saturated polymer accent panels — Hier PANELS.
        static readonly Dictionary<string, Color> PanelColors = new Dictionary<string, Color>
        {
            ["Blue"] = new Color(0.08f, 0.32f, 0.78f),
            ["Mint"] = new Color(0.06f, 0.58f, 0.48f),
            ["Orange"] = new Color(1.00f, 0.48f, 0.05f),
            ["Lavender"] = new Color(0.48f, 0.28f, 0.82f),
            ["Tan"] = new Color(0.10f, 0.48f, 0.68f),
            ["Red"] = new Color(0.72f, 0.06f, 0.10f),
        };

        static Material _matJoint, _matSensor;
        static readonly Dictionary<string, Material> _bodyMats = new Dictionary<string, Material>();
        static readonly Dictionary<string, Material> _panelMats = new Dictionary<string, Material>();

        public static bool PrefabHasRenderer(GameObject prefab)
        {
            return prefab != null && prefab.GetComponentInChildren<Renderer>(true) != null;
        }

        /// <param name="colorKey">
        /// Runner foam/panel key (Blue/Mint/Orange/Lavender/Tan/Red). Ignored when asIt —
        /// It uses Orange body/panels to match Dummy_Mannequin_Orange_Hier_Hi.
        /// </param>
        public static GameObject Build(Transform parent, bool asIt, string colorKey = null)
        {
            // It: Orange foam + Orange panels (match Hier Dummy_Mannequin_Orange_Hier_Hi). Runner: PickColor key.
            string key = asIt ? "Orange" : NormalizeColorKey(colorKey);
            EnsureMaterials(key);
            var bodyMat = _bodyMats[key];
            var panelMat = _panelMats[key];
            var joint = _matJoint;

            var root = new GameObject(asIt ? "DummyVisual_It" : "DummyVisual_Runner");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            // Black rubber chest / pelvis core
            Prim(PrimitiveType.Sphere, root.transform, "ChestPlate",
                new Vector3(0f, 1.26f, 0.02f), new Vector3(0.50f, 0.48f, 0.30f), joint);
            Prim(PrimitiveType.Sphere, root.transform, "Pelvis",
                new Vector3(0f, 0.90f, 0f), new Vector3(0.44f, 0.20f, 0.30f), joint);

            // Colored polymer torso panels (Navy Spade crash-dummy look)
            Prim(PrimitiveType.Sphere, root.transform, "Panel_Chest",
                new Vector3(0f, 1.30f, 0.155f), new Vector3(0.38f, 0.26f, 0.045f), panelMat);
            Prim(PrimitiveType.Sphere, root.transform, "Panel_Abs",
                new Vector3(0f, 1.06f, 0.145f), new Vector3(0.32f, 0.16f, 0.04f), panelMat);
            Prim(PrimitiveType.Sphere, root.transform, "Panel_Back",
                new Vector3(0f, 1.22f, -0.15f), new Vector3(0.42f, 0.36f, 0.05f), bodyMat);
            Prim(PrimitiveType.Sphere, root.transform, "Panel_Side_L",
                new Vector3(-0.26f, 1.18f, 0f), new Vector3(0.06f, 0.32f, 0.22f), panelMat);
            Prim(PrimitiveType.Sphere, root.transform, "Panel_Side_R",
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

        static string NormalizeColorKey(string colorKey)
        {
            if (!string.IsNullOrEmpty(colorKey) && BodyColors.ContainsKey(colorKey))
                return colorKey;
            return "Tan";
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
            LimbMesh(hand, "HandMesh_" + side, Vector3.zero, new Vector3(0.11f, 0.08f, 0.10f), joint, PrimitiveType.Sphere);
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
            LimbMesh(foot, "FootMesh_" + side, Vector3.zero, new Vector3(0.14f, 0.07f, 0.24f), joint, PrimitiveType.Sphere);
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

        static void EnsureMaterials(string colorKey)
        {
            // Hier Navy Spade palette (build_mannequin_hier.py COLORS/PANELS/JOINT/SENSOR).
            // Smoothness ~= 1 - Blender roughness (body 0.40, panel 0.28, joint 0.90, sensor 0.22).
            if (_matJoint == null)
                _matJoint = MakeMat(new Color(0.02f, 0.02f, 0.025f), 0.10f);
            if (_matSensor == null)
                _matSensor = MakeMat(new Color(0.20f, 0.90f, 1.0f), 0.78f);

            if (!_bodyMats.ContainsKey(colorKey))
                _bodyMats[colorKey] = MakeMat(BodyColors[colorKey], 0.60f);
            if (!_panelMats.ContainsKey(colorKey))
                _panelMats[colorKey] = MakeMat(PanelColors[colorKey], 0.72f);
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
