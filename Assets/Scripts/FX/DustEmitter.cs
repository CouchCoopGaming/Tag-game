using Tag.Art;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Foot puffs on the gait plant, a slide trail, and wall-run scuffs.
    /// One pooled system per pawn. Reduced flashing skips the cloud.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class DustEmitter : MonoBehaviour
    {
        DummyLocomotor _loco;
        PlayerMotor _motor;
        FxBurstPool _fx;
        int _colId;
        int _surface = (int)DustLook.Surface.Concrete;
        int _wallId;
        int _wallSurf = (int)DustLook.Surface.Concrete;
        float _prevCycle;
        float _prevSurf;
        float _prevSpeed;
        float _slideGap;
        bool _wasSlide;
        bool _ready;

        public static void Ensure(GameObject host)
        {
            if (host == null) return;
            if (host.GetComponent<DustEmitter>() == null)
                host.AddComponent<DustEmitter>();
            ComicBurst.Ensure();
            VerbFxHost.Ensure(host);
        }

        void Awake()
        {
            _loco = GetComponent<DummyLocomotor>();
            _motor = GetComponent<PlayerMotor>();
            _fx = FxBurstPool.Ensure(transform);
            _ready = _loco != null && _motor != null && _fx != null;
        }

        void LateUpdate()
        {
            if (!_ready) return;
            if (!DustLook.CloudsOn(GameSettings.Current)) return;
            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            MoveState st = _motor.State;
            float speed = _motor.HorizSpeed;
            bool grounded = _motor.Ground.grounded;
            if (grounded)
                _surface = DustContact.Read(_motor.Ground.collider, ref _colId, ref _surface);

            float cycle = _loco.GaitCycle;
            bool gait = grounded && (st == MoveState.Walk || st == MoveState.Sprint || st == MoveState.Crouch);
            bool left;
            if (gait && DustLook.FootDown(_prevCycle, cycle, out left))
            {
                int kick = (int)DustLook.KickOf(_prevSpeed, speed, _loco.HardTurnVis, false);
                DustLook.Puff puff = DustLook.At(_surface, speed, kick);
                _fx.PlayShaped(FxBurstKind.Run, FootPos(left), puff);
            }

            bool slide = st == MoveState.Slide;
            if (slide && !_wasSlide)
            {
                DustLook.Puff puff = DustLook.At(_surface, speed, (int)DustLook.Kick.Slide);
                _fx.PlayShaped(FxBurstKind.Run, FootPos(false), puff);
                _slideGap = DustLook.TrailGap;
            }
            else if (slide)
            {
                _slideGap -= dt;
                if (_slideGap <= 0f)
                {
                    _slideGap = DustLook.TrailGap;
                    DustLook.Puff puff = DustLook.At(_surface, speed, (int)DustLook.Kick.Trail);
                    _fx.PlayShaped(FxBurstKind.Run, FootPos(false), puff);
                }
            }

            if (st == MoveState.WallRun)
            {
                _wallSurf = DustContact.Read(_motor.WallCollider, ref _wallId, ref _wallSurf);
                float surf = _loco.SurfPhase;
                if (DustLook.FootDown(_prevSurf, surf, out left))
                {
                    DustLook.Puff puff = DustLook.At(_wallSurf, speed, (int)DustLook.Kick.None);
                    _fx.PlayShaped(FxBurstKind.WallScuff, WallPos(), puff);
                }
                _prevSurf = surf;
            }
            else
                _prevSurf = _loco.SurfPhase;

            _prevCycle = cycle;
            _prevSpeed = speed;
            _wasSlide = slide;
        }

        Vector3 FootPos(bool left)
        {
            Vector3 pos = _motor.Ground.point;
            if (pos.sqrMagnitude < 0.0001f)
                pos = _motor.transform.position;
            pos.y += 0.06f;
            float side = left ? -0.16f : 0.16f;
            pos += _motor.transform.right * side;
            return pos;
        }

        Vector3 WallPos()
        {
            Vector3 pos = _motor.WallPoint;
            if (pos.sqrMagnitude < 0.0001f)
                pos = _motor.transform.position + Vector3.up * 0.4f;
            return pos;
        }
    }
}
