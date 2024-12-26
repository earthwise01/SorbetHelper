using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Utils;


namespace Celeste.Mod.SorbetHelper.Entities;

/// <summary>
/// marks entities with the specified class names, with entitystylegroundmarker components to be rendered by an entitystylegroundrenderer<br/>
/// also see components.entitystylegroundmarker and backdrops.entitystylegroundrenderer
/// </summary>
[CustomEntity("SorbetHelper/EntityStylegroundController")]
public class EntityStylegroundController : ClassControllerBase {
    private readonly string StylegroundTag;

    public EntityStylegroundController(EntityData data, Vector2 _) : base(data) {
        StylegroundTag = data.Attr("tag", "");
    }

    public override void ProcessEntity(Entity entity) {
        if (entity.Get<EntityStylegroundMarker>() is null)
            entity.Add(new EntityStylegroundMarker(StylegroundTag));
    }
}

// swapped to in mapdataprocessor based on data.Bool("global")
[GlobalEntity("SorbetHelper/GlobalEntityStylegroundController")]
public class GlobalEntityStylegroundController : GlobalClassControllerBase {
    private readonly string StylegroundTag;

    public GlobalEntityStylegroundController(EntityData data, Vector2 _) : base(data) {
        StylegroundTag = data.Attr("tag", "");
    }

    public override void ProcessEntity(Entity entity) {
        if (entity.Get<EntityStylegroundMarker>() is null)
            entity.Add(new EntityStylegroundMarker(StylegroundTag));
    }
}
