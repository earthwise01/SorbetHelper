using System;
using MonoMod;
using MonoMod.ModInterop;

namespace Celeste.Mod.SorbetHelper {

    [ModImportName("GravityHelper")]
    public static class GravityHelperImports {
        public static Func<bool> IsPlayerInverted;
    }
}
