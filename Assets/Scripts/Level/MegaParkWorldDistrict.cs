using System;

namespace Tag.Level
{
    /// <summary>
    /// Pass 1 dresses Z7, the kickball field. That is the ground the four
    /// split cameras share across the open bowl. Placements are real meters,
    /// yaw degrees, scale 1. The headless layout audit still counts the infield
    /// lumps; play mode hides those meshes and colliders so the court is the floor.
    /// Feel locks are not stored here.
    /// </summary>
    public static class MegaParkWorldDistrict
    {
        public const string District = "Z7";

        /// <summary>Gray cubes replaced by the court and the street. Rail, dugout, and cover vaults stay.</summary>
        public static readonly string[] HiddenGraybox =
        {
            "Mound",
            "Base_Home",
            "Base_First",
            "Base_Second",
            "Base_Third",
        };

        public struct Place
        {
            public string Name;
            public string Prefab;
            public float X, Y, Z, Yaw;

            public Place(string name, string prefab, float x, float y, float z, float yaw)
            {
                Name = name;
                Prefab = prefab;
                X = x;
                Y = y;
                Z = z;
                Yaw = yaw;
            }
        }

        const string B = "Assets/Art/Props/Library/Buildings/Prefabs/";
        const string S = "Assets/Art/Props/Library/StreetFurniture/Prefabs/";
        const string R = "Assets/Art/Props/Library/Roads/Prefabs/";
        const string P = "Assets/Art/Props/Library/Park/Prefabs/";

        // Court center (88.6, 53.2). Slab is 12 x 22, so x[82.6, 94.6] z[42.2, 64.2].
        // Street is the east-west two-lane at z=34, south of that slab.
        // Climb walls and the gazebo sit in the southwest pocket, clear of the open rect (z < 40).
        public static readonly Place[] Places =
        {
            new Place("Court", P + "Court.prefab", 88.6f, 0f, 53.2f, 0f),
            new Place("Hoop_S", P + "Hoop.prefab", 88.6f, 0f, 41.0f, 0f),
            new Place("Hoop_N", P + "Hoop.prefab", 88.6f, 0f, 65.4f, 180f),

            new Place("Road_0", S + "StreetRoad_TwoLane.prefab", 84f, 0f, 34f, 90f),
            new Place("Road_1", S + "StreetRoad_TwoLane.prefab", 88f, 0f, 34f, 90f),
            new Place("Road_2", S + "StreetRoad_TwoLane.prefab", 92f, 0f, 34f, 90f),
            new Place("Road_3", S + "StreetRoad_TwoLane.prefab", 96f, 0f, 34f, 90f),
            new Place("Road_4", S + "StreetRoad_TwoLane.prefab", 100f, 0f, 34f, 90f),
            new Place("Road_5", S + "StreetRoad_TwoLane.prefab", 104f, 0f, 34f, 90f),
            new Place("Road_6", S + "StreetRoad_TwoLane.prefab", 108f, 0f, 34f, 90f),
            new Place("Median", S + "StreetMedian_Planted.prefab", 106.2f, 0f, 35.55f, 90f),

            new Place("Walk_0", R + "Sidewalk.prefab", 84f, 0f, 29.6f, 90f),
            new Place("Walk_1", R + "Sidewalk.prefab", 88f, 0f, 29.6f, 90f),
            new Place("Walk_2", R + "Sidewalk.prefab", 92f, 0f, 29.6f, 90f),
            new Place("Walk_3", R + "Sidewalk.prefab", 96f, 0f, 29.6f, 90f),
            new Place("Walk_4", R + "Sidewalk.prefab", 100f, 0f, 29.6f, 90f),

            new Place("Facade_Door", B + "Brick_Door.prefab", 84f, 0f, 28.15f, 0f),
            new Place("Facade_Window", B + "Brick_Window.prefab", 88f, 0f, 28.15f, 0f),
            new Place("Facade_Wall", B + "Brick_Wall.prefab", 92f, 0f, 28.15f, 0f),
            new Place("Facade_WindowB", B + "Brick_Window.prefab", 96f, 0f, 28.15f, 0f),
            new Place("Facade_DoorB", B + "Brick_Door.prefab", 100f, 0f, 28.15f, 0f),
            new Place("AC_Facade", B + "RooftopAC.prefab", 92f, 3.2f, 28.15f, 0f),

            new Place("Climb_A", B + "Brick_Wall.prefab", 71.75f, 0f, 33f, 90f),
            new Place("Climb_B", B + "Brick_Wall.prefab", 71.75f, 0f, 37f, 90f),
            new Place("Gazebo", P + "Gazebo.prefab", 77.78f, 0f, 35f, 0f),
            new Place("AC_Gazebo", B + "RooftopAC.prefab", 77.78f, 3.05f, 35f, 0f),

            new Place("Car_Sedan", S + "Car_Sedan.prefab", 86f, 0.12f, 35.55f, 90f),
            new Place("Car_Hatch", S + "Car_Hatch.prefab", 92f, 0.12f, 35.55f, 90f),
            new Place("Car_Pickup", S + "Car_Pickup.prefab", 98f, 0.12f, 35.55f, 90f),

            new Place("Light_W", S + "LightPost_Single.prefab", 81.0f, 0f, 29.0f, 0f),
            new Place("Light_E", S + "LightPost_Single.prefab", 110.2f, 0f, 30.4f, 180f),
            new Place("Hydrant", S + "FireHydrant.prefab", 83.2f, 0.27f, 29.55f, 0f),
            new Place("Bench_A", S + "Bench_Wood.prefab", 86.6f, 0.27f, 29.85f, 0f),
            new Place("Trash_A", S + "TrashCan_Lidded.prefab", 88.6f, 0.27f, 29.45f, 0f),
            new Place("Bench_B", S + "Bench_Wood.prefab", 95.0f, 0.27f, 29.85f, 0f),
            new Place("Trash_B", S + "TrashCan_Lidded.prefab", 97.2f, 0.27f, 29.45f, 0f),

            new Place("Scaffold_A", S + "Scaffold_Bay.prefab", 100.2f, 0f, 39.6f, 0f),
            new Place("Scaffold_B", S + "Scaffold_Bay.prefab", 102.95f, 0f, 39.6f, 0f),

            new Place("Tree_W", P + "Tree_Maple.prefab", 76.2f, 0f, 30.2f, 0f),
            new Place("Tree_N", P + "Tree_Maple.prefab", 85.2f, 0f, 67.6f, 15f),
            new Place("Planter_W", P + "Planter.prefab", 74.4f, 0f, 31.4f, 0f),
            new Place("Planter_N", P + "Planter.prefab", 91.2f, 0f, 67.55f, 0f),
            new Place("Picnic", P + "PicnicTable.prefab", 88.0f, 0f, 67.7f, 90f),
            new Place("ParkLamp", P + "ParkLamp.prefab", 93.4f, 0f, 67.55f, 0f),
            new Place("Shrub", P + "Shrub.prefab", 79.6f, 0f, 31.8f, 0f),
        };

        public static bool Hides(string solidName)
        {
            for (int i = 0; i < HiddenGraybox.Length; i++)
            {
                if (HiddenGraybox[i] == solidName)
                    return true;
            }
            return false;
        }
    }
}
