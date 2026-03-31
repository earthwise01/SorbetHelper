using System.Runtime.CompilerServices;
using ModInteropImportGenerator;

namespace Celeste.Mod.SorbetHelper.Utils;

#region ModInterop Imports

[GenerateImports("GravityHelper", RequiredDependency = false)]
public static partial class GravityHelperInterop
{
    public static partial bool IsPlayerInverted();
}

[GenerateImports("ExtendedCameraDynamics", RequiredDependency = false)]
public static partial class ExtendedCameraDynamicsInterop
{
    public static partial bool ExtendedCameraHooksEnabled();
}

[GenerateImports("CommunalHelper.DashStates", RequiredDependency = false)]
public static partial class CommunalHelperDashStatesInterop
{
    public static partial Component DreamTunnelInteraction(Action<Player> onPlayerEnter, Action<Player> onPlayerExit);
}

#endregion

#region Mod Compat

public static class ExtendedVariantsCompat
{
    public static bool IsLoaded { get; private set; } = false;

    internal static void Load()
    {
        IsLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata
        {
            Name = "ExtendedVariantMode",
            Version = new Version(0, 48, 1)
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool GetUpsideDown()
        => (bool)ExtendedVariants.Module.LuaCutscenesUtils.GetCurrentVariantValue("UpsideDown");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float GetForegroundEffectOpacity()
        => (float)ExtendedVariants.Module.LuaCutscenesUtils.GetCurrentVariantValue("ForegroundEffectOpacity");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float GetZoomLevel()
        => (float)ExtendedVariants.Module.LuaCutscenesUtils.GetCurrentVariantValue("ZoomLevel");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector2 AddZoomPaddingOffset(Vector2 paddingOffset)
    {
        ExtendedVariants.Module.ExtendedVariantsModule.Variant zoomLevelVariant = Enum.Parse<ExtendedVariants.Module.ExtendedVariantsModule.Variant>("ZoomLevel");
        return (ExtendedVariants.Module.ExtendedVariantsModule.Instance.VariantHandlers[zoomLevelVariant] as ExtendedVariants.Variants.ZoomLevel)!.getScreenPosition(paddingOffset);
    }
}

public static class ChronoHelperCompat
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

#endregion
