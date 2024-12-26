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
using System.Linq;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Components;

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
public class DepthAdheringDisplacementRenderHook : RenderOverride {
    public readonly Action renderEntity;
    public readonly Action renderDisplacement;
    public readonly bool distortBehind;

    public DepthAdheringDisplacementRenderHook(Action renderEntity, Action renderDisplacement, bool distortBehind) : base(active: false, visible: true) {
        this.renderEntity = renderEntity;
        this.renderDisplacement = renderDisplacement;
        this.distortBehind = distortBehind;
    }

    public override void EntityRenderOverride() {
        GameplayRenderer.End();

        var entityBuffer = RenderTargetHelper.GetGameplayBuffer();
        var displacementMapBuffer = RenderTargetHelper.GetGameplayBuffer();

        var gd = Engine.Instance.GraphicsDevice;
        var origRenderTargets = gd.GetRenderTargets();

        gd.SetRenderTarget(entityBuffer);
        gd.Clear(Color.Transparent);

        GameplayRenderer.Begin();

        // copy the gameplay buffer into the entity buffer if the component is set to also distort stuff behind the entity
        Camera camera = SceneAs<Level>().Camera;
        if (distortBehind)
            Draw.SpriteBatch.Draw((RenderTarget2D)origRenderTargets[0].RenderTarget, camera.Position, Color.White);

        renderEntity();

        GameplayRenderer.End();

        // displacement map rendering stuff
        Color displacementBgColor = DisplacementEffectBlocker.NoDisplacementColor;

        gd.SetRenderTarget(displacementMapBuffer);
        gd.Clear(displacementBgColor);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, camera.Matrix);

        renderDisplacement();

        var displacementBlockers = Scene.Tracker.GetEntities<DisplacementEffectBlocker>();
        // support for displacement effect blockers
        foreach (var entity in displacementBlockers) {
            if (entity is DisplacementEffectBlocker { DepthAdhering: true, WaterOnly: false } && entity.Depth <= Entity.Depth) {
                Draw.Rect(entity.X, entity.Y, entity.Width, entity.Height, displacementBgColor);
            }
        }

        Draw.SpriteBatch.End();

        // water only displacement blockers
        var waterBlockers = displacementBlockers.Where(entity => entity is DisplacementEffectBlocker { DepthAdhering: true, WaterOnly: true } && entity.Depth <= Entity.Depth);
        if (waterBlockers.Any()) {
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, DisplacementEffectBlocker.WaterDisplacementBlockerBlendState, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, camera.Matrix);

            foreach (var entity in waterBlockers) {
                Draw.Rect(entity.Position, entity.Width, entity.Height, DisplacementEffectBlocker.NoWaterDisplacementMultColor);
            }

            Draw.SpriteBatch.End();
        }

        gd.SetRenderTargets(origRenderTargets);
        // if distortBehind is enabled, clear the gameplay buffer first before drawing the result (since in this case it also already includes a copy of the gameplay buffer alongside the entity)
        if (distortBehind)
            gd.Clear(Color.Transparent);

        // temporarily force the anxiety effect off to stop the game from applying it multiple times
        // done manually rather than via Distort.Anxiety to prevent issues with ExtendedVariants
        // this is gonna affect literally no-one but at the very least this means i don't have to freak out over the fact that i knowingly left a bug in
        float anxietyBackup = GFX.FxDistort.Parameters["anxiety"].GetValueSingle();
        GFX.FxDistort.Parameters["anxiety"].SetValue(0f);

        // apply the displacement effect to the entity buffer and render the result to the main gameplay buffer
        Distort.Render((RenderTarget2D)entityBuffer, (RenderTarget2D)displacementMapBuffer, hasDistortion: true);

        GFX.FxDistort.Parameters["anxiety"].SetValue(anxietyBackup);

        RenderTargetHelper.ReturnGameplayBuffer(entityBuffer);
        RenderTargetHelper.ReturnGameplayBuffer(displacementMapBuffer);

        GameplayRenderer.Begin();
    }
}
