using UnityEngine;

[CreateAssetMenu(menuName = "Chroma/Palette", fileName = "Palette_")]
public class ChromaPalette : ScriptableObject
{
    [Header("Identity")]
    public string id = "green_standard";
    public string displayName = "Green / Standard";

    [Header("Tint Zones")]
    public Color primary = Color.white;
    public Color secondary = Color.gray;
    public Color accent = Color.red;

    [Header("Tron / visor")]
    [Range(0f, 8f)] public float emission = 0f;

    public static Color FromHex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }
}
