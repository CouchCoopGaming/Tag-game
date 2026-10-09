namespace Tag.Experimental
{
    /// <summary>
    /// Click bookkeeping for the existing rope. A press on a static anchor
    /// attaches. A second press inside <see cref="Window"/> lets go. A later
    /// press starts the pull after the same window. No new verb.
    /// </summary>
    public struct GrappleClick
    {
        public const float Window = 0.28f;
        public const float Pull = 12f;

        public bool Attached;
        public bool Pulling;
        public bool Pending;
        public bool AnchorStatic;
        public float ClickTime;
        public float PendingSince;

        public static GrappleClick Fresh()
        {
            GrappleClick s;
            s.Attached = false;
            s.Pulling = false;
            s.Pending = false;
            s.AnchorStatic = false;
            s.ClickTime = -1f;
            s.PendingSince = 0f;
            return s;
        }

        public void NoteFirstAttach(float time, bool staticAnchor)
        {
            Attached = true;
            AnchorStatic = staticAnchor;
            ClickTime = time;
            Pending = false;
            Pulling = false;
        }

        /// <summary>Second press while a rope is already attached. True means release.</summary>
        public bool OnAttachedPress(float time, float window)
        {
            if (ClickTime >= 0f && time - ClickTime <= window)
            {
                Pending = false;
                Pulling = false;
                Attached = false;
                AnchorStatic = false;
                ClickTime = time;
                return true;
            }

            ClickTime = time;
            Pending = true;
            PendingSince = time;
            Pulling = false;
            return false;
        }

        public void ResolvePending(float time, float window)
        {
            if (Pending && Attached && time - PendingSince >= window)
            {
                Pending = false;
                if (AnchorStatic)
                    Pulling = true;
            }
        }
    }
}
