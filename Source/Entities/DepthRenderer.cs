namespace Celeste.Mod.SorbetHelper.Entities;

// generics :yum:
public abstract class DepthRenderer<TSelf, TTrack, TOptions> : Entity
    where TSelf : DepthRenderer<TSelf, TTrack, TOptions>, new()
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(DepthRenderer<,,>)}";

    protected abstract TOptions Options { init; }

    // hmm
    protected abstract bool OptionsEquals(TOptions options);
    protected virtual string OptionsToString(TOptions options) => options.ToString();

    private readonly List<TTrack> tracked = [];
    protected IReadOnlyList<TTrack> Tracked => tracked;

    protected DepthRenderer()
    {
        Tag = Tags.Global;
    }

    public void Track(TTrack toTrack) => tracked.Add(toTrack);
    public void Untrack(TTrack toUntrack) => tracked.Remove(toUntrack);

    public static TSelf GetRenderer(Scene scene, int depth, TOptions options)
    {
        // can't automatically track generic types weh
        if (!scene.Tracker.Entities.TryGetValue(typeof(TSelf), out List<Entity> trackedRenderers))
            throw new InvalidOperationException($"{nameof(DepthRenderer<,,>)} type {typeof(TSelf).Name} is not tracked!");

        if (trackedRenderers.Concat(scene.Entities.ToAdd)
                            .FirstOrDefault(e => e is TSelf r && r.Depth == depth && r.OptionsEquals(options))
            is TSelf renderer)
            return renderer;

        scene.Add(renderer = new TSelf() { Depth = depth, Options = options });
        Logger.Info(LogID, $"created new {typeof(TSelf).Name} with depth {depth}{(options is not null ? $" and options {renderer.OptionsToString(options)}." : ".")}");

        return renderer;
    }
}

// hmm
public abstract class DepthRenderer<TSelf, TTracked> : DepthRenderer<TSelf, TTracked, DepthRenderer<TSelf, TTracked>.NoOptions>
    where TSelf : DepthRenderer<TSelf, TTracked>, new()
{
    public sealed class NoOptions { private NoOptions() { } }

    protected sealed override NoOptions Options { init { } }
    protected sealed override bool OptionsEquals(NoOptions options) => true;
    protected sealed override string OptionsToString(NoOptions options) => throw new InvalidOperationException();

    public static TSelf GetRenderer(Scene scene, int depth) => GetRenderer(scene, depth, null);
}
