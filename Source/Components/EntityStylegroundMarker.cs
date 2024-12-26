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
    public EntityStylegroundMarker(string tag) : base(active: false, visible: false) {
        Tag = tag;
    }

    public override void Added(Entity entity) {
        base.Added(entity);

        // no empty tags or obvious infinite loops!
        // you could probably still work around the loop thing but honestly if youre doing that you should be expecting a freeze anyway
        if (string.IsNullOrEmpty(Tag) || (entity is StylegroundEntityController stylegroundEntity && stylegroundEntity.StylegroundTag == Tag))
            RemoveSelf();

    }
}
