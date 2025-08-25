using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Celeste.Mod.Backdrops;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Celeste.Mod.SorbetHelper.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
public class StylegroundDepthController : Entity {
    public enum Modes { Normal, AboveColorgrade, AboveHud, AbovePauseHud }

    // -- Tracker.GetEntity controller's data
    private readonly HashSet<Backdrop> _current = [];
    private StylegroundDepthController _aboveColorgrade, _aboveHud, _abovePauseHud;

    // -- current controller's data --
    public readonly Modes Mode = Modes.Normal;
    public readonly HashSet<string> StylegroundTags = [];
    private readonly List<Backdrop> BackdropsBG = [], BackdropsFG = [];

    // -- above hud rendering specific data --
    private static BlendState AdditiveFix = new BlendState() {
        // modified additive blendstate that still works after being rendered to a transparent render target
        ColorSourceBlend = Blend.SourceAlpha,
        AlphaSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
    };
    private VirtualRenderTarget Buffer;
    private static Matrix scaleMatrix;
    private static Vector2 paddingOffset;
    private static Vector2 zoomFocusOffset;
    private static float scale = 1f;

    // -- loading / constructor --
    // see map data processor
    public readonly record struct StylegroundDepthControllerData(string StylegroundTag, int Depth, Modes Mode);
    private static void Event_OnLoadingThread(Level level) {
        if (!SorbetHelperMapDataProcessor.StylegroundDepthControllers.TryGetValue((level.Session.Area.ID, level.Session.Area.Mode), out var depthControllers))
            return;

        StylegroundDepthController trackedController = null;
        var currentDepthControllers = new List<StylegroundDepthController>();
        foreach ((string tag, int depth, Modes mode) in depthControllers) {
            if (currentDepthControllers.FirstOrDefault(c => mode == c.Mode && ((mode != Modes.Normal || c.Depth == depth))) is not { } depthController) {
                depthController = new StylegroundDepthController(depth, mode);
                level.Add(depthController);
                currentDepthControllers.Add(depthController);
                trackedController ??= depthController;

                // store the instances of any controllers with a non-default mode for easier access when using them in hooks
                switch (mode) {
                    case Modes.Normal:
                        break;
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

                Logger.Info("SorbetHelper", $"creating new {nameof(StylegroundDepthController)} for mode {mode} at depth {depth}.");
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
        RenderingDepthBackdrops = true;

        var rendererBg = (Scene as Level).Background;
        var rendererFg = (Scene as Level).Foreground;

        var backupBgs = rendererBg.Backdrops;
        rendererBg.Backdrops = BackdropsBG;
        rendererBg.Render(Scene);
        rendererBg.Backdrops = backupBgs;

        backupBgs = rendererFg.Backdrops;
        rendererFg.Backdrops = BackdropsFG;
        rendererFg.Render(Scene);
        rendererFg.Backdrops = backupBgs;

        RenderingDepthBackdrops = false;
    }

    private void RegisterStylegrounds(Level level) {
        RegisterStylegrounds(level.Background.Backdrops, BackdropsBG);
        RegisterStylegrounds(level.Foreground.Backdrops, BackdropsFG);
    }

    private void RegisterStylegrounds(List<Backdrop> origBackdrops, List<Backdrop> into) {
        var current = Scene.Tracker.GetEntity<StylegroundDepthController>()._current;

        foreach (var backdrop in origBackdrops) {
            if (backdrop.Tags.Overlaps(StylegroundTags) && current.Add(backdrop)) {
                into.Add(backdrop);

                if (Mode != Modes.Normal && backdrop is Parallax parallax && parallax.BlendState == BlendState.Additive)
                    parallax.BlendState = AdditiveFix;
            }
        }
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        RegisterStylegrounds(scene as Level);
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);

        Buffer?.Dispose();
        Buffer = null;
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);

        Buffer?.Dispose();
        Buffer = null;
    }

    // for the AbovePauseHud mode, we need to manually update the stylegrounds
    public override void Update() {
        base.Update();

        if (Mode != Modes.AbovePauseHud || !(Scene as Level).Paused)
            return;

        foreach (var backdrop in BackdropsBG.Concat(BackdropsFG))
            backdrop.Update(Scene);
    }

    // for "above hud"-type modes, in order to draw the stylegrounds *after* everything else (and at the correct resolution), they need to be drawn to a render target first
    public void BeforeRender() {
        if (Mode == Modes.Normal)
            return;

        Buffer ??= VirtualContent.CreateRenderTarget("sorbetHelper_stylegroundDepthControllerBuffer", Util.GameplayBufferWidth, Util.GameplayBufferHeight);
        Util.EnsureBufferSize(Buffer);

        Engine.Instance.GraphicsDevice.SetRenderTarget(Buffer);
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

    // prevent depth backdrops from being rendered normally
    private static readonly HashSet<Backdrop> EmptyBackdropHashSet = [];
    private static bool RenderingDepthBackdrops;
    private static void IL_BackdropRenderer_Render(ILContext il) {
        var cursor = new ILCursor(il);

        var depthRenderedBackdropsVar = new VariableDefinition(il.Import(typeof(HashSet<Backdrop>)));
        il.Body.Variables.Add(depthRenderedBackdropsVar);

        cursor.GotoNext(MoveType.Before, instr => instr.MatchBr(out _));
        cursor.EmitLdarg1();
        cursor.EmitDelegate(getDepthRenderedBackdrops);
        cursor.EmitStloc(depthRenderedBackdropsVar);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Backdrop>("Visible"))) {
            Logger.Warn("SorbetHelper", $"ilhook error! failed to find backdrop visible check in CIL code for {cursor.Method.Name}!");
            return;
        }
        cursor.GotoNext(MoveType.Before, i => i.MatchBrfalse(out _));

        Logger.Verbose("SorbetHelper", $"injecting check for styleground depth controllers at {cursor.Index} in CIL code for {cursor.Method.Name}!");

        cursor.EmitLdloc2();
        cursor.EmitLdloc(depthRenderedBackdropsVar);
        cursor.EmitDelegate(canRenderBackdrop);

        static HashSet<Backdrop> getDepthRenderedBackdrops(Scene scene) {
            if (RenderingDepthBackdrops)
                return EmptyBackdropHashSet;

            var controller = scene.Tracker.GetEntity<StylegroundDepthController>();
            if (controller is null)
                return EmptyBackdropHashSet;

            return controller._current;
        }

        static bool canRenderBackdrop(bool orig, Backdrop backdrop, HashSet<Backdrop> depthRendered) => orig && !depthRendered.Contains(backdrop);
    }

