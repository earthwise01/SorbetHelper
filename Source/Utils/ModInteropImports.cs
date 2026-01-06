using System.Runtime.CompilerServices;
using System.Reflection;
using ModInteropImportGenerator;
using ExtendedVariants.Module;

namespace Celeste.Mod.SorbetHelper.Utils;

#region ModInterop Imports

[GenerateImports("GravityHelper")]
public static partial class GravityHelperInterop {
    public static partial bool IsPlayerInverted();
}

[GenerateImports("ExtendedCameraDynamics")]
public static partial class ExtendedCameraDynamicsInterop {
    public static partial bool ExtendedCameraHooksEnabled();
}

[GenerateImports("CommunalHelper.DashStates")]
public static partial class CommunalHelperDashStatesInterop {
    public static partial Component DreamTunnelInteraction(Action<Player> onPlayerEnter, Action<Player> onPlayerExit);
}

#endregion

#region Mod Compat

public static class ExtendedVariantsCompat {
    public static bool IsLoaded { get; private set; } = false;
    internal static void Load() {
        IsLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata {
            Name = "ExtendedVariantMode",
            Version = new Version(0, 38, 0)
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GetUpsideDown() => (bool)ExtendedVariantsModule.Instance.TriggerManager.GetCurrentVariantValue(ExtendedVariantsModule.Variant.UpsideDown);
    public static bool UpsideDown {
        get {
            if (!IsLoaded)
                return false;

            return GetUpsideDown();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float GetForegroundEffectOpacity() => (float)ExtendedVariantsModule.Instance.TriggerManager.GetCurrentVariantValue(ExtendedVariantsModule.Variant.ForegroundEffectOpacity);
    public static float ForegroundEffectOpacity {
        get {
            if (!IsLoaded)
                return 1f;

            return GetForegroundEffectOpacity();
        }
    }
}

public static class ChronoHelperCompat {
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(ChronoHelperCompat)}";

    public static bool IsLoaded { get; private set; } = false;

    private static MethodInfo m_Module_get_Session;
    private static MethodInfo m_Session_get_gravityModeUp;
    private static bool reflectionSucceeded;

    // just using an assembly reference might be more performant?? but doing this with reflection gives a bit more control over error checking in case of an update
    internal static void Load() {
        IsLoaded = reflectionSucceeded = false;

        Everest.Loader.TryGetDependency(new EverestModuleMetadata {
            Name = "ChronoHelper",
            Version = new Version(1, 2, 2)
        }, out EverestModule chronoHelper);

        // chrono helper is not loaded
        if (chronoHelper is null)
            return;

        Assembly assembly = chronoHelper.GetType().Assembly;
        m_Module_get_Session = assembly.GetType("Celeste.Mod.ChronoHelper.ChronoHelper")?.GetMethod("get_Session");
        m_Session_get_gravityModeUp = assembly.GetType("ChronoHelperSessionModule")?.GetMethod("get_gravityModeUp");

        // chrono helper is loaded, but getting the falling block gravity property getter with reflection failed!
        if (m_Module_get_Session is null || m_Session_get_gravityModeUp is null) {
            Logger.Error(LogID, "loading support for chrono helper gravity falling block switches failed even though chrono helper is installed! expect a crash if using custom/dash falling blocks with chrono helper gravity enabled");

            IsLoaded = true;
            reflectionSucceeded = false;
            return;
        }

        // chrono helper compat is loaded
        IsLoaded = reflectionSucceeded = true;
    }

    public static bool SessionGravityModeUp {
        get {
            if (!IsLoaded)
                throw new Exception("failed to get chrono helper falling block gravity: chrono helper is not installed!");

            if (!reflectionSucceeded) // maybe change to a postcard???
                throw new Exception("failed to get chrono helper gravity!     this is likely due to an unexpected code change, please report this   to @earthwise_ in the celeste discord, or on SorbetHelper's github!");

            return (bool)m_Session_get_gravityModeUp.Invoke(m_Module_get_Session.Invoke(null, null), null);
        }
    }
}

#endregion
