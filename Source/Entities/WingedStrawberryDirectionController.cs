using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;

namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/WingedStrawberryDirectionController")]
[Tracked]
public class WingedStrawberryDirectionController(EntityData data, Vector2 offset) : Entity(data.Position + offset) {
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(WingedStrawberryDirectionController)}";

    private static readonly Dictionary<string, Vector2> DirectionToVector = new Dictionary<string, Vector2> {
        {"up", new Vector2(0f, -1f)}, {"down", new Vector2(0f, 1f)}, {"left", new Vector2(-1f, 0f)}, {"right", new Vector2(1f, 0f)},
        {"upleft", new Vector2(-1f, -1f)}, {"upright", new Vector2(1f, -1f)}, {"downleft", new Vector2(-1f, 1f)}, {"downright", new Vector2(1f, 1f)}
    };

    private readonly Vector2 direction = DirectionToVector[data.Attr("direction", "up").ToLower()].SafeNormalize();

    #region Hooks

    private static ILHook ilHook_Strawberry_orig_Update;

    internal static void Load() {
        ilHook_Strawberry_orig_Update = new ILHook(typeof(Strawberry).GetMethod("orig_Update", HookHelper.Bind.PublicInstance)!, IL_Strawberry_orig_Update);
    }

    internal static void Unload() {
        HookHelper.DisposeAndSetNull(ref ilHook_Strawberry_orig_Update);
    }

    private static void IL_Strawberry_orig_Update(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        VariableDefinition controllerVariable = new VariableDefinition(il.Import(typeof(WingedStrawberryDirectionController)));
        il.Body.Variables.Add(controllerVariable);

        // inject direction non-specific movement code
        ILLabel afterVanillaMovementLabel = cursor.DefineLabel();

        if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdfld<Strawberry>(nameof(Strawberry.flyingAway)))
            || !cursor.TryGotoPrev(MoveType.Before,
                instr => instr.MatchLdarg0(),
                instr => instr.MatchLdarg0(),
                instr => instr.MatchCall<Entity>("get_Y"))) {
            Logger.Warn(LogID, $"Failed to inject custom flight movement in CIL code for {cursor.Method.FullName}!");
            return;
        }

        Logger.Verbose(LogID, $"Injecting custom flight movement at {cursor.Index} in CIL code for {cursor.Method.FullName}");

        // initialize controller variable
        cursor.EmitLdarg0();
        cursor.EmitDelegate(GetDirectionController);
        cursor.EmitStloc(controllerVariable);

        // inject custom movement code
        cursor.EmitLdarg0();
        cursor.EmitLdarg0();
        cursor.EmitLdfld(typeof(Strawberry).GetField(nameof(Strawberry.flapSpeed), HookHelper.Bind.NonPublicInstance)!);
        cursor.EmitLdloc(controllerVariable);
        cursor.EmitDelegate(DirectionalMovement);

        cursor.EmitBrtrue(afterVanillaMovementLabel);
        cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Entity>("set_Y"))
              .MarkLabel(afterVanillaMovementLabel);

        // inject downwards and horizontal out of room bounds checks
        ILLabel afterOOBChecksLabel = null;

        if (!cursor.TryGotoNext(MoveType.Before,
                instr => instr.MatchLdarg0(),
                instr => instr.MatchCall<Entity>("get_Y"))) {
            Logger.Warn(LogID, $"Failed to inject additional out of room bounds checks in CIL code for {cursor.Method.FullName}!");
            return;
        }

        Logger.Verbose(LogID, $"Injecting additional out of room bounds checks at {cursor.Index} in CIL code for {cursor.Method.FullName}");

        // use custom oob checks if a controller exists and skip past the vanilla ones if necessary
        cursor.EmitLdarg0();
        cursor.EmitLdloc(controllerVariable);
        cursor.EmitDelegate(DirectionalOutOfBoundsCheck);
        cursor.FindNext(out _, instr => instr.MatchBgeUn(out afterOOBChecksLabel));
        cursor.EmitBrtrue(afterOOBChecksLabel);

        // inject horizontal idle bounds checks alongside the vanilla vertical checks
        if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchMul(),
                instr => instr.MatchCall(typeof(Calc), nameof(Calc.Approach)),
                instr => instr.MatchStfld<Strawberry>(nameof(Strawberry.flapSpeed)))) {
            Logger.Warn(LogID, $"Failed to inject horizontal idle bounds checks in CIL code for {cursor.Method.FullName}!");
            return;
        }

        Logger.Verbose(LogID, $"Injecting horizontal idle bounds checks at {cursor.Index} in CIL code for {cursor.Method.FullName}");

        cursor.EmitLdarg0();
        cursor.EmitLdarg0();
        cursor.EmitLdfld(typeof(Strawberry).GetField(nameof(Strawberry.start), HookHelper.Bind.NonPublicInstance)!);
        cursor.EmitLdloc(controllerVariable);
        cursor.EmitDelegate(HorizontalIdleBoundsCheck);

        return;

        static WingedStrawberryDirectionController GetDirectionController(Entity self)
            => self.Scene?.Tracker.GetEntity<WingedStrawberryDirectionController>();

        // returns true if vanilla movement should be skipped
        static bool DirectionalMovement(Entity self, float flapSpeed, WingedStrawberryDirectionController controller) {
            if (controller is null)
                return false;

            self.Position += controller.direction * -flapSpeed * Engine.DeltaTime;

            return true;
        }

        // returns true if vanilla oob check should be skipped
        static bool DirectionalOutOfBoundsCheck(Entity self, WingedStrawberryDirectionController controller) {
            if (controller is null)
                return false;

            Rectangle levelBounds = self.SceneAs<Level>().Bounds;
            if (controller.direction.Y < 0f && self.Y < levelBounds.Top - 16
                || controller.direction.Y > 0f && self.Y > levelBounds.Bottom + 16
                || controller.direction.X < 0f && self.X < levelBounds.Left - 24
                || controller.direction.X > 0f && self.X > levelBounds.Right + 24)
                self.RemoveSelf();

            return true;
        }

        static void HorizontalIdleBoundsCheck(Entity self, Vector2 start, WingedStrawberryDirectionController controller) {
            // only perform horizontal idle bounds checks if a controller exists
            if (controller is null)
                return;

            if (self.X < start.X - 5f)
                self.X = start.X - 5f;
            else if (self.X > start.X + 5f)
                self.X = start.X + 5f;
        }
    }

    #endregion

}
