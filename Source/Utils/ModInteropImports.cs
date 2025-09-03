using System;
using MonoMod;
using MonoMod.ModInterop;
using ExtendedVariants;
using ExtendedVariants.Module;
using static Celeste.Mod.SorbetHelper.SorbetHelperModule;
using static ExtendedVariants.Module.ExtendedVariantsModule;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Linq;
using Monocle;

namespace Celeste.Mod.SorbetHelper.Utils;

[ModImportName("GravityHelper")]
public static class GravityHelperImports {
    public static Func<bool> IsPlayerInverted;
}

// ext vars doesn't seem to have actual modinterop but akdjsf im putting this in this file anyways bc it still fits
public static class ExtendedVariantsCompat {
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GetUpsideDown() => (bool)ExtendedVariantsModule.Instance.TriggerManager.GetCurrentVariantValue(Variant.UpsideDown);
    public static bool UpsideDown {
        get {
            if (!ExtendedVariantsLoaded)
                return false;

            return GetUpsideDown();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float GetForegroundEffectOpacity() => (float)ExtendedVariantsModule.Instance.TriggerManager.GetCurrentVariantValue(Variant.ForegroundEffectOpacity);
    public static float ForegroundEffectOpacity {
        get {
            if (!ExtendedVariantsLoaded)
                return 1f;

            return GetForegroundEffectOpacity();
        }
    }
}

public static class ChronoHelperCompat {
    private static MethodInfo Module_get_Session;
    private static MethodInfo Session_get_gravityModeUp;
    private static bool ReflectionSucceeded;

    // just using an assembly reference might be more performant?? but doing this with reflection gives a bit more control over error checking in case of an update
    internal static bool TryLoad() {
        ReflectionSucceeded = false;

        Everest.Loader.TryGetDependency(new EverestModuleMetadata {
            Name = "ChronoHelper",
            Version = new Version(1, 2, 2)
        }, out var chronoHelper);

        if (chronoHelper is null)
            return false;

        var assembly = chronoHelper.GetType().Assembly;
        Module_get_Session = assembly.GetType("Celeste.Mod.ChronoHelper.ChronoHelper")?.GetMethod("get_Session");
        Session_get_gravityModeUp = assembly.GetType("ChronoHelperSessionModule")?.GetMethod("get_gravityModeUp");

        if (Module_get_Session is null || Session_get_gravityModeUp is null) {
            Logger.Warn(nameof(SorbetHelper) + "/ModInterop", "loading support for chrono helper gravity falling block switches failed even though chrono helper is installed! expect a crash if using custom/dash falling blocks with chrono helper gravity enabled");
            return true;
        }

        return ReflectionSucceeded = true;
    }

    public static bool SessionGravityModeUp {
        get {
            if (!ChronoHelperLoaded)
                return false;

            if (!ReflectionSucceeded) // maybe change to a postcard???
                throw new Exception("failed to get chrono helper gravity!     this is likely due to an unexpected code change, please report this   to @earthwise_ in the celeste discord!");

            return (bool)Session_get_gravityModeUp.Invoke(Module_get_Session.Invoke(null, null), null);
        }
    }
}

[ModImportName("ExtendedCameraDynamics")]
public static class ExtendedCameraDynamicsImports {
    public static Func<bool> ExtendedCameraHooksEnabled;
}

[ModImportName("CommunalHelper.DashStates")]
public static class CommunalHelperDashStateImports {
    public static Func<Action<Player>, Action<Player>, Component> DreamTunnelInteraction;
}
