using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using Mono.Cecil;
using MonoMod.Cil;
using Celeste.Mod.Entities;

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
            On.Celeste.DisplacementRenderer.BeforeRender += onBeforeRender;
        }

        public static void Unload() {
            On.Celeste.DisplacementRenderer.BeforeRender -= onBeforeRender;
        }

        // probably kinda bad to start the spritebatch again immediatley after it stops but like for some reason using an il hook basically just refused to work?? so thisll have to do i guess (like literally it didnt even crash it just did. absolutely nothing and i  have no freaking clue what i was doing worng)
        private static void onBeforeRender(On.Celeste.DisplacementRenderer.orig_BeforeRender orig, DisplacementRenderer self, Scene scene) {
            orig(self, scene);

            if (scene.Tracker.GetEntities<DisplacementEffectBlocker>().Count != 0) {
                Color color = new Color(0.5f, 0.5f, 0f, 1f);
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, (scene as Level).Camera.Matrix);

                foreach (DisplacementEffectBlocker entity in scene.Tracker.GetEntities<DisplacementEffectBlocker>()) {
                    if (!entity.depthAdhering)
                        Draw.Rect(entity.X, entity.Y, entity.Width, entity.Height, color);
                }

                Draw.SpriteBatch.End();
            }
        }
    }
}
