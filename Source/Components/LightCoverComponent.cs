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


namespace Celeste.Mod.SorbetHelper.Components {

    [Tracked]
    public class LightCoverComponent : Component {
        public class LightCoverDecalRegistryHandler : DecalRegistryHandler {
            public override string Name => "sorbetHelper_lightCover";

            private int minDepth, maxDepth;

            public override void ApplyTo(Decal decal) {
                decal.Add(new LightCoverComponent(minDepth, maxDepth));
            }

            public override void Parse(XmlAttributeCollection xml) {
                // defaults to make only decals above the player cover light
                minDepth = Get(xml, "minimumDepth", int.MinValue);
                maxDepth = Get(xml, "maximumDepth", -1);
            }
        }

        private readonly int minDepth, maxDepth;

        public LightCoverComponent(int minDepth, int maxDepth) : base(false, true) {
            this.minDepth = minDepth;
            this.maxDepth = maxDepth;
        }

        internal static void Load() {
            DecalRegistry.AddPropertyHandler<LightCoverDecalRegistryHandler>();

            IL.Celeste.LightingRenderer.BeforeRender += modLightingRendererBeforeRender;
        }

        internal static void Unload() {
            IL.Celeste.LightingRenderer.BeforeRender -= modLightingRendererBeforeRender;
        }

        private static void modLightingRendererBeforeRender(ILContext il) {
            ILCursor cursor = new(il) {
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
            List<Component> toRender = level?.Tracker.GetComponents<LightCoverComponent>();

            if (toRender is null || toRender.Count == 0)
                return;

            SorbetHelperModule.AlphaMaskShader.Parameters["mask_color"].SetValue(level.Lighting.BaseColor.ToVector4());
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, SorbetHelperModule.AlphaMaskShader, level.Camera.Matrix);

            // this probably absolutely Sucks for performance but im too tired to think about optimisation right now :<
            foreach (var component in toRender) {
                var entity = component.Entity;
                var lightCover = component as LightCoverComponent;

                if (lightCover.Visible && entity.Visible && entity.Depth >= lightCover.minDepth && entity.Depth <= lightCover.maxDepth) {
                    entity.Render();
                }
            }

            Draw.SpriteBatch.End();
        }
    }
}
