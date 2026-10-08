using Tag.Settings;
using UnityEngine;

namespace Tag.FX
{
    public enum FxBurstKind
    {
        Roll = 0,
        Land = 1,
        WallScuff = 2,
        VaultPuff = 3,
        Run = 4
    }

    /// <summary>
    /// One pooled particle system per pawn. Bursts emit from the prebuilt system.
    /// Reduced flashing skips the burst. The motor is not written.
    /// </summary>
    public sealed class FxBurstPool : MonoBehaviour
    {
        ParticleSystem _ps;
        ParticleSystem.MainModule _main;
        bool _ready;

        public static FxBurstPool Ensure(Transform host)
        {
            if (host == null) return null;
            FxBurstPool have = host.GetComponent<FxBurstPool>();
            if (have != null) return have;
            Transform child = host.Find("FxBurstPool");
            if (child != null)
            {
                have = child.GetComponent<FxBurstPool>();
                if (have != null) return have;
            }
            var go = new GameObject("FxBurstPool");
            go.transform.SetParent(host, false);
            return go.AddComponent<FxBurstPool>();
        }

        void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            if (_ps == null) _ps = gameObject.AddComponent<ParticleSystem>();
            _main = _ps.main;
            _main.playOnAwake = false;
            _main.loop = false;
            _main.duration = 0.45f;
            _main.startLifetime = 0.38f;
            _main.startSpeed = 2.2f;
            _main.startSize = 0.18f;
            _main.maxParticles = 64;
            _main.simulationSpace = ParticleSystemSimulationSpace.World;
            _main.gravityModifier = 0.55f;
            var emission = _ps.emission;
            emission.enabled = false;
            var shape = _ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.12f;
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _ready = true;
        }

        public static bool Calmed()
        {
            GameSettings settings = GameSettings.Current;
            if (settings == null) return false;
            if (settings.AnyReduceFlash()) return true;
            return settings.Effects <= 0;
        }

        public void Play(FxBurstKind kind, Vector3 worldPos)
        {
            if (!_ready || _ps == null) return;
            if (Calmed()) return;
            Apply(kind);
            _ps.transform.position = worldPos;
            int emit = Count(kind);
            if (GameSettings.Current != null && GameSettings.Current.Effects == FxAmount.Low)
            {
                emit = emit / 2;
                if (emit < 1) emit = 1;
            }
            _ps.Emit(emit);
        }

        public void PlayShaped(FxBurstKind kind, Vector3 worldPos, DustLook.Puff puff)
        {
            if (!_ready || _ps == null) return;
            if (Calmed()) return;
            if (puff.Count <= 0) return;
            Apply(kind);
            _main.startSize = puff.Size;
            _main.startLifetime = puff.Life > 0.05f ? puff.Life : 0.05f;
            float a = puff.Opacity;
            if (a < 0f) a = 0f;
            if (a > 1f) a = 1f;
            _main.startColor = new Color(puff.R, puff.G, puff.B, a);
            if (puff.Splash != 0)
            {
                _main.startSpeed = 2.4f;
                _main.gravityModifier = 1.1f;
            }
            else if (puff.Spark != 0)
            {
                _main.startSpeed = 1.8f;
                _main.gravityModifier = 0.15f;
            }
            else
            {
                _main.gravityModifier = 0.55f;
            }
            _ps.transform.position = worldPos;
            int n = puff.Count;
            float dens = FxAmount.Density(GameSettings.Current);
            if (dens < 0.99f)
            {
                n = (int)(n * dens + 0.001f);
                if (n < 1) n = 1;
            }
            if (n > 12) n = 12;
            _ps.Emit(n);
        }

        void Apply(FxBurstKind kind)
        {
            if (kind == FxBurstKind.Roll)
            {
                _main.startSpeed = 2.6f;
                _main.startSize = 0.24f;
                _main.startColor = new Color(0.62f, 0.52f, 0.38f, 0.82f);
            }
            else if (kind == FxBurstKind.Land)
            {
                _main.startSpeed = 1.6f;
                _main.startSize = 0.16f;
                _main.startColor = new Color(0.70f, 0.64f, 0.52f, 0.7f);
            }
            else if (kind == FxBurstKind.WallScuff)
            {
                _main.startSpeed = 1.2f;
                _main.startSize = 0.10f;
                _main.startColor = new Color(0.75f, 0.72f, 0.66f, 0.65f);
            }
            else if (kind == FxBurstKind.Run)
            {
                _main.startSpeed = 1.5f;
                _main.startSize = 0.16f;
                _main.startColor = new Color(0.70f, 0.62f, 0.42f, 0.7f);
            }
            else
            {
                _main.startSpeed = 1.4f;
                _main.startSize = 0.12f;
                _main.startColor = new Color(0.78f, 0.74f, 0.66f, 0.6f);
            }
        }

        static int Count(FxBurstKind kind)
        {
            if (kind == FxBurstKind.Roll) return 12;
            if (kind == FxBurstKind.Land) return 7;
            if (kind == FxBurstKind.WallScuff) return 4;
            return 5;
        }
    }
}
