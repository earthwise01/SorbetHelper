using System.Linq;
using Celeste.Mod.SorbetHelper.Backdrops;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

/// <summary>
/// Marks entities with the specified type names with <see cref="EntityStylegroundMarker"/> components to be rendered by an <see cref="EntityStylegroundRenderer"/><br/>
/// </summary>
[GlobalEntity(              EntityDataID + "Global")] // global version is swapped to in mapdataprocessor based on data.Bool("global")
[CustomEntity(EntityDataID, EntityDataID + "Global")]
public class EntityStylegroundController : Entity {
    public const string EntityDataID = "SorbetHelper/EntityStylegroundController";

    private readonly string stylegroundTag;

    public EntityStylegroundController(EntityData data, Vector2 _) {
        stylegroundTag = data.Attr("tag", "");

        Add(new EntityAwakeProcessor(ProcessEntity, data.Bool("global", false) ? EntityAwakeProcessor.ProcessModes.OnEntityAwake : EntityAwakeProcessor.ProcessModes.OnProcessorAwake)
            .WithTypeNameCheck(data.Attr("classNames").Split(',', StringSplitOptions.TrimAndRemoveEmpty).ToHashSet())
            .WithDepthCheck(data.Int("minDepth", int.MinValue), data.Int("maxDepth", int.MaxValue)));
    }

    private void ProcessEntity(Entity entity) {
        if (entity.Get<EntityStylegroundMarker>() is null)
            entity.Add(new EntityStylegroundMarker(stylegroundTag));
    }
}
