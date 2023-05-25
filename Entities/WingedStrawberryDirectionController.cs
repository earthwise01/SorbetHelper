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

            // modify the code to make the strawberry fly in the correct direction
            ILLabel label = cursor.DefineLabel();

            if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdfld<Strawberry>("flyingAway")) &&
            cursor.TryGotoPrev(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdarg(0),
            instr => instr.OpCode == OpCodes.Call && ((MethodReference) instr.Operand).Name.Contains("get_Y"))) {
                Logger.Log("SorbetHelper", $"Injecting custom flight movement at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                // skip over the vanilla code for moving the berry up if a controller both exists and doesn't have its direction set to something that goes up.
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
                        // moving up isn't handled here, as it's already done in the vanilla code.
                        if (movesDownCheck(self))
                            self.Y -= flapSpeed * Engine.DeltaTime;
                        if (movesLeftCheck(self))
                            self.X += flapSpeed * Engine.DeltaTime;
                        if (movesRightCheck(self))
                            self.X -= flapSpeed * Engine.DeltaTime;
                    }
                });
            }

            // reset the cursor
            cursor.Index = 0;

            // mofify the code that removes the berry if it leaves the room
            ILLabel label2 = cursor.DefineLabel();

            if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdfld<Strawberry>("flyingAway")) &&
            cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.OpCode == OpCodes.Call && ((MethodReference) instr.Operand).Name.Contains("get_Y"))) {
                Logger.Log("SorbetHelper", $"Injecting additional out of bounds checks at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                // skip over the vanilla code for removing the berry once it goes above the room if a controller both exists and doesn't have its direction set to something that goes up.
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Strawberry, bool>>(movesUpCheck);
                cursor.Emit(OpCodes.Brfalse, label2);
                cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Call && ((MethodReference) instr.Operand).Name.Contains("RemoveSelf"))
                .MarkLabel(label2);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Strawberry>>(self => {
                    WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
                    if (controller != null && controller.direction != Directions.Up) {
                        // removing the berry if it goes up above the room isn't handled here, as it's already done in the vanilla code.
                        if (movesDownCheck(self) && self.Y > (float)(self.SceneAs<Level>().Bounds.Bottom + 16))
							self.RemoveSelf();
                        if (movesLeftCheck(self) && self.X < (float)(self.SceneAs<Level>().Bounds.Left - 24))
							self.RemoveSelf();
                        if (movesRightCheck(self) && self.X > (float)(self.SceneAs<Level>().Bounds.Right + 24))
							self.RemoveSelf();
                    }
                });
            }
        }

        private static bool movesUpCheck(Entity self) {
            WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
            return controller == null || controller.direction == Directions.Up || controller.direction == Directions.UpLeft  || controller.direction == Directions.UpRight;
        }

        private static bool movesDownCheck(Entity self) {
            WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
            return controller != null && (controller.direction == Directions.Down || controller.direction == Directions.DownLeft  || controller.direction == Directions.DownRight);
        }

        private static bool movesLeftCheck(Entity self) {
            WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
            return controller != null && (controller.direction == Directions.Left || controller.direction == Directions.UpLeft  || controller.direction == Directions.DownLeft);
        }

        private static bool movesRightCheck(Entity self) {
            WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
            return controller != null && (controller.direction == Directions.Right || controller.direction == Directions.UpRight  || controller.direction == Directions.DownRight);
        }
    }    
}
