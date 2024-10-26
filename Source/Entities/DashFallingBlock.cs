using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Components;

namespace Celeste.Mod.SorbetHelper.Entities {

    [TrackedAs(typeof(FallingBlock))]
    [CustomEntity("SorbetHelper/DashFallingBlock")]
    public class DashFallingBlock : FallingBlock {

        /*
            the implementation for scale and hitOffset (and how they affect the appearance of the block) is heavily based on code from Communal Helper's Station Blocks
            https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/Entities/StationBlocks/StationBlock.cs
        */

        private enum FallDashModes { Disabled, Push, Pull }

        private readonly string shakeSfx;
        private readonly string impactSfx;
        private readonly bool fallOnTouch;
        private readonly bool fallOnStaticMover;
        private readonly bool allowWavedash;
        private readonly bool dashCornerCorrection;
        private readonly bool breakDashBlocks;

        private Vector2 scale = Vector2.One;
        private Vector2 hitOffset;

        private static readonly Dictionary<string, Vector2> directionToVector = new Dictionary<string, Vector2>() {
            {"down", new Vector2(0f, 1f)}, {"up", new Vector2(0f, -1f)}, {"left", new Vector2(-1f, 0f)}, {"right", new Vector2(1f, 0f)}
        };
        public Vector2 Direction;
        private readonly FallDashModes fallDashMode;

        public static ParticleType P_HitFallDust { get; private set; }

        public DashFallingBlock(EntityData data, Vector2 offset) : base(data, offset) {
            // remove the Coroutine added by the vanilla falling block
            Remove(Get<Coroutine>());

            shakeSfx = data.Attr("shakeSfx", "event:/game/general/fallblock_shake");
            impactSfx = data.Attr("impactSfx", "event:/game/general/fallblock_impact");
            fallOnTouch = data.Bool("fallOnTouch", false);
            fallOnStaticMover = data.Bool("fallOnStaticMover", false);
            allowWavedash = data.Bool("allowWavedash", false);
            dashCornerCorrection = data.Bool("dashCornerCorrection", false);
            breakDashBlocks = data.Bool("breakDashBlocks", true);
            Depth = data.Int("depth", Depth);
            Direction = directionToVector[data.Attr("direction", "down").ToLower()];
            fallDashMode = data.Enum("fallDashMode", FallDashModes.Disabled);

            // make the tilegrid invisible since we want to render it manually later
            tiles.Visible = false;

            Add(new Coroutine(Sequence()));

            // allows disabling dash activation
            if (data.Bool("dashActivated", true))
                OnDashCollide = OnDashCollision;

            // i was going to use a moving block hittable component before i remembered that. right TrackedAs is a Thing i can just use that asdfasd (+ having falling blocks trigger other falling blocks is neat maybe but inconsistent with vanilla)
            // Add(new MovingBlockHittable(OnMovingBlockHit));
        }

        public DashCollisionResults OnDashCollision(Player player, Vector2 dir) {
            if (!HasStartedFalling && !Triggered) {
                // gravity helper support
                bool gravityInverted = GravityHelperImports.IsPlayerInverted?.Invoke() ?? false;

                // make wallbouncing easier if dash corner correction is enabled
                if ((player.Left >= Right - 4f || player.Right < Left + 4f) && dir.Y == (gravityInverted ? 1f : -1f) && dashCornerCorrection) {
                    return DashCollisionResults.NormalCollision;
                }

                // if the falling block is set to move in the direction of madeline's dash, update the direction accordingly
                if (fallDashMode != FallDashModes.Disabled) {
                    Direction = fallDashMode == FallDashModes.Pull ? -dir : dir;
                }

                // trigger the block
                (Scene as Level).DirectionalShake(dir);
                Triggered = true;
                Audio.Play(impactSfx, Center);

                // emit the dust particles and update the scale and hitOffset
                for (int i = 2; i <= Width; i += 4) {
                    if (!Scene.CollideCheck<Solid>(BottomLeft + new Vector2(i, 3f))) {
                        SceneAs<Level>().Particles.Emit(P_HitFallDust, 1, new Vector2(X + i, Bottom), Vector2.One * 4f);
                        SceneAs<Level>().Particles.Emit(P_FallDustA, 1, new Vector2(X + i, Bottom), Vector2.One * 4f);
                    }
                }
                scale = new Vector2(
                    1f + Math.Abs(dir.Y) * 0.28f - Math.Abs(dir.X) * 0.28f,
                    1f + Math.Abs(dir.X) * 0.28f - Math.Abs(dir.Y) * 0.28f
                );
                hitOffset = dir * 4.15f;

                if (allowWavedash && dir.Y == (gravityInverted ? -1f : 1f)) {
                    return DashCollisionResults.NormalCollision;
                }
                return DashCollisionResults.Rebound;
            }

            return DashCollisionResults.NormalCollision;
        }

