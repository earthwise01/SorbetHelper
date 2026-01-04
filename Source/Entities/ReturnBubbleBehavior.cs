using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Utils;
using MonoMod.Cil;
using FMOD.Studio;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
[CustomEntity("SorbetHelper/ReturnBubbleBehaviorController")]
public class ReturnBubbleBehaviorController : Entity {
    public class ReturnBubbleBehavior {
        public enum CollisionModes { Vanilla, SquishFix, TriggersOnly, NoCollide }

        public float Time { get; init; } = 1f / 1.6f;
        public float Speed { get; init; } = 192f;
        public bool UseSpeed { get; init; } = false;
        public Ease.Easer Easer { get; init; } = Ease.SineInOut;
        public bool SmoothCamera { get; init; } = false;
        // public bool CanSkip { get; init; } = false;
        public bool RefillDash { get; init; } = false;
        public CollisionModes CollisionMode { get; init; } = CollisionModes.Vanilla;

        public bool PlayerYOffsetApplied;

        public static ReturnBubbleBehavior FromData(EntityData data) {
            return new ReturnBubbleBehavior() {
                Time = data.Float("time", 1f / 1.6f),
                Speed = data.Float("speed", 192f),
                UseSpeed = data.Bool("useSpeed", false),
                Easer = data.Easer("easing", Ease.SineInOut),
                SmoothCamera = data.Bool("smoothCamera", false),
                // CanSkip = data.Bool("canSkip", false),
                RefillDash = data.Bool("refillDash", false),
                CollisionMode = data.Enum("collisionMode", CollisionModes.Vanilla)
            };
        }
    }

    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(ReturnBubbleBehaviorController)}";

    private readonly ReturnBubbleBehavior behavior;

    public ReturnBubbleBehaviorController(EntityData data, Vector2 _) : base() {
        behavior = ReturnBubbleBehavior.FromData(data);
    }

    private static ReturnBubbleBehavior GetReturnBubbleBehavior(Scene scene)
        => scene.Tracker.GetEntity<ReturnBubbleBehaviorController>()?.behavior;

    // need to be able to store the current behavior so that the collision check hooks know about it
    // (would this break with like, savestates or something?? should i make an entire component for this one thing or idk is this fine)
    public static ReturnBubbleBehavior GetActiveBehavior(Player player) {
        DynamicData data = DynamicData.For(player);
        return data.Get<ReturnBubbleBehavior>("sorbet_cassetteFlyBehavior");
    }
    private static void SetActiveBehavior(Player player, ReturnBubbleBehavior behavior) {
        DynamicData data = DynamicData.For(player);
        data.Set("sorbet_cassetteFlyBehavior", behavior);
    }

    #region Hooks

    internal static void Load() {
        On.Celeste.Player.CassetteFlyCoroutine += On_CassetteFlyCoroutine;
        On.Celeste.Player.CassetteFlyEnd += On_CassetteFlyEnd;
        On.Celeste.Audio.CreateInstance += On_Audio_CreateInstance;
        On.Celeste.Player.OnSquish += On_OnSquish;
        IL.Monocle.Collide.Check_Entity_Entity += IL_Collide_Check;
    }

    internal static void Unload() {
        On.Celeste.Player.CassetteFlyCoroutine -= On_CassetteFlyCoroutine;
        On.Celeste.Player.CassetteFlyEnd -= On_CassetteFlyEnd;
        On.Celeste.Audio.CreateInstance -= On_Audio_CreateInstance;
        On.Celeste.Player.OnSquish -= On_OnSquish;
        IL.Monocle.Collide.Check_Entity_Entity -= IL_Collide_Check;
    }

    private static IEnumerator On_CassetteFlyCoroutine(On.Celeste.Player.orig_CassetteFlyCoroutine orig, Player self) {
        ReturnBubbleBehavior behavior = GetReturnBubbleBehavior(self.Scene);
        SetActiveBehavior(self, behavior);

        if (behavior is null) {
            yield return new SwapImmediately(orig(self));
            yield break;
        }

        self.level.Session.SetFlag("SorbetHelper_InReturnBubble");
        behavior.PlayerYOffsetApplied = true;

        if (behavior.RefillDash)
            self.RefillDash();

        self.level.CanRetry = false;
        self.level.FormationBackdrop.Display = true;
        self.level.FormationBackdrop.Alpha = 0.5f;
        self.Sprite.Scale = new Vector2(1.25f); // madeline scales back to 1x automatically so this just makes a little bounce effect
        self.Depth = Depths.FormationSequences;

        Audio.Play("event:/sorbethelper/sfx/bubblereturn_split/start", self.level.Camera.GetCenter());
        yield return 0.4f;

        Audio.Play("event:/sorbethelper/sfx/bubblereturn_split/slide", self.level.Camera.GetCenter());
        float time = behavior.UseSpeed ? self.cassetteFlyCurve.GetLengthParametric(100) / behavior.Speed : behavior.Time;
        while (self.cassetteFlyLerp < 1f) {
            if (self.level.OnInterval(0.03f))
                self.level.Particles.Emit(Player.P_CassetteFly, 2, self.Center, Vector2.One * 4f);

            self.cassetteFlyLerp = Calc.Approach(self.cassetteFlyLerp, 1f, Engine.DeltaTime / time);
            self.Position = self.cassetteFlyCurve.GetPoint(behavior.Easer(self.cassetteFlyLerp)).Floor(); // floored to prevent non-pixel aligned positions (sorry)

            // camera
            if (behavior.SmoothCamera) {
                // based on non-bubble normal camera movement, but with a faster catchup speed (1 -> 10)
                Vector2 position = self.level.Camera.Position;
                Vector2 cameraTarget = self.CameraTarget;

                const float catchup = 10f;
                self.level.Camera.Position = position + (cameraTarget - position) * (1f - (float)Math.Pow(0.01f / catchup, Engine.DeltaTime));
            } else {
                self.level.Camera.Position = self.CameraTarget;
            }

            yield return null;
        }

        self.Position = self.cassetteFlyCurve.End;
        self.Sprite.Scale = new Vector2(1.25f);
        // undo the sprite offset which is applied in CassetteFlyBegin. also checked in CassetteFlyEnd !!
        if (behavior.PlayerYOffsetApplied)
            self.Sprite.Y -= 5f; behavior.PlayerYOffsetApplied = false;
        self.Sprite.Play("fallFast");

        Audio.Play("event:/sorbethelper/sfx/bubblereturn_split/pop", self.level.Camera.GetCenter());
        yield return 0.2f;

        self.level.CanRetry = true;
        self.level.FormationBackdrop.Display = false;
        self.level.FormationBackdrop.Alpha = 0.5f;
        self.StateMachine.State = 0;
        self.Depth = Depths.Player;
    }

    private static void On_CassetteFlyEnd(On.Celeste.Player.orig_CassetteFlyEnd orig, Player self) {
        orig(self);

        ReturnBubbleBehavior behavior = GetActiveBehavior(self);
        if (behavior is null)
            return;

        self.level.Session.SetFlag("SorbetHelper_InReturnBubble", false);

        // in case the routine was maybe interrupted
        if (behavior.PlayerYOffsetApplied)
            self.Sprite.Y -= 5f; behavior.PlayerYOffsetApplied = false;

        self.level.CanRetry = true;
        if (self.Depth == Depths.FormationSequences)
            self.Depth = Depths.Player;
        self.level.FormationBackdrop.Display = false;
        self.level.FormationBackdrop.Alpha = 0.5f;
    }

    // the vanilla bubble sounds are all in one single event, which means that in order for the speed of bubble to be adjusted it needs to be replaced with multiple split ones
    // vanilla also does something funny though where instead of Player.StartCassetteFly playing the sound it's manually played by each entity that calls that
    // so just to be safe im muting it at the source rather than case by case
    // not checking the current "active" behavior since vanilla plays the sfx before updating the player's state
    private static bool MuteVanillaSFX => GetReturnBubbleBehavior(Engine.Scene) is not null;
    private static EventInstance On_Audio_CreateInstance(On.Celeste.Audio.orig_CreateInstance orig, string path, Vector2? position = null) {
        if (path is SFX.game_gen_cassette_bubblereturn && MuteVanillaSFX)
            return null;

        return orig(path, position);
    }

    private static void On_OnSquish(On.Celeste.Player.orig_OnSquish orig, Player self, CollisionData data) {
        // only care about behavior if in cassettefly & skip onsquish if behavior exists and has disablesquish set
        if (self.StateMachine.State is Player.StCassetteFly && GetActiveBehavior(self) is { CollisionMode: ReturnBubbleBehavior.CollisionModes.SquishFix })
            return;

        orig(self, data);
    }

    // absolutely awful but setting player.collidable to false wouldnt prevent the player itself from colliding with other entities, and i dont think theres a better way to do this??? or im stupid
    // in any case like, i already did this for another mod once so if i ever wanted to port that mechanic here id have to do this anyway
    // could also add hook lazy loading so any maps where this never does anything dont get any extra overhead
    private static void IL_Collide_Check(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        ILLabel endLabel = null;

        if (!cursor.TryGotoNextBestFit(MoveType.After, i => i.MatchLdfld(out _), i => i.MatchBrfalse(out endLabel))) {
            Logger.Error(LogID, $"failed to inject collision disabling in CIL code for {cursor.Method.Name}!");
            return;
        }

        Logger.Verbose(LogID, $"injecting collision disabling at {cursor.Index} in CIL code for {cursor.Method.Name}");

        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitDelegate(ShouldDisableCollision);
        cursor.EmitBrtrue(endLabel);
    }

    private static bool ShouldDisableCollision(Entity a, Entity b) {
        // looks a bit worse than it is maybe , code in both if checks is the same but i didnt want to include another method call and im not confident in using aggressiveinlining
        // still awful though gladstare
        if (a is Player p1) {
            if (p1.StateMachine.State is not Player.StCassetteFly)
                return false;

            ReturnBubbleBehavior behavior = GetActiveBehavior(p1);
            if (behavior is null)
                return false;

            if (behavior.CollisionMode == ReturnBubbleBehavior.CollisionModes.TriggersOnly)
                return b is not Trigger;

            return behavior.CollisionMode == ReturnBubbleBehavior.CollisionModes.NoCollide;
        }

        if (b is Player p2) {
            if (p2.StateMachine.State is not Player.StCassetteFly)
                return false;

            ReturnBubbleBehavior behavior = GetActiveBehavior(p2);
            if (behavior is null)
                return false;

            if (behavior.CollisionMode == ReturnBubbleBehavior.CollisionModes.TriggersOnly)
                return a is not Trigger;

            return behavior.CollisionMode == ReturnBubbleBehavior.CollisionModes.NoCollide;
        }

        return false;
    }

    #endregion

}
