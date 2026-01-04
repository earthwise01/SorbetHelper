using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Backdrops;
using Celeste.Mod.SorbetHelper.Entities;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Utils;
using FMOD.Studio;

namespace Celeste.Mod.SorbetHelper.Backdrops;

/// <summary>
/// Renders entities with an EntityStylegroundMarker component as if they were part of a styleground.<br/>
/// See also Components.EntityStylegroundMarker and Entities.EntityStylegroundController.
/// </summary>
[CustomBackdrop("SorbetHelper/EntityStylegroundRenderer")]
public class EntityStylegroundRenderer : Backdrop {
    public EntityStylegroundRenderer(BinaryPacker.Element data) {
        UseSpritebatch = false;
    }

    public override void Render(Scene scene) {
        base.Render(scene);

        List<Component> components = scene.Tracker.GetComponents<EntityStylegroundMarker>();
        if (components.Count == 0)
            return;

        List<EntityStylegroundMarker> toRender = GetEntitiesToRender(components);

        GameplayRenderer.Begin();

        foreach (EntityStylegroundMarker marker in toRender)
            marker.Entity.Render();

        GameplayRenderer.End();
    }

    private static readonly Comparison<EntityStylegroundMarker> CompareDepth = (a, b) => Math.Sign(b.Entity.actualDepth - a.Entity.actualDepth);

    private List<EntityStylegroundMarker> GetEntitiesToRender(List<Component> components) {
        List<EntityStylegroundMarker> markers = new List<EntityStylegroundMarker>(components.Count);

        foreach (Component t in components) {
            EntityStylegroundMarker marker = (EntityStylegroundMarker)t;

            if (marker.EntityVisible && Tags.Contains(marker.Tag))
                markers.Add(marker);
        }

        // todo: potential optimization could be to use a custom tracker entity thing which sorts depth only when needed like how EntityList.UpdateLists works?
        // at the very least though thankfully only needs to go through currently visible marked entities
        markers.Sort(CompareDepth);

        return markers;
    }
}
