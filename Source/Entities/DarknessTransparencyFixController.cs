using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
[GlobalEntity(              EntityDataID + "Global")] // global version is swapped to in mapdataprocessor based on data.Bool("global")
[CustomEntity(EntityDataID, EntityDataID + "Global")]
public class DarknessTransparencyFixController : Entity {
    public const string EntityDataID = "SorbetHelper/DarknessTransparencyFixController";

    public static readonly BlendState DestinationTransparencySubtractFixed = new BlendState {
        // certified one line fix :white_check_mark: i love premultiplied alpha
        ColorSourceBlend = Blend.DestinationAlpha, // Blend.One,
        ColorDestinationBlend = Blend.One,
        ColorBlendFunction = BlendFunction.ReverseSubtract,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add
    };

    internal static void Load() {
        IL.Celeste.LightingRenderer.Render += IL_LightingRenderer_Render;
    }

    internal static void Unload() {
        IL.Celeste.LightingRenderer.Render -= IL_LightingRenderer_Render;
    }

    // doesn't work with lighting masks because stylemask helper is evil and hates me grr
    private static void IL_LightingRenderer_Render(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(typeof(GFX), nameof(GFX.DestinationTransparencySubtract))))
            throw new IndexOutOfRangeException("Unable to find GFX.DestinationTransparencySubtract to replace with premultiplied alpha compatible version!");

        cursor.EmitLdarg1();
        cursor.EmitDelegate(useFixedDestinationTransparencySubtract);

        static BlendState useFixedDestinationTransparencySubtract(BlendState orig, Scene scene) {
            if (scene.Tracker.GetEntity<DarknessTransparencyFixController>() is null)
                return orig;

            return DestinationTransparencySubtractFixed;
        }
    }
}