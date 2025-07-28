using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.SorbetHelper.HiRes;

public static class HiResRenderer {

    public static SorbetHelperMetadata.HiResRenderingData Settings { get; private set; }

    private static Effect orig_FxDistort;

    public static bool Enabled { get; private set; }
    private static void Enable() {
        if (Enabled)
            return;

        Logger.Warn("SorbetHelper", "enablin ghi-res rendering...");

        On.Celeste.GameplayBuffers.Create += On_GameplayBuffers_Create;
        On.Celeste.GameplayBuffers.Unload += On_GameplayBuffers_Unload;

        On.Celeste.Level.Render += On_Level_Render;
        IL.Celeste.Level.Render += IL_Level_Render;

        IL.Celeste.LightingRenderer.BeforeRender += IL_LightingRenderer_BeforeRender;
        IL.Celeste.LightingRenderer.Render += IL_LightingRenderer_Render;

        IL.Celeste.BloomRenderer.Apply += IL_BloomRenderer_Apply;

        orig_FxDistort = GFX.FxDistort;
        //GFX.FxDistort = SorbetHelperModule.FxHiResDistort;

        Enabled = true;
    }

    private static void Disable() {
        if (!Enabled)
            return;

        Logger.Warn("SorbetHelper", "disabling hi-res rendering...");

        if (orig_FxDistort is not null)
            GFX.FxDistort = orig_FxDistort;

        On.Celeste.GameplayBuffers.Create -= On_GameplayBuffers_Create;
        On.Celeste.GameplayBuffers.Unload -= On_GameplayBuffers_Unload;

        On.Celeste.Level.Render -= On_Level_Render;
        IL.Celeste.Level.Render -= IL_Level_Render;

        IL.Celeste.LightingRenderer.BeforeRender -= IL_LightingRenderer_BeforeRender;
        IL.Celeste.LightingRenderer.Render -= IL_LightingRenderer_Render;

        IL.Celeste.BloomRenderer.Apply -= IL_BloomRenderer_Apply;

        Enabled = false;
    }

    internal static VirtualRenderTarget GameplayBufferHiRes;
    internal static VirtualRenderTarget LevelBufferHiRes;
    internal static VirtualRenderTarget LightBufferHiRes;
    internal static VirtualRenderTarget TempABufferHiRes, TempBBufferHiRes;

    private static void CreateBuffers() {
        GameplayBufferHiRes = VirtualContent.CreateRenderTarget("sorbetHelper_gameplayHiRes", 1920, 1080);
        LevelBufferHiRes = VirtualContent.CreateRenderTarget("sorbetHelper_levelHiRes", 1920, 1080);
        LightBufferHiRes = VirtualContent.CreateRenderTarget("sorbetHelper_lightHiRes", 1920, 1080);
        TempABufferHiRes = VirtualContent.CreateRenderTarget("sorbetHelper_tempAHiRes", 1920, 1080);
        TempBBufferHiRes = VirtualContent.CreateRenderTarget("sorbetHelper_tempBHiRes", 1920, 1080);
    }

    private static void DisposeBuffers() {
        GameplayBufferHiRes?.Dispose();
        GameplayBufferHiRes = null;
        LevelBufferHiRes?.Dispose();
        LevelBufferHiRes = null;
        LightBufferHiRes?.Dispose();
        LightBufferHiRes = null;
        TempABufferHiRes?.Dispose();
        TempABufferHiRes = null;
        TempBBufferHiRes?.Dispose();
        TempBBufferHiRes = null;
    }

    private static void FlushToHiResBuffer(VirtualRenderTarget lowRes, VirtualRenderTarget hiRes, bool clearHiRes = false) {
        var gd = Engine.Graphics.GraphicsDevice;
        var sb = Draw.SpriteBatch;

        gd.SetRenderTarget(hiRes);
        if (clearHiRes)
            gd.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        sb.Draw(lowRes, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 6f, SpriteEffects.None, 0f);
        sb.End();
    }

    internal static void FlushGameplayBuffer() => FlushToHiResBuffer(GameplayBuffers.Gameplay, GameplayBufferHiRes);

    #region Hooks

    private static void On_GameplayBuffers_Create(On.Celeste.GameplayBuffers.orig_Create orig) {
        orig();
        CreateBuffers();
    }

    private static void On_GameplayBuffers_Unload(On.Celeste.GameplayBuffers.orig_Unload orig) {
        orig();
        DisposeBuffers();
    }

