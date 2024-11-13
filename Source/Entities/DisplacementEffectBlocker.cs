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
using System.Linq;

namespace Celeste.Mod.SorbetHelper.Entities {

    [Tracked]
    [CustomEntity("SorbetHelper/DisplacementEffectBlocker")]
    public class DisplacementEffectBlocker : Entity {
        public readonly bool DepthAdhering;
        public readonly bool WaterOnly;

        public static readonly Color NoDisplacementColor = new(0.5f, 0.5f, 0.0f, 1.0f);
        public static readonly Color NoWaterDisplacementMultColor = new(1.0f, 1.0f, 0.0f, 1.0f);
        public static readonly BlendState WaterDisplacementBlockerBlendState = new() {
            Name = "DisplacementEffectBlocker.WaterDisplacementBlocker",
            ColorSourceBlend = Blend.Zero,
            AlphaSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.SourceColor,
            AlphaDestinationBlend = Blend.One
        };

        public DisplacementEffectBlocker(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Collider = new Hitbox(data.Width, data.Height);
            DepthAdhering = data.Bool("depthAdhering", false);
            WaterOnly = data.Bool("waterOnly", false);
            Depth = data.Int("depth", 0);
        }

        public static void Load() {
            IL.Celeste.DisplacementRenderer.BeforeRender += IL_BeforeRender;
        }

        public static void Unload() {
            IL.Celeste.DisplacementRenderer.BeforeRender -= IL_BeforeRender;
        }

        private static void IL_BeforeRender(ILContext il) {
            ILCursor cursor = new(il) {
                Index = -1
            };

            if (!cursor.TryGotoPrev(MoveType.Before, instr => instr.MatchCallvirt<SpriteBatch>("End"))) {
                Logger.Log(LogLevel.Error, "SorbetHelper", $"failed to inject check for full DisplacementEffectBlockers in CIL code for {cursor.Method.Name}!");
                return;
            }

            Logger.Log("SorbetHelper", $"injecting check for full DisplacementEffectBlockers at {cursor.Index} in CIL code for {cursor.Method.Name}");
            cursor.EmitLdarg1();
            cursor.EmitDelegate(renderFullBlockers);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<SpriteBatch>("End"))) {
                Logger.Log(LogLevel.Error, "SorbetHelper", $"failed to inject check for water DisplacementEffectBlockers in CIL code for {cursor.Method.Name}!");
                return;
            }

            Logger.Log("SorbetHelper", $"injecting check for water DisplacementEffectBlockers at {cursor.Index} in CIL code for {cursor.Method.Name}");
            cursor.EmitLdarg1();
            cursor.EmitDelegate(renderWaterBlockers);
        }

        private static void renderFullBlockers(Scene scene) {
            foreach (var entity in scene.Tracker.GetEntities<DisplacementEffectBlocker>()) {
                if (entity is DisplacementEffectBlocker {DepthAdhering: false, WaterOnly: false}) {
                    Draw.Rect(entity.Position, entity.Width, entity.Height, NoDisplacementColor);
                }
            }
        }

        private static void renderWaterBlockers(Scene scene) {
            var waterBlockers = scene.Tracker.GetEntities<DisplacementEffectBlocker>().Where(entity => entity is DisplacementEffectBlocker {DepthAdhering: false, WaterOnly: true});
            if (!waterBlockers.Any())
                return;

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, WaterDisplacementBlockerBlendState, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, (scene as Level).Camera.Matrix);
            foreach (var entity in waterBlockers) {
                Draw.Rect(entity.Position, entity.Width, entity.Height, NoWaterDisplacementMultColor);
            }
            Draw.SpriteBatch.End();
        }
    }
}
