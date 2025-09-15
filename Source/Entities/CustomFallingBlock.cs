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
[CustomEntity("SorbetHelper/CustomFallingBlock", "SorbetHelper/CustomGravityFallingBlock = LoadGravity")]
public class CustomFallingBlock : FallingBlock {
    protected readonly string flagOnFall, flagOnLand, triggerFlag;
    protected readonly bool resetFlags;
    protected bool fallOnTouch;
    protected bool fallOnStaticMover;
    protected readonly bool breakDashBlocks;
    protected readonly bool ignoreSolids;
    protected readonly float initialShakeTime, variableShakeTime;
    protected readonly float maxSpeed, acceleration;
    protected readonly string shakeSfx, impactSfx;

    private static readonly Dictionary<string, Vector2> directionToVector = new() {
        {"down", new Vector2(0f, 1f)}, {"up", new Vector2(0f, -1f)}, {"left", new Vector2(-1f, 0f)}, {"right", new Vector2(1f, 0f)}
    };
    public Vector2 Direction;

    // chrono helper gravity falling block switch support
    private readonly bool chronoHelperGravityFallingBlock;
    private bool chronoHelperGravityUp, chronoHelperGravityWasUp;
    private bool chronoHelperHasShaken;

    public static Entity Load(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
        new CustomFallingBlock(entityData, offset, entityData.Bool("chronoHelperGravity", false));

    public static Entity LoadGravity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
        new CustomFallingBlock(entityData, offset, true);

    public CustomFallingBlock(EntityData data, Vector2 offset, bool chronoHelperGravity) : base(data, offset) {
        // remove the Coroutine added by the vanilla falling block
        Remove(Get<Coroutine>());

        Direction = directionToVector[data.Attr("direction", "down").ToLower()];
        flagOnFall = data.Attr("flagOnFall", "");
        flagOnLand = data.Attr("flagOnLand", "");
        triggerFlag = data.Attr("triggerFlag", "");
        resetFlags = data.Bool("resetFlags", false);
        fallOnTouch = data.Bool("fallOnTouch", true);
        fallOnStaticMover = data.Bool("fallOnStaticMover", true);
        breakDashBlocks = data.Bool("breakDashBlocks", true);
        ignoreSolids = data.Bool("ignoreSolids", false);
        initialShakeTime = data.Float("initialShakeTime", 0.2f);
        variableShakeTime = data.Float("variableShakeTime", 0.4f);
        maxSpeed = data.Float("maxSpeed", 160f);
        acceleration = data.Float("acceleration", 500f);
        shakeSfx = data.Attr("shakeSfx", "event:/game/general/fallblock_shake");
        impactSfx = data.Attr("impactSfx", "event:/game/general/fallblock_impact");
        Depth = data.Int("depth", Depth);

        chronoHelperGravityFallingBlock = chronoHelperGravity;
        if (chronoHelperGravity && !ChronoHelperCompat.IsLoaded)
            Logger.Warn(nameof(SorbetHelper), "Trying to load a Custom Gravity Falling Block without chrono helper enabled!");

        Add(new Coroutine(Sequence()));

        // i was going to use a moving block hittable component before i remembered that. right TrackedAs is a Thing i can just use that asdfasd (+ having falling blocks trigger other falling blocks is neat maybe but inconsistent with vanilla)
        // Add(new MovingBlockHittable(OnMovingBlockHit));
    }

    public override void OnStaticMoverTrigger(StaticMover sm) {
        if (fallOnStaticMover) {
            Triggered = true;
        }
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (!resetFlags)
            return;

        var session = (Scene as Level).Session;
        if (!string.IsNullOrEmpty(flagOnFall) && session.GetFlag(flagOnFall))
            session.SetFlag(flagOnFall, false);
        if (!string.IsNullOrEmpty(flagOnLand) && session.GetFlag(flagOnLand))
            session.SetFlag(flagOnLand, false);
        if (!string.IsNullOrEmpty(triggerFlag) && session.GetFlag(triggerFlag))
            session.SetFlag(triggerFlag, false);
    }

    public override void Update() {
        // chronohelper gravity
        if (chronoHelperGravityFallingBlock) {
            chronoHelperGravityWasUp = chronoHelperGravityUp;
            chronoHelperGravityUp = ChronoHelperCompat.SessionGravityModeUp;

            if (chronoHelperGravityUp != chronoHelperGravityWasUp)
                Direction = -Direction;
        }

        // flag trigger
        if (!string.IsNullOrEmpty(triggerFlag) && !HasStartedFalling && !Triggered && (Scene as Level).Session.GetFlag(triggerFlag))
            Triggered = true;

        base.Update();
    }

    private new IEnumerator Sequence() {
        while (!Triggered && (!fallOnTouch || !PlayerFallCheck()))
            yield return null;

        HasStartedFalling = true;
        if (!string.IsNullOrEmpty(flagOnFall))
            (Scene as Level).Session.SetFlag(flagOnFall);

        while (true) {
            Audio.Play(shakeSfx, Center);
            StartShaking();
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

            if (!chronoHelperGravityFallingBlock || !chronoHelperHasShaken)
                yield return initialShakeTime;
            // this is so so awful  butwhen implementing support for chrono helper gravity originally i had some delay before the blocks fell after landing
            //  turns out though chrono helper does Not do that itself and has the blocks fall again instantly but i both had already used the extra delay and found it felt good
            // so for publically releasing this im compromising by keeping that behavior by default but disabling the delay for parity if a flag is set
            else if (!(Scene as Level).Session.GetFlag("SorbetHelper_CorrectChronoHelperParity"))
                yield return 0.1f;

            float shakeTimer = variableShakeTime;
            while (shakeTimer > 0f && PlayerWaitCheck() && (!chronoHelperGravityFallingBlock || !chronoHelperHasShaken)) {
                yield return null;
                shakeTimer -= Engine.DeltaTime;
            }
            chronoHelperHasShaken = true;

            StopShaking();
            DirectionalShakeParticles();

            var speed = new Vector2(0f, 0f);
            while (true) {
                Level level = Scene as Level;
                speed.X = Calc.Approach(speed.X, Direction.X * maxSpeed, acceleration * Engine.DeltaTime);
                speed.Y = Calc.Approach(speed.Y, Direction.Y * maxSpeed, acceleration * Engine.DeltaTime);

                if (ignoreSolids) {
                    MoveV(speed.Y * Engine.DeltaTime);
                    MoveH(speed.X * Engine.DeltaTime);
                } else {
                    if (MoveVCollideSolids(speed.Y * Engine.DeltaTime, thruDashBlocks: breakDashBlocks))
                        break;

                    if (MoveHCollideSolids(speed.X * Engine.DeltaTime, thruDashBlocks: breakDashBlocks))
                        break;
                }

                // checks whether the falling block fell out of bounds
                // all of these checks are done on any custom falling block regardless of its direction so hopefully that wont break anything somewhere
                if (Top > level.Bounds.Bottom + 16 || Bottom < level.Bounds.Top - 16 || Right < level.Bounds.Left - 16 || Left > level.Bounds.Right + 16 ||
                ((Top > level.Bounds.Bottom - 1 || Bottom < level.Bounds.Top + 1 || Right < level.Bounds.Left + 1 || Left > level.Bounds.Right - 1) && CollideCheck<Solid>(Position + Direction))) {
                    Collidable = Visible = false;
                    yield return 0.2f;

                    // checks whether the falling block fell into a screen transition (i think)
                    if (level.Session.MapData.CanTransitionTo(level, new Vector2(Center.X, Bottom + 12f)) || level.Session.MapData.CanTransitionTo(level, new Vector2(Center.X, Top - 12f)) ||
                    level.Session.MapData.CanTransitionTo(level, new Vector2(Left - 12f, Center.Y)) || level.Session.MapData.CanTransitionTo(level, new Vector2(Right + 12f, Center.Y))) {
                        yield return 0.2f;
                        level.Shake();
                        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                    }

                    RemoveSelf();
                    DestroyStaticMovers();
                    yield break;
                }

                yield return null;
            }

            if (!string.IsNullOrEmpty(flagOnLand))
                (Scene as Level).Session.SetFlag(flagOnLand);
            Audio.Play(impactSfx, BottomCenter);
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            (Scene as Level).DirectionalShake(Direction, 0.3f);
            StartShaking();
            DirectionalLandParticles();
            yield return 0.2f;
            StopShaking();

            if (chronoHelperGravityFallingBlock)
                while (CollideCheck<SolidTiles>(Position + Direction))
                    yield return null;
            else if (CollideCheck<SolidTiles>(Position + Direction))
                break;

            // could be a simple collidecheck but this fixes the extremely niche case of not falling if 1. sideways 2. next to a jumpthru and 3. active but landed (for using attached jumpthrus on sideways gravity falling blocks)
            while (CollideFirst<Platform>(Position + Direction) is { } platform && (Direction.X == 0f || platform is not JumpThru)) {
                yield return 0.1f;
            }

            // makes platforms moving out of the way still behave like normal for gravity falling blocks
            if (chronoHelperGravityFallingBlock && chronoHelperGravityUp == chronoHelperGravityWasUp)
                chronoHelperHasShaken = false;
        }

        Safe = true;
    }

    public void DirectionalShakeParticles() {
        var dir = Direction.FourWayNormal();

        var level = Scene as Level;
        switch (dir) {
            case { X: 1f }:
                for (int i = 2; i < Height; i += 4) {
                    if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(-2f, i)))
                        level.Particles.Emit(P_FallDustA, 2, new Vector2(X + 2f, Y + i), Vector2.One * 4f, 0f);

                    level.Particles.Emit(P_FallDustB, 2, new Vector2(X + 2f, Y + i), Vector2.One * 4f, 0.1f);
                }
                break;
            case { X: -1f }:
                for (int i = 2; i < Height; i += 4) {
                    if (Scene.CollideCheck<Solid>(TopRight + new Vector2(2f, i)))
                        level.Particles.Emit(P_FallDustA, 2, new Vector2(Right - 2f, Y + i), Vector2.One * 4f, MathF.PI);

                    level.Particles.Emit(P_FallDustB, 2, new Vector2(Right - 2f, Y + i), Vector2.One * 4f, MathF.PI - 0.1f);
                }
                break;
            case { Y: -1f }:
                for (int i = 2; i < Width; i += 4) {
                    if (Scene.CollideCheck<Solid>(BottomLeft + new Vector2(i, 2f)))
                        level.Particles.Emit(P_FallDustA, 2, new Vector2(X + i, Bottom - 4f), Vector2.One * 4f, -MathF.PI / 2f);

                    level.Particles.Emit(P_FallDustB, 2, new Vector2(X + i, Bottom - 2f), Vector2.One * 4f);
                }
                break;
            default:
                for (int i = 2; i < Width; i += 4) {
                    if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -2f)))
                        level.Particles.Emit(P_FallDustA, 2, new Vector2(X + i, Y), Vector2.One * 4f, MathF.PI / 2f);

                    level.Particles.Emit(P_FallDustB, 2, new Vector2(X + i, Y), Vector2.One * 4f);
                }
                break;
        }
    }

    public void DirectionalLandParticles() {
        var dir = Direction.FourWayNormal();

        var P_DirectionalLandDust = new ParticleType(P_LandDust) {
            Acceleration = dir * -30f
        };

        var level = Scene as Level;
        switch (dir) {
            case { X: 1f }:
                for (int i = 2; i <= Height; i += 4) {
                    if (Scene.CollideCheck<Solid>(TopRight + new Vector2(3f, i))) {
                        level.ParticlesFG.Emit(P_FallDustA, 1, new Vector2(Right, Y + i), Vector2.One * 4f, 0f);
                        float direction = i >= Height / 2f ? MathF.PI / 2f : -MathF.PI / 2f;
                        level.ParticlesFG.Emit(P_DirectionalLandDust, 1, new Vector2(Right, Y + i), Vector2.One * 4f, direction);
                    }
                }
                break;
            case { X: -1f }:
                for (int i = 2; i <= Height; i += 4) {
                    if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(-3f, i))) {
                        level.ParticlesFG.Emit(P_FallDustA, 1, new Vector2(X, Y + i), Vector2.One * 4f, 0f);
                        float direction = i >= Height / 2f ? MathF.PI / 2f : -MathF.PI / 2f;
                        level.ParticlesFG.Emit(P_DirectionalLandDust, 1, new Vector2(X, Y + i), Vector2.One * 4f, direction);
                    }
                }
                break;
            case { Y: -1f }:
                for (int i = 2; i <= Width; i += 4) {
                    if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -3f))) {
                        level.ParticlesFG.Emit(P_FallDustA, 1, new Vector2(X + i, Y), Vector2.One * 4f, -MathF.PI / 2f);
                        float direction = i >= Width / 2f ? 0f : MathF.PI;
                        level.ParticlesFG.Emit(P_DirectionalLandDust, 1, new Vector2(X + i, Y), Vector2.One * 4f, direction);
                    }
                }
                break;
            default:
                LandParticles();
                break;
        }
    }
}
