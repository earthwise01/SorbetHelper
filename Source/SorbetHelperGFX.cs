namespace Celeste.Mod.SorbetHelper;

public static class SorbetHelperGFX
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(SorbetHelperGFX)}";

    private static Effect alphaMaskEffect;
    public static Effect FxAlphaMask => alphaMaskEffect;

    private static Effect sparklingWaterEffect;
    public static Effect FxSparklingWater => sparklingWaterEffect;

    internal static void LoadContent(bool firstLoad)
    {
        alphaMaskEffect = EffectHelper.LoadEffect("alpha_mask");
        sparklingWaterEffect = EffectHelper.LoadEffect("sparkling_water");
    }

    internal static void UnloadContent()
    {
        EffectHelper.DisposeAndSetNull(ref alphaMaskEffect);
        EffectHelper.DisposeAndSetNull(ref sparklingWaterEffect);
    }

#if DEBUG

    [Command("sorbethelper_reloadgfx", "Reloads SorbetHelper GFX")]
    public static void ReloadContentCommand()
    {
        Logger.Info(LogID, "Reloading GFX...");

        UnloadContent();
        LoadContent(false);
    }

#endif
}
