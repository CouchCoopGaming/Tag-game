using System;

namespace Tag.Level
{
    /// <summary>
    /// Dressed Mega Park districts. Z7 is the kickball field the four split
    /// cameras share. Z1 dresses around the soft-play decks. Z2 dresses the
    /// east lawn of the cling lanes. Placements are real meters, yaw degrees,
    /// scale 1. The headless layout audit still counts the gray solids; play
    /// mode hides only the Z7 infield lumps. Feel locks are not stored here.
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
            // Yaw 180 puts the +X gate on the west sideline, toward the chase.
            // Play disables Col_Gate so that opening is a real entrance.
            new Place("CourtFence", P + "CourtFence.prefab", 88.6f, 0f, 53.2f, 180f),
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
            // Yaw 180 puts the open entry on the west, toward the climb.
            // The west rail at yaw 0 sits in the 30° and 60° arcs.
            new Place("Gazebo", P + "Gazebo.prefab", 77.53f, 0f, 35f, 180f),
            new Place("AC_Gazebo", B + "RooftopAC.prefab", 77.53f, 3.05f, 35f, 0f),

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
            new Place("Tree_N", P + "Tree_Maple.prefab", 85.2f, 0f, 67.9f, 15f),
            new Place("Planter_W", P + "Planter.prefab", 74.4f, 0f, 31.4f, 0f),
            new Place("Planter_N", P + "Planter.prefab", 91.2f, 0f, 67.85f, 0f),
            new Place("Picnic", P + "PicnicTable.prefab", 88.0f, 0f, 68.25f, 90f),
            new Place("ParkLamp", P + "ParkLamp.prefab", 93.4f, 0f, 67.7f, 0f),
            new Place("Shrub", P + "Shrub.prefab", 79.6f, 0f, 31.8f, 0f),
        };

        /// <summary>
        /// Z1 soft-play. Decks, tubes, cubes, the west step, and the rim stay
        /// gray. These props sit around them. Smaller than Z7, and play batches
        /// the group. The landmark pole at (20, 10) and the z=8 loop stay clear.
        /// </summary>
        public static readonly Place[] SoftPlay =
        {
            new Place("Sp_ClimbA", B + "Brick_Wall.prefab", 5.15f, 0f, 23f, 90f),
            new Place("Sp_ClimbB", B + "Brick_Wall.prefab", 5.15f, 0f, 29f, 90f),
            new Place("Sp_Gazebo", P + "Gazebo.prefab", 32.0f, 0f, 15.6f, 0f),
            new Place("Sp_ScaffoldA", S + "Scaffold_Bay.prefab", 30.4f, 0f, 23.5f, 0f),
            new Place("Sp_ScaffoldB", S + "Scaffold_Bay.prefab", 33.15f, 0f, 23.5f, 0f),
            new Place("Sp_Tree", P + "Tree_Maple.prefab", 26.2f, 0f, 14.2f, 0f),
            new Place("Sp_Planter", P + "Planter.prefab", 28.6f, 0f, 14.0f, 0f),
            new Place("Sp_Bench", S + "Bench_Wood.prefab", 24.4f, 0f, 14.6f, 0f),
            new Place("Sp_Trash", S + "TrashCan_Lidded.prefab", 35.0f, 0f, 19.2f, 0f),
            new Place("Sp_Light", S + "LightPost_Single.prefab", 33.6f, 0f, 20.6f, 180f),
            new Place("Sp_Shrub", P + "Shrub.prefab", 23.4f, 0f, 16.2f, 0f),
            new Place("Sp_Picnic", P + "PicnicTable.prefab", 27.2f, 0f, 30.6f, 90f),
        };

        /// <summary>
        /// Z2 cling. The gray faces stay. These props sit on the east lawn,
        /// off the x=8 loop and off the landmark at (15.2, 77.4). Play batches
        /// the group. The gazebo entry faces the brick climb.
        /// </summary>
        public static readonly Place[] Cling =
        {
            new Place("Cl_ClimbA", B + "Brick_Wall.prefab", 9.14f, 0f, 50f, 90f),
            new Place("Cl_ClimbB", B + "Brick_Wall.prefab", 9.14f, 0f, 54f, 90f),
            new Place("Cl_Gazebo", P + "Gazebo.prefab", 14.62f, 0f, 52f, 180f),
            new Place("Cl_AC", B + "RooftopAC.prefab", 14.62f, 3.05f, 52f, 0f),
            new Place("Cl_ScaffoldA", S + "Scaffold_Bay.prefab", 12.80f, 0f, 42.8f, 0f),
            new Place("Cl_ScaffoldB", S + "Scaffold_Bay.prefab", 15.55f, 0f, 42.8f, 0f),
            new Place("Cl_Tree", P + "Tree_Maple.prefab", 12.8f, 0f, 63.5f, 0f),
            new Place("Cl_Planter", P + "Planter.prefab", 15.4f, 0f, 64.2f, 0f),
            new Place("Cl_Bench", S + "Bench_Wood.prefab", 11.2f, 0f, 60.8f, 0f),
            new Place("Cl_Trash", S + "TrashCan_Lidded.prefab", 16.4f, 0f, 61.2f, 0f),
            new Place("Cl_Shrub", P + "Shrub.prefab", 13.6f, 0f, 66.2f, 0f),
            new Place("Cl_Picnic", P + "PicnicTable.prefab", 15.0f, 0f, 68.6f, 90f),
            new Place("Cl_Light", S + "LightPost_Single.prefab", 17.0f, 0f, 66.4f, 180f),
        };

        public static Place[] AllPlaces()
        {
            var all = new Place[Places.Length + SoftPlay.Length + Cling.Length];
            int n = 0;
            for (int i = 0; i < Places.Length; i++)
                all[n++] = Places[i];
            for (int i = 0; i < SoftPlay.Length; i++)
                all[n++] = SoftPlay[i];
            for (int i = 0; i < Cling.Length; i++)
                all[n++] = Cling[i];
            return all;
        }

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
