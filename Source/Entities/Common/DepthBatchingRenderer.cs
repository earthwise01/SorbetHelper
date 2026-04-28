namespace Celeste.Mod.SorbetHelper.Entities;

public interface IDepthRenderable<TSelf, TRenderer, out TOptions>
    where TSelf : IDepthRenderable<TSelf, TRenderer, TOptions>
    where TRenderer : DepthBatchingRenderer<TRenderer, TSelf, TOptions>, new()
    where TOptions : IEquatable<TOptions>
{
    public TOptions GetRendererOptions();
    public bool GetVisible();
}

// generics :yum:
public abstract class DepthBatchingRenderer<TSelf, TRendered, TOptions> : Entity
    where TSelf : DepthBatchingRenderer<TSelf, TRendered, TOptions>, new()
    where TRendered : IDepthRenderable<TRendered, TSelf, TOptions>
    where TOptions : IEquatable<TOptions>
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(DepthBatchingRenderer<,,>)}";

    private readonly List<TRendered> allTracked = [];
    private ILookup<TOptions, TRendered> visibleGroups;

    protected IReadOnlyList<TRendered> AllTracked => allTracked;
    protected ILookup<TOptions, TRendered> VisibleGroups => visibleGroups;

    protected DepthBatchingRenderer()
    {
        Tag = Tags.Global;
        Add(new BeforeRenderHook(OnBeforeRender));
    }

    private void Track(TRendered toTrack) => allTracked.Add(toTrack);
    private void Untrack(TRendered toUntrack) => allTracked.Remove(toUntrack);

    private void OnBeforeRender()
    {
        // kinda hate doing thisevery framee but i feel like it does make more sense having the visible checks here rather than hidden away in each renderer
        // we're interating through the entire list >1 times a frame either way  it's just more obvious like this,
        visibleGroups = allTracked.Where(tracked => tracked.GetVisible())
                                  .ToLookup(tracked => tracked.GetRendererOptions());

        BeforeRender();
    }

    protected virtual void BeforeRender()
    {
        foreach (IGrouping<TOptions, TRendered> group in visibleGroups)
            GroupBeforeRender(group);
    }

    public override void Render()
    {
        foreach (IGrouping<TOptions, TRendered> group in visibleGroups)
            GroupRender(group);
    }

    protected virtual void GroupBeforeRender(IGrouping<TOptions, TRendered> group) { }
    protected virtual void GroupRender(IGrouping<TOptions, TRendered> group) { }

    private static TSelf GetOrCreateRenderer(Scene scene, int depth)
    {
        // can't automatically track generic types using an attribute (and i don't rly feel like using addtypetotracker)
        if (!scene.Tracker.Entities.TryGetValue(typeof(TSelf), out List<Entity> trackedRenderers))
            throw new InvalidOperationException($"{nameof(DepthBatchingRenderer<,,>)} type {typeof(TSelf).Name} is not tracked!");

        if (trackedRenderers.Concat(scene.Entities.ToAdd)
                            .FirstOrDefault(e => e is TSelf r && r.Depth == depth)
            is TSelf renderer)
            return renderer;

        scene.Add(renderer = new TSelf() { Depth = depth });
        Logger.Info(LogID, $"created new {typeof(TSelf).Name} with depth {depth}.");

        return renderer;
    }

    public static void Track(Scene scene, TRendered toTrack, int depth) => GetOrCreateRenderer(scene, depth).Track(toTrack);
    public static void Untrack(Scene scene, TRendered toUntrack, int depth) => GetOrCreateRenderer(scene, depth).Untrack(toUntrack);
}

// hmm^2
public interface IDepthRenderable<TSelf, TRenderer> : IDepthRenderable<TSelf, TRenderer, DepthBatchingRenderer<TRenderer, TSelf>.NoOptions>
    where TSelf : IDepthRenderable<TSelf, TRenderer, DepthBatchingRenderer<TRenderer, TSelf>.NoOptions>
    where TRenderer : DepthBatchingRenderer<TRenderer, TSelf>, new();
public abstract class DepthBatchingRenderer<TSelf, TRender>
    : DepthBatchingRenderer<TSelf, TRender, DepthBatchingRenderer<TSelf, TRender>.NoOptions>
    where TSelf : DepthBatchingRenderer<TSelf, TRender>, new()
    where TRender : IDepthRenderable<TRender, TSelf, DepthBatchingRenderer<TSelf, TRender>.NoOptions>
{
    public sealed record NoOptions { private NoOptions() { } }
}
