namespace AfterSignal
{
    public enum MusicCue { Home, Town, Tension, Action, Boss, Mystery }

    public static class MusicCatalog
    {
        public static readonly string[] Names = {
            "01_서하의집", "02_애프터라이트", "08_중앙역",
            "09_밤의객실", "11_과부하지휘자", "16_유리잔향"
        };
        // Linear attenuation to -18 LUFS, measured on the unchanged user masters.
        public static readonly float[] Gains = { .725270f, .666807f, .658415f, .699842f, .535797f, .547016f };
        public static string Path(MusicCue cue) => "Audio/Music/" + Names[(int)cue];

        public static MusicCue Select(StageId stage, int site, bool bossActive, bool bossDefeated)
        {
            switch (stage)
            {
                case StageId.Residence: case StageId.Clinic: case StageId.Harbor:
                    return MusicCue.Home;
                case StageId.Haven: case StageId.School: case StageId.Headquarters: case StageId.UrbanCity:
                    return MusicCue.Town;
                case StageId.UrbanInterior:
                    switch (UrbanCatalog.Kind(site))
                    {
                        case 0: case 4: case 9: case 15: return MusicCue.Home;
                        default: return MusicCue.Town;
                    }
                case StageId.Roof:
                    return bossDefeated ? MusicCue.Town : bossActive ? MusicCue.Boss : MusicCue.Action;
                case StageId.Carriage: case StageId.Transit: case StageId.Tower:
                case StageId.Skyline: case StageId.Foundry: case StageId.Crown: case StageId.Breach:
                    return MusicCue.Action;
                case StageId.Lab: case StageId.Archive: case StageId.Observatory: case StageId.Origin:
                    return MusicCue.Mystery;
                default:
                    return MusicCue.Tension;
            }
        }
    }
}
