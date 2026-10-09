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
        const int ShapeSlots = 16;
        const int MarkSlots = 4;
        const float MarkLife = 0.42f;

        ParticleSystem _ps;
        ParticleSystem.MainModule _main;
        bool _ready;
        Transform[] _shapeT;
        Renderer[] _shapeR;
        Material[] _shapeM;
        float[] _shapeAge;
        float[] _shapeLife;
        Vector3[] _shapeV;
        Transform[] _markT;
        Renderer[] _markR;
        Material[] _markM;
        float[] _markAge;

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
            BuildShapes();
            _ready = true;
        }

        void LateUpdate()
        {
            AdvanceShapes();
        }

        /// <summary>
        /// Unlit soft disc. The built-in URP particle material soft-fades
        /// anything sitting on the ground, which hid the whole puff.
        /// The disc fades to the edge. startSize is the soft quad; the dense core is inside it.
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
                    if (r < 1f)
                    {
                        float t = 1f - r;
                        a = t * t * (3f - 2f * t);
                    }
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
            // The mark is the contact. A metal plant still emits nothing.
            // DustLook.Life stays on the proof line. The drawn puff ends with the mark
            // so a concrete sprint (0.50 s) cannot outlast the 0.42 s sheet.
            // Brick still places one chip; the airborne count is unchanged.
            if (kind == FxBurstKind.Run && puff.Stamp != 0)
            {
                PlaceMark(worldPos, puff);
                if (puff.Life > MarkLife)
                    puff.Life = MarkLife;
            }
            if (puff.Count <= 0) return;
            if (puff.Shape > 0.5f && (kind == FxBurstKind.Run || kind == FxBurstKind.WallScuff))
            {
                EmitShaped(worldPos, puff);
                return;
            }
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
            else if (puff.Back > 0.04f)
            {
                EmitTrail(worldPos, puff, a);
                return;
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

        /// <summary>
        /// Foot dust as a trail: small and dense at the plant, thinner as it kicks back and out.
        /// Positions come from the puff the game already built. No new forces.
        /// </summary>
        void EmitTrail(Vector3 foot, DustLook.Puff puff, float alpha)
        {
            Transform root = transform.parent;
            Vector3 fwd = root != null ? root.forward : Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f)
                fwd = Vector3.forward;
            else
                fwd.Normalize();
            Vector3 side = root != null ? root.right : Vector3.right;
            side.y = 0f;
            if (side.sqrMagnitude < 0.0001f)
                side = Vector3.right;
            else
                side.Normalize();

            int n = puff.Count;
            float dens = FxAmount.Density(GameSettings.Current);
            if (dens < 0.99f)
            {
                n = (int)(n * dens + 0.001f);
                if (n < 1) n = 1;
            }
            if (n > 16) n = 16;
            // The tuned span stays in DustLook (sprint 72 cm). The cloud draws at half.
            const float vis = 0.5f;
            float span = (puff.Span > 0.05f ? puff.Span : puff.Back) * vis;
            float life = puff.Life > 0.05f ? puff.Life : 0.05f;
            float gap = span / (n > 0 ? n : 1);
            // Dirt's diameter is several times the spacing and fused into one blob.
            // Concrete, grass, and wood stay on the half-size curve.
            bool split = puff.Size * vis > gap * 2.8f;
            Vector3 kick = (-fwd * (puff.Back * vis) + Vector3.up * (puff.Lift * vis * 0.35f)) / life;
            for (int i = 0; i < n; i++)
            {
                float u = n == 1 ? 0f : i / (float)(n - 1);
                float t = Mathf.Pow(u, 1.65f);
                float h1 = TrailHash(i, 1);
                float h2 = TrailHash(i, 2);
                float h3 = TrailHash(i, 3);
                float along = span * (0.02f + 0.96f * t);
                float spread = span * (0.05f + 0.18f * t);
                float sideOff = (h1 - 0.5f) * spread + 0.05f;
                float up = 0.02f + puff.Lift * vis * (0.10f + 0.28f * t) * (0.40f + 0.60f * h2);
                Vector3 pos = foot - fwd * along + side * sideOff + Vector3.up * up;
                // Concrete and wood sprints arch up a little. The span stays put.
                float arch = 0f;
                if (CurlPuff(puff))
                {
                    arch = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
                    pos += Vector3.up * (0.11f * arch) + side * (0.04f * arch);
                }
                if (pos.y < foot.y + 0.018f)
                    pos.y = foot.y + 0.018f;
                float size = puff.Size * vis * (0.55f + 0.45f * t) * (0.78f + 0.44f * h3);
                if (split)
                {
                    float uNext = n == 1 ? 1f : Mathf.Min(1f, (i + 1) / (float)(n - 1));
                    float step = span * 0.96f * Mathf.Max(0.04f, Mathf.Pow(uNext, 1.65f) - t);
                    size = Mathf.Min(size, step * (0.62f + 0.22f * h3));
                }
                if (size < 0.025f) size = 0.025f;
                float fade = 1f - 0.58f * t;
                float a = alpha * fade * (0.82f + 0.18f * h2);
                if (a > 0.95f) a = 0.95f;
                float tint = 0.90f + 0.16f * h1;
                var ep = new ParticleSystem.EmitParams();
                ep.position = pos;
                ep.velocity = kick * (0.35f + 0.25f * h2) + Vector3.up * (arch * 0.55f);
                ep.startSize = size / 0.55f;
                ep.startLifetime = life;
                ep.startColor = new Color(puff.R * tint, puff.G * tint, puff.B * tint, a);
                ep.applyShapeToPosition = false;
                _ps.Emit(ep, 1);
                if (puff.Core > 0.15f && i < 2)
                {
                    ep.position = pos + Vector3.up * 0.012f;
                    ep.startSize = size * 0.62f / 0.55f;
                    ep.startColor = new Color(puff.R * 0.62f, puff.G * 0.62f, puff.B * 0.62f, a > 0.9f ? 0.9f : a);
                    _ps.Emit(ep, 1);
                }
            }
        }

        static bool CurlPuff(DustLook.Puff puff)
        {
            bool grey = puff.B > 0.70f && Mathf.Abs(puff.R - puff.G) < 0.06f && puff.R > 0.70f;
            bool tan = puff.R > 0.84f && puff.G > 0.70f && puff.B < 0.62f && puff.R > puff.G + 0.08f;
            if (grey && puff.Life > 0.42f && puff.Span >= 0.60f && puff.Span < 1.40f)
                return true;
            if (tan && puff.Life > 0.34f && puff.Life < 0.48f && puff.Span >= 0.32f && puff.Span < 0.75f)
                return true;
            return false;
        }

        static float TrailHash(int i, int salt)
        {
            uint x = (uint)i * 374761393u + (uint)salt * 668265263u;
            x = (x ^ (x >> 13)) * 1274126177u;
            return (x & 65535u) / 65535f;
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

        void BuildShapes()
        {
            Shader shader = Shader.Find("Tag/FxKitSprite");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            Mesh quad = ShapeQuad();
            _shapeT = new Transform[ShapeSlots];
            _shapeR = new Renderer[ShapeSlots];
            _shapeM = new Material[ShapeSlots];
            _shapeAge = new float[ShapeSlots];
            _shapeLife = new float[ShapeSlots];
            _shapeV = new Vector3[ShapeSlots];
            for (int i = 0; i < ShapeSlots; i++)
            {
                _shapeAge[i] = -1f;
                _shapeM[i] = ShapeMat(shader);
                _shapeT[i] = ShapeObj("DustShape", quad, _shapeM[i], out _shapeR[i]);
            }
            _markT = new Transform[MarkSlots];
            _markR = new Renderer[MarkSlots];
            _markM = new Material[MarkSlots];
            _markAge = new float[MarkSlots];
            for (int i = 0; i < MarkSlots; i++)
            {
                _markAge[i] = -1f;
                _markM[i] = ShapeMat(shader);
                _markM[i].SetFloat("_Billboard", 0f);
                _markT[i] = ShapeObj("DustMark", quad, _markM[i], out _markR[i]);
            }
        }

        static Material ShapeMat(Shader shader)
        {
            var mat = new Material(shader);
            mat.SetFloat("_Shape", 0f);
            mat.SetFloat("_Billboard", 1f);
            mat.SetFloat("_Guard", 0f);
            mat.SetFloat("_Edge", -1f);
            return mat;
        }

        Transform ShapeObj(string name, Mesh mesh, Material mat, out Renderer rend)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.enabled = false;
            return go.transform;
        }

        static Mesh ShapeQuad()
        {
            var mesh = new Mesh();
            mesh.name = "DustShape";
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            return mesh;
        }

        void EmitShaped(Vector3 worldPos, DustLook.Puff puff)
        {
            if (_shapeT == null) return;
            int n = puff.Count;
            if (n > ShapeSlots) n = ShapeSlots;
            if (n < 1) return;
            Transform root = transform.parent;
            Vector3 fwd = root != null ? root.forward : Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            else fwd.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, fwd);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            else side.Normalize();
            float life = puff.Life > 0.05f ? puff.Life : 0.05f;
            for (int i = 0; i < n; i++)
            {
                int slot = FreeShape();
                float u = n == 1 ? 0.5f : i / (float)(n - 1);
                float along = (u - 0.5f) * (puff.Span > 0.05f ? puff.Span : puff.Size * 2f);
                float lift = 0.04f + (i & 1) * 0.03f;
                Vector3 v = Vector3.up * 0.15f;
                if (puff.Splash != 0) v = Vector3.up * -1.4f;
                else if (puff.Spark != 0) v = side * ((i & 1) == 0 ? 0.8f : -0.8f) + Vector3.up * 0.35f;
                else v = -fwd * 0.4f + Vector3.up * 0.2f;
                ScaleOf(puff.Shape, puff.Size, out float sx, out float sy);
                _shapeAge[slot] = 0.0001f;
                _shapeLife[slot] = life;
                _shapeV[slot] = v;
                _shapeT[slot].position = worldPos - fwd * along + Vector3.up * lift;
                _shapeT[slot].localScale = new Vector3(sx, sy, 1f);
                _shapeM[slot].SetFloat("_Shape", puff.Shape);
                _shapeM[slot].SetFloat("_Billboard", 1f);
                _shapeM[slot].color = new Color(puff.R, puff.G, puff.B, puff.Opacity);
                _shapeR[slot].enabled = true;
            }
        }

        void PlaceMark(Vector3 worldPos, DustLook.Puff puff)
        {
            if (_markT == null) return;
            int slot = 0;
            float oldest = -1f;
            for (int i = 0; i < MarkSlots; i++)
            {
                if (_markAge[i] < 0f)
                {
                    slot = i;
                    oldest = -1f;
                    break;
                }
                if (_markAge[i] > oldest)
                {
                    oldest = _markAge[i];
                    slot = i;
                }
            }
            float shape = puff.Shape;
            if (shape < 0.5f) shape = 6f;
            if (shape > 6.5f && shape < 7.5f) shape = 6f;
            ScaleOf(shape, 0.22f, out float sx, out float sy);
            if (shape > 5.5f && shape < 6.5f)
            {
                sx = 0.55f;
                sy = 0.10f;
            }
            _markAge[slot] = 0.0001f;
            _markT[slot].position = worldPos + Vector3.up * 0.02f;
            _markT[slot].rotation = Quaternion.Euler(90f, 0f, 0f);
            _markT[slot].localScale = new Vector3(sx, sy, 1f);
            _markM[slot].SetFloat("_Shape", shape);
            _markM[slot].SetFloat("_Billboard", 0f);
            float dark = shape > 2.5f && shape < 3.5f ? 1f : 0.72f;
            _markM[slot].color = new Color(puff.R * dark, puff.G * dark, puff.B * dark, 0.9f);
            _markR[slot].enabled = true;
        }

        static void ScaleOf(float shape, float size, out float sx, out float sy)
        {
            sx = size;
            sy = size;
            if (size < 0.04f) size = 0.04f;
            if (shape > 4.5f && shape < 5.5f)
            {
                sx = size * 2.6f;
                sy = size * 0.35f;
            }
            else if (shape > 5.5f && shape < 6.5f)
            {
                sx = size * 1.8f;
                sy = size * 0.42f;
            }
            else if (shape > 3.5f && shape < 4.5f)
            {
                sx = size * 0.28f;
                sy = size * 1.7f;
            }
            else if (shape > 6.5f && shape < 7.5f)
            {
                sx = size * 0.22f;
                sy = size * 0.9f;
            }
            else if (shape > 2.5f && shape < 3.5f)
            {
                sx = size * 0.7f;
                sy = size * 0.55f;
            }
        }

        int FreeShape()
        {
            int slot = 0;
            float oldest = -1f;
            for (int i = 0; i < ShapeSlots; i++)
            {
                if (_shapeAge[i] < 0f) return i;
                if (_shapeAge[i] > oldest)
                {
                    oldest = _shapeAge[i];
                    slot = i;
                }
            }
            return slot;
        }

        void AdvanceShapes()
        {
            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            if (dt > 0.05f) dt = 0.05f;
            if (_shapeAge != null)
            {
                for (int i = 0; i < ShapeSlots; i++)
                {
                    if (_shapeAge[i] < 0f) continue;
                    _shapeAge[i] += dt;
                    if (_shapeAge[i] >= _shapeLife[i])
                    {
                        _shapeAge[i] = -1f;
                        _shapeR[i].enabled = false;
                        continue;
                    }
                    _shapeT[i].position += _shapeV[i] * dt;
                    float a = 1f - _shapeAge[i] / _shapeLife[i];
                    Color c = _shapeM[i].color;
                    c.a = a;
                    _shapeM[i].color = c;
                }
            }
            if (_markAge == null) return;
            for (int i = 0; i < MarkSlots; i++)
            {
                if (_markAge[i] < 0f) continue;
                _markAge[i] += dt;
                if (_markAge[i] >= MarkLife)
                {
                    _markAge[i] = -1f;
                    _markR[i].enabled = false;
                    continue;
                }
                float a = 1f - _markAge[i] / MarkLife;
                Color c = _markM[i].color;
                c.a = a > 0.85f ? 0.9f : a;
                _markM[i].color = c;
            }
        }
    }
}
