using System.Collections.Generic;
using MonoMod.Cil;

namespace Celeste.Mod.SorbetHelper.Components;

[Tracked]
public class MovingPlatformHittable(MovingPlatformHittable.PlatformHitCallback onHit, bool breakDashBlocksRequired = true) : Component(false, false) {
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(MovingPlatformHittable)}";

    public delegate void PlatformHitCallback(Platform platform, Vector2 direction);

    public PlatformHitCallback OnHit = onHit;

    private readonly bool breakDashBlocksRequired = breakDashBlocksRequired;

    public static void ActivateMovingPlatformHittables(Platform self, Vector2 direction, bool breakDashBlocks) {
        List<MovingPlatformHittable> toHit = self.CollideAllByComponent<MovingPlatformHittable>(self.Position + direction);

        foreach (MovingPlatformHittable platformHittable in toHit) {
            if (!platformHittable.breakDashBlocksRequired || breakDashBlocks)
                platformHittable.OnHit(self, direction);
        }
    }

    #region Hooks

    internal static void Load() {
        IL.Celeste.Platform.MoveHExactCollideSolids += IL_Platform_MoveHExactCollideSolids;
        IL.Celeste.Platform.MoveVExactCollideSolids += IL_Platform_MoveVExactCollideSolids;
    }

    internal static void Unload() {
        IL.Celeste.Platform.MoveHExactCollideSolids -= IL_Platform_MoveHExactCollideSolids;
        IL.Celeste.Platform.MoveVExactCollideSolids -= IL_Platform_MoveVExactCollideSolids;
    }

    private static void IL_Platform_MoveHExactCollideSolids(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        // jump to just before the check for dash blocks, afterlabel is needed since this is at the start of the movement loop
        if (!cursor.TryGotoNext(MoveType.AfterLabel,
                instr => instr.MatchLdarg2(),
                instr => instr.MatchBrfalse(out _))) {
            Logger.Warn(LogID, $"Failed to inject code to make horizontal falling blocks/kevins/etc activate moving platform hittable components in CIL code for {cursor.Method.Name}");
            return;
        }

        Logger.Verbose(LogID, $"Injecting code to make horizontal falling blocks/kevins/etc activate moving platform hittable components at {cursor.Index} in CIL code for {cursor.Method.Name}");

        cursor.EmitLdarg0(); // this
        cursor.EmitLdloc1(); // direction sign
        cursor.EmitLdarg2(); // breakDashBlocks
        cursor.EmitDelegate(ActivateMovingPlatformHittablesH);

        return;

        static void ActivateMovingPlatformHittablesH(Platform self, int directionSign, bool breakDashBlocks)
            => ActivateMovingPlatformHittables(self, new Vector2(directionSign, 0f), breakDashBlocks);
    }

    private static void IL_Platform_MoveVExactCollideSolids(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        // go to *just* before the check for dash blocks, afterlabel is needed since this is at the start of the movement loop
        if (!cursor.TryGotoNext(MoveType.AfterLabel,
                instr => instr.MatchLdarg2(),
                instr => instr.MatchBrfalse(out _))) {
            Logger.Warn(LogID, $"Failed to inject code to make vertical falling blocks/kevins/etc activate moving platform hittable components in CIL code for {cursor.Method.Name}");
            return;
        }

        Logger.Verbose(LogID, $"Injecting code to make vertical falling blocks/kevins/etc activate moving platform hittable components at {cursor.Index} in CIL code for {cursor.Method.Name}");

        cursor.EmitLdarg0(); // this
        cursor.EmitLdloc1(); // direction sign
        cursor.EmitLdarg2(); // breakDashBlocks
        cursor.EmitDelegate(ActivateMovingPlatformHittablesV);

        return;

        static void ActivateMovingPlatformHittablesV(Platform self, int directionSign, bool breakDashBlocks)
            => ActivateMovingPlatformHittables(self, new Vector2(0f, directionSign), breakDashBlocks);
    }

    #endregion
}
