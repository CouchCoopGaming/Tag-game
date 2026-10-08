using Tag.Settings;
using UnityEngine;

namespace Tag.FX
{
    public enum FxBurstKind
    {
        Roll = 0,
        Land = 1,
        WallScuff = 2,
        VaultPuff = 3,
        Run = 4
    }

    /// <summary>
    /// One pooled particle system per pawn. Bursts emit from the prebuilt system.
    /// Reduced flashing skips the burst. The motor is not written.
    /// </summary>
    public sealed class FxBurstPool : MonoBehaviour
    {
        ParticleSystem _ps;
        ParticleSystem.MainModule _main;
        bool _ready;

        public static FxBurstPool Ensure(Transform host)
        {
            if (host == null) return null;
            FxBurstPool have = host.GetComponent<FxBurstPool>();
            if (have != null) return have;
            Transform child = host.Find("FxBurstPool");
            if (child != null)
            {
                have = child.GetComponent<FxBurstPool>();
                if (have != null) return have;
            }
            var go = new GameObject("FxBurstPool");
            go.transform.SetParent(host, false);
            return go.AddComponent<FxBurstPool>();
        }

        void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            if (_ps == null) _ps = gameObject.AddComponent<ParticleSystem>();
            _main = _ps.main;
            _main.playOnAwake = false;
            _main.loop = false;
            _main.duration = 0.45f;
            _main.startLifetime = 0.38f;
            _main.startSpeed = 2.2f;
            _main.startSize = 0.18f;
            _main.maxParticles = 64;
            _main.simulationSpace = ParticleSystemSimulationSpace.World;
            _main.gravityModifier = 0.55f;
            var emission = _ps.emission;
            emission.enabled = false;
            var shape = _ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.12f;
            var rend = _ps.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.alignment = ParticleSystemRenderSpace.View;
            rend.minParticleSize = 0f;
            rend.maxParticleSize = 2f;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.material = DustMaterial();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _ready = true;
        }

