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
        public bool dashCornerCorrection;

        public bool isTriggered;
        public TileGrid tilegrid;
        public Vector2 hitOffset;
        public Vector2 scale = Vector2.One;

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
            DynamicData fallingBlockData = new DynamicData(this);
            Remove(fallingBlockData.Get<TileGrid>("tiles"));
            shakeSfx = data.Attr("shakeSfx", "event:/game/general/fallblock_shake");
            impactSfx = data.Attr("impactSfx", "event:/game/general/fallblock_impact");
            fallOnTouch = data.Bool("fallOnTouch", false);
            fallOnStaticMover = data.Bool("fallOnStaticMover", false);
            allowWavedash = data.Bool("allowWavedash", false);
            dashCornerCorrection = data.Bool("dashCornerCorrection", false);
            base.Depth = data.Int("depth", base.Depth);
            char tile = data.Char("tiletype", '3');
            int newSeed = Calc.Random.Next();
            Calc.PushRandom(newSeed);
            Add(tilegrid = GFX.FGAutotiler.GenerateBox(tile, data.Width / 8, data.Height / 8).TileGrid);
            Calc.PopRandom();
            Add(new TileInterceptor(tilegrid, highPriority: false));
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
                ActivateParticles(-dir);
                scale = new Vector2(
                    1f + Math.Abs(dir.Y) * 0.35f - Math.Abs(dir.X) * 0.35f,
                    1f + Math.Abs(dir.X) * 0.35f - Math.Abs(dir.Y) * 0.35f);
                hitOffset = dir * 5f;
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

        public override void OnShake(Vector2 amount) {
            base.OnShake(amount);
            tilegrid.Position += amount;
        }

        public override void Render() {
            tilegrid.Alpha = 0f;

            base.Render();

            tilegrid.Alpha = 1f;

            Vector2 position = Position + tilegrid.Position;

            var clip = tilegrid.GetClippedRenderTiles();
            //MTexture tile;

            for (int tx = clip.Left; tx < clip.Right; tx++) {
                for (int ty = clip.Top; ty < clip.Bottom; ty++) {
                    Vector2 vec = new Vector2(position.X + tx * tilegrid.TileWidth, position.Y + ty * tilegrid.TileHeight) + (Vector2.One * 4f) + hitOffset;
                    vec.X = Center.X + (vec.X - Center.X) * scale.X;
                    vec.Y = Center.Y + (vec.Y - Center.Y) * scale.Y;

                    //tile = tiles.Tiles[tx, ty];
                    //if (tile != null)
                    tilegrid.Tiles[tx, ty].DrawCentered(vec, Color.White, scale);
                }
            }
        }

        public override void Update() {
            base.Update();
            scale.X = Calc.Approach(scale.X, 1f, Engine.DeltaTime * 4f);
            scale.Y = Calc.Approach(scale.Y, 1f, Engine.DeltaTime * 4f);
            hitOffset.X = Calc.Approach(hitOffset.X, 0f, Engine.DeltaTime * 15f);
            hitOffset.Y = Calc.Approach(hitOffset.Y, 0f, Engine.DeltaTime * 15f);
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

        private void ActivateParticles(Vector2 dir) {
			float direction;
			Vector2 position;
			Vector2 positionRange;
			int num;
			if (dir == Vector2.UnitX) {
				direction = 0f;
				position = base.CenterRight - Vector2.UnitX;
				positionRange = Vector2.UnitY * (base.Height - 2f) * 0.5f;
				num = (int)(base.Height / 8f) * 4;
			}
			else if (dir == -Vector2.UnitX) {
				direction = (float)Math.PI;
				position = base.CenterLeft + Vector2.UnitX;
				positionRange = Vector2.UnitY * (base.Height - 2f) * 0.5f;
				num = (int)(base.Height / 8f) * 4;
			}
			else if (dir == Vector2.UnitY) {
				direction = (float)Math.PI / 2f;
				position = base.BottomCenter - Vector2.UnitY;
				positionRange = Vector2.UnitX * (base.Width - 2f) * 0.5f;
				num = (int)(base.Width / 8f) * 4;
			}
			else {
				direction = -(float)Math.PI / 2f;
				position = base.TopCenter + Vector2.UnitY;
				positionRange = Vector2.UnitX * (base.Width - 2f) * 0.5f;
				num = (int)(base.Width / 8f) * 4;
			}
			num += 2;
			SceneAs<Level>().Particles.Emit(P_LandDust, num, position, positionRange, direction);
		}
    }
}