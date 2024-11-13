using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.SorbetHelper.Backdrops {

    // thank you to StyleMaskHelper which i referenced quite a bit while making this bc i have no clue what im doing still asdfsadf
    public class StylegroundOverHudRenderer : Renderer {
        public const string Tag = "sorbetHelper_drawAboveHud";

        public struct StylegroundOverHudControllerData {
            public bool PauseUpdate;
            public bool DisableWhenPaused;
        }
        public static bool LevelHasController(Level level) => SorbetHelperMapDataProcessor.StylegroundOverHudControllers.ContainsKey((level.Session.Area.ID, level.Session.Area.Mode));
        public static StylegroundOverHudControllerData GetControllerSettings(Level level) => SorbetHelperMapDataProcessor.StylegroundOverHudControllers[(level.Session.Area.ID, level.Session.Area.Mode)];

        private static StylegroundOverHudRenderer Instance;
        private StylegroundOverHudControllerData Settings;

        private readonly BackdropRenderer BackdropRenderer = new();
        private readonly List<Backdrop> Backdrops = [];
        private static VirtualRenderTarget Buffer;

        private readonly List<Backdrop> AdditiveBackdrops = [];
        private static readonly List<VirtualRenderTarget> AdditiveBuffers = [];
        private static bool additiveRenderingJankHookEnabled = false;

        private Matrix scaleMatrix;
        private Vector2 paddingOffset;
        private Vector2 zoomFocusOffset;
        private float scale = 1f;

        public void DrawToBuffers(Scene scene) {
            Level level = scene as Level;

            // additive blendmode backdrops
            if (AdditiveBackdrops.Count > 0) {
                BackdropRenderer.Backdrops = AdditiveBackdrops;
                if (!level.Paused || Settings.PauseUpdate)
                    BackdropRenderer.Update(level);

                BackdropRenderer.BeforeRender(level);

                additiveRenderingJankHookEnabled = true;
                BackdropRenderer.Render(level);
                additiveRenderingJankHookEnabled = false;
            }

            // normal backdrops
            if (Backdrops.Count > 0) {
                BackdropRenderer.Backdrops = Backdrops;
                if (!level.Paused || Settings.PauseUpdate)
                    BackdropRenderer.Update(level);

                BackdropRenderer.BeforeRender(level);

                Buffer ??= VirtualContent.CreateRenderTarget("sorbethelper_stylegrounds_above_hud_buffer", Util.GameplayBufferWidth, Util.GameplayBufferHeight);
                Util.CheckResizeBuffer(Buffer);

                Engine.Instance.GraphicsDevice.SetRenderTarget(Buffer);
                Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

                BackdropRenderer.Render(level);
            }
        }

        private static void DisposeBuffers() {
            Buffer?.Dispose();
            Buffer = null;

            for (int i = 0; i < AdditiveBuffers.Count; i++) {
                AdditiveBuffers[i]?.Dispose();
            }
            AdditiveBuffers.Clear();
        }

        public override void BeforeRender(Scene scene) {
            DrawToBuffers(scene);
        }

        public override void Render(Scene scene) {
            SpriteEffects spriteEffect = SpriteEffects.None;
            if (SaveData.Instance.Assists.MirrorMode)
                spriteEffect |= SpriteEffects.FlipHorizontally;

            if (ExtendedVariantsCompat.UpsideDown)
                spriteEffect |= SpriteEffects.FlipVertically;

            // regular backdrops
            if (Backdrops.Count > 0 && Buffer is not null) {
                // doesn't support colorgrades currently since those seem to cause bad weird issues :< with any sort of partial transparency
                // going to look into maybe trying to rewrite a celeste-accurate version that. doesn't break but that might take a bit so this is Good Enough for now i hope
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, scaleMatrix);
                Draw.SpriteBatch.Draw(Buffer, zoomFocusOffset + paddingOffset, Buffer.Bounds, Color.White * ExtendedVariantsCompat.ForegroundEffectOpacity, 0f, zoomFocusOffset, scale, spriteEffect, 0f);
                Draw.SpriteBatch.End();
            }

            // additive blending backdrops
            if (AdditiveBackdrops.Count > 0) {
                // funnily enough this isn't fully accurately compatible with extended variants foreground effect opacity, since that doesnt seem to do anything special to handle additive blending
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, scaleMatrix);
                for (int i = 0; i < AdditiveBackdrops.Count; i++) {
                    var buffer = AdditiveBuffers[i];
                    if (buffer is not null && AdditiveBackdrops[i].Visible)
                        Draw.SpriteBatch.Draw(buffer, zoomFocusOffset + paddingOffset, Buffer.Bounds, Color.White * ExtendedVariantsCompat.ForegroundEffectOpacity, 0f, zoomFocusOffset, scale, spriteEffect, 0f);
                }
                Draw.SpriteBatch.End();
            }
        }

        private void ConsumeStylegrounds(Level level) {
            ConsumeStylegrounds(level.Foreground.Backdrops);
            ConsumeStylegrounds(level.Background.Backdrops);
            Logger.Log(LogLevel.Verbose, "SorbetHelper", "[StylegroundOverHudRenderer] consumed stylegrounds!");
        }

        private void ConsumeStylegrounds(List<Backdrop> origBackdrops) {
            // i dont know why the fk i need to do this but for some bizarre reason it literally doesnt work if i iterate through the list forwards catplush
            for (int i = origBackdrops.Count - 1; i >= 0; i--) {
                // for (int i = 0; i < origBackdrops.Count; i++) {
                var backdrop = origBackdrops[i];

                foreach (string tag in backdrop.Tags) {
                    if (tag == Tag) {
                        if (backdrop is Parallax parallax && parallax.BlendState == BlendState.Additive) {
                            // handle additive parallax
                            parallax.BlendState = BlendState.AlphaBlend;
                            AdditiveBackdrops.Insert(0, parallax);
                            AdditiveBuffers.Insert(0, null);
                        } else {
                            // handle all other backdrops
                            Backdrops.Insert(0, backdrop);
                        }

                        backdrop.Renderer = BackdropRenderer;
                        origBackdrops.RemoveAt(i);
                    }
                }
            }
        }

        internal static void Load() {
            On.Celeste.Level.End += onLevelEnd;
            IL.Celeste.Level.Render += modLevelRender;
            IL.Celeste.BackdropRenderer.Render += modBackdropRendererRender;

            Everest.Events.Level.OnLoadLevel += onLoadLevelEvent;
        }

        internal static void Unload() {
            On.Celeste.Level.End -= onLevelEnd;
            IL.Celeste.Level.Render -= modLevelRender;
            IL.Celeste.BackdropRenderer.Render -= modBackdropRendererRender;

            Everest.Events.Level.OnLoadLevel -= onLoadLevelEvent;
        }

        private static void onLoadLevelEvent(Level level, Player.IntroTypes introType, bool isFromLoader) {
            if (LevelHasController(level) && (isFromLoader || Instance is null || !level.RendererList.Renderers.Contains(Instance))) {
                DisposeBuffers();

                // idk if this does anything or not but if the instance isn't null then make sure it isn't in the rendererlist
                if (Instance is not null)
                    level.Remove(Instance);

                Instance = new() {
                    Settings = GetControllerSettings(level)
                };
                Instance.ConsumeStylegrounds(level);

                level.Add(Instance);
            }
        }

        private static void onLevelEnd(On.Celeste.Level.orig_End orig, Level level) {
            orig(level);

            DisposeBuffers();
            Instance = null;
        }

        private static void modBackdropRendererRender(ILContext il) {
            ILCursor cursor = new(il);

            VariableDefinition loopCounterVariable = new(il.Import(typeof(int)));
            il.Body.Variables.Add(loopCounterVariable);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Backdrop>("Visible"))) {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"[StylegroundAboveHudRenderer] ilhook error! failed to find loop start in CIL code for {cursor.Method.Name}!");
            }

            Logger.Log(LogLevel.Verbose, "SorbetHelper", $"[StylegroundAboveHudRenderer] injecting janky buffer swapping for additive backdrops at {cursor.Index} in CIL code for {cursor.Method.Name}!");

            cursor.EmitDup();
            cursor.EmitLdarg0();
            cursor.EmitLdloca(loopCounterVariable);
            cursor.EmitDelegate(backdropRenderLoopAdditiveBlendBufferSwap);
        }

        private static void backdropRenderLoopAdditiveBlendBufferSwap(bool backdropVisible, BackdropRenderer self, ref int i) {
            if (!additiveRenderingJankHookEnabled)
                return;

            if (backdropVisible) {
                if (i >= AdditiveBuffers.Count) {
                    Logger.Log(LogLevel.Warn, "SorbetHelper", $"[StylegroundOverHudRenderer] error! additive blending buffer {i} is out of range!");
                } else {
                    self.EndSpritebatch();

                    AdditiveBuffers[i] ??= VirtualContent.CreateRenderTarget("sorbethelper_stylegrounds_above_hud_buffer_additive_" + i, Util.GameplayBufferWidth, Util.GameplayBufferHeight);
                    Util.CheckResizeBuffer(AdditiveBuffers[i]);

                    Engine.Graphics.GraphicsDevice.SetRenderTarget(AdditiveBuffers[i]);
                    Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
                }
            }

            i++;
        }

        private static void modLevelRender(ILContext il) {
            ILCursor cursor = new(il);

            // apparently celestenet  broke a similar cursor.goto in functionalzoomout so to be safe im removing this and just hoping nobody added another matrix createscale
            // i dont think it actually matters here since this is applied on game load and not level enter but idkk whatever
            // if (!cursor.TryGotoPrev(MoveType.After, instr => instr.MatchLdnull(), instr => instr.MatchCallOrCallvirt<GraphicsDevice>("SetRenderTarget"))) {
            //     Logger.Log(LogLevel.Warn, "SorbetHelper", $"[StylegroundAboveHudRenderer] ilhook error! failed to find where hd rendering starts in CIL code for {cursor.Method.Name}!");
            // }

            // grab upscaling locals
            int matrixLocal = -1;
            int paddingLocal = -1;
            int zoomFocusLocal = -1;
            int scaleLocal = -1;

            // matrix
            if (cursor.TryGotoNext(instr => instr.MatchCall<Matrix>("CreateScale"))) {
                cursor.TryGotoNext(instr => instr.MatchStloc(out matrixLocal));
            }

            if (LogMissingLocalError(matrixLocal, "matrix", cursor.Method.Name))
                return;

            // zoom focus offset
            if (cursor.TryGotoNext(instr => instr.MatchLdfld<Level>("ZoomFocusPoint"))) {
                cursor.TryGotoNext(instr => instr.MatchStloc(out zoomFocusLocal));
            }

            if (LogMissingLocalError(zoomFocusLocal, "zoomFocusOffset", cursor.Method.Name))
                return;

            // scale
            if (cursor.TryGotoNext(instr => instr.MatchLdfld<Level>("Zoom"))) {
                cursor.TryGotoNext(instr => instr.MatchStloc(out scaleLocal));
            }

            if (LogMissingLocalError(scaleLocal, "scale", cursor.Method.Name))
                return;

            // padding offset
            cursor.TryGotoNext(instr => instr.MatchLdloca(out paddingLocal),
                instr => instr.MatchLdarg(out _),
                instr => instr.MatchLdfld<Level>("ScreenPadding"));

            if (LogMissingLocalError(paddingLocal, "paddingOffset", cursor.Method.Name))
                return;

            // actually do the thing
            cursor.Index = -1;

            if (!cursor.TryGotoPrev(MoveType.AfterLabel, instr => instr.MatchLdarg0(), instr => instr.MatchLdfld<Level>("SubHudRenderer"))) {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"[StylegroundAboveHudRenderer] ilhook error! failed to inject below hud rendering in CIL code for {cursor.Method.Name}!");
                return;
            }

            Logger.Log(LogLevel.Verbose, "SorbetHelper", $"[StylegroundAboveHudRenderer] injecting below hud rendering at {cursor.Index} in CIL code for {cursor.Method.Name}!");

            // update upscale data
            cursor.EmitLdloc(matrixLocal);
            cursor.EmitLdloc(paddingLocal);
            cursor.EmitLdloc(zoomFocusLocal);
            cursor.EmitLdloc(scaleLocal);
            cursor.EmitDelegate(updateUpscaleData);

            // render stylegrounds (when paused and with disable when paused enabled)
            cursor.EmitLdarg0();
            cursor.EmitDelegate(renderBehind);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Level>("HudRenderer"), instr => instr.MatchLdarg0(), instr => instr.MatchCallOrCallvirt<Renderer>("Render"))) {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"[StylegroundAboveHudRenderer] ilhook error! failed to inject above hud rendering in CIL code for {cursor.Method.Name}!");
                return;
            }

            Logger.Log(LogLevel.Verbose, "SorbetHelper", $"[StylegroundAboveHudRenderer] injecting above hud rendering at {cursor.Index} in CIL code for {cursor.Method.Name}!");

            // render stylegrounds
            cursor.EmitLdarg0();
            cursor.EmitDelegate(renderAbove);
        }

        private static void renderBehind(Level self) {
            if (Instance is null || !Instance.Settings.DisableWhenPaused || !self.Paused)
                return;

            renderStylegrounds(self);
        }

        private static void renderAbove(Level self) {
            if (Instance is not null && (!Instance.Settings.DisableWhenPaused || !self.Paused))
                renderStylegrounds(self);
        }

        private static void renderStylegrounds(Level self) {
            Instance?.Render(self);
        }

        private static void updateUpscaleData(Matrix scaleMatrix, Vector2 paddingOffset, Vector2 zoomFocusOffset, float scale) {
            if (Instance is null)
                return;

            Instance.scaleMatrix = scaleMatrix;
            Instance.paddingOffset = paddingOffset;
            Instance.zoomFocusOffset = zoomFocusOffset;
            Instance.scale = scale;
        }

        private static bool LogMissingLocalError(int localIndex, string name, string methodName) {
            if (localIndex == -1) {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"[StylegroundAboveHudRenderer] ilhook error! failed to find local `{name}` in CIL code for {methodName}!");
                return true;
            }

            return false;
        }

        // entity that deletes itself upon loading
        // this only exists to make the [LoadLevel] Failed loading entity SorbetHelper/StylegroundOverHudController thing shut up because the controller is only used to get settings for and check whether to enable the renderer
        // probably a better way to do this maybe but as is the case with literally everything else in this mod it Works(tm) which means it is Good Enough(tm)
        [CustomEntity("SorbetHelper/StylegroundOverHudController")]
        private class DummyEntity : Entity {
            public override void Added(Scene scene) {
                RemoveSelf();
            }
        }
    }
}
