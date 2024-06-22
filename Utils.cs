using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.SorbetHelper {
    public class Utils {
        private static CrystalStaticSpinner rainbowSpinner;
        public static Color GetRainbowHue(Scene scene, Vector2 position) {
            rainbowSpinner ??= new CrystalStaticSpinner(Vector2.Zero, false, CrystalColor.Rainbow);
            rainbowSpinner.Scene = scene;

            return rainbowSpinner.GetHue(position);
        }
    }
}
