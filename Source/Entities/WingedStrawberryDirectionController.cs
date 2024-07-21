using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Utils;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Mono.Cecil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/WingedStrawberryDirectionController")]
    [Tracked]
    public class WingedStrawberryDirectionController : Entity {
        private static ILHook strawberryUpdateHook;

        private static readonly Dictionary<string, Vector2> directionToVector = new Dictionary<string, Vector2>{
            {"up", new Vector2(0f, -1f)}, {"down", new Vector2(0f, 1f)}, {"left", new Vector2(-1f, 0f)}, {"right", new Vector2(1f, 0f)},
            {"upleft", new Vector2(-1f, -1f)}, {"upright", new Vector2(1f, -1f)}, {"downleft", new Vector2(-1f, 1f)}, {"downright", new Vector2(1f, 1f)}
        };
        public Vector2 direction;

        private bool movesUp => direction.Y < 0f;
        private bool movesDown => direction.Y > 0f;
        private bool movesLeft => direction.X < 0f;
        private bool movesRight => direction.X > 0f;

        public WingedStrawberryDirectionController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            direction = directionToVector[data.Attr("direction", "up").ToLower()];
            direction.Normalize();
        }

        public static void Load() {
            strawberryUpdateHook = new ILHook(typeof(Strawberry).GetMethod("orig_Update"), modStrawberryUpdate);
        }

        public static void Unload() {
            strawberryUpdateHook.Dispose();
        }

        private static void modStrawberryUpdate(ILContext il) {
            // this is probably an absolute mess but aaaaaaa it works i guess?????
            ILCursor cursor = new ILCursor(il);

            VariableDefinition controllerVariable = new(il.Import(typeof(WingedStrawberryDirectionController)));
            il.Body.Variables.Add(controllerVariable);

            // inject direction nonspecific movement code
            ILLabel afterVanillaMovementLabel = cursor.DefineLabel();

            if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdfld<Strawberry>(nameof(Strawberry.flyingAway))) &&
            cursor.TryGotoPrev(MoveType.Before,
            instr => instr.MatchLdarg0(),
            instr => instr.MatchLdarg0(),
            instr => instr.MatchCall<Entity>("get_Y"))) {
                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting custom flight movement at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                // initialize controller variable
                cursor.EmitLdarg0();
                cursor.EmitDelegate(getController);
                cursor.EmitStloc(controllerVariable);

                // inject custom movement code
                cursor.EmitLdarg0();
                cursor.EmitLdarg0();
                cursor.EmitLdfld(typeof(Strawberry).GetField(nameof(Strawberry.flapSpeed), BindingFlags.NonPublic | BindingFlags.Instance));
                cursor.EmitLdloc(controllerVariable);
                cursor.EmitDelegate(moveStrawberry);

                cursor.EmitBrtrue(afterVanillaMovementLabel);
                cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Entity>("set_Y"))
                .MarkLabel(afterVanillaMovementLabel);
            } else {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject custom flight movement in CIL code for {cursor.Method.FullName}!");
            }

            // inject downwards and horizontal out of room bounds checks.
            ILLabel afterOOBChecksLabel = null;

            if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdarg0(),
            instr => instr.MatchCall<Entity>("get_Y"))) {
                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting additional out of room bounds checks at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                // use custom oob checks if a controller exists, otheriwse skip past the vanilla code for removing the berry once it flies above the room bounds
                cursor.EmitLdarg0();
                cursor.EmitLdloc(controllerVariable);
                cursor.EmitDelegate(addExtraOutOfBoundsChecks);
                cursor.FindNext(out _, instr => instr.MatchBgeUn(out afterOOBChecksLabel));
                cursor.EmitBrtrue(afterOOBChecksLabel);
            } else {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject additional out of room bounds checks in CIL code for {cursor.Method.FullName}!");
            }

            // inject horizontal idle bounds checks alongside the vanilla vertical checks.
            if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchMul(),
            instr => instr.MatchCall(typeof(Calc), nameof(Calc.Approach)),
            instr => instr.MatchStfld<Strawberry>(nameof(Strawberry.flapSpeed)))) {
                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting horizontal idle bounds checks at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                cursor.EmitLdarg0();
                cursor.EmitLdarg0();
                cursor.EmitLdfld(typeof(Strawberry).GetField(nameof(Strawberry.start), BindingFlags.NonPublic | BindingFlags.Instance));
                cursor.EmitLdloc(controllerVariable);
                cursor.EmitDelegate(addHorizontalIdleBoundsChecks);
            } else {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject horizontal idle bounds checks in CIL code for {cursor.Method.FullName}!");
            }
        }

        private static WingedStrawberryDirectionController getController(Entity self) {
            if (self.Scene == null)
                return null;

            return self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
        }

        // returns true if custom movement was used
        private static bool moveStrawberry(Entity self, float flapSpeed, WingedStrawberryDirectionController controller) {
            if (controller == null)
                return false;

            self.Position += controller.direction * -flapSpeed * Engine.DeltaTime;

            return true;
        }

        // returns true if custom oob check was used
        private static bool addExtraOutOfBoundsChecks(Entity self, WingedStrawberryDirectionController controller) {
            if (controller == null)
                return false;

            // out of bounds checks are only done for the directions the berry flies towards bc otherwise berries that would fly into the room from offscreen will be removed as soon as they start flying
            Rectangle levelBounds = self.SceneAs<Level>().Bounds;
            if (controller.movesUp && self.Y < levelBounds.Top - 16)
                self.RemoveSelf();
            else if (controller.movesDown && self.Y > levelBounds.Bottom + 16)
                self.RemoveSelf();
            else if (controller.movesLeft && self.X < levelBounds.Left - 24)
                self.RemoveSelf();
            else if (controller.movesRight && self.X > levelBounds.Right + 24)
                self.RemoveSelf();

            return true;
        }

        private static void addHorizontalIdleBoundsChecks(Entity self, Vector2 start, WingedStrawberryDirectionController controller) {
            // only perform horizontal idle bounds checks if a controller exists
            if (controller == null)
                return;

            if (self.X < start.X - 5f)
                self.X = start.X - 5f;
            else if (self.X > start.X + 5f)
                self.X = start.X + 5f;
        }
    }
}
