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
using Celeste.Mod.Registry.DecalRegistryHandlers;
using System.Xml;

namespace Celeste.Mod.SorbetHelper.Components {

    [Tracked]
    public class LightCoverComponent : Component {
        public class LightCoverDecalRegistryHandler : DecalRegistryHandler {
            public override string Name => "sorbetHelper_lightCover";

            public override void ApplyTo(Decal decal) {
                decal.Add(new LightCoverComponent());
            }

            public override void Parse(XmlAttributeCollection xml) {

            }
        }

        public LightCoverComponent() : base(false, true) { }

        internal static void Load() {
            DecalRegistry.AddPropertyHandler<LightCoverDecalRegistryHandler>();

            IL.Celeste.LightingRenderer.BeforeRender += modLightingRendererBeforeRender;
        }

        internal static void Unload() {
            IL.Celeste.LightingRenderer.BeforeRender -= modLightingRendererBeforeRender;
        }

        private static void modLightingRendererBeforeRender(ILContext il) {
            ILCursor cursor = new ILCursor(il) {
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

            foreach (Component component in toRender) {
                if (component.Visible && component.Entity.Visible) {
                    component.Entity.Render();
                }
            }

            Draw.SpriteBatch.End();
        }
    }
}
