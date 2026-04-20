namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity(           EntitySID + "Global")] // global version is swapped to in mapdataprocessor based on data.Bool("global")
[CustomEntity(EntitySID, EntitySID + "Global")]
public class LightCoverController : EntityProcessingController
{
    public const string EntitySID = "SorbetHelper/LightCoverController";

    private readonly float alpha;

    public LightCoverController(EntityData data, Vector2 offset)
        : base(data, offset, data.Bool("global", false) ? ProcessModes.OnEntityAwake : ProcessModes.OnProcessorAwake)
    {
        AffectedTypes = data.Attr("classNames").Split(',', StringSplitOptions.TrimAndRemoveEmpty).ToHashSet();
        MinDepth = data.Int("minDepth", int.MinValue);
        MaxDepth = data.Int("maxDepth", int.MaxValue);

        alpha = data.Float("alpha", 1f);
    }

    protected override void ProcessEntity(Entity entity)
    {
        if (entity.Get<LightCover>() is null)
            entity.Add(new LightCover(alpha));
    }
}
