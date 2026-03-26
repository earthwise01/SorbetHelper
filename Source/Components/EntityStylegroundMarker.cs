using Celeste.Mod.SorbetHelper.Backdrops;

namespace Celeste.Mod.SorbetHelper.Components;

/// <summary>
/// Marks an entity to be rendered by an <see cref="EntityStylegroundRenderer"/> backdrop.<br/>
/// </summary>
[Tracked]
public class EntityStylegroundMarker : Component
{
    public readonly string Tag;

    private readonly VisibleOverride visibleOverride;
    public bool EntityVisible => visibleOverride?.EntityVisible != false;

    public EntityStylegroundMarker(string tag, bool respectVisible) : base(false, false)
    {
        Tag = tag;

        if (respectVisible)
            visibleOverride = new VisibleOverride();
    }

    public EntityStylegroundMarker(string tag) : this(tag, true) { }

    public override void Added(Entity entity)
    {
        base.Added(entity);

        if (string.IsNullOrEmpty(Tag))
        {
            RemoveSelf();
            return;
        }

        if (visibleOverride is not null)
            entity.Add(visibleOverride);
        else
            entity.Visible = false;

        // todo: this used to support entities with displacement render hooks
    }

    public override void Removed(Entity entity)
    {
        if (visibleOverride is not null)
            entity.Remove(visibleOverride);

        base.Removed(entity);
    }
}
