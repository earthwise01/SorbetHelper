using System;
using MonoMod;
using MonoMod.ModInterop;

namespace Celeste.Mod.SorbetHelper.Utils {

    [ModImportName("GravityHelper")]
    public static class GravityHelperImports {
        public static Func<bool> IsPlayerInverted;
    }
}
