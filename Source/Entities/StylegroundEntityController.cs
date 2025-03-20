using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Celeste.Mod.Backdrops;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity]
[CustomEntity("SorbetHelper/StylegroundEntityController")]
public class StylegroundEntityController : Entity {
    public static Entity Load(Level level, LevelData levelData, Vector2 position, EntityData entityData) {
        if (entityData.Bool("noConsume", false))
            return new StylegroundEntityControllerNoConsume(entityData, position);
        else
            return new StylegroundEntityController(entityData, position);
    }

    private readonly BackdropRenderer BackdropRenderer = new();

    public readonly string StylegroundTag;
    private bool consumedStylegrounds;

    public StylegroundEntityController(EntityData data, Vector2 _) {
        Depth = data.Int("depth", Depths.Above);
        StylegroundTag = data.Attr("tag", "");

        Add(new BeforeRenderHook(BeforeRender));
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        if (string.IsNullOrEmpty(StylegroundTag)) {
            RemoveSelf();
            return;
        }

        if (!consumedStylegrounds)
            ConsumeStylegrounds(scene as Level);
    }

    public override void Update() {
        BackdropRenderer.Update(Scene);
    }

    public void BeforeRender() {
        BackdropRenderer.BeforeRender(Scene);
    }

    public override void Render() {
        GameplayRenderer.End();
        BackdropRenderer.Render(Scene);
        GameplayRenderer.Begin();
        // backdrop renderer doesnt implement afterrender
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);
        BackdropRenderer.Ended(scene);
    }

    private void ConsumeStylegrounds(Level level) {
        ConsumeStylegrounds(level.Foreground.Backdrops);
        ConsumeStylegrounds(level.Background.Backdrops);
        consumedStylegrounds = true;
        Logger.Log(LogLevel.Verbose, "SorbetHelper", "[StylegroundEntityDepthController] consumed stylegrounds!");
    }

    private void ConsumeStylegrounds(List<Backdrop> origBackdrops) {
        // i dont know why the fk i need to do this but for some bizarre reason it literally doesnt work if i iterate through the list forwards catplush
        for (int i = origBackdrops.Count - 1; i >= 0; i--) {
            // for (int i = 0; i < origBackdrops.Count; i++) {
            var backdrop = origBackdrops[i];

            foreach (string tag in backdrop.Tags) {
                if (tag == StylegroundTag) {
                    BackdropRenderer.Backdrops.Insert(0, backdrop);
                    backdrop.Renderer = BackdropRenderer;
                    origBackdrops.RemoveAt(i);
                }
            }
        }
    }
}

// woww
// context someone ran into a mod compatibility issue because the backdrops werent technicallyyy backdrops anymore
// unfortunately! "fixing" this is so so much messier and even requires an  il hook (did you know? Common Language Runtime detected an invalid program.  fun !)
// so ill still add this because   helper dev! i love making stuff for public use .   but its going to be seperate and not the default because of how niche the fix seems to me
[Tracked]
public class StylegroundEntityControllerNoConsume : Entity {
    private readonly BackdropRenderer BackdropRenderer = new();
    private readonly List<Backdrop> BackdropsBG = [], BackdropsFG = [];
    private readonly HashSet<Backdrop> Current = [];

    public readonly string StylegroundTag;

    public StylegroundEntityControllerNoConsume(EntityData data, Vector2 _) {
        Depth = data.Int("depth", Depths.Above);
        StylegroundTag = data.Attr("tag", "");

        LoadIfNeeded();
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        if (string.IsNullOrEmpty(StylegroundTag)) {
            RemoveSelf();
            return;
        }

        RegisterStylegrounds(scene as Level);
    }

    public override void Render() {
        GameplayRenderer.End();
        RenderingEntityBackdrops = true;

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

        RenderingEntityBackdrops = false;
        GameplayRenderer.Begin();
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);
        BackdropRenderer.Ended(scene);
    }

    private void RegisterStylegrounds(Level level) {
        RegisterStylegrounds(level.Foreground.Backdrops, BackdropsFG);
        RegisterStylegrounds(level.Background.Backdrops, BackdropsBG);
    }

    private void RegisterStylegrounds(List<Backdrop> origBackdrops, List<Backdrop> into) {
        // i dont know why the fk i need to do this but for some bizarre reason it literally doesnt work if i iterate through the list forwards catplush
        for (int i = origBackdrops.Count - 1; i >= 0; i--) {
            // for (int i = 0; i < origBackdrops.Count; i++) {
            var backdrop = origBackdrops[i];

            foreach (string tag in backdrop.Tags) {
                if (tag == StylegroundTag && Current.Add(backdrop)) {
                    into.Insert(0, backdrop);
                }
            }
        }
    }
    private static readonly HashSet<Backdrop> EmptyBackdropHashSet = [];
    private static bool RenderingEntityBackdrops;
    private static void IL_BackdropRenderer_Render(ILContext il) {
        var cursor = new ILCursor(il);

        var entityRenderedBackdropsVar = new VariableDefinition(il.Import(typeof(HashSet<Backdrop>)));
        il.Body.Variables.Add(entityRenderedBackdropsVar);

        cursor.GotoNext(MoveType.Before, instr => instr.MatchBr(out _));
        cursor.EmitLdarg1();
        cursor.EmitDelegate(getEntityRenderedBackdrops);
        cursor.EmitStloc(entityRenderedBackdropsVar);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Backdrop>("Visible"))) {
            Logger.Warn("SorbetHelper", $"ilhook error! failed to find backdrop visible check in CIL code for {cursor.Method.Name}!");
            return;
        }

        Logger.Verbose("SorbetHelper", $"injecting check to skip rendering styleground as entity backdrops at {cursor.Index} in CIL code for {cursor.Method.Name}!");

        cursor.EmitLdloc2();
        cursor.EmitLdloc(entityRenderedBackdropsVar);
        cursor.EmitDelegate(canRenderBackdrop);

        static HashSet<Backdrop> getEntityRenderedBackdrops(Scene scene) {
            if (RenderingEntityBackdrops)
                return EmptyBackdropHashSet;

            var controllers = scene.Tracker.GetEntities<StylegroundEntityControllerNoConsume>();
            if (controllers.Count == 0)
                return EmptyBackdropHashSet;

            var result = new HashSet<Backdrop>();
            foreach (StylegroundEntityControllerNoConsume controller in controllers)
                result.UnionWith(controller.Current);

            return result;
        }

        static bool canRenderBackdrop(bool orig, Backdrop backdrop, HashSet<Backdrop> entityRendered) => orig && !entityRendered.Contains(backdrop);
    }

    private static bool Loaded;
    internal static void LoadIfNeeded() {
        if (Loaded)
            return;

        IL.Celeste.BackdropRenderer.Render += IL_BackdropRenderer_Render;

        Loaded = true;
    }
    internal static void UnloadIfNeeded() {
        if (!Loaded)
            return;

        IL.Celeste.BackdropRenderer.Render -= IL_BackdropRenderer_Render;
    }
}

