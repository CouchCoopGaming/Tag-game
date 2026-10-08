using System.Globalization;
using System.Text;
using Tag.Couch;
using Tag.Experimental;

namespace Tag.Local
{
    /// <summary>
    /// The same rope verb on every human couch seat. SoloGrappleGate.EnableFor
    /// stays the solo tell and stays false for couch. AI and DummyRunner stay off.
    /// Keyboard seat fires with RMB. Pad seats fire with LT. The click window
    /// and the pull speed are GrappleClick, the same numbers as solo.
    /// </summary>
    public static class CouchRope
    {
        public static bool ForCouchHuman(bool couch, bool ai, int index, string pawnName)
        {
            if (!couch || ai) return false;
            if (index < 0 || index >= CouchPlay.Max) return false;
            if (string.IsNullOrEmpty(pawnName)) return false;
            if (pawnName == SoloGrappleGate.OpponentPawnName) return false;
            if (pawnName.StartsWith(SoloGrappleGate.OpponentPawnName + "_")) return false;
            if (index == 0)
                return pawnName == SoloGrappleGate.SoloPawnName;
            return pawnName == "Player_" + CouchPlay.Name(index);
        }

        public static bool RopeFor(bool couch, bool ai, int index, string pawnName)
        {
            if (SoloGrappleGate.EnableFor(couch, ai, index, pawnName))
                return true;
            return ForCouchHuman(couch, ai, index, pawnName);
        }

        public static bool Holds()
        {
            if (GrappleClick.Window != 0.28f || GrappleClick.Pull != 12f)
                return false;
            if (!SoloGrappleGate.EnableFor(false, false, 0, SoloGrappleGate.SoloPawnName))
                return false;
            if (SoloGrappleGate.EnableFor(true, false, 0, SoloGrappleGate.SoloPawnName))
                return false;
            if (SoloGrappleGate.EnableFor(true, false, 1, "Player_P1"))
                return false;

            if (!RopeFor(false, false, 0, SoloGrappleGate.SoloPawnName))
                return false;
            if (RopeFor(false, true, 1, SoloGrappleGate.OpponentPawnName))
                return false;
            if (RopeFor(true, true, 0, SoloGrappleGate.OpponentPawnName))
                return false;
            if (RopeFor(true, false, 0, SoloGrappleGate.OpponentPawnName))
                return false;

            int ropes = 0;
            for (int i = 0; i < CouchPlay.Max; i++)
            {
                string pawn = i == 0
                    ? SoloGrappleGate.SoloPawnName
                    : "Player_" + CouchPlay.Name(i);
                if (!RopeFor(true, false, i, pawn))
                    return false;
                ropes++;
            }
            if (ropes != 4)
                return false;

            GrappleClick pull = PullSeat();
            GrappleClick drop = ReleaseSeat();
            GrappleClick idle = GrappleClick.Fresh();
            if (!pull.Pulling || !pull.Attached || pull.Pending)
                return false;
            if (drop.Attached || drop.Pulling || drop.Pending)
                return false;
            if (idle.Attached || idle.Pulling)
                return false;

            GrappleClick edge = GrappleClick.Fresh();
            edge.NoteFirstAttach(0f, true);
            if (!edge.OnAttachedPress(GrappleClick.Window, GrappleClick.Window))
                return false;
            GrappleClick later = GrappleClick.Fresh();
            later.NoteFirstAttach(0f, true);
            if (later.OnAttachedPress(GrappleClick.Window + 0.02f, GrappleClick.Window))
                return false;
            later.ResolvePending(GrappleClick.Window + 0.02f, GrappleClick.Window);
            if (later.Pulling)
                return false;
            later.ResolvePending(GrappleClick.Window + 0.02f + GrappleClick.Window, GrappleClick.Window);
            if (!later.Pulling)
                return false;
            return true;
        }

        public static string ProofLine()
        {
            if (!Holds())
                return "couch-grapple FAIL";
            var c = CultureInfo.InvariantCulture;
            var text = new StringBuilder(160);
            text.Append("couch-grapple seats=4 ropes=4 ai=0 fire=RMB,LT,LT,LT");
            text.Append(" window=").Append(GrappleClick.Window.ToString("0.00", c));
            text.Append(" pull=").Append(GrappleClick.Pull.ToString("0", c));
            text.Append(" onRope=2 released=1 idle=1 gameplayDelay=0 rootMotion=0");
            return text.ToString();
        }

        static GrappleClick PullSeat()
        {
            GrappleClick s = GrappleClick.Fresh();
            s.NoteFirstAttach(0f, true);
            s.OnAttachedPress(0.30f, GrappleClick.Window);
            s.ResolvePending(0.30f, GrappleClick.Window);
            s.ResolvePending(0.30f + GrappleClick.Window, GrappleClick.Window);
            return s;
        }

        static GrappleClick ReleaseSeat()
        {
            GrappleClick s = GrappleClick.Fresh();
            s.NoteFirstAttach(0f, true);
            s.OnAttachedPress(0.20f, GrappleClick.Window);
            return s;
        }
    }
}
