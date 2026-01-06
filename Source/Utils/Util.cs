using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.SorbetHelper.Utils;
internal static class Util {
    public static void DisposeAndSetNull(ref Hook hook) {
        hook?.Dispose();
        hook = null;
    }

    public static void DisposeAndSetNull(ref ILHook ilHook) {
        ilHook?.Dispose();
        ilHook = null;
    }

    private static CrystalStaticSpinner rainbowSpinner;
    public static Color GetRainbowHue(Scene scene, Vector2 position) {
        rainbowSpinner ??= new CrystalStaticSpinner(Vector2.Zero, false, CrystalColor.Rainbow);
        rainbowSpinner.Scene = scene;

        return rainbowSpinner.GetHue(position);
    }

    /// <summary>
    /// maps all Monocle.Ease Easers to their names (i.e. "SineInOut" => Ease.SineInOut)
    /// </summary>
    public static ReadOnlyDictionary<string, Ease.Easer> Easers { get; } = new ReadOnlyDictionary<string, Ease.Easer>(
        new Dictionary<string, Ease.Easer> {
            { "Linear", Ease.Linear },
            { "SineIn", Ease.SineIn },
            { "SineOut", Ease.SineOut },
            { "SineInOut", Ease.SineInOut },
            { "QuadIn", Ease.QuadIn },
            { "QuadOut", Ease.QuadOut },
            { "QuadInOut", Ease.QuadInOut },
            { "CubeIn", Ease.CubeIn },
            { "CubeOut", Ease.CubeOut },
            { "CubeInOut", Ease.CubeInOut },
            { "QuintIn", Ease.QuintIn },
            { "QuintOut", Ease.QuintOut },
            { "QuintInOut", Ease.QuintInOut },
            { "ExpoIn", Ease.ExpoIn },
            { "ExpoOut", Ease.ExpoOut },
            { "ExpoInOut", Ease.ExpoInOut },
            { "BackIn", Ease.BackIn },
            { "BackOut", Ease.BackOut },
            { "BackInOut", Ease.BackInOut },
            { "BigBackIn", Ease.BigBackIn },
            { "BigBackOut", Ease.BigBackOut },
            { "BigBackInOut", Ease.BigBackInOut },
            { "ElasticIn", Ease.ElasticIn },
            { "ElasticOut", Ease.ElasticOut },
            { "ElasticInOut", Ease.ElasticInOut },
            { "BounceIn", Ease.BounceIn },
            { "BounceOut", Ease.BounceOut },
            { "BounceInOut", Ease.BounceInOut },
        });
}
