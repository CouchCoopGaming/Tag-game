using UnityEngine;
using Tag.Audio;

namespace TagArena.Movement
{
    /// <summary>
    /// Fires one-shot animator triggers and optional root-motion-free additive poses.
    /// Body mesh should be a child. First-person arms can listen to the same parameters.
    /// Empty AudioClip fields fall back to TagSfx (Resources or procedural).
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
            EnsureAudio();
            if (!motor) return;
            motor.OnJumped += HandleJump;
            motor.OnSlid += HandleSlide;
            motor.OnWallBounced += HandleBounce;
            motor.OnSuperGlide += HandleGlide;
            motor.OnMantle += HandleMantle;
            motor.OnStateChanged += HandleState;
            motor.OnBecameIt += HandleBecameIt;
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
            motor.OnBecameIt -= HandleBecameIt;
        }

        void EnsureAudio()
        {
            if (source == null)
                source = TagSfx.EnsureSource(gameObject);
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
            Play(jumpClip != null ? jumpClip : TagSfx.Jump, 0.55f);
        }

        void HandleSlide()
        {
            Play(slideClip != null ? slideClip : TagSfx.Slide, 0.5f);
        }

        void HandleBounce()
        {
            if (animator) animator.SetTrigger(AnimIds.BounceTrig);
            Play(bounceClip != null ? bounceClip : TagSfx.Land, 0.55f);
        }

        void HandleGlide()
        {
            if (animator) animator.SetTrigger(AnimIds.GlideTrig);
            Play(glideClip != null ? glideClip : TagSfx.Jet, 0.45f);
        }

        void HandleMantle()
        {
            if (animator) animator.SetTrigger(AnimIds.MantleTrig);
        }

        void HandleBecameIt()
        {
            TagSfx.BecomeIt(transform.position);
        }

        void HandleState(MoveState prev, MoveState next)
        {
            if (next == MoveState.LandStun)
            {
                if (animator) animator.SetTrigger(AnimIds.LandTrig);
                Play(landClip != null ? landClip : TagSfx.Land, 0.48f);
            }
            else if (next == MoveState.Ski && prev != MoveState.Ski)
            {
                Play(skiLoop != null ? skiLoop : TagSfx.Ski, 0.4f);
            }
            else if (next == MoveState.Jet && prev != MoveState.Jet)
            {
                Play(jetLoop != null ? jetLoop : TagSfx.Jet, 0.38f);
            }
        }

        void Play(AudioClip clip, float vol)
        {
            EnsureAudio();
            TagSfx.Play(source, clip, vol);
        }
    }
}
