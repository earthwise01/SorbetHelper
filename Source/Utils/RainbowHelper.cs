namespace Celeste.Mod.SorbetHelper.Utils;

// based on frost helper https://github.com/JaThePlayer/FrostHelper/blob/master/Code/FrostHelper/Helpers/ColorHelper.cs#L105
// licensed under the MIT License https://github.com/JaThePlayer/FrostHelper/blob/master/LICENSE

internal static class RainbowHelper
{
    private static CrystalStaticSpinner rainbowSpinner;

    public static void SetGetHueScene(Scene scene)
    {
        rainbowSpinner ??= new CrystalStaticSpinner(Vector2.Zero, false, CrystalColor.Rainbow);
        rainbowSpinner.Scene = scene;
    }

    /// <summary>
    /// Make sure to call <see cref="SetGetHueScene"/> beforehand!
    /// </summary>
    public static Color GetHue(Vector2 position)
        => rainbowSpinner.GetHue(position);

    public static Color GetHue(Scene scene, Vector2 position)
    {
        rainbowSpinner ??= new CrystalStaticSpinner(Vector2.Zero, false, CrystalColor.Rainbow);
        rainbowSpinner.Scene = scene;
        return rainbowSpinner.GetHue(position);
    }

    #region Hooks

    [OnLoad]
    internal static void Load()
    {
        Everest.Events.Celeste.OnSceneTransition += Event_OnSceneTransition;
    }

    [OnUnload]
    internal static void Unload()
    {
        Everest.Events.Celeste.OnSceneTransition -= Event_OnSceneTransition;
    }

    private static void Event_OnSceneTransition(Scene last, Scene next)
        => rainbowSpinner?.Scene = null;

    #endregion
}
