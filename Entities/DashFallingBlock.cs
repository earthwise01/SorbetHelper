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
        
        public string shakeSfx;
        public string impactSfx;
        public bool fallOnTouch;
        public bool fallOnStaticMover;

        private Wiggler bounce;
        private Shaker shaker;

        public bool isTriggered;

        public static void Load() {
            On.Celeste.FallingBlock.Sequence += onSequence;
            On.Celeste.FallingBlock.PlayerFallCheck += onPlayerFallCheck;
            On.Celeste.FallingBlock.ShakeSfx += onShakeSfx;
            On.Celeste.FallingBlock.ImpactSfx += onImpactSfx;
        }

        public static void Unload() {
            On.Celeste.FallingBlock.Sequence -= onSequence;
            On.Celeste.FallingBlock.PlayerFallCheck -= onPlayerFallCheck;
            On.Celeste.FallingBlock.ShakeSfx -= onShakeSfx;
            On.Celeste.FallingBlock.ImpactSfx -= onImpactSfx;
        }

        public DashFallingBlock(EntityData data, Vector2 offset) : base(data, offset) {
            shakeSfx = data.Attr("shakeSfx", "event:/game/general/fallblock_shake");
            impactSfx = data.Attr("impactSfx", "event:/game/general/fallblock_impact");
            fallOnTouch = data.Bool("fallOnTouch", false);
            fallOnStaticMover = data.Bool("fallOnStaticMover", false);
            base.Depth = data.Int("depth", base.Depth);
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
                Audio.Play(impactSfx, base.Center);
                return DashCollisionResults.Rebound;
            }

            return DashCollisionResults.NormalCollision;

        }

        public override void OnStaticMoverTrigger(StaticMover sm) {
            if (fallOnStaticMover) {
                base.OnStaticMoverTrigger(sm);
                isTriggered = true;
            }
        }

        private static IEnumerator onSequence(On.Celeste.FallingBlock.orig_Sequence orig, FallingBlock self) {
            if (self is DashFallingBlock block) {
                self.Triggered = block.isTriggered;
            }
            yield return new SwapImmediately(orig(self));
        }

        private static bool onPlayerFallCheck(On.Celeste.FallingBlock.orig_PlayerFallCheck orig, FallingBlock self) {
            if (self is DashFallingBlock block) {
                if (block.isTriggered) {
                    return true;
                } else if (!block.fallOnTouch) {
                    return false;
                } else if (orig(self)) {
                    block.isTriggered = true;
                }
            }
            return orig(self);
        }

        private static void onShakeSfx(On.Celeste.FallingBlock.orig_ShakeSfx orig, FallingBlock self) {
            if (self is DashFallingBlock block) {
                Audio.Play(block.shakeSfx, self.Center);
            } else {
                orig(self);
            }
        }

        private static void onImpactSfx(On.Celeste.FallingBlock.orig_ImpactSfx orig, FallingBlock self) {
            if (self is DashFallingBlock block) {
                Audio.Play(block.impactSfx, self.BottomCenter);
            } else {
                orig(self);
            }
        }
    }
}