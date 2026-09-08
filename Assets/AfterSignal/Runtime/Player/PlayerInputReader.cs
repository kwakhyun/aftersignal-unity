using UnityEngine;
using UnityEngine.InputSystem;

namespace AfterSignal
{
    public struct ControlFrame
    {
        public Vector2 move, pointer, lookDelta;
        public float zoom, vertical;
        public bool boost, passenger, surrender;
        public bool journal;
        public bool look, cameraReset, run;
        public bool attack, grapple, jump, interact, dash, skill, guard, pause, reload, map, exit;
        public int weapon, weaponCycle;
        public static ControlFrame Empty => new ControlFrame
        {
            weapon = -1
        };
    }

    public sealed class PlayerInputReader : MonoBehaviour
    {
        InputActionAsset asset;
        InputAction move, pointer, attack, grapple, jump, interact, dash, skill, guard, pause, one, two, three;
        bool captured;
        Vector2 savedPointer;

        void ReleaseLook()
        {
            if (!captured) return;
            captured = false;
            Cursor.lockState = CursorLockMode.None;
            if (Application.isFocused && Mouse.current != null) Mouse.current.WarpCursorPosition(savedPointer);
        }

        void OnApplicationFocus(bool focus) { if (!focus) ReleaseLook(); }
        void LateUpdate()
        {
            var game = GameDirector.Instance;
            if (captured && (!game || !game.CameraRig || !game.CameraRig.CanLook)) ReleaseLook();
        }
        public ControlFrame Frame { get; private set; }
        public bool ExternalControl { get; set; }
        public ControlFrame ExternalFrame { get; set; }
        public string Diagnostics => $"asset={asset != null}, action={move?.enabled}, controls={move?.controls.Count}, value={move?.ReadValue<Vector2>()}";

        void Awake()
        {
            asset = Instantiate(Resources.Load<InputActionAsset>("Controls"));
            move = asset.FindAction("Move", true);
            pointer = asset.FindAction("Point", true);
            attack = asset.FindAction("Attack", true);
            grapple = asset.FindAction("Grapple", true);
            jump = asset.FindAction("Jump", true);
            interact = asset.FindAction("Interact", true);
            dash = asset.FindAction("Dash", true);
            skill = asset.FindAction("Skill", true);
            guard = asset.FindAction("Guard", true);
            pause = asset.FindAction("Pause", true);
            one = asset.FindAction("Katana", true);
            two = asset.FindAction("Greatsword", true);
            three = asset.FindAction("Pistol", true);
            Frame = ControlFrame.Empty;
        }

        void OnEnable()
        {
            if (asset)
                asset.Enable();
        }

        void OnDisable()
        {
            ReleaseLook();
            if (asset)
                asset.Disable();
            Frame = ControlFrame.Empty;
        }

        void OnDestroy()
        {
            if (asset)
                Destroy(asset);
        }

        public ControlFrame Read()
        {
            if (ExternalControl)
            { ReleaseLook(); return Frame = ExternalFrame; }
            var game = GameDirector.Instance;
            bool held = Application.isFocused && Mouse.current != null  && game && game.CameraRig && game.CameraRig.CanLook;
            bool starting = held && !captured;
            if (starting) { savedPointer = pointer.ReadValue<Vector2>(); captured = true; Cursor.lockState = CursorLockMode.Locked; }
            if (!held) ReleaseLook();
            Frame = new ControlFrame
            {
                boost = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed),
                passenger = Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame,
                surrender = Keyboard.current != null && Keyboard.current.hKey.isPressed,
                vertical = Keyboard.current == null ? 0 : (Keyboard.current.spaceKey.isPressed ? 1 : 0) - (Keyboard.current.leftCtrlKey.isPressed ? 1 : 0),
                run = move.ReadValue<Vector2>().sqrMagnitude > .04f,
                look = held,
                lookDelta = held && !starting ? Vector2.ClampMagnitude(Mouse.current.delta.ReadValue(), 2000) : Vector2.zero,
                cameraReset = Keyboard.current != null && Keyboard.current.homeKey.wasPressedThisFrame,
                move = move.ReadValue<Vector2>(),
                pointer = new Vector2(Screen.width*.5f,Screen.height*.5f),
                zoom = held ? Mouse.current.scroll.ReadValue().y : 0,
                journal = Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame,
                attack = attack.IsPressed(),
                grapple = grapple.IsPressed(),
                jump = jump.WasPressedThisFrame(),
                interact = interact.WasPressedThisFrame(),
                dash = dash.WasPressedThisFrame(),
                skill = skill.WasPressedThisFrame(),
                guard = guard.IsPressed(),
                pause = pause.WasPressedThisFrame(),
                reload = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame,
                map = Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame,
                exit = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame,
                weaponCycle = 0,
                weapon = one.WasPressedThisFrame() ? 0 : two.WasPressedThisFrame() ? 1 : three.WasPressedThisFrame() ? 2 : Keyboard.current==null?-1:Keyboard.current.digit4Key.wasPressedThisFrame?3:Keyboard.current.digit5Key.wasPressedThisFrame?4:Keyboard.current.digit6Key.wasPressedThisFrame?5:Keyboard.current.digit7Key.wasPressedThisFrame?6:-1
            };
            return Frame;
        }
    }
}