        public override void OnStaticMoverTrigger(StaticMover sm) {
            if (fallOnStaticMover) {
                Triggered = true;
            }
        }

        public override void Render() {
            base.Render();

            // TileGrids can't have their scale changed, so we have to render the block manually
            Vector2 position = Position + tiles.Position;

            var clip = tiles.GetClippedRenderTiles();

            for (int tx = clip.Left; tx < clip.Right; tx++) {
                for (int ty = clip.Top; ty < clip.Bottom; ty++) {
                    Vector2 vec = new Vector2(position.X + tx * tiles.TileWidth, position.Y + ty * tiles.TileHeight) + (Vector2.One * 4f) + hitOffset;
                    vec.X = Center.X + (vec.X - Center.X) * scale.X;
                    vec.Y = Center.Y + (vec.Y - Center.Y) * scale.Y;

                    tiles.Tiles[tx, ty].DrawCentered(vec, Color.White, scale);
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

        private new IEnumerator Sequence() {
            while (!Triggered && (!fallOnTouch || !PlayerFallCheck()))
                yield return null;

            HasStartedFalling = true;
            while (true) {
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
                DirectionalShakeParticles();

                float speed = 0f;
                float maxSpeed = 160f;
                while (true) {
                    Level level = SceneAs<Level>();
                    speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);

                    if (MoveVCollideSolids(speed * Direction.Y * Engine.DeltaTime, thruDashBlocks: breakDashBlocks))
                        break;

                    if (MoveHCollideSolids(speed * Direction.X * Engine.DeltaTime, thruDashBlocks: breakDashBlocks))
                        break;

                    // checks whether the falling block fell out of bounds
                    // all of these checks are done on any dash falling block regardless of its direction so hopefully that wont break anything somewhere
                    if (Top > level.Bounds.Bottom + 16 || Bottom < level.Bounds.Top - 16 || Right < level.Bounds.Left - 16 || Left > level.Bounds.Right + 16 ||
                    ((Top > level.Bounds.Bottom - 1 || Bottom < level.Bounds.Top + 1 || Right < level.Bounds.Left + 1 || Left > level.Bounds.Right - 1) && CollideCheck<Solid>(Position + Direction))) {
                        Collidable = Visible = false;
                        yield return 0.2f;

                        // checks whether the falling block fell into a screen transition (i think)
                        if (level.Session.MapData.CanTransitionTo(level, new Vector2(Center.X, Bottom + 12f)) || level.Session.MapData.CanTransitionTo(level, new Vector2(Center.X, Top - 12f)) ||
                        level.Session.MapData.CanTransitionTo(level, new Vector2(Left - 12f, Center.Y)) || level.Session.MapData.CanTransitionTo(level, new Vector2(Right + 12f, Center.Y))) {
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

                Audio.Play(impactSfx, base.BottomCenter);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().DirectionalShake(Direction, 0.3f);
                StartShaking();
                DirectionalLandParticles();
                yield return 0.2f;
                StopShaking();

                if (CollideCheck<SolidTiles>(Position + Direction))
                    break;
                while (CollideCheck<Platform>(Position + Direction))
                    yield return 0.1f;
            }

            Safe = true;
        }

        public void DirectionalShakeParticles() {
            Vector2 dir = Direction.FourWayNormal();

            switch (dir) {
                case { X: 1f }:
                    for (int i = 2; i < Height; i += 4) {
                        if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(-2f, i)))
                            SceneAs<Level>().Particles.Emit(P_FallDustA, 2, new Vector2(X + 2f, Y + i), Vector2.One * 4f, 0f);

                        SceneAs<Level>().Particles.Emit(P_FallDustB, 2, new Vector2(X + 2f, Y + i), Vector2.One * 4f, 0.1f);
                    }
                    break;
                case { X: -1f }:
                    for (int i = 2; i < Height; i += 4) {
                        if (Scene.CollideCheck<Solid>(TopRight + new Vector2(2f, i)))
                            SceneAs<Level>().Particles.Emit(P_FallDustA, 2, new Vector2(Right - 2f, Y + i), Vector2.One * 4f, MathF.PI);

                        SceneAs<Level>().Particles.Emit(P_FallDustB, 2, new Vector2(Right - 2f, Y + i), Vector2.One * 4f, MathF.PI - 0.1f);
                    }
                    break;
                case { Y: -1f }:
                    for (int i = 2; i < Width; i += 4) {
                        if (Scene.CollideCheck<Solid>(BottomLeft + new Vector2(i, 2f)))
                            SceneAs<Level>().Particles.Emit(P_FallDustA, 2, new Vector2(X + i, Bottom - 4f), Vector2.One * 4f, -MathF.PI / 2f);

                        SceneAs<Level>().Particles.Emit(P_FallDustB, 2, new Vector2(X + i, Bottom - 2f), Vector2.One * 4f);
                    }
                    break;
                default:
                    for (int i = 2; i < Width; i += 4) {
                        if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -2f)))
                            SceneAs<Level>().Particles.Emit(P_FallDustA, 2, new Vector2(X + i, Y), Vector2.One * 4f, MathF.PI / 2f);

                        SceneAs<Level>().Particles.Emit(P_FallDustB, 2, new Vector2(X + i, Y), Vector2.One * 4f);
                    }
                    break;
            }
        }

