using UnityEngine;
using UnityEngine.InputSystem;

namespace AfterSignal
{
    public struct ControlFrame
    {
        public Vector2 move, pointer;
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
                return Frame = ExternalFrame;
            Frame = new ControlFrame
            {
                move = move.ReadValue<Vector2>(),
                pointer = pointer.ReadValue<Vector2>(),
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
                weaponCycle = Mouse.current != null && Mathf.Abs(Mouse.current.scroll.ReadValue().y) > .01f ? (Mouse.current.scroll.ReadValue().y > 0 ? 1 : -1) : 0,
                weapon = one.WasPressedThisFrame() ? 0 : two.WasPressedThisFrame() ? 1 : three.WasPressedThisFrame() ? 2 : -1
            };
            return Frame;
        }
    }
}
