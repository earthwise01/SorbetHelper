using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using Celeste.Mod.Entities;

namespace Celeste.Mod.SorbetHelper.Entities {

    [Tracked(false)]
    [CustomEntity("SorbetHelper/DashFallingBlock")]
    public class DashFallingBlock : FallingBlock {
        
        private Wiggler bounce;
        private Shaker shaker;

        public bool isTriggered;

        public static void Load() {
            On.Celeste.FallingBlock.Sequence += onSequence;
            On.Celeste.FallingBlock.PlayerFallCheck += onPlayerFallCheck;
        }

        public static void Unload() {
            On.Celeste.FallingBlock.Sequence -= onSequence;
            On.Celeste.FallingBlock.PlayerFallCheck -= onPlayerFallCheck;
        }

        public DashFallingBlock(EntityData data, Vector2 offset) : base (data, offset) {
            bounce = Wiggler.Create(1f, 0.5f);
            bounce.StartZero = false;
            Add(bounce);
            Add(shaker = new Shaker(on: false));
            OnDashCollide = OnDashCollision;
        }

        public DashCollisionResults OnDashCollision(Player player, Vector2 dir) {
            if (!isTriggered) {
                (Scene as Level).DirectionalShake(dir);
                shaker.On = true;
                bounce.Start();
                isTriggered = true;
				Audio.Play("event:/game/general/fallblock_impact", base.Center);
                return DashCollisionResults.Rebound;
            }

            return DashCollisionResults.NormalCollision;

        }

        private static IEnumerator onSequence(On.Celeste.FallingBlock.orig_Sequence orig, FallingBlock self) {
            if (self is DashFallingBlock block) {
                self.Triggered = block.isTriggered;
            }
            yield return new SwapImmediately(orig(self));
        }

        private static bool onPlayerFallCheck(On.Celeste.FallingBlock.orig_PlayerFallCheck orig, FallingBlock self) {
            if (self is DashFallingBlock block) {
                if (!block.isTriggered) {
                    return false;
                }
                return true;
            }
            return orig(self);
        }
    }
}