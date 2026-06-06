namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/DisplacementEffectArea")]
public class DisplacementEffectArea : Entity
{
    private readonly Color color;

    private readonly Condition condition;

    public DisplacementEffectArea(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Collider = new Hitbox(data.Width, data.Height);

        // remap the values from the "more friendly" -1 to 1 range to the actual expected 0 to 1 range (where 0.5 is no displacement)
        float horizontalDisplacement = 1f - (data.Float("horizontalDisplacement", 0.0f) + 1f) / 2f;
        float verticalDisplacement = 1f - (data.Float("verticalDisplacement", 0.0f) + 1f) / 2f;
        float waterDisplacement = data.Float("waterDisplacement", 0.25f);
        float alpha = data.Float("alpha", 1.0f);
        color = new Color(horizontalDisplacement, verticalDisplacement, waterDisplacement) * alpha;

        condition = data.Condition("flag");

        if (data.Bool("depthAdhering", false))
        {
            Depth = data.Int("depth", 0);
            Add(new DepthAdheringDisplacementRenderHook(() => { }, RenderDisplacement, true, false));
        }
        else
        {
            Add(new DisplacementRenderHook(RenderDisplacement));
        }
    }

    private void RenderDisplacement()
    {
        if (condition.Check(SceneAs<Level>().Session))
            Draw.Rect(Position, Width, Height, color);
    }
}