    private static void On_Level_Render(On.Celeste.Level.orig_Render orig, Level self) {
        // no orig call! but this is only for prototyping ill try n turn this into an il hook later,
        _ = nameof(Level.Render);

        var gd = Engine.Instance.GraphicsDevice;

        // clear hi-res gameplay buffer (new)
        gd.SetRenderTarget(GameplayBufferHiRes);
        gd.Clear(Color.Transparent);

        gd.SetRenderTarget(GameplayBuffers.Gameplay);
        gd.Clear(Color.Transparent);

        self.GameplayRenderer.Render(self);
        // make sure to flush low-res gameplay buffer to hi-res gameplay buffer
        FlushToHiResBuffer(GameplayBuffers.Gameplay, GameplayBufferHiRes);

        // new
        // var light = GameplayBuffers.Light;
        var temp = TempABufferHiRes;
        // FlushToHiResBuffer(light, temp, clearHiRes: true);
        // GameplayBuffers.Light = temp;
        // gd.SetRenderTarget(GameplayBufferHiRes);

        self.Lighting.Render(self); // il hooked !!

        // GameplayBuffers.Light = light; // new

        gd.SetRenderTarget(GameplayBuffers.Level);
        gd.Clear(self.BackgroundColor);

        self.Background.Render(self);
        FlushToHiResBuffer(GameplayBuffers.Level, LevelBufferHiRes, clearHiRes: true);

        // new
        FlushToHiResBuffer(GameplayBuffers.Displacement, temp, clearHiRes: true);
        gd.SetRenderTarget(LevelBufferHiRes);

        Distort.Render((RenderTarget2D)GameplayBufferHiRes, (RenderTarget2D)temp, self.Displacement.HasDisplacement(self));

        //self.Bloom.Apply(GameplayBuffers.Level, self);

        // render foreground to low res buffer first
        gd.SetRenderTarget(GameplayBuffers.Level);
        gd.Clear(Color.Transparent);

        BlendState.Additive.AlphaSourceBlend = Blend.Zero;
        self.Foreground.Render(self);
        BlendState.Additive.AlphaSourceBlend = Blend.SourceAlpha;

        // Glitch.Apply(GameplayBuffers.Level, self.glitchTimer * 2f, self.glitchSeed, MathF.PI * 2f);

        if (Engine.DashAssistFreeze) {
            PlayerDashAssist entity = self.Tracker.GetEntity<PlayerDashAssist>();
            if (entity != null) {
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, self.Camera.Matrix);
                entity.Render();
                Draw.SpriteBatch.End();
            }
        }

        if (self.flash > 0f) {
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null);
            Draw.Rect(-1f, -1f, 322f, 182f, self.flashColor * self.flash);
            Draw.SpriteBatch.End();
            if (self.flashDrawPlayer) {
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, self.Camera.Matrix);
                Player entity2 = self.Tracker.GetEntity<Player>();
                if (entity2 != null && entity2.Visible) {
                    entity2.Render();
                }

