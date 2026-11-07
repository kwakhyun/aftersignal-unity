using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AfterSignal
{
    /// <summary>
    /// Keeps on-screen key hints truthful. Hint strings are authored with the default keyboard keys ("E 상호작용",
    /// "F 하차", "SHIFT"…). When the player rebinds a key, or is playing with a gamepad, <see cref="Apply"/> rewrites
    /// those tokens to the current binding. With default bindings on keyboard/mouse the text is returned unchanged.
    /// </summary>
    public static class KeyHints
    {
        public const string KeyboardGroup = "Keyboard&Mouse", GamepadGroup = "Gamepad";
        // Default key token → action. Movement tokens map to the Move action.
        static readonly (string token, string action)[] tokens =
        {
            ("WASD", "Move"), ("W/S", "Move"), ("A/D", "Move"), ("SHIFT", "Dash"), ("SPACE", "Jump"), ("CTRL", "Guard"), ("HOME", "CameraReset"), ("ESC", "Pause"),
            ("LMB", "Attack"), ("RMB", "Grapple"), ("좌클릭", "Attack"), ("우클릭", "Grapple"), ("F6", "RescueReport"),
            ("E", "Interact"), ("F", "Exit"), ("G", "Passenger"), ("M", "Map"), ("J", "Journal"), ("R", "Reload"), ("Q", "Skill"), ("C", "VehicleView"), ("B", "Horn"), ("V", "SummonBike"), ("H", "Surrender"),
        };
        static readonly Regex pattern = new Regex(@"(?<![A-Za-z0-9가-힣]|\d\s)(WASD|W/S|A/D|SHIFT|SPACE|CTRL|HOME|ESC|LMB|RMB|F6|[EFGMJRQCBVH])(?![A-Za-z0-9])|좌클릭|우클릭", RegexOptions.CultureInvariant);
        static readonly Dictionary<string, string> gamepadNames = new()
        {
            { "buttonSouth", "[A]" }, { "buttonEast", "[B]" }, { "buttonWest", "[X]" }, { "buttonNorth", "[Y]" },
            { "leftShoulder", "[LB]" }, { "rightShoulder", "[RB]" }, { "leftTrigger", "[LT]" }, { "rightTrigger", "[RT]" },
            { "start", "[START]" }, { "select", "[VIEW]" }, { "leftStickPress", "[LS 누름]" }, { "rightStickPress", "[RS 누름]" },
            { "dpad/up", "[↑]" }, { "dpad/down", "[↓]" }, { "dpad/left", "[←]" }, { "dpad/right", "[→]" }, { "leftStick", "[LS]" }, { "rightStick", "[RS]" },
        };
        static readonly Dictionary<string, string> cache = new();
        static readonly Dictionary<string, string> replacements = new();
        static InputActionAsset asset;
        static bool customized;

        /// <summary>True after the last meaningful input came from a gamepad.</summary>
        public static bool UsingGamepad { get; private set; }

        public static void Attach(InputActionAsset actions) { asset = actions; Refresh(); }
        public static void Detach(InputActionAsset actions) { if (asset == actions) asset = null; Refresh(); }

        public static void Refresh()
        {
            cache.Clear();
            replacements.Clear();
            customized = false;
            if (!asset) return;
            foreach (var (token, actionName) in tokens)
            {
                var action = asset.FindAction(actionName);
                if (action == null) continue;
                string keyboard = Display(action, KeyboardGroup);
                if (!string.IsNullOrEmpty(keyboard) && !Same(keyboard, token) && actionName != "Move" && token != "좌클릭" && token != "우클릭")
                    customized = true;
                replacements[token + "|kb"] = string.IsNullOrEmpty(keyboard) || actionName == "Move" ? token : keyboard;
                string pad = Display(action, GamepadGroup);
                replacements[token + "|pad"] = string.IsNullOrEmpty(pad) ? token : pad;
            }
        }

        /// <summary>Track the device family from actual input so hints follow the controller in use.</summary>
        public static void Observe()
        {
            var pad = Gamepad.current;
            bool padActive = pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame || pad.leftStick.ReadValue().sqrMagnitude > .25f || pad.rightStick.ReadValue().sqrMagnitude > .25f);
            bool keyboardActive = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame || Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.delta.ReadValue().sqrMagnitude > 25);
            if (padActive && !UsingGamepad) { UsingGamepad = true; cache.Clear(); }
            else if (keyboardActive && UsingGamepad) { UsingGamepad = false; cache.Clear(); }
        }

        /// <summary>Display name of the current binding for an action ("E", "[X]"…), for menus.</summary>
        public static string For(string actionName, bool gamepad = false)
        {
            var action = asset ? asset.FindAction(actionName) : null;
            return action == null ? "" : Display(action, gamepad ? GamepadGroup : KeyboardGroup);
        }

        public static string Apply(string text)
        {
            if (string.IsNullOrEmpty(text) || !asset || !customized && !UsingGamepad) return text;
            if (cache.TryGetValue(text, out var done)) return done;
            string suffix = UsingGamepad ? "|pad" : "|kb";
            done = pattern.Replace(text, m => replacements.TryGetValue(m.Value + suffix, out var r) ? r : m.Value);
            if (cache.Count > 256) cache.Clear();
            cache[text] = done;
            return done;
        }

        static bool Same(string display, string token) => string.Equals(Normalize(display), Normalize(token), System.StringComparison.OrdinalIgnoreCase);
        static string Normalize(string s) => s.Replace("LEFT ", "").Replace("Left ", "").Replace(" ", "").Replace("ESCAPE", "ESC").Replace("Escape", "ESC").Replace("Control", "CTRL").Replace("Ctrl", "CTRL").Replace("LMB", "LeftButton").Replace("RMB", "RightButton");

        static string Display(InputAction action, string group)
        {
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isComposite || b.isPartOfComposite || string.IsNullOrEmpty(b.groups) || !b.groups.Contains(group)) continue;
                string path = string.IsNullOrEmpty(b.overridePath) ? b.path : b.overridePath;
                if (group == GamepadGroup)
                {
                    int slash = path.IndexOf('/');
                    string control = slash >= 0 ? path.Substring(slash + 1) : path;
                    return gamepadNames.TryGetValue(control, out var name) ? name : "[" + control + "]";
                }
                if (path.StartsWith("<Mouse>"))
                    return path.EndsWith("leftButton") ? "좌클릭" : path.EndsWith("rightButton") ? "우클릭" : path.EndsWith("middleButton") ? "휠 클릭" : action.GetBindingDisplayString(i);
                string shown = action.GetBindingDisplayString(i);
                return string.IsNullOrEmpty(shown) ? path : shown.ToUpperInvariant().Replace("LEFT SHIFT", "SHIFT").Replace("RIGHT SHIFT", "SHIFT").Replace("LEFT CTRL", "CTRL").Replace("LEFT CONTROL", "CTRL").Replace("ESCAPE", "ESC");
            }
            return "";
        }
    }
}
