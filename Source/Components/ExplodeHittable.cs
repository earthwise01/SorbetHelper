using System.Reflection;
using Celeste.Mod.SorbetHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.SorbetHelper.Components;

[Tracked]
public class ExplodeHittable(ExplodeHittable.ExplodeHitCallback onHit) : Component(false, false) {
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(ExplodeHittable)}";

    public delegate void ExplodeHitCallback(Entity entity, Vector2 direction);
    public ExplodeHitCallback OnHit = onHit;

    public static void ActivateExplodeHittables(Entity self) {
        foreach (ExplodeHittable expolodeHittable in self.CollideAllByComponent<ExplodeHittable>())
            expolodeHittable.OnHit(self, expolodeHittable.Entity.Center - self.Position);
    }

    #region Hooks

    private static ILHook ilHook_Seeker_RegenerateCoroutine;

    internal static void Load() {
        IL.Celeste.Puffer.Explode += IL_Puffer_Explode;
        ilHook_Seeker_RegenerateCoroutine = new ILHook(
            typeof(Seeker).GetMethod(nameof(Seeker.RegenerateCoroutine), BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
            IL_Seeker_RegenerateCoroutine
        );
    }

    internal static void Unload() {
        IL.Celeste.Puffer.Explode -= IL_Puffer_Explode;
        Util.DisposeAndSetNull(ref ilHook_Seeker_RegenerateCoroutine);
    }

    private static void IL_Puffer_Explode(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.Before,
                instr => instr.MatchLdarg0(),
                instr => instr.MatchCallOrCallvirt<Entity>(nameof(Entity.CollideFirst)),
                instr => instr.MatchStloc(out _))) {
            Logger.Warn(LogID, $"Failed to inject code to make puffer explosions activate explode hittable components in CIL code for {cursor.Method.Name}!");
            return;
        }
        Logger.Verbose(LogID, $"Injecting code to make puffer explosions activate explode hittable components at {cursor.Index} in CIL code for {cursor.Method.Name}");

        cursor.EmitLdarg0();
        cursor.EmitDelegate(ActivateExplodeHittables);
    }

    private static void IL_Seeker_RegenerateCoroutine(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        int seekerVariable = 1;

        if (!cursor.TryGotoNext(MoveType.Before,
                instr => instr.MatchLdloc(out seekerVariable),
                instr => instr.MatchCallOrCallvirt<Entity>(nameof(Entity.CollideFirst)),
                instr => instr.MatchStloc(out _))) {
            Logger.Warn(LogID, $"Failed to inject code to make seeker regenerate explosions activate explode hittable components in CIL code for {cursor.Method.Name}!");
            return;
        }

        Logger.Verbose(LogID, $"Injecting code to make seeker regenerate explosions activate explode hittable components at {cursor.Index} in CIL code for {cursor.Method.Name}");

        cursor.EmitLdloc(seekerVariable);
        cursor.EmitDelegate(ActivateExplodeHittables);
    }

    #endregion

}
