using System;
using Monocle;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Entities;
using Celeste.Mod.SorbetHelper.Components;
using MonoMod;
using MonoMod.ModInterop;

namespace Celeste.Mod.SorbetHelper {
    public class SorbetHelperModule : EverestModule {
        public static SorbetHelperModule Instance;

        public SorbetHelperModule() {
            Instance = this;
        }

        public override void Load() {
            typeof(GravityHelperImports).ModInterop();

            WingedStrawberryDirectionController.Load();
            DisplacementEffectBlocker.Load();
            DepthAdheringDisplacementRenderHook.Load();
            DisableFrictionComponent.Load();
            ExclamationBlock.Load();
        }

        public override void Unload() {
            WingedStrawberryDirectionController.Unload();
            DisplacementEffectBlocker.Unload();
            DepthAdheringDisplacementRenderHook.Unload();
            DisableFrictionComponent.Unload();
            ExclamationBlock.Unload();
        }

        public override void Initialize() {
            ExclamationBlock.Intitialize();
        }
    }
}
