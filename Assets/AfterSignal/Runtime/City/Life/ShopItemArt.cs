using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public static class ShopItemArt
    {
        public static readonly string[] Keys =
        {
            "NightCoat", "TravelCoat", "LunchBox", "EnergyDrink",
            "WarmMeal", "SpecialMeal", "Coffee", "SandwichSet"
        };
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!cache.TryGetValue(key, out var sprite))
            {
                sprite = Resources.Load<Sprite>("Art/ShopItems/" + key);
                cache[key] = sprite;
            }
            return sprite;
        }
    }
}
