using System.Linq;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity(EntityDataID + "Global")] // global version is swapped to in mapdataprocessor based on data.Bool("global")
[CustomEntity(EntityDataID, EntityDataID + "Global")]
public class LightCoverController : Entity
{
    public const string EntityDataID = "SorbetHelper/LightCoverController";

    private readonly float alpha;

    public LightCoverController(EntityData data, Vector2 _)
    {
        alpha = data.Float("alpha", 1f);

        Add(new EntityAwakeProcessor(ProcessEntity, data.Bool("global", false) ? EntityAwakeProcessor.ProcessModes.OnEntityAwake : EntityAwakeProcessor.ProcessModes.OnProcessorAwake)
            .WithTypeNameCheck(data.Attr("classNames").Split(',', StringSplitOptions.TrimAndRemoveEmpty).ToHashSet())
            .WithDepthCheck(data.Int("minDepth", int.MinValue), data.Int("maxDepth", int.MaxValue)));
    }

    private void ProcessEntity(Entity entity)
    {
        if (entity.Get<LightCover>() is null)
            entity.Add(new LightCover(alpha));
    }
}
