using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Entities;

namespace Celeste.Mod.SorbetHelper.Triggers {

    [CustomEntity("SorbetHelper/LinkedGateBlockActivatorTrigger")]
    public class LinkedGateBlockActivator : Trigger {

        private string linkTag;
        private string flag;
        private bool inverted;

        public LinkedGateBlockActivator(EntityData data, Vector2 offset) : base(data, offset) {
            linkTag = data.Attr("linkTag", "");
            flag = data.Attr("flag", "");
            inverted = data.Bool("inverted");
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            if (!string.IsNullOrEmpty(linkTag)) {
                foreach (GateBlock gateBlock in Scene.Tracker.GetEntities<GateBlock>()) {
                    if (!gateBlock.Triggered && gateBlock.linked && gateBlock.linkTag == linkTag) {
                        gateBlock.Triggered = true;
                    }
                }
            }
        }

        public override void Update() {
            base.Update();
            if (!string.IsNullOrEmpty(flag)) {
                if ((!inverted && !(Scene as Level).Session.GetFlag(flag)) || (inverted && (Scene as Level).Session.GetFlag(flag)))
                    Collidable = false;
                else if (!Collidable)
                    Collidable = true;
            }
        }
    }
}
