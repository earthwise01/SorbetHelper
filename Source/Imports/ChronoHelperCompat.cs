using System.Runtime.CompilerServices;

namespace Celeste.Mod.SorbetHelper.Imports;

internal static class ChronoHelperCompat
{
    public static bool IsLoaded { get; private set; } = false;

    internal static void Load()
    {
        IsLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata
        {
            Name = "ChronoHelper",
            Version = new Version(1, 2, 2)
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool GetSessionGravityModeUp()
        => ChronoHelper.ChronoHelper.Session.gravityModeUp;
}
