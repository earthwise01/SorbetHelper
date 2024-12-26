using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Backdrops;
using Celeste.Mod.SorbetHelper.Entities;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Backdrops;

/// <summary>
/// renders entities with an entitystyleground component as if they were a styleground<br/>
/// also see components.entitystylegroundmarker and entities.entitystylegroundcontroller
/// </summary>
[CustomBackdrop("SorbetHelper/EntityStylegroundRenderer")]
public class EntityStylegroundRenderer : Backdrop {
    // private readonly bool AccurateDepthSorting;
    public EntityStylegroundRenderer(BinaryPacker.Element data) {
        // probablyll just cause more confusion than its worth
        // AccurateDepthSorting = data.AttrBool("accurateDepthSorting", true);
        UseSpritebatch = false;
    }

    public override void Update(Scene scene) {
        base.Update(scene);
    }

    public override void Render(Scene scene) {
        base.Render(scene);

        var components = scene.Tracker.GetComponents<EntityStylegroundMarker>();
        if (components.Count == 0)
            return;

        var toRender = GetEntitiesToRender(components);

        GameplayRenderer.Begin();

        foreach (var entity in toRender)
            entity.Render();

        GameplayRenderer.End();
    }

    public List<Entity> GetEntitiesToRender(List<Component> components) {
        var entities = new List<Entity>(components.Count);

        for (int i = 0; i < components.Count; i++) {
            var marker = (EntityStylegroundMarker)components[i];

            if (marker.Entity.Visible == true && Tags.Contains(marker.Tag))
                entities.Add(marker.Entity);
        }

        // if (AccurateDepthSorting)
        // pain  (potential optimization could be to use a custom tracker entity thing which sorts depth only when needed like how entitylist.updatelists works?)
        // at the very least though thankfully only needs to go through currently visible marked entities
        entities.Sort(EntityList.CompareDepth);

        return entities;
    }
}
