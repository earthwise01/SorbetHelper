using System;
using Monocle;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Entities;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Backdrops;
using MonoMod;
using MonoMod.ModInterop;

namespace Celeste.Mod.SorbetHelper {
    public class SorbetHelperModule : EverestModule {
        public static SorbetHelperModule Instance;

        public static bool ExtendedVariantsLoaded { get; private set; }

        public SorbetHelperModule() {
            Instance = this;
        }

        public override void Initialize() {
            base.Initialize();

            ExtendedVariantsLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata {
                Name = "ExtendedVariantMode",
                Version = new Version(0, 38, 0)
            });
        }

        public override void Load() {
            typeof(GravityHelperImports).ModInterop();

            WingedStrawberryDirectionController.Load();
            DisplacementEffectBlocker.Load();
            DepthAdheringDisplacementRenderHook.Load();
            StylegroundOverHudRenderer.Load();
        }

        public override void Unload() {
            WingedStrawberryDirectionController.Unload();
            DisplacementEffectBlocker.Unload();
            DepthAdheringDisplacementRenderHook.Unload();
            StylegroundOverHudRenderer.Unload();
        }

        public override void PrepareMapDataProcessors(MapDataFixup context) {
            base.PrepareMapDataProcessors(context);

            context.Add<SorbetHelperMapDataProcessor>();
        }
    }
}
