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

        /*
         * the implementation for scale and hitOffset (and how they affect the appearance of the block) is based on code from Communal Helper's Station Blocks
         * https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/Entities/StationBlock/StationBlock.cs
         */

        public string shakeSfx;
        public string impactSfx;
        public bool fallOnTouch;
        public bool fallOnStaticMover;
        public bool allowWavedash;
        public bool dashCornerCorrection;

        //public bool isTriggered;
        public TileGrid tilegrid;
        public Vector2 scale = Vector2.One;
        public Vector2 hitOffset;
        private bool climbFall;

        public new bool HasStartedFalling { get; private set; }

        public static void Load() {
            /*On.Celeste.FallingBlock.Sequence += onSequence;
            On.Celeste.FallingBlock.PlayerFallCheck += onPlayerFallCheck;
            On.Celeste.FallingBlock.ShakeSfx += onShakeSfx;
            On.Celeste.FallingBlock.ImpactSfx += onImpactSfx;*/
        }

        public static void Unload() {
            /*On.Celeste.FallingBlock.Sequence -= onSequence;
            On.Celeste.FallingBlock.PlayerFallCheck -= onPlayerFallCheck;
            On.Celeste.FallingBlock.ShakeSfx -= onShakeSfx;
            On.Celeste.FallingBlock.ImpactSfx -= onImpactSfx;*/
        }

        public DashFallingBlock(EntityData data, Vector2 offset) : base(data, offset) {
            // remove the TileGrid added by the vanilla falling block
            Remove(Get<TileGrid>());
            Remove(Get<Coroutine>());

            shakeSfx = data.Attr("shakeSfx", "event:/game/general/fallblock_shake");
            impactSfx = data.Attr("impactSfx", "event:/game/general/fallblock_impact");
            fallOnTouch = data.Bool("fallOnTouch", false);
            fallOnStaticMover = data.Bool("fallOnStaticMover", false);
            allowWavedash = data.Bool("allowWavedash", false);
            dashCornerCorrection = data.Bool("dashCornerCorrection", false);
            climbFall = data.Bool("climbFall", true);
            base.Depth = data.Int("depth", base.Depth);

            // generate the TileGrid
            char tile = data.Char("tiletype", '3');
            int newSeed = Calc.Random.Next();
            Calc.PushRandom(newSeed);
            Add(tilegrid = GFX.FGAutotiler.GenerateBox(tile, data.Width / 8, data.Height / 8).TileGrid);
            Calc.PopRandom();
            Add(new TileInterceptor(tilegrid, highPriority: false));
            // make the tilegrid invisible since we want to render it manually later
            tilegrid.Visible = false;

            Add(new Coroutine(Sequence()));

            OnDashCollide = OnDashCollision;
        }

        public DashCollisionResults OnDashCollision(Player player, Vector2 dir) {
            if (!Triggered) {
                // make wallbouncing easier if dash corner correction is enabled
                if ((player.Left >= Right - 4f || player.Right < Left + 4f) && dir.Y == -1 && dashCornerCorrection) {
                    return DashCollisionResults.NormalCollision;
                }

                // trigger the block
                (Scene as Level).DirectionalShake(dir);
                Triggered = true;
                Audio.Play(impactSfx, base.Center);

                // emit the dust particles and update the scale and hitOffset
                for (int i = 2; (float)i <= base.Width; i += 4) {
                    if (!base.Scene.CollideCheck<Solid>(base.BottomLeft + new Vector2(i, 3f))) {
                        SceneAs<Level>().Particles.Emit(P_FallDustB, 1, new Vector2(base.X + (float)i, base.Bottom), Vector2.One * 4f);
                        SceneAs<Level>().Particles.Emit(P_FallDustA, 1, new Vector2(base.X + (float)i, base.Bottom), Vector2.One * 4f);
                    }
                }
                scale = new Vector2(
                    1f + Math.Abs(dir.Y) * 0.28f - Math.Abs(dir.X) * 0.28f,
                    1f + Math.Abs(dir.X) * 0.28f - Math.Abs(dir.Y) * 0.28f
                );
                hitOffset = dir * 4.15f;

                if (allowWavedash && dir.Y == 1) {
                    return DashCollisionResults.NormalCollision;
                }
                return DashCollisionResults.Rebound;
            }

            return DashCollisionResults.NormalCollision;

        }

        public override void OnStaticMoverTrigger(StaticMover sm) {
            if (fallOnStaticMover) {
                base.OnStaticMoverTrigger(sm);
            }
        }

        public override void OnShake(Vector2 amount) {
            base.OnShake(amount);
            tilegrid.Position += amount;
        }

        public override void Render() {

            base.Render();

            // TileGrids can't have their scale changed, so we have to render the block manually
            Vector2 position = Position + tilegrid.Position;

            var clip = tilegrid.GetClippedRenderTiles();

            for (int tx = clip.Left; tx < clip.Right; tx++) {
                for (int ty = clip.Top; ty < clip.Bottom; ty++) {
                    Vector2 vec = new Vector2(position.X + tx * tilegrid.TileWidth, position.Y + ty * tilegrid.TileHeight) + (Vector2.One * 4f) + hitOffset;
                    vec.X = Center.X + (vec.X - Center.X) * scale.X;
                    vec.Y = Center.Y + (vec.Y - Center.Y) * scale.Y;

                    tilegrid.Tiles[tx, ty].DrawCentered(vec, Color.White, scale);
                }
            }
        }

        public override void Update() {
            base.Update();
            // ease scale and hitOffset towards their default values
            scale.X = Calc.Approach(scale.X, 1f, Engine.DeltaTime * 4f);
            scale.Y = Calc.Approach(scale.Y, 1f, Engine.DeltaTime * 4f);
            hitOffset.X = Calc.Approach(hitOffset.X, 0f, Engine.DeltaTime * 15f);
            hitOffset.Y = Calc.Approach(hitOffset.Y, 0f, Engine.DeltaTime * 15f);
        }

        private IEnumerator Sequence() {
            while (!Triggered && (fallOnTouch ? !PlayerFallCheck() : true)) {
                yield return null;
            }
            while (FallDelay > 0f) {
                FallDelay -= Engine.DeltaTime;
                yield return null;
            }
            HasStartedFalling = true;
            while (true) {
                //ShakeSfx();
                Audio.Play(shakeSfx, base.Center);
                StartShaking();
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                yield return 0.2f;
                float timer = 0.4f;
                while (timer > 0f && PlayerWaitCheck()) {
                    yield return null;
                    timer -= Engine.DeltaTime;
                }
                StopShaking();
                for (int i = 2; (float)i < Width; i += 4) {
                    if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -2f))) {
                        SceneAs<Level>().Particles.Emit(P_FallDustA, 2, new Vector2(X + (float)i, Y), Vector2.One * 4f, (float)Math.PI / 2f);
                    }
                    SceneAs<Level>().Particles.Emit(P_FallDustB, 2, new Vector2(X + (float)i, Y), Vector2.One * 4f);
                }
                float speed = 0f;
                float maxSpeed = (160f);
                while (true) {
                    Level level = SceneAs<Level>();
                    speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);
                    if (MoveVCollideSolids(speed * Engine.DeltaTime, thruDashBlocks: true)) {
                        break;
                    }
                    if (Top > (float)(level.Bounds.Bottom + 16) || (Top > (float)(level.Bounds.Bottom - 1) && CollideCheck<Solid>(Position + new Vector2(0f, 1f)))) {
                        Collidable = (Visible = false);
                        yield return 0.2f;
                        if (level.Session.MapData.CanTransitionTo(level, new Vector2(Center.X, Bottom + 12f))) {
                            yield return 0.2f;
                            SceneAs<Level>().Shake();
                            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                        }
                        RemoveSelf();
                        DestroyStaticMovers();
                        yield break;
                    }
                    yield return null;
                }
                //ImpactSfx();
                Audio.Play(impactSfx, base.BottomCenter);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.3f);
                StartShaking();
                LandParticles();
                yield return 0.2f;
                StopShaking();
                if (CollideCheck<SolidTiles>(Position + new Vector2(0f, 1f))) {
                    break;
                }
                while (CollideCheck<Platform>(Position + new Vector2(0f, 1f))) {
                    yield return 0.1f;
                }
            }
            Safe = true;
        }

        private void LandParticles() {
            for (int i = 2; (float)i <= base.Width; i += 4) {
                if (base.Scene.CollideCheck<Solid>(base.BottomLeft + new Vector2(i, 3f))) {
                    SceneAs<Level>().ParticlesFG.Emit(P_FallDustA, 1, new Vector2(base.X + (float)i, base.Bottom), Vector2.One * 4f, -(float)Math.PI / 2f);
                    float direction = ((!((float)i < base.Width / 2f)) ? 0f : ((float)Math.PI));
                    SceneAs<Level>().ParticlesFG.Emit(P_LandDust, 1, new Vector2(base.X + (float)i, base.Bottom), Vector2.One * 4f, direction);
                }
            }
        }

        private bool PlayerFallCheck() {
            if (climbFall) {
                return HasPlayerRider();
            }
            return HasPlayerOnTop();
        }

        private bool PlayerWaitCheck() {
            if (Triggered) {
                return true;
            }
            if (PlayerFallCheck()) {
                return true;
            }
            if (climbFall) {
                if (!CollideCheck<Player>(Position - Vector2.UnitX)) {
                    return CollideCheck<Player>(Position + Vector2.UnitX);
                }
                return true;
            }
            return false;
        }

        /*private static IEnumerator onSequence(On.Celeste.FallingBlock.orig_Sequence orig, FallingBlock self) {
            if (self is DashFallingBlock block) {
                self.Triggered = block.isTriggered;
            }
            yield return new SwapImmediately(orig(self));
        }

        private static bool onPlayerFallCheck(On.Celeste.FallingBlock.orig_PlayerFallCheck orig, FallingBlock self) {
            if (self is DashFallingBlock block) {
                if (!block.Triggered && block.fallOnTouch) {
                    block.Triggered = orig(self);
                }
                return block.Triggered;
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
        }*/
    }
}
