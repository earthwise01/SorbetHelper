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

        private bool allowWavedash;

        public DashGateBlock(EntityData data, Vector2 offset) : base(data, offset) {
            allowWavedash = data.Bool("allowWavedash", false);
            OnDashCollide = OnDashCollision;
        }

        public DashCollisionResults OnDashCollision(Player player, Vector2 dir) {
            if (!Triggered) {
                (Scene as Level).DirectionalShake(dir);
                Triggered = true;
                Audio.Play("event:/game/general/wall_break_stone", base.Center);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                if (allowWavedash && dir.Y == 1)
                    return DashCollisionResults.NormalCollision;
                return DashCollisionResults.Rebound;
            }

            return DashCollisionResults.NormalCollision;

        }
    }
}