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
        public static bool LevelHasController(Level level) => SorbetHelperMapDataProcessor.LevelContainsStylegroundOverHudController(level.Session.Area.ID, level.Session.Area.Mode);
        private readonly BackdropRenderer backdropRenderer = new();
        private readonly List<Backdrop> Backdrops = [];
        private static VirtualRenderTarget Buffer;

        private static StylegroundOverHudRenderer Instance;

        private struct LevelUpscaleData {
            public Matrix scaleMatrix;
            public Vector2 paddingOffset;
            public Vector2 zoomFocusOffset;
            public float scale = 1f;

            public LevelUpscaleData() { }
        }

        private static LevelUpscaleData upscaleData = new();

        public void DrawToBuffer(Scene scene) {
            Level level = scene as Level;
            backdropRenderer.Backdrops = Backdrops;
            if (!level.Paused)
                backdropRenderer.Update(level);

            backdropRenderer.BeforeRender(level);

            Buffer ??= VirtualContent.CreateRenderTarget("sorbethelper_stylegrounds_above_hud_buffer", GameplayBuffers.Level.Width, GameplayBuffers.Level.Height);

            GraphicsDevice gd = Engine.Instance.GraphicsDevice;
            gd.SetRenderTarget(Buffer);
            gd.Clear(Color.Transparent);

            backdropRenderer.Render(level);
        }

        private static void DisposeBuffer() {
            Buffer?.Dispose();
            Buffer = null;
        }

        public override void BeforeRender(Scene scene) {
            DrawToBuffer(scene);
        }

        public override void Render(Scene scene) {
            if (Backdrops.Count <= 0 || Buffer is null)
                return;

            SpriteEffects spriteEffect = SpriteEffects.None;
            if (SaveData.Instance.Assists.MirrorMode)
                spriteEffect |= SpriteEffects.FlipHorizontally;

            if (ExtendedVariantsCompat.UpsideDown)
                spriteEffect |= SpriteEffects.FlipVertically;

            // doesn't support colorgrades currently since those seem to cause bad weird issues :< with any sort of partial transparency
            // going to look into maybe trying to rewrite a celeste-accurate version that. doesn't break but that might take a bit so this is Good Enough for now i hope
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, upscaleData.scaleMatrix);
            Draw.SpriteBatch.Draw(Buffer, upscaleData.zoomFocusOffset + upscaleData.paddingOffset, Buffer.Bounds, Color.White * ExtendedVariantsCompat.ForegroundEffectOpacity, 0f, upscaleData.zoomFocusOffset, upscaleData.scale, spriteEffect, 0f);
            Draw.SpriteBatch.End();
        }

        private void ConsumeStylegrounds(Level level) {
            ConsumeStylegrounds(level.Foreground.Backdrops);
            ConsumeStylegrounds(level.Background.Backdrops);
            Logger.Log(LogLevel.Info, "SorbetHelper", "[StylegroundOverHudRenderer] consumed stylegrounds!");
        }

        private void ConsumeStylegrounds(List<Backdrop> origBackdrops) {
            // i dont know why the fk i need to do this but for some bizarre reason it literally doesnt work if i iterate through the list forwards catplush
            for (int i = origBackdrops.Count - 1; i >= 0; i--) {
                // for (int i = 0; i < origBackdrops.Count; i++) {
                var backdrop = origBackdrops[i];

                foreach (string tag in backdrop.Tags) {
                    if (tag == Tag) {
                        Backdrops.Insert(0, backdrop);
                        backdrop.Renderer = backdropRenderer;
                        origBackdrops.RemoveAt(i);
                    }
                }
            }
        }

        internal static void Load() {
            On.Celeste.Level.End += onLevelEnd;
            IL.Celeste.Level.Render += modLevelRender;

            Everest.Events.Level.OnLoadLevel += onLoadLevelEvent;
        }

        internal static void Unload() {
            On.Celeste.Level.End -= onLevelEnd;
            IL.Celeste.Level.Render -= modLevelRender;

            Everest.Events.Level.OnLoadLevel -= onLoadLevelEvent;
        }

        private static void onLoadLevelEvent(Level level, Player.IntroTypes introType, bool isFromLoader) {
            if (LevelHasController(level) && (isFromLoader || Instance is null || !level.RendererList.Renderers.Contains(Instance))) {
                DisposeBuffer();

                // idk if this does anything or not but if the instance isn't null then make sure it isn't in the rendererlist
                if (Instance is not null)
                    level.Remove(Instance);

                Instance = new();
                Instance.ConsumeStylegrounds(level);

                level.Add(Instance);
            }
        }

        private static void onLevelEnd(On.Celeste.Level.orig_End orig, Level level) {
            orig(level);

            // if (Engine.NextScene is LevelExit || Engine.NextScene is OverworldLoader || Engine.NextScene is LevelLoader) {
            DisposeBuffer();
            Instance = null;
            // }
        }

        private static void modLevelRender(ILContext il) {
            ILCursor cursor = new(il);
            cursor.Index = -1;

            // grab upscaling locals
            if (!cursor.TryGotoPrev(MoveType.After, instr => instr.MatchLdnull(), instr => instr.MatchCallOrCallvirt<GraphicsDevice>("SetRenderTarget"))) {
                Logger.Log(LogLevel.Warn, "SorbetHelper", "[StylegroundAboveHudRenderer] ilhook error! failed to find where hd rendering starts!");
            }

            int matrixLocal = -1;
            int paddingLocal = -1;
            int zoomFocusLocal = -1;
            int scaleLocal = -1;

            // matrix
            cursor.TryGotoNext(instr => instr.MatchCall<Matrix>("CreateScale"),
                instr => instr.MatchLdsfld<Engine>("ScreenMatrix"),
                instr => true,
                instr => instr.MatchStloc(out matrixLocal));

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
            if (cursor.TryGotoPrev(MoveType.After, instr => instr.MatchLdfld<Level>("HudRenderer"), instr => instr.MatchLdarg0(), instr => instr.MatchCallOrCallvirt<Renderer>("Render"))) {
                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"[StylegroundAboveHudRenderer] injecting above hud rendering at {cursor.Index} in CIL code for {cursor.Method.Name}!");
                // update upscale data (honestly i probably could just put the locals directly into render but doing it this way helps(?) if i want to be able to maybe force the styleground to go behind the hud instead or smth idk)
                cursor.EmitLdloc(matrixLocal);
                cursor.EmitLdloc(paddingLocal);
                cursor.EmitLdloc(zoomFocusLocal);
                cursor.EmitLdloc(scaleLocal);
                cursor.EmitDelegate(updateUpscaleData);

                // render stylegrounds
                cursor.EmitLdarg0();
                cursor.EmitDelegate(renderInLevelRender);
            } else {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"[StylegroundAboveHudRenderer] ilhook error! failed to inject above hud rendering in CIL code for {cursor.Method.Name}!");
            }

        }

        private static void renderInLevelRender(Level self) {
            Instance?.Render(self);

            // i dont know why im putting this here (might remove idk) but better to be safe ig
            if (LevelHasController(self) && Instance is null && self.OnRawInterval(0.1f))
                Logger.Log(LogLevel.Error, "SorbetHelper", "[StylegroundAboveHudRenderer] error! the styleground above hud renderer instance is (somehow) null. if you see this message at all please report this thx!");
        }

        private static void updateUpscaleData(Matrix scaleMatrix, Vector2 paddingOffset, Vector2 zoomFocusOffset, float scale) {
            upscaleData.scaleMatrix = scaleMatrix;
            upscaleData.paddingOffset = paddingOffset;
            upscaleData.zoomFocusOffset = zoomFocusOffset;
            upscaleData.scale = scale;
        }

        private static bool LogMissingLocalError(int localIndex, string name, string methodName) {
            if (localIndex == -1) {
                Logger.Log(LogLevel.Warn, "SorbetHelper", $"[StylegroundAboveHudRenderer] ilhook error! failed to find local `{name}` in CIL code for {methodName}!");
                return true;
            }

            return false;
        }
    }
}
