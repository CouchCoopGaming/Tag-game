using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Two TextMesh faces, yaw 0 and yaw 180, both at scale 1. Each uses a
    /// back-face-culled material so a split camera on either side reads the
    /// front of one face. Negative scale is not used.
    /// </summary>
    public static class WorldSign
    {
        public static void AddTwoSided(Transform parent, string text, int fontSize, float characterSize, Color color)
        {
            AddFace(parent, text, fontSize, characterSize, color, 0f, 0.02f, "Face_0");
            AddFace(parent, text, fontSize, characterSize, color, 180f, -0.02f, "Face_180");
        }

        /// <summary>
        /// One culled face, read only from the side its yaw faces. For a sign
        /// aimed at one view that should not show from behind.
        /// </summary>
        public static void AddOneSided(Transform parent, string text, int fontSize, float characterSize, Color color)
        {
            AddFace(parent, text, fontSize, characterSize, color, 0f, 0f, "Face_0");
        }

        static void AddFace(Transform parent, string text, int fontSize, float characterSize, Color color, float yaw, float z, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;

            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = fontSize;
            tm.characterSize = characterSize;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;

            MeshRenderer rend = go.GetComponent<MeshRenderer>();
            Material src = Resources.Load<Material>("World/SignText");
            if (rend != null && src != null)
            {
                Material mat = new Material(src);
                if (tm.font != null && tm.font.material != null)
                    mat.mainTexture = tm.font.material.mainTexture;
                rend.sharedMaterial = mat;
            }
        }
    }
}
