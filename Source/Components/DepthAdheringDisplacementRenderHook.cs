using System;
using System.Linq;
using Monocle;
using Celeste.Mod.SorbetHelper.Entities;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace Celeste.Mod.SorbetHelper.Components;

[Tracked]
public class DepthAdheringDisplacementRenderHook : Component {
    public readonly Action RenderEntity;
    public readonly Action RenderDisplacement;

    private readonly bool distortBehind;

    private readonly VisibleOverride VisibleOverride;
    public bool EntityVisible => VisibleOverride?.EntityVisible != false;

    public DepthAdheringDisplacementRenderHook(Action renderEntity, Action renderDisplacement, bool distortBehind, bool respectVisible) : base(false, false) {
        RenderEntity = renderEntity;
        RenderDisplacement = renderDisplacement;
        this.distortBehind = distortBehind;

        if (respectVisible)
            VisibleOverride = new VisibleOverride();
    }

    public DepthAdheringDisplacementRenderHook(Action renderEntity, Action renderDisplacement, bool distortBehind) : this(renderEntity, renderDisplacement, distortBehind, true) { }

    private void TrackSelf() => DepthAdheringDisplacementRenderer.GetRenderer(Scene, Entity.Depth, distortBehind).Track(this);
    private void UntrackSelf() => DepthAdheringDisplacementRenderer.GetRenderer(Scene, Entity.Depth, distortBehind).Untrack(this);

    public override void Added(Entity entity) {
        base.Added(entity);

        if (Scene is not null)
            TrackSelf();

        if (VisibleOverride is not null)
            entity.Add(VisibleOverride);
        else
            entity.Visible = false;
    }

    public override void EntityAdded(Scene scene) {
        base.EntityAdded(scene);
        TrackSelf();
    }

    public override void Removed(Entity entity) {
        UntrackSelf();

        if (VisibleOverride is not null)
            entity.Remove(VisibleOverride);

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
        hook_Entity_set_Depth?.Dispose();
        hook_Entity_set_Depth = null;
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
