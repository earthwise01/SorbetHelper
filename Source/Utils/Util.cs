using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using Celeste.Mod.SorbetHelper.Entities;
using MonoMod.Utils;
using System.Collections.ObjectModel;

namespace Celeste.Mod.SorbetHelper.Utils;
internal static class Util {
    private static CrystalStaticSpinner rainbowSpinner;
    public static Color GetRainbowHue(Scene scene, Vector2 position) {
        rainbowSpinner ??= new CrystalStaticSpinner(Vector2.Zero, false, CrystalColor.Rainbow);
        rainbowSpinner.Scene = scene;

        return rainbowSpinner.GetHue(position);
    }

    public static Color HexToColorWithAlphaNonPremult(string hex) {
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

    #region zoom out or otherwise nonstandard camera/buffer size support
    // i coullddd just use the interop from ext camera dynamics but im stubborn so im implementing this stuff myself so that it just  generally works rather than being dependent on someone else's specific mod

    /// <summary>
    /// whether "zoom out" is active, checks whether or not the camera viewport and gameplay buffers match their vanilla sizes
    /// </summary>
    public static bool ZoomOutActive => UsingExtendedCameraDynamics || CameraWidth != 320 || CameraHeight != 180 || GameplayBufferWidth != 320 || GameplayBufferHeight != 180;

    /// <summary>
    /// the width of the camera, use instead of inlining Celeste.GameWidth or 320 (for zoom out or otherwise nonstandard camera size support)
    /// </summary>
    public static int CameraWidth => (Engine.Scene as Level)?.Camera.Viewport.Width ?? 320;

    /// <summary>
    /// the height of the camera, use instead of inlining Celeste.GameHeight or 180 (for zoom out or otherwise nonstandard camera size support)
    /// </summary>
    public static int CameraHeight => (Engine.Scene as Level)?.Camera.Viewport.Height ?? 180;

    /// <summary>
    /// the width of the gameplay buffers, use instead of inlining Celeste.GameWidth or 320 (for zoom out or otherwise nonstandard buffer size support)
    /// </summary>
    public static int GameplayBufferWidth => GameplayBuffers.Gameplay?.Width ?? 320;

    /// <summary>
    /// the height of the gameplay buffers, use instead of inlining Celeste.GameHeight or 180 (for zoom out or otherwise nonstandard buffer size support)
    /// </summary>
    public static int GameplayBufferHeight => GameplayBuffers.Gameplay?.Height ?? 180;

    /// <summary>
    /// whether extended camera dynamics specifically is being used for zoom out
    /// </summary>
    public static bool UsingExtendedCameraDynamics => ExtendedCameraDynamicsInterop.IsImported && ExtendedCameraDynamicsInterop.ExtendedCameraHooksEnabled();

    /// <summary>
    /// how far the center of the screen has been shifted as a result of zoom out or otherwise nonstandard camera sizes
    /// </summary>
    public static Vector2 ZoomCenterOffset =>
        new(CameraWidth / 2f - 320f / 2f, CameraHeight / 2f - 180f / 2f);

    /// <summary>
    /// checks if a render target's dimensions match the gameplay buffers' and resizes it if not (for zoom out or otherwise nonstandard buffer size support)
    /// </summary>
    /// <param name="target">the virtual render target to resize</param>
    public static void EnsureBufferSize(VirtualRenderTarget target) {
        if (target is null || target.IsDisposed || (target.Width == GameplayBufferWidth && target.Height == GameplayBufferHeight))
            return;

        target.Width = GameplayBufferWidth;
        target.Height = GameplayBufferHeight;
        target.Reload();
    }

    #endregion

    /// <summary>
    /// maps all Monocle.Ease Easers to their names (i.e. "SineInOut" => Ease.SineInOut)
    /// </summary>
    public static ReadOnlyDictionary<string, Ease.Easer> Easers { get; } = new(new Dictionary<string, Ease.Easer> {
        {"Linear", Ease.Linear},
        {"SineIn", Ease.SineIn},
        {"SineOut", Ease.SineOut},
        {"SineInOut", Ease.SineInOut},
        {"QuadIn", Ease.QuadIn},
        {"QuadOut", Ease.QuadOut},
        {"QuadInOut", Ease.QuadInOut},
        {"CubeIn", Ease.CubeIn},
        {"CubeOut", Ease.CubeOut},
        {"CubeInOut", Ease.CubeInOut},
        {"QuintIn", Ease.QuintIn},
        {"QuintOut", Ease.QuintOut},
        {"QuintInOut", Ease.QuintInOut},
        {"ExpoIn", Ease.ExpoIn},
        {"ExpoOut", Ease.ExpoOut},
        {"ExpoInOut", Ease.ExpoInOut},
        {"BackIn", Ease.BackIn},
        {"BackOut", Ease.BackOut},
        {"BackInOut", Ease.BackInOut},
        {"BigBackIn", Ease.BigBackIn},
        {"BigBackOut", Ease.BigBackOut},
        {"BigBackInOut", Ease.BigBackInOut},
        {"ElasticIn", Ease.ElasticIn},
        {"ElasticOut", Ease.ElasticOut},
        {"ElasticInOut", Ease.ElasticInOut},
        {"BounceIn", Ease.BounceIn},
        {"BounceOut", Ease.BounceOut},
        {"BounceInOut", Ease.BounceInOut},
    });
}
