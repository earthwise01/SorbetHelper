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

        public override void Update()
        {
            base.Update();
        }

        public static void Load() {
            strawberryUpdateHook = new ILHook(typeof(Strawberry).GetMethod("orig_Update"), modFlyDirection);
        }

        public static void Unload() {
            strawberryUpdateHook.Dispose();
        }

        private static bool movesUpCheck(Strawberry self) {
            WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
            if (controller == null || controller.direction == Directions.Up || controller.direction == Directions.UpLeft  || controller.direction == Directions.UpRight) {
                return true;
            }
            return false;
        }

        private static void modFlyDirection(ILContext il) {
            // this is probably an absolute mess but aaaaaaa it works i guess?????
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Strawberry>("flyingAway"))) {
                Logger.Log("sdksdkjhflaksdjfslakfdh", $"Applying hook at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                ILLabel l = cursor.DefineLabel();

                if (cursor.TryGotoPrev(MoveType.Before,
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdarg(0),
                instr => instr.OpCode == OpCodes.Call && ((MethodReference) instr.Operand).Name.Contains("get_Y"))) {
                    Logger.Log("sadksdkjhflaksdjfslakfdh", $"Applying hook at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate<Func<Strawberry, bool>>(movesUpCheck);
                    cursor.Emit(OpCodes.Brfalse, l);
                    cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Call && ((MethodReference) instr.Operand).Name.Contains("set_Y"))
                    .MarkLabel(l);

                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldfld, typeof(Strawberry).GetField("flapSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
                    cursor.EmitDelegate<Action<Strawberry, float>>((self, flapSpeed) => {
                        WingedStrawberryDirectionController controller = self.Scene.Tracker.GetEntity<WingedStrawberryDirectionController>();
                        if (controller != null && controller.direction != Directions.Up) {
                            switch (controller.direction) {
                                case Directions.Down:
                                    self.Y -= flapSpeed * Engine.DeltaTime;
                                    break;
                                case Directions.Left:
                                    self.X += flapSpeed * Engine.DeltaTime;
                                    break;
                                case Directions.Right:
                                    self.X -= flapSpeed * Engine.DeltaTime;
                                    break;
                                case Directions.UpLeft:
                                    // the up part is handled normally by the game
                                    self.X += flapSpeed * Engine.DeltaTime; // left
                                    break;
                                case Directions.UpRight:
                                    // the up part is handled normally by the game
                                    self.X -= flapSpeed * Engine.DeltaTime; // right
                                    break;
                                case Directions.DownLeft:
                                    self.Y -= flapSpeed * Engine.DeltaTime; // down
                                    self.X += flapSpeed * Engine.DeltaTime; // left
                                    break;
                                case Directions.DownRight:
                                    self.Y -= flapSpeed * Engine.DeltaTime; // down
                                    self.X -= flapSpeed * Engine.DeltaTime; // right
                                    break;
                                default:
                                    break;
                            }
                        }
                    });
                }
            }
        }
    }    
}