namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/DarknessTransparencyFixController")]
[GlobalEntity(onlyGlobalIf: "global")]
[Tracked]
public class DarknessTransparencyFixController : Entity
{
    private static readonly BlendState DestinationTransparencySubtractAlphaFixed = new BlendState()
    {
        // use ColorSourceBlend = Blend.DestinationAlpha instead so that darkness behaves correctly with premultiplied alpha
        ColorSourceBlend = Blend.DestinationAlpha, // Blend.One,
        ColorDestinationBlend = Blend.One,
        ColorBlendFunction = BlendFunction.ReverseSubtract,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add
    };

    #region Hooks

    [OnLoad]
    internal static void Load()
    {
        On.Celeste.Level.Render += On_Level_Render;
    }

    [OnUnload]
    internal static void Unload()
    {
        On.Celeste.Level.Render -= On_Level_Render;
    }

    // we hook Level.Render instead of LightingRenderer.Render since we can't guarantee hook order with StyleMaskHelper for the latter (i love mod compat :tada:)
    private static void On_Level_Render(On.Celeste.Level.orig_Render orig, Level self)
    {
        if (self.Tracker.GetEntity<DarknessTransparencyFixController>() is not null)
        {
            using (new SetTemporaryValue<BlendState>(ref GFX.DestinationTransparencySubtract, DestinationTransparencySubtractAlphaFixed))
                orig(self);
        }
        else
        {
            orig(self);
        }
    }

    #endregion
}
