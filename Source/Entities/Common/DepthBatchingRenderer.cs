namespace Celeste.Mod.SorbetHelper.Entities;

// generics :yum:
public abstract class DepthBatchingRenderer<TSelf, TRendered, TOptions> : Entity
    where TSelf : DepthBatchingRenderer<TSelf, TRendered, TOptions>, new()
    where TRendered : DepthBatchingRenderer<TSelf, TRendered, TOptions>.IRenderable
    where TOptions : IEquatable<TOptions>
{
    public interface IRenderable
    {
        public TOptions GetRendererOptions();
        public bool GetVisible();
    }

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
public abstract class DepthBatchingRenderer<TSelf, TRendered>
    : DepthBatchingRenderer<TSelf, TRendered, DepthBatchingRenderer<TSelf, TRendered>.NoOptions>
    where TSelf : DepthBatchingRenderer<TSelf, TRendered>, new()
    where TRendered : DepthBatchingRenderer<TSelf, TRendered>.IRenderable
{
    public sealed record NoOptions { private NoOptions() { } }
}
