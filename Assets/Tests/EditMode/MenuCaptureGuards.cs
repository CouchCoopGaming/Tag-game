using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tag.Tests.EditMode
{
    /// <summary>
    /// Station job 3 showed three bugs. Each one was fixed in 85d7ad3b
    /// ("Make the menu compile on Unity 6000.3 and keep RESULTS and the
    /// main-menu figures in frame"). These tests fail if that fix regresses.
    /// The test assembly cannot reference Assembly-CSharp, so menu types
    /// are reached through reflection.
    /// </summary>
    public class MenuCaptureGuards
    {
        const float UpDegrees = 5f;
        const float MinMeters = 1.6f;
        const float MaxMeters = 2.0f;
        const float PxPerMeter = 100f;

        /// <summary>
        /// Menu figures were lying flat. 85d7ad3b stands a Z-up Hier on Y
        /// in MenuMannequin.StandUp. The root up-vector stays within 5° of world up.
        /// </summary>
        [Test]
        public void FigureStandsUp()
        {
            object host = Open("Main");
            try
            {
                int seen = 0;
                foreach (Transform body in Figures(host))
                {
                    float angle = Vector3.Angle(body.up, Vector3.up);
                    Assert.LessOrEqual(angle, UpDegrees, body.name + " up " + angle.ToString("0.0"));
                    seen++;
                }
                Assert.Greater(seen, 0, "main menu figures");
            }
            finally
            {
                Close(host);
            }
        }

        /// <summary>
        /// RESULTS showed giant capsules. 85d7ad3b dropped the 1400 px wipe
        /// capsules and keeps each figure at about 1.8 m. The on-screen height
        /// of a menu figure, and the results slot at 100 px per metre, stay
        /// between 1.6 m and 2.0 m.
        /// </summary>
        [Test]
        public void FigureHeight()
        {
            object host = Open("Main");
            try
            {
                Camera cam = CameraNamed(host, "PairCam");
                Assert.IsNotNull(cam, "PairCam");
                int seen = 0;
                foreach (Transform body in Figures(host))
                {
                    float meters = ScreenMeters(BoundsOf(body), cam);
                    Assert.GreaterOrEqual(meters, MinMeters, body.name);
                    Assert.LessOrEqual(meters, MaxMeters, body.name);
                    seen++;
                }
                Assert.Greater(seen, 0, "main menu figures");
            }
            finally
            {
                Close(host);
            }

            SetCapture(true);
            host = Open("Results");
            try
            {
                int ranks = 0;
                foreach (Transform body in All(host))
                {
                    if (body.name != "HierRank") continue;
                    RectTransform rt = body as RectTransform;
                    Assert.IsNotNull(rt, "HierRank");
                    float meters = rt.sizeDelta.y / PxPerMeter;
                    Assert.GreaterOrEqual(meters, MinMeters, "results slot");
                    Assert.LessOrEqual(meters, MaxMeters, "results slot");
                    ranks++;
                }
                Assert.Greater(ranks, 0, "results figures");
            }
            finally
            {
                SetCapture(false);
                Close(host);
            }
        }

        /// <summary>
        /// A grey box sat over PLAY. 85d7ad3b keeps preview meshes on layer 31
        /// and the menu canvas in screen space, so a plate cannot cover a button.
        /// An Image drawn above a focusable button, opaque enough to hide it, fails.
        /// </summary>
        [Test]
        public void NoOverlayOnFocus()
        {
            object host = Open("Main");
            try
            {
                Type imageType = Find("UnityEngine.UI.Image");
                Type buttonType = Find("UnityEngine.UI.Button");
                Type textType = Find("UnityEngine.UI.Text");
                Assert.IsNotNull(imageType, "Image");
                Assert.IsNotNull(buttonType, "Button");
                Transform root = HostRoot(host);
                bool play = false;
                int buttons = 0;
                foreach (Component button in root.GetComponentsInChildren(buttonType, false))
                {
                    if (!button.gameObject.activeInHierarchy) continue;
                    PropertyInfo interactable = buttonType.GetProperty("interactable");
                    if (interactable != null && !(bool)interactable.GetValue(button, null)) continue;
                    buttons++;
                    if (Reads(button.transform, textType, "Play")) play = true;
                    RectTransform buttonRt = button.transform as RectTransform;
                    Assert.IsNotNull(buttonRt);
                    Vector3 mid = Center(buttonRt);
                    foreach (Component image in root.GetComponentsInChildren(imageType, false))
                    {
                        if (!image.gameObject.activeInHierarchy) continue;
                        if (image is Behaviour behaviour && !behaviour.enabled) continue;
                        if (image.transform == button.transform || image.transform.IsChildOf(button.transform))
                            continue;
                        if (button.transform.IsChildOf(image.transform)) continue;
                        PropertyInfo color = imageType.GetProperty("color");
                        Color ink = color != null ? (Color)color.GetValue(image, null) : Color.white;
                        if (ink.a < 0.5f) continue;
                        RectTransform imageRt = image.transform as RectTransform;
                        if (imageRt == null) continue;
                        if (!Covers(imageRt, mid)) continue;
                        if (!PaintsOver(image.transform, button.transform)) continue;
                        Assert.Fail(image.name + " covers " + button.name);
                    }
                }
                Assert.Greater(buttons, 0, "focusable buttons");
                Assert.IsTrue(play, "Play");
            }
            finally
            {
                Close(host);
            }
        }

        static object Open(string screen)
        {
            Type hostType = Find("Tag.Ui.Menu.MenuHost");
            Assert.IsNotNull(hostType, "MenuHost");
            Type idType = hostType.Assembly.GetType("Tag.Ui.Menu.MenuScreenId");
            Assert.IsNotNull(idType, "MenuScreenId");
            MethodInfo ensure = hostType.GetMethod("Ensure", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(ensure, "Ensure");
            object host = ensure.Invoke(null, null);
            Assert.IsNotNull(host, "host");
            MethodInfo present = hostType.GetMethod("Present", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(present, "Present");
            object id = Enum.Parse(idType, screen);
            present.Invoke(host, new[] { id });
            return host;
        }

        static void Close(object host)
        {
            Component behaviour = host as Component;
            if (behaviour != null)
                UnityEngine.Object.DestroyImmediate(behaviour.gameObject);
        }

        static void SetCapture(bool on)
        {
            Type cap = Find("Tag.Ui.Menu.MenuCapture");
            Assert.IsNotNull(cap, "MenuCapture");
            FieldInfo run = cap.GetField("_run", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(run, "_run");
            run.SetValue(null, on);
        }

        static Transform HostRoot(object host)
        {
            PropertyInfo transform = host.GetType().GetProperty("transform");
            Assert.IsNotNull(transform, "transform");
            return (Transform)transform.GetValue(host, null);
        }

        static System.Collections.Generic.List<Transform> Figures(object host)
        {
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in All(host))
            {
                if (t.name == "HierPreview" || t.name.StartsWith("DummyVisual", StringComparison.Ordinal))
                    list.Add(t);
            }
            return list;
        }

        static System.Collections.Generic.List<Transform> All(object host)
        {
            var list = new System.Collections.Generic.List<Transform>();
            Transform root = HostRoot(host);
            if (root == null) return list;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                list.Add(t);
            return list;
        }

        static Camera CameraNamed(object host, string name)
        {
            foreach (Transform t in All(host))
            {
                if (t.name != name) continue;
                Camera cam = t.GetComponent<Camera>();
                if (cam != null) return cam;
            }
            return null;
        }

        static Bounds BoundsOf(Transform body)
        {
            Renderer[] rends = body.GetComponentsInChildren<Renderer>(true);
            Bounds b = new Bounds(body.position, Vector3.zero);
            bool any = false;
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null) continue;
                if (!any)
                {
                    b = rends[i].bounds;
                    any = true;
                }
                else
                    b.Encapsulate(rends[i].bounds);
            }
            return b;
        }

        /// <summary>
        /// Height along the menu camera's up axis, in metres. That is the
        /// vertical size on screen. A figure lying flat is short on that axis.
        /// </summary>
        static float ScreenMeters(Bounds b, Camera cam)
        {
            Vector3 up = cam.transform.up;
            Vector3 c = b.center;
            Vector3 e = b.extents;
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        float d = Vector3.Dot(c + new Vector3(x * e.x, y * e.y, z * e.z), up);
                        if (d < min) min = d;
                        if (d > max) max = d;
                    }
                }
            }
            return max - min;
        }

        static bool Reads(Transform root, Type textType, string word)
        {
            if (textType == null || root == null) return false;
            PropertyInfo text = textType.GetProperty("text");
            foreach (Component label in root.GetComponentsInChildren(textType, true))
            {
                string value = text != null ? text.GetValue(label, null) as string : null;
                if (value == word) return true;
            }
            return false;
        }

        static Vector3 Center(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        static bool Covers(RectTransform rt, Vector3 world)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 a = corners[0];
            Vector3 x = corners[3] - corners[0];
            Vector3 y = corners[1] - corners[0];
            float xx = Vector3.Dot(x, x);
            float yy = Vector3.Dot(y, y);
            if (xx < 1e-6f || yy < 1e-6f) return false;
            Vector3 d = world - a;
            float u = Vector3.Dot(d, x) / xx;
            float v = Vector3.Dot(d, y) / yy;
            return u >= 0f && u <= 1f && v >= 0f && v <= 1f;
        }

        static bool PaintsOver(Transform front, Transform back)
        {
            if (front == null || back == null || front == back) return false;
            var chainA = new System.Collections.Generic.List<Transform>();
            var chainB = new System.Collections.Generic.List<Transform>();
            for (Transform t = front; t != null; t = t.parent) chainA.Add(t);
            for (Transform t = back; t != null; t = t.parent) chainB.Add(t);
            chainA.Reverse();
            chainB.Reverse();
            int n = chainA.Count < chainB.Count ? chainA.Count : chainB.Count;
            int i = 0;
            while (i < n && chainA[i] == chainB[i]) i++;
            if (i >= chainA.Count || i >= chainB.Count) return false;
            return chainA[i].GetSiblingIndex() > chainB[i].GetSiblingIndex();
        }

        static Type Find(string name)
        {
            Assembly[] all = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < all.Length; i++)
            {
                Type type = all[i].GetType(name);
                if (type != null) return type;
            }
            return null;
        }
    }
}
