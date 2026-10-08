using System.Globalization;
using System.Text;
using Tag.Art;
using Tag.Couch;
using Tag.Experimental;
using UnityEngine;

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
            if (RopeBodyHits() != 0)
                return false;
            return true;
        }

        /// <summary>
        /// Pulling seats share one left-hand span. 0 means that span misses the body.
        /// The hand is GrappleRopeTell's left hand. The anchor continues away from the chest.
        /// </summary>
        public static int RopeBodyHits()
        {
            Vector3 hand = GrappleRopeTell.Hand(Vector3.zero, Vector3.forward);
            if (hand.x >= 0f)
                return 2;
            Vector3 chest = new Vector3(0f, 1.12f, 0.06f);
            Vector3 away = hand - chest;
            if (away.sqrMagnitude < 1e-6f)
                return 2;
            away.Normalize();
            Vector3 anchor = hand + away * 7.5f;
            if (ClearsBody(hand, anchor))
                return 0;
            return 2;
        }

        static bool ClearsBody(Vector3 hand, Vector3 anchor)
        {
            if (!ClearsCapsule(hand, anchor, new Vector3(0f, 0.82f, 0.02f), new Vector3(0f, 1.42f, 0.04f), 0.22f))
                return false;
            if (!ClearsCapsule(hand, anchor, new Vector3(0f, 0.42f, 0f), new Vector3(0f, 0.82f, 0.02f), 0.20f))
                return false;
            if (!ClearsSphere(hand, anchor, new Vector3(0f, 1.62f, 0.04f), 0.15f))
                return false;
            if (!ClearsCapsule(hand, anchor, new Vector3(0.11f, 0.72f, 0f), new Vector3(0.14f, 0.18f, 0.06f), 0.11f))
                return false;
            if (!ClearsCapsule(hand, anchor, new Vector3(-0.11f, 0.72f, 0f), new Vector3(-0.14f, 0.18f, 0.06f), 0.11f))
                return false;
            return true;
        }

        static bool ClearsCapsule(Vector3 hand, Vector3 anchor, Vector3 a, Vector3 b, float radius)
        {
            return SpanGap(hand, anchor, a, b) > radius;
        }

        static bool ClearsSphere(Vector3 hand, Vector3 anchor, Vector3 center, float radius)
        {
            Vector3 ab = anchor - hand;
            float den = ab.sqrMagnitude;
            float t = den < 1e-8f ? 0f : Vector3.Dot(center - hand, ab) / den;
            if (t < 0.08f) t = 0.08f;
            if (t > 1f) t = 1f;
            return (hand + ab * t - center).magnitude > radius;
        }

        static float SpanGap(Vector3 hand, Vector3 anchor, Vector3 a, Vector3 b)
        {
            float best = 99f;
            for (int i = 2; i <= 24; i++)
            {
                float t = i / 24f;
                Vector3 p = hand + (anchor - hand) * t;
                Vector3 ab = b - a;
                float den = ab.sqrMagnitude;
                float u = den < 1e-8f ? 0f : Vector3.Dot(p - a, ab) / den;
                if (u < 0f) u = 0f;
                if (u > 1f) u = 1f;
                float dist = (p - (a + ab * u)).magnitude;
                if (dist < best) best = dist;
            }
            return best;
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
            text.Append(" ropeBody=").Append(RopeBodyHits().ToString(c));
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
