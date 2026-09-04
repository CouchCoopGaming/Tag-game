using Tag.Gameplay;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Simple CC wander so DummyRunner leaves trails and is useful for TrailTag smoke tests.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DummyPatrol : MonoBehaviour
    {
        [SerializeField] float speed = 4.2f;
        [SerializeField] float radius = 5.5f;
        [SerializeField] float turnSpeed = 120f;
        [SerializeField] Vector3 centerOffset = Vector3.zero;

        CharacterController _cc;
        ItController _it;
        Vector3 _center;
        float _angle;
        float _gravity;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _it = GetComponent<ItController>();
            _center = transform.position + centerOffset;
            _angle = Random.Range(0f, 360f);
        }

        void Update()
        {
            if (_it != null && _it.IsEliminated) return;
            if (_cc == null || !_cc.enabled) return;

            _angle += (speed / Mathf.Max(0.5f, radius)) * Mathf.Rad2Deg * Time.deltaTime;
            Vector3 target = _center + new Vector3(Mathf.Cos(_angle * Mathf.Deg2Rad), 0f, Mathf.Sin(_angle * Mathf.Deg2Rad)) * radius;
            Vector3 to = target - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
            }

            Vector3 move = transform.forward * speed;
            if (_cc.isGrounded) _gravity = -2f;
            else _gravity += -20f * Time.deltaTime;
            move.y = _gravity;
            _cc.Move(move * Time.deltaTime);
        }
    }
}
