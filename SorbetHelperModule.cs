using System;
using Monocle;
using Celeste.Mod.SorbetHelper.Entities;

namespace Celeste.Mod.SorbetHelper {
    public class SorbetHelperModule : EverestModule {
        
        public static SorbetHelperModule Instance;
        public SorbetHelperModule() {
            Instance = this;
        }

                public override void Load() {
            DashFallingBlock.Load();
        }

        public override void Unload() {
            DashFallingBlock.Unload();
        }

    }

}