using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Cil;

namespace Celeste.Mod.SorbetHelper.Components;

[Tracked]
public class LightCover(float alpha) : Component(false, true) {
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(LightCover)}";

    // storing this as a byte because otherwise im worried about using floats when grouping the alpha batches
    private readonly byte alpha = (byte)MathHelper.Clamp(alpha * 255f, 0f, 255f);

    #region Hooks

    internal static void Load() {
        IL.Celeste.LightingRenderer.BeforeRender += IL_LightingRenderer_BeforeRender;
    }

    internal static void Unload() {
        IL.Celeste.LightingRenderer.BeforeRender -= IL_LightingRenderer_BeforeRender;
    }

    private static void IL_LightingRenderer_BeforeRender(ILContext il) {
        ILCursor cursor = new ILCursor(il) {
            Index = -1
        };

        if (!cursor.TryGotoPrev(MoveType.After,
                instr => instr.MatchCallOrCallvirt(typeof(GFX), nameof(GFX.DrawIndexedVertices)))) {
            Logger.Warn(LogID, $"Failed to inject check to render LightCover components in CIL code for {cursor.Method.Name}!");
            return;
        }

        Logger.Verbose(LogID, $"Injecting check to render LightCover components at {cursor.Index} in CIL code for {cursor.Method.Name}");

        cursor.EmitLdloc0();
        cursor.EmitDelegate(DrawLightCovers);

        return;

        static void DrawLightCovers(Level level) {
            // todo: this sucksssss

            List<Component> components = level?.Tracker.GetComponents<LightCover>();

            if (components is null || components.Count == 0)
                return;

            // split the components up based on alpha (is there a better way to do this?)
            List<IGrouping<byte, LightCover>> alphaBatches =
                components.Cast<LightCover>().GroupBy(lightCover => lightCover.alpha)
                                             .ToList();
            int batchCount = alphaBatches.Count;

            RenderTargetBinding[] initalBuffer = Engine.Instance.GraphicsDevice.GetRenderTargets();
            VirtualRenderTarget[] tempBuffers = RenderTargetHelper.GetGameplayBuffers(batchCount);

            for (int i = 0; i < batchCount; i++) {
                IGrouping<byte, LightCover> batch = alphaBatches[i];
                VirtualRenderTarget tempBuffer = tempBuffers[i];
                Engine.Instance.GraphicsDevice.SetRenderTarget(tempBuffer);
                Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

                GameplayRenderer.Begin();

                // performance is in shambles but i don't see how else im supposed to do this bweh
                foreach (LightCover component in batch) {
                    Entity entity = component.Entity;

                    if (component.Visible && entity.Visible)
                        entity.Render();
                }

                GameplayRenderer.End();
            }

            // draw buffers over lighting
            Color baseColor = level.Lighting.BaseColor;

            Engine.Instance.GraphicsDevice.SetRenderTargets(initalBuffer);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, SorbetHelperGFX.FxAlphaMask);

            for (int i = 0; i < batchCount; i++) {
                VirtualRenderTarget tempBuffer = tempBuffers[i];
                float alpha = alphaBatches[i].Key / 255f;

                Draw.SpriteBatch.Draw(tempBuffer, Vector2.Zero, baseColor * alpha);
            }

            Draw.SpriteBatch.End();

            RenderTargetHelper.ReturnGameplayBuffers(tempBuffers);
        }
    }

    #endregion

}
