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

    [CustomEntity("SorbetHelper/DashGateBlock")]
    [Tracked]
    public class DashGateBlock : GateBlock {

        private Wiggler bounce;
        private Shaker shaker;

        public DashGateBlock(EntityData data, Vector2 offset) : base(data, offset) {
            bounce = Wiggler.Create(1f, 0.5f);
            bounce.StartZero = false;
            Add(bounce);
            Add(shaker = new Shaker(on: false));
            OnDashCollide = OnDashCollision;
        }

        public DashCollisionResults OnDashCollision(Player player, Vector2 dir) {
            if (!Triggered) {
                (Scene as Level).DirectionalShake(dir);
                shaker.On = true;
                bounce.Start();
                Triggered = true;
                Audio.Play("event:/game/general/wall_break_stone", base.Center);
                return DashCollisionResults.Rebound;
            }

            return DashCollisionResults.NormalCollision;

        }
    }
}