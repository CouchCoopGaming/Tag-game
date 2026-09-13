using UnityEngine;

/// Attach to the character root. Assign every tinted renderer.
/// Colors are pushed through a MaterialPropertyBlock so 33 shades
/// do not require 33 material instances.
public class ChromaApplier : MonoBehaviour
{
    static readonly int IdPrimary   = Shader.PropertyToID("_Primary");
    static readonly int IdSecondary = Shader.PropertyToID("_Secondary");
    static readonly int IdAccent    = Shader.PropertyToID("_Accent");
    static readonly int IdEmission  = Shader.PropertyToID("_EmissionStrength");

    public Renderer[] renderers;
    public ChromaPalette palette;

    MaterialPropertyBlock _block;

    void OnEnable()
    {
        if (palette != null) Apply(palette);
    }

    public void Apply(ChromaPalette p)
    {
        if (p == null || renderers == null) return;
        palette = p;
        if (_block == null) _block = new MaterialPropertyBlock();

        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_block);
            _block.SetColor(IdPrimary,   p.primary);
            _block.SetColor(IdSecondary, p.secondary);
            _block.SetColor(IdAccent,    p.accent);
            _block.SetFloat(IdEmission,  p.emission);
            r.SetPropertyBlock(_block);
        }
    }
}