        /// <summary>
        /// Unlit soft disc. The built-in URP particle material soft-fades
        /// anything sitting on the ground, which hid the whole puff.
        /// The opaque core fills the billboard, so startSize is the visible diameter.
        /// </summary>
        static Material DustMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader);
            mat.mainTexture = Disc();
            if (mat.HasProperty("_SoftParticlesEnabled"))
                mat.SetFloat("_SoftParticlesEnabled", 0f);
            if (mat.HasProperty("_CameraFadingEnabled"))
                mat.SetFloat("_CameraFadingEnabled", 0f);
            mat.DisableKeyword("_SOFTPARTICLES_ON");
            return mat;
        }

        static Texture2D Disc()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var pix = new Color[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n - 0.5f;
                    float dy = (y + 0.5f) / n - 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / 0.5f;
                    float a = 0f;
                    if (r < 0.72f) a = 1f;
                    else if (r < 1f) a = 1f - (r - 0.72f) / 0.28f;
                    pix[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pix);
            tex.Apply(false, true);
            return tex;
        }

        public static bool Calmed()
        {
            GameSettings settings = GameSettings.Current;
            if (settings == null) return false;
            if (settings.AnyReduceFlash()) return true;
            return settings.Effects <= 0;
        }

        public void Play(FxBurstKind kind, Vector3 worldPos)
        {
            if (!_ready || _ps == null) return;
            if (Calmed()) return;
            Apply(kind);
            _ps.transform.position = worldPos;
            int emit = Count(kind);
            if (GameSettings.Current != null && GameSettings.Current.Effects == FxAmount.Low)
            {
                emit = emit / 2;
                if (emit < 1) emit = 1;
            }
            _ps.Emit(emit);
        }

        public void PlayShaped(FxBurstKind kind, Vector3 worldPos, DustLook.Puff puff)
        {
            if (!_ready || _ps == null) return;
            if (Calmed()) return;
            if (puff.Count <= 0) return;
            Apply(kind);
            _main.startSize = puff.Size;
            _main.startLifetime = puff.Life > 0.05f ? puff.Life : 0.05f;
            float a = puff.Opacity;
            if (a < 0f) a = 0f;
            if (a > 1f) a = 1f;
            _main.startColor = new Color(puff.R, puff.G, puff.B, a);
            if (puff.Splash != 0)
            {
                _main.startSpeed = 2.4f;
                _main.gravityModifier = 1.1f;
            }
            else if (puff.Spark != 0)
            {
                _main.startSpeed = 1.8f;
                _main.gravityModifier = 0.15f;
            }
            else
            {
                _main.gravityModifier = 0.55f;
            }
            AimPlume(puff);
            _ps.transform.position = worldPos;
            int n = puff.Count;
            float dens = FxAmount.Density(GameSettings.Current);
            if (dens < 0.99f)
            {
                n = (int)(n * dens + 0.001f);
                if (n < 1) n = 1;
            }
            if (n > 16) n = 16;
            _ps.Emit(n);
            if (puff.Core > 0.15f && n > 1)
            {
                _main.startSize = puff.Size * 0.55f;
                float grit = 0.62f;
                _main.startColor = new Color(puff.R * grit, puff.G * grit, puff.B * grit, a);
                int core = n / 3;
                if (core < 1) core = 1;
                _ps.Emit(core);
            }
        }

        /// <summary>
        /// One mote with its own velocity. The land ring uses this so each puff
        /// stays on the circle instead of inheriting one shared forward speed.
        /// </summary>
        public void PlayRadial(Vector3 worldPos, Vector3 velocity, DustLook.Puff puff)
        {
            if (!_ready || _ps == null) return;
            if (Calmed()) return;
            if (puff.Count <= 0 && puff.Opacity <= 0.01f) return;
            float a = puff.Opacity;
            if (a < 0f) a = 0f;
            if (a > 1f) a = 1f;
            float life = puff.Life > 0.05f ? puff.Life : 0.05f;
            var ep = new ParticleSystem.EmitParams();
            ep.position = worldPos;
            ep.velocity = velocity;
            ep.startSize = puff.Size;
            ep.startLifetime = life;
            ep.startColor = new Color(puff.R, puff.G, puff.B, a);
            ep.applyShapeToPosition = false;
            _ps.Emit(ep, 1);
            if (puff.Core > 0.15f)
            {
                float grit = 0.62f;
                ep.startSize = puff.Size * 0.55f;
                ep.startColor = new Color(puff.R * grit, puff.G * grit, puff.B * grit, a);
                ep.position = worldPos + Vector3.up * 0.02f;
                _ps.Emit(ep, 1);
            }
        }

        void AimPlume(DustLook.Puff puff)
        {
            var shape = _ps.shape;
            shape.enabled = true;
            if (puff.Back > 0.04f && transform.parent != null)
            {
                Vector3 fwd = transform.parent.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.0001f)
                    fwd = Vector3.forward;
                else
                    fwd.Normalize();
                float rise = puff.Lift > 0.01f ? puff.Lift : 0.04f;
                Vector3 aim = -fwd * puff.Back + Vector3.up * rise;
                transform.rotation = Quaternion.LookRotation(aim.normalized, Vector3.up);
                float life = puff.Life > 0.05f ? puff.Life : 0.05f;
                _main.startSpeed = puff.Back / life;
                _main.gravityModifier = 0.25f;
                // The cloud is Span long at birth. A radius of Span * 0.16
                // was an 11 cm ball, which is why a 72 cm sprint read as a speck.
                float span = puff.Span > 0.05f ? puff.Span : puff.Back;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(
                    Mathf.Max(0.05f, span * 0.36f),
                    Mathf.Max(0.04f, rise * 0.8f + 0.04f),
                    span);
                shape.position = new Vector3(0f, rise * 0.2f, span * 0.5f);
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Hemisphere;
                shape.radius = 0.05f;
                shape.scale = Vector3.one;
                shape.position = Vector3.zero;
                if (puff.Spark == 0 && puff.Splash == 0)
                {
                    _main.startSpeed = 0.35f;
                    _main.gravityModifier = 0.45f;
                }
                if (transform.parent != null)
                    transform.rotation = transform.parent.rotation;
            }
        }

        void Apply(FxBurstKind kind)
        {
            if (kind == FxBurstKind.Roll)
            {
                _main.startSpeed = 2.6f;
                _main.startSize = 0.24f;
                _main.startColor = new Color(0.62f, 0.52f, 0.38f, 0.82f);
            }
            else if (kind == FxBurstKind.Land)
            {
                _main.startSpeed = 1.6f;
                _main.startSize = 0.16f;
                _main.startColor = new Color(0.70f, 0.64f, 0.52f, 0.7f);
            }
            else if (kind == FxBurstKind.WallScuff)
            {
                _main.startSpeed = 1.2f;
                _main.startSize = 0.10f;
                _main.startColor = new Color(0.75f, 0.72f, 0.66f, 0.65f);
            }
            else if (kind == FxBurstKind.Run)
            {
                _main.startSpeed = 1.5f;
                _main.startSize = 0.16f;
                _main.startColor = new Color(0.70f, 0.62f, 0.42f, 0.7f);
            }
            else
            {
                _main.startSpeed = 1.4f;
                _main.startSize = 0.12f;
                _main.startColor = new Color(0.78f, 0.74f, 0.66f, 0.6f);
            }
        }

        static int Count(FxBurstKind kind)
        {
            if (kind == FxBurstKind.Roll) return 12;
            if (kind == FxBurstKind.Land) return 7;
            if (kind == FxBurstKind.WallScuff) return 4;
            return 5;
        }
    }
}
