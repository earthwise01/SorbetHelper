namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/SparklingWaterColorController")]
[GlobalEntity(onlyGlobalIf: "global")]
[Tracked]
public class SparklingWaterColorController(EntityData data, Vector2 offset) : EntityProcessingController(data, offset)
{
    private readonly SparklingWaterRenderer.Options options = new SparklingWaterRenderer.Options(data);
    private readonly int? affectedDepth = data.Nullable<int>("affectedDepth");

    // prioritise controllers that specify a depth
    protected override int ProcessPriority => affectedDepth is not null ? -1 : 0;

    protected override void ProcessEntity(Entity entity)
    {
        if (entity is SparklingWater { RendererOptions: null } sparklingWater
            && (affectedDepth == sparklingWater.Depth || affectedDepth is null))
            sparklingWater.RendererOptions = options;
    }
}
