using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    /// <summary>
    /// Shared IMGUI text for vehicle/swimming readouts. Styles are cached (OnGUI runs several times per frame),
    /// sizes follow the screen height and the HUD scale setting, and every label gets a dark shadow so it stays
    /// legible over bright scenes.
    /// </summary>
    public static class OverlayText
    {
        static readonly Dictionary<long, GUIStyle> styles = new();
        static Font font;
        static int cachedHeight;
        static float cachedScale;

        public static float Scale => Screen.height / 900f * PresentationSettings.UiScale;

        public static GUIStyle Style(int referenceSize, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            if (cachedHeight != Screen.height || !Mathf.Approximately(cachedScale, PresentationSettings.UiScale))
            {
                styles.Clear();
                cachedHeight = Screen.height;
                cachedScale = PresentationSettings.UiScale;
            }
            long key = ((long)referenceSize << 40) ^ ((long)anchor << 32) ^ (uint)((Color32)color).GetHashCode();
            if (styles.TryGetValue(key, out var style))
                return style;
            if (!font)
                font = Resources.Load<Font>("Fonts/NotoSansKR");
            style = new GUIStyle(GUI.skin.label) { font = font, fontSize = Mathf.Max(11, Mathf.RoundToInt(referenceSize * Scale)), alignment = anchor, wordWrap = true };
            style.normal.textColor = color;
            styles[key] = style;
            return style;
        }

        public static void Label(Rect rect, string text, GUIStyle style)
        {
            var shadow = Style(Mathf.RoundToInt(style.fontSize / Scale), new Color(0, 0, 0, .85f), style.alignment);
            shadow.fontSize = style.fontSize;
            float offset = Mathf.Max(1, style.fontSize / 14f);
            text = KeyHints.Apply(text);
            GUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, shadow);
            GUI.Label(rect, text, style);
        }

        /// <summary>Reference-space rect (1600×900 layout) anchored to the left edge, scaled to the screen.</summary>
        public static Rect Left(float x, float yFraction, float width, float height) => new Rect(x * Scale, Screen.height * yFraction, width * Scale, height * Scale);
    }
}
