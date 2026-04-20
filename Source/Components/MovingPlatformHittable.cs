using static Celeste.Mod.SorbetHelper.Components.MovingPlatformHittable;

namespace Celeste.Mod.SorbetHelper.Components;

[Tracked]
public class MovingPlatformHittable(PlatformHitCallback onHit, bool breakDashBlocksRequired = true)
    : Component(false, false)
{
    public delegate void PlatformHitCallback(Platform platform, Vector2 direction);

    public PlatformHitCallback OnHit = onHit;

    private readonly bool breakDashBlocksRequired = breakDashBlocksRequired;

    public static void ActivateMovingPlatformHittables(Platform self, Vector2 direction, bool breakDashBlocks)
    {
        List<MovingPlatformHittable> toHit = self.CollideAllByComponent<MovingPlatformHittable>(self.Position + direction);

        foreach (MovingPlatformHittable platformHittable in toHit)
        {
            if (!platformHittable.breakDashBlocksRequired || breakDashBlocks)
                platformHittable.OnHit(self, direction);
        }
    }

    #region Hooks

    [OnLoad]
    internal static void Load()
    {
        IL.Celeste.Platform.MoveHExactCollideSolids += IL_Platform_MoveHExactCollideSolids;
        IL.Celeste.Platform.MoveVExactCollideSolids += IL_Platform_MoveVExactCollideSolids;
    }

    [OnUnload]
    internal static void Unload()
    {
        IL.Celeste.Platform.MoveHExactCollideSolids -= IL_Platform_MoveHExactCollideSolids;
        IL.Celeste.Platform.MoveVExactCollideSolids -= IL_Platform_MoveVExactCollideSolids;
    }

    private static void IL_Platform_MoveHExactCollideSolids(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNextBestFit(MoveType.AfterLabel,
            instr => instr.MatchLdarg2(),
            instr => instr.MatchBrfalse(out _)))
            throw new HookHelper.HookException(il, "Unable to find dash block breaking logic to modify.");

        cursor.EmitLdarg0(); // this
        cursor.EmitLdloc1(); // direction sign
        cursor.EmitLdarg2(); // thruDashBlocks
        cursor.EmitDelegate(ActivateMovingPlatformHittablesH);

        return;

        static void ActivateMovingPlatformHittablesH(Platform self, int directionSign, bool thruDashBlocks)
            => ActivateMovingPlatformHittables(self, new Vector2(directionSign, 0f), thruDashBlocks);
    }

    private static void IL_Platform_MoveVExactCollideSolids(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNextBestFit(MoveType.AfterLabel,
            instr => instr.MatchLdarg2(),
            instr => instr.MatchBrfalse(out _)))
            throw new HookHelper.HookException(il, "Unable to find dash block breaking logic to modify.");

        cursor.EmitLdarg0(); // this
        cursor.EmitLdloc1(); // direction sign
        cursor.EmitLdarg2(); // breakDashBlocks
        cursor.EmitDelegate(ActivateMovingPlatformHittablesV);

        return;

        static void ActivateMovingPlatformHittablesV(Platform self, int directionSign, bool thruDashBlocks)
            => ActivateMovingPlatformHittables(self, new Vector2(0f, directionSign), thruDashBlocks);
    }

    #endregion
}
