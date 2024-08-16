using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste;
using Celeste.Mod.SorbetHelper.Entities;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Celeste.Mod.SorbetHelper.Components {
    public class MovingBlockHittable : Component {
        public Action<Vector2> OnHit;
        private readonly bool breakDashBlocksRequired;

        public MovingBlockHittable(Action<Vector2> onHit, bool breakDashBlocksRequired = true) : base(false, false) {
            OnHit = onHit;
            this.breakDashBlocksRequired = breakDashBlocksRequired;
        }

        internal static void Load() {
            IL.Celeste.Platform.MoveHExactCollideSolids += modPlatformMoveHExactCollideSolids;
            IL.Celeste.Platform.MoveVExactCollideSolids += modPlatformMoveVExactCollideSolids;
        }

        internal static void Unload() {
            IL.Celeste.Platform.MoveHExactCollideSolids -= modPlatformMoveHExactCollideSolids;
            IL.Celeste.Platform.MoveVExactCollideSolids -= modPlatformMoveVExactCollideSolids;
        }

        private static void modPlatformMoveHExactCollideSolids(ILContext il) {
            ILCursor cursor = new(il) {
                Index = -1
            };

            if (cursor.TryGotoPrev(MoveType.After,
            instr => instr.MatchLdarg(out _),
            instr => instr.MatchLdloc(out _),
            instr => instr.MatchCallOrCallvirt<Platform>("MoveHExact"))) {
                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting code to make horizontal falling blocks/kevins/etc activate moving block hittable components at {cursor.Index} in CIL code for {cursor.Method.Name}");

                cursor.EmitLdloc3(); // collided solid
                cursor.EmitLdloc1(); // direction sign
                cursor.EmitLdarg2(); // breakDashBlocks
                cursor.EmitDelegate(makeHorizontalMovingPlatformsActiveMovingBlockHittableComponents);
            } else {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject code to make horizontal falling blocks/kevins/etc activate moving block hittable components in CIL code for {cursor.Method.Name}");
            }
        }

        private static void modPlatformMoveVExactCollideSolids(ILContext il) {
            ILCursor cursor = new ILCursor(il) {
                Index = -1
            };

            if (cursor.TryGotoPrev(MoveType.After,
            instr => instr.MatchLdarg(out _),
            instr => instr.MatchLdloc(out _),
            instr => instr.MatchCallOrCallvirt<Platform>("MoveVExact"))) {
                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting code to make vertical falling blocks/kevins/etc activate moving block hittable components at {cursor.Index} in CIL code for {cursor.Method.Name}");

                cursor.EmitLdloc3(); // collided solid
                cursor.EmitLdloc1(); // direction sign
                cursor.EmitLdarg2(); // breakDashBlocks
                cursor.EmitDelegate(makeVerticalMovingPlatformsActiveMovingBlockHittableComponents);
            } else {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject code to make vertical falling blocks/kevins/etc activate moving block hittable components in CIL code for {cursor.Method.Name}");
            }
        }

        private static void makeHorizontalMovingPlatformsActiveMovingBlockHittableComponents(Platform collided, int directionSign, bool breakDashBlocks) {
            MovingBlockHittable component;
            if (collided is not null && (component = collided.Get<MovingBlockHittable>()) is not null) {
                if (component.breakDashBlocksRequired && !breakDashBlocks)
                    return;

                component.OnHit(Vector2.UnitX * directionSign);
            }
        }

        private static void makeVerticalMovingPlatformsActiveMovingBlockHittableComponents(Platform collided, int directionSign, bool breakDashBlocks) {
            MovingBlockHittable component;
            if (collided is not null && (component = collided.Get<MovingBlockHittable>()) is not null) {
                if (component.breakDashBlocksRequired && !breakDashBlocks)
                    return;

                component.OnHit(Vector2.UnitY * directionSign);
            }
        }
    }
}
