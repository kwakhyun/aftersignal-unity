namespace AfterSignal
{
    public static class PoliceSpriteCatalog
    {
        public const int FrameCount = 8;

        public static string Art(PoliceWeapon weapon)
        {
            switch (weapon)
            {
                case PoliceWeapon.Shotgun: return "LawEnforcement/PoliceShotgun";
                case PoliceWeapon.Rifle: return "LawEnforcement/SwatRifle";
                default: return "LawEnforcement/PolicePistol";
            }
        }
    }
}
