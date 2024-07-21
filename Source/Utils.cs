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

        public static Color HexToRGBAColor(string hex) {
            Color color = Calc.HexToColor(hex);

            int num = 0;
            if (hex.Length >= 1 && hex[0] == '#') {
                num = 1;
            }
            if (hex.Length - num >= 8) {
                float a = (Calc.HexToByte(hex[num + 6]) * 16 + Calc.HexToByte(hex[num + 7])) / 255f;
                color *= a;
            }

            return color;
        }
    }
}
