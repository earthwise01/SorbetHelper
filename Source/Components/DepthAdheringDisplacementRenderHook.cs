namespace Celeste.Mod.SorbetHelper.Components;

[Tracked]
public class DepthAdheringDisplacementRenderHook : Component, IDepthRendered<bool>
{
    public readonly Action RenderEntity;
    public readonly Action RenderDisplacement;

    private readonly bool distortBehind;

    private readonly VisibleOverride visibleOverride;
    public bool EntityVisible => visibleOverride?.EntityVisible != false;

    public DepthAdheringDisplacementRenderHook(Action renderEntity, Action renderDisplacement, bool distortBehind, bool respectVisible)
        : base(false, true)
    {
        RenderEntity = renderEntity;
        RenderDisplacement = renderDisplacement;
        this.distortBehind = distortBehind;

        if (respectVisible)
            visibleOverride = new VisibleOverride();
    }

    public DepthAdheringDisplacementRenderHook(Action renderEntity, Action renderDisplacement, bool distortBehind)
        : this(renderEntity, renderDisplacement, distortBehind, true) { }

    private void TrackSelf() => DepthAdheringDisplacementRenderer.GetRenderer(Scene, Entity.Depth).Track(this);
    private void UntrackSelf() => DepthAdheringDisplacementRenderer.GetRenderer(Scene, Entity.Depth).Untrack(this);

    public bool GetRendererOptions() => distortBehind;
    public bool GetVisible() => EntityVisible;

    public override void Added(Entity entity)
    {
        base.Added(entity);

        if (Scene is not null)
            TrackSelf();

        if (visibleOverride is not null)
            entity.Add(visibleOverride);
        else
            entity.Visible = false;
    }

    public override void EntityAdded(Scene scene)
    {
        base.EntityAdded(scene);
        TrackSelf();
    }

    public override void Removed(Entity entity)
    {
        UntrackSelf();

        if (visibleOverride is not null)
            entity.Remove(visibleOverride);

        base.Removed(entity);
    }

    public override void EntityRemoved(Scene scene)
    {
        UntrackSelf();
        base.EntityRemoved(scene);
    }

    #region Hooks

    private static Hook hook_Entity_set_Depth;

    [OnLoad]
    internal static void Load()
    {
        hook_Entity_set_Depth = new Hook(
            typeof(Entity).GetProperty(nameof(Entity.Depth), HookHelper.Bind.PublicInstance)!.GetSetMethod()!,
            On_Entity_set_Depth);
    }

    [OnUnload]
    internal static void Unload()
    {
        HookHelper.DisposeAndSetNull(ref hook_Entity_set_Depth);
    }

    private delegate void orig_Entity_set_Depth(Entity self, int value);
    private static void On_Entity_set_Depth(orig_Entity_set_Depth orig, Entity self, int value)
    {
        if (self.Depth == value || self.Scene is null || self.Scene.Tracker.CountComponents<DepthAdheringDisplacementRenderHook>() == 0)
        {
            orig(self, value);
            return;
        }

        DepthAdheringDisplacementRenderHook[] renderHooks = self.Components.GetAll<DepthAdheringDisplacementRenderHook>().ToArray();
        foreach (DepthAdheringDisplacementRenderHook renderHook in renderHooks)
            renderHook.UntrackSelf();

        orig(self, value);

        foreach (DepthAdheringDisplacementRenderHook renderHook in renderHooks)
            renderHook.TrackSelf();
    }

    #endregion
}
