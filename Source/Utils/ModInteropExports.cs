using MonoMod.ModInterop;

namespace Celeste.Mod.SorbetHelper.Utils;

[ModExportName("SorbetHelper")]
public static class SorbetHelperExports {
    // let other mods make global entities without an assembly reference
    public static void RegisterGlobalEntity(string entityDataName, bool onlyOne) =>
        GlobalEntities.RegisterGlobalEntity(entityDataName, onlyOne);
}