using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste;
using MonoMod.Utils;
using FMOD.Studio;
using System.Runtime.CompilerServices;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

public class ReturnBubbleBehavior {
    public enum CollisionModes { Vanilla, DisableSquish, NoCollide }

    public float Time { get; init; } = 1f / 1.6f;
    public float Speed { get; init; } = 192f;
    public bool UseSpeed { get; init; } = false;
    public Ease.Easer Easer { get; init; } = Ease.SineInOut;
    public bool SmoothCamera { get; init; } = false;
    public bool CanSkip { get; init; } = false;
    public bool RefillDash { get; init; } = false;
    public CollisionModes CollisionMode { get; init; } = CollisionModes.Vanilla;

    public static ReturnBubbleBehavior FromData(EntityData data) {
        return new ReturnBubbleBehavior() {
            Time = data.Float("time", 1f / 1.6f),
            Speed = data.Float("speed", 192f),
            UseSpeed = data.Bool("useSpeed", false),
            Easer = data.Easer("easing", Ease.SineInOut),
            SmoothCamera = data.Bool("smoothCamera", false),
            CanSkip = data.Bool("canSkip", false),
            RefillDash = data.Bool("refillDash", false),
            CollisionMode = data.Enum("collisionMode", CollisionModes.Vanilla)
        };
    }
}

[Tracked]
[CustomEntity("SorbetHelper/ReturnBubbleBehaviorController")]
public class ReturnBubbleBehaviorController : Entity {
    public readonly ReturnBubbleBehavior Behavior;

    public ReturnBubbleBehaviorController(EntityData data, Vector2 _) : base() {
        Behavior = ReturnBubbleBehavior.FromData(data);
    }

    private static ReturnBubbleBehavior GetReturnBubbleBehavior(Player player)
        => player is null ? null : player.CollideFirst<ReturnBubbleBehaviorTrigger>()?.Behavior ?? player.Scene.Tracker.GetEntity<ReturnBubbleBehaviorController>()?.Behavior;

    // need to be able to store the current behavior so that the collision check hooks know about it
    // (would this break with like, savestates or something?? should i make an entire component for this one thing or idk is this fine)
    public static ReturnBubbleBehavior GetActiveBehavior(Player player) {
        var data = DynamicData.For(player);
        return data.Get<ReturnBubbleBehavior>("sorbet_cassetteFlyBehavior");
    }
    private static void SetActiveBehavior(Player player, ReturnBubbleBehavior behavior) {
        var data = DynamicData.For(player);
        data.Set("sorbet_cassetteFlyBehavior", behavior);
    }

    // replacing methods like this kinda sucks but im gonna be real theres no way in hell this was ever going to be an ilhook
    private static IEnumerator On_CassetteFlyCoroutine(On.Celeste.Player.orig_CassetteFlyCoroutine orig, Player self) {
        var behavior = GetReturnBubbleBehavior(self);
        SetActiveBehavior(self, behavior);

        if (behavior is null) {
            yield return new SwapImmediately(orig(self));
            yield break;
        }

        if (behavior.RefillDash)
            self.RefillDash();

        // if (behavior.CanSkip) {
        //     self.level.StartCutscene(level => {
        //         self.cassetteFlyLerp = 1f;
        //         self.Position = self.cassetteFlyCurve.End;
        //         level.Camera.Position = level.GetFullCameraTargetAt(self, self.Position);

        //         self.level.Particles.Emit(Player.P_CassetteFly, 8, self.Center, Vector2.One * 4f);
        //         self.Sprite.Scale = new(1.25f);
        //         self.Sprite.Y -= 5f;
        //         Audio.Play("event:/sorbethelper/sfx/bubblereturn_split/pop", self.level.Camera.Position + new Vector2(160f, 90f));

        //         self.level.CanRetry = true;
        //         self.level.FormationBackdrop.Display = false;
        //         self.level.FormationBackdrop.Alpha = 0.5f;
        //         self.StateMachine.State = 0;
        //         self.Depth = Depths.Player;
        //     });
        // }

        self.level.CanRetry = false; // i assume this is for safety? and also bc of the formation backdrop thing
        self.level.FormationBackdrop.Display = true;
        self.level.FormationBackdrop.Alpha = 0.5f;
        self.Sprite.Scale = new(1.25f); // madeline auto scales back to 1x so this just makes a little bounce effect
        self.Depth = Depths.FormationSequences;

        Audio.Play("event:/sorbethelper/sfx/bubblereturn_split/start", self.level.Camera.GetCenter());
        yield return 0.4f;

        Audio.Play("event:/sorbethelper/sfx/bubblereturn_split/slide", self.level.Camera.GetCenter());
        var time = behavior.UseSpeed ? self.cassetteFlyCurve.GetLengthParametric(64) / behavior.Speed : behavior.Time;
        while (self.cassetteFlyLerp < 1f) {
            if (self.level.OnInterval(0.03f))
                self.level.Particles.Emit(Player.P_CassetteFly, 2, self.Center, Vector2.One * 4f);

            self.cassetteFlyLerp = Calc.Approach(self.cassetteFlyLerp, 1f, Engine.DeltaTime / time);
            self.Position = self.cassetteFlyCurve.GetPoint(behavior.Easer(self.cassetteFlyLerp)).Floor(); // floored to prevent non-pixel aligned positions (sorry)

            // camera
            if (behavior.SmoothCamera) {
                // based on non-bubble normal camera movement with a faster catchup speed (1 -> 10)
                Vector2 position = self.level.Camera.Position;
                Vector2 cameraTarget = self.CameraTarget;

                const float catchup = 10f;
                self.level.Camera.Position = position + (cameraTarget - position) * (1f - (float)Math.Pow(0.01f / catchup, Engine.DeltaTime));
            } else {
                self.level.Camera.Position = self.CameraTarget;
            }

            yield return null;
        }

        // dont revert stuff twice so just exit now if skipping cutscene
        // if (self.level.SkippingCutscene)
        //     yield break;

        self.level.EndCutscene();
        self.Position = self.cassetteFlyCurve.End;
        self.Sprite.Scale = new(1.25f);
        self.Sprite.Y -= 5f; // undo the sprite offset which is applied in the begin method for cassettefly. for safety i feel like this should also be checked for in cassettefly end but bweh
        self.Sprite.Play("fallFast");

        Audio.Play("event:/sorbethelper/sfx/bubblereturn_split/pop", self.level.Camera.GetCenter());
        yield return 0.2f;

        self.level.CanRetry = true;
        self.level.FormationBackdrop.Display = false;
        self.level.FormationBackdrop.Alpha = 0.5f;
        self.StateMachine.State = 0;
        self.Depth = Depths.Player;
    }

    // the vanilla bubble sounds are all in one single event, which means that in order for the speed of bubble to be adjusted it needs to be replaced with multiple split ones
    // vanilla also does something funny though where instead of Player.StartCassetteFly playing the sound it's manually played by each entity that calls that
    // so just to be safe im muting it at the source rather than case by case
    // not checking the current "active" behavior since vanilla plays the sfx before updating the player's state
    private static bool MuteVanillaSFX => GetReturnBubbleBehavior(Engine.Scene?.Tracker.GetEntity<Player>()) is not null;
    private static EventInstance On_Audio_CreateInstance(On.Celeste.Audio.orig_CreateInstance orig, string path, Vector2? position = null) {
        if (path is SFX.game_gen_cassette_bubblereturn && MuteVanillaSFX)
            return null;

        return orig(path, position);
    }

    private static void On_OnSquish(On.Celeste.Player.orig_OnSquish orig, Player self, CollisionData data) {
        // only care about behavior if in cassettefly & skip onsquish if behavior exists and has disablesquish set
        if (self.StateMachine.State is Player.StCassetteFly && GetActiveBehavior(self) is { CollisionMode: ReturnBubbleBehavior.CollisionModes.DisableSquish })
            return;

        orig(self, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldDisableCollision(Entity entity) => entity is Player { StateMachine.State: Player.StCassetteFly } player && GetActiveBehavior(player) is { CollisionMode: ReturnBubbleBehavior.CollisionModes.NoCollide };
    // absolutely awful but setting player.collidable to false doesnt prevent the player itself from colliding, and i dont think theres a better way to do this
    // in any case like, i already did this for another mod once so if i ever wanted to port that mechanic here id have to do this anyway
    // would this be faster as an il hook??? because i imagine this method gets called a lot and i dont know if adding layers of on hooks and orig calls is any slower than having the one il patched method
    // could also add hook lazy loading so any maps where this never does anything dont get any extra overhead
    private static bool On_Collide_Check(On.Monocle.Collide.orig_Check_Entity_Entity orig, Entity a, Entity b) {
        if (ShouldDisableCollision(a) || ShouldDisableCollision(b))
            return false;

        return orig(a, b);
    }

    internal static void Load() {
        On.Celeste.Player.CassetteFlyCoroutine += On_CassetteFlyCoroutine;
        On.Celeste.Audio.CreateInstance += On_Audio_CreateInstance;
        On.Celeste.Player.OnSquish += On_OnSquish;
        On.Monocle.Collide.Check_Entity_Entity += On_Collide_Check;
    }

    internal static void Unload() {
        On.Celeste.Player.CassetteFlyCoroutine -= On_CassetteFlyCoroutine;
        On.Celeste.Audio.CreateInstance -= On_Audio_CreateInstance;
        On.Celeste.Player.OnSquish -= On_OnSquish;
        On.Monocle.Collide.Check_Entity_Entity -= On_Collide_Check;
    }
}

[Tracked]
[CustomEntity("SorbetHelper/ReturnBubbleBehaviorTrigger")]
public class ReturnBubbleBehaviorTrigger : Entity {
    public readonly ReturnBubbleBehavior Behavior;

    public ReturnBubbleBehaviorTrigger(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Collider = new Hitbox(data.Width, data.Height);
        Behavior = ReturnBubbleBehavior.FromData(data);
    }
}
