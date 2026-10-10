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
            if (d > _maxStep) _maxStep = d;
            _last = p;
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
                for (t = 0f; t < 1.5f; t += Time.deltaTime)
                {
                    Pad(new Vector2(0f, 1f), Vector2.zero, 0, true);
                    worstArm = Mathf.Max(worstArm, Quaternion.Angle(restL, ul.localRotation), Quaternion.Angle(restR, ur.localRotation));
                    Step(me);
                    yield return null;
                }
                entry = HorizSpeed(me);
                Assert.Less(worstArm, 150f, "upper arm left its sane range while sprinting");

                // Slide: speed never rises above the entry speed.
                float slideMax = 0f;
                for (t = 0f; t < 0.8f; t += Time.deltaTime)
                {
                    Pad(new Vector2(0f, 1f), Vector2.zero, GamepadButton.East, true);
                    slideMax = Mathf.Max(slideMax, HorizSpeed(me));
                    Step(me);
                    yield return null;
                }
                Assert.LessOrEqual(slideMax, entry + 0.1f, "slide raised speed above the entry speed");
                Assert.Less(_maxStep, 0.5f, "a frame moved the pawn more than 0.5 m");

                // Tag: the It bot within 1 m transfers It.
                Pad(Vector2.zero, Vector2.zero);
                yield return new WaitForSeconds(0.5f);
                GameObject it = null;
                foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(T("Tag.Gameplay.ItController"), FindObjectsSortMode.None))
                {
                    var c = (Component)o;
                    if (c.gameObject != me && IsIt(c.gameObject)) it = c.gameObject;
                }
                if (it == null && IsIt(me)) Assert.Pass("human started It; transfer covered by the bot path elsewhere");
                Assert.IsNotNull(it, "no It bot");
                bool tagged = false;
                for (t = 0f; t < 6f && !tagged; t += Time.deltaTime)
                {
                    if (Vector3.Distance(it.transform.position, me.transform.position) > 1f)
                    {
                        var cc = it.GetComponent<CharacterController>();
                        if (cc) cc.enabled = false;
                        it.transform.position = me.transform.position + me.transform.forward * 0.8f;
                        if (cc) cc.enabled = true;
                    }
                    tagged = IsIt(me);
                    yield return null;
                }
                Assert.IsTrue(tagged, "the It bot within 1 m never tagged the player");
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
