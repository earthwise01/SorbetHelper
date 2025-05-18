using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Cil;
using Celeste.Mod.SorbetHelper.Components;
using System.Linq;

namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity(              EntityDataID + "Global")] // global version is swapped to in mapdataprocessor based on data.Bool("global")
[CustomEntity(EntityDataID, EntityDataID + "Global")]
public class LightCoverController : Entity {
    private const string EntityDataID = "SorbetHelper/LightCoverController";

    private readonly float Alpha;

    public LightCoverController(EntityData data, Vector2 _) {
        Alpha = data.Float("alpha", 1f);

        if (data.Bool("global", false))
            Add(new GlobalTypeNameProcessor(data, ProcessEntity));
        else
            Add(new TypeNameProcessor(data, ProcessEntity));
    }

    private void ProcessEntity(Entity entity) {
        if (entity.Get<LightCover>() is null)
            entity.Add(new LightCover(Alpha));
    }
}