    //
    public void RenderToHiRes() {
        if (Mode == Modes.Normal)
            return;

        var spriteEffect = SpriteEffects.None;
        if (SaveData.Instance.Assists.MirrorMode)
            spriteEffect |= SpriteEffects.FlipHorizontally;

        if (ExtendedVariantsCompat.UpsideDown)
            spriteEffect |= SpriteEffects.FlipVertically;

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, scaleMatrix);
        Draw.SpriteBatch.Draw(Buffer, zoomFocusOffset + paddingOffset, Buffer.Bounds, Color.White * ExtendedVariantsCompat.ForegroundEffectOpacity, 0f, zoomFocusOffset, scale, spriteEffect, 0f);
        Draw.SpriteBatch.End();
    }

    private static void IL_Level_Render(ILContext il) {
        var cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdarg0(), i => i.MatchLdfld<Level>(nameof(Level.Pathfinder)))) {
            Logger.Warn("SorbetHelper", $"ilhook error! failed to inject above colorgrade rendering in CIL code for {cursor.Method.Name}!");
            return;
        }

        Logger.Verbose("SorbetHelper", $"injecting above colorgrade rendering at {cursor.Index} in CIL code for {cursor.Method.Name}!");

        // -- update upscale data

        cursor.EmitLdloc(2); // matrix
        cursor.EmitLdloc(9); // padding
        cursor.EmitLdloc(5); // zoom focus
        cursor.EmitLdloc(8); // scale
        cursor.EmitDelegate(updateUpscaleData);

        static void updateUpscaleData(Matrix scaleMatrix, Vector2 paddingOffset, Vector2 zoomFocusOffset, float scale) {
            StylegroundDepthController.scaleMatrix = scaleMatrix;
            StylegroundDepthController.paddingOffset = paddingOffset;
            StylegroundDepthController.zoomFocusOffset = zoomFocusOffset;
            StylegroundDepthController.scale = scale;
        }

        // -- setup locals

        var firstDepthController = new VariableDefinition(il.Import(typeof(StylegroundDepthController)));
        var pausedLocal = new VariableDefinition(il.Import(typeof(bool)));
        il.Body.Variables.Add(firstDepthController);
        il.Body.Variables.Add(pausedLocal);

        cursor.EmitLdarg0();
        cursor.EmitDelegate(getDepthController);
        cursor.EmitStloc(firstDepthController);

        cursor.EmitLdarg0();
        cursor.EmitLdfld(typeof(Level).GetField(nameof(Level.Paused)));
        cursor.EmitStloc(pausedLocal);

        static StylegroundDepthController getDepthController(Level self) {
            return self.Tracker.GetEntity<StylegroundDepthController>();
        }

        // -- actual rendering

        cursor.EmitLdloc(firstDepthController);
        cursor.EmitDelegate(renderAboveColorgrade);

        if (!cursor.TryGotoNext(MoveType.AfterLabel, instr => instr.MatchLdarg0(), instr => instr.MatchLdfld<Level>("SubHudRenderer"))) {
            Logger.Warn("SorbetHelper", $"ilhook error! failed to inject below hud rendering in CIL code for {cursor.Method.Name}!");
            return;
        }

        Logger.Verbose("SorbetHelper", $"injecting below hud rendering at {cursor.Index} in CIL code for {cursor.Method.Name}!");

        // render below hud
        cursor.EmitLdloc(firstDepthController);
        cursor.EmitLdloc(pausedLocal);
        cursor.EmitDelegate(renderBelowHud);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Level>("HudRenderer"), instr => instr.MatchLdarg0(), instr => instr.MatchCallOrCallvirt<Renderer>("Render"))) {
            Logger.Warn("SorbetHelper", $"ilhook error! failed to inject above hud rendering in CIL code for {cursor.Method.Name}!");
            return;
        }
        cursor.MoveAfterLabels();

        Logger.Verbose("SorbetHelper", $"injecting above hud rendering at {cursor.Index} in CIL code for {cursor.Method.Name}!");

        // render above hud
        cursor.EmitLdloc(firstDepthController);
        cursor.EmitLdloc(pausedLocal);
        cursor.EmitDelegate(renderAboveHud);

        static void renderAboveColorgrade(StylegroundDepthController depthController) {
            depthController?._aboveColorgrade?.RenderToHiRes();
        }

        static void renderBelowHud(StylegroundDepthController depthController, bool paused) {
            if (paused)
                depthController?._aboveHud?.RenderToHiRes();
        }

        static void renderAboveHud(StylegroundDepthController depthController, bool paused) {
            if (!paused)
                depthController?._aboveHud?.RenderToHiRes();

            depthController?._abovePauseHud?.RenderToHiRes();
        }
    }

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
}
