namespace Celeste.Mod.SorbetHelper.Entities;

public abstract class Renderer<TSelf> : Entity
    where TSelf : Renderer<TSelf>, new()
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(Renderer<>)}";

    protected Renderer()
    {
        Tag = Tags.Global;
    }

    public static TSelf GetOrCreateRenderer(Scene scene)
    {
        if (!scene.Tracker.Entities.TryGetValue(typeof(TSelf), out List<Entity> trackedRenderers))
            throw new InvalidOperationException($"{nameof(Renderer<>)} type {typeof(TSelf).Name} is not tracked!");

        if (trackedRenderers.Concat(scene.Entities.ToAdd)
                            .FirstOrDefault(e => e is TSelf r)
            is TSelf renderer)
            return renderer;

        scene.Add(renderer = new TSelf());
        Logger.Info(LogID, $"created new {typeof(TSelf).Name}.");

        return renderer;
    }
}
