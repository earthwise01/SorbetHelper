using System.Runtime.CompilerServices;
using ExtendedVariants.Module;

namespace Celeste.Mod.SorbetHelper.Imports;

internal static class ExtendedVariantsCompat
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
        => (bool)LuaCutscenesUtils.GetCurrentVariantValue("UpsideDown");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float GetForegroundEffectOpacity()
        => (float)LuaCutscenesUtils.GetCurrentVariantValue("ForegroundEffectOpacity");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float GetZoomLevel()
        => (float)LuaCutscenesUtils.GetCurrentVariantValue("ZoomLevel");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector2 AddZoomPaddingOffset(Vector2 paddingOffset)
    {
        ExtendedVariantsModule.Variant zoomLevelVariant = Enum.Parse<ExtendedVariantsModule.Variant>("ZoomLevel");
        return (ExtendedVariantsModule.Instance.VariantHandlers[zoomLevelVariant] as ExtendedVariants.Variants.ZoomLevel)!.getScreenPosition(paddingOffset);
    }
}
