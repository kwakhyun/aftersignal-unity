namespace AfterSignal
{
    public static class GangSpriteCatalog
    {
        public const int FrameCount = 8;
        public static string Art(int crew) => "Gangs/" + (crew % 3 == 0 ? "RedIron" : crew % 3 == 1 ? "VioletCrew" : "RustFang");
    }
}
