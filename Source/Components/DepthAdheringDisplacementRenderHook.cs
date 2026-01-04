using System;
using System.Linq;
using Monocle;
using Celeste.Mod.SorbetHelper.Entities;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Components;

[Tracked]
public class DepthAdheringDisplacementRenderHook : Component {
    public readonly Action RenderEntity;
    public readonly Action RenderDisplacement;

    private readonly bool distortBehind;

    private readonly VisibleOverride visibleOverride;
    public bool EntityVisible => visibleOverride?.EntityVisible != false;

    public DepthAdheringDisplacementRenderHook(Action renderEntity, Action renderDisplacement, bool distortBehind, bool respectVisible) : base(false, false) {
        RenderEntity = renderEntity;
        RenderDisplacement = renderDisplacement;
        this.distortBehind = distortBehind;

        if (respectVisible)
            visibleOverride = new VisibleOverride();
    }

    public DepthAdheringDisplacementRenderHook(Action renderEntity, Action renderDisplacement, bool distortBehind) : this(renderEntity, renderDisplacement, distortBehind, true) { }

    private void TrackSelf() => DepthAdheringDisplacementRenderer.GetRenderer(Scene, Entity.Depth, distortBehind).Track(this);
    private void UntrackSelf() => DepthAdheringDisplacementRenderer.GetRenderer(Scene, Entity.Depth, distortBehind).Untrack(this);

    public override void Added(Entity entity) {
        base.Added(entity);

        if (Scene is not null)
            TrackSelf();

        if (visibleOverride is not null)
            entity.Add(visibleOverride);
        else
            entity.Visible = false;
    }

    public override void EntityAdded(Scene scene) {
        base.EntityAdded(scene);
        TrackSelf();
    }

    public override void Removed(Entity entity) {
        UntrackSelf();

        if (visibleOverride is not null)
            entity.Remove(visibleOverride);

        base.Removed(entity);
    }

    public override void EntityRemoved(Scene scene) {
        UntrackSelf();
        base.EntityRemoved(scene);
    }

    #region Hooks

    private static Hook hook_Entity_set_Depth;

    internal static void Load() {
        hook_Entity_set_Depth = new Hook(typeof(Entity).GetMethod("set_Depth", BindingFlags.Instance | BindingFlags.Public), On_Entity_set_Depth);
    }

    internal static void Unload() {
        Util.DisposeAndSetNull(ref hook_Entity_set_Depth);
    }

    // i'm kinda bleh on hooking this but any other approaches seem slightly too unreliable + i guess depth doesn't change that oftenn and communal helper dream sprites take this approach too
    private static void On_Entity_set_Depth(Action<Entity, int> orig, Entity self, int value) {
        if (self.Depth == value || self.Scene is null || self.Scene.Tracker.CountComponents<DepthAdheringDisplacementRenderHook>() == 0) {
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
