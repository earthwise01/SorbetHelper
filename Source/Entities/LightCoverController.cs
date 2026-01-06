using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity(              EntityDataID + "Global")] // global version is swapped to in mapdataprocessor based on data.Bool("global")
[CustomEntity(EntityDataID, EntityDataID + "Global")]
public class LightCoverController : Entity {
    public const string EntityDataID = "SorbetHelper/LightCoverController";

    private readonly float alpha;

    public LightCoverController(EntityData data, Vector2 _) {
        alpha = data.Float("alpha", 1f);

        if (data.Bool("global", false))
            Add(new GlobalTypeNameProcessor(data, ProcessEntity));
        else
            Add(new TypeNameProcessor(data, ProcessEntity));
    }

    private void ProcessEntity(Entity entity) {
        if (entity.Get<LightCover>() is null)
            entity.Add(new LightCover(alpha));
    }
}
