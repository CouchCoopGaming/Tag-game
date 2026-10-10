using System.IO;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Own blob, next to the other settings. Missing file keeps the defaults.
    /// </summary>
    public static class FxKitStore
    {
        public const string FileName = "fx-kit.txt";

        public static void Load()
        {
            string path = PathOf();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try
            {
                FxKitOptions.Read(File.ReadAllText(path));
            }
            catch (IOException)
            {
            }
        }

        public static void Save()
        {
            string path = PathOf();
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                File.WriteAllText(path, FxKitOptions.Write());
            }
            catch (IOException)
            {
            }
        }

        static string PathOf()
        {
            string dir = Application.persistentDataPath;
            if (string.IsNullOrEmpty(dir)) return "";
            return Path.Combine(dir, FileName);
        }
    }
}
