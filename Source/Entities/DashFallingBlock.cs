using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities {

    [Tracked(false)]
    [CustomEntity("SorbetHelper/DashFallingBlock")]
    public class DashFallingBlock : FallingBlock {

        /*
            the implementation for scale and hitOffset (and how they affect the appearance of the block) is heavily based on code from Communal Helper's Station Blocks
            https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/Entities/StationBlocks/StationBlock.cs
        */

        private enum FallDashModes { Disabled, Push, Pull }

        private readonly string shakeSfx;
        private readonly string impactSfx;
        public bool fallOnTouch;
        public bool fallOnStaticMover;
        public bool allowWavedash;
        public bool dashCornerCorrection;

        private Vector2 scale = Vector2.One;
        private Vector2 hitOffset;

        private static readonly Dictionary<string, Vector2> directionToVector = new Dictionary<string, Vector2>() {
            {"down", new Vector2(0f, 1f)}, {"up", new Vector2(0f, -1f)}, {"left", new Vector2(-1f, 0f)}, {"right", new Vector2(1f, 0f)}
        };
        public Vector2 direction;
        private readonly FallDashModes fallDashMode;

        public DashFallingBlock(EntityData data, Vector2 offset) : base(data, offset) {
            // remove the Coroutine added by the vanilla falling block
            Remove(Get<Coroutine>());

            shakeSfx = data.Attr("shakeSfx", "event:/game/general/fallblock_shake");
            impactSfx = data.Attr("impactSfx", "event:/game/general/fallblock_impact");
            fallOnTouch = data.Bool("fallOnTouch", false);
            fallOnStaticMover = data.Bool("fallOnStaticMover", false);
            allowWavedash = data.Bool("allowWavedash", false);
            dashCornerCorrection = data.Bool("dashCornerCorrection", false);
            base.Depth = data.Int("depth", base.Depth);
            direction = directionToVector[data.Attr("direction", "down").ToLower()];
            fallDashMode = data.Enum<FallDashModes>("fallDashMode", FallDashModes.Disabled);

            // make the tilegrid invisible since we want to render it manually later
            tiles.Visible = false;

            Add(new Coroutine(Sequence()));

            OnDashCollide = OnDashCollision;
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
                    direction = fallDashMode == FallDashModes.Pull ? -dir : dir;
                }

                // trigger the block
                (Scene as Level).DirectionalShake(dir);
                Triggered = true;
                Audio.Play(impactSfx, base.Center);

                // emit the dust particles and update the scale and hitOffset
                for (int i = 2; i <= base.Width; i += 4) {
                    if (!base.Scene.CollideCheck<Solid>(base.BottomLeft + new Vector2(i, 3f))) {
                        SceneAs<Level>().Particles.Emit(P_FallDustB, 1, new Vector2(base.X + i, base.Bottom), Vector2.One * 4f);
                        SceneAs<Level>().Particles.Emit(P_FallDustA, 1, new Vector2(base.X + i, base.Bottom), Vector2.One * 4f);
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
                for (int i = 2; i < Width; i += 4) {
                    if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -2f)))
                        SceneAs<Level>().Particles.Emit(P_FallDustA, 2, new Vector2(X + i, Y), Vector2.One * 4f, (float)Math.PI / 2f);

                    SceneAs<Level>().Particles.Emit(P_FallDustB, 2, new Vector2(X + i, Y), Vector2.One * 4f);
                }

                float speed = 0f;
                float maxSpeed = 160f;
                while (true) {
                    Level level = SceneAs<Level>();
                    speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);

                    if (MoveVCollideSolids(speed * direction.Y * Engine.DeltaTime, thruDashBlocks: true))
                        break;

                    if (MoveHCollideSolids(speed * direction.X * Engine.DeltaTime, thruDashBlocks: true))
                        break;

                    // checks whether the falling block fell out of bounds
                    // all of these checks are done on any dash falling block regardless of its direction so hopefully that wont break anything somewhere
                    if (Top > level.Bounds.Bottom + 16 || Bottom < level.Bounds.Top - 16 || Right < level.Bounds.Left - 16 || Left > level.Bounds.Right + 16 ||
                    ((Top > level.Bounds.Bottom - 1 || Bottom < level.Bounds.Top + 1 || Right < level.Bounds.Left + 1 || Left > level.Bounds.Right - 1) && CollideCheck<Solid>(Position + direction))) {
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
                SceneAs<Level>().DirectionalShake(direction, 0.3f);
                StartShaking();
                LandParticles();
                yield return 0.2f;
                StopShaking();

                if (CollideCheck<SolidTiles>(Position + direction))
                    break;
                while (CollideCheck<Platform>(Position + direction))
                    yield return 0.1f;
            }

            Safe = true;
        }
    }
}
