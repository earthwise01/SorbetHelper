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
        public enum Directions {Up, Down, Left, Right, UpLeft, UpRight, DownLeft, DownRight};
        public Directions direction;

        public WingedStrawberryDirectionController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            direction = data.Enum("direction", Directions.Up);
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

            // inject code for moving the berry downwards and horizontally.
            ILLabel label = cursor.DefineLabel();

            if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdfld<Strawberry>("flyingAway")) &&
            cursor.TryGotoPrev(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdarg(0),
            instr => instr.OpCode == OpCodes.Call && ((MethodReference) instr.Operand).Name.Contains("get_Y"))) {
                Logger.Log("SorbetHelper", $"Injecting custom flight movement at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                // skip over the vanilla code for moving the berry upwards if the berry shouldn't fly up.
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Strawberry, bool>>(movesUpCheck);
                cursor.Emit(OpCodes.Brfalse, label);
                cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Call && ((MethodReference) instr.Operand).Name.Contains("set_Y"))
                .MarkLabel(label);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(Strawberry).GetField("flapSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
                cursor.EmitDelegate<Action<Strawberry, float>>((self, flapSpeed) => {
                    WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
                    if (controller != null && controller.direction != Directions.Up) {
                        if (movesDownCheck(self))
                            self.Y -= flapSpeed * Engine.DeltaTime;
                        if (movesLeftCheck(self))
                            self.X += flapSpeed * Engine.DeltaTime;
                        if (movesRightCheck(self))
                            self.X -= flapSpeed * Engine.DeltaTime;
                    }
                });
            }

            // inject downwards and horizontal out of room bounds checks.
            ILLabel label2 = cursor.DefineLabel();

            if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.OpCode == OpCodes.Call && ((MethodReference) instr.Operand).Name.Contains("get_Y"))) {
                Logger.Log("SorbetHelper", $"Injecting additional out of room bounds checks at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                // skip over the vanilla code for removing the berry once it flies above the room bounds if the berry shouldn't fly up.
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Strawberry, bool>>(movesUpCheck);
                cursor.Emit(OpCodes.Brfalse, label2);
                cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Call && ((MethodReference) instr.Operand).Name.Contains("RemoveSelf"))
                .MarkLabel(label2);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Strawberry>>(self => {
                    WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
                    if (controller != null && controller.direction != Directions.Up) {
                        if (movesDownCheck(self) && self.Y > (float)(self.SceneAs<Level>().Bounds.Bottom + 16))
                            self.RemoveSelf();
                        if (movesLeftCheck(self) && self.X < (float)(self.SceneAs<Level>().Bounds.Left - 24))
                            self.RemoveSelf();
                        if (movesRightCheck(self) && self.X > (float)(self.SceneAs<Level>().Bounds.Right + 24))
                            self.RemoveSelf();
                    }
                });
            }

            // inject horizontal idle bounds checks alongside the vanilla vertical checks.
            ILLabel label3 = cursor.DefineLabel();

            if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.OpCode == OpCodes.Call && ((MethodReference) instr.Operand).Name.Contains("get_Y"))) {
                Logger.Log("SorbetHelper", $"Injecting horizontal idle bounds checks at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                // skip over the vanilla vertical bounds checks if the berry doesn't fly vertically.
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Strawberry, bool>>(self => { return movesUpCheck(self) || movesDownCheck(self); });
                cursor.Emit(OpCodes.Brfalse, label3);
                cursor.GotoNext(MoveType.After, instr => instr.MatchAdd(), instr => instr.OpCode == OpCodes.Call && ((MethodReference) instr.Operand).Name.Contains("set_Y"))
                .MarkLabel(label3);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(Strawberry).GetField("start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
                cursor.EmitDelegate<Action<Strawberry, Vector2>>((self, start) => {
                    if (movesLeftCheck(self) || movesRightCheck(self)) {
                        if (self.X < start.X - 5f)
                            self.X = start.X - 5f;
                        else if (self.Y > start.Y + 5f)
                            self.X = start.X + 5f;
                    }
                });
            }
        }

        // returns true only if a controller either doesn't exist or has its direction set to move up.
        private static bool movesUpCheck(Entity self) {
            WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
            return controller == null || controller.direction == Directions.Up || controller.direction == Directions.UpLeft  || controller.direction == Directions.UpRight;
        }

        // returns true only if a controller both exists and has its direction set to move down.
        private static bool movesDownCheck(Entity self) {
            WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
            return controller != null && (controller.direction == Directions.Down || controller.direction == Directions.DownLeft  || controller.direction == Directions.DownRight);
        }

        // returns true only if a controller both exists and has its direction set to move left.
        private static bool movesLeftCheck(Entity self) {
            WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
            return controller != null && (controller.direction == Directions.Left || controller.direction == Directions.UpLeft  || controller.direction == Directions.DownLeft);
        }

        // returns true only if a controller both exists and has its direction set to move right.
        private static bool movesRightCheck(Entity self) {
            WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
            return controller != null && (controller.direction == Directions.Right || controller.direction == Directions.UpRight  || controller.direction == Directions.DownRight);
        }
    }    
}
