using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Fires one-shot animator triggers and optional root-motion-free additive poses.
    /// Body mesh should be a child. First-person arms can listen to the same parameters.
    /// </summary>
    public class MoveAnimDriver : MonoBehaviour
    {
        public PlayerMotor motor;
        public Animator animator;
        public ParticleSystem skiSparks;
        public ParticleSystem jetTrail;
        public AudioSource source;

        public AudioClip jumpClip;
        public AudioClip slideClip;
        public AudioClip bounceClip;
        public AudioClip glideClip;
        public AudioClip landClip;
        public AudioClip skiLoop;
        public AudioClip jetLoop;

        void OnEnable()
        {
            if (!motor) motor = GetComponent<PlayerMotor>();
            if (!motor) return;
            motor.OnJumped += HandleJump;
            motor.OnSlid += HandleSlide;
            motor.OnWallBounced += HandleBounce;
            motor.OnSuperGlide += HandleGlide;
            motor.OnMantle += HandleMantle;
            motor.OnStateChanged += HandleState;
        }

        void OnDisable()
        {
            if (!motor) return;
            motor.OnJumped -= HandleJump;
            motor.OnSlid -= HandleSlide;
            motor.OnWallBounced -= HandleBounce;
            motor.OnSuperGlide -= HandleGlide;
            motor.OnMantle -= HandleMantle;
            motor.OnStateChanged -= HandleState;
        }

        void Update()
        {
            if (!motor) return;
            if (skiSparks)
            {
                var em = skiSparks.emission;
                em.enabled = motor.Skiing && motor.Ground.grounded && motor.HorizSpeed > 6f;
            }
            if (jetTrail)
            {
                var em = jetTrail.emission;
                em.enabled = motor.Jetting;
            }
        }

        void HandleJump()
        {
            if (animator) animator.SetTrigger(AnimIds.JumpTrig);
            Play(jumpClip, 0.7f);
        }

        void HandleSlide()
        {
            Play(slideClip, 0.85f);
        }

        void HandleBounce()
        {
            if (animator) animator.SetTrigger(AnimIds.BounceTrig);
            Play(bounceClip, 1f);
        }

        void HandleGlide()
        {
            if (animator) animator.SetTrigger(AnimIds.GlideTrig);
            Play(glideClip, 1f);
        }

        void HandleMantle()
        {
            if (animator) animator.SetTrigger(AnimIds.MantleTrig);
        }

        void HandleState(MoveState prev, MoveState next)
        {
            if (next == MoveState.LandStun)
            {
                if (animator) animator.SetTrigger(AnimIds.LandTrig);
                Play(landClip, 1f);
            }
        }

        void Play(AudioClip clip, float vol)
        {
            if (source && clip) source.PlayOneShot(clip, vol);
        }
    }
}
