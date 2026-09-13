using System.Collections.Generic;
using UnityEngine;

/// Holds every official 33-charge palette.
/// Create one asset in the project and generate children from palettes.json
/// or fill them in the inspector.
[CreateAssetMenu(menuName = "Chroma/Palette Library", fileName = "ChromaPaletteLibrary")]
public class ChromaPaletteLibrary : ScriptableObject
{
    public List<ChromaPalette> all = new List<ChromaPalette>();

    public ChromaPalette Get(string id)
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] != null && all[i].id == id) return all[i];
        }
        return null;
    }

    public ChromaPalette Get(string hue, string shade)
    {
        return Get(hue.ToLowerInvariant() + "_" + shade.ToLowerInvariant());
    }
}
