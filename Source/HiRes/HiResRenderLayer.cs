using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.SorbetHelper.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.SorbetHelper.HiRes;

// references communal helper's DreamSpriteRenderer and Shape
// https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/Entities/Misc/DreamSpriteRenderer.cs
[Tracked]
public class HiResRenderLayer : Entity {
    private readonly int rendererDepth;
    private readonly List<HiResRenderHook> renderHooks = [];

    public HiResRenderLayer(int rendererDepth) {
        Depth = this.rendererDepth = rendererDepth;
        Tag = Tags.Global;
    }

    public void Track(HiResRenderHook renderHook) => renderHooks.Add(renderHook);
    public void Untrack(HiResRenderHook renderHook) => renderHooks.Remove(renderHook);

    public override void Added(Scene scene) {
        base.Added(scene);

        if (!HiResRenderer.Enabled) {
            Logger.Warn("SorbetHelper", "Failed to add hi-res layer! Hi-res rendering is not enabled.");
            Scene.Tracker.GetEntity<MiniPopupDisplay>()?.CreatePopup(10f, "sorbethelper_failedtoaddhiresrenderlayer", "sorbethelper_hiresdisabled");
        }
    }

    public override void Render() {
        if ((RenderTarget2D)Engine.Graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget != GameplayBuffers.Gameplay.Target)
            Logger.Warn("SorbetHelper", "oh its so over for me");

        if (!HiResRenderer.Enabled)
            return;

        GameplayRenderer.End();
        HiResRenderer.FlushGameplayBuffer();

        var matrix = (Scene as Level).Camera.Matrix * Matrix.CreateScale(6f, 6f, 1f);
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, matrix);

        foreach (var renderHook in renderHooks)
        // if (renderHook.Visible)
            renderHook.RenderHiRes();

        Draw.SpriteBatch.End();

        Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
        GameplayRenderer.Begin();
    }

    public static HiResRenderLayer GetHiResRenderLayer(Scene scene, int depth) {
        if (scene.Tracker.GetEntities<HiResRenderLayer>()
                         .Concat(scene.Entities.ToAdd)
                         .FirstOrDefault(r => r is HiResRenderLayer && r.Depth == depth)
                         is not HiResRenderLayer renderLayer) {
            scene.Add(renderLayer = new HiResRenderLayer(depth));
            Logger.Info("SorbetHelper", $"creating new {nameof(HiResRenderLayer)} at depth {depth}.");
        }

        return renderLayer;
    }
}