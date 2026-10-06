using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Audio
{
    /// <summary>
    /// Footsteps, climb scuffs, and wall-run patter. Presentation only.
    /// The motor still owns the single Move.
    /// </summary>
    public static class PawnAudio
    {
        struct Slot
        {
            public int Id;
            public float Next;
            public int ColId;
            public int Surface;
        }

        static readonly Slot[] Slots = new Slot[8];

        public static void Step(PlayerMotor motor, float dt)
        {
            AudioMix.Pump();
            if (motor == null || dt <= 0f) return;
            int id = motor.GetInstanceID();
            int slot = Find(id);
            MoveState state = motor.State;
            bool it = motor.tagRole != null && motor.tagRole.IsIt;
            Vector3 pos = motor.transform.position;
            if (state == MoveState.WallClimb)
            {
                Scuff(slot, dt, pos, it, true);
                return;
            }
            if (state == MoveState.WallRun)
            {
                Scuff(slot, dt, pos, it, false);
                return;
            }
            if (state != MoveState.Walk && state != MoveState.Sprint && state != MoveState.Crouch)
                return;
            float speed = motor.HorizSpeed;
            if (speed < 0.8f || !motor.Ground.grounded) return;
            Foot(slot, dt, pos, it, speed, motor.Ground.collider);
        }

        static void Foot(int slot, float dt, Vector3 pos, bool it, float speed, Collider col)
        {
            FootstepMap.Gait gait = FootstepMap.FromSpeed(speed);
            Slots[slot].Next -= dt;
            if (Slots[slot].Next > 0f) return;
            Slots[slot].Next = FootstepMap.Interval(gait);
            int surface = (int)SurfaceOf(slot, col);
            AudioClip clip = TagSfx.StepClip(surface);
            float vol = FootstepMap.Volume(gait) * (1f + Jitter(FootstepMap.VolumeJitter));
            if (vol < 0.05f) vol = 0.05f;
            int pri = it ? VoiceBudget.PriItStep : VoiceBudget.PriStep;
            AudioMix.PlayWorld(clip, pos, vol, pri, it, FootstepMap.Pitch(gait));
            if (it) CaptionFeed.Footstep(pos.x, pos.z);
        }

        static void Scuff(int slot, float dt, Vector3 pos, bool it, bool climb)
        {
            float gap = climb ? 0.30f : 0.16f;
            Slots[slot].Next -= dt;
            if (Slots[slot].Next > 0f) return;
            Slots[slot].Next = gap;
            AudioClip clip = climb ? TagSfx.ClimbScuff : TagSfx.WallPatter;
            float vol = climb ? 0.28f : 0.22f;
            AudioMix.PlayWorld(clip, pos, vol, VoiceBudget.PriScuff, false, 1f);
        }

        static FootstepMap.Surface SurfaceOf(int slot, Collider col)
        {
            int id = col != null ? col.GetInstanceID() : 0;
            if (id != 0 && id == Slots[slot].ColId)
                return (FootstepMap.Surface)Slots[slot].Surface;
            Slots[slot].ColId = id;
            string name = "";
            if (col != null)
            {
                MeshRenderer rend = col.GetComponent<MeshRenderer>();
                if (rend != null && rend.sharedMaterial != null)
                    name = rend.sharedMaterial.name;
                else
                    name = col.name;
            }
            var surface = FootstepMap.Classify(name);
            Slots[slot].Surface = (int)surface;
            return surface;
        }

        static int Find(int id)
        {
            int free = -1;
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].Id == id) return i;
                if (Slots[i].Id == 0 && free < 0) free = i;
            }
            int slot = free >= 0 ? free : 0;
            Slots[slot].Id = id;
            Slots[slot].Next = 0f;
            Slots[slot].ColId = 0;
            return slot;
        }

        static uint _rng = 0xA11CE5u;

        static float Jitter(float amount)
        {
            _rng = _rng * 1664525u + 1013904223u;
            float u = ((_rng >> 8) & 0xFFFFFFu) / 16777215f;
            return (u * 2f - 1f) * amount;
        }
    }
}
