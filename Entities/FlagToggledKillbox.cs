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
        private readonly string flag;
        private readonly bool inverted;

        public FlagToggledKillbox(EntityData data, Vector2 offset) : base(data, offset) {
            flag = data.Attr("flag");
            inverted = data.Bool("inverted");
        }

        public override void Update() {
            base.Update();
            Player player = base.Scene.Tracker.GetEntity<Player>();

            if (Collidable && player != null && !string.IsNullOrEmpty(flag)) {
                if (!SceneAs<Level>().Session.GetFlag(flag, inverted)) {
                    Collidable = false;
                }
            }
        }
    }
}
