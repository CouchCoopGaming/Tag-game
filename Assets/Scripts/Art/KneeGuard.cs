using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Runs after every pose write (DummyLocomotor and any later overlay) and keeps each knee a
    /// forward-only hinge, 0..DummyLocomotor.KneeMaxFlex. See DummyLocomotor.GuardKnees.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class KneeGuard : MonoBehaviour
    {
        DummyLocomotor _loco;

        public static KneeGuard On(DummyLocomotor loco)
        {
            KneeGuard g = loco.GetComponent<KneeGuard>();
            if (g == null) g = loco.gameObject.AddComponent<KneeGuard>();
            g._loco = loco;
            return g;
        }

        void LateUpdate()
        {
            if (_loco != null && _loco.isActiveAndEnabled) _loco.GuardKnees();
        }
    }
}
