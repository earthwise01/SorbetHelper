using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using Celeste.Mod.Entities;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/LinkedGateBlock")]
    [Tracked]
    public class LinkedGateBlock : GateBlock {

        public LinkedGateBlock(EntityData data, Vector2 offset) : base(data, offset) {
            linked = true;
        }
        
    }
}