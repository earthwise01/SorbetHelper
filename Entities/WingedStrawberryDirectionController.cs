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
using MonoMod.RuntimeDetour;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Celeste;

namespace Celeste.Mod.SorbetHelper.Entities {

    public class WingedStrawberryDirectionController : Entity {

        private static ILHook strawberryUpdateHook;
        public WingedStrawberryDirectionController(EntityData data, Vector2 offset) : base(data.Position + offset) {

        }

        public static void Load() {
            //IL.Celeste.Strawberry.Update += modFlyDirection;
            strawberryUpdateHook = new ILHook(typeof(Strawberry).GetMethod("orig_Update"), modFlyDirection);
        }

        public static void Unload() {
            //IL.Celeste.Strawberry.orig_Update += modFlyDirection;
            strawberryUpdateHook.Dispose();
        }

        private static void modFlyDirection(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Strawberry>("flyingAway"))) {
                Logger.Log("sdkjfasdkjhflaksdjfslakfdh", $"Applying hook at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                //cursor.EmitDelegate<Func<float>>(determineDashLengthFactor);
                //cursor.Emit(OpCodes.Mul);
                if (cursor.TryGotoPrev(MoveType.Before, instr => instr.MatchAdd())) {
                    cursor.Remove();
                    cursor.Emit(OpCodes.Sub);
                    return;
                }
                //cursor.GotoPrev(MoveType.Before, instr => instr.MatchAdd());


            }
        }
    }    
}