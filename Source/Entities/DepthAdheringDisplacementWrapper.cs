namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/DepthAdheringDisplacementWrapper")]
[Tracked]
public class DepthAdheringDisplacementWrapper : Entity
{
    private readonly bool distortBehind;
    private readonly bool ignoreBounds;

    public DepthAdheringDisplacementWrapper(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Collider = new Hitbox(data.Width, data.Height);
        distortBehind = data.Bool("distortBehind");
        ignoreBounds = data.Bool("ignoreBounds");

        EntityAwakeProcessor processor = new EntityAwakeProcessor(ProcessEntity)
            .WithDepthCheck(data.Int("minDepth", int.MinValue), data.Int("maxDepth", int.MaxValue));

        if (data.Attr("affectedTypes").Split(',', StringSplitOptions.TrimAndRemoveEmpty).ToHashSet() is { Count: > 0 } affectedTypes)
            processor.WithTypeNameCheck(affectedTypes);

        Add(processor);
    }

    private void ProcessEntity(Entity entity)
    {
        if (!ignoreBounds && !CollideCheckWithNullHitboxFallback(entity))
            return;

        DisplacementRenderHook[] displacementRenderHooks = entity.Components.GetAll<DisplacementRenderHook>().ToArray();
        if (displacementRenderHooks.Length == 0)
            return;

        Action renderDisplacement = null;
        foreach (DisplacementRenderHook displacementRenderHook in displacementRenderHooks)
        {
            renderDisplacement += displacementRenderHook.RenderDisplacement;
            displacementRenderHook.RemoveSelf();
        }

        entity.Add(new DepthAdheringDisplacementRenderHook(entity.Render, renderDisplacement, distortBehind));
    }

    private bool CollideCheckWithNullHitboxFallback(Entity entity)
    {
        if (entity.Collider is { } entityCollider)
            return entityCollider.Collide(Collider);

        // if the entity doesn't have a collider (since waterfalls unfortunately don't while also being one of the main things you'd want to affect)
        // try and make a fallback collider so that it's still possible to give them depth adhering displacement
        float tempWidth = 8f;
        float tempHeight = 8f;

        // if the entity has fields called "width" or "height" try to use those for the temporary hitbox dimensions instead of the default of 8px
        DynamicData dynamicData = DynamicData.For(entity);
        if (dynamicData.Get("width") is float width)
            tempWidth = width;
        if (dynamicData.Get("height") is float height)
            tempHeight = height;

        return CollideRect(new Rectangle((int)entity.X, (int)entity.Y, (int)tempWidth, (int)tempHeight));
    }
}
