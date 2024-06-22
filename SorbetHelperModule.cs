using System;
using Monocle;
using Celeste.Mod.SorbetHelper.Entities;
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
        }

        public override void Unload() {
            WingedStrawberryDirectionController.Unload();
        }
    }
}
