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
[GlobalEntity(              EntityDataID + "Global")] // global version is swapped to in mapdataprocessor based on data.Bool("global")
[CustomEntity(EntityDataID, EntityDataID + "Global")]
public class EntityStylegroundController : Entity {
    private const string EntityDataID = "SorbetHelper/EntityStylegroundController";

    private readonly string StylegroundTag;

    public EntityStylegroundController(EntityData data, Vector2 _) {
        StylegroundTag = data.Attr("tag", "");

        if (data.Bool("global", false))
            Add(new GlobalTypeNameProcessor(data, ProcessEntity));
        else
            Add(new TypeNameProcessor(data, ProcessEntity));
    }

    private void ProcessEntity(Entity entity) {
        if (entity.Get<EntityStylegroundMarker>() is null)
            entity.Add(new EntityStylegroundMarker(StylegroundTag));
    }
}
