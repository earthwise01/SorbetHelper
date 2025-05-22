using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Entities;

namespace Celeste.Mod.SorbetHelper.Components;

/// <summary>
/// marks entities to be renderer by an entitystylegroundrenderer<br/>
/// check components.renderoverridecomponent for why this makes entities stop rendering normally  ( i love reusing code<br/>
/// also see backdrops.entitystylegroundrenderer and entities.entitystylegroundcontroller
/// </summary>
[Tracked]
public class EntityStylegroundMarker : RenderOverride {
    public readonly string Tag;
    public Action EntityRender { get; private set; }

    public EntityStylegroundMarker(string tag) : base(active: false, visible: false) {
        Tag = tag;
    }

    public override void Added(Entity entity) {
        base.Added(entity);

        // no empty tags!
        if (string.IsNullOrEmpty(Tag))
            RemoveSelf();

        // extremelyy niche; mostly for fg waterfalls
        var depthDisplacement = entity.Get<DepthAdheringDisplacementRenderHook>();
        if (depthDisplacement is null && entity.Get<DisplacementRenderHook>() is { } displacement) {
            depthDisplacement = new DepthAdheringDisplacementRenderHook(entity.Render, displacement.RenderDisplacement, true) {
                Visible = false
            };
            entity.Remove(displacement);
            entity.Add(depthDisplacement);
        }

        if (depthDisplacement is null)
            EntityRender = entity.Render;
        else
            EntityRender = depthDisplacement.EntityRenderOverride;
    }
}
