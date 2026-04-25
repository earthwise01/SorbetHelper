namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
public class DepthAdheringDisplacementRenderer
    : DepthRenderer<DepthAdheringDisplacementRenderer, DepthAdheringDisplacementRenderHook, bool>
{
    protected override void RenderGroup(IGrouping<bool, DepthAdheringDisplacementRenderHook> renderHookGroup)
    {
        bool distortBehind = renderHookGroup.Key;

        GameplayRenderer.End();

        VirtualRenderTarget entityBuffer = RenderTargetHelper.GetTempBuffer();
        VirtualRenderTarget displacementMapBuffer = RenderTargetHelper.GetTempBuffer();

        RenderTargetBinding[] prevRenderTargets = Engine.Instance.GraphicsDevice.GetRenderTargets();
        RenderTarget2D gameplayBuffer = GameplayBuffers.Gameplay;
        if (prevRenderTargets.Length > 0)
            gameplayBuffer = prevRenderTargets[0].RenderTarget as RenderTarget2D ?? gameplayBuffer;

        Camera camera = SceneAs<Level>().Camera;

        #region Displacement Map Rendering

        Color noDisplacementColor = DisplacementEffectBlocker.NoDisplacementColor;
        Engine.Instance.GraphicsDevice.SetRenderTarget(displacementMapBuffer);
        Engine.Instance.GraphicsDevice.Clear(noDisplacementColor);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, camera.Matrix);

        foreach (DepthAdheringDisplacementRenderHook renderHook in renderHookGroup)
        {
            if (renderHook.Visible)
                renderHook.RenderDisplacement();
        }

        List<Entity> displacementBlockers = Scene.Tracker.GetEntities<DisplacementEffectBlocker>();
        foreach (Entity entity in displacementBlockers)
        {
            if (entity is DisplacementEffectBlocker { Visible: true, DepthAdhering: true, WaterOnly: false } && entity.Depth <= Depth)
                Draw.Rect(entity.X, entity.Y, entity.Width, entity.Height, noDisplacementColor);
        }

        Draw.SpriteBatch.End();

        List<Entity> waterBlockers = displacementBlockers.Where(entity => entity is DisplacementEffectBlocker { Visible: true, DepthAdhering: true, WaterOnly: true } && entity.Depth <= Depth).ToList();
        if (waterBlockers.Count > 0)
        {
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, DisplacementEffectBlocker.WaterDisplacementBlockerBlendState, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, camera.Matrix);

            foreach (Entity entity in waterBlockers)
                Draw.Rect(entity.Position, entity.Width, entity.Height, DisplacementEffectBlocker.NoWaterDisplacementMultColor);

            Draw.SpriteBatch.End();
        }

        #endregion

        #region Entity Rendering

        Engine.Instance.GraphicsDevice.SetRenderTarget(entityBuffer);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        GameplayRenderer.Begin();

        if (distortBehind)
            Draw.SpriteBatch.Draw(gameplayBuffer, camera.Position, Color.White);

        foreach (DepthAdheringDisplacementRenderHook renderHook in renderHookGroup)
            renderHook.RenderEntity();

        GameplayRenderer.End();

        #endregion

        #region Displacement Rendering

        Engine.Instance.GraphicsDevice.SetRenderTargets(prevRenderTargets);
        // clear the gameplay buffer if it is already included on the entity buffer
        if (distortBehind)
            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        // temporarily trick Distort.Render into only using the "Displace" technique as to not apply the anxiety effect
        using (new SetTemporaryValue<float>(ref Distort.anxiety, 0f))
        using (new SetTemporaryValue<float>(ref Distort.gamerate, 1f))
            Distort.Render((RenderTarget2D)entityBuffer, (RenderTarget2D)displacementMapBuffer, hasDistortion: true);

        RenderTargetHelper.ReturnTempBuffer(entityBuffer);
        RenderTargetHelper.ReturnTempBuffer(displacementMapBuffer);

        #endregion

        GameplayRenderer.Begin();
    }
}
