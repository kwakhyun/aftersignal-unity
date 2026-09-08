namespace AfterSignal.Editor
{
    // Preserve the public build command while retiring the duplicated Haven village export.
    public static class WorldRegionalBuilder
    {
        public static void Prepare()=>CompactCityBuilder.Prepare();
        public static void PrepareAndBuild(){Prepare();ProjectBuilder.BuildRelease();}
    }
}
