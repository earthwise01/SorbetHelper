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

        public static MTexture[,] CreateNineSlice(MTexture source, int tileWidth, int tileHeight) {
            MTexture[,] nineSlice = new MTexture[3,3];

            for (int x = 0; x < 3; x++) {
                for (int y = 0; y < 3; y++) {
                    nineSlice[x, y] = source.GetSubtexture(x * tileWidth, y * tileHeight, tileWidth, tileHeight);
                }
            }

            return nineSlice;
        }

        // overlayNineSlice is maybe a bit of a werid thing to put in just a "regular" utils nineslice renderer but its needed to make the flash effect on empty blocks
        public static void RenderNineSlice(Vector2 position, MTexture[,] nineSlice, int width, int height, Vector2 scale) => RenderNineSlice(position, nineSlice, null, 0f, width, height, scale);
        public static void RenderNineSlice(Vector2 position, MTexture[,] nineSlice, MTexture[,] overlayNineSlice, float overlayNineSliceFade, int width, int height, Vector2 scale) {
            Vector2 center = position + new Vector2(width * 8f / 2f, height * 8f / 2f);

            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    int textureX = x < width - 1 ? Math.Min(x, 1) : 2;
                    int textureY = y < height - 1 ? Math.Min(y, 1) : 2;
                    Vector2 tilePosition = position + new Vector2(x * 8, y * 8) + new Vector2(4, 4);
                    tilePosition = center + ((tilePosition - center) * scale);

                    nineSlice[textureX, textureY].DrawCentered(tilePosition, Color.White);
                    if (overlayNineSlice != null && overlayNineSliceFade > 0f) overlayNineSlice[textureX, textureY].DrawCentered(tilePosition, Color.White * overlayNineSliceFade);
                }
            }
        }
    }
}
