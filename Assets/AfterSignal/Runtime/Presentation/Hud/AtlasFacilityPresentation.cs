using UnityEngine;
using UnityEngine.UI;

namespace AfterSignal
{
    public sealed partial class SignalHud
    {
        void AddAtlasRowIcon(Button button, Sprite sprite, string title)
        {
            if (!sprite) return; // Source-only checkout: retain the existing text fallback.
            var iconObject = new GameObject("Facility icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);
            var icon = iconObject.GetComponent<Image>();
            icon.sprite = sprite;
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            Rect(icon.rectTransform, 8, 6, 31, 31);
            var text = button.GetComponentInChildren<Text>();
            text.text = title;
            Rect(text.rectTransform, 49, 6, 273, 31);
            text.fontSize = 14;
        }
    }
}