                Draw.SpriteBatch.End();
            }
        }

        FlushToHiResBuffer(GameplayBuffers.Level, LevelBufferHiRes);

        gd.SetRenderTarget(null);
        gd.Clear(Color.Black);
        gd.Viewport = Engine.Viewport;

        Matrix matrix = Matrix.CreateScale(6f) * Engine.ScreenMatrix; // changed, now 1f instead of 6f (so basically removed)

        var screenSize = new Vector2(320f, 180f); // added * 6f
        var focusPoint = self.ZoomFocusPoint; // added * 6f
        var zoomedScreenSize = screenSize / self.ZoomTarget;
        var zoomOffset = ((self.ZoomTarget != 1f) ? ((focusPoint - zoomedScreenSize / 2f) / (screenSize - zoomedScreenSize) * screenSize) : Vector2.Zero);

        MTexture lastColorGrade = GFX.ColorGrades.GetOrDefault(self.lastColorGrade, GFX.ColorGrades["none"]);
        MTexture colorGrade = GFX.ColorGrades.GetOrDefault(self.Session.ColorGrade, GFX.ColorGrades["none"]);
        if (self.colorGradeEase > 0f && lastColorGrade != colorGrade) {
            ColorGrade.Set(lastColorGrade, colorGrade, self.colorGradeEase);
        } else {
            ColorGrade.Set(colorGrade);
        }

        float scale = self.Zoom * ((320f - self.ScreenPadding * 2f) / 320f);
        var paddingOffset = new Vector2(self.ScreenPadding, self.ScreenPadding * 0.5625f);
        if (SaveData.Instance.Assists.MirrorMode) {
            paddingOffset.X = 0f - paddingOffset.X;
            zoomOffset.X = 160f - (zoomOffset.X - 160f);
        }

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, ColorGrade.Effect, matrix);
        Draw.SpriteBatch.Draw((RenderTarget2D)LevelBufferHiRes, zoomOffset + paddingOffset, LevelBufferHiRes.Bounds, Color.White, 0f, zoomOffset, scale / 6f, SaveData.Instance.Assists.MirrorMode ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
        Draw.SpriteBatch.End();

        if (self.Pathfinder != null && self.Pathfinder.DebugRenderEnabled) {
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, self.Camera.Matrix * matrix);
            self.Pathfinder.Render();
            Draw.SpriteBatch.End();
        }

        self.SubHudRenderer.Render(self);
        if (((!self.Paused || !self.PauseMainMenuOpen) && !(self.wasPausedTimer < 1f)) || !Input.MenuJournal.Check || !self.AllowHudHide) {
            self.HudRenderer.Render(self);
        }

        if (self.Wipe != null) {
            self.Wipe.Render(self);
        }

        if (self.HiresSnow != null) {
            self.HiresSnow.Render(self);
        }
    }

    private static void IL_Level_Render(ILContext il) {
        _ = nameof(Level.Render);
        var cursor = new ILCursor(il);
    }

    private static void IL_LightingRenderer_BeforeRender(ILContext il) {
        var cursor = new ILCursor(il);

        var usedHiResBuffers = cursor.DefineLabel();
        var useVanillaBuffers = cursor.DefineLabel();

        // hi-res lighting on
        cursor.Index = -1;
        cursor.GotoPrev(MoveType.Before, i => i.MatchLdsfld(typeof(GameplayBuffers), nameof(GameplayBuffers.TempA)));
        cursor.EmitDelegate(usingHiResLighting);
        cursor.EmitBrfalse(useVanillaBuffers);
        cursor.EmitLdsfld(typeof(HiResRenderer).GetField(nameof(TempABufferHiRes), BindingFlags.NonPublic | BindingFlags.Static));
        cursor.EmitLdsfld(typeof(HiResRenderer).GetField(nameof(LightBufferHiRes), BindingFlags.NonPublic | BindingFlags.Static));
        cursor.EmitBr(usedHiResBuffers);
        cursor.MarkLabel(useVanillaBuffers);
        cursor.GotoNext(MoveType.After, i => i.MatchLdsfld(typeof(GameplayBuffers), nameof(GameplayBuffers.Light)));
        cursor.MarkLabel(usedHiResBuffers);

        cursor.GotoNext(MoveType.After, i => i.MatchLdcR4(1f));
        cursor.EmitDelegate(increaseBlur);

        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(GaussianBlur), "Blur"));
        cursor.EmitDelegate(blurEvenMore);

        static bool usingHiResLighting() => Settings.HiResLighting;

        static float increaseBlur(float orig) {
            if (Settings.HiResLighting)
                return 3f;

            return orig;
        }

        static void blurEvenMore() {
            if (Settings.HiResLighting)
                GaussianBlur.Blur((RenderTarget2D)LightBufferHiRes, TempABufferHiRes, LightBufferHiRes);
        }
    }

    private static void IL_LightingRenderer_Render(ILContext il) {
        var cursor = new ILCursor(il);

        // hi-res lighting off
        var usedScaledDraw = cursor.DefineLabel();
        var useVanillaDraw = cursor.DefineLabel();

        cursor.GotoNext(MoveType.Before, i => i.MatchCallOrCallvirt<SpriteBatch>(nameof(SpriteBatch.Draw)));
        cursor.EmitDelegate(usingHiResLighting);
        cursor.EmitBrtrue(useVanillaDraw);
        cursor.EmitDelegate(drawScaled);
        cursor.EmitBr(usedScaledDraw);
        cursor.MarkLabel(useVanillaDraw);
        cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<SpriteBatch>(nameof(SpriteBatch.Draw)));
        cursor.MarkLabel(usedScaledDraw);

        Console.WriteLine(il);

        static bool usingHiResLighting() => Settings?.HiResLighting ?? true;

        static void drawScaled(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color color) =>
            spriteBatch.Draw(texture, position, null, color, 0f, Vector2.Zero, 6f, SpriteEffects.None, 0f);

        // hi-res lighting on
        cursor.Index = -1;
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(typeof(GameplayBuffers), nameof(GameplayBuffers.Light)));
        cursor.EmitDelegate(swapToHiResIfNeeded);

        static VirtualRenderTarget swapToHiResIfNeeded(VirtualRenderTarget lightBuffer) {
            if (!usingHiResLighting())
                return lightBuffer;

            return LightBufferHiRes;
        }
    }

    private static void IL_BloomRenderer_Apply(ILContext il) {
        var cursor = new ILCursor(il);
    }

    #endregion
















    #region Loading

    internal static void LoadLoader() {
        On.Celeste.LevelLoader.ctor += On_LevelLoader_ctor;
        On.Celeste.OverworldLoader.ctor += On_OverworldLoader_ctor;
    }

    internal static void UnloadLoader() {
        On.Celeste.LevelLoader.ctor -= On_LevelLoader_ctor;
        On.Celeste.OverworldLoader.ctor -= On_OverworldLoader_ctor;

        Disable();
    }

    // roughly based on gravityhelper
    private static void On_LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startPosition) {
        Settings = SorbetHelperMetadata.TryGetMetadata(session)?.HiResRendering;
        UpdateDynamicHooks(Settings = null);

        orig(self, session, startPosition);
    }

    private static void On_OverworldLoader_ctor(On.Celeste.OverworldLoader.orig_ctor orig, OverworldLoader self, Overworld.StartMode startMode, HiresSnow hiresSnow) {
        orig(self, startMode, hiresSnow);

        // Don't mistakenly unload the hooks when using a CollabUtils2 chapter panel
        if (startMode != (Overworld.StartMode)(-1))
            UpdateDynamicHooks(Settings = null);
    }

    private static void UpdateDynamicHooks(SorbetHelperMetadata.HiResRenderingData metadata) {
        if (metadata is { Enabled: true })
            Enable();
        else
            Disable();

    }

    #endregion
}