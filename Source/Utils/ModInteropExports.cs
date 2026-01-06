using MonoMod.ModInterop;

namespace Celeste.Mod.SorbetHelper.Utils;

[ModExportName("SorbetHelper")]
public static class SorbetHelperExports {
    public static void RegisterGlobalEntity(string entityDataName, bool onlyOne)
        => GlobalEntities.RegisterGlobalEntity(entityDataName, onlyOne);
}
