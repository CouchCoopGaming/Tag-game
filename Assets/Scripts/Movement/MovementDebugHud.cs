using Tag.Input;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tag.Movement
{
    /// <summary>
    /// On-screen m/s + locomotion state. Toggle with F3 (P0 only to avoid couch overlap).
    /// Auto-added by PlayerMotor.
    /// </summary>
    public class MovementDebugHud : MonoBehaviour
    {
        PlayerMotor _motor;
        PlayerInputReader _input;
        bool _visible = true;
        GUIStyle _box;
        GUIStyle _label;

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _input = GetComponent<PlayerInputReader>();
        }

        void Update()
        {
            if (!IsLocalPrimary()) return;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
                _visible = !_visible;
#else
            if (UnityEngine.Input.GetKeyDown(KeyCode.F3))
                _visible = !_visible;
#endif
        }

        bool IsLocalPrimary()
        {
            if (_input == null) return true;
            return _input.PlayerIndex == 0;
        }

        void OnGUI()
        {
            if (_motor == null) return;
            var tuning = _motor.Tuning;
            if (tuning != null && !tuning.showDebugHud) return;
            if (!_visible || !IsLocalPrimary()) return;

            EnsureStyles();
            float x = 12f;
            float y = 12f;
            GUI.Box(new Rect(x, y, 280, 168), "", _box);

            float spd = _motor.HorizontalSpeed;
            float vy = _motor.Velocity.y;
            string line1 = $"MOVE  {spd:0.00} m/s   vy {vy:+0.00;-0.00}";
            string line2 = $"STATE {_motor.LocomotionState}   dash {_motor.AirDodgeChargesLeft}";
            string line3 = tuning != null
                ? $"walk {tuning.walkSpeed:0.0}  sprint {tuning.sprintSpeed:0.0}  slide {tuning.slidePeakSpeed:0.0}"
                : "";
            string line4 = tuning != null
                ? $"dash {tuning.airDodgeSpeed:0.0} m/s × {tuning.airDodgeLock * 1000f:0} ms  → {MovementKinematics.EffectiveAirDashDistance(tuning):0.00} m"
                : "";
            string line5 = _motor.IsGrounded ? "grounded" : "air";
            if (_motor.IsGrounded) line5 += $"  slope {_motor.SlopeAngleDeg:0}°";
            if (tuning != null && tuning.autoSprint) line5 += "   auto-sprint";
            if (_motor.HasAirDodgeIFrames) line5 += "   i-frames";

            GUI.Label(new Rect(x + 8, y + 6, 264, 22), line1, _label);
            GUI.Label(new Rect(x + 8, y + 30, 264, 22), line2, _label);
            GUI.Label(new Rect(x + 8, y + 54, 264, 22), line3, _label);
            GUI.Label(new Rect(x + 8, y + 78, 264, 22), line4, _label);
            GUI.Label(new Rect(x + 8, y + 102, 264, 22), line5, _label);
            GUI.Label(new Rect(x + 8, y + 126, 264, 18), "3rd-person CC · F3 HUD", _label);
        }

        void EnsureStyles()
        {
            if (_box != null) return;
            _box = new GUIStyle(GUI.skin.box);
            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            _label.normal.textColor = Color.white;
        }
    }
}
