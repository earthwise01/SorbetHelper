namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/DisplacementEffectBlocker")]
[Tracked]
public class DisplacementEffectAreaBlocker : Entity
{
    public bool DepthAdhering { get; }
    public bool WaterOnly { get; }

    private readonly Condition condition;

    public static readonly Color NoDisplacementColor = new(0.5f, 0.5f, 0.0f, 1.0f);

    public static readonly BlendState WaterChannelOnlyBlendState = new()
    {
        Name = "DisplacementEffectAreaBlocker.WaterChannelOnly",
        ColorSourceBlend = Blend.One,
        AlphaSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaDestinationBlend = Blend.InverseSourceAlpha,
        ColorWriteChannels = ColorWriteChannels.Blue // water displacement uses the blue channel
    };

    public DisplacementEffectAreaBlocker(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Collider = new Hitbox(data.Width, data.Height);
        DepthAdhering = data.Bool("depthAdhering", false);
        WaterOnly = data.Bool("waterOnly", false);
        Depth = data.Int("depth", 0);

        condition = data.Condition("flag");
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        if (condition is not Condition.None)
            Visible = condition.Check(SceneAs<Level>().Session);
    }

    public override void Update()
    {
        base.Update();

        if (condition is not Condition.None)
            Visible = condition.Check(SceneAs<Level>().Session);
    }

    #region Hooks

    [OnLoad]
    internal static void Load()
    {
        IL.Celeste.DisplacementRenderer.BeforeRender += IL_DisplacementRenderer_BeforeRender;
    }

    [OnUnload]
    internal static void Unload()
    {
        IL.Celeste.DisplacementRenderer.BeforeRender -= IL_DisplacementRenderer_BeforeRender;
    }

    private static void IL_DisplacementRenderer_BeforeRender(ILContext il)
    {
        ILCursor cursor = new(il)
        {
            Index = -1
        };

        VariableDefinition waterBlockersVariable = cursor.AddVariable<List<DisplacementEffectAreaBlocker>>();

        if (!cursor.TryGotoPrev(MoveType.Before, instr => instr.MatchCallvirt<SpriteBatch>("End")))
            throw new HookHelper.HookException(il, "Unable to find `SpriteBatch.End` call to insert full displacement blocking before.");

        cursor.EmitLdarg1();
        cursor.EmitLdloca(waterBlockersVariable);
        cursor.EmitDelegate(RenderFullBlockers);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<SpriteBatch>("End")))
            throw new HookHelper.HookException(il, "Unable to find `SpriteBatch.End` call to insert water displacement blocking after.");

        cursor.EmitLdarg1();
        cursor.EmitLdloc(waterBlockersVariable);
        cursor.EmitDelegate(RenderWaterBlockers);

        return;

        static void RenderFullBlockers(Scene scene, out List<DisplacementEffectAreaBlocker> waterBlockers)
        {
            waterBlockers = [];
            foreach (Entity entity in scene.Tracker.GetEntities<DisplacementEffectAreaBlocker>())
            {
                if (entity is not DisplacementEffectAreaBlocker { Visible: true, DepthAdhering: false } blocker)
                    continue;

                if (!blocker.WaterOnly)
                    Draw.Rect(blocker.Position, blocker.Width, blocker.Height, NoDisplacementColor);
                else
                    waterBlockers.Add(blocker);
            }
        }

        static void RenderWaterBlockers(Scene scene, List<DisplacementEffectAreaBlocker> waterBlockers)
        {
            if (waterBlockers.Count <= 0)
                return;

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, WaterChannelOnlyBlendState, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, (scene as Level)!.Camera.Matrix);
            foreach (DisplacementEffectAreaBlocker blocker in waterBlockers)
                Draw.Rect(blocker.Position, blocker.Width, blocker.Height, NoDisplacementColor);
            Draw.SpriteBatch.End();
        }
    }

    #endregion
}
