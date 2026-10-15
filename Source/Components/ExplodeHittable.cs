namespace Celeste.Mod.SorbetHelper.Components;

[Tracked]
public class ExplosionCollider(Action<Entity, Vector2> onCollide) : Component(false, false)
{
    public Action<Entity, Vector2> OnCollide = onCollide;

    public static void TriggerExplosionColliders(Entity entity)
    {
        foreach (ExplosionCollider explosionCollider in entity.Scene.Tracker.GetComponents<ExplosionCollider>())
        {
            if (entity.CollideCheck(explosionCollider.Entity))
                explosionCollider.OnCollide(entity, explosionCollider.Entity.Center - entity.Position);
        }
    }

    #region Hooks

    private static ILHook ilHook_Seeker_RegenerateCoroutine;

    [OnLoad]
    internal static void Load()
    {
        IL.Celeste.Puffer.Explode += IL_Puffer_Explode;
        ilHook_Seeker_RegenerateCoroutine = new ILHook(typeof(Seeker).GetMethod(nameof(Seeker.RegenerateCoroutine), HookHelper.Bind.NonPublicInstance)!.GetStateMachineTarget()!, IL_Seeker_RegenerateCoroutine);
    }

    [OnUnload]
    internal static void Unload()
    {
        IL.Celeste.Puffer.Explode -= IL_Puffer_Explode;
        HookHelper.DisposeAndSetNull(ref ilHook_Seeker_RegenerateCoroutine);
    }

    private static void IL_Puffer_Explode(ILContext il)
    {
        ILCursor cursor = new(il);

        if (!cursor.TryGotoNextBestFit(MoveType.AfterLabel,
            instr => instr.MatchLdarg0(),
            instr => instr.MatchLdloc0(),
            instr => instr.MatchCallOrCallvirt<Entity>("set_Collider")))
            throw new HookHelper.HookException(il, "Unable to find assignment of original collider to `this.Collider`.");

        cursor.EmitLdarg0();
        cursor.EmitDelegate(TriggerExplosionColliders);
    }

    private static void IL_Seeker_RegenerateCoroutine(ILContext il)
    {
        ILCursor cursor = new(il);

        if (!cursor.TryGotoNextBestFit(MoveType.AfterLabel,
            instr => instr.MatchLdloc1(),
            instr => instr.MatchLdloc1(),
            instr => instr.MatchLdfld<Seeker>("physicsHitbox"),
            instr => instr.MatchCallOrCallvirt<Entity>("set_Collider")))
            throw new HookHelper.HookException(il, "Unable to find assignment of `this.physicsHitbox` to `this.Collider`.");

        cursor.EmitLdloc1();
        cursor.EmitDelegate(TriggerExplosionColliders);
    }

    #endregion
}
