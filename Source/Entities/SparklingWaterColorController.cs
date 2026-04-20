namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity(           EntitySID + "Global")]
[CustomEntity(EntitySID, EntitySID + "Global")]
[Tracked]
public class SparklingWaterColorController(EntityData data, Vector2 offset) : Entity(data.Position + offset)
{
    public const string EntitySID = "SorbetHelper/SparklingWaterColorController";

    public readonly SparklingWaterRenderer.Settings Settings = new SparklingWaterRenderer.Settings(data);
    public readonly int? AffectedDepth = data.Nullable<int>("affectedDepth");

    public static SparklingWaterColorController GetController(Scene scene, int depth)
    {
        SparklingWaterColorController allDepthsController = null;
        foreach (SparklingWaterColorController controller in scene.Tracker.GetEntities<SparklingWaterColorController>())
        {
            if (controller.AffectedDepth == depth)
                return controller;

            if (controller.AffectedDepth is null && allDepthsController is null)
                allDepthsController = controller;
        }

        return allDepthsController;
    }
}
