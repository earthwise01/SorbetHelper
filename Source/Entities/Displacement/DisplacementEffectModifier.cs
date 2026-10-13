namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/DisplacementEffectModifier", "SorbetHelper/DepthAdheringDisplacementWrapper")]
public class DisplacementEffectModifier : EntityProcessingController
{
    private enum ModificationModes
    {
        AtEntityDepth,
        EntityOnly,
        Remove
    }

    private readonly ModificationModes mode;
    private readonly bool ignoreBounds;

    public DisplacementEffectModifier(EntityData data, Vector2 offset) : base(data, offset)
    {
        if (data.Attr("affectedTypes").Split(',', StringSplitOptions.TrimAndRemoveEmpty).ToHashSet() is { Count: > 0 } affectedTypes)
            AffectedTypes = affectedTypes;
        MinDepth = data.Int("minDepth", int.MinValue);
        MaxDepth = data.Int("maxDepth", int.MaxValue);

        mode = data.Nullable<bool>("distortBehind") is { } distortBehind
            ? distortBehind
                ? ModificationModes.AtEntityDepth
                : ModificationModes.EntityOnly
            : data.Enum("mode", ModificationModes.AtEntityDepth);
        ignoreBounds = data.Bool("ignoreBounds");
    }

    protected override bool ControllerContains(Entity entity)
        => ignoreBounds || CollideCheckWithNullHitboxFallback(entity);

    protected override void ProcessEntity(Entity entity)
    {
        DisplacementRenderHook[] displacementRenderHooks = entity.Components.GetAll<DisplacementRenderHook>().ToArray();
        if (displacementRenderHooks.Length == 0)
            return;

        switch (mode)
        {
            case ModificationModes.AtEntityDepth or ModificationModes.EntityOnly:
                Action renderDisplacement = null;
                foreach (DisplacementRenderHook displacementRenderHook in displacementRenderHooks)
                {
                    renderDisplacement += displacementRenderHook.RenderDisplacement;
                    displacementRenderHook.RemoveSelf();
                }

                bool distortBehind = mode == ModificationModes.AtEntityDepth;
                entity.Add(new DepthAdheringDisplacementRenderHook(entity.Render, renderDisplacement, distortBehind));
                break;

            case ModificationModes.Remove:
                foreach (DisplacementRenderHook displacementRenderHook in displacementRenderHooks)
                    displacementRenderHook.RemoveSelf();
                break;
        }
    }

    private bool CollideCheckWithNullHitboxFallback(Entity entity)
    {
        if (entity.Collider is { } entityCollider)
            return entityCollider.Collide(Collider);

        // if the entity doesn't have a collider (waterfalls unfortunately don't while also being one of the main things you'd want to affect)
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
