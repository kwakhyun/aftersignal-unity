using UnityEngine;
using UnityEngine.InputSystem;

namespace AfterSignal
{
    public struct ControlFrame
    {
        public Vector2 move, pointer, lookDelta;
        public float zoom, vertical;
        public bool boost, passenger, surrender, vehicleView, secondaryFire, horn, summonBike;
        public bool journal, rescueReport, panorama;
        public bool look, cameraReset, run;
        public bool attack, grapple, jump, interact, dash, skill, guard, pause, reload, map, exit;
        public int weapon, weaponCycle;
        public static ControlFrame Empty => new ControlFrame
        {
            weapon = -1
        };
    }

    /// <summary>
    /// Reads every gameplay control through the Controls input-action asset (keyboard/mouse and gamepad), so all
    /// keys can be rebound. Binding overrides are stored in PlayerPrefs and shared by every scene.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public const string BindingsKey = "AFTERSIGNAL.Unity.Bindings";
        /// <summary>Gamepad right-stick look speed in "mouse pixels" per second at full deflection.</summary>
        const float StickLookSpeed = 900;
        InputActionAsset asset;
        InputAction move, pointer, attack, grapple, jump, interact, dash, skill, guard, pause, look, lookStick, zoom, boost, ascend, descend, vehicleView, horn, summonBike, secondaryFire, passenger, surrender, cameraReset, journal, reload, map, exit, weaponNext, rescueReport, panorama;
        readonly InputAction[] weapons = new InputAction[7];
        bool captured;
        Vector2 savedPointer;
        public InputActionAsset Actions => asset;

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
            var overrides = PlayerPrefs.GetString(BindingsKey, "");
            if (!string.IsNullOrEmpty(overrides))
            {
                try { asset.LoadBindingOverridesFromJson(overrides); }
                catch (System.Exception error) { Debug.LogWarning("AFTERSIGNAL: ignored invalid key bindings · " + error.Message); }
            }
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
            weapons[0] = asset.FindAction("Katana", true);
            weapons[1] = asset.FindAction("Greatsword", true);
            weapons[2] = asset.FindAction("Pistol", true);
            for (int i = 3; i < weapons.Length; i++)
                weapons[i] = asset.FindAction("Weapon" + (i + 1), true);
            look = asset.FindAction("Look", true);
            lookStick = asset.FindAction("LookStick", true);
            zoom = asset.FindAction("Zoom", true);
            boost = asset.FindAction("Boost", true);
            ascend = asset.FindAction("Ascend", true);
            descend = asset.FindAction("Descend", true);
            vehicleView = asset.FindAction("VehicleView", true);
            horn = asset.FindAction("Horn", true);
            summonBike = asset.FindAction("SummonBike", true);
            secondaryFire = asset.FindAction("SecondaryFire", true);
            passenger = asset.FindAction("Passenger", true);
            surrender = asset.FindAction("Surrender", true);
            cameraReset = asset.FindAction("CameraReset", true);
            journal = asset.FindAction("Journal", true);
            reload = asset.FindAction("Reload", true);
            map = asset.FindAction("Map", true);
            exit = asset.FindAction("Exit", true);
            weaponNext = asset.FindAction("WeaponNext", true);
            rescueReport = asset.FindAction("RescueReport", true);
            panorama = asset.FindAction("Panorama", true);
            KeyHints.Attach(asset);
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
            KeyHints.Detach(asset);
            if (asset)
                Destroy(asset);
        }

        /// <summary>Persists the current binding overrides (called by the settings panel after a rebind or reset).</summary>
        public void SaveBindings()
        {
            PlayerPrefs.SetString(BindingsKey, asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
            KeyHints.Refresh();
        }

        public ControlFrame Read()
        {
            if (ExternalControl)
            { ReleaseLook(); return Frame = ExternalFrame; }
            var game = GameDirector.Instance;
            bool canLook = Application.isFocused && game && game.CameraRig && game.CameraRig.CanLook;
            bool held = canLook && Mouse.current != null;
            bool starting = held && !captured;
            if (starting) { savedPointer = pointer.ReadValue<Vector2>(); captured = true; Cursor.lockState = CursorLockMode.Locked; }
            if (!held) ReleaseLook();
            Vector2 stick = canLook ? lookStick.ReadValue<Vector2>() : Vector2.zero;
            Vector2 mouseDelta = held && !starting ? Vector2.ClampMagnitude(look.ReadValue<Vector2>(), 2000) : Vector2.zero;
            Vector2 stickDelta = stick.sqrMagnitude > .01f ? stick * StickLookSpeed * Time.unscaledDeltaTime : Vector2.zero;
            int weapon = -1;
            for (int i = 0; i < weapons.Length && weapon < 0; i++)
                if (weapons[i].WasPressedThisFrame()) weapon = i;
            Vector2 moveValue = move.ReadValue<Vector2>();
            KeyHints.Observe();
            Frame = new ControlFrame
            {
                boost = boost.IsPressed(),
                vehicleView = vehicleView.WasPressedThisFrame(),
                horn = horn.WasPressedThisFrame(),
                summonBike = summonBike.WasPressedThisFrame(),
                secondaryFire = secondaryFire.IsPressed(),
                passenger = passenger.WasPressedThisFrame(),
                surrender = surrender.IsPressed(),
                vertical = (ascend.IsPressed() ? 1 : 0) - (descend.IsPressed() ? 1 : 0),
                run = moveValue.sqrMagnitude > .04f,
                look = held || canLook && stickDelta != Vector2.zero,
                lookDelta = mouseDelta + stickDelta,
                // R3 is both CameraReset and VehicleView: in a vehicle a shared press belongs to VehicleView; HOME alone still resets.
                cameraReset = cameraReset.WasPressedThisFrame() && !(vehicleView.WasPressedThisFrame() && UrbanSimulation.Instance && UrbanSimulation.Instance.Driving),
                move = moveValue,
                pointer = new Vector2(Screen.width * .5f, Screen.height * .5f),
                zoom = held ? zoom.ReadValue<Vector2>().y : 0,
                journal = journal.WasPressedThisFrame(),
                rescueReport = rescueReport.WasPressedThisFrame(),
                panorama = panorama.WasPressedThisFrame(),
                attack = attack.IsPressed(),
                grapple = grapple.IsPressed(),
                jump = jump.WasPressedThisFrame(),
                interact = interact.WasPressedThisFrame(),
                dash = dash.WasPressedThisFrame(),
                skill = skill.WasPressedThisFrame(),
                guard = guard.IsPressed(),
                pause = pause.WasPressedThisFrame(),
                reload = reload.WasPressedThisFrame(),
                map = map.WasPressedThisFrame(),
                exit = exit.WasPressedThisFrame(),
                weaponCycle = weaponNext.WasPressedThisFrame() ? 1 : 0,
                weapon = weapon
            };
            return Frame;
        }
    }
}
