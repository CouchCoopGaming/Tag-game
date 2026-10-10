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
        const float MaxHipsHeadDeg = 10f;

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
    }
}
