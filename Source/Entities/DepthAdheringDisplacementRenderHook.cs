using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Celeste.Mod.Entities;
using Mono.Cecil;

namespace Celeste.Mod.SorbetHelper.Entities {

    /*
        some personal notes, probably not necessary but due to the relative messiness of all of this i felt the need to document What and Why i did what i did here

        what this attempts to do is apply displacement in the middle of rendering gameplay as opposed to afterwards like a normal displacement render hook does.
        the implementation i've ended up i've come up with can probably be improved on somewhat, both in terms of performance and also visual parity (there are a few minor differences compared to normal displacement i think),
        but i'll take it since it seems to somehow have ended up actually working?? which is a first for any cursed workarounds for silly minor effects i've tried

        roughly how it works is when added to an entity it gets passed both the entity's render method and a render displacement method, which then are used in the component's render method to render the entity to the screen
        during gameplay rendering instead of the entity's normal render method, due to a hook which makes the game attempt to render its component instead of the entity if it has one.

        also if anything here seems like a weird thing to do its probably because i had/still have basically no clue how rendering actually works so im suprised this even ended up working at all,
        granted a most of this is. pretty much the result of tons of trial and error and just  throwing stuff together until it works over probably too much time but still.
        (like Oh My God basically everything in here, even the Comments and Variable Names, have been through a. somewhat ridiculous amount uncommited revisions)
    */

    [Tracked]
    public class DepthAdheringDisplacementRenderHook : Component {
        public readonly Action renderEntity;
        public readonly Action renderDisplacement;
        public readonly bool distortBehind;

        private VirtualRenderTarget entityBuffer;
        private VirtualRenderTarget displacementMapBuffer;
        private static bool renderingCustomDisplacement;

        public DepthAdheringDisplacementRenderHook(Action renderEntity, Action renderDisplacement, bool distortBehind) : base(active: false, visible: true) {
            this.renderEntity = renderEntity;
            this.renderDisplacement = renderDisplacement;
            this.distortBehind = distortBehind;
        }

        public override void Render() {
            // somewhat of a hackfix to try and prevent an infinite loop if renderEntity points to the entity's normal render method and that also calls Components.Render
            if (renderingCustomDisplacement)
                return;

            renderingCustomDisplacement = true;

            base.Render();

            GameplayRenderer.End();

            if (entityBuffer == null)
                entityBuffer = VirtualContent.CreateRenderTarget("depth-adhering-displacement-render-hook-entity-buffer", 320, 180);
            if (displacementMapBuffer == null)
                displacementMapBuffer = VirtualContent.CreateRenderTarget("depth-adhering-displacement-render-hook-displacementmap-buffer", 320, 180);

            Engine.Instance.GraphicsDevice.SetRenderTarget(entityBuffer);
            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

            GameplayRenderer.Begin();

            // copy the gameplay buffer into the entity buffer if the component is set to also distort stuff behind the entity
            Camera camera = SceneAs<Level>().Camera;
            if (distortBehind)
                Draw.SpriteBatch.Draw((RenderTarget2D)GameplayBuffers.Gameplay, camera.Position, Color.White);

            renderEntity();

            GameplayRenderer.End();

            // displacement map rendering stuff
            Color displacementBgColor = new Color(0.5f, 0.5f, 0f, 1f);

            Engine.Graphics.GraphicsDevice.SetRenderTarget(displacementMapBuffer);
            Engine.Graphics.GraphicsDevice.Clear(displacementBgColor);

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, camera.Matrix);

            renderDisplacement();

            // support for displacement effect blockers
            foreach (DisplacementEffectBlocker entity in Scene.Tracker.GetEntities<DisplacementEffectBlocker>()) {
                if (entity.depthAdhering && entity.Depth <= Entity.Depth)
                    Draw.Rect(entity.X, entity.Y, entity.Width, entity.Height, displacementBgColor);
            }

            Draw.SpriteBatch.End();

            Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
            // if distortBehind is enabled, clear the gameplay buffer first before drawing the result (since in this case it also already includes a copy of the gameplay buffer alongside the entity)
            if (distortBehind)
                Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

            // temporarily force the anxiety effect off to stop the game from applying it multiple times
            // done manually rather than via Distort.Anxiety to prevent issues with ExtendedVariants
            // this is gonna affect literally no-one but at the very least this means i don't have to freak out over the fact that i knowingly left a bug in
            float anxietyBackup = GFX.FxDistort.Parameters["anxiety"].GetValueSingle();
            GFX.FxDistort.Parameters["anxiety"].SetValue(0f);

            // apply the displacement effect to the entity buffer and render the result to the main gameplay buffer
            Distort.Render((RenderTarget2D)entityBuffer, (RenderTarget2D)displacementMapBuffer, hasDistortion: true);

            GFX.FxDistort.Parameters["anxiety"].SetValue(anxietyBackup);

            GameplayRenderer.Begin();
            renderingCustomDisplacement = false;
        }

        public override void SceneEnd(Scene scene) {
            Dispose();
            base.SceneEnd(scene);
        }

        public override void Removed(Entity entity) {
            Dispose();
            base.Removed(entity);
        }

        private void Dispose() {
            entityBuffer?.Dispose();
            displacementMapBuffer?.Dispose();

            entityBuffer = null;
            displacementMapBuffer = null;
        }

        public static void Load() {
            IL.Monocle.EntityList.RenderExcept += modEntityListRenderExcept;
        }

        public static void Unload() {
            IL.Monocle.EntityList.RenderExcept -= modEntityListRenderExcept;
        }

        private static void modEntityListRenderExcept(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            VariableDefinition renderHooksList = new(il.Import(typeof(List<Component>)));
            il.Body.Variables.Add(renderHooksList);

            // get render hook list
            cursor.EmitLdarg0();
            cursor.EmitCallvirt(typeof(EntityList).GetMethod("get_Scene"));
            cursor.EmitDelegate(getRenderHooks);
            cursor.EmitStloc(renderHooksList);

            ILLabel label = cursor.DefineLabel();

            if (cursor.TryGotoNext(MoveType.Before,
              instr => instr.MatchLdloc1(),
              instr => instr.MatchCallvirt<Entity>("Render"))) {
                // check if the entity has a DepthAdheringDisplacementRenderHook, if it does skip rendering the entity and render it instead
                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting check to render DepthAdheringDisplacementRenderHooks instead of their entity at {cursor.Index} in CIL code for {cursor.Method.FullName}");
                cursor.EmitLdloc1();
                cursor.EmitLdloc(renderHooksList);
                cursor.EmitDelegate(renderDepthAdheringRenderHooks);
                cursor.EmitBrtrue(label);

                cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Entity>("Render"));

                cursor.MarkLabel(label);
            } else {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject check to render DepthAdheringDisplacementRenderHooks instead of their entity in CIL code for {cursor.Method.FullName}");
            }
        }

        private static List<Component> getRenderHooks(Scene scene) {
            if (scene == null)
                return null;

            // the tracker is used here instead of searching through each entity's components as to not something something
            return scene.Tracker.GetComponents<DepthAdheringDisplacementRenderHook>();
        }

        private static bool renderDepthAdheringRenderHooks(Entity entity, List<Component> renderHooks) {
            if (renderHooks == null)
                return false;

            foreach (Component component in renderHooks) {
                if (component.Entity == entity) {
                    component.Render();
                    return true;
                }
            }

            return false;
        }
    }
}
