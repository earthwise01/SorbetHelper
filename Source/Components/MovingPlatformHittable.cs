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

    [Tracked]
    public class MovingPlatformHittable : Component {
        public delegate void PlatformHitCallback(Platform platform, Vector2 direction);
        public PlatformHitCallback OnHit;

        private readonly bool breakDashBlocksRequired;

        public MovingPlatformHittable(PlatformHitCallback onHit, bool breakDashBlocksRequired = true) : base(false, false) {
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
            ILCursor cursor = new(il);

            // go to *just* before the check for dash blocks, afterlabel is needed since this is at the start of the movement loop
            if (!cursor.TryGotoNext(MoveType.AfterLabel, instr => instr.MatchLdarg2(), instr => instr.MatchBrfalse(out _))) {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject code to make horizontal falling blocks/kevins/etc activate moving platform hittable components in CIL code for {cursor.Method.Name}");
                return;
            }

            Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting code to make horizontal falling blocks/kevins/etc activate moving platform hittable components at {cursor.Index} in CIL code for {cursor.Method.Name}");

            cursor.EmitLdarg0(); // this
            cursor.EmitLdloc1(); // direction sign
            cursor.EmitLdarg2(); // breakDashBlocks
            cursor.EmitDelegate(activateMovingPlatformHittablesH);
        }

        private static void modPlatformMoveVExactCollideSolids(ILContext il) {
            ILCursor cursor = new(il);

            // go to *just* before the check for dash blocks, afterlabel is needed since this is at the start of the movement loop
            if (!cursor.TryGotoNext(MoveType.AfterLabel, instr => instr.MatchLdarg2(), instr => instr.MatchBrfalse(out _))) {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject code to make vertical falling blocks/kevins/etc activate moving platform hittable components in CIL code for {cursor.Method.Name}");
                return;
            }

            Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting code to make vertical falling blocks/kevins/etc activate moving platform hittable components at {cursor.Index} in CIL code for {cursor.Method.Name}");

            cursor.EmitLdarg0(); // this
            cursor.EmitLdloc1(); // direction sign
            cursor.EmitLdarg2(); // breakDashBlocks
            cursor.EmitDelegate(activateMovingPlatformHittablesV);
        }

        private static void activateMovingPlatformHittablesH(Platform self, int directionSign, bool breakDashBlocks) => activateMovingPlatformHittables(self, new(directionSign, 0f), breakDashBlocks);
        private static void activateMovingPlatformHittablesV(Platform self, int directionSign, bool breakDashBlocks) => activateMovingPlatformHittables(self, new(0f, directionSign), breakDashBlocks);
        private static void activateMovingPlatformHittables(Platform self, Vector2 direction, bool breakDashBlocks) {
            var toHit = self.CollideAllByComponent<MovingPlatformHittable>(self.Position + direction);

            foreach (var platformHittable in toHit) {
                if (!platformHittable.breakDashBlocksRequired || breakDashBlocks)
                    platformHittable.OnHit(self, direction);
            }
        }
    }
}
