using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.SorbetHelper.Backdrops {

    // thank you to StyleMaskHelper which i referenced quite a bit while making this bc i have no clue what im doing still
    public class FGStylegroundOverHUDRenderer : Renderer {
        public const string Tag = "sorbetHelper_drawAboveHud";
        private static readonly BackdropRenderer backdropRenderer = new();
        public List<Backdrop> Backdrops = [];
        private static VirtualRenderTarget Buffer;
        private static FGStylegroundOverHUDRenderer Instance;

        // fixes some extremely broken rendering with transparent stylegrounds but might produce other weird results maybe possibly
        // or break if someone messes with switching render targets midrender and doesnt set it back correctly (displacement wrapper flashbacks i still need to make that better there huh right asdfahks)
        // if i dont find anything completely awful i might make this the behavior by default but for now im leaving it as a toggle just in case
        private static bool fixWeirdAlphaRenderingIssuesButMaybeCauseOtherIssuesIdk = true;

        private void ConsumeStylegrounds(Level level) {
            fixWeirdAlphaRenderingIssuesButMaybeCauseOtherIssuesIdk = true;
            List<Backdrop> origBackdrops = level.Foreground.Backdrops;

            // i dont know why the fk i need to do this but for some bizarre reason it literally doesnt work if i iterate through the list forwards catplush
            for (int i = origBackdrops.Count - 1; i >= 0; i--) {
                //for (int i = 0; i < origBackdrops.Count; i++) {
                var backdrop = origBackdrops[i];

                foreach (string tag in backdrop.Tags) {
                    if (tag == Tag) {
                        Backdrops.Insert(0, backdrop);
                        backdrop.Renderer = backdropRenderer;
                        origBackdrops.RemoveAt(i);
                    } else if (tag == "sorbetHelper_useOldDrawAboveHudBehavior") {
                        fixWeirdAlphaRenderingIssuesButMaybeCauseOtherIssuesIdk = false;
                    }
                }
            }
        }

        public void DrawToBuffer(Scene scene) {
            Level level = scene as Level;
            backdropRenderer.Backdrops = Backdrops;
            if (!level.Paused)
                backdropRenderer.Update(level);

            backdropRenderer.BeforeRender(level);

            if (fixWeirdAlphaRenderingIssuesButMaybeCauseOtherIssuesIdk)
                return;

            // old behavior

            Buffer ??= VirtualContent.CreateRenderTarget("fgstylegrounds_above_hud_buffer", 320, 180);

            Engine.Instance.GraphicsDevice.SetRenderTarget(Buffer);
            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

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
            Level level = scene as Level;

            if (Backdrops.Count <= 0)
                return;

            // copied from vanilla Level.Render when drawing the level to the screen
            // really should try and get the actual values used there in case theyre changed but i havent figured out how yet so this shd do for now
            Matrix matrix = Matrix.CreateScale(6f) * Engine.ScreenMatrix;
            Vector2 vector = new Vector2(320f, 180f);
            Vector2 vector2 = vector / level.ZoomTarget;
            Vector2 vector3 = (level.ZoomTarget != 1f) ? ((level.ZoomFocusPoint - vector2 / 2f) / (vector - vector2) * vector) : Vector2.Zero;
            float scale = level.Zoom * ((320f - level.ScreenPadding * 2f) / 320f);
            Vector2 vector4 = new Vector2(level.ScreenPadding, level.ScreenPadding * 0.5625f);

            if (fixWeirdAlphaRenderingIssuesButMaybeCauseOtherIssuesIdk) {
                matrix = Matrix.CreateTranslation(new Vector3(vector4, 0f)) * Matrix.CreateTranslation(new Vector3(-vector3, 0f)) * Matrix.CreateScale(scale) * Matrix.CreateTranslation(new Vector3(vector3, 0f)) * matrix;
                backdropRenderer.Matrix = matrix;

                backdropRenderer.Render(level);

                return;
            }

            // old behavior

            if (Buffer is null)
                return;

            if (SaveData.Instance.Assists.MirrorMode) {
                vector4.X = 0f - vector4.X;
                vector3.X = 160f - (vector3.X - 160f);
            }

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, ColorGrade.Effect, matrix);
            Draw.SpriteBatch.Draw(Buffer, vector3 + vector4, Buffer.Bounds, Color.White, 0f, vector3, scale, SaveData.Instance.Assists.MirrorMode ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            Draw.SpriteBatch.End();
        }

        internal static void Load() {
            On.Celeste.Level.LoadLevel += onLevelLoadLevel;
            On.Celeste.Level.End += onLevelEnd;
            IL.Celeste.Level.Render += modLevelRender;
        }

        internal static void Unload() {
            On.Celeste.Level.LoadLevel -= onLevelLoadLevel;
            On.Celeste.Level.End -= onLevelEnd;
            IL.Celeste.Level.Render -= modLevelRender;
        }

        private static void onLevelLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes introType, bool isFromLoader) {
            orig(self, introType, isFromLoader);

            if (isFromLoader || Instance is null || !self.RendererList.Renderers.Contains(Instance)) {
                DisposeBuffer();

                Instance = new();
                Instance.ConsumeStylegrounds(self);

                self.Add(Instance);
            }
        }

        private static void onLevelEnd(On.Celeste.Level.orig_End orig, Level self) {
            orig(self);

            // if (Engine.NextScene is LevelExit || Engine.NextScene is OverworldLoader || Engine.NextScene is LevelLoader) {
            DisposeBuffer();
            Instance = null;
            // }
        }

        private static void modLevelRender(ILContext il) {
            ILCursor cursor = new(il);
            cursor.Index = -1;

            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdfld<Level>("HudRenderer"), instr => instr.MatchLdarg0(), instr => instr.MatchCallOrCallvirt<Renderer>("Render"));
            //cursor.EmitLdloc(renderForegroundAboveHudVariable);
            cursor.EmitLdarg0();
            cursor.EmitDelegate(renderInLevelRender);
        }

        private static void renderInLevelRender(Level level) {
            Instance?.Render(level);
        }
    }
}
