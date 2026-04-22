using MonoMod.ModInterop;

namespace Celeste.Mod.SorbetHelper;

public static class SorbetHelperExports
{
    internal static void Load()
    {
        typeof(Utils).ModInterop();
    }

    [ModExportName("SorbetHelper")]
    public static class Utils
    {
        // is this even worth keeping around
        public static void RegisterGlobalEntity(string entitySID, string onlyGlobalIf)
            => GlobalEntityHelper.RegisterGlobalEntity(entitySID, onlyGlobalIf);
    }
}
