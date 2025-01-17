using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste;
using Celeste.Mod.Registry.DecalRegistryHandlers;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Linq;

namespace Celeste.Mod.SorbetHelper.Components;

[Tracked]
public class LightCover : Component {
    // private readonly int minDepth, maxDepth;
    private readonly byte alpha;

    public LightCover(float alpha) : base(false, true) {
        // this.minDepth = minDepth;
        // this.maxDepth = maxDepth;
        this.alpha = (byte)MathHelper.Clamp(alpha * 255f, 0f, 255f); // storing this as a byte because otherwise im worried about using floats when grouping the alpha batches
    }

    internal static void Load() {
        IL.Celeste.LightingRenderer.BeforeRender += modLightingRendererBeforeRender;
    }

    internal static void Unload() {
        IL.Celeste.LightingRenderer.BeforeRender -= modLightingRendererBeforeRender;
    }

    private static void modLightingRendererBeforeRender(ILContext il) {
        var cursor = new ILCursor(il) {
            Index = -1
        };

        if (cursor.TryGotoPrev(MoveType.After, instr => instr.MatchCallOrCallvirt(typeof(GFX), nameof(GFX.DrawIndexedVertices)))) {
            Logger.Log(LogLevel.Verbose, "SorbetHelper", $"Injecting check to render LightCoverComponents at {cursor.Index} in CIL code for {cursor.Method.Name}");

            cursor.EmitLdloc0();
            cursor.EmitDelegate(drawLightCover);
        } else {
            Logger.Log(LogLevel.Warn, "SorbetHelper", $"Failed to inject check to render LightCoverComponents in CIL code for {cursor.Method.Name}!");
        }
    }

    private static void drawLightCover(Level level) {
        var components = level?.Tracker.GetComponents<LightCover>();

        if (components is null || components.Count == 0)
            return;

        var toRender = components.Cast<LightCover>();
        var alphaBatches = toRender.GroupBy(lightCover => lightCover.alpha).ToList(); // splits the components up based on alpha (is there a better way to do this?)
        var batchCount = alphaBatches.Count;

        // render to intermediate targets
        var gd = Engine.Instance.GraphicsDevice;
        var initalBuffer = gd.GetRenderTargets();

        var tempBuffers = RenderTargetHelper.GetGameplayBuffers(batchCount);

        for (int i = 0; i < batchCount; i++) {
            var batch = alphaBatches[i];
            var tempBuffer = tempBuffers[i];
            gd.SetRenderTarget(tempBuffer);
            gd.Clear(Color.Transparent);

            GameplayRenderer.Begin();

            // performance is in shambles but i dont see how else im supposed to do this bweh
            // a potential optimization could maybe be to group the entities into depth-based batches which also replace their normal render calls, so that they only get rendered once per frame, but that sounds trickier to implement and could come with its own issues
            foreach (var component in batch) {
                var entity = component.Entity;

                if (component.Visible && entity.Visible/*  && entity.Depth >= component.minDepth && entity.Depth <= component.maxDepth */)
                    entity.Render();
            }

            GameplayRenderer.End();
        }

        // draw buffers over lighting
        var baseColor = level.Lighting.BaseColor;

        gd.SetRenderTargets(initalBuffer);
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, SorbetHelperModule.AlphaMaskShader);

        for (int i = 0; i < batchCount; i++) {
            var tempBuffer = tempBuffers[i];
            var alpha = alphaBatches[i].Key / 255f;

            Draw.SpriteBatch.Draw(tempBuffer, Vector2.Zero, baseColor * alpha);
        }

        Draw.SpriteBatch.End();

        RenderTargetHelper.ReturnGameplayBuffers(tempBuffers);
    }
}
