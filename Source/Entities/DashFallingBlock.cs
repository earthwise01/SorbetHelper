using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Components;

namespace Celeste.Mod.SorbetHelper.Entities;

[TrackedAs(typeof(FallingBlock))]
[CustomEntity("SorbetHelper/DashFallingBlock")]
public class DashFallingBlock : CustomFallingBlock {

    /*
        the implementation for scale and hitOffset (and how they affect the appearance of the block) is heavily based on code from Communal Helper's Station Blocks
        https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/Entities/StationBlocks/StationBlock.cs
    */

    private enum FallDashModes { Disabled, Push, Pull }
    private readonly FallDashModes fallDashMode;

    private readonly bool allowWavedash;
    private readonly bool dashCornerCorrection;
    private readonly bool refillDash;

    private Vector2 scale = Vector2.One;
    private Vector2 hitOffset;

    public static ParticleType P_HitFallDust { get; private set; }
    public static ParticleType P_RefillDash { get; private set; }

    public DashFallingBlock(EntityData data, Vector2 offset) : base(data, offset) {
        fallOnTouch = data.Bool("fallOnTouch", false); // override the default to hopefully reduce the chance of breaking stuff
        fallOnStaticMover = data.Bool("fallOnStaticMover", false);

        allowWavedash = data.Bool("allowWavedash", false);
        dashCornerCorrection = data.Bool("dashCornerCorrection", false);
        refillDash = data.Bool("refillDash", false);
        fallDashMode = data.Enum("fallDashMode", FallDashModes.Disabled);

        // make the tilegrid invisible since we want to render it manually later
        tiles.Visible = false;

        // allows disabling dash activation (for some reason)
        if (data.Bool("dashActivated", true))
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

            var level = Scene as Level;

            // if the falling block is set to move in the direction of madeline's dash, update the direction accordingly
            if (fallDashMode != FallDashModes.Disabled) {
                Direction = fallDashMode == FallDashModes.Pull ? -dir : dir;
            }

            // trigger the block
            level.DirectionalShake(dir);
            Triggered = true;
            Audio.Play(impactSfx, Center);

            if (refillDash && player.Dashes < player.MaxDashes) {
                player.RefillDash();
                Audio.Play(SFX.game_gen_diamond_return, player.Center);

                float angle = dir.Angle();
                level.ParticlesFG.Emit(P_RefillDash, 4, player.Center, Vector2.One * 4f, angle - MathF.PI / 2f);
                level.ParticlesFG.Emit(P_RefillDash, 4, player.Center, Vector2.One * 4f, angle + MathF.PI / 2f);
            }

            // emit the dust particles and update the scale and hitOffset
            for (int i = 2; i <= Width; i += 4) {
                if (!Scene.CollideCheck<Solid>(BottomLeft + new Vector2(i, 3f))) {
                    level.Particles.Emit(P_HitFallDust, 1, new Vector2(X + i, Bottom), Vector2.One * 4f);
                    level.Particles.Emit(P_FallDustA, 1, new Vector2(X + i, Bottom), Vector2.One * 4f);
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

    public override void Render() {
        base.Render();

        // TileGrids can't have their scale changed, so we have to render the block manually
        var position = Position + tiles.Position;

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

    internal static void LoadParticles() {
        P_HitFallDust = new(P_FallDustB) {
            SpeedMin = 18f,
            SpeedMax = 24f,
            LifeMin = 0.4f,
            LifeMax = 0.55f,
            Acceleration = Vector2.UnitY * 15f
        };
        P_RefillDash = new(Refill.P_Shatter) {
            SpeedMin = 120f,
            SpeedMax = 190f,
        };
    }
}
