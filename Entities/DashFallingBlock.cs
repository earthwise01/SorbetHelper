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
        public bool allowWavedash;
        private bool dashCornerCorrection;

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
            allowWavedash = data.Bool("allowWavedash", false);
            dashCornerCorrection = data.Bool("dashCornerCorrection", false);
            base.Depth = data.Int("depth", base.Depth);
            OnDashCollide = OnDashCollision;
        }

        public DashCollisionResults OnDashCollision(Player player, Vector2 dir) {
            if (!isTriggered) {
                // Make wallbouncing easier if dash corner correction is enabled
                if ((player.Left >= Right - 4f || player.Right < Left + 4f) && dir.Y == -1 && dashCornerCorrection)
                    return DashCollisionResults.NormalCollision;
                // Trigger the block
                (Scene as Level).DirectionalShake(dir);
                isTriggered = true;
                Audio.Play(impactSfx, base.Center);
                if (allowWavedash && dir.Y == 1)
                    return DashCollisionResults.NormalCollision;
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
                if (!block.isTriggered && block.fallOnTouch) {
                    block.isTriggered = orig(self);
                }
                return block.isTriggered;
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