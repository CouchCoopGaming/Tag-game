using Tag.Art;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Pooled pad wind, zip sparks, wall scuffs, and sprint wisps.
    /// Landing, dash ghosts, the rope snap, dizzy stars, the tag rim, and wet drips
    /// live on VerbOwnedFx. Reduced flashing and Effects Off hide all of it.
    /// </summary>
    [DefaultExecutionOrder(120)]
    public sealed class VerbFxHost : MonoBehaviour
    {
        const int Streaks = 4;
        const int Wisps = 4;
        const int RingSeg = 20;

        PlayerMotor _motor;
        DummyLocomotor _loco;
        FxBurstPool _fx;
        bool _ready;

        bool _prevLaunch;
        bool _prevZip;
        float _prevSurf;
        int _wallId;
        int _wallSurf = (int)DustLook.Surface.Concrete;
        int _groundId;
        int _groundSurf = (int)DustLook.Surface.Concrete;

        float _padAge = -1f;
        readonly float[] _streakAge = new float[Streaks];
        readonly Vector3[] _streakA = new Vector3[Streaks];
        readonly Vector3[] _streakB = new Vector3[Streaks];
        int _streakCursor;
        readonly Vector3[] _wispPos = new Vector3[Wisps];
        float _wispGap;
        int _wispCount;
        float _zipGap;

        LineRenderer _pad;
        LineRenderer _shimmer;
        LineRenderer[] _streaks;
        Transform[] _wisps;
        Material[] _wispMat;
        Transform _wind;
        Material _windMat;

        public static void Ensure(GameObject host)
        {
            if (host == null) return;
            if (host.GetComponent<VerbFxHost>() == null)
                host.AddComponent<VerbFxHost>();
        }

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _loco = GetComponent<DummyLocomotor>();
            _fx = FxBurstPool.Ensure(transform);
            _pad = MakeLine("PadRing", new Color(0.45f, 0.85f, 1f, 0.9f));
            _shimmer = MakeLine("ZipShimmer", new Color(0.85f, 0.45f, 1f, 0.7f));
            _streaks = new LineRenderer[Streaks];
            for (int i = 0; i < Streaks; i++)
            {
                _streaks[i] = MakeLine("WallStreak", new Color(0.8f, 0.78f, 0.72f, 0.7f));
                _streakAge[i] = -1f;
            }
            Shader shader = Shader.Find("Tag/ComicBillboard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Mesh quad = Quad();
            _wisps = new Transform[Wisps];
            _wispMat = new Material[Wisps];
            for (int i = 0; i < Wisps; i++)
                _wisps[i] = MakeQuad(transform, "SpeedWisp", quad, shader, out _wispMat[i]);
            _wind = MakeQuad(transform, "PadWind", quad, shader, out _windMat);
            _ready = _motor != null && _fx != null;
        }

        void LateUpdate()
        {
            if (!_ready) return;
            bool show = FxAmount.Show(GameSettings.Current);
            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            if (!show)
            {
                HideAll();
                Remember();
                return;
            }
            float speed = _motor.HorizSpeed;
            bool grounded = _motor.Ground.grounded;
            if (grounded)
                _groundSurf = DustContact.Read(_motor.Ground.collider, ref _groundId, ref _groundSurf);
            TickLaunch();
            TickZip(dt, speed);
            TickWall(speed);
            TickWisps(dt, speed);
            FadeRing(_pad, ref _padAge, 0.32f, VerbFxLook.PadRing, dt, 0.45f, 0.85f, 1f);
            FadeStreaks(dt);
            Remember();
        }

        void TickLaunch()
        {
            bool arc = _motor.LaunchArc;
            if (arc && !_prevLaunch)
            {
                _padAge = 0f;
                Vector3 pos = _motor.transform.position;
                DustLook.Puff puff = DustLook.At(_groundSurf, DustLook.Sprint, (int)DustLook.Kick.None);
                puff.Count = 6;
                puff.R = 0.55f;
                puff.G = 0.85f;
                puff.B = 1f;
                _fx.PlayShaped(FxBurstKind.Land, pos, puff);
            }
            bool rise = arc && _motor.Velocity.y > 0.5f;
            _wind.gameObject.SetActive(rise);
            if (rise)
            {
                _wind.position = _motor.transform.position + Vector3.up * 1.05f - _motor.transform.forward * 0.35f;
                float a = 0.35f + 0.25f * Mathf.Sin(Time.time * 20f);
                Color c = _windMat.color;
                c.r = 0.75f;
                c.g = 0.92f;
                c.b = 1f;
                c.a = a;
                _windMat.color = c;
                _wind.localScale = new Vector3(0.12f, 0.7f, 1f);
            }
        }

        void TickZip(float dt, float speed)
        {
            bool ride = _motor.ZipRiding;
            _shimmer.enabled = ride;
            if (!ride) return;
            Vector3 v = _motor.Velocity;
            if (v.sqrMagnitude < 0.01f) v = _motor.transform.forward;
            v.Normalize();
            Vector3 side = Vector3.Cross(v, Vector3.up);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side.Normalize();
            Vector3 origin = _motor.transform.position + Vector3.up * 1.2f;
            int n = 6;
            if (_shimmer.positionCount != n) _shimmer.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float wob = VerbFxLook.ShimmerAmp * Mathf.Sin(Time.time * 16f + t * 8f);
                _shimmer.SetPosition(i, origin + v * ((t - 0.5f) * 1.6f) + side * wob);
            }
            _zipGap -= dt;
            if (_zipGap <= 0f)
            {
                _zipGap = 0.05f;
                DustLook.Puff puff = DustLook.At((int)DustLook.Surface.Metal, speed, (int)DustLook.Kick.Pivot);
                puff.Spark = 1;
                puff.Count = VerbFxLook.ZipSparks(speed, 1f);
                if (puff.Count > 4) puff.Count = 4;
                _fx.PlayShaped(FxBurstKind.WallScuff, origin, puff);
            }
        }

        void TickWall(float speed)
        {
            if (_motor.State == MoveState.WallRun && _loco != null)
            {
                _wallSurf = DustContact.Read(_motor.WallCollider, ref _wallId, ref _wallSurf);
                float surf = _loco.SurfPhase;
                bool left;
                if (DustLook.FootDown(_prevSurf, surf, out left))
                {
                    Vector3 point = _motor.WallPoint;
                    if (point.sqrMagnitude < 0.0001f)
                        point = _motor.transform.position + Vector3.up * 0.5f;
                    Vector3 along = _motor.Velocity;
                    along.y = 0f;
                    if (along.sqrMagnitude < 0.04f) along = _motor.transform.forward;
                    along.Normalize();
                    float len = VerbFxLook.ScuffLength(speed);
                    int slot = _streakCursor % Streaks;
                    _streakCursor++;
                    _streakAge[slot] = 0f;
                    _streakA[slot] = point;
                    _streakB[slot] = point - along * len;
                }
                _prevSurf = surf;
            }
            else if (_loco != null)
                _prevSurf = _loco.SurfPhase;
        }

        void TickWisps(float dt, float speed)
        {
            int want = VerbFxLook.Wisps(speed, FxAmount.Density(GameSettings.Current));
            _wispGap -= dt;
            if (want > 0 && _wispGap <= 0f)
            {
                _wispGap = 0.06f;
                _wispPos[_wispCount % Wisps] = _motor.transform.position + Vector3.up * 0.8f;
                _wispCount++;
            }
            for (int i = 0; i < Wisps; i++)
            {
                bool on = want > 0 && i < want;
                _wisps[i].gameObject.SetActive(on);
                if (!on) continue;
                int idx = (_wispCount - 1 - i) % Wisps;
                if (idx < 0) idx += Wisps;
                _wisps[i].position = _wispPos[idx] - _motor.transform.forward * (0.15f * i);
                float a = 0.45f - i * 0.08f;
                Color c = _wispMat[i].color;
                c.r = 0.9f;
                c.g = 0.92f;
                c.b = 1f;
                c.a = a;
                _wispMat[i].color = c;
                _wisps[i].localScale = new Vector3(0.16f, 0.28f, 1f);
            }
        }

        void FadeRing(LineRenderer line, ref float age, float life, float radius, float dt, float r, float g, float b)
        {
            if (age < 0f)
            {
                line.enabled = false;
                return;
            }
            age += dt;
            if (age >= life)
            {
                age = -1f;
                line.enabled = false;
                return;
            }
            float u = 1f - age / life;
            float rad = radius * (0.7f + 0.5f * (age / life));
            Color c = new Color(r, g, b, 0.8f * u);
            PlaceRing(line, _motor.transform.position + Vector3.up * 0.06f, rad, c);
        }

        void FadeStreaks(float dt)
        {
            for (int i = 0; i < Streaks; i++)
            {
                if (_streakAge[i] < 0f)
                {
                    _streaks[i].enabled = false;
                    continue;
                }
                _streakAge[i] += dt;
                if (_streakAge[i] > 0.28f)
                {
                    _streakAge[i] = -1f;
                    _streaks[i].enabled = false;
                    continue;
                }
                _streaks[i].enabled = true;
                if (_streaks[i].positionCount != 2) _streaks[i].positionCount = 2;
                _streaks[i].SetPosition(0, _streakA[i]);
                _streaks[i].SetPosition(1, _streakB[i]);
                float u = 1f - _streakAge[i] / 0.28f;
                _streaks[i].startWidth = 0.07f * u;
                _streaks[i].endWidth = 0.02f * u;
            }
        }

        void PlaceRing(LineRenderer line, Vector3 center, float radius, Color color)
        {
            line.enabled = true;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = 0.045f;
            line.endWidth = 0.045f;
            if (line.positionCount != RingSeg) line.positionCount = RingSeg;
            for (int i = 0; i < RingSeg; i++)
            {
                float a = i / (float)(RingSeg - 1) * 6.2831855f;
                line.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        void HideAll()
        {
            _padAge = -1f;
            if (_pad != null) _pad.enabled = false;
            if (_shimmer != null) _shimmer.enabled = false;
            if (_wind != null) _wind.gameObject.SetActive(false);
            if (_streaks != null)
            {
                for (int i = 0; i < Streaks; i++)
                {
                    _streakAge[i] = -1f;
                    _streaks[i].enabled = false;
                }
            }
            if (_wisps != null)
            {
                for (int i = 0; i < Wisps; i++)
                    _wisps[i].gameObject.SetActive(false);
            }
        }

        void Remember()
        {
            _prevLaunch = _motor.LaunchArc;
            _prevZip = _motor.ZipRiding;
        }

        LineRenderer MakeLine(string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startWidth = 0.04f;
            line.endWidth = 0.04f;
            line.startColor = color;
            line.endColor = color;
            line.material = LineMat(color);
            line.enabled = false;
            return line;
        }

        static Material LineMat(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        static Transform MakeQuad(Transform parent, string name, Mesh mesh, Shader shader, out Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            mat = new Material(shader);
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            go.SetActive(false);
            return go.transform;
        }

        static Mesh Quad()
        {
            var mesh = new Mesh();
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
    }
}
