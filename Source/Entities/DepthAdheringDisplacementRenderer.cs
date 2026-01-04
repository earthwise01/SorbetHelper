using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
public class DepthAdheringDisplacementRenderer : Entity {
    private readonly List<DepthAdheringDisplacementRenderHook> renderHooks = [];

    private readonly bool distortBehind;

    private DepthAdheringDisplacementRenderer(int depth, bool distortBehind) {
        Tag = Tags.Global;
        Depth = depth;
        this.distortBehind = distortBehind;
    }

    public void Track(DepthAdheringDisplacementRenderHook renderHook) => renderHooks.Add(renderHook);
    public void Untrack(DepthAdheringDisplacementRenderHook renderHook) => renderHooks.Remove(renderHook);

    public override void Render() {
        if (renderHooks.Count == 0)
            return;

        GameplayRenderer.End();

        Camera camera = SceneAs<Level>().Camera;

        VirtualRenderTarget entityBuffer = RenderTargetHelper.GetGameplayBuffer();
        VirtualRenderTarget displacementMapBuffer = RenderTargetHelper.GetGameplayBuffer();

        RenderTargetBinding[] prevRenderTargets = Engine.Instance.GraphicsDevice.GetRenderTargets();
        RenderTarget2D gameplayBuffer = GameplayBuffers.Gameplay;
        if (prevRenderTargets.Length > 0)
            gameplayBuffer = prevRenderTargets[0].RenderTarget as RenderTarget2D ?? gameplayBuffer;

        // step 1: prepare the displacement map

        Color noDisplacementColor = DisplacementEffectBlocker.NoDisplacementColor;

        Engine.Instance.GraphicsDevice.SetRenderTarget(displacementMapBuffer);
        Engine.Instance.GraphicsDevice.Clear(noDisplacementColor);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, camera.Matrix);

        foreach (DepthAdheringDisplacementRenderHook renderHook in renderHooks) {
            if (renderHook.EntityVisible)
                renderHook.RenderDisplacement();
        }

        List<Entity> displacementBlockers = Scene.Tracker.GetEntities<DisplacementEffectBlocker>();
        foreach (Entity entity in displacementBlockers) {
            if (entity is DisplacementEffectBlocker { Visible: true, DepthAdhering: true, WaterOnly: false } && entity.Depth <= Depth)
                Draw.Rect(entity.X, entity.Y, entity.Width, entity.Height, noDisplacementColor);
        }

        Draw.SpriteBatch.End();

        List<Entity> waterBlockers = displacementBlockers.Where(entity => entity is DisplacementEffectBlocker { Visible: true, DepthAdhering: true, WaterOnly: true } && entity.Depth <= Depth).ToList();
        if (waterBlockers.Count > 0) {
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, DisplacementEffectBlocker.WaterDisplacementBlockerBlendState, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, camera.Matrix);

            foreach (Entity entity in waterBlockers)
                Draw.Rect(entity.Position, entity.Width, entity.Height, DisplacementEffectBlocker.NoWaterDisplacementMultColor);

            Draw.SpriteBatch.End();
        }

        // prepare the entity buffer

        Engine.Instance.GraphicsDevice.SetRenderTarget(entityBuffer);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        GameplayRenderer.Begin();

        if (distortBehind)
            Draw.SpriteBatch.Draw(gameplayBuffer, camera.Position, Color.White);

        foreach (DepthAdheringDisplacementRenderHook renderHook in renderHooks) {
            if (renderHook.EntityVisible)
                renderHook.RenderEntity();
        }

        GameplayRenderer.End();

        // distort and render the result to the gameplay buffer

        Engine.Instance.GraphicsDevice.SetRenderTargets(prevRenderTargets);
        // clear the gameplay buffer if it is already included on the entity buffer
        if (distortBehind)
            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        // temporarily trick Distort.Render into only using the "Displace" technique as to not apply the anxiety effect
        float anxietyBackup = Distort.anxiety;
        float gamerateBackup = Distort.gamerate;
        Distort.anxiety = 0f;
        Distort.gamerate = 1f;

        // apply the displacement effect to the entity buffer and render the result to the gameplay buffer
        Distort.Render((RenderTarget2D)entityBuffer, (RenderTarget2D)displacementMapBuffer, hasDistortion: true);

        Distort.anxiety = anxietyBackup;
        Distort.gamerate = gamerateBackup;

        RenderTargetHelper.ReturnGameplayBuffer(entityBuffer);
        RenderTargetHelper.ReturnGameplayBuffer(displacementMapBuffer);

        GameplayRenderer.Begin();
    }

    public static DepthAdheringDisplacementRenderer GetRenderer(Scene scene, int depth, bool distortBehind) {
        if (scene.Tracker.GetEntities<DepthAdheringDisplacementRenderer>()
                         .Concat(scene.Entities.ToAdd)
                         .FirstOrDefault(e => e is DepthAdheringDisplacementRenderer r && r.Depth == depth && r.distortBehind == distortBehind)
                         is not DepthAdheringDisplacementRenderer renderer) {
            scene.Add(renderer = new DepthAdheringDisplacementRenderer(depth, distortBehind));
            Logger.Info("SorbetHelper", $"creating new DepthAdheringDisplacementRenderer at depth {depth} with distort behind {(distortBehind ? "enabled" : "disabled")}.");
        }

        return renderer;
    }
}
