using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Draws the grapple line as a short sagging wobble. The pull is unchanged.
    /// </summary>
    public static class VerbFxRope
    {
        public static void Lay(LineRenderer line, Vector3 a, Vector3 b, float tension, float slack, float time)
        {
            if (line == null) return;
            int n = VerbFxLook.RopePoints;
            if (line.positionCount != n) line.positionCount = n;
            Vector3 span = b - a;
            Vector3 side = Vector3.Cross(span, Vector3.up);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side.Normalize();
            line.enabled = true;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                VerbFxLook.RopeOffset(t, tension, slack, time, out float lateral, out float drop);
                Vector3 p = a + span * t + side * lateral + Vector3.down * drop;
                line.SetPosition(i, p);
            }
        }
    }
}
