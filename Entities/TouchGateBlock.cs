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

    [CustomEntity("SorbetHelper/TouchGateBlock")]
    [Tracked]
    public class TouchGateBlock : GateBlock {

        private bool moveOnGrab;
        private bool moveOnStaticMover;

        public TouchGateBlock(EntityData data, Vector2 offset) : base(data, offset) {
            moveOnGrab = data.Bool("moveOnGrab", true);
            moveOnStaticMover = data.Bool("moveOnStaticMoverInteract", false);
        }

        public override void OnStaticMoverTrigger(StaticMover sm) {
            if (!Triggered && moveOnStaticMover) {
                Triggered = true;
                TriggerLinked();
                Audio.Play("event:/game/general/fallblock_shake", Position);
            }
        }

        public override bool TriggerCheck() {
            if (!Triggered && (moveOnGrab && HasPlayerRider()) || (!moveOnGrab && HasPlayerOnTop())) {
                Triggered = true;
                TriggerLinked();
                Audio.Play("event:/game/general/fallblock_shake", Position);
            }
            return base.TriggerCheck();
        }

    }
}