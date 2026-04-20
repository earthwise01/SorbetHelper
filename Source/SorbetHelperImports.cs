namespace Celeste.Mod.SorbetHelper;

internal static class SorbetHelperImports
{
    public static void Initialize()
    {
        CommunalHelperDashStates.Load();
        ExtendedCameraDynamics.Load();
        GravityHelper.Load();

        // hmm
        ChronoHelperCompat.Load();
        ExtendedVariantsCompat.Load();
    }
}
