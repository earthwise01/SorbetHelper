namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/WaterInteractionController")]
[GlobalEntity(globalAttributeName: "global")]
public class WaterInteractionController : EntityProcessingController
{
    public WaterInteractionController(EntityData data, Vector2 offset) : base(data, offset)
    {
        AffectedTypes = data.Attr("affectedTypes").Split(',', StringSplitOptions.TrimAndRemoveEmpty).ToHashSet();
    }

    protected override void ProcessEntity(Entity entity)
    {
        if (entity.Get<WaterInteraction>() is null)
            entity.Add(new WaterInteraction(() => false));
    }
}
