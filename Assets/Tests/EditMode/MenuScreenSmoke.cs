using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tag.Tests.EditMode
{
    /// <summary>
    /// Every menu screen has to build. The test assembly cannot reference
    /// Assembly-CSharp, so this reaches MenuHost through reflection.
    /// </summary>
    public class MenuScreenSmoke
    {
        [Test]
        public void EveryScreenBuilds()
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
            PropertyInfo screen = hostType.GetProperty("Screen", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(present, "Present");
            Assert.IsNotNull(screen, "Screen");
            int built = 0;
            foreach (object id in Enum.GetValues(idType))
            {
                if (Convert.ToInt32(id) == 0) continue;
                present.Invoke(host, new[] { id });
                object shown = screen.GetValue(host);
                Assert.AreEqual(id, shown, id.ToString());
                built++;
            }
            Assert.Greater(built, 0);
            if (host is Object unity)
                Object.DestroyImmediate(unity.gameObject);
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
