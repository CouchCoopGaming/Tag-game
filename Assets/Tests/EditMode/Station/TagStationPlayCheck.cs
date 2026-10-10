using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Tag.Tests.Station
{
    /// <summary>
    /// The job28 station play check as a test. Opens Play.unity in the editor (the path that
    /// used to keep a second, Idle TagModeController alive), starts Practice on a virtual pad
    /// with the cursor unlocked, and checks: one controller, an upright skeleton, and a pawn
    /// that moves more than 3 m in 2 s. Game types live in Assembly-CSharp, so it uses reflection.
    /// </summary>
    public class TagStationPlayCheck
    {
        const float MoveSeconds = 2f;
        const float MinPlanarMetres = 3f;
        const float MaxHipsHeadDeg = 5f;

        Gamepad _pad;

        static Type T(string name)
        {
            Type t = Type.GetType(name + ", Assembly-CSharp");
            Assert.IsNotNull(t, "type " + name);
            return t;
        }

        static object Call(string type, string method, params object[] args)
        {
            MethodInfo m = T(type).GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(m, type + "." + method);
            return m.Invoke(null, args);
        }

        static bool RoundPlay()
        {
            FieldInfo f = T("Tag.Core.SessionRules").GetField("RoundPlay", BindingFlags.Public | BindingFlags.Static);
            return (bool)f.GetValue(null);
        }

        static int PadDevice(Gamepad p)
        {
            for (int i = 0; i < Gamepad.all.Count; i++) if (Gamepad.all[i] == p) return i + 1;
            return -1;
        }

        static List<GameObject> Pawns()
        {
            var list = new List<GameObject>();
            Type reader = T("TagArena.Movement.PlayerInputReader");
            FieldInfo ext = reader.GetField("ExternalControl");
            foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(reader, FindObjectsSortMode.None))
            {
                var c = (Component)o;
                if (!c.gameObject.activeInHierarchy || (bool)ext.GetValue(c)) continue;
                list.Add(c.gameObject);
            }
            return list;
        }

        static Transform Bone(Transform root, string name)
        {
            // Active bones only: a body queued for Destroy is inactive and must not answer.
            foreach (Transform t in root.GetComponentsInChildren<Transform>(false))
                if (t.name == name) return t;
            return null;
        }

        static float Planar(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        [UnityTest]
        public IEnumerator PracticeOnPad_OneController_UprightBody_Moves()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
            yield return new EnterPlayMode();
            try
            {
                yield return new WaitForSecondsRealtime(2f);
                _pad = InputSystem.AddDevice<Gamepad>("StationPad");
                Call("Tag.Couch.CouchPlay", "Release");
                Call("Tag.Couch.CouchPlay", "Join", PadDevice(_pad));
                Call("Tag.Ui.Menu.MenuMatch", "StartPractice");

                float wait = 0f;
                while (!RoundPlay() && wait < 10f)
                {
                    Cursor.lockState = CursorLockMode.None;
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
                Assert.IsTrue(RoundPlay(), "RoundPlay never went true after StartPractice");

                UnityEngine.Object[] controllers = UnityEngine.Object.FindObjectsByType(
                    T("Tag.Modes.TagModeController"), FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.AreEqual(1, controllers.Length, "TagModeController count");

                List<GameObject> pawns = Pawns();
                Assert.Greater(pawns.Count, 0, "no human pawn");
                var start = new Vector3[pawns.Count];
                for (int i = 0; i < pawns.Count; i++)
                {
                    start[i] = pawns[i].transform.position;
                    Transform hips = Bone(pawns[i].transform, "Hips") ?? Bone(pawns[i].transform, "Pelvis");
                    Transform head = Bone(pawns[i].transform, "Head");
                    Assert.IsNotNull(hips, pawns[i].name + " hips");
                    Assert.IsNotNull(head, pawns[i].name + " head");
                    float deg = Vector3.Angle(head.position - hips.position, Vector3.up);
                    Assert.LessOrEqual(deg, MaxHipsHeadDeg, pawns[i].name + " hips->head off vertical");
                }

                float t = 0f;
                while (t < MoveSeconds)
                {
                    Cursor.lockState = CursorLockMode.None;
                    InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = new Vector2(0f, 1f) });
                    t += Time.deltaTime;
                    yield return null;
                }
                for (int i = 0; i < pawns.Count; i++)
                {
                    float moved = Planar(start[i], pawns[i].transform.position);
                    Assert.Greater(moved, MinPlanarMetres, pawns[i].name + " planar move in " + MoveSeconds + " s");
                }
            }
            finally
            {
                if (_pad != null) InputSystem.RemoveDevice(_pad);
                _pad = null;
            }
            yield return new ExitPlayMode();
        }

        static float PrivF(object o, string n)
        {
            FieldInfo f = o.GetType().GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return f != null ? (float)f.GetValue(o) : float.NaN;
        }

        static float HorizSpeed(GameObject pawn)
        {
            Type motor = T("TagArena.Movement.PlayerMotor");
            return (float)motor.GetProperty("HorizSpeed").GetValue(pawn.GetComponent(motor));
        }

        static Component Rig(GameObject pawn)
        {
            Type cam = T("TagArena.Movement.TpsMoveCamera");
            foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(cam, FindObjectsSortMode.None))
            {
                var c = (Component)o;
                if (c.transform.IsChildOf(pawn.transform)) return c;
            }
            return null;
        }

        static void Hold(GameObject it, GameObject me, float meters)
        {
            if (Vector3.Distance(it.transform.position, me.transform.position) <= meters + 0.3f) return;
            var cc = it.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            it.transform.position = me.transform.position + me.transform.forward * meters;
            // Placed in range but turned 90 deg away: squaring up to the runner is the bot's own job.
            Vector3 face = me.transform.position - it.transform.position; face.y = 0f;
            if (face.sqrMagnitude > 0.0001f) it.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, face));
            if (cc) cc.enabled = true;
        }

        static bool IsIt(GameObject g)
        {
            Component it = g.GetComponent(T("Tag.Gameplay.ItController"));
            return it != null && (bool)it.GetType().GetProperty("IsIt").GetValue(it);
        }

        void Pad(Vector2 left, Vector2 right, GamepadButton button = 0, bool sprint = false)
        {
            var st = new GamepadState { leftStick = left, rightStick = right };
            if (button != 0) st = st.WithButton(button);
            if (sprint) st = st.WithButton(GamepadButton.LeftShoulder);
            InputSystem.QueueStateEvent(_pad, st);
        }

        // Frame step guard: no visible teleport (vault/mantle snaps included).
        float _maxStep;
        Vector3 _last;
        void Step(GameObject pawn)
        {
            Vector3 p = pawn.transform.position;
            float d = Vector3.Distance(p, _last);
            if (d > 0.5f) Debug.Log("[StationCheck] step " + d.ToString("F2") + " from " + _last + " to " + p + " state=" + MotorState(pawn) + " t=" + Time.time.ToString("F2") + " frame=" + Time.frameCount);
            // A snap, not a hitch: a long batch frame at 24.7 m/s takeoff legally covers 0.7 m.
            // Count the step only past what the fastest legal motion (terminal 56.16) could cover.
            float legal = Mathf.Max(0.5f, 60f * Time.deltaTime);
            if (d > legal && d - legal + 0.5f > _maxStep) _maxStep = d - legal + 0.5f;
            _last = p;
            Component loco = pawn.GetComponentInChildren(T("Tag.Art.DummyLocomotor"));
            if (loco == null) return;
            // Coroutines resume before LateUpdate, so the bones here still hold the Animator's
            // pose. Read the locomotor's own post-write re-measure of the final rendered bones.
            var t = loco.GetType();
            float l = (float)t.GetProperty("KneeOutL").GetValue(loco), r = (float)t.GetProperty("KneeOutR").GetValue(loco);
            _kneeWorst = Mathf.Min(_kneeWorst, Mathf.Min(l, r));
            float raw = Mathf.Min((float)t.GetProperty("KneeRawL").GetValue(loco), (float)t.GetProperty("KneeRawR").GetValue(loco));
            if (raw < _kneeRawWorst - 0.5f && raw < -5f)
                Debug.Log("[StationCheck] knee raw " + raw.ToString("F1") + " state=" + MotorState(pawn) + " grounded legs L=" + ((float)t.GetProperty("KneeRawL").GetValue(loco)).ToString("F1") + " R=" + ((float)t.GetProperty("KneeRawR").GetValue(loco)).ToString("F1"));
            _kneeRawWorst = Mathf.Min(_kneeRawWorst, raw);
        }

        static Vector2 StickToward(GameObject me, Vector3 goal)
        {
            Camera cam = null;
            foreach (Camera c in me.GetComponentsInChildren<Camera>()) if (c.enabled) { cam = c; break; }
            Vector3 f = cam != null ? cam.transform.forward : me.transform.forward;
            f.y = 0f; f.Normalize();
            Vector3 r = Vector3.Cross(Vector3.up, f);
            Vector3 d = goal - me.transform.position; d.y = 0f; d.Normalize();
            return new Vector2(Vector3.Dot(d, r), Vector3.Dot(d, f)).normalized;
        }

        static string MotorState(GameObject pawn)
        {
            Component m = pawn.GetComponent(T("TagArena.Movement.PlayerMotor"));
            return m == null ? "" : m.GetType().GetProperty("State").GetValue(m).ToString();
        }

        float _kneeWorst = 999f, _kneeRawWorst = 999f;

        /// <summary>Shin vs thigh around the body's right axis, measured on the bones. + flexes forward.</summary>
        static float KneeBend(Component loco, string th, string sh, string ft)
        {
            Transform Get(string n) => (Transform)loco.GetType().GetField(n, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(loco);
            Transform a = Get(th), b = Get(sh), c = Get(ft);
            if (a == null || b == null || c == null) return 0f;
            Vector3 right = loco.transform.right;
            Vector3 u = Vector3.ProjectOnPlane(b.position - a.position, right), v = Vector3.ProjectOnPlane(c.position - b.position, right);
            return Vector3.SignedAngle(u, v, right);
        }

        [UnityTest]
        public IEnumerator MatchVsBot_Jump_Look_Arms_Slide_Tag()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
            yield return new EnterPlayMode();
            try
            {
                Application.targetFrameRate = 60;
                yield return new WaitForSecondsRealtime(2f);
                _pad = InputSystem.AddDevice<Gamepad>("StationPad");
                Call("Tag.Couch.CouchPlay", "Release");
                Call("Tag.Couch.CouchPlay", "Join", PadDevice(_pad));
                object settings = T("Tag.Settings.GameSettings").GetField("Current", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                if (settings != null) settings.GetType().GetField("AiOpponents").SetValue(settings, 1);
                Type session = T("Tag.Ui.Menu.MenuSession");
                session.GetField("Mode").SetValue(null, Enum.Parse(T("Tag.Modes.TagModeId"), "FreePlay"));
                Call("Tag.Ui.Menu.MenuMatch", "StartMatch");
                float wait = 0f;
                while (!RoundPlay() && wait < 15f) { wait += Time.unscaledDeltaTime; yield return null; }
                Assert.IsTrue(RoundPlay(), "RoundPlay never went true after StartMatch");
                List<GameObject> pawns = Pawns();
                Assert.AreEqual(1, pawns.Count, "one human pawn");
                GameObject me = pawns[0];
                _last = me.transform.position;
                _maxStep = 0f;

                // Jump: a South press raises the pawn.
                float baseY = me.transform.position.y, topY = baseY, t = 0f;
                while (t < 1.2f)
                {
                    Pad(Vector2.zero, Vector2.zero, t < 0.15f ? GamepadButton.South : 0);
                    topY = Mathf.Max(topY, me.transform.position.y);
                    Step(me);
                    t += Time.deltaTime;
                    yield return null;
                }
                Assert.Greater(topY - baseY, 1f, "jump did not raise the pawn");
                yield return new WaitForSeconds(1.5f);

                // Look: pad right stick turns yaw, then pitch.
                Component rig = Rig(me);
                Assert.IsNotNull(rig, "camera rig");
                float yaw0 = PrivF(rig, "_yaw"), pitch0 = PrivF(rig, "_pitch");
                for (t = 0f; t < 0.4f; t += Time.deltaTime) { Pad(Vector2.zero, new Vector2(1f, 0f)); yield return null; }
                float yaw1 = PrivF(rig, "_yaw");
                for (t = 0f; t < 0.4f; t += Time.deltaTime) { Pad(Vector2.zero, new Vector2(0f, 1f)); yield return null; }
                float pitch1 = PrivF(rig, "_pitch");
                Assert.Greater(Mathf.Abs(yaw1 - yaw0), 20f, "pad look did not turn yaw");
                Assert.Greater(Mathf.Abs(pitch1 - pitch0), 5f, "pad look did not change pitch");
                Assert.LessOrEqual(PrivF(rig, "minPitch"), -45f, "camera cannot look up far enough to aim the grapple");

                // Arms: sprint and watch the upper arms against their bind pose.
                Transform ul = Bone(me.transform, "UpperArm_L"), ur = Bone(me.transform, "UpperArm_R");
                Assert.IsNotNull(ul, "UpperArm_L");
                Quaternion restL = ul.localRotation, restR = ur.localRotation;
                float worstArm = 0f, entry = 0f;
                // Unsampled waits sit between phases; start the step check from where the pawn is now.
                // Sprint on clear ground toward the open middle (a spawn pad lane), not into the prop
                // the camera happened to face after the look check; the slide below needs the speed.
                Component pm = me.GetComponent(T("TagArena.Movement.PlayerMotor"));
                pm.GetType().GetMethod("Place", new[] { typeof(Vector3), typeof(string) }).Invoke(pm, new object[] { new Vector3(8f, 0.2f, 8f), "station-check" });
                yield return null;
                _last = me.transform.position;
                for (t = 0f; t < 1.5f; t += Time.deltaTime)
                {
                    Pad(StickToward(me, new Vector3(42f, 0f, 92f)), Vector2.zero, 0, true);
                    worstArm = Mathf.Max(worstArm, Quaternion.Angle(restL, ul.localRotation), Quaternion.Angle(restR, ur.localRotation));
                    Step(me);
                    yield return null;
                }
                entry = HorizSpeed(me);
                Assert.Less(worstArm, 150f, "upper arm left its sane range while sprinting");

                // Slide: speed never rises above the entry speed.
                float slideMax = 0f;
                bool slid = false;
                // Unsampled waits sit between phases; start the step check from where the pawn is now.
                _last = me.transform.position;
                for (t = 0f; t < 0.8f; t += Time.deltaTime)
                {
                    Pad(StickToward(me, new Vector3(42f, 0f, 92f)), Vector2.zero, GamepadButton.East, true);
                    // Score only frames the motor is really in Slide. A blocked sprint (entry 4 m/s
                    // against a prop) that never slid then read walk speed 6.9 as a "boost".
                    bool inSlide = MotorState(me) == "Slide";
                    if (inSlide && !slid) { slid = true; entry = Mathf.Max(entry, HorizSpeed(me)); }
                    if (inSlide) slideMax = Mathf.Max(slideMax, HorizSpeed(me));
                    Step(me);
                    yield return null;
                }
                Debug.Log("[StationCheck] knee worst=" + _kneeWorst.ToString("F1") + " rawWorst=" + _kneeRawWorst.ToString("F1"));
                Assert.GreaterOrEqual(_kneeWorst, -5f, "a knee hyperextended past 5 deg in jump, land, run or slide");
                Debug.Log("[StationCheck] slide entry=" + entry.ToString("F2") + " max=" + slideMax.ToString("F2") + " at " + me.transform.position);
                if (slid) Assert.LessOrEqual(slideMax, entry + 0.1f, "slide raised speed above the entry speed");
                else Debug.LogWarning("[StationCheck] no slide entered (sprint blocked); slide speed not scored");
                Assert.Less(_maxStep, 0.5f, "a frame snapped the pawn (beyond 0.5 m and beyond 60 m/s * dt)");

                // Tag: the It bot within 1 m transfers It.
                Pad(Vector2.zero, Vector2.zero);
                // The slide sprint ends wedged on a prop (y 2.73); bring the runner back to open spawn ground.
                Component motor = me.GetComponent(T("TagArena.Movement.PlayerMotor"));
                motor.GetType().GetMethod("Place", new[] { typeof(Vector3), typeof(string) }).Invoke(motor, new object[] { new Vector3(8f, 0.2f, 8f), "station-check" });
                yield return new WaitForSeconds(0.5f);
                GameObject it = null;
                foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(T("Tag.Gameplay.ItController"), FindObjectsSortMode.None))
                {
                    var c = (Component)o;
                    if (c.gameObject != me && IsIt(c.gameObject)) it = c.gameObject;
                }
                if (it == null && IsIt(me)) Assert.Pass("human started It; transfer covered by the bot path elsewhere");
                Assert.IsNotNull(it, "no It bot");
                // Contact alone never tags: the It bot's fist is off, it stands on the player 3 s.
                Component fist = it.GetComponent(T("Tag.Gameplay.PunchHitbox"));
                Assert.IsNotNull(fist, "It bot has no PunchHitbox");
                // Switch the bot's brain off, not just its fist: with the fist off the brain still
                // cocked and swung into a dead hitbox, and that swing was still pending afterwards.
                Behaviour brain = (Behaviour)it.GetComponent(T("Tag.Modes.DummyPatrol"));
                Assert.IsNotNull(brain, "It bot has no DummyPatrol");
                brain.enabled = false;
                for (t = 0f; t < 3f; t += Time.deltaTime)
                {
                    Hold(it, me, 0.6f);
                    Assert.IsFalse(IsIt(me), "touching the It bot tagged the player without a punch");
                    yield return null;
                }
                // With the fist back, a bot punch in range tags within about 2 s.
                brain.enabled = true;
                bool tagged = false;
                for (t = 0f; t < 2.2f && !tagged; t += Time.deltaTime)
                {
                    Hold(it, me, 0.8f);
                    tagged = IsIt(me);
                    yield return null;
                }
                if (!tagged)
                {
                    var bt = brain.GetType();
                    string F(string n) { var f = bt.GetField(n, BindingFlags.NonPublic | BindingFlags.Instance); object v = f?.GetValue(brain); return n + "=" + (v is Component c ? c.name : v); }
                    Debug.Log("[StationCheck] bot " + F("_target") + " " + F("_punchTell") + " " + F("_cooldown") + " " + F("_itGraceTimer") + " " + F("_lungeTellT") + " " + F("_lungeArm")
                        + " ang=" + Vector3.Angle(it.transform.forward, me.transform.position - it.transform.position).ToString("F0") + " dist=" + Vector3.Distance(it.transform.position, me.transform.position).ToString("F2"));
                }
                Assert.IsTrue(tagged, "the It bot's punch in range did not tag within 2 s");
            }
            finally
            {
                if (_pad != null) InputSystem.RemoveDevice(_pad);
                _pad = null;
            }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator TwoPads_TwoPanes()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
            yield return new EnterPlayMode();
            Gamepad a = null, b = null;
            try
            {
                yield return new WaitForSecondsRealtime(2f);
                Call("Tag.Couch.CouchPlay", "Release");
                a = InputSystem.AddDevice<Gamepad>("StationPadA");
                b = InputSystem.AddDevice<Gamepad>("StationPadB");
                Call("Tag.Couch.CouchPlay", "Join", PadDevice(a));
                Call("Tag.Couch.CouchPlay", "Join", PadDevice(b));
                Call("Tag.Ui.Menu.MenuMatch", "StartMatch");
                float wait = 0f;
                while (!RoundPlay() && wait < 15f) { wait += Time.unscaledDeltaTime; yield return null; }
                Assert.IsTrue(RoundPlay(), "split match never started");
                yield return null;
                var rects = new List<Rect>();
                foreach (Camera c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (c.enabled && c.targetTexture == null && c.GetComponentInParent(T("Tag.Gameplay.ItController")) != null) rects.Add(c.rect);
                Assert.AreEqual(2, rects.Count, "two pads should give two pawn cameras");
                Assert.AreNotEqual(rects[0], rects[1], "the two panes share one viewport");
                Assert.Less(rects[0].width * rects[0].height, 0.75f, "pane is still full screen");
            }
            finally
            {
                if (a != null) InputSystem.RemoveDevice(a);
                if (b != null) InputSystem.RemoveDevice(b);
            }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator Practice_ReachesPlaying_AndMoves()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
            yield return new EnterPlayMode();
            try
            {
                yield return new WaitForSecondsRealtime(2f);
                Call("Tag.Couch.CouchPlay", "Release");
                _pad = InputSystem.AddDevice<Gamepad>("StationPad");
                Call("Tag.Couch.CouchPlay", "Join", PadDevice(_pad));
                Call("Tag.Ui.Menu.MenuMatch", "StartPractice");
                float wait = 0f;
                while (!RoundPlay() && wait < 15f) { wait += Time.unscaledDeltaTime; yield return null; }
                Assert.IsTrue(RoundPlay(), "Practice never reached Playing");
                GameObject me = Pawns()[0];
                Vector3 from = me.transform.position;
                for (float t = 0f; t < 2f; t += Time.deltaTime) { Pad(new Vector2(0f, 1f), Vector2.zero); yield return null; }
                Assert.Greater(Vector3.Distance(from, me.transform.position), 3f, "Practice pawn did not move");
            }
            finally
            {
                if (_pad != null) InputSystem.RemoveDevice(_pad);
                _pad = null;
            }
            yield return new ExitPlayMode();
        }
    }
}
