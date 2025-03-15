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

[CustomEntity("SorbetHelper/LightCoverController")]
public class LightCoverController : ClassControllerBase {
    private readonly float Alpha;

    public LightCoverController(EntityData data, Vector2 _) : base(data) {
        Alpha = data.Float("alpha", 1f);
    }

    public override void ProcessEntity(Entity entity) {
        if (entity.Get<LightCover>() is null)
            entity.Add(new LightCover(Alpha));
    }
}

// swapped to in mapdataprocessor based on data.Bool("global")
[GlobalEntity]
[CustomEntity("SorbetHelper/LightCoverControllerGlobal")]
public class LightCoverControllerGlobal : GlobalClassControllerBase {
    private readonly float Alpha;

    public LightCoverControllerGlobal(EntityData data, Vector2 _) : base(data) {
        Alpha = data.Float("alpha", 1f);
    }

    public override void ProcessEntity(Entity entity) {
        if (entity.Get<LightCover>() is null)
            entity.Add(new LightCover(Alpha));
    }
}
