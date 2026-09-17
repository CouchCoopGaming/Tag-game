using UnityEngine;

namespace TagArena.Movement
{
    public struct GroundInfo
    {
        public bool grounded;
        public bool walkable;
        public Vector3 point;
        public Vector3 normal;
        public float slopeAngle;
        public float downhillDot; // 1 = facing straight down the fall line
        public Vector3 fallLine;  // gravity projected on plane, normalized
        public Collider collider;
    }

    public struct WallHit
    {
        public bool hit;
        public Vector3 point;
        public Vector3 normal;
        public float distance;
        public Collider collider;
        public bool left;
    }

    public struct LedgeHit
    {
        public bool hit;
        public Vector3 standPoint;
        public Vector3 wallNormal;
        public float height;
    }

    /// <summary>
    /// All world queries live here so the motor never scatters SphereCasts.
    /// </summary>
    public class SurfaceProbe : MonoBehaviour
    {
        public MovementConfig cfg;
        public Transform body;

        public GroundInfo Ground;
        public WallHit Wall;
        public LedgeHit Ledge;

        CapsuleCollider _cap;
        // Brief wall memory so one-frame SphereCast misses do not drop wall-run/climb.
        float _wallStickyUntil;
        Vector3 _stickyNormal;
        bool _stickyLeft;

        public void Init(MovementConfig config, Transform t, CapsuleCollider cap)
        {
            cfg = config;
            body = t;
            _cap = cap;
        }

        public void Refresh(float currentHeight, Vector3 velocity)
        {
            ProbeGround(currentHeight);
            ProbeWall(velocity);
            ProbeLedge(currentHeight);
        }

        void ProbeGround(float height)
        {
            Ground = default;
            float radius = cfg.radius * 0.92f;
            Vector3 origin = body.position + Vector3.up * (radius + 0.05f);
            float dist = cfg.groundProbe + 0.12f;

            if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, dist, cfg.groundMask, QueryTriggerInteraction.Ignore))
            {
                Ground.grounded = hit.distance <= cfg.groundProbe + radius * 0.15f;
                Ground.point = hit.point;
                Ground.normal = hit.normal;
                Ground.slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                Ground.walkable = Ground.slopeAngle <= cfg.maxWalkableAngle;
                Ground.collider = hit.collider;

                Vector3 g = Vector3.ProjectOnPlane(Vector3.down, hit.normal);
                Ground.fallLine = g.sqrMagnitude > 0.0001f ? g.normalized : Vector3.zero;
                Vector3 flatVel = Vector3.ProjectOnPlane(body.forward, Vector3.up);
                Ground.downhillDot = Ground.fallLine.sqrMagnitude > 0f
                    ? Vector3.Dot(flatVel.normalized, Vector3.ProjectOnPlane(Ground.fallLine, Vector3.up).normalized)
                    : 0f;
            }
        }

        void ProbeWall(Vector3 velocity)
        {
            Wall = default;
            Vector3 origin = body.position + Vector3.up * (cfg.standingHeight * 0.55f);
            Vector3 dir = velocity.sqrMagnitude > 1f
                ? Vector3.ProjectOnPlane(velocity, Vector3.up).normalized
                : body.forward;
            if (dir.sqrMagnitude < 0.01f) dir = body.forward;

            // Slightly longer reach + fatter cast = stickier wall detect for TP parkour
            float reach = cfg.radius + 0.68f;

            if (CastWall(origin, dir, reach, out RaycastHit hit))
            {
                FillWall(hit, false);
                return;
            }

            // Side probes for wall-run / bounce from glancing slides
            Vector3 right = body.right;
            if (CastWall(origin, right, reach, out hit)) { FillWall(hit, false); return; }
            if (CastWall(origin, -right, reach, out hit)) { FillWall(hit, true); return; }

            // Sticky re-probe into last wall normal so brief gaps do not cancel wall-run
            if (Time.time <= _wallStickyUntil && _stickyNormal.sqrMagnitude > 0.01f)
            {
                if (CastWall(origin, -_stickyNormal, reach * 1.18f, out hit))
                    FillWall(hit, _stickyLeft);
            }
        }

        bool CastWall(Vector3 origin, Vector3 dir, float reach, out RaycastHit hit)
        {
            return Physics.SphereCast(origin, cfg.radius * 0.62f, dir, out hit, reach, cfg.wallMask, QueryTriggerInteraction.Ignore)
                   && Vector3.Angle(hit.normal, Vector3.up) > cfg.maxWalkableAngle + 4f;
        }

        void FillWall(RaycastHit hit, bool left)
        {
            Wall.hit = true;
            Wall.point = hit.point;
            Wall.normal = hit.normal;
            Wall.distance = hit.distance;
            Wall.collider = hit.collider;
            Wall.left = left || Vector3.Dot(body.right, -hit.normal) < 0f;
            _stickyNormal = Wall.normal;
            _stickyLeft = Wall.left;
            _wallStickyUntil = Time.time + 0.18f; // was 0.14 — brief climb/run gap forgiveness
        }

        void ProbeLedge(float height)
        {
            Ledge = default;
            if (!Wall.hit) return;

            // Multi-depth casts: mega-park / HiPoly tops miss a single thin cast.
            Vector3 into = Vector3.ProjectOnPlane(-Wall.normal, Vector3.up);
            if (into.sqrMagnitude < 0.01f) into = -Wall.normal;
            into.Normalize();

            float castR = Mathf.Max(0.16f, cfg.radius * 0.48f);
            float maxH = cfg.mantleMaxLedgeHeight;
            float castDist = maxH + 0.55f;
            float bestScore = float.MaxValue;
            RaycastHit best = default;
            bool found = false;

            // Near lip, mid slab, deep top — covers thin rails and thick Mega_ decks.
            float[] depths = {
                cfg.radius + 0.12f,
                cfg.radius + 0.32f,
                cfg.radius + 0.55f,
                cfg.radius + 0.85f
            };

            for (int i = 0; i < depths.Length; i++)
            {
                Vector3 start = body.position + Vector3.up * (maxH + 0.25f) + into * depths[i];
                if (!Physics.SphereCast(start, castR, Vector3.down, out RaycastHit top, castDist, cfg.groundMask, QueryTriggerInteraction.Ignore))
                    continue;

                float ledgeH = top.point.y - body.position.y;
                if (ledgeH < cfg.mantleMinLedgeHeight || ledgeH > maxH)
                    continue;

                // Prefer walkable tops; steep ramps are climb, not mantle.
                if (Vector3.Angle(top.normal, Vector3.up) > cfg.maxWalkableAngle + 8f)
                    continue;

                // Clearance: slightly slimmer so busy park props do not false-reject.
                if (Physics.CheckCapsule(
                        top.point + Vector3.up * (cfg.radius + 0.04f),
                        top.point + Vector3.up * (cfg.standingHeight - cfg.radius * 0.9f),
                        cfg.radius * 0.72f, cfg.wallMask, QueryTriggerInteraction.Ignore))
                    continue;

                // Prefer mid-height lips near the wall (lower score wins).
                float score = Mathf.Abs(ledgeH - (cfg.mantleMinLedgeHeight + maxH) * 0.45f) + depths[i] * 0.15f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = top;
                    found = true;
                }
            }

            if (!found) return;

            Ledge.hit = true;
            Ledge.standPoint = best.point + Vector3.up * 0.03f;
            Ledge.wallNormal = Wall.normal;
            Ledge.height = best.point.y - body.position.y;
        }

        public static Vector3 ProjectOnPlanePreserveMag(Vector3 vel, Vector3 normal)
        {
            Vector3 p = Vector3.ProjectOnPlane(vel, normal);
            float mag = new Vector3(vel.x, 0f, vel.z).magnitude;
            if (p.sqrMagnitude > 0.0001f && mag > 0.01f)
                p = p.normalized * Mathf.Max(p.magnitude, mag * 0.98f);
            return p;
        }
    }
}
