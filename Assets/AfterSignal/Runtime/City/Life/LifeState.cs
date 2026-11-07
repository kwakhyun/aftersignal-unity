using UnityEngine;

namespace AfterSignal
{
    public static class LifeState
    {
        const string Prefix = "AFTERSIGNAL.Unity.Life.";
        static bool loaded;static int loadedGeneration=-1;
        public static bool SuppressSave;
        public static int Credits { get; private set; }
        public static int Savings { get; private set; }
        public static int HomeCash { get; private set; }
        public static int Outfit { get; private set; }
        public static int Outfits { get; private set; }

        public static float Hours = 8;
        public static float Heat, HiddenSeconds;
        // In-game hour of the last wake-up this session (sleep gating); not persisted.
        public static float LastWake = -999;
        public static HiddenErrand Errand;
        public static int Day => Mathf.FloorToInt(Hours / 24) + 1;
        public static float Hour => Mathf.Repeat(Hours, 24);

        public static void Load()
        {
            if (loaded && loadedGeneration == SaveStore.Generation)
                return;
            loaded = true;loadedGeneration = SaveStore.Generation;
            Credits = SaveStore.GetInt(Prefix + "Credits", 1500);
            Savings = SaveStore.GetInt(Prefix + "Savings", 0);
            HomeCash = SaveStore.GetInt(Prefix + "HomeCash", 0);
            Outfit = SaveStore.GetInt(Prefix + "Outfit", 0);
            Outfits = SaveStore.GetInt(Prefix + "Outfits", 3);
            Hours = SaveStore.GetFloat(Prefix + "Hours", 8);
            Heat = SaveStore.GetFloat(Prefix + "Heat", 0);
            HiddenSeconds = SaveStore.GetFloat(Prefix + "Hidden", 0);
            // Older saves start accruing from the current day instead of paying back-interest.
            InterestDay = SaveStore.GetInt(Prefix + "InterestDay", Day);
            // Session-only values and the errand belong to the slot that was loaded before (another slot or a reset one).
            LastWake = -999;
            LastInterest = 0;
            Errand = null;
            string json = SaveStore.GetString(Prefix + "Errand", "");
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

        // Savings earn a small capped daily interest when the in-game day changes (clock, sleep, work).
        // Carried credits are what the hospital fee takes on death, so banking protects money.
        public static int InterestDay { get; private set; }
        public static int LastInterest { get; private set; }
        public static int DailyInterest => Mathf.Min(GameTuning.Active.savingsInterestCap, Mathf.FloorToInt(Savings * GameTuning.Active.savingsInterest * DifficultySettings.Payout));
        public static void SettleDay()
        {
            Load();
            int day = Day;
            if (day <= InterestDay)
            {
                if (day < InterestDay) InterestDay = day;
                return;
            }
            int days = Mathf.Min(day - InterestDay, 7), paid = 0;
            InterestDay = day;
            for (int i = 0; i < days; i++)
            {
                int interest = Mathf.Max(0, DailyInterest);
                if (Savings > 9999999 - interest) break;
                Savings += interest;
                paid += interest;
            }
            LastInterest = paid;
            Save();
            if (paid > 0)
                GameDirector.Instance?.Toast("예금 이자 +" + paid.ToString("N0") + " C · 예금 " + Savings.ToString("N0") + " C", 4);
        }

        public static bool StoreHomeCash(int amount)
        {
            Load();if(amount<=0||Credits<amount||HomeCash>9999999-amount)return false;
            Credits-=amount;HomeCash+=amount;Save();return true;
        }
        public static bool TakeHomeCash(int amount)
        {
            Load();if(amount<=0||HomeCash<amount||Credits>9999999-amount)return false;
            HomeCash-=amount;Credits+=amount;Save();return true;
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

        /// <summary>Once-per-day reward stored as one "serial:day" entry per id (no new key per day).</summary>
        public static bool RewardDaily(string id, int amount)
        {
            string key = Prefix + "RewardDay." + id, stamp = CampaignSerial + ":" + Day;
            if (SaveStore.GetString(key, "") == stamp)
                return false;
            SaveStore.SetString(key, stamp);
            Earn(amount);
            return true;
        }

        public static bool RewardOnce(string id, int amount)
        {
            string key = Prefix + "Reward." + id;
            if (SaveStore.GetInt(key, 0) > 0)
                return false;
            SaveStore.SetInt(key, 1);
            Earn(amount);
            return true;
        }

        public static void Save()
        {
            // Values loaded from another slot (title switched slots before the old scene unloaded) must not be written into the new one.
            if (!loaded || SuppressSave || loadedGeneration != SaveStore.Generation)
                return;
            SaveStore.SetInt(Prefix + "Credits", Credits);
            SaveStore.SetInt(Prefix + "Savings", Savings);
            SaveStore.SetInt(Prefix + "HomeCash", HomeCash);
            SaveStore.SetInt(Prefix + "Outfit", Outfit);
            SaveStore.SetInt(Prefix + "Outfits", Outfits);
            SaveStore.SetFloat(Prefix + "Hours", Hours);
            SaveStore.SetFloat(Prefix + "Heat", Heat);
            SaveStore.SetFloat(Prefix + "Hidden", HiddenSeconds);
            SaveStore.SetInt(Prefix + "InterestDay", InterestDay);
            SaveStore.SetString(Prefix + "Errand", Errand == null ? "" : JsonUtility.ToJson(Errand));
            SaveStore.Save();
        }

        /// <summary>Fresh life state for a New Game. Pass the serial read before the slot was wiped so it keeps increasing.</summary>
        public static void Reset(int previousSerial = 0)
        {
            ArmoryInventory.Reset();
            loaded = true;loadedGeneration = SaveStore.Generation;
            Credits = 1500;
            HomeCash = Savings = Outfit = 0;
            Outfits = 3;
            Hours = 8;
            Heat = HiddenSeconds = 0;
            InterestDay = Day;
            LastWake = -999;
            LastInterest = 0;
            Errand = null;
            PrisonSystem.ResetSentence();
            SaveStore.SetInt(Prefix + "CampaignSerial", Mathf.Max(previousSerial, SaveStore.GetInt(Prefix + "CampaignSerial", 0)) + 1);
            Save();
        }

        public static int CampaignSerial => SaveStore.GetInt(Prefix + "CampaignSerial", 0);
    }

    [System.Serializable]
    public sealed class HiddenErrand
    {
        public string title, description, kind, giver;
        public int site, reward;
        public bool accepted, completed;
    }
}
