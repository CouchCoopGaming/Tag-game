using Tag.Art;
using Tag.Experimental;
using Tag.Gameplay;
using TagArena.Movement;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// One kit on a pawn. LateUpdate only steps the pooled sim.
    /// </summary>
    [DefaultExecutionOrder(140)]
    public sealed class FxKitHost : MonoBehaviour
    {
        FxKitSim _sim;

        public static void Ensure(GameObject host)
        {
            if (host == null) return;
            if (host.GetComponent<FxKitHost>() == null)
                host.AddComponent<FxKitHost>();
        }

        void Awake()
        {
            _sim = new FxKitSim();
            _sim.Motor = GetComponent<PlayerMotor>();
            _sim.Loco = GetComponent<DummyLocomotor>();
            _sim.Grapple = GetComponent<ExperimentalGrapple>();
            _sim.It = GetComponent<ItController>();
            _sim.Role = GetComponent<TagRole>();
            _sim.Build(transform);
        }

        public void BindCamera()
        {
            if (_sim != null) _sim.BindCamera();
        }

        void LateUpdate()
        {
            if (_sim == null) return;
            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            if (dt > 0.05f) dt = 0.05f;
            _sim.Tick(dt);
        }

        void OnDisable()
        {
            if (_sim != null) _sim.Hide();
        }
    }
}
