using Tag.Couch;
using Tag.Experimental;
using Tag.Gameplay;
using Tag.Modes;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Ui.Hud
{
    /// <summary>
    /// Finds split cameras and pawns when the roster changes. Not called every frame.
    /// </summary>
    public static class MatchHudBind
    {
        public static void Refresh(MatchHud hud)
        {
            if (hud == null) return;
            int humans = CouchPlay.Humans;
            if (humans < 1) humans = 1;
            if (humans > CouchPlay.Max) humans = CouchPlay.Max;
            int split = GameSettings.Current != null ? GameSettings.Current.SplitAxis : GameSettings.SplitVertical;
            bool stale = humans != hud.Humans || split != hud.Split;
            if (!stale)
            {
                int n = hud.Shown;
                for (int i = 0; i < n; i++)
                {
                    if (hud.Pawns[i] == null) stale = true;
                }
            }
            if (!stale) return;
            Pull(hud, humans, split);
        }

        static void Pull(MatchHud hud, int humans, int split)
        {
            hud.Humans = humans;
            hud.Split = split;
            for (int i = 0; i < CouchPlay.Max; i++)
            {
                hud.Pawns[i] = null;
                hud.Cams[i] = null;
                hud.Motors[i] = null;
                hud.Ropes[i] = null;
                hud.Seat[i] = i;
            }
            ItController[] found = Object.FindObjectsByType<ItController>(FindObjectsSortMode.None);
            int shown = 0;
            if (CouchPlay.Humans >= 2)
            {
                for (int s = 0; s < CouchPlay.Max && shown < humans; s++)
                {
                    if (!CouchPlay.HumanAt(s)) continue;
                    Assign(hud, shown, s, Match(found, CouchPlay.Name(s)));
                    shown++;
                }
            }
            else
            {
                Assign(hud, 0, 0, FirstHuman(found));
                shown = 1;
            }
            hud.Shown = shown;
            hud.LayoutDirty();
        }

        static void Assign(MatchHud hud, int pane, int seat, ItController pawn)
        {
            hud.Seat[pane] = seat;
            hud.Pawns[pane] = pawn;
            if (pawn == null) return;
            hud.Cams[pane] = pawn.GetComponentInChildren<Camera>();
            hud.Motors[pane] = pawn.GetComponent<PlayerMotor>();
            hud.Ropes[pane] = pawn.GetComponent<ExperimentalGrapple>();
        }

        static ItController Match(ItController[] found, string name)
        {
            if (found == null || string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < found.Length; i++)
            {
                ItController it = found[i];
                if (it == null || !it.gameObject.activeInHierarchy) continue;
                if (it.PlayerId == name) return it;
            }
            return null;
        }

        static ItController FirstHuman(ItController[] found)
        {
            if (found == null) return null;
            ItController any = null;
            for (int i = 0; i < found.Length; i++)
            {
                ItController it = found[i];
                if (it == null || !it.gameObject.activeInHierarchy) continue;
                if (any == null) any = it;
                if (it.GetComponent<DummyPatrol>() != null) continue;
                if (it.GetComponent<PlayerInputReader>() != null) return it;
            }
            return any;
        }
    }
}
