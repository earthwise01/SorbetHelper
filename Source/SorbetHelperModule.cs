using System;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Entities;
using Celeste.Mod.SorbetHelper.Components;
using MonoMod;
using MonoMod.ModInterop;

namespace Celeste.Mod.SorbetHelper {
    public class SorbetHelperModule : EverestModule {
        public static SorbetHelperModule Instance;

        public static Effect AlphaMaskShader { get; private set; }

        public SorbetHelperModule() {
            Instance = this;
        }

        public override void Load() {
            typeof(GravityHelperImports).ModInterop();

            WingedStrawberryDirectionController.Load();
            DisplacementEffectBlocker.Load();
            DepthAdheringDisplacementRenderHook.Load();
            LightCoverComponent.Load();
        }

        public override void Unload() {
            WingedStrawberryDirectionController.Unload();
            DisplacementEffectBlocker.Unload();
            DepthAdheringDisplacementRenderHook.Unload();
            LightCoverComponent.Unload();
        }

        public override void LoadContent(bool firstLoad) {
            AlphaMaskShader = LoadShader("alpha_mask");
        }

        private static Effect LoadShader(string id)
            => new(Engine.Graphics.GraphicsDevice, Everest.Content.Get($"SorbetHelper:/Effects/SorbetHelper/{id}.fxb").Data);
    }
}
