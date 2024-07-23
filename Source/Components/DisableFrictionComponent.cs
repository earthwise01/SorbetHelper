using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Entities;
using MonoMod.Cil;

namespace Celeste.Mod.SorbetHelper.Components {
    public class DisableFrictionComponent : Component {
        public DisableFrictionComponent() : base(false, false) { }

        public static void Load() {
            IL.Celeste.Solid.MoveHExact += modSolidMoveHVExact;
            IL.Celeste.Solid.MoveVExact += modSolidMoveHVExact;
        }

        public static void Unload() {
            IL.Celeste.Solid.MoveHExact -= modSolidMoveHVExact;
            IL.Celeste.Solid.MoveVExact -= modSolidMoveHVExact;
        }

        private static void modSolidMoveHVExact(ILContext il) {
            ILCursor cursor = new ILCursor(il) {
                Index = -1
            };

            if (cursor.TryGotoPrev(MoveType.After,
                instr => instr.MatchLdloc(out int _),
                instr => instr.MatchCallOrCallvirt<HashSet<Actor>>("Contains"))) {
                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting check to disable friction at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                cursor.EmitLdarg0();
                cursor.EmitDelegate(frictionCheck);
            } else {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject check to disable friction in CIL code for {cursor.Method.FullName}!");
            }
        }

        private static bool frictionCheck(bool orig, Entity self) {
            return orig && self.Get<DisableFrictionComponent>() == null;
        }
    }
}

