using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Cil;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
[CustomEntity("SorbetHelper/DisplacementEffectBlocker")]
public class DisplacementEffectBlocker : Entity {
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(DisplacementEffectBlocker)}";

    public readonly bool DepthAdhering;
    public readonly bool WaterOnly;

    private readonly string flag;
    private readonly bool invertFlag;

    public static readonly Color NoDisplacementColor = new Color(0.5f, 0.5f, 0.0f, 1.0f);
    public static readonly Color NoWaterDisplacementMultColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);
    public static readonly BlendState WaterDisplacementBlockerBlendState = new BlendState {
        Name = "DisplacementEffectBlocker.WaterDisplacementBlocker",
        ColorSourceBlend = Blend.Zero,
        AlphaSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.SourceColor,
        AlphaDestinationBlend = Blend.One
    };

    public DisplacementEffectBlocker(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Collider = new Hitbox(data.Width, data.Height);
        DepthAdhering = data.Bool("depthAdhering", false);
        WaterOnly = data.Bool("waterOnly", false);
        Depth = data.Int("depth", 0);

        flag = data.Attr("flag", "");
        if (flag.StartsWith('!')) {
            invertFlag = true;
            flag = flag.Substring(1);
        }
    }

    public override void Update() {
        base.Update();

        if (!string.IsNullOrEmpty(flag))
            Visible = SceneAs<Level>().Session.GetFlag(flag, invertFlag);
    }

    #region Hooks

    internal static void Load() {
        IL.Celeste.DisplacementRenderer.BeforeRender += IL_DisplacementRenderer_BeforeRender;
    }

    internal static void Unload() {
        IL.Celeste.DisplacementRenderer.BeforeRender -= IL_DisplacementRenderer_BeforeRender;
    }

    private static void IL_DisplacementRenderer_BeforeRender(ILContext il) {
        ILCursor cursor = new ILCursor(il) {
            Index = -1
        };

        if (!cursor.TryGotoPrev(MoveType.Before, instr => instr.MatchCallvirt<SpriteBatch>("End"))) {
            Logger.Error(LogID, $"failed to inject check for full DisplacementEffectBlockers in CIL code for {cursor.Method.Name}!");
            return;
        }

        Logger.Verbose(LogID, $"injecting check for full DisplacementEffectBlockers at {cursor.Index} in CIL code for {cursor.Method.Name}");
        cursor.EmitLdarg1();
        cursor.EmitDelegate(RenderFullBlockers);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<SpriteBatch>("End"))) {
            Logger.Error(LogID, $"failed to inject check for water DisplacementEffectBlockers in CIL code for {cursor.Method.Name}!");
            return;
        }

        Logger.Verbose(LogID, $"injecting check for water DisplacementEffectBlockers at {cursor.Index} in CIL code for {cursor.Method.Name}");
        cursor.EmitLdarg1();
        cursor.EmitDelegate(RenderWaterBlockers);

        return;

        static void RenderFullBlockers(Scene scene) {
            foreach (Entity entity in scene.Tracker.GetEntities<DisplacementEffectBlocker>()
                                                   .Where(entity => entity is DisplacementEffectBlocker { Visible: true, DepthAdhering: false, WaterOnly: false })) {
                Draw.Rect(entity.Position, entity.Width, entity.Height, NoDisplacementColor);
            }
        }

        static void RenderWaterBlockers(Scene scene) {
            List<Entity> waterBlockers = scene.Tracker.GetEntities<DisplacementEffectBlocker>()
                                                      .Where(entity => entity is DisplacementEffectBlocker { Visible: true, DepthAdhering: false, WaterOnly: true })
                                                      .ToList();

            if (waterBlockers.Count <= 0)
                return;

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, WaterDisplacementBlockerBlendState, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, (scene as Level)!.Camera.Matrix);

            foreach (Entity entity in waterBlockers)
                Draw.Rect(entity.Position, entity.Width, entity.Height, NoWaterDisplacementMultColor);

            Draw.SpriteBatch.End();
        }
    }

    #endregion

}
