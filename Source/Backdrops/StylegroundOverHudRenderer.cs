using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
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

        public void DrawToBuffer(Scene scene) {
            Level level = scene as Level;
            backdropRenderer.Backdrops = Backdrops;
            if (!level.Paused)
                backdropRenderer.Update(level);

            backdropRenderer.BeforeRender(level);

            Buffer ??= VirtualContent.CreateRenderTarget("sorbethelper_stylegrounds_above_hud_buffer", GameplayBuffers.Level.Width, GameplayBuffers.Level.Height);
            //GraphicsDevice device = Engine.Instance.GraphicsDevice;

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

            if (Backdrops.Count <= 0 || Buffer is null)
                return;

            // copied from vanilla Level.Render when drawing the level to the screen
            // really should try and get the actual values used there in case theyre changed but i havent figured out how yet so this shd do for now
            Matrix matrix = Matrix.CreateScale(6f) * Engine.ScreenMatrix;
            Vector2 vector = new Vector2(320f, 180f);
            Vector2 vector2 = vector / level.ZoomTarget;
            Vector2 vector3 = (level.ZoomTarget != 1f) ? ((level.ZoomFocusPoint - vector2 / 2f) / (vector - vector2) * vector) : Vector2.Zero;
            float scale = level.Zoom * ((320f - level.ScreenPadding * 2f) / 320f);
            Vector2 vector4 = new Vector2(level.ScreenPadding, level.ScreenPadding * 0.5625f);

            if (SaveData.Instance.Assists.MirrorMode) {
                vector4.X = 0f - vector4.X;
                vector3.X = 160f - (vector3.X - 160f);
            }

            // doesn't support colorgrades currently since those seem to cause bad weird issues :< with any sort of partial transparency
            // going to look into maybe trying to rewrite a celeste-accurate version that. doesn't break but that might take a bit so this is Good Enough for now i hope
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, matrix);
            Draw.SpriteBatch.Draw(Buffer, vector3 + vector4, Buffer.Bounds, Color.White, 0f, vector3, scale, SaveData.Instance.Assists.MirrorMode ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
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

            // get styleground render inject location
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdfld<Level>("HudRenderer"), instr => instr.MatchLdarg0(), instr => instr.MatchCallOrCallvirt<Renderer>("Render"));
            int emitToIndex = cursor.Index;

            // unfinished
            // grab upscaling locals
            // cursor.GotoPrev(MoveType.After, instr => instr.MatchLdnull(), instr => instr.MatchCallOrCallvirt<GraphicsDevice>("SetRenderTarget"));

            // render stylegrounds
            cursor.Index = emitToIndex;
            cursor.EmitLdarg0();
            cursor.EmitDelegate(renderInLevelRender);
        }

        private static void renderInLevelRender(Level self) {
            Instance?.Render(self);

            // i dont know why im putting this here (might remove idk) but better to be safe ig
            if (LevelHasController(self) && Instance is null && self.OnRawInterval(0.1f))
                Logger.Log(LogLevel.Error, "SorbetHelper", "[StylegroundAboveHudRenderer] error! the styleground above hud renderer instance is (somehow) null. if you see this message at all please report this thx!");
        }
    }
}