        public void DirectionalLandParticles() {
            Vector2 dir = Direction.FourWayNormal();

            ParticleType P_DirectionalLandDust = new(P_LandDust) {
                Acceleration = dir * -30f
            };

            switch (dir) {
                case { X: 1f }:
                    for (int i = 2; i <= Height; i += 4) {
                        if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(3f, i))) {
                            SceneAs<Level>().ParticlesFG.Emit(P_FallDustA, 1, new Vector2(Right, Y + i), Vector2.One * 4f, 0f);
                            float direction = i >= Height / 2f ? MathF.PI / 2f : -MathF.PI / 2f;
                            SceneAs<Level>().ParticlesFG.Emit(P_DirectionalLandDust, 1, new Vector2(Right, Y + i), Vector2.One * 4f, direction);
                        }
                    }
                    break;
                case { X: -1f }:
                    for (int i = 2; i <= Height; i += 4) {
                        if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(-3f, i))) {
                            SceneAs<Level>().ParticlesFG.Emit(P_FallDustA, 1, new Vector2(X, Y + i), Vector2.One * 4f, 0f);
                            float direction = i >= Height / 2f ? MathF.PI / 2f : -MathF.PI / 2f;
                            SceneAs<Level>().ParticlesFG.Emit(P_DirectionalLandDust, 1, new Vector2(X, Y + i), Vector2.One * 4f, direction);
                        }
                    }
                    break;
                case { Y: -1f }:
                    for (int i = 2; i <= Width; i += 4) {
                        if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -3f))) {
                            SceneAs<Level>().ParticlesFG.Emit(P_FallDustA, 1, new Vector2(X + i, Y), Vector2.One * 4f, -MathF.PI / 2f);
                            float direction = i >= Width / 2f ? 0f : MathF.PI;
                            SceneAs<Level>().ParticlesFG.Emit(P_DirectionalLandDust, 1, new Vector2(X + i, Y), Vector2.One * 4f, direction);
                        }
                    }
                    break;
                default:
                    LandParticles();
                    break;
            }
        }

        internal static void Initialize() {
            P_HitFallDust = new(P_FallDustB) {
                SpeedMin = 18f,
                SpeedMax = 24f,
                LifeMin = 0.4f,
                LifeMax = 0.55f,
                Acceleration = Vector2.UnitY * 15f
            };
        }
    }
}
