using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
[GlobalEntity(              EntityDataID + "Global")] // global version is swapped to in mapdataprocessor based on data.Bool("global")
[CustomEntity(EntityDataID, EntityDataID + "Global")]
public class DarknessTransparencyFixController : Entity {
    public const string EntityDataID = "SorbetHelper/DarknessTransparencyFixController";

    #region Hooks

    internal static void Load() {
        // guarantee hook order with style mask helper
        using (new DetourConfigContext(new DetourConfig("SorbetHelper", before: ["StyleMaskHelper"])).Use())
            On.Celeste.LightingRenderer.Render += On_LightingRenderer_Render;
    }

    internal static void Unload() {
        On.Celeste.LightingRenderer.Render -= On_LightingRenderer_Render;
    }

    // kinda bleh on modifying a static field in a hook like this but  oh well (i love style mask helper compat :tada:)
    private static void On_LightingRenderer_Render(On.Celeste.LightingRenderer.orig_Render orig, LightingRenderer self, Scene scene) {
        if (scene.Tracker.GetEntity<DarknessTransparencyFixController>() is not null) {
            // use ColorSourceBlend = Blend.DestinationAlpha instead so that darkness behaves correctly with premultiplied alpha
            GFX.DestinationTransparencySubtract.ColorSourceBlend = Blend.DestinationAlpha;
            orig(self, scene);
            GFX.DestinationTransparencySubtract.ColorSourceBlend = Blend.One;
        } else {
            orig(self, scene);
        }
    }

    #endregion
}
