namespace Celeste.Mod.SorbetHelper.Entities;

/// <summary>
/// Marks entities with the specified type names with <see cref="EntityStylegroundMarker"/> components to be rendered by an <see cref="EntityStylegroundRenderer"/><br/>
/// </summary>
[CustomEntity("SorbetHelper/EntityStylegroundController")]
[GlobalEntity(onlyGlobalIf: "global")]
public class EntityStylegroundController : EntityProcessingController
{
    private readonly string stylegroundTag;

    public EntityStylegroundController(EntityData data, Vector2 offset)
        : base(data, offset, data.Bool("global", false) ? ProcessModes.OnEntityAwake : ProcessModes.OnProcessorAwake)
    {
        AffectedTypes = data.Attr("classNames").Split(',', StringSplitOptions.TrimAndRemoveEmpty).ToHashSet();
        MinDepth = data.Int("minDepth", int.MinValue);
        MaxDepth = data.Int("maxDepth", int.MaxValue);

        stylegroundTag = data.Attr("tag", "");
    }

    protected override void ProcessEntity(Entity entity)
    {
        if (entity.Get<EntityStylegroundMarker>() is null)
            entity.Add(new EntityStylegroundMarker(stylegroundTag));
    }
}
