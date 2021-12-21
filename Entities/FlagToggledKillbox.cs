using Monocle;
using MonoMod.Utils;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.SorbetHelper.Entities {
    
    [CustomEntity("SorbetHelper/FlagToggledKillbox")]
    [TrackedAs(typeof(Killbox))]

    public class FlagToggledKillbox : Killbox {
        private string flag;
        private bool inverted;
        public FlagToggledKillbox(EntityData data, Vector2 offset) : base(data, offset) {
            flag = data.Attr("flag");
            inverted = data.Bool("inverted");
        }

        public override void Update() {
            base.Update();
            Player entity = base.Scene.Tracker.GetEntity<Player>();
            if (Collidable && entity != null && !string.IsNullOrEmpty(flag)) {
                if ((!inverted && !SceneAs<Level>().Session.GetFlag(flag)) 
                || (inverted && SceneAs<Level>().Session.GetFlag(flag))) {
                    Collidable = false;
                }
            }
        }
    }
}