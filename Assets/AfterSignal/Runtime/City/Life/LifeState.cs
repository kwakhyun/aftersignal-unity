using UnityEngine;

namespace AfterSignal
{
    public static class LifeState
    {
        const string Prefix = "AFTERSIGNAL.Unity.Life.";
        static bool loaded;
        public static bool SuppressSave;
        public static int Credits { get; private set; }
        public static int Savings { get; private set; }
        public static int Outfit { get; private set; }
        public static int Outfits { get; private set; }

        public static float Hours = 8;
        public static float Heat, HiddenSeconds;
        public static HiddenErrand Errand;
        public static int Day => Mathf.FloorToInt(Hours / 24) + 1;
        public static float Hour => Mathf.Repeat(Hours, 24);

        public static void Load()
        {
            if (loaded)
                return;
            loaded = true;
            Credits = PlayerPrefs.GetInt(Prefix + "Credits", 1500);
            Savings = PlayerPrefs.GetInt(Prefix + "Savings", 0);
            Outfit = PlayerPrefs.GetInt(Prefix + "Outfit", 0);
            Outfits = PlayerPrefs.GetInt(Prefix + "Outfits", 3);
            Hours = PlayerPrefs.GetFloat(Prefix + "Hours", 8);
            Heat = PlayerPrefs.GetFloat(Prefix + "Heat", 0);
            HiddenSeconds = PlayerPrefs.GetFloat(Prefix + "Hidden", 0);
            string json = PlayerPrefs.GetString(Prefix + "Errand", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    Errand = JsonUtility.FromJson<HiddenErrand>(json);
                }
                catch
                {
                    Errand = null;
                }
            }
        }

        public static bool Spend(int cost)
        {
            Load();
            if (cost < 0 || Credits < cost)
                return false;
            Credits -= cost;
            Save();
            return true;
        }

        public static void Earn(int amount)
        {
            Load();
            Credits = Mathf.Clamp(Credits + Mathf.Max(0, amount), 0, 9999999);
            Save();
        }

        public static bool Deposit(int amount)
        {
            if (!Spend(amount))
                return false;
            Savings += amount;
            Save();
            return true;
        }

        public static bool Withdraw(int amount)
        {
            Load();
            if (amount < 0 || Savings < amount)
                return false;
            Savings -= amount;
            Earn(amount);
            return true;
        }

        public static void Wear(int index)
        {
            Load();
            if (index < 0 || index > 3 || (Outfits & (1 << index)) == 0)
                return;
            Outfit = index;
            Save();
        }

        public static bool BuyOutfit(int index, int price)
        {
            if (index < 0 || index > 3 || (Outfits & (1 << index)) != 0 || !Spend(price))
                return false;
            Outfits |= 1 << index;
            Save();
            return true;
        }

        public static bool RewardOnce(string id, int amount)
        {
            string key = Prefix + "Reward." + id;
            if (PlayerPrefs.GetInt(key, 0) > 0)
                return false;
            PlayerPrefs.SetInt(key, 1);
            Earn(amount);
            return true;
        }

        public static void Save()
        {
            if (!loaded || SuppressSave)
                return;
            PlayerPrefs.SetInt(Prefix + "Credits", Credits);
            PlayerPrefs.SetInt(Prefix + "Savings", Savings);
            PlayerPrefs.SetInt(Prefix + "Outfit", Outfit);
            PlayerPrefs.SetInt(Prefix + "Outfits", Outfits);
            PlayerPrefs.SetFloat(Prefix + "Hours", Hours);
            PlayerPrefs.SetFloat(Prefix + "Heat", Heat);
            PlayerPrefs.SetFloat(Prefix + "Hidden", HiddenSeconds);
            PlayerPrefs.SetString(Prefix + "Errand", Errand == null ? "" : JsonUtility.ToJson(Errand));
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            ArmoryInventory.Reset();
            loaded = true;
            Credits = 1500;
            Savings = Outfit = 0;
            Outfits = 3;
            Hours = 8;
            Heat = HiddenSeconds = 0;
            Errand = null;
            PlayerPrefs.SetInt(Prefix + "CampaignSerial", PlayerPrefs.GetInt(Prefix + "CampaignSerial", 0) + 1);
            Save();
        }

        public static int CampaignSerial => PlayerPrefs.GetInt(Prefix + "CampaignSerial", 0);
    }

    [System.Serializable]
    public sealed class HiddenErrand
    {
        public string title, description, kind, giver;
        public int site, reward;
        public bool accepted, completed;
    }
}
