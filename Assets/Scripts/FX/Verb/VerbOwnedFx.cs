using Tag.Art;
using Tag.Experimental;
using Tag.Gameplay;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Landing ring and debris, roll swirl, dash ghosts, hook spark,
    /// release snap, comic dizzy stars, the tag-back rim, and wet drips.
    /// Pad, zip, wisps, and wall scuffs stay on VerbFxHost. Visual only.
    /// </summary>
    [DefaultExecutionOrder(130)]
    public sealed class VerbOwnedFx : MonoBehaviour
    {
        const int Ghosts = 4;
        const int Stars = 3;
        const int RingSeg = 20;

        PlayerMotor _motor;
        DummyLocomotor _loco;
        FxBurstPool _fx;
        ExperimentalGrapple _grapple;
        ItController _it;
        TagRole _role;
        int _seat;
        bool _ready;

        bool _prevGround;
        bool _prevDash;
        bool _prevGrapple;
        float _prevSurf;
        int _wallId;
        int _wallSurf = (int)DustLook.Surface.Concrete;
        int _groundId;
        int _groundSurf = (int)DustLook.Surface.Concrete;
        int _hookId;

        float _ringAge = -1f;
        float _ringR;
        float _swirlAge = -1f;
        float _swirlGap;
        Vector3 _swirlDir;
        Vector3 _swirlPos;
        float _snapAge = -1f;
        Vector3 _snapPos;
        float _ghostGap;
        int _ghostCursor;
        readonly float[] _ghostAge = new float[Ghosts];
        readonly Vector3[] _ghostPos = new Vector3[Ghosts];
        float _colorR = 0.95f;
        float _colorG = 0.28f;
        float _colorB = 0.32f;

        LineRenderer _ring;
        LineRenderer _snap;
        LineRenderer[] _rims;
        Transform[] _ghosts;
        Material[] _ghostMat;
        Transform _dizzy;
        Transform[] _stars;
        RaycastHit _hookHit;

        public static void Ensure(GameObject host)
        {
            if (host == null) return;
            if (host.GetComponent<VerbOwnedFx>() == null)
                host.AddComponent<VerbOwnedFx>();
        }

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _loco = GetComponent<DummyLocomotor>();
            _fx = FxBurstPool.Ensure(transform);
            _grapple = GetComponent<ExperimentalGrapple>();
            _it = GetComponent<ItController>();
            _role = GetComponent<TagRole>();
            CacheSeat();
            VerbFxLook.PlayerColor(_seat, out _colorR, out _colorG, out _colorB);
            _ring = MakeLine("LandRing", new Color(0.85f, 0.72f, 0.48f, 0.85f));
            _snap = MakeLine("RopeSnap", new Color(1f, 0.9f, 0.4f, 0.9f));
            _rims = new LineRenderer[2];
            _rims[0] = MakeLine("TagRimLow", new Color(_colorR, _colorG, _colorB, 0.7f));
            _rims[1] = MakeLine("TagRimHigh", new Color(_colorR, _colorG, _colorB, 0.7f));
            Shader shader = Shader.Find("Tag/ComicBillboard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Mesh quad = Quad();
            Texture2D starTex = LoadStar();
            _ghosts = new Transform[Ghosts];
            _ghostMat = new Material[Ghosts];
            for (int i = 0; i < Ghosts; i++)
            {
                _ghostAge[i] = -1f;
                _ghosts[i] = MakeQuad(transform, "DashGhost", quad, shader, null, out _ghostMat[i]);
            }
            _dizzy = new GameObject("Dizzy").transform;
            _dizzy.SetParent(transform, false);
            _stars = new Transform[Stars];
            for (int i = 0; i < Stars; i++)
            {
                Material mat;
                _stars[i] = MakeQuad(_dizzy, "Star", quad, shader, starTex, out mat);
                mat.color = Color.white;
                mat.SetFloat("_Tilt", (i - 1) * 0.12f);
            }
            _dizzy.gameObject.SetActive(false);
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
            TickLand(grounded, speed, dt);
            TickDash(dt);
            TickGrapple();
            TickStagger(dt);
            TickRim();
            TickDrip();
            FadeRing(dt);
            FadeSnap(dt);
            FadeGhosts(dt);
            Remember();
        }

        void TickLand(bool grounded, float speed, float dt)
        {
            if (grounded && !_prevGround)
            {
                float impact = _motor.LastLandImpactSpeed;
                if (impact >= LandingRollPose.SoftFloor)
                {
                    float tier = LandingRollPose.TierScale(impact);
                    _ringAge = 0f;
                    _ringR = VerbFxLook.LandRing(impact) * (0.55f + 0.45f * tier);
                    Vector3 pos = _motor.transform.position;
                    pos.y += 0.05f;
                    DustLook.Puff puff = DustLook.At(_groundSurf, speed, (int)DustLook.Kick.None);
                    puff.Count = VerbFxLook.Debris(impact, 1f);
                    puff.Size *= 0.75f + 0.5f * tier;
                    _fx.PlayShaped(FxBurstKind.Land, pos, puff);
                    if (VerbFxLook.RollSwirl(impact, speed))
                    {
                        _swirlAge = 0f;
                        _swirlGap = 0f;
                        Vector3 v = _motor.Velocity;
                        v.y = 0f;
                        if (v.sqrMagnitude < 0.25f) v = _motor.transform.forward;
                        _swirlDir = v.normalized;
                        _swirlPos = pos;
                    }
                }
            }
            if (_swirlAge >= 0f)
            {
                _swirlAge += dt;
                _swirlGap -= dt;
                if (_swirlAge >= LandingRollPose.Seconds)
                    _swirlAge = -1f;
                else if (_swirlGap <= 0f)
                {
                    _swirlGap = 0.06f;
                    Vector3 p = _swirlPos + _swirlDir * (_swirlAge * 2.2f);
                    DustLook.Puff swirl = DustLook.At(_groundSurf, DustLook.Sprint, (int)DustLook.Kick.Trail);
                    swirl.Count = 2;
                    _fx.PlayShaped(FxBurstKind.Roll, p, swirl);
                }
            }
        }

        void TickDash(float dt)
        {
            bool dash = _motor.IsAirDashing;
            int cap = VerbFxLook.Ghosts(FxAmount.Density(GameSettings.Current));
            if (dash && !_prevDash)
            {
                _ghostGap = 0f;
                _ghostCursor = 0;
                if (cap > 0)
                    DropGhost(_motor.transform.position + Vector3.up * 0.9f);
            }
            else if (dash)
            {
                _ghostGap += dt;
                float step = VerbFxLook.DashSeconds / VerbFxLook.GhostsFull;
                if (_ghostGap >= step && _ghostCursor < cap)
                {
                    _ghostGap = 0f;
                    DropGhost(_motor.transform.position + Vector3.up * 0.9f);
                }
            }
        }

        void TickGrapple()
        {
            bool on = _grapple != null && _grapple.IsPulling;
            if (on && !_prevGrapple)
            {
                Vector3 anchor;
                float length;
                float slack;
                if (_grapple.TryGetRope(out anchor, out length, out slack))
                {
                    int surf = _groundSurf;
                    if (Physics.Raycast(anchor + Vector3.up * 0.2f, Vector3.down, out _hookHit, 1.2f))
                        surf = DustContact.Read(_hookHit.collider, ref _hookId, ref surf);
                    DustLook.Puff puff = DustLook.At(surf, DustLook.Sprint, (int)DustLook.Kick.Pivot);
                    puff.Count = VerbFxLook.HookBits(surf, true);
                    if (surf == (int)DustLook.Surface.Metal) puff.Spark = 1;
                    _fx.PlayShaped(FxBurstKind.VaultPuff, anchor, puff);
                }
            }
            if (!on && _prevGrapple)
            {
                _snapAge = 0f;
                _snapPos = _motor.transform.position + Vector3.up * 1.1f;
                DustLook.Puff puff = DustLook.At(_groundSurf, DustLook.Walk, (int)DustLook.Kick.None);
                puff.Count = 4;
                puff.Life = VerbFxLook.ReleaseSeconds;
                _fx.PlayShaped(FxBurstKind.VaultPuff, _snapPos, puff);
            }
        }

        void TickStagger(float dt)
        {
            bool on = _motor.IsPunchStaggered;
            _dizzy.gameObject.SetActive(on);
            if (!on) return;
            _dizzy.position = _motor.transform.position + Vector3.up * 1.85f;
            _dizzy.Rotate(0f, 140f * dt, 0f, Space.World);
            float spin = Time.time * 3f;
            for (int i = 0; i < Stars; i++)
            {
                float a = spin + i * 2.094f;
                _stars[i].localPosition = new Vector3(Mathf.Cos(a) * 0.32f, Mathf.Sin(a) * 0.12f, 0f);
                _stars[i].localScale = new Vector3(0.42f, 0.42f, 1f);
            }
        }

        void TickRim()
        {
            float left = 0f;
            if (_it != null && _it.TagBackRemaining > left) left = _it.TagBackRemaining;
            if (_role != null && _role.TagBackRemaining > left) left = _role.TagBackRemaining;
            float pulse = VerbFxLook.Rim(left, TagBackImmunity.DefaultSeconds, Time.time);
            bool on = pulse > 0.02f;
            _rims[0].enabled = on;
            _rims[1].enabled = on;
            if (!on) return;
            float radius = 0.46f + 0.06f * pulse;
            Color c = new Color(_colorR, _colorG, _colorB, 0.25f + 0.55f * pulse);
            PlaceRing(_rims[0], _motor.transform.position + Vector3.up * (0.7f + 0.04f * pulse), radius, c);
            PlaceRing(_rims[1], _motor.transform.position + Vector3.up * (1.25f + 0.04f * pulse), radius * 0.92f, c);
        }

        void TickDrip()
        {
            if (_loco == null) return;
            if (_motor.State == MoveState.WallRun)
            {
                float surf = _loco.SurfPhase;
                bool left;
                if (DustLook.FootDown(_prevSurf, surf, out left))
                {
                    _wallSurf = DustContact.Read(_motor.WallCollider, ref _wallId, ref _wallSurf);
                    if (VerbFxLook.Drip(_wallSurf))
                    {
                        Vector3 point = _motor.WallPoint;
                        if (point.sqrMagnitude < 0.0001f)
                            point = _motor.transform.position + Vector3.up * 0.5f;
                        DustLook.Puff puff = DustLook.At(_wallSurf, _motor.HorizSpeed, (int)DustLook.Kick.None);
                        puff.Count = 3;
                        _fx.PlayShaped(FxBurstKind.WallScuff, point, puff);
                    }
                }
                _prevSurf = surf;
            }
            else
                _prevSurf = _loco.SurfPhase;
        }

        void DropGhost(Vector3 pos)
        {
            int slot = _ghostCursor % Ghosts;
            _ghostCursor++;
            _ghostAge[slot] = 0f;
            _ghostPos[slot] = pos;
        }

        void FadeGhosts(float dt)
        {
            for (int i = 0; i < Ghosts; i++)
            {
                if (_ghostAge[i] < 0f)
                {
                    _ghosts[i].gameObject.SetActive(false);
                    continue;
                }
                _ghostAge[i] += dt;
                if (_ghostAge[i] >= VerbFxLook.GhostLife)
                {
                    _ghostAge[i] = -1f;
                    _ghosts[i].gameObject.SetActive(false);
                    continue;
                }
                _ghosts[i].gameObject.SetActive(true);
                _ghosts[i].position = _ghostPos[i];
                float u = 1f - _ghostAge[i] / VerbFxLook.GhostLife;
                Color c = _ghostMat[i].color;
                c.r = _colorR;
                c.g = _colorG;
                c.b = _colorB;
                c.a = 0.55f * u;
                _ghostMat[i].color = c;
                _ghosts[i].localScale = new Vector3(0.42f, 0.9f, 1f);
            }
        }

        void FadeRing(float dt)
        {
            if (_ringAge < 0f)
            {
                _ring.enabled = false;
                return;
            }
            _ringAge += dt;
            if (_ringAge >= 0.36f)
            {
                _ringAge = -1f;
                _ring.enabled = false;
                return;
            }
            float u = 1f - _ringAge / 0.36f;
            float rad = _ringR * (0.7f + 0.5f * (_ringAge / 0.36f));
            Color c = new Color(0.85f, 0.72f, 0.48f, 0.8f * u);
            PlaceRing(_ring, _motor.transform.position + Vector3.up * 0.06f, rad, c);
        }

        void FadeSnap(float dt)
        {
            if (_snapAge < 0f)
            {
                _snap.enabled = false;
                return;
            }
            _snapAge += dt;
            if (_snapAge >= VerbFxLook.ReleaseSeconds)
            {
                _snapAge = -1f;
                _snap.enabled = false;
                return;
            }
            float u = 1f - _snapAge / VerbFxLook.ReleaseSeconds;
            _snap.enabled = true;
            if (_snap.positionCount != 2) _snap.positionCount = 2;
            _snap.SetPosition(0, _snapPos);
            _snap.SetPosition(1, _snapPos + Vector3.up * (0.15f + u * 0.45f));
            _snap.startWidth = 0.08f * u;
            _snap.endWidth = 0.01f;
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
            _ringAge = -1f;
            _swirlAge = -1f;
            _snapAge = -1f;
            if (_ring != null) _ring.enabled = false;
            if (_snap != null) _snap.enabled = false;
            if (_dizzy != null) _dizzy.gameObject.SetActive(false);
            if (_rims != null)
            {
                _rims[0].enabled = false;
                _rims[1].enabled = false;
            }
            if (_ghosts != null)
            {
                for (int i = 0; i < Ghosts; i++)
                {
                    _ghostAge[i] = -1f;
                    _ghosts[i].gameObject.SetActive(false);
                }
            }
        }

        void Remember()
        {
            _prevGround = _motor.Ground.grounded;
            _prevDash = _motor.IsAirDashing;
            _prevGrapple = _grapple != null && _grapple.IsPulling;
        }

        void CacheSeat()
        {
            _seat = 0;
            string n = gameObject.name;
            if (string.IsNullOrEmpty(n)) return;
            char c = n[n.Length - 1];
            if (c >= '1' && c <= '4') _seat = c - '1';
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

        static Transform MakeQuad(Transform parent, string name, Mesh mesh, Shader shader, Texture2D tex, out Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            mat = new Material(shader);
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }
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

        static Texture2D LoadStar()
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(ComicDizzy.Png());
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
    }
}
