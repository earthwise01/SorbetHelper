using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Cil;

namespace Celeste.Mod.SorbetHelper.Entities {

    [Tracked]
    [CustomEntity("SorbetHelper/DisplacementEffectBlocker")]
    public class DisplacementEffectBlocker : Entity {
        public readonly bool depthAdhering;

        public DisplacementEffectBlocker(EntityData data, Vector2 offset) : base(data.Position + offset) {
            base.Collider = new Hitbox(data.Width, data.Height);
            depthAdhering = data.Bool("depthAdhering", false);
            Depth = data.Int("depth", 0);
        }

        public static void Load() {
            IL.Celeste.DisplacementRenderer.BeforeRender += modBeforeRender;
        }

        public static void Unload() {
            IL.Celeste.DisplacementRenderer.BeforeRender -= modBeforeRender;
        }

        // this took way to long to figure out lmao
        private static void modBeforeRender(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchCall(typeof(Draw), "get_SpriteBatch"),
            instr => instr.MatchCallvirt<SpriteBatch>("End")) &&
            cursor.TryGotoPrev(MoveType.AfterLabel, instr => instr.MatchEndfinally())) {
                Logger.Log("SorbetHelper", $"Injecting check for DisplacementEffectBlocker at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                cursor.EmitLdarg1(); // scene
                cursor.EmitLdloc1(); // color
                cursor.EmitDelegate(renderDisplacementEffectBlockers);
            } else {
                Logger.Log(LogLevel.Error, "SorbetHelper", $"Failed to inject check for DisplacementEffectBlocker in CIL code for {cursor.Method.FullName}");
            }
        }

        private static void renderDisplacementEffectBlockers(Scene scene, Color color) {
            foreach (DisplacementEffectBlocker entity in scene.Tracker.GetEntities<DisplacementEffectBlocker>()) {
                if (!entity.depthAdhering)
                    Draw.Rect(entity.X, entity.Y, entity.Width, entity.Height, color);
            }
        }
    }
}
