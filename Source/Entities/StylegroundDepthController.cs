namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
public class StylegroundDepthController : Entity
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(StylegroundDepthController)}";

    public enum Modes
    {
        Normal,
        AboveColorgrade,
        AboveHud,
        AbovePauseHud
    }

    public readonly record struct StylegroundDepthControllerData(string StylegroundTag, int Depth, Modes Mode);

    // grrrr i kind of hate having this be a static class like thiss but im stupid and cant think of another way that doesnt have its own downsides
    private static class CameraToScreenData
    {
        public static Matrix UpscaleMatrix;
        public static Vector2 PaddingOffset;
        public static Vector2 ZoomOrigin;
        public static float Scale;
    }

    private static readonly BlendState AdditiveTransparentAlphaBlendDestinationFix = new BlendState()
    {
        ColorSourceBlend = Blend.SourceAlpha,
        AlphaSourceBlend = Blend.Zero, // Blend.SourceAlpha,
        ColorDestinationBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
    };

    private static readonly HashSet<Backdrop> EmptyBackdropHashSet = [];

    private static bool renderingDepthBackdrops;

    // only assigned on the first added StylegroundDepthController (ie the one returned by Tracker.GetEntity)
    private HashSet<Backdrop> allAffectedBackdrops;
    private StylegroundDepthController aboveColorgradeController, aboveHudController, abovePauseHudController;

    private readonly Modes mode;
    private readonly HashSet<string> stylegroundTags = [];
    private readonly List<Backdrop> backdropsBg = [], backdropsFg = [];
    private VirtualRenderTarget buffer;
    
    private StylegroundDepthController(int depth, Modes mode)
    {
        Depth = depth;
        this.mode = mode;

        Tag |= Tags.Global;

        if (this.mode != Modes.Normal)
            Add(new BeforeRenderHook(BeforeRender));
        if (this.mode == Modes.AbovePauseHud)
            Tag |= Tags.PauseUpdate;
    }

    private void RegisterStylegrounds(Level level)
    {
        RegisterStylegrounds(level.Background.Backdrops, backdropsBg);
        RegisterStylegrounds(level.Foreground.Backdrops, backdropsFg);
    }

    private void RegisterStylegrounds(List<Backdrop> from, List<Backdrop> into)
    {
        HashSet<Backdrop> current = Scene.Tracker.GetEntity<StylegroundDepthController>().allAffectedBackdrops ??= [];

        foreach (Backdrop backdrop in from)
        {
            if (!backdrop.Tags.Overlaps(stylegroundTags) || !current.Add(backdrop))
                continue;

            into.Add(backdrop);

            if (backdrop is Parallax parallax && parallax.BlendState == BlendState.Additive)
                parallax.BlendState = AdditiveTransparentAlphaBlendDestinationFix;
        }
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        RegisterStylegrounds(SceneAs<Level>());
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);

        RenderTargetHelper.DisposeAndSetNull(ref buffer);
    }

    public override void SceneEnd(Scene scene)
    {
        base.SceneEnd(scene);

        RenderTargetHelper.DisposeAndSetNull(ref buffer);
    }

    public override void Update()
    {
        base.Update();

        // for the AbovePauseHud mode we need to manually update the stylegrounds when paused
        if (mode != Modes.AbovePauseHud || !SceneAs<Level>().Paused)
            return;

        foreach (Backdrop backdrop in backdropsBg.Concat(backdropsFg))
            backdrop.Update(Scene);
    }

    private void DrawStylegrounds()
    {
        using (new SetTemporaryValue<bool>(ref renderingDepthBackdrops, true))
        {
            BackdropRenderer bgRenderer = SceneAs<Level>().Background;
            using (new SetTemporaryValue<List<Backdrop>>(ref bgRenderer.Backdrops, backdropsBg))
                bgRenderer.Render(Scene);

            BackdropRenderer fgRenderer = SceneAs<Level>().Foreground;
            using (new SetTemporaryValue<List<Backdrop>>(ref fgRenderer.Backdrops, backdropsFg))
                fgRenderer.Render(Scene);
        }
    }

    // for "above hud"-type modes, in order to draw the stylegrounds *after* everything else (and at the correct resolution), they need to be drawn to a render target first
    private void BeforeRender()
    {
        if (mode == Modes.Normal)
            return;

        buffer ??= VirtualContent.CreateRenderTarget("sorbetHelper_stylegroundDepthControllerBuffer", SorbetHelperGFX.GameplayBufferWidth, SorbetHelperGFX.GameplayBufferHeight);
        SorbetHelperGFX.EnsureBufferSize(buffer);

        Engine.Instance.GraphicsDevice.SetRenderTarget(buffer);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        DrawStylegrounds();
    }

    // otherwise, just draw them directly to the gameplay buffer
    public override void Render()
    {
        if (mode != Modes.Normal)
            return;

        GameplayRenderer.End();
        DrawStylegrounds();
        GameplayRenderer.Begin();
    }

    private void RenderToHiRes()
    {
        if (mode == Modes.Normal)
            return;

        SpriteEffects spriteEffect = SpriteEffects.None;
        if (SaveData.Instance.Assists.MirrorMode)
            spriteEffect |= SpriteEffects.FlipHorizontally;

        if (ExtendedVariantsCompat.IsLoaded && ExtendedVariantsCompat.GetUpsideDown())
            spriteEffect |= SpriteEffects.FlipVertically;

        Color color = Color.White;
        if (ExtendedVariantsCompat.IsLoaded)
            color *= ExtendedVariantsCompat.GetForegroundEffectOpacity();

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, CameraToScreenData.UpscaleMatrix);
        Draw.SpriteBatch.Draw(buffer, CameraToScreenData.ZoomOrigin + CameraToScreenData.PaddingOffset, buffer.Bounds, color, 0f, CameraToScreenData.ZoomOrigin, CameraToScreenData.Scale, spriteEffect, 0f);
        Draw.SpriteBatch.End();
    }

    #region Hooks

    internal static void Load()
    {
        Everest.Events.LevelLoader.OnLoadingThread += Event_OnLoadingThread;

        IL.Celeste.BackdropRenderer.Render += IL_BackdropRenderer_Render;
        IL.Celeste.Level.Render += IL_Level_Render;
    }

    internal static void Unload()
    {
        Everest.Events.LevelLoader.OnLoadingThread -= Event_OnLoadingThread;

        IL.Celeste.BackdropRenderer.Render -= IL_BackdropRenderer_Render;
        IL.Celeste.Level.Render -= IL_Level_Render;
    }

    private static void Event_OnLoadingThread(Level level)
    {
        if (!SorbetHelperMapDataProcessor.StylegroundDepthControllers.TryGetValue((level.Session.Area.ID, level.Session.Area.Mode), out List<StylegroundDepthControllerData> depthControllers))
            return;

        StylegroundDepthController trackedController = null;
        List<StylegroundDepthController> currentDepthControllers = [];
        foreach ((string tag, int depth, Modes mode) in depthControllers)
        {
            if (currentDepthControllers.FirstOrDefault(c => mode == c.mode && (mode != Modes.Normal || c.Depth == depth)) is not { } depthController)
            {
                depthController = new StylegroundDepthController(depth, mode);
                level.Add(depthController);
                currentDepthControllers.Add(depthController);
                trackedController ??= depthController;

                // store the instances of any controllers with a non-default mode for quicker access when using them in the Level.Render hook
                switch (mode)
                {
                    case Modes.AboveColorgrade:
                        trackedController.aboveColorgradeController = depthController;
                        break;
                    case Modes.AboveHud:
                        trackedController.aboveHudController = depthController;
                        break;
                    case Modes.AbovePauseHud:
                        trackedController.abovePauseHudController = depthController;
                        break;
                }

                Logger.Info(LogID, $"creating new {nameof(StylegroundDepthController)} for mode {mode} at depth {depth}.");
            }

            depthController.stylegroundTags.Add(tag);
        }
    }

    private static void IL_BackdropRenderer_Render(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        VariableDefinition depthRenderedBackdropsVariable = cursor.AddVariable<HashSet<Backdrop>>();

        if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchBr(out _)))
            throw new HookHelper.HookException(il, "Unable to find start of rendering loop to get depth controller affected backdrops before.");

        cursor.EmitLdarg1();
        cursor.EmitDelegate(GetDepthControllerAffectedBackdrops);
        cursor.EmitStloc(depthRenderedBackdropsVariable);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Backdrop>(nameof(Backdrop.Visible)))
            || !cursor.TryGotoNext(MoveType.Before, i => i.MatchBrfalse(out _)))
            throw new HookHelper.HookException(il, "Unable to find `Backdrop.Visible` check to modify.`");

        cursor.EmitLdloc2();
        cursor.EmitLdloc(depthRenderedBackdropsVariable);
        cursor.EmitDelegate(CanRenderBackdrop);

        return;

        static HashSet<Backdrop> GetDepthControllerAffectedBackdrops(Scene scene)
        {
            if (renderingDepthBackdrops)
                return EmptyBackdropHashSet;

            return scene.Tracker.GetEntity<StylegroundDepthController>()?.allAffectedBackdrops ?? EmptyBackdropHashSet;
        }

        static bool CanRenderBackdrop(bool orig, Backdrop backdrop, HashSet<Backdrop> depthRendered)
            => orig && !depthRendered.Contains(backdrop);
    }

    private static void IL_Level_Render(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNextBestFit(MoveType.Before,
            instr => instr.MatchLdarg0(),
            instr => instr.MatchLdfld<Level>(nameof(Level.Pathfinder))))
            throw new HookHelper.HookException(il, "Unable to find pathfinder rendering to render stylegrounds before.");

        // initalize local variables
        VariableDefinition trackedDepthControllerVariable = cursor.AddVariable<StylegroundDepthController>();
        VariableDefinition pausedVariable = cursor.AddVariable<bool>();

        cursor.EmitLdloc(2); // matrix
        cursor.EmitLdloc(9); // padding
        cursor.EmitLdloc(5); // zoom focus
        cursor.EmitLdloc(8); // scale
        cursor.EmitDelegate(UpdateCameraToScreenData);

        cursor.EmitLdarg0();
        cursor.EmitDelegate(GetTrackedController);
        cursor.EmitStloc(trackedDepthControllerVariable);

        cursor.EmitLdarg0();
        cursor.EmitLdfld(typeof(Level).GetField(nameof(Level.Paused), HookHelper.Bind.PublicInstance)!);
        cursor.EmitStloc(pausedVariable);

        // render above colorgrade
        cursor.EmitLdloc(trackedDepthControllerVariable);
        cursor.EmitDelegate(RenderAboveColorgrade);

        if (!cursor.TryGotoNextBestFit(MoveType.After,
            instr => instr.MatchLdfld<Level>(nameof(Level.SubHudRenderer)),
            instr => instr.MatchLdarg0(),
            instr => instr.MatchCallOrCallvirt<Renderer>(nameof(Renderer.Render))))
            throw new HookHelper.HookException(il, "Unable to find sub hud rendering to render stylegrounds after.");

        // render below hud (when paused)
        cursor.EmitLdloc(trackedDepthControllerVariable);
        cursor.EmitLdloc(pausedVariable);
        cursor.EmitDelegate(RenderBelowHud);

        if (!cursor.TryGotoNextBestFit(MoveType.After,
            instr => instr.MatchLdfld<Level>(nameof(Level.HudRenderer)),
            instr => instr.MatchLdarg0(),
            instr => instr.MatchCallOrCallvirt<Renderer>(nameof(Renderer.Render))))
            throw new HookHelper.HookException(il, "Unable to find hud rendering to render stylegrounds after.");

        cursor.MoveAfterLabels();

        // render above hud
        cursor.EmitLdloc(trackedDepthControllerVariable);
        cursor.EmitLdloc(pausedVariable);
        cursor.EmitDelegate(RenderAboveHud);

        return;

        static void UpdateCameraToScreenData(Matrix scaleMatrix, Vector2 paddingOffset, Vector2 zoomFocusOffset, float scale)
        {
            CameraToScreenData.UpscaleMatrix = scaleMatrix;
            CameraToScreenData.PaddingOffset = paddingOffset;
            CameraToScreenData.ZoomOrigin = zoomFocusOffset;
            CameraToScreenData.Scale = scale;
        }

        static StylegroundDepthController GetTrackedController(Level self)
        {
            return self.Tracker.GetEntity<StylegroundDepthController>();
        }

        static void RenderAboveColorgrade(StylegroundDepthController trackedController)
        {
            trackedController?.aboveColorgradeController?.RenderToHiRes();
        }

        static void RenderBelowHud(StylegroundDepthController trackedController, bool paused)
        {
            if (paused)
                trackedController?.aboveHudController?.RenderToHiRes();
        }

        static void RenderAboveHud(StylegroundDepthController trackedController, bool paused)
        {
            if (!paused)
                trackedController?.aboveHudController?.RenderToHiRes();

            trackedController?.abovePauseHudController?.RenderToHiRes();
        }
    }

    #endregion
}
