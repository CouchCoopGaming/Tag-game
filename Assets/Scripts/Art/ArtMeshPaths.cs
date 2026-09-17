using System.IO;

namespace Tag.Art
{
    /// <summary>
    /// Resolves Landon Art FBX paths. Prefers HiPoly <c>*_Hi.fbx</c> when those
    /// files are on disk (playtest drop). No Resources FBX copies.
    /// </summary>
    public static class ArtMeshPaths
    {
        public const string Characters = "Assets/Art/Characters";
        public const string CharactersHi = "Assets/Art/Characters/HiPoly";
        public const string Props = "Assets/Art/Props/Playground";
        public const string PropsHi = "Assets/Art/Props/Playground/HiPoly";

        public static string PreferCharacterFbx(bool asIt)
        {
            string stem = asIt ? "Dummy_It" : "Dummy_Runner";
            return FirstExisting(
                CharactersHi + "/" + stem + "_Hi.fbx",
                CharactersHi + "/" + stem + ".fbx",
                Characters + "/" + stem + ".fbx");
        }

        public static string PreferPropFbx(string toyName)
        {
            if (string.IsNullOrEmpty(toyName)) return Props + "/Toy_Bench.fbx";
            return FirstExisting(PropCandidates(toyName));
        }

        public static string[] PropCandidates(string toyName)
        {
            if (toyName == "Toy_Slide")
            {
                return new[]
                {
                    PropsHi + "/Toy_Slide_Hi.fbx",
                    PropsHi + "/Toy_Slide_C1_Hi.fbx",
                    PropsHi + "/Toy_RubberTrack_C3_Hi.fbx",
                    PropsHi + "/Toy_Slide.fbx",
                    Props + "/Toy_Slide.fbx"
                };
            }

            return new[]
            {
                PropsHi + "/" + toyName + "_Hi.fbx",
                PropsHi + "/" + toyName + ".fbx",
                Props + "/" + toyName + ".fbx"
            };
        }

        public static string FirstExisting(params string[] paths)
        {
            if (paths == null || paths.Length == 0) return string.Empty;
            for (int i = 0; i < paths.Length; i++)
            {
                if (!string.IsNullOrEmpty(paths[i]) && File.Exists(paths[i]))
                    return paths[i];
            }
            return paths[paths.Length - 1];
        }
    }
}
