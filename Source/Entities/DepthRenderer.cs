namespace Celeste.Mod.SorbetHelper.Entities;

public interface IDepthRendered<out TOptions>
    where TOptions : IEquatable<TOptions>
{
    public TOptions GetRendererOptions();
    public bool GetVisible();
}

// generics :yum:
public abstract class DepthRenderer<TSelf, TRender, TOptions> : Entity
    where TSelf : DepthRenderer<TSelf, TRender, TOptions>, new()
    where TRender : IDepthRendered<TOptions>
    where TOptions : IEquatable<TOptions>
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(DepthRenderer<,,>)}";

    private readonly List<TRender> allTracked = [];
    private ILookup<TOptions, TRender> visibleGroups;

    protected IReadOnlyList<TRender> AllTracked => allTracked;
    protected ILookup<TOptions, TRender> VisibleGroups => visibleGroups;

    protected DepthRenderer()
    {
        Tag = Tags.Global;
        Add(new BeforeRenderHook(OnBeforeRender));
    }

    public void Track(TRender toTrack) => allTracked.Add(toTrack);
    public void Untrack(TRender toUntrack) => allTracked.Remove(toUntrack);

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
        foreach (IGrouping<TOptions, TRender> group in visibleGroups)
            GroupBeforeRender(group);
    }

    public override void Render()
    {
        foreach (IGrouping<TOptions, TRender> group in visibleGroups)
            GroupRender(group);
    }

    protected virtual void GroupBeforeRender(IGrouping<TOptions, TRender> group) { }
    protected virtual void GroupRender(IGrouping<TOptions, TRender> group) { }

    // wehh the world if u could call static method on generic parameters & calling methods on an implemented interface didnt require 5000 casts
    public static TSelf GetRenderer(Scene scene, int depth)
    {
        // can't automatically track generic types using an attribute (and i don't rly feel like using addtypetotracker)
        if (!scene.Tracker.Entities.TryGetValue(typeof(TSelf), out List<Entity> trackedRenderers))
            throw new InvalidOperationException($"{nameof(DepthRenderer<,,>)} type {typeof(TSelf).Name} is not tracked!");

        if (trackedRenderers.Concat(scene.Entities.ToAdd)
                            .FirstOrDefault(e => e is TSelf r && r.Depth == depth)
            is TSelf renderer)
            return renderer;

        scene.Add(renderer = new TSelf() { Depth = depth });
        Logger.Info(LogID, $"created new {typeof(TSelf).Name} with depth {depth}.");

        return renderer;
    }
}

// hmm
public abstract class DepthRenderer<TSelf, TRender> : DepthRenderer<TSelf, TRender, DepthRenderer<TSelf, TRender>.NoOptions>
    where TSelf : DepthRenderer<TSelf, TRender>, new()
    where TRender : IDepthRendered<DepthRenderer<TSelf, TRender>.NoOptions>
{
    public sealed record NoOptions { private NoOptions() { } }
}
