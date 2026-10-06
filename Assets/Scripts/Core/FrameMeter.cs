namespace Tag.Core
{
    /// <summary>
    /// Live bucket counts for the frame overlay. The headless proof uses the same weights.
    /// Movement, pose, HUD, audio, and round flow add their weight every frame.
    /// AI adds a small tick every physics step and the decide/path/loop weight only on a refresh.
    /// </summary>
    public static class FrameMeter
    {
        public const int BudgetOps = 240;
        public const int MoveOps = 8;
        public const int PoseOps = 6;
        public const int HudOps = 5;
        public const int AudioOps = 2;
        public const int RoundOps = 3;
        public const int AiTickOps = 2;
        public const int AiDecideOps = 14;
        public const int AiPathOps = 11;
        public const int AiLoopOps = 1;

        public static bool Overlay;

        public static int Move;
        public static int Ai;
        public static int Pose;
        public static int Hud;
        public static int Audio;
        public static int Round;

        public static int LastMove;
        public static int LastAi;
        public static int LastPose;
        public static int LastHud;
        public static int LastAudio;
        public static int LastRound;
        public static int LastTotal;
        public static float LastMs;
        public static float Fps;

        public static void AddMove(int n) { Move += n; }
        public static void AddAi(int n) { Ai += n; }
        public static void AddPose(int n) { Pose += n; }
        public static void AddHud(int n) { Hud += n; }
        public static void AddAudio(int n) { Audio += n; }
        public static void AddRound(int n) { Round += n; }

        public static void Close(float unscaledDt)
        {
            LastMove = Move;
            LastAi = Ai;
            LastPose = Pose;
            LastHud = Hud;
            LastAudio = Audio;
            LastRound = Round;
            LastTotal = Move + Ai + Pose + Hud + Audio + Round;
            Move = 0;
            Ai = 0;
            Pose = 0;
            Hud = 0;
            Audio = 0;
            Round = 0;
            if (unscaledDt < 0f) unscaledDt = 0f;
            LastMs = unscaledDt * 1000f;
            float inst = unscaledDt > 0.00001f ? 1f / unscaledDt : 0f;
            Fps = Fps <= 0f ? inst : Fps * 0.9f + inst * 0.1f;
        }

        public static void Discard()
        {
            Move = 0;
            Ai = 0;
            Pose = 0;
            Hud = 0;
            Audio = 0;
            Round = 0;
        }

        public static void ResetStatics()
        {
            Overlay = false;
            Discard();
            LastMove = 0;
            LastAi = 0;
            LastPose = 0;
            LastHud = 0;
            LastAudio = 0;
            LastRound = 0;
            LastTotal = 0;
            LastMs = 0f;
            Fps = 0f;
        }
    }
}
