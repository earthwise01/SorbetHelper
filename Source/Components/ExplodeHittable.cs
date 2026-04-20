using static Celeste.Mod.SorbetHelper.Components.ExplodeHittable;

namespace Celeste.Mod.SorbetHelper.Components;

[Tracked]
public class ExplodeHittable(ExplodeHitCallback onHit)
    : Component(false, false)
{
    public delegate void ExplodeHitCallback(Entity entity, Vector2 direction);

    public ExplodeHitCallback OnHit = onHit;

    public static void ActivateExplodeHittables(Entity self)
    {
        foreach (ExplodeHittable expolodeHittable in self.CollideAllByComponent<ExplodeHittable>())
            expolodeHittable.OnHit(self, expolodeHittable.Entity.Center - self.Position);
    }

    #region Hooks

    private static ILHook ilHook_Seeker_RegenerateCoroutine;

    [OnLoad]
    internal static void Load()
    {
        IL.Celeste.Puffer.Explode += IL_Puffer_Explode;
        ilHook_Seeker_RegenerateCoroutine = new ILHook(
            typeof(Seeker).GetMethod(nameof(Seeker.RegenerateCoroutine), HookHelper.Bind.NonPublicInstance)!.GetStateMachineTarget()!,
            IL_Seeker_RegenerateCoroutine);
    }

    [OnUnload]
    internal static void Unload()
    {
        IL.Celeste.Puffer.Explode -= IL_Puffer_Explode;
        HookHelper.DisposeAndSetNull(ref ilHook_Seeker_RegenerateCoroutine);
    }

    private static void IL_Puffer_Explode(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNextBestFit(MoveType.Before,
            instr => instr.MatchLdarg0(),
            instr => instr.MatchCallOrCallvirt<Entity>(nameof(Entity.CollideFirst)),
            instr => instr.MatchStloc(out _)))
            throw new HookHelper.HookException(il, "Unable to find puffer explosion collision checks to modify.");

        cursor.EmitLdarg0();
        cursor.EmitDelegate(ActivateExplodeHittables);
    }

    private static void IL_Seeker_RegenerateCoroutine(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        int seekerVariable = 1;

        if (!cursor.TryGotoNextBestFit(MoveType.Before,
            instr => instr.MatchLdloc(out seekerVariable),
            instr => instr.MatchCallOrCallvirt<Entity>(nameof(Entity.CollideFirst)),
            instr => instr.MatchStloc(out _)))
            throw new HookHelper.HookException(il, "Unable to find seeker explosion collision checks to modify.");

        cursor.EmitLdloc(seekerVariable);
        cursor.EmitDelegate(ActivateExplodeHittables);
    }

    #endregion
}
