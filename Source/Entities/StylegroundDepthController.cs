using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.SorbetHelper.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
public class StylegroundDepthController : Entity {
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(StylegroundDepthController)}";

    public enum Modes { Normal, AboveColorgrade, AboveHud, AbovePauseHud }

    // only stored in the first added StylegroundDepthController (aka the one returned by Tracker.GetEntity)
    private readonly HashSet<Backdrop> _current = [];
    private StylegroundDepthController _aboveColorgrade, _aboveHud, _abovePauseHud;

    public readonly Modes Mode;
    public readonly HashSet<string> StylegroundTags = [];
    private readonly List<Backdrop> backdropsBg = [], backdropsFg = [];

    // modified additive blendstate that still works after being rendered to a transparent render target
    private static readonly BlendState AdditiveFix = new BlendState() {
        ColorSourceBlend = Blend.SourceAlpha,
        AlphaSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
    };
    private VirtualRenderTarget buffer;
    private static Matrix scaleMatrix;
    private static Vector2 paddingOffset;
    private static Vector2 zoomFocusOffset;
    private static float scale = 1f;

    private static readonly HashSet<Backdrop> EmptyBackdropHashSet = [];
    private static bool renderingDepthBackdrops;

    // also see map data processor
    public readonly record struct StylegroundDepthControllerData(string StylegroundTag, int Depth, Modes Mode);
    private static void Event_OnLoadingThread(Level level) {
        if (!SorbetHelperMapDataProcessor.StylegroundDepthControllers.TryGetValue((level.Session.Area.ID, level.Session.Area.Mode), out List<StylegroundDepthControllerData> depthControllers))
            return;

        StylegroundDepthController trackedController = null;
        List<StylegroundDepthController> currentDepthControllers = [];
        foreach ((string tag, int depth, Modes mode) in depthControllers) {
            if (currentDepthControllers.FirstOrDefault(c => mode == c.Mode && (mode != Modes.Normal || c.Depth == depth)) is not { } depthController) {
                depthController = new StylegroundDepthController(depth, mode);
                level.Add(depthController);
                currentDepthControllers.Add(depthController);
                trackedController ??= depthController;

                // store the instances of any controllers with a non-default mode for easier access when using them in hooks
                switch (mode) {
                    case Modes.AboveColorgrade:
                        trackedController._aboveColorgrade = depthController;
                        break;
                    case Modes.AboveHud:
                        trackedController._aboveHud = depthController;
                        break;
                    case Modes.AbovePauseHud:
                        trackedController._abovePauseHud = depthController;
                        break;
                }

                Logger.Info(LogID, $"creating new {nameof(StylegroundDepthController)} for mode {mode} at depth {depth}.");
            }

            depthController.StylegroundTags.Add(tag);
        }
    }

    private StylegroundDepthController(int depth, Modes mode) {
        Depth = depth;
        Mode = mode;

        Tag |= Tags.Global;

        if (Mode != Modes.Normal)
            Add(new BeforeRenderHook(BeforeRender));
        if (Mode == Modes.AbovePauseHud)
            Tag |= Tags.PauseUpdate;
    }

    private void DrawStylegrounds() {
        renderingDepthBackdrops = true;

        BackdropRenderer rendererBg = SceneAs<Level>().Background;
        BackdropRenderer rendererFg = SceneAs<Level>().Foreground;

        List<Backdrop> backupBgs = rendererBg.Backdrops;
        rendererBg.Backdrops = backdropsBg;
        rendererBg.Render(Scene);
        rendererBg.Backdrops = backupBgs;

        backupBgs = rendererFg.Backdrops;
        rendererFg.Backdrops = backdropsFg;
        rendererFg.Render(Scene);
        rendererFg.Backdrops = backupBgs;

        renderingDepthBackdrops = false;
    }

    private void RegisterStylegrounds(Level level) {
        RegisterStylegrounds(level.Background.Backdrops, backdropsBg);
        RegisterStylegrounds(level.Foreground.Backdrops, backdropsFg);
    }

    private void RegisterStylegrounds(List<Backdrop> origBackdrops, List<Backdrop> into) {
        HashSet<Backdrop> current = Scene.Tracker.GetEntity<StylegroundDepthController>()._current;

        foreach (Backdrop backdrop in origBackdrops) {
            if (backdrop.Tags.Overlaps(StylegroundTags) && current.Add(backdrop)) {
                into.Add(backdrop);

                if (Mode != Modes.Normal && backdrop is Parallax parallax && parallax.BlendState == BlendState.Additive)
                    parallax.BlendState = AdditiveFix;
            }
        }
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        RegisterStylegrounds(SceneAs<Level>());
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);

        buffer?.Dispose();
        buffer = null;
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);

        buffer?.Dispose();
        buffer = null;
    }

    // for the AbovePauseHud mode, we need to manually update the stylegrounds
    public override void Update() {
        base.Update();

        if (Mode != Modes.AbovePauseHud || SceneAs<Level>().Paused)
            return;

        foreach (Backdrop backdrop in backdropsBg.Concat(backdropsFg))
            backdrop.Update(Scene);
    }

    // for "above hud"-type modes, in order to draw the stylegrounds *after* everything else (and at the correct resolution), they need to be drawn to a render target first
    public void BeforeRender() {
        if (Mode == Modes.Normal)
            return;

        buffer ??= VirtualContent.CreateRenderTarget("sorbetHelper_stylegroundDepthControllerBuffer", SorbetHelperGFX.GameplayBufferWidth, SorbetHelperGFX.GameplayBufferHeight);
        SorbetHelperGFX.EnsureBufferSize(buffer);

        Engine.Instance.GraphicsDevice.SetRenderTarget(buffer);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        DrawStylegrounds();
    }

    // otherwise, just draw them directly to the gameplay buffer at the specified depth
    public override void Render() {
        if (Mode != Modes.Normal)
            return;

        GameplayRenderer.End();
        DrawStylegrounds();
        GameplayRenderer.Begin();
    }

    public void RenderToHiRes() {
        if (Mode == Modes.Normal)
            return;

        SpriteEffects spriteEffect = SpriteEffects.None;
        if (SaveData.Instance.Assists.MirrorMode)
            spriteEffect |= SpriteEffects.FlipHorizontally;

        if (ExtendedVariantsCompat.UpsideDown)
            spriteEffect |= SpriteEffects.FlipVertically;

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, scaleMatrix);
        Draw.SpriteBatch.Draw(buffer, zoomFocusOffset + paddingOffset, buffer.Bounds, Color.White * ExtendedVariantsCompat.ForegroundEffectOpacity, 0f, zoomFocusOffset, scale, spriteEffect, 0f);
        Draw.SpriteBatch.End();
    }

    #region Hooks

    internal static void Load() {
        Everest.Events.LevelLoader.OnLoadingThread += Event_OnLoadingThread;

        IL.Celeste.BackdropRenderer.Render += IL_BackdropRenderer_Render;
        IL.Celeste.Level.Render += IL_Level_Render;
    }
    internal static void Unload() {
        Everest.Events.LevelLoader.OnLoadingThread -= Event_OnLoadingThread;

        IL.Celeste.BackdropRenderer.Render -= IL_BackdropRenderer_Render;
        IL.Celeste.Level.Render -= IL_Level_Render;
    }

    // prevent depth backdrops from being rendered normally
    private static void IL_BackdropRenderer_Render(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        VariableDefinition depthRenderedBackdropsVar = new VariableDefinition(il.Import(typeof(HashSet<Backdrop>)));
        il.Body.Variables.Add(depthRenderedBackdropsVar);

        cursor.GotoNext(MoveType.Before, instr => instr.MatchBr(out _));
        cursor.EmitLdarg1();
        cursor.EmitDelegate(GetDepthRenderedBackdrops);
        cursor.EmitStloc(depthRenderedBackdropsVar);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Backdrop>("Visible"))) {
            Logger.Warn(LogID, $"ilhook error! failed to find backdrop visible check in CIL code for {cursor.Method.Name}!");
            return;
        }
        cursor.GotoNext(MoveType.Before, i => i.MatchBrfalse(out _));

        Logger.Verbose(LogID, $"injecting check for styleground depth controllers at {cursor.Index} in CIL code for {cursor.Method.Name}!");

        cursor.EmitLdloc2();
        cursor.EmitLdloc(depthRenderedBackdropsVar);
        cursor.EmitDelegate(CanRenderBackdrop);

        return;

        static HashSet<Backdrop> GetDepthRenderedBackdrops(Scene scene) {
            if (renderingDepthBackdrops)
                return EmptyBackdropHashSet;

            StylegroundDepthController controller = scene.Tracker.GetEntity<StylegroundDepthController>();
            if (controller is null)
                return EmptyBackdropHashSet;

            return controller._current;
        }

        static bool CanRenderBackdrop(bool orig, Backdrop backdrop, HashSet<Backdrop> depthRendered)
            => orig && !depthRendered.Contains(backdrop);
    }

    private static void IL_Level_Render(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdarg0(), i => i.MatchLdfld<Level>(nameof(Level.Pathfinder)))) {
            Logger.Warn(LogID, $"ilhook error! failed to inject above colorgrade rendering in CIL code for {cursor.Method.Name}!");
            return;
        }

        Logger.Verbose(LogID, $"injecting above colorgrade rendering at {cursor.Index} in CIL code for {cursor.Method.Name}!");

        // update upscale data
        cursor.EmitLdloc(2); // matrix
        cursor.EmitLdloc(9); // padding
        cursor.EmitLdloc(5); // zoom focus
        cursor.EmitLdloc(8); // scale
        cursor.EmitDelegate(UpdateUpscaleData);

        // setup locals
        VariableDefinition firstDepthController = new VariableDefinition(il.Import(typeof(StylegroundDepthController)));
        VariableDefinition pausedLocal = new VariableDefinition(il.Import(typeof(bool)));
        il.Body.Variables.Add(firstDepthController);
        il.Body.Variables.Add(pausedLocal);

        cursor.EmitLdarg0();
        cursor.EmitDelegate(GetDepthController);
        cursor.EmitStloc(firstDepthController);

        cursor.EmitLdarg0();
        cursor.EmitLdfld(typeof(Level).GetField(nameof(Level.Paused)));
        cursor.EmitStloc(pausedLocal);

        // render above colorgrade
        cursor.EmitLdloc(firstDepthController);
        cursor.EmitDelegate(RenderAboveColorgrade);

        if (!cursor.TryGotoNext(MoveType.AfterLabel, instr => instr.MatchLdarg0(), instr => instr.MatchLdfld<Level>("SubHudRenderer"))) {
            Logger.Warn(LogID, $"ilhook error! failed to inject below hud rendering in CIL code for {cursor.Method.Name}!");
            return;
        }

        Logger.Verbose(LogID, $"injecting below hud rendering at {cursor.Index} in CIL code for {cursor.Method.Name}!");

        // render below hud
        cursor.EmitLdloc(firstDepthController);
        cursor.EmitLdloc(pausedLocal);
        cursor.EmitDelegate(RenderBelowHud);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Level>("HudRenderer"), instr => instr.MatchLdarg0(), instr => instr.MatchCallOrCallvirt<Renderer>("Render"))) {
            Logger.Warn(LogID, $"ilhook error! failed to inject above hud rendering in CIL code for {cursor.Method.Name}!");
            return;
        }
        cursor.MoveAfterLabels();

        Logger.Verbose(LogID, $"injecting above hud rendering at {cursor.Index} in CIL code for {cursor.Method.Name}!");

        // render above hud
        cursor.EmitLdloc(firstDepthController);
        cursor.EmitLdloc(pausedLocal);
        cursor.EmitDelegate(RenderAboveHud);

        return;

        static void UpdateUpscaleData(Matrix scaleMatrix, Vector2 paddingOffset, Vector2 zoomFocusOffset, float scale) {
            StylegroundDepthController.scaleMatrix = scaleMatrix;
            StylegroundDepthController.paddingOffset = paddingOffset;
            StylegroundDepthController.zoomFocusOffset = zoomFocusOffset;
            StylegroundDepthController.scale = scale;
        }

        static StylegroundDepthController GetDepthController(Level self)
            => self.Tracker.GetEntity<StylegroundDepthController>();

        static void RenderAboveColorgrade(StylegroundDepthController depthController) {
            depthController?._aboveColorgrade?.RenderToHiRes();
        }

        static void RenderBelowHud(StylegroundDepthController depthController, bool paused) {
            if (paused)
                depthController?._aboveHud?.RenderToHiRes();
        }

        static void RenderAboveHud(StylegroundDepthController depthController, bool paused) {
            if (!paused)
                depthController?._aboveHud?.RenderToHiRes();

            depthController?._abovePauseHud?.RenderToHiRes();
        }
    }

    #endregion

}
