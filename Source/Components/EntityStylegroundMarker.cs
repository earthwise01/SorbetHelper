namespace Celeste.Mod.SorbetHelper.Components;

/// <summary>
/// Marks an entity to be rendered by an EntityStylegroundRenderer backdrop.<br/>
/// See also Backdrops.EntityStylegroundRenderer and Entities.EntityStylegroundController.
/// </summary>
[Tracked]
public class EntityStylegroundMarker : Component {
    public readonly string Tag;
    // public Action EntityRender { get; private set; }

    private readonly VisibleOverride visibleOverride;
    public bool EntityVisible => visibleOverride?.EntityVisible != false;

    public EntityStylegroundMarker(string tag, bool respectVisible) : base(false, false) {
        Tag = tag;

        if (respectVisible)
            visibleOverride = new VisibleOverride();
    }

    public EntityStylegroundMarker(string tag) : this(tag, true) { }

    public override void Added(Entity entity) {
        base.Added(entity);

        if (string.IsNullOrEmpty(Tag)) {
            RemoveSelf();
            return;
        }

        if (visibleOverride is not null)
            entity.Add(visibleOverride);
        else
            entity.Visible = false;

        // hmm i feel kind of bad removing support for this but it did rely pretty heavily on depth displacement not being depth batched
        // hopefully no one was using this yet

        // extremelyy niche; mostly for fg waterfalls
        // var depthDisplacement = entity.Get<DepthAdheringDisplacementRenderHook>();
        // if (depthDisplacement is null && entity.Get<DisplacementRenderHook>() is { } displacement) {
        //     depthDisplacement = new DepthAdheringDisplacementRenderHook(entity.Render, displacement.RenderDisplacement, true) {
        //         Visible = false
        //     };
        //     entity.Remove(displacement);
        //     entity.Add(depthDisplacement);
        // }

        // if (depthDisplacement is null)
        //     EntityRender = entity.Render;
        // else
        //     EntityRender = depthDisplacement.EntityRenderOverride;
    }

    public override void Removed(Entity entity) {
        if (visibleOverride is not null)
            entity.Remove(visibleOverride);

        base.Removed(entity);
    }
}
