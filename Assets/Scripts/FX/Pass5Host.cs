using Tag.Art;
using Tag.Gameplay;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Speed lines, wall-scrape sparks, the It handoff flash, and pad and zip trails.
    /// Pooled. Effects Off and Reduced flashing hide the layer. The motor is not written.
    /// </summary>
    [DefaultExecutionOrder(130)]
    public sealed class Pass5Host : MonoBehaviour
    {
        const int Lines = 8;
        const int Points = 12;

        PlayerMotor _motor;
        ItController _it;
        FxBurstPool _fx;
        int _wallId;
        int _wallSurf = (int)DustLook.Surface.Concrete;
        float _scrapeGap;
        float _lineR = 0.95f;
        float _lineG = 0.28f;
        float _lineB = 0.32f;

        bool _primed;
        bool _wasIt;
        bool _gained;
        float _flashAge = -1f;

        readonly Vector3[] _pad = new Vector3[Points];
        readonly Vector3[] _zip = new Vector3[Points];
        int _padCursor;
        int _padCount;
        int _zipCursor;
        int _zipCount;
        float _padGap;
        float _zipGap;
        float _padFade = -1f;
        float _zipFade = -1f;

        LineRenderer[] _lines;
        LineRenderer _flashRing;
        LineRenderer _padLine;
        LineRenderer _zipLine;
        Transform _flashQuad;
        Material _flashMat;

        public static void Ensure(GameObject host)
        {
            if (host == null) return;
            if (host.GetComponent<Pass5Host>() == null)
                host.AddComponent<Pass5Host>();
        }

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _it = GetComponent<ItController>();
            _fx = FxBurstPool.Ensure(transform);
            int seat = 0;
            string n = gameObject.name;
            if (!string.IsNullOrEmpty(n))
            {
                char c = n[n.Length - 1];
                if (c >= '1' && c <= '4') seat = c - '1';
            }
            VerbFxLook.PlayerColor(seat, out _lineR, out _lineG, out _lineB);
            _lines = new LineRenderer[Lines];
            for (int i = 0; i < Lines; i++)
                _lines[i] = MakeLine("SpeedLine", new Color(0.92f, 0.94f, 1f, 0.8f));
            _flashRing = MakeLine("ItHandoff", new Color(_lineR, _lineG, _lineB, 0.9f));
            _padLine = MakeLine("PadTrail", new Color(0.45f, 0.9f, 1f, 0.85f));
            _zipLine = MakeLine("ZipTrail", new Color(0.82f, 0.45f, 1f, 0.85f));
            _padLine.positionCount = Points;
            _zipLine.positionCount = Points;
            _flashRing.positionCount = 16;
            Shader shader = Shader.Find("Tag/ComicBillboard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var quad = new GameObject("ItHandoffFlash");
            quad.transform.SetParent(transform, false);
            var filter = quad.AddComponent<MeshFilter>();
            filter.sharedMesh = QuadMesh();
            var rend = quad.AddComponent<MeshRenderer>();
            _flashMat = new Material(shader);
            _flashMat.color = new Color(_lineR, _lineG, _lineB, 0f);
            rend.sharedMaterial = _flashMat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            _flashQuad = quad.transform;
            _flashQuad.gameObject.SetActive(false);
        }

        static Mesh QuadMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            return mesh;
        }

        void LateUpdate()
        {
            if (_motor == null) return;
            bool show = FxAmount.Show(GameSettings.Current);
            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            float density = show ? FxAmount.Density(GameSettings.Current) : 0f;
            bool it = _it != null && _it.IsAlive && _it.IsIt;
            if (!_primed)
            {
                _wasIt = it;
                _primed = true;
            }
            if (!show)
            {
                _wasIt = it;
                Hide();
                return;
            }
            TickLines(density);
            TickScrape(dt, density);
            TickFlash(dt, density, it);
            TickTrail(_motor.LaunchArc, ref _padGap, ref _padCursor, ref _padCount, ref _padFade, _pad, _padLine, dt, density, 0.45f, 0.90f, 1f);
            TickTrail(_motor.ZipRiding, ref _zipGap, ref _zipCursor, ref _zipCount, ref _zipFade, _zip, _zipLine, dt, density, 0.82f, 0.45f, 1f);
        }

        void TickLines(float density)
        {
            float speed = _motor.HorizSpeed;
            int n = Pass5Look.SpeedLines(speed, density);
            float len = Pass5Look.LineLength(speed);
            Vector3 v = _motor.Velocity;
            v.y = 0f;
            if (v.sqrMagnitude < 0.04f) v = _motor.transform.forward;
            v.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, v);
            if (side.sqrMagnitude < 0.0001f) side = _motor.transform.right;
            side.Normalize();
            Vector3 body = _motor.transform.position + Vector3.up * 1.05f;
            for (int i = 0; i < Lines; i++)
            {
                bool on = i < n;
                _lines[i].enabled = on;
                if (!on) continue;
                float lat = (i - (n - 1) * 0.5f) * 0.16f;
                float back = 0.25f + (i & 1) * 0.12f;
                Vector3 a = body + side * lat - v * back;
                Vector3 b = a - v * len;
                _lines[i].SetPosition(0, a);
                _lines[i].SetPosition(1, b);
                float aFade = 0.15f + 0.1f * (i & 1);
                _lines[i].startWidth = 0.035f;
                _lines[i].endWidth = 0.01f;
                Color c = new Color(0.92f, 0.95f, 1f, 0.55f);
                _lines[i].startColor = c;
                c.a = aFade;
                _lines[i].endColor = c;
            }
        }

        void TickScrape(float dt, float density)
        {
            MoveState st = _motor.State;
            bool scrape = st == MoveState.WallRun || st == MoveState.WallClimb;
            if (!scrape)
            {
                _scrapeGap = 0f;
                return;
            }
            _wallSurf = DustContact.Read(_motor.WallCollider, ref _wallId, ref _wallSurf);
            int bits = Pass5Look.ScrapeBits(_wallSurf, _motor.HorizSpeed, 1f);
            if (bits <= 0 || _fx == null) return;
            _scrapeGap -= dt;
            float gap = density < 0.99f ? 0.12f : 0.07f;
            if (_scrapeGap > 0f) return;
            _scrapeGap = gap;
            Vector3 n = _motor.WallNormal;
            if (n.sqrMagnitude < 0.0001f) n = -_motor.transform.forward;
            else n.Normalize();
            Vector3 origin = _motor.transform.position - n * 0.32f;
            origin.y += Pass5Look.ScrapeHeight(false);
            DustLook.Puff puff = DustLook.At(_wallSurf, _motor.HorizSpeed, (int)DustLook.Kick.None);
            puff.Count = bits;
            if (Pass5Look.ScrapeSpark(_wallSurf)) puff.Spark = 1;
            if (Pass5Look.ScrapeDrop(_wallSurf)) puff.Splash = 1;
            _fx.PlayShaped(FxBurstKind.WallScuff, origin, puff);
            origin.y = _motor.transform.position.y + Pass5Look.ScrapeHeight(true);
            puff.Count = bits > 2 ? 2 : 1;
            _fx.PlayShaped(FxBurstKind.WallScuff, origin, puff);
        }

        void TickFlash(float dt, float density, bool it)
        {
            if (it != _wasIt)
            {
                _gained = it;
                _flashAge = 0f;
                _wasIt = it;
            }
            if (_flashAge < 0f)
            {
                _flashRing.enabled = false;
                if (_flashQuad != null) _flashQuad.gameObject.SetActive(false);
                return;
            }
            _flashAge += dt;
            float a = Pass5Look.FlashAlpha(_flashAge, density);
            if (a <= 0.001f && _flashAge >= Pass5Look.FlashSeconds)
            {
                _flashAge = -1f;
                _flashRing.enabled = false;
                if (_flashQuad != null) _flashQuad.gameObject.SetActive(false);
                return;
            }
            float span = Pass5Look.FlashSpan(density);
            Vector3 chest = _motor.transform.position + Vector3.up * 1.15f;
            if (_flashQuad != null)
            {
                _flashQuad.gameObject.SetActive(true);
                _flashQuad.position = chest;
                _flashQuad.localScale = new Vector3(span, span, 1f);
                Color c = _gained
                    ? new Color(_lineR, _lineG, _lineB, a)
                    : new Color(0.85f, 0.95f, 1f, a);
                _flashMat.color = c;
            }
            Color ring = _gained
                ? new Color(_lineR, _lineG, _lineB, a)
                : new Color(0.85f, 0.95f, 1f, a);
            PlaceRing(_flashRing, chest, 0.55f + 0.35f * (_flashAge / Pass5Look.FlashSeconds), ring);
        }

        void TickTrail(
            bool on, ref float gap, ref int cursor, ref int count, ref float fade,
            Vector3[] pts, LineRenderer line, float dt, float density,
            float r, float g, float b)
        {
            if (on)
            {
                fade = 0.35f;
                gap -= dt;
                if (count == 0 || gap <= 0f)
                {
                    gap = 0.045f;
                    pts[cursor] = _motor.transform.position + Vector3.up * 0.85f;
                    cursor++;
                    if (cursor >= Points) cursor = 0;
                    if (count < Points) count++;
                }
            }
            else if (fade >= 0f)
            {
                fade -= dt;
                if (fade < 0f)
                {
                    count = 0;
                    cursor = 0;
                    line.enabled = false;
                    return;
                }
            }
            int show = Pass5Look.TrailCount(density);
            if (show > count) show = count;
            if (show < 2)
            {
                line.enabled = false;
                return;
            }
            line.enabled = true;
            float alpha = on ? 0.8f : fade / 0.35f;
            if (alpha < 0f) alpha = 0f;
            Color c = new Color(r, g, b, alpha);
            line.startColor = c;
            line.endColor = new Color(r, g, b, alpha * 0.15f);
            line.startWidth = 0.08f;
            line.endWidth = 0.02f;
            int start = cursor - show;
            if (start < 0) start += Points;
            Vector3 last = pts[start];
            for (int i = 0; i < Points; i++)
            {
                Vector3 p;
                if (i < show)
                {
                    int idx = start + i;
                    if (idx >= Points) idx -= Points;
                    p = pts[idx];
                    last = p;
                }
                else
                    p = last;
                line.SetPosition(i, p);
            }
        }

        void Hide()
        {
            _flashAge = -1f;
            _padFade = -1f;
            _zipFade = -1f;
            _padCount = 0;
            _zipCount = 0;
            if (_lines != null)
            {
                for (int i = 0; i < Lines; i++)
                    _lines[i].enabled = false;
            }
            if (_flashRing != null) _flashRing.enabled = false;
            if (_padLine != null) _padLine.enabled = false;
            if (_zipLine != null) _zipLine.enabled = false;
            if (_flashQuad != null) _flashQuad.gameObject.SetActive(false);
        }

        void PlaceRing(LineRenderer line, Vector3 center, float radius, Color color)
        {
            line.enabled = true;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = 0.06f;
            line.endWidth = 0.06f;
            int seg = line.positionCount;
            if (seg < 2) return;
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)(seg - 1) * 6.2831855f;
                line.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
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
            line.endWidth = 0.02f;
            line.startColor = color;
            line.endColor = color;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            mat.color = color;
            line.material = mat;
            line.enabled = false;
            return line;
        }
    }
}
