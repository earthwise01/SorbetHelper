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

        public TouchGateBlock(EntityData data, Vector2 offset) : base(data, offset) {
            moveOnGrab = data.Bool("moveOnGrab", true);
        }

        public override bool TriggerCheck() {
            if (!moveOnGrab) {
                return HasPlayerOnTop();
            }
            return HasPlayerRider();

        }

        public override void PlayMoveSounds() {
            base.PlayMoveSounds();
            Audio.Play("event:/game/general/fallblock_shake", Position);
        }

    }
}