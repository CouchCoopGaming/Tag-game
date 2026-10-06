using Tag.Core;
using Tag.Couch;
using Tag.Practice;
using Tag.Profiles;
using UnityEngine;

namespace Tag.Front
{
    /// <summary>
    /// Title and match setup. Same box-and-highlight rows as the pause card.
    /// Strings are rebuilt when the selection changes, not every repaint.
    /// </summary>
    public static class FrontEndView
    {
        public static void Draw()
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            if (FrontSession.Screen == FrontScreen.Join)
                DrawJoin(cx, cy);
            else if (FrontSession.Screen == FrontScreen.Setup)
                DrawSetup(cx, cy);
            else if (FrontSession.Screen == FrontScreen.Practice)
                DrawPractice(cx, cy);
            else
                DrawTitle(cx, cy);
        }

        static void DrawTitle(float cx, float cy)
        {
            GUI.Box(new Rect(cx - 210f, cy - 180f, 420f, 400f), FrontSession.GameName);
            GUI.Label(new Rect(cx - 190f, cy - 144f, 380f, 28f), "Least It");
            float y = cy - 108f;
            for (int i = 0; i < 5; i++)
            {
                if (Button(cx, y, i, FrontSession.RowText(i)))
                    FrontSession.Pointer(i);
                y += 36f;
            }
            GUI.Label(new Rect(cx - 190f, cy + 100f, 380f, 48f),
                "Up / Down or the stick picks. Enter or South uses it.\nEsc or East goes back.");
        }

        static void DrawPractice(float cx, float cy)
        {
            GUI.Box(new Rect(cx - 240f, cy - 180f, 480f, 400f), "Practice");
            float y = cy - 140f;
            for (int i = 0; i < PracticeSession.Rows; i++)
            {
                if (Button(cx, y, i, FrontSession.RowText(i)))
                    FrontSession.Pointer(i);
                y += 32f;
            }
            GUI.Label(new Rect(cx - 220f, cy + 120f, 440f, 48f),
                "Left / Right changes the row. Enter or South starts.\nEsc or East returns to the title.");
        }

        static void DrawSetup(float cx, float cy)
        {
            GUI.Box(new Rect(cx - 240f, cy - 180f, 480f, 400f), "Match setup");
            float y = cy - 140f;
            for (int i = 0; i < 7; i++)
            {
                if (Button(cx, y, i, FrontSession.RowText(i)))
                    FrontSession.Pointer(i);
                y += 32f;
            }
            GUI.Label(new Rect(cx - 220f, cy + 120f, 440f, 48f),
                "Up / Down picks. Left / Right changes the row.\nEnter or South starts. Esc or East returns to the title.");
        }

        static void DrawJoin(float cx, float cy)
        {
            GUI.Box(new Rect(cx - 240f, cy - 200f, 480f, 440f), "Join");
            float y = cy - 160f;
            for (int i = 0; i < 4; i++)
            {
                if (MenuClick.Button(new Rect(cx - 200f, y, 400f, 22f), CouchPlay.SeatLine(i)))
                    CouchPlay.CycleProfile(i, 1);
                y += 22f;
            }
            string card = LocalProfiles.FirstCard();
            if (!string.IsNullOrEmpty(card))
                GUI.Label(new Rect(cx - 200f, y, 400f, 72f), card);
            y += 8f;
            for (int i = 0; i < 4; i++)
            {
                if (Button(cx, y, i, FrontSession.RowText(i)))
                    FrontSession.Pointer(i);
                y += 32f;
            }
            GUI.Label(new Rect(cx - 220f, cy + 150f, 440f, 48f),
                "A key joins the keyboard. Any pad button joins that pad.\nBack leaves. Enter or South starts.\nA seat picks a profile or Guest. Names stay apart.");
        }

        static bool Button(float cx, float y, int index, string label)
        {
            var r = new Rect(cx - 170f, y, 340f, 28f);
            bool sel = FrontSession.Row == index;
            if (sel) GUI.Box(new Rect(r.x - 4f, r.y - 2f, r.width + 8f, r.height + 4f), "");
            if (!MenuClick.Button(r, (sel ? "> " : "  ") + (label ?? ""))) return false;
            return true;
        }
    }
}
