﻿using System;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Entities;
using Celeste.Mod.SorbetHelper.Triggers;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Backdrops;
using MonoMod;
using MonoMod.ModInterop;

namespace Celeste.Mod.SorbetHelper;
public class SorbetHelperModule : EverestModule {
    public static SorbetHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(SorbetHelperSettings);
    public static SorbetHelperSettings Settings => (SorbetHelperSettings)Instance._Settings;
    public override Type SessionType => typeof(SorbetHelperSession);
    public static SorbetHelperSession Session => (SorbetHelperSession)Instance._Session;

    public static bool ExtendedVariantsLoaded { get; private set; }
    public static bool ChronoHelperLoaded { get; private set; }

    public static Effect FxAlphaMask { get; private set; }

    public SorbetHelperModule() {
        Instance = this;
    }

    public override void Initialize() {
        base.Initialize();

        ExtendedVariantsLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata {
            Name = "ExtendedVariantMode",
            Version = new Version(0, 38, 0)
        });
        ChronoHelperLoaded = ChronoHelperCompat.TryLoad();

        DashFallingBlock.LoadParticles();
    }

    public override void Load() {
        // mod interop
        typeof(GravityHelperImports).ModInterop();
        typeof(ExtendedCameraDynamicsImports).ModInterop();
        typeof(SorbetHelperExports).ModInterop();

        // sorbet helper misc stuff
        On.Celeste.GameplayBuffers.Unload += On_GameplayBuffers_Unload;

        GlobalEntities.ProcessAttributes();
        GlobalEntities.Load();

        SorbetHelperDecalRegistry.LoadHandlers();

        // entities
        WingedStrawberryDirectionController.Load();
        DisplacementEffectBlocker.Load();
        AlternateInteractPromptWrapper.Load();
        MiniPopupDisplay.Load();
        ReturnBubbleBehaviorController.Load();
        StylegroundDepthController.Load();
        MusicSyncControllerFMOD.Load();
        PufferTweaksController.Load();

        // components
        RenderOverride.Load();
        MovingPlatformHittable.Load();
        ExplodeHittable.Load();
        LightCover.Load();
        GlobalTypeNameProcessor.Load();

        // backdrops
        ParallaxHiResSnow.Load();
        HiResGodrays.Load();
    }

    public override void Unload() {
        // sorbet helper misc stuff
        FxAlphaMask?.Dispose();
        FxAlphaMask = null;

        On.Celeste.GameplayBuffers.Unload -= On_GameplayBuffers_Unload;
        RenderTargetHelper.DisposeQueue();

        GlobalEntities.Unload();

        // entities
        WingedStrawberryDirectionController.Unload();
        DisplacementEffectBlocker.Unload();
        AlternateInteractPromptWrapper.Unload();
        MiniPopupDisplay.Unload();
        ReturnBubbleBehaviorController.Unload();
        StylegroundDepthController.Unload();
        MusicSyncControllerFMOD.Unload();
        PufferTweaksController.Unload();

        // components
        RenderOverride.Unload();
        MovingPlatformHittable.Unload();
        ExplodeHittable.Unload();
        LightCover.Unload();
        GlobalTypeNameProcessor.Unload();

        // backdrops
        ParallaxHiResSnow.Unload();
        HiResGodrays.Unload();
    }

    public override void PrepareMapDataProcessors(MapDataFixup context) {
        base.PrepareMapDataProcessors(context);

        context.Add<SorbetHelperMapDataProcessor>();
    }

    public override void LoadContent(bool firstLoad) {
        FxAlphaMask = LoadShader("AlphaMask");
    }

    private static Effect LoadShader(string id) =>
        new(Engine.Graphics.GraphicsDevice, Everest.Content.Get($"SorbetHelper:/Effects/SorbetHelper/{id}.cso").Data);

    // unload any leftover queued buffers with the normal gameplay buffers
    private static void On_GameplayBuffers_Unload(On.Celeste.GameplayBuffers.orig_Unload orig) {
        orig();
        RenderTargetHelper.DisposeQueue();
    }
}
