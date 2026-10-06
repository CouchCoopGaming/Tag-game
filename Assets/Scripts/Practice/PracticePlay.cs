using System.Text;
using Tag.Core;
using Tag.Local;
using Tag.Modes;
using Tag.Onboard;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tag.Practice
{
    /// <summary>
    /// Live practice trial. Recording writes the buffers allocated in PracticeGhost.
    /// The figure is a mesh. Restart reuses it.
    /// </summary>
    public static class PracticeRuntime
    {
        static readonly float[] PbSplits = new float[PracticeBests.Splits];
        static readonly float[] Splits = new float[PracticeBests.Splits];
        static readonly string[] SplitHud = new string[PracticeBests.Splits];
        static readonly StringBuilder Sb = new StringBuilder(64);

        static PlayerMotor _motor;
        static PracticeRoute _route;
        static bool _booted;
        static bool _placed;
        static bool _done;
        static bool _saved;
        static float _time;
        static float _punchShow;
        static int _next;
        static int _splitN;
        static int _pbN;
        static float _pbTime;
        static int _mask;

        public static float TimeSeconds => _time;
        public static int SplitCount => _splitN;
        public static string SplitLine(int index)
        {
            if (index < 0 || index >= SplitHud.Length) return " ";
            return SplitHud[index] ?? " ";
        }

        public static int InputMask => _mask;
        public static bool Running => _placed && !_done;
        public static float Best => _pbTime;

        public static void Tick()
        {
            if (!PracticeSession.Active)
            {
                if (_booted)
                {
                    _booted = false;
                    _motor = null;
                    PracticeArena.Restore();
                    PracticeGhostView.Hide();
                }
                return;
            }

            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            if (BindSampler.PracticeRestartDown())
                Restart();
            if (BindSampler.PracticeGhostDown())
                PracticeSession.GhostOn = !PracticeSession.GhostOn;
            if (BindSampler.PracticeInputDown())
                PracticeSession.InputsOn = !PracticeSession.InputsOn;

            if (!_booted)
            {
                _booted = true;
                if (PracticeCatalog.Count < 1)
                    PracticeCatalog.Load();
                BeginRun();
                PracticeArena.Apply();
            }

            BindMotor();
            var modes = TagModeController.Instance;
            if (modes == null) return;
            if (modes.Phase == MatchPhase.Playing && !_placed)
                PlaceNow();
            if (!_placed) return;

            if (_punchShow > 0f) _punchShow -= dt;
            if (BindSampler.Pressed(PlayAction.Punch)) _punchShow = 0.12f;
            bool dash = BindSampler.Held(PlayAction.AirDash) || BindSampler.Pressed(PlayAction.AirDash);
            bool cling = BindSampler.Held(PlayAction.Cling);
            if (_motor != null)
            {
                if (_motor.IsAirDashing) dash = true;
                if (_motor.ClingHeldActive) cling = true;
            }
            _mask = PracticeInput.Mask(
                BindSampler.Held(PlayAction.Jump) || BindSampler.Pressed(PlayAction.Jump),
                BindSampler.Held(PlayAction.Slide),
                dash,
                _punchShow > 0f || BindSampler.Held(PlayAction.Punch),
                BindSampler.Held(PlayAction.Sprint),
                cling);

            if (_done || _route == null)
            {
                PracticeGhostView.Sync(PracticeSession.RouteId, _time);
                return;
            }

            _time += dt;
            Vector3 feet = _motor != null ? _motor.transform.position : Vector3.zero;
            Vector3 body = feet + Vector3.up * 0.9f;
            byte pose = PoseOf();
            float yaw = _motor != null ? _motor.transform.eulerAngles.y : 0f;
            PracticeGhost.Offer(dt, feet.x, feet.y, feet.z, yaw, pose);
            NoteGates(body);
            PracticeGhostView.Sync(PracticeSession.RouteId, _time);
        }

        static void Restart()
        {
            PracticeSession.RestartRun();
            BeginRun();
            var modes = TagModeController.Instance;
            if (modes != null && modes.Phase == MatchPhase.Playing)
                PlaceNow();
        }

        static void BeginRun()
        {
            _time = 0f;
            _next = 1;
            _splitN = 0;
            _done = false;
            _saved = false;
            _placed = false;
            _punchShow = 0f;
            for (int i = 0; i < SplitHud.Length; i++)
                SplitHud[i] = " ";
            _route = string.IsNullOrEmpty(PracticeSession.RouteId)
                ? null
                : PracticeCatalog.ById(PracticeSession.RouteId);
            _pbTime = _route == null ? 0f : PracticeBests.TimeOf(_route.Id);
            _pbN = _route == null ? 0 : PracticeBests.SplitsOf(_route.Id);
            for (int i = 0; i < PbSplits.Length; i++)
                PbSplits[i] = _route == null ? 0f : PracticeBests.SplitOf(_route.Id, i);
        }

        static void PlaceNow()
        {
            _placed = true;
            _time = 0f;
            if (_motor == null || _route == null || _route.Gates == null || _route.Gates.Length < 1)
                return;
            PracticeGate start = _route.Gates[0];
            _motor.Place(new Vector3(start.X, 0.2f, start.Z));
        }

        static void NoteGates(Vector3 body)
        {
            if (_route.Gates == null) return;
            while (_next < _route.Gates.Length)
            {
                PracticeGate gate = _route.Gates[_next];
                if (!PracticeCatalog.Hit(gate, body.x, body.y, body.z)) return;
                if (_splitN < Splits.Length)
                {
                    Splits[_splitN] = _time;
                    float pb = _splitN < _pbN ? PbSplits[_splitN] : 0f;
                    SplitHud[_splitN] = FormatSplit(_splitN + 1, _time, pb);
                    _splitN++;
                }
                _next++;
            }
            _done = true;
            if (_saved || _route.Id.Length == 0) return;
            _saved = true;
            if (_pbTime > 0f && _time >= _pbTime) return;
            PracticeBests.Set(_route.Id, _time, Splits, _splitN);
            PracticeGhost.Keep(_route.Id);
            SettingsRuntime.Save();
        }

        static string FormatSplit(int index, float time, float pb)
        {
            Sb.Length = 0;
            Sb.Append("CP ");
            Sb.Append(HudDigits.Whole0(index));
            Sb.Append(' ');
            Sb.Append(HudDigits.TenthSeconds(time));
            if (pb > 0f)
            {
                float d = time - pb;
                Sb.Append(d <= 0f ? "  -" : "  +");
                float mag = d < 0f ? -d : d;
                Sb.Append(HudDigits.TenthSeconds(mag));
            }
            return Sb.ToString();
        }

        static byte PoseOf()
        {
            if (_motor == null) return PracticeVerb.None;
            if (_motor.ZipRiding) return PracticeVerb.Zip;
            if (_motor.IsWallRunning || _motor.State == MoveState.WallClimb) return PracticeVerb.WallRun;
            if (_motor.IsAirDashing) return PracticeVerb.AirDash;
            if (_motor.LaunchArc) return PracticeVerb.Pad;
            if (_motor.State == MoveState.Slide || _motor.State == MoveState.Crouch) return PracticeVerb.Slide;
            if (_motor.State == MoveState.Air) return PracticeVerb.Jump;
            if (_motor.State == MoveState.Sprint) return PracticeVerb.Sprint;
            return PracticeVerb.None;
        }

        static void BindMotor()
        {
            if (_motor != null) return;
            GameObject go = GameObject.Find(LocalPlayerSpawner.SoloPawnName);
            if (go == null) return;
            _motor = go.GetComponent<PlayerMotor>();
        }
    }

    public static class PracticeHud
    {
        static readonly Rect Caption = new Rect(16f, 16f, 220f, 22f);
        static readonly Rect TimeValue = new Rect(16f, 36f, 120f, 22f);
        static readonly Rect BestValue = new Rect(140f, 36f, 160f, 22f);

        public static void Draw()
        {
            if (!PracticeSession.Active) return;
            GUI.Label(Caption, "Practice");
            if (!string.IsNullOrEmpty(PracticeSession.RouteId))
            {
                GUI.Label(TimeValue, HudDigits.TenthSeconds(PracticeRuntime.TimeSeconds));
                if (PracticeRuntime.Best > 0f)
                    GUI.Label(BestValue, HudDigits.TenthSeconds(PracticeRuntime.Best));
            }
            float y = 58f;
            int n = PracticeRuntime.SplitCount;
            for (int i = 0; i < n; i++)
            {
                GUI.Label(new Rect(16f, y, 280f, 20f), PracticeRuntime.SplitLine(i));
                y += 18f;
            }
            if (!PracticeSession.InputsOn) return;
            GUI.Label(new Rect(16f, y + 4f, 420f, 22f), PracticeInput.Line(PracticeRuntime.InputMask));
        }
    }

    public static class PracticeGhostView
    {
        static GameObject _figure;
        static Transform _chest;
        static Renderer _renderer;
        static bool _built;

        public static void Hide()
        {
            if (_renderer != null) _renderer.enabled = false;
        }

        public static void Sync(string id, float time)
        {
            if (!PracticeSession.GhostOn || string.IsNullOrEmpty(id) || !PracticeGhost.HasReplay(id))
            {
                Hide();
                return;
            }
            if (!PracticeGhost.Replay(id, time, out float x, out float y, out float z, out float yaw, out byte pose))
            {
                Hide();
                return;
            }
            Ensure();
            if (_figure == null) return;
            _figure.transform.position = new Vector3(x, y, z);
            _figure.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (_renderer != null) _renderer.enabled = true;
            if (_chest != null)
            {
                float speed = 0f;
                float w = PracticePose.Weight(pose, 0.08f, 0f, speed);
                _chest.localRotation = Quaternion.Euler(w * 18f, 0f, 0f);
            }
        }

        static void Ensure()
        {
            if (_built && _figure != null) return;
            _built = true;
            _figure = new GameObject("PracticeGhost");
            var filter = _figure.AddComponent<MeshFilter>();
            var renderer = _figure.AddComponent<MeshRenderer>();
            filter.sharedMesh = Box();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.65f, 0.88f, 1f, 0.4f);
                renderer.sharedMaterial = mat;
            }
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer = renderer;
            var chest = new GameObject("Chest");
            chest.transform.SetParent(_figure.transform, false);
            chest.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            _chest = chest.transform;
        }

        static Mesh Box()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-0.3f, 0f, -0.2f),
                new Vector3(0.3f, 0f, -0.2f),
                new Vector3(0.3f, 1.8f, -0.2f),
                new Vector3(-0.3f, 1.8f, -0.2f),
                new Vector3(-0.3f, 0f, 0.2f),
                new Vector3(0.3f, 0f, 0.2f),
                new Vector3(0.3f, 1.8f, 0.2f),
                new Vector3(-0.3f, 1.8f, 0.2f)
            };
            mesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                3, 6, 2, 3, 7, 6,
                1, 2, 6, 1, 6, 5,
                0, 4, 7, 0, 7, 3
            };
            return mesh;
        }
    }

    public static class PracticeArena
    {
        static bool _applied;

        public static void Apply()
        {
            if (_applied) return;
            string want = PracticeSession.RootName();
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null || !IsArena(root.name)) continue;
                root.SetActive(root.name == want);
            }
            _applied = true;
        }

        public static void Restore()
        {
            if (!_applied) return;
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null || !IsArena(root.name)) continue;
                root.SetActive(true);
            }
            _applied = false;
        }

        static bool IsArena(string name)
        {
            for (int i = 0; i < ArenaRegistry.Count; i++)
            {
                if (ArenaRegistry.All[i].Root == name) return true;
            }
            return false;
        }
    }
}
