using UnityEngine;
using UnityEngine.InputSystem;

namespace Starfall
{
    // Keyboard/mouse only. Sampling stays out of the physics and presentation code.
    public sealed class PlayerInputReader : System.IDisposable
    {
        readonly InputAction move = new InputAction("Move", InputActionType.Value);
        readonly InputAction attack = new InputAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
        readonly InputAction dodge = new InputAction("Dodge", InputActionType.Button, "<Keyboard>/space");
        readonly InputAction pickup = new InputAction("Pickup", InputActionType.Button, "<Keyboard>/e");
        readonly InputAction interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/f");
        readonly InputAction pause = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        readonly InputAction map = new InputAction("Map", InputActionType.Button, "<Keyboard>/tab");
        readonly InputAction active = new InputAction("Active", InputActionType.Button, "<Keyboard>/q");
        readonly InputAction slot1 = new InputAction("Pistol", InputActionType.Button, "<Keyboard>/1");
        readonly InputAction slot2 = new InputAction("Special", InputActionType.Button, "<Keyboard>/2");
        bool requireAttackRelease;
        bool requirePickupRelease, requireInteractRelease;
        public Vector2 Move { get; private set; }
        public Vector2 Pointer { get; private set; }
        public bool Attack { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool PickupPressed { get; private set; }
        public bool PausePressed { get; private set; }
        public bool MapPressed { get; private set; }
        public bool ActivePressed { get; private set; }
        public bool PistolPressed { get; private set; }
        public bool SpecialPressed { get; private set; }
        public PlayerInputReader()
        {
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.Enable(); attack.Enable(); dodge.Enable(); pickup.Enable(); interact.Enable(); pause.Enable(); map.Enable();
            active.Enable(); slot1.Enable(); slot2.Enable();
            Flush();
        }
        public void Sample(bool gameplay)
        {
            PausePressed = pause.WasPressedThisFrame(); MapPressed = map.WasPressedThisFrame();
            Pointer = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
            if (!attack.IsPressed()) requireAttackRelease = false;
            if (!pickup.IsPressed()) requirePickupRelease = false;
            if (!interact.IsPressed()) requireInteractRelease = false;
            Move = gameplay ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1) : Vector2.zero;
            Attack = gameplay && !requireAttackRelease && attack.IsPressed();
            DodgePressed = gameplay && dodge.WasPressedThisFrame();
            InteractPressed = gameplay && !requireInteractRelease && interact.WasPressedThisFrame();
            PickupPressed = gameplay && !requirePickupRelease && pickup.WasPressedThisFrame();
            ActivePressed = gameplay && active.WasPressedThisFrame(); PistolPressed = gameplay && slot1.WasPressedThisFrame(); SpecialPressed = gameplay && slot2.WasPressedThisFrame();
        }
        public void Flush()
        {
            Move = Vector2.zero; Attack = DodgePressed = InteractPressed = PickupPressed = false; requireAttackRelease = true;
            requirePickupRelease = pickup.IsPressed(); requireInteractRelease = interact.IsPressed();
            ActivePressed = PistolPressed = SpecialPressed = false;
        }
        public void Dispose() { move.Dispose(); attack.Dispose(); dodge.Dispose(); pickup.Dispose(); interact.Dispose(); pause.Dispose(); map.Dispose(); active.Dispose(); slot1.Dispose(); slot2.Dispose(); }
    }
}
