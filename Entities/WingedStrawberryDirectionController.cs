using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Mono.Cecil;
using MonoMod.RuntimeDetour;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Celeste;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/WingedStrawberryDirectionController")]
    [Tracked]
    public class WingedStrawberryDirectionController : Entity {
        private static ILHook strawberryUpdateHook;

        private static Dictionary<string, Vector2> directionToVector = new Dictionary<string, Vector2>{
            {"up", new Vector2(0f, -1f)}, {"down", new Vector2(0f, 1f)}, {"left", new Vector2(-1f, 0f)}, {"right", new Vector2(1f, 0f)},
            {"upleft", new Vector2(-1f, -1f)}, {"upright", new Vector2(1f, -1f)}, {"downleft", new Vector2(-1f, 1f)}, {"downright", new Vector2(1f, 1f)}
        };
        public Vector2 direction;

        public bool movesUp => direction.Y < 0f;
        public bool movesDown => direction.Y > 0f;
        public bool movesLeft => direction.X < 0f;
        public bool movesRight => direction.X > 0f;

        public WingedStrawberryDirectionController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            direction = directionToVector[data.Attr("direction", "up").ToLower()];
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

            // inject direction nonspecific movement code
            ILLabel label = cursor.DefineLabel();

            if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdfld<Strawberry>("flyingAway")) &&
            cursor.TryGotoPrev(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdarg(0),
            instr => instr.OpCode == OpCodes.Call && ((MethodReference)instr.Operand).Name.Contains("get_Y"))) {
                Logger.Log("SorbetHelper", $"Injecting custom flight movement at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                // skip over the vanilla code for moving the berry vertically if a controller exists
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Strawberry, bool>>((self) => {
                    return self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>() == null;
                });
                cursor.Emit(OpCodes.Brfalse, label);
                cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Call && ((MethodReference)instr.Operand).Name.Contains("set_Y"))
                .MarkLabel(label);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(Strawberry).GetField("flapSpeed", BindingFlags.NonPublic | BindingFlags.Instance));
                cursor.EmitDelegate<Action<Strawberry, float>>((self, flapSpeed) => {
                    WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
                    if (controller == null)
                        return;

                    self.Position += -controller.direction * flapSpeed * Engine.DeltaTime;
                });
            }

            // inject downwards and horizontal out of room bounds checks.
            ILLabel label2 = cursor.DefineLabel();

            if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.OpCode == OpCodes.Call && ((MethodReference)instr.Operand).Name.Contains("get_Y"))) {
                Logger.Log("SorbetHelper", $"Injecting additional out of room bounds checks at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                cursor.Emit(OpCodes.Ldarg_0);
                // check if the berry flies upwards and if it doesn't, skip past the vanilla code for removing the berry once it flies above the room bounds
                cursor.EmitDelegate<Func<Strawberry, bool>>((self) => {
                    WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
                    return controller == null || controller.movesUp;
                });
                cursor.Emit(OpCodes.Brfalse, label2);
                cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Call && ((MethodReference)instr.Operand).Name.Contains("RemoveSelf"))
                .MarkLabel(label2);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Strawberry>>(self => {
                    WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
                    if (controller == null)
                        return;

                    // out of bounds checks are only done for the directions the berry flies towards bc otherwise berries that would fly into the room from offscreen will be removed as soon as they start flying
                    if (controller.movesDown && self.Y > (float)(self.SceneAs<Level>().Bounds.Bottom + 16))
                        self.RemoveSelf();
                    if (controller.movesLeft && self.X < (float)(self.SceneAs<Level>().Bounds.Left - 24))
                        self.RemoveSelf();
                    if (controller.movesRight && self.X > (float)(self.SceneAs<Level>().Bounds.Right + 24))
                        self.RemoveSelf();
                });
            }

            // inject horizontal idle bounds checks alongside the vanilla vertical checks.
            if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdcR4(5f),
            instr => instr.MatchAdd(),
            instr => instr.OpCode == OpCodes.Call && ((MethodReference)instr.Operand).Name.Contains("set_Y"))) {
                Logger.Log("SorbetHelper", $"Injecting horizontal idle bounds checks at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(Strawberry).GetField("start", BindingFlags.NonPublic | BindingFlags.Instance));
                cursor.EmitDelegate<Action<Strawberry, Vector2>>((self, start) => {
                    // only perform horizontal idle bounds checks if a controller exists
                    WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
                    if (controller == null)
                        return;

                    if (self.X < start.X - 5f)
                        self.X = start.X - 5f;
                    else if (self.Y > start.Y + 5f)
                        self.X = start.X + 5f;
                });
            }
        }
    }
}
