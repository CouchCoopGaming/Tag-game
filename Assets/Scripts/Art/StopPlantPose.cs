using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual hard-brake stop only. Grounded, a high recent planar speed that
    /// drops hard toward the gait idle gate plants the lead foot, settles the
    /// hips back, and damps the gait arm swing. The window then hands off to
    /// the idle weight shift. A gentle slow keeps the GaitBlend lean. Slides,
    /// crouches, and lunges do not enter. No root motion, and no motor numbers.
    /// </summary>
    public static class StopPlantPose
    {
        public const bool RootMotion = false;

        /// <summary>Plant, then the idle handoff. The whole read stays in this window.</summary>
        public const float WindowSeconds = 0.18f;
        /// <summary>Share of the window the plant still owns. Idle eases in after this.</summary>
        public const float IdleAt = 0.50f;
        /// <summary>Fast enough that the short window is what you see.</summary>
        public const float Slew = 320f;

        /// <summary>Recent planar speed at or above a run. A walk is a gentle slow.</summary>
        public const float RecentMin = 9f;
        /// <summary>Speed has arrived at the idle gate. Above this the gait still owns the slow.</summary>
        public const float ArriveSpeed = 2.0f;
        /// <summary>How far under the recent speed the arrival has to be.</summary>
        public const float DropMin = 4f;
        /// <summary>
        /// Planar speed loss, m/s^2, that counts as a hard brake.
        /// A full ground release is sharper than this. A gentle ease stays under it.
        /// groundDecel is not read and is not written.
        /// </summary>
        public const float BrakeMin = 26f;
        /// <summary>How long the fast speed and the hard brake are still remembered.</summary>
        public const float MemorySeconds = 0.45f;

        /// <summary>Lead foot reaches forward and plants. Positive pitch is forward.</summary>
        public const float LeadThigh = 16f;
        public const float LeadKnee = -24f;
        /// <summary>Trail foot stays back under the hip.</summary>
        public const float TrailThigh = -6f;
        public const float TrailKnee = -12f;
        /// <summary>Hips pitch back over the plant. Negative is back.</summary>
        public const float HipPitch = -9f;
        /// <summary>Chest follows less, so the settle is the hips.</summary>
        public const float ChestPitch = -3.5f;
        /// <summary>Head pitches against the chest so the face stays up.</summary>
        public const float HeadPitch = 2.5f;
        /// <summary>Quiet hang the gait swing damps toward.</summary>
        public const float ArmPitch = -8f;
        public const float ArmYaw = 12f;
        public const float Elbow = -14f;

        public struct Memory
        {
            public float Recent;
            public float RecentAge;
            public float Brake;
            public float BrakeAge;
            public bool Armed;
        }

        public struct Sample
        {
            public float HipPitch, ChestPitch, HeadPitch;
            public float LeadThigh, LeadKnee, LeadFoot;
            public float TrailThigh, TrailKnee, TrailFoot;
            public float ArmPitch, ArmYaw, Elbow;
        }

        /// <summary>
        /// Remember the fastest recent speed and the hardest recent brake.
        /// Armed only after that speed has been high, so a rest pose cannot fire.
        /// </summary>
        public static void Remember(ref Memory memory, float planarSpeed, float decel, float dt)
        {
            float speed = planarSpeed < 0f ? 0f : planarSpeed;
            float brake = decel < 0f ? 0f : decel;
            if (dt < 0f) dt = 0f;
            if (speed >= memory.Recent)
            {
                memory.Recent = speed;
                memory.RecentAge = 0f;
            }
            else
            {
                memory.RecentAge += dt;
                if (memory.RecentAge >= MemorySeconds)
                    memory.Recent = speed;
            }
            if (brake >= memory.Brake)
            {
                memory.Brake = brake;
                memory.BrakeAge = 0f;
            }
            else
            {
                memory.BrakeAge += dt;
                if (memory.BrakeAge >= MemorySeconds)
                    memory.Brake = brake;
            }
            if (speed >= RecentMin)
                memory.Armed = true;
        }

        /// <summary>
        /// 1 while the plant owns the bones, 0 once the idle weight shift has it.
        /// Idle starts after the plant has landed. The two shares sum to 1.
        /// </summary>
        public static void IntoIdle(float timer01, out float plantW, out float idleW)
        {
            float u = timer01 < 0f ? 0f : (timer01 > 1f ? 1f : timer01);
            float span = 1f - IdleAt;
            if (span < 0.0001f) span = 1f;
            idleW = PoseHandoff.Ease((u - IdleAt) / span);
            plantW = 1f - idleW;
        }

        /// <summary>
        /// Gait swing left after the plant weight. 1 damps it out.
        /// A gentle slow never reaches here, so the stride swing stays.
        /// </summary>
        public static float DampSwing(float swingDeg, float plant01)
        {
            float p = plant01 < 0f ? 0f : (plant01 > 1f ? 1f : plant01);
            return swingDeg * (1f - p);
        }

        /// <summary>True when a remembered hard brake has arrived at the idle gate.</summary>
        public static bool Arrives(Memory memory, float planarSpeed)
        {
            if (!memory.Armed) return false;
            float speed = planarSpeed < 0f ? 0f : planarSpeed;
            if (memory.Recent < RecentMin) return false;
            if (speed > ArriveSpeed) return false;
            if (memory.Recent < speed + DropMin) return false;
            if (memory.Brake < BrakeMin) return false;
            return true;
        }

        /// <summary>Slides, crouches, and lunges keep their own poses.</summary>
        public static bool Fires(Memory memory, float planarSpeed, bool sliding, bool crouch, bool lunging)
        {
            if (sliding || crouch || lunging) return false;
            return Arrives(memory, planarSpeed);
        }

        public static Sample At()
        {
            return new Sample
            {
                HipPitch = HipPitch,
                ChestPitch = ChestPitch,
                HeadPitch = HeadPitch,
                LeadThigh = LeadThigh,
                LeadKnee = LeadKnee,
                LeadFoot = GaitBlend.SoleLevelDeg(LeadThigh, LeadKnee),
                TrailThigh = TrailThigh,
                TrailKnee = TrailKnee,
                TrailFoot = GaitBlend.SoleLevelDeg(TrailThigh, TrailKnee),
                ArmPitch = ArmPitch,
                ArmYaw = ArmYaw,
                Elbow = Elbow,
            };
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (WindowSeconds < 0.12f || WindowSeconds > 0.20f) return false;
            if (IdleAt < 0.35f || IdleAt > 0.65f) return false;
            if (Slew < 240f || Slew > 420f) return false;
            if (Mathf.Abs(RecentMin - GaitBlend.RunSpeed) > 0.001f) return false;
            if (ArriveSpeed <= GaitBlend.IdleGate || ArriveSpeed >= GaitBlend.WalkSpeed) return false;
            if (DropMin < 3f || DropMin >= RecentMin * 0.5f) return false;
            if (BrakeMin < 22f || BrakeMin > 34f) return false;
            if (MemorySeconds < 0.30f || MemorySeconds > 0.55f) return false;
            if (LeadThigh <= 8f || LeadThigh >= 28f) return false;
            if (TrailThigh >= 0f || LeadThigh <= TrailThigh) return false;
            if (LeadKnee >= TrailKnee || LeadKnee >= -16f) return false;
            if (TrailKnee >= 0f) return false;
            if (HipPitch >= -6f || HipPitch < -14f) return false;
            if (ChestPitch >= 0f || ChestPitch <= HipPitch) return false;
            if (HeadPitch <= 0f) return false;
            if (ArmPitch >= 0f || ArmYaw < 8f || Elbow >= 0f) return false;
            if (Mathf.Abs(ArmPitch) >= GaitBlend.ArmAmp(GaitBlend.PoseWeight(GaitBlend.RunSpeed)) * 0.5f) return false;
            if (Mathf.Abs(GaitBlend.AccelLeanDeg - 6.5f) > 0.01f) return false;
            if (!(GaitBlend.AccelLean(-16f) < -1f)) return false;

            IntoIdle(0f, out float plant0, out float idle0);
            if (plant0 < 0.999f || idle0 > 0.0001f) return false;
            IntoIdle(IdleAt, out float plantHold, out float idleHold);
            if (plantHold < 0.999f || idleHold > 0.0001f) return false;
            IntoIdle(1f, out float plant1, out float idle1);
            if (plant1 > 0.0001f || idle1 < 0.999f) return false;
            IntoIdle((IdleAt + 1f) * 0.5f, out float plantMid, out float idleMid);
            if (Mathf.Abs(plantMid + idleMid - 1f) > 0.0001f) return false;
            if (idleMid < 0.2f || plantMid < 0.2f) return false;
            float prevIdle = -1f;
            for (int i = 0; i <= 8; i++)
            {
                IntoIdle(i / 8f, out float plantW, out float idleW);
                if (Mathf.Abs(plantW + idleW - 1f) > 0.0001f) return false;
                if (idleW + 0.0001f < prevIdle) return false;
                prevIdle = idleW;
            }

            if (Mathf.Abs(DampSwing(40f, 0f) - 40f) > 0.01f) return false;
            if (Mathf.Abs(DampSwing(40f, 0.5f) - 20f) > 0.01f) return false;
            if (Mathf.Abs(DampSwing(40f, 1f)) > 0.01f) return false;
            if (Mathf.Abs(DampSwing(-32f, 1f)) > 0.01f) return false;

            Sample pose = At();
            if (Mathf.Abs(pose.HipPitch - HipPitch) > 0.01f) return false;
            if (pose.LeadThigh <= pose.TrailThigh) return false;
            if (pose.LeadKnee >= pose.TrailKnee) return false;
            if (Mathf.Abs(GaitBlend.StanceWorldPitch(pose.LeadThigh, pose.LeadKnee, pose.LeadFoot)) > 0.05f) return false;
            if (Mathf.Abs(GaitBlend.StanceWorldPitch(pose.TrailThigh, pose.TrailKnee, pose.TrailFoot)) > 0.05f) return false;

            Memory hard = default;
            hard.Armed = true;
            hard.Recent = GaitBlend.SprintSpeed;
            hard.Brake = BrakeMin;
            if (!Fires(hard, ArriveSpeed, false, false, false)) return false;
            if (!Fires(hard, GaitBlend.IdleGate, false, false, false)) return false;
            if (Fires(hard, ArriveSpeed + 0.05f, false, false, false)) return false;
            if (Fires(hard, GaitBlend.WalkSpeed, false, false, false)) return false;
            if (Fires(hard, ArriveSpeed, true, false, false)) return false;
            if (Fires(hard, ArriveSpeed, false, true, false)) return false;
            if (Fires(hard, ArriveSpeed, false, false, true)) return false;
            hard.Armed = false;
            if (Fires(hard, ArriveSpeed, false, false, false)) return false;
            hard.Armed = true;
            hard.Brake = BrakeMin - 0.5f;
            if (Fires(hard, ArriveSpeed, false, false, false)) return false;
            hard.Brake = 40f;
            hard.Recent = GaitBlend.WalkSpeed;
            if (Fires(hard, GaitBlend.IdleGate, false, false, false)) return false;
            hard.Recent = GaitBlend.RunSpeed - 0.1f;
            if (Fires(hard, GaitBlend.IdleGate, false, false, false)) return false;
            hard.Recent = GaitBlend.RunSpeed;
            if (!Fires(hard, GaitBlend.IdleGate, false, false, false)) return false;

            if (!Saw(GaitBlend.SprintSpeed, 0.12f, 32f, 1.2f, false, false, false)) return false;
            if (Saw(GaitBlend.SprintSpeed, 0.12f, 12f, 2.0f, false, false, false)) return false;
            if (Saw(GaitBlend.WalkSpeed, 0.12f, 38f, 1.2f, false, false, false)) return false;
            if (Saw(GaitBlend.SprintSpeed, 0.12f, 32f, 1.2f, true, false, false)) return false;
            if (Saw(GaitBlend.SprintSpeed, 0.12f, 32f, 1.2f, false, true, false)) return false;
            if (Saw(GaitBlend.SprintSpeed, 0.12f, 32f, 1.2f, false, false, true)) return false;
            if (RanHoldWalk(GaitBlend.SprintSpeed, 38f, GaitBlend.WalkSpeed)) return false;
            return true;
        }

        static bool Saw(float holdSpeed, float holdSeconds, float decel, float seconds, bool sliding, bool crouch, bool lunging)
        {
            Memory memory = default;
            float speed = holdSpeed;
            const float dt = 0.02f;
            int n = (int)(seconds / dt);
            int hold = (int)(holdSeconds / dt);
            for (int i = 0; i < n; i++)
            {
                bool braking = i >= hold && speed > 0.02f;
                if (braking)
                {
                    speed -= decel * dt;
                    if (speed < 0f) speed = 0f;
                }
                float felt = braking ? decel : 0f;
                Remember(ref memory, speed, felt, dt);
                if (Fires(memory, speed, sliding, crouch, lunging))
                    return true;
            }
            return false;
        }

        /// <summary>Sprint released only as far as a walk. That slow is not a stop.</summary>
        static bool RanHoldWalk(float fromSpeed, float decel, float holdAt)
        {
            Memory memory = default;
            float speed = fromSpeed;
            const float dt = 0.02f;
            for (int i = 0; i < 80; i++)
            {
                float felt = 0f;
                if (speed > holdAt + 0.05f)
                {
                    felt = decel;
                    speed -= decel * dt;
                    if (speed < holdAt) speed = holdAt;
                }
                Remember(ref memory, speed, felt, dt);
                if (Fires(memory, speed, false, false, false))
                    return true;
            }
            return false;
        }

        public static string ProofLine()
        {
            Memory sprint = default;
            sprint.Armed = true;
            sprint.Recent = GaitBlend.SprintSpeed;
            sprint.Brake = BrakeMin;
            float hard = Fires(sprint, ArriveSpeed, false, false, false) ? 1f : 0f;
            sprint.Recent = GaitBlend.WalkSpeed;
            float walk = Fires(sprint, GaitBlend.IdleGate, false, false, false) ? 1f : 0f;
            sprint.Recent = GaitBlend.SprintSpeed;
            sprint.Brake = 12f;
            float gentle = Fires(sprint, ArriveSpeed, false, false, false) ? 1f : 0f;
            sprint.Brake = BrakeMin;
            float slide = Fires(sprint, ArriveSpeed, true, false, false) ? 1f : 0f;
            float crouch = Fires(sprint, ArriveSpeed, false, true, false) ? 1f : 0f;
            float lunge = Fires(sprint, ArriveSpeed, false, false, true) ? 1f : 0f;
            IntoIdle(1f, out float plantEnd, out float idleEnd);
            float lean = GaitBlend.AccelLean(-16f);
            return "stop plant"
                + " window=" + WindowSeconds.ToString("0.00")
                + " idleAt=" + IdleAt.ToString("0.00")
                + " recent>=" + RecentMin.ToString("0.0")
                + " arrive<=" + ArriveSpeed.ToString("0.0")
                + " brake>=" + BrakeMin.ToString("0")
                + " hip=" + HipPitch.ToString("0.0")
                + " lead=" + LeadThigh.ToString("0")
                + " trail=" + TrailThigh.ToString("0")
                + " armDamp=" + (1f - DampSwing(1f, 1f)).ToString("0.00")
                + " hard=" + hard.ToString("0")
                + " walk=" + walk.ToString("0")
                + " gentle=" + gentle.ToString("0")
                + " slide=" + slide.ToString("0")
                + " crouch=" + crouch.ToString("0")
                + " lunge=" + lunge.ToString("0")
                + " handoff=" + plantEnd.ToString("0.00") + "+" + idleEnd.ToString("0.00")
                + " leanKept=" + lean.ToString("0.0")
                + " gate=grounded recent>=" + RecentMin.ToString("0.0")
                + " speed<=" + ArriveSpeed.ToString("0.0")
                + " brake>=" + BrakeMin.ToString("0")
                + " window " + WindowSeconds.ToString("0.00")
                + "s"
                + "; lead plant; hips back; arms damp"
                + "; handoff IdlePose"
                + "; gentle keeps GaitBlend lean"
                + "; blocks slide+crouch+lunge"
                + "; rootMotion=0";
        }
    }
}
