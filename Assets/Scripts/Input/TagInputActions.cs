#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tag.Input
{
    /// <summary>
    /// New Input System map for one local slot (keyboard+mouse on P0, plus that pad).
    /// Pattern from Landon's Input System ref (actions + bindings) — not an FPS Player look.
    /// </summary>
    public sealed class TagInputActions : IDisposable
    {
        readonly InputActionAsset _asset;
        readonly InputAction _move;
        readonly InputAction _look;
        readonly InputAction _jump;
        readonly InputAction _sprint;
        readonly InputAction _slide;
        readonly InputAction _punch;
        readonly InputAction _airDash;

        public bool IsValid => _asset != null && _move != null;

        TagInputActions(InputActionAsset asset, InputAction move, InputAction look, InputAction jump,
            InputAction sprint, InputAction slide, InputAction punch, InputAction airDash)
        {
            _asset = asset;
            _move = move;
            _look = look;
            _jump = jump;
            _sprint = sprint;
            _slide = slide;
            _punch = punch;
            _airDash = airDash;
        }

        public static TagInputActions Bind(int playerIndex)
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = $"TagInput_P{playerIndex}";
            var map = asset.AddActionMap("Gameplay");

            // AddAction(name, type) — do not pass expectedControlType (removed/renamed on some Input System versions).
            var move = map.AddAction("Move", InputActionType.Value);
            var look = map.AddAction("Look", InputActionType.Value);
            var jump = map.AddAction("Jump", InputActionType.Button);
            var sprint = map.AddAction("Sprint", InputActionType.Button);
            var slide = map.AddAction("Slide", InputActionType.Button);
            var punch = map.AddAction("Punch", InputActionType.Button);
            var airDash = map.AddAction("AirDash", InputActionType.Button);
            move.expectedControlType = "Vector2";
            look.expectedControlType = "Vector2";

            if (playerIndex == 0)
            {
                move.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
                look.AddBinding("<Mouse>/delta");
                jump.AddBinding("<Keyboard>/space");
                sprint.AddBinding("<Keyboard>/leftShift");
                sprint.AddBinding("<Keyboard>/rightShift");
                slide.AddBinding("<Keyboard>/leftCtrl");
                slide.AddBinding("<Keyboard>/c");
                punch.AddBinding("<Mouse>/leftButton");
                airDash.AddBinding("<Keyboard>/leftAlt");
                airDash.AddBinding("<Keyboard>/q");
            }
            else if (playerIndex == 1)
            {
                move.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow")
                    .With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow")
                    .With("Right", "<Keyboard>/rightArrow");
                look.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/i")
                    .With("Down", "<Keyboard>/k")
                    .With("Left", "<Keyboard>/j")
                    .With("Right", "<Keyboard>/l");
                jump.AddBinding("<Keyboard>/rightCtrl");
                sprint.AddBinding("<Keyboard>/rightShift");
                slide.AddBinding("<Keyboard>/slash");
                slide.AddBinding("<Keyboard>/period");
                punch.AddBinding("<Keyboard>/enter");
                punch.AddBinding("<Keyboard>/rightBracket");
                airDash.AddBinding("<Keyboard>/rightAlt");
                airDash.AddBinding("<Keyboard>/quote");
            }

            move.AddBinding("<Gamepad>/leftStick");
            look.AddBinding("<Gamepad>/rightStick");
            jump.AddBinding("<Gamepad>/buttonSouth");
            sprint.AddBinding("<Gamepad>/leftStickPress");
            sprint.AddBinding("<Gamepad>/leftShoulder");
            slide.AddBinding("<Gamepad>/buttonEast");
            punch.AddBinding("<Gamepad>/buttonWest");
            airDash.AddBinding("<Gamepad>/rightShoulder");

            var devices = DevicesFor(playerIndex);
            if (devices != null && devices.Length > 0)
                asset.devices = devices;

            map.Enable();
            return new TagInputActions(asset, move, look, jump, sprint, slide, punch, airDash);
        }

        static InputDevice[] DevicesFor(int playerIndex)
        {
            var list = new List<InputDevice>();
            if (playerIndex == 0)
            {
                if (Keyboard.current != null) list.Add(Keyboard.current);
                if (Mouse.current != null) list.Add(Mouse.current);
            }
            else if (playerIndex == 1 && Keyboard.current != null)
                list.Add(Keyboard.current);

            if (Gamepad.all.Count > playerIndex)
                list.Add(Gamepad.all[playerIndex]);
            return list.Count > 0 ? list.ToArray() : null;
        }

        public void Read(out Vector2 move, out Vector2 look, out bool sprint, out bool jump,
            out bool slideHeld, out bool slidePressed, out bool punch, out bool airDash)
        {
            move = Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);
            look = _look.ReadValue<Vector2>();
            if (_look.activeControl != null && _look.activeControl.device is Mouse)
                look *= 0.1f;
            else if (_look.activeControl != null && _look.activeControl.device is Gamepad)
                look *= 8f;
            else
                look *= 2.5f;

            sprint = _sprint.IsPressed();
            jump = _jump.WasPressedThisFrame();
            slideHeld = _slide.IsPressed();
            slidePressed = _slide.WasPressedThisFrame();
            punch = _punch.WasPressedThisFrame();
            airDash = _airDash.WasPressedThisFrame();
        }

        public void Dispose()
        {
            if (_asset != null)
            {
                _asset.Disable();
                UnityEngine.Object.Destroy(_asset);
            }
        }
    }
}
#endif
