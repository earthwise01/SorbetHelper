using System;
using MonoMod;
using MonoMod.ModInterop;
using ExtendedVariants;
using ExtendedVariants.Module;
using static Celeste.Mod.SorbetHelper.SorbetHelperModule;
using static ExtendedVariants.Module.ExtendedVariantsModule;

namespace Celeste.Mod.SorbetHelper.Utils {

    [ModImportName("GravityHelper")]
    public static class GravityHelperImports {
        public static Func<bool> IsPlayerInverted;
    }

    // ext vars doesn't seem to have actual modinterop but akdjsf im putting this in this file anyways bc it still fits
    public static class ExtendedVariantsCompat {
        public static bool UpsideDown {
            get {
                if (!ExtendedVariantsLoaded)
                    return false;

                return (bool)ExtendedVariantsModule.Instance.TriggerManager.GetCurrentVariantValue(Variant.UpsideDown);
            }
        }

        public static float ForegroundEffectOpacity {
            get {
                if (!ExtendedVariantsLoaded)
                    return 1f;

                return (float)ExtendedVariantsModule.Instance.TriggerManager.GetCurrentVariantValue(Variant.ForegroundEffectOpacity);
            }
        }
    }
}
