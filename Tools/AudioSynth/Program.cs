using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Tag.Tools
{
    /// <summary>
    /// Deterministic one-shot baker. Same source, same WAVs.
    /// Writes Assets/Audio and the Resources mirror Unity loads at play.
    /// No samples are pulled from outside the repo.
    /// </summary>
    static class Program
    {
        const int Rate = 22050;

        struct Rng
        {
            uint _s;

            public Rng(uint seed)
            {
                _s = seed == 0 ? 1u : seed;
            }

            public float Next()
            {
                _s = _s * 1664525u + 1013904223u;
                return ((_s >> 8) & 0xFFFFFFu) / 16777215f * 2f - 1f;
            }
        }

        delegate void Fill(float[] dst, Rng rng);

        struct Job
        {
            public string Rel;
            public float Seconds;
            public uint Salt;
            public Fill Paint;
            public string[] Also;
        }

        static int Main()
        {
            string root = FindRoot();
            var jobs = new List<Job>();
            Add(jobs, "SFX/sfx_jump.wav", 0.08f, Jump);
            Add(jobs, "SFX/sfx_land_soft.wav", 0.12f, LandSoft);
            Add(jobs, "SFX/sfx_land_hard.wav", 0.16f, LandHard);
            Add(jobs, "SFX/sfx_slide_start.wav", 0.14f, SlideStart, "SFX/sfx_slide.wav");
            Add(jobs, "SFX/sfx_slide_loop.wav", 0.44f, SlideLoop);
            Add(jobs, "SFX/sfx_slide_end.wav", 0.09f, SlideEnd);
            Add(jobs, "SFX/sfx_cling.wav", 0.07f, Cling);
            Add(jobs, "SFX/sfx_climb_scuff.wav", 0.045f, ClimbScuff);
            Add(jobs, "SFX/sfx_wallrun.wav", 0.032f, WallPatter);
            Add(jobs, "SFX/sfx_wall_jump.wav", 0.11f, WallJump);
            Add(jobs, "SFX/sfx_air_dash.wav", 0.075f, AirDash, "SFX/sfx_air_dodge.wav");
            Add(jobs, "SFX/sfx_punch_whiff.wav", 0.06f, PunchWhiff, "SFX/sfx_punch_miss.wav");
            Add(jobs, "SFX/sfx_punch_hit.wav", 0.085f, PunchHit);
            Add(jobs, "SFX/sfx_tag_transfer.wav", 0.22f, TagSting);
            Add(jobs, "SFX/sfx_tagback.wav", 0.30f, TagShimmer);
            Add(jobs, "SFX/sfx_stagger.wav", 0.09f, Stagger);
            Add(jobs, "SFX/sfx_pad.wav", 0.20f, PadBoing);
            Add(jobs, "SFX/sfx_zip_grab.wav", 0.06f, ZipGrab);
            Add(jobs, "SFX/sfx_zip_loop.wav", 0.44f, ZipLoop);
            Add(jobs, "SFX/sfx_zip_drop.wav", 0.12f, ZipDrop);
            Add(jobs, "SFX/sfx_countdown.wav", 0.055f, Countdown);
            Add(jobs, "SFX/sfx_round_start.wav", 0.24f, RoundStart);
            Add(jobs, "SFX/sfx_round_tick.wav", 0.04f, RoundTick);
            Add(jobs, "SFX/sfx_round_end.wav", 0.30f, RoundEnd);
            Add(jobs, "SFX/sfx_round_win.wav", 0.38f, RoundWin);
            Add(jobs, "SFX/sfx_round_lose.wav", 0.28f, RoundLose);
            Add(jobs, "SFX/sfx_step_concrete.wav", 0.05f, StepConcrete);
            Add(jobs, "SFX/sfx_step_grass.wav", 0.06f, StepGrass);
            Add(jobs, "SFX/sfx_step_metal.wav", 0.08f, StepMetal);
            Add(jobs, "SFX/sfx_step_wood.wav", 0.055f, StepWood);
            Add(jobs, "UI/ui_move.wav", 0.032f, UiMove);
            Add(jobs, "UI/ui_confirm.wav", 0.08f, UiConfirm);
            Add(jobs, "UI/ui_back.wav", 0.07f, UiBack);
            Add(jobs, "UI/ui_click.wav", 0.035f, UiClick);
            Add(jobs, "UI/ui_hover.wav", 0.028f, UiHover);

            int written = 0;
            for (int i = 0; i < jobs.Count; i++)
            {
                Job job = jobs[i];
                float[] data = Bake(job);
                WriteBoth(root, job.Rel, data);
                written++;
                if (job.Also == null) continue;
                for (int a = 0; a < job.Also.Length; a++)
                {
                    WriteBoth(root, job.Also[a], data);
                    written++;
                }
            }

            Console.WriteLine("audio-synth clips=" + jobs.Count.ToString() + " writes=" + written.ToString() + " rate=" + Rate.ToString());
            return 0;
        }

        static void Add(List<Job> jobs, string rel, float seconds, Fill paint, params string[] also)
        {
            jobs.Add(new Job { Rel = rel, Seconds = seconds, Salt = SaltOf(rel), Paint = paint, Also = also });
        }

        static float[] Bake(Job job)
        {
            int n = Math.Max(8, (int)(Rate * job.Seconds));
            var data = new float[n];
            job.Paint(data, new Rng(job.Salt));
            Finish(data);
            return data;
        }

        static void Finish(float[] data)
        {
            double sum = 0.0;
            for (int i = 0; i < data.Length; i++) sum += data[i];
            float mean = (float)(sum / data.Length);
            int fade = data.Length / 8;
            if (fade < 8) fade = 8;
            if (fade > 64) fade = 64;
            float peak = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float s = data[i] - mean;
                float env = 1f;
                if (i < fade) env = i / (float)fade;
                else if (i > data.Length - 1 - fade) env = (data.Length - 1 - i) / (float)fade;
                if (env < 0f) env = 0f;
                s *= env;
                data[i] = s;
                float a = s < 0f ? -s : s;
                if (a > peak) peak = a;
            }
            if (peak < 0.0001f) return;
            float gain = 0.8f / peak;
            for (int i = 0; i < data.Length; i++)
                data[i] *= gain;
        }

        static void Jump(float[] d, Rng rng)
        {
            double phase = 0.0;
            for (int i = 0; i < d.Length; i++)
            {
                float u = i / (float)(d.Length - 1);
                float t = i / (float)Rate;
                float hz = Lerp(260f, 740f, u);
                phase += 2.0 * Math.PI * hz / Rate;
                float env = (float)Math.Exp(-16.0 * t);
                float click = i < Rate * 0.004f ? rng.Next() * (1f - i / (Rate * 0.004f)) : 0f;
                d[i] = (float)Math.Sin(phase) * 0.7f * env + click * 0.45f;
            }
        }

        static void LandSoft(float[] d, Rng rng)
        {
            double phase = 0.0;
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                phase += 2.0 * Math.PI * 96.0 / Rate;
                float env = (float)Math.Exp(-12.0 * t);
                lp = Low(lp, rng.Next(), 700f);
                d[i] = (float)Math.Sin(phase) * 0.45f * env + lp * 0.35f * env;
            }
        }

        static void LandHard(float[] d, Rng rng)
        {
            double phase = 0.0;
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                phase += 2.0 * Math.PI * 52.0 / Rate;
                float body = (float)Math.Exp(-9.0 * t);
                float click = (float)Math.Exp(-48.0 * t);
                lp = Low(lp, rng.Next(), 1800f);
                float x = (float)Math.Sin(phase) * 0.7f * body + lp * 0.55f * click;
                d[i] = (float)Math.Tanh(x * 1.6);
            }
        }

        static void SlideStart(float[] d, Rng rng)
        {
            float lp = 0f;
            float hp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float u = i / (float)(d.Length - 1);
                float cut = Lerp(500f, 2200f, u);
                float band = Band(ref lp, ref hp, rng.Next(), 280f, cut);
                float env = (float)Math.Sin(Math.PI * u);
                d[i] = band * 0.85f * env;
            }
        }

        static void SlideLoop(float[] d, Rng rng)
        {
            float lp = 0f;
            float hp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float wob = 0.75f + 0.25f * (float)Math.Sin(2.0 * Math.PI * 7.0 * t);
                float band = Band(ref lp, ref hp, rng.Next(), 350f, 1600f);
                d[i] = band * wob;
            }
        }

        static void SlideEnd(float[] d, Rng rng)
        {
            float lp = 0f;
            float hp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float band = Band(ref lp, ref hp, rng.Next(), 200f, 900f);
                d[i] = band * (float)Math.Exp(-22.0 * t);
            }
        }

        static void Cling(float[] d, Rng rng)
        {
            double phase = 0.0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                phase += 2.0 * Math.PI * 170.0 / Rate;
                float click = i < Rate * 0.003f ? rng.Next() : 0f;
                float env = (float)Math.Exp(-20.0 * t);
                d[i] = (float)Math.Sin(phase) * 0.55f * env + click * 0.7f * env;
            }
        }

        static void ClimbScuff(float[] d, Rng rng)
        {
            float lp = 0f;
            float hp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float band = Band(ref lp, ref hp, rng.Next(), 400f, 1400f);
                d[i] = band * (float)Math.Exp(-28.0 * t);
            }
        }

        static void WallPatter(float[] d, Rng rng)
        {
            float lp = 0f;
            float hp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float band = Band(ref lp, ref hp, rng.Next(), 900f, 2800f);
                d[i] = band * (float)Math.Exp(-40.0 * t);
            }
        }

        static void WallJump(float[] d, Rng rng)
        {
            double phase = 0.0;
            float lp = 0f;
            float hp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float u = i / (float)(d.Length - 1);
                float t = i / (float)Rate;
                phase += 2.0 * Math.PI * 88.0 / Rate;
                float thump = (float)Math.Sin(phase) * (float)Math.Exp(-14.0 * t);
                float cut = Lerp(600f, 2400f, u);
                float whoosh = Band(ref lp, ref hp, rng.Next(), 300f, cut) * (float)Math.Sin(Math.PI * u);
                d[i] = thump * 0.65f + whoosh * 0.7f;
            }
        }

        static void AirDash(float[] d, Rng rng)
        {
            float lp = 0f;
            float hp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float u = i / (float)(d.Length - 1);
                float cut = Lerp(3200f, 700f, u);
                float band = Band(ref lp, ref hp, rng.Next(), 500f, cut);
                float env = (float)Math.Sin(Math.PI * u);
                env *= env;
                d[i] = band * env;
            }
        }

        static void PunchWhiff(float[] d, Rng rng)
        {
            float lp = 0f;
            float hp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float u = i / (float)(d.Length - 1);
                float band = Band(ref lp, ref hp, rng.Next(), 1200f, 4200f);
                float env = (float)Math.Sin(Math.PI * u);
                d[i] = band * env * 0.8f;
            }
        }

        static void PunchHit(float[] d, Rng rng)
        {
            double phase = 0.0;
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                phase += 2.0 * Math.PI * 145.0 / Rate;
                float env = (float)Math.Exp(-24.0 * t);
                float click = i < 4 ? rng.Next() : 0f;
                lp = Low(lp, rng.Next(), 2200f);
                float x = (float)Math.Sin(phase) * 0.65f * env + click * 0.8f + lp * 0.35f * env;
                d[i] = (float)Math.Tanh(x * 1.8);
            }
        }

        static void TagSting(float[] d, Rng rng)
        {
            double p0 = 0.0;
            double p1 = 0.0;
            for (int i = 0; i < d.Length; i++)
            {
                float u = i / (float)(d.Length - 1);
                float hz = u < 0.42f ? 880f : 1318f;
                p0 += 2.0 * Math.PI * hz / Rate;
                p1 += 2.0 * Math.PI * hz * 2.0 / Rate;
                float env = (float)Math.Sin(Math.PI * u);
                float bell = (float)(Math.Sin(p0) * 0.75 + Math.Sin(p1) * 0.18);
                d[i] = bell * env;
            }
        }

        static void TagShimmer(float[] d, Rng rng)
        {
            double a = 0.0;
            double b = 0.0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float u = i / (float)(d.Length - 1);
                a += 2.0 * Math.PI * 1960.0 / Rate;
                b += 2.0 * Math.PI * 2480.0 / Rate;
                float trem = 0.65f + 0.35f * (float)Math.Sin(2.0 * Math.PI * 14.0 * t);
                float env = (float)Math.Sin(Math.PI * u);
                d[i] = (float)(Math.Sin(a) * 0.55 + Math.Sin(b) * 0.35) * trem * env;
            }
        }

        static void Stagger(float[] d, Rng rng)
        {
            double phase = 0.0;
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                phase += 2.0 * Math.PI * 78.0 / Rate;
                float body = (float)Math.Exp(-18.0 * t);
                lp = Low(lp, rng.Next(), 500f);
                float x = (float)Math.Sin(phase) * 0.8f * body + lp * 0.25f * body;
                d[i] = (float)Math.Tanh(x * 1.4);
            }
        }

        static void PadBoing(float[] d, Rng rng)
        {
            double phase = 0.0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float hz = 78f + 90f * (float)Math.Exp(-7.0 * t) * (float)Math.Cos(2.0 * Math.PI * 8.5 * t);
                if (hz < 40f) hz = 40f;
                phase += 2.0 * Math.PI * hz / Rate;
                float env = (float)Math.Exp(-3.2 * t);
                d[i] = (float)Math.Sin(phase) * env;
            }
        }

        static void ZipGrab(float[] d, Rng rng)
        {
            double a = 0.0;
            double b = 0.0;
            double c = 0.0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                a += 2.0 * Math.PI * 980.0 / Rate;
                b += 2.0 * Math.PI * 1460.0 / Rate;
                c += 2.0 * Math.PI * 2140.0 / Rate;
                float env = (float)Math.Exp(-26.0 * t);
                float click = i < 3 ? rng.Next() : 0f;
                d[i] = (float)(Math.Sin(a) * 0.4 + Math.Sin(b) * 0.3 + Math.Sin(c) * 0.2) * env + click * 0.5f;
            }
        }

        static void ZipLoop(float[] d, Rng rng)
        {
            double phase = 0.0;
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float hz = 510f + 12f * (float)Math.Sin(2.0 * Math.PI * 6.0 * t);
                phase += 2.0 * Math.PI * hz / Rate;
                lp = Low(lp, rng.Next(), 900f);
                d[i] = (float)Math.Sin(phase) * 0.7f + lp * 0.08f;
            }
        }

        static void ZipDrop(float[] d, Rng rng)
        {
            double phase = 0.0;
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float u = i / (float)(d.Length - 1);
                float t = i / (float)Rate;
                float hz = Lerp(520f, 160f, u);
                phase += 2.0 * Math.PI * hz / Rate;
                lp = Low(lp, rng.Next(), 1200f);
                float env = (float)Math.Exp(-10.0 * t);
                d[i] = (float)Math.Sin(phase) * 0.65f * env + lp * 0.25f * env;
            }
        }

        static void Countdown(float[] d, Rng rng)
        {
            SquareTone(d, 880f, 1800f);
        }

        static void RoundTick(float[] d, Rng rng)
        {
            SquareTone(d, 660f, 1400f);
        }

        static void RoundStart(float[] d, Rng rng)
        {
            Arp(d, new float[] { 523f, 659f, 784f });
        }

        static void RoundEnd(float[] d, Rng rng)
        {
            Sweep(d, 523f, 196f);
        }

        static void RoundWin(float[] d, Rng rng)
        {
            Arp(d, new float[] { 523f, 659f, 784f, 1046f });
        }

        static void RoundLose(float[] d, Rng rng)
        {
            Sweep(d, 392f, 130f);
        }

        static void StepConcrete(float[] d, Rng rng)
        {
            double phase = 0.0;
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                phase += 2.0 * Math.PI * 180.0 / Rate;
                float click = (float)Math.Exp(-70.0 * t);
                lp = Low(lp, rng.Next(), 2500f);
                d[i] = lp * click + (float)Math.Sin(phase) * 0.25f * (float)Math.Exp(-30.0 * t);
            }
        }

        static void StepGrass(float[] d, Rng rng)
        {
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float u = i / (float)(d.Length - 1);
                float t = i / (float)Rate;
                lp = Low(lp, rng.Next(), 500f);
                float env = (float)Math.Sin(Math.PI * u) * (float)Math.Exp(-8.0 * t);
                d[i] = lp * env;
            }
        }

        static void StepMetal(float[] d, Rng rng)
        {
            double a = 0.0;
            double b = 0.0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                a += 2.0 * Math.PI * 1540.0 / Rate;
                b += 2.0 * Math.PI * 2320.0 / Rate;
                float click = i < 2 ? rng.Next() : 0f;
                float env = (float)Math.Exp(-16.0 * t);
                d[i] = (float)(Math.Sin(a) * 0.55 + Math.Sin(b) * 0.3) * env + click * 0.8f;
            }
        }

        static void StepWood(float[] d, Rng rng)
        {
            double a = 0.0;
            double b = 0.0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                a += 2.0 * Math.PI * 210.0 / Rate;
                b += 2.0 * Math.PI * 420.0 / Rate;
                float env = (float)Math.Exp(-22.0 * t);
                float click = i < 3 ? rng.Next() * 0.4f : 0f;
                d[i] = (float)(Math.Sin(a) * 0.7 + Math.Sin(b) * 0.25) * env + click;
            }
        }

        static void UiMove(float[] d, Rng rng)
        {
            Tone(d, 640f, false);
        }

        static void UiConfirm(float[] d, Rng rng)
        {
            Sweep(d, 520f, 780f);
        }

        static void UiBack(float[] d, Rng rng)
        {
            Sweep(d, 620f, 320f);
        }

        static void UiClick(float[] d, Rng rng)
        {
            Tone(d, 720f, false);
        }

        static void UiHover(float[] d, Rng rng)
        {
            Tone(d, 540f, false);
        }

        static void Tone(float[] d, float hz, bool square)
        {
            double phase = 0.0;
            for (int i = 0; i < d.Length; i++)
            {
                float u = i / (float)(d.Length - 1);
                phase += 2.0 * Math.PI * hz / Rate;
                float s = (float)Math.Sin(phase);
                if (square) s = s >= 0f ? 1f : -1f;
                d[i] = s * (float)Math.Sin(Math.PI * u);
            }
        }

        static void SquareTone(float[] d, float hz, float cut)
        {
            double phase = 0.0;
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                phase += 2.0 * Math.PI * hz / Rate;
                float s = Math.Sin(phase) >= 0.0 ? 1f : -1f;
                lp = Low(lp, s, cut);
                d[i] = lp * (float)Math.Exp(-18.0 * t);
            }
        }

        static void Sweep(float[] d, float hz0, float hz1)
        {
            double phase = 0.0;
            for (int i = 0; i < d.Length; i++)
            {
                float u = i / (float)(d.Length - 1);
                float hz = Lerp(hz0, hz1, u);
                phase += 2.0 * Math.PI * hz / Rate;
                d[i] = (float)Math.Sin(phase) * (float)Math.Sin(Math.PI * u);
            }
        }

        static void Arp(float[] d, float[] notes)
        {
            double phase = 0.0;
            for (int i = 0; i < d.Length; i++)
            {
                float u = i / (float)(d.Length - 1);
                int n = (int)(u * notes.Length);
                if (n >= notes.Length) n = notes.Length - 1;
                float local = u * notes.Length - n;
                phase += 2.0 * Math.PI * notes[n] / Rate;
                float env = (float)Math.Sin(Math.PI * local);
                d[i] = (float)Math.Sin(phase) * env;
            }
        }

        static float Low(float state, float x, float cut)
        {
            float a = 1f - (float)Math.Exp(-2.0 * Math.PI * cut / Rate);
            if (a < 0f) a = 0f;
            if (a > 1f) a = 1f;
            return state + a * (x - state);
        }

        static float Band(ref float lp, ref float hp, float x, float lo, float hi)
        {
            lp = Low(lp, x, hi);
            hp = Low(hp, lp, lo);
            return lp - hp;
        }

        static float Lerp(float a, float b, float u)
        {
            return a + (b - a) * u;
        }

        static uint SaltOf(string rel)
        {
            uint h = 2166136261u;
            for (int i = 0; i < rel.Length; i++)
            {
                h ^= rel[i];
                h *= 16777619u;
            }
            return h == 0 ? 1u : h;
        }

        static void WriteBoth(string root, string rel, float[] data)
        {
            WriteOne(Path.Combine(root, "Assets", "Audio", rel.Replace('/', Path.DirectorySeparatorChar)), data, "Assets/Audio/" + rel);
            WriteOne(Path.Combine(root, "Assets", "Resources", "Audio", rel.Replace('/', Path.DirectorySeparatorChar)), data, "Assets/Resources/Audio/" + rel);
        }

        static void WriteOne(string path, float[] data, string guidKey)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var pcm = new byte[data.Length * 2];
            for (int i = 0; i < data.Length; i++)
            {
                float s = data[i];
                if (s > 1f) s = 1f;
                if (s < -1f) s = -1f;
                short v = (short)(s * 32767f);
                pcm[i * 2] = (byte)(v & 0xFF);
                pcm[i * 2 + 1] = (byte)((v >> 8) & 0xFF);
            }
            int dataBytes = pcm.Length;
            int riff = 36 + dataBytes;
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var w = new BinaryWriter(fs))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(riff);
                w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(Rate);
                w.Write(Rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(dataBytes);
                w.Write(pcm);
            }
            string meta = path + ".meta";
            if (!File.Exists(meta))
                File.WriteAllText(meta, Meta(guidKey));
        }

        static string Meta(string guidKey)
        {
            string guid = GuidFor(guidKey);
            return "fileFormatVersion: 2\n"
                + "guid: " + guid + "\n"
                + "AudioImporter:\n"
                + "  externalObjects: {}\n"
                + "  serializedVersion: 7\n"
                + "  defaultSettings:\n"
                + "    serializedVersion: 2\n"
                + "    loadType: 0\n"
                + "    sampleRateSetting: 0\n"
                + "    sampleRateOverride: 22050\n"
                + "    compressionFormat: 1\n"
                + "    quality: 1\n"
                + "    conversionMode: 0\n"
                + "    preloadAudioData: 1\n"
                + "  platformSettingOverrides: {}\n"
                + "  forceToMono: 1\n"
                + "  normalize: 1\n"
                + "  preloadAudioData: 1\n"
                + "  loadInBackground: 0\n"
                + "  ambisonic: 0\n"
                + "  3D: 1\n"
                + "  userData: \n"
                + "  assetBundleName: \n"
                + "  assetBundleVariant: \n";
        }

        static string GuidFor(string key)
        {
            byte[] hash;
            using (var sha = SHA256.Create())
                hash = sha.ComputeHash(Encoding.UTF8.GetBytes("tag-audio-v1:" + key));
            var sb = new StringBuilder(32);
            for (int i = 0; i < 16; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }

        static string FindRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir))
            {
                if (Directory.Exists(Path.Combine(dir, "Assets")) && Directory.Exists(Path.Combine(dir, "Tools")))
                    return dir;
                DirectoryInfo parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
