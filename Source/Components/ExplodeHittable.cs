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
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.SorbetHelper.Components {

    [Tracked]
    public class ExplodeHittable : Component {
        public delegate void ExplodeHitCallback(Entity entity, Vector2 direction);
        public ExplodeHitCallback OnHit;

        public ExplodeHittable(ExplodeHitCallback onHit) : base(false, false) {
            OnHit = onHit;
        }

        private static ILHook seekerRegenerateCoroutineHook;

        internal static void Load() {
            seekerRegenerateCoroutineHook = new ILHook(
                typeof(Seeker).GetMethod("RegenerateCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                IL_Seeker_RegenerateCoroutine
            );
            IL.Celeste.Puffer.Explode += IL_Puffer_Explode;
        }

        internal static void Unload() {
            seekerRegenerateCoroutineHook?.Dispose();
            seekerRegenerateCoroutineHook = null;
            IL.Celeste.Puffer.Explode -= IL_Puffer_Explode;
        }

        private static void IL_Seeker_RegenerateCoroutine(ILContext il) {
            ILCursor cursor = new(il);
            int seekerVariable = 1;

            if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdloc(out seekerVariable),
            instr => instr.MatchCallOrCallvirt<Entity>("CollideFirst"),
            instr => instr.MatchStloc(out _))) {
                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting code to make seeker regenerate explosions activate explode hittable components at {cursor.Index} in CIL code for {cursor.Method.Name}");

                cursor.EmitLdloc(seekerVariable);
                cursor.EmitDelegate(activateExplodeHittables);
            } else {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject code to make seeker regenerate explosions activate explode hittable components in CIL code for {cursor.Method.Name}!");
            }
        }

        private static void IL_Puffer_Explode(ILContext il) {
            ILCursor cursor = new(il);
            int pufferVariable = 0;

            if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdarg(out pufferVariable),
            instr => instr.MatchCallOrCallvirt<Entity>("CollideFirst"),
            instr => instr.MatchStloc(out _))) {
                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting code to make puffer explosions activate explode hittable components at {cursor.Index} in CIL code for {cursor.Method.Name}");

                cursor.EmitLdarg0();
                cursor.EmitDelegate(activateExplodeHittables);
            } else {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject code to make puffer explosions activate explode hittable components in CIL code for {cursor.Method.Name}!");
            }
        }

        private static void activateExplodeHittables(Entity self) {
            var toHit = self.CollideAllByComponent<ExplodeHittable>();

            foreach (var expolodeHittable in toHit) {
                expolodeHittable.OnHit(self, expolodeHittable.Entity.Center - self.Position);
            }
        }
    }
}
