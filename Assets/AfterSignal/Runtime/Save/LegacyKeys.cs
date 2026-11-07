namespace AfterSignal
{
    /// <summary>
    /// Story-progress keys that used to live in PlayerPrefs. <see cref="SaveStore"/> imports them once into save slot 1
    /// so existing players keep their progress. Daily one-shot keys (cash loot, facility jobs, daily rewards) are
    /// intentionally not imported — they only matter for the day they were written.
    /// </summary>
    public static class LegacyKeys
    {
        public const string Stage = "AFTERSIGNAL.Unity.Stage", Hours = "AFTERSIGNAL.Unity.Life.Hours", Credits = "AFTERSIGNAL.Unity.Life.Credits";
        public static readonly string[] Progress =
        {
            Stage, "AFTERSIGNAL.Unity.Memories", "AFTERSIGNAL.Unity.Completed", "AFTERSIGNAL.Unity.CivicPermit",
            "AFTERSIGNAL.Unity.Expansion.Chapters", "AFTERSIGNAL.Unity.Expansion.Accepted", "AFTERSIGNAL.Unity.Expansion.Jobs",
            "AFTERSIGNAL.Unity.Chronicle.V1", "AFTERSIGNAL.Unity.Armory.v1", "AFTERSIGNAL.Unity.RespawnFacility", "AFTERSIGNAL.Unity.JailSeconds",
            "AFTERSIGNAL.Unity.FourCities.Chapter", "AFTERSIGNAL.Unity.FourCities.Choice", "AFTERSIGNAL.Unity.FourCities.Sports",
            Credits, "AFTERSIGNAL.Unity.Life.Savings", "AFTERSIGNAL.Unity.Life.HomeCash", "AFTERSIGNAL.Unity.Life.Outfit", "AFTERSIGNAL.Unity.Life.Outfits",
            Hours, "AFTERSIGNAL.Unity.Life.Heat", "AFTERSIGNAL.Unity.Life.Hidden", "AFTERSIGNAL.Unity.Life.Errand", "AFTERSIGNAL.Unity.Life.CampaignSerial",
            "AFTERSIGNAL.Unity.Urban.Site", "AFTERSIGNAL.Unity.Urban.Car", "AFTERSIGNAL.Unity.Urban.CarStage", "AFTERSIGNAL.Unity.Urban.CarX", "AFTERSIGNAL.Unity.Urban.CarZ",
            "AFTERSIGNAL.Unity.Urban.CarYaw", "AFTERSIGNAL.Unity.Urban.CarFuel", "AFTERSIGNAL.Unity.Urban.CarHealth", "AFTERSIGNAL.Unity.Urban.CarDurabilityVersion",
            "AFTERSIGNAL.Unity.Urban.CarDesign", "AFTERSIGNAL.Unity.Urban.CarType", "AFTERSIGNAL.Unity.Urban.CarColor", "AFTERSIGNAL.Unity.Urban.CarWheels", "AFTERSIGNAL.Unity.Urban.CarSpoiler",
        };
    }
}
