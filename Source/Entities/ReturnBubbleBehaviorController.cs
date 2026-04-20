using FMOD.Studio;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
[CustomEntity("SorbetHelper/ReturnBubbleBehaviorController")]
public class ReturnBubbleBehaviorController : Entity
{
    private class CassetteFlyOptions(EntityData data)
    {
        public enum CollisionModes { Vanilla, SquishFix, TriggersOnly, NoCollide }

        public readonly float Time = data.Float("time", 1f / 1.6f);
        public readonly float Speed = data.Float("speed", 192f);
        public readonly bool UseSpeed = data.Bool("useSpeed", false);
        public readonly Ease.Easer Easer = data.Easer("easing", Ease.SineInOut);
        public readonly bool SmoothCamera = data.Bool("smoothCamera", false);
        public readonly bool RefillDash = data.Bool("refillDash", false);
        public readonly CollisionModes CollisionMode = data.Enum("collisionMode", CollisionModes.Vanilla);
    }

    [Tracked]
    private class CassetteFlyOptionsComponent() : Component(false, false)
    {
        public CassetteFlyOptions Options;

        public bool PlayerYOffsetApplied;
    }

    private readonly CassetteFlyOptions options;

    public ReturnBubbleBehaviorController(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        options = new CassetteFlyOptions(data);
        // hmm
        LoadLazyHooksIfNeeded(options);
    }

    #region Hooks

    [OnLoad]
    internal static void Load()
    {
        On.Celeste.Player.CassetteFlyCoroutine += On_CassetteFlyCoroutine;
        On.Celeste.Player.CassetteFlyEnd += On_CassetteFlyEnd;
        On.Celeste.Audio.CreateInstance += On_Audio_CreateInstance;
        On.Celeste.Player.OnSquish += On_OnSquish;
    }

    [OnUnload]
    internal static void Unload()
    {
        On.Celeste.Player.CassetteFlyCoroutine -= On_CassetteFlyCoroutine;
        On.Celeste.Player.CassetteFlyEnd -= On_CassetteFlyEnd;
        On.Celeste.Audio.CreateInstance -= On_Audio_CreateInstance;
        On.Celeste.Player.OnSquish -= On_OnSquish;

        UnloadLazyHooksIfNeeded();
    }

    private static bool collideCheckHookLoaded = false;

    private static void LoadLazyHooksIfNeeded(CassetteFlyOptions options)
    {
        if (!collideCheckHookLoaded
            && options.CollisionMode is CassetteFlyOptions.CollisionModes.NoCollide or CassetteFlyOptions.CollisionModes.TriggersOnly)
        {
            IL.Monocle.Collide.Check_Entity_Entity += IL_Collide_Check;
            collideCheckHookLoaded = true;
        }
    }

    private static void UnloadLazyHooksIfNeeded()
    {
        if (collideCheckHookLoaded)
        {
            IL.Monocle.Collide.Check_Entity_Entity -= IL_Collide_Check;
            collideCheckHookLoaded = false;
        }
    }

    private static IEnumerator On_CassetteFlyCoroutine(On.Celeste.Player.orig_CassetteFlyCoroutine orig, Player self)
    {
        CassetteFlyOptions options = self.Scene.Tracker.GetEntity<ReturnBubbleBehaviorController>()?.options;
        CassetteFlyOptionsComponent cassetteFlyComponent = self.GetComponentFromTracker<CassetteFlyOptionsComponent>();

        if (options is null)
        {
            cassetteFlyComponent?.Options = null;

            yield return new SwapImmediately(orig(self));
            yield break;
        }

        if (cassetteFlyComponent is null)
            self.Add(cassetteFlyComponent = new CassetteFlyOptionsComponent());
        cassetteFlyComponent.Options = options;

        cassetteFlyComponent.PlayerYOffsetApplied = true;

        if (cassetteFlyComponent.Options.RefillDash)
            self.RefillDash();

        self.level.CanRetry = false;
        self.level.FormationBackdrop.Display = true;
        self.level.FormationBackdrop.Alpha = 0.5f;
        self.Sprite.Scale = new Vector2(1.25f);
        self.Depth = Depths.FormationSequences;

        Audio.Play("event:/sorbethelper/sfx/bubblereturn_split/start", self.level.Camera.GetCenter());

        yield return 0.4f;

        Audio.Play("event:/sorbethelper/sfx/bubblereturn_split/slide", self.level.Camera.GetCenter());

        float time = cassetteFlyComponent.Options.UseSpeed
            ? self.cassetteFlyCurve.GetLengthParametric(100) / cassetteFlyComponent.Options.Speed
            : cassetteFlyComponent.Options.Time;

        while (self.cassetteFlyLerp < 1f)
        {
            if (self.level.OnInterval(0.03f))
                self.level.Particles.Emit(Player.P_CassetteFly, 2, self.Center, Vector2.One * 4f);

            self.cassetteFlyLerp = Calc.Approach(self.cassetteFlyLerp, 1f, Engine.DeltaTime / time);
            self.Position = self.cassetteFlyCurve.GetPoint(cassetteFlyComponent.Options.Easer(self.cassetteFlyLerp)).Floor();

            if (cassetteFlyComponent.Options.SmoothCamera)
            {
                const float catchupSpeed = 10f;

                Vector2 position = self.level.Camera.Position;
                Vector2 cameraTarget = self.CameraTarget;

                self.level.Camera.Position = position + (cameraTarget - position) * (1f - (float)Math.Pow(0.01f / catchupSpeed, Engine.DeltaTime));
            }
            else
            {
                self.level.Camera.Position = self.CameraTarget;
            }

            yield return null;
        }

        self.Position = self.cassetteFlyCurve.End;
        self.Sprite.Scale = new Vector2(1.25f);
        self.Sprite.Play("fallFast");

        if (cassetteFlyComponent.PlayerYOffsetApplied)
        {
            self.Sprite.Y -= 5f;
            cassetteFlyComponent.PlayerYOffsetApplied = false;
        }

        Audio.Play("event:/sorbethelper/sfx/bubblereturn_split/pop", self.level.Camera.GetCenter());

        yield return 0.2f;

        self.level.CanRetry = true;
        self.level.FormationBackdrop.Display = false;
        self.level.FormationBackdrop.Alpha = 0.5f;
        self.StateMachine.State = 0;
        self.Depth = Depths.Player;
    }

    private static void On_CassetteFlyEnd(On.Celeste.Player.orig_CassetteFlyEnd orig, Player self)
    {
        orig(self);

        if (self.GetComponentFromTracker<CassetteFlyOptionsComponent>() is not { } cassetteFlyComponent)
            return;

        // in case the routine was interrupted
        if (cassetteFlyComponent.PlayerYOffsetApplied)
        {
            self.Sprite.Y -= 5f;
            cassetteFlyComponent.PlayerYOffsetApplied = false;
        }

        self.level.CanRetry = true;
        if (self.Depth == Depths.FormationSequences)
            self.Depth = Depths.Player;
        self.level.FormationBackdrop.Display = false;
        self.level.FormationBackdrop.Alpha = 0.5f;
    }

    // we need to mute the non-split vanilla bubble event here, since instead of something like Player.StartCassetteFly playing it,
    // everywhere that calls it plays the sound manually ?? for some reason
    // this also means we check if a controller exists rather than the component's options, since StCassetteFly hasn't actually been entered yet
    private static EventInstance On_Audio_CreateInstance(On.Celeste.Audio.orig_CreateInstance orig, string path, Vector2? position = null)
    {
        if (path == SFX.game_gen_cassette_bubblereturn
            && Engine.Scene.Tracker.GetEntity<ReturnBubbleBehaviorController>() is not null)
            return null;

        return orig(path, position);
    }

    private static void On_OnSquish(On.Celeste.Player.orig_OnSquish orig, Player self, CollisionData data)
    {
        if (self.StateMachine.State == Player.StCassetteFly
            && self.GetComponentFromTracker<CassetteFlyOptionsComponent>()?.Options is { CollisionMode: CassetteFlyOptions.CollisionModes.SquishFix })
            return;

        orig(self, data);
    }

    // this suckssss
    private static void IL_Collide_Check(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        ILLabel returnLabel = null;
        if (!cursor.TryGotoNextBestFit(MoveType.After,
            instr => instr.MatchLdfld<Entity>(nameof(Collidable)),
            instr => instr.MatchBrfalse(out returnLabel)))
            throw new HookHelper.HookException(il, "Unable to find `Collidable` check to modify.");

        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitDelegate(ShouldDisableCollisionForCassetteFlyPlayer);
        cursor.EmitBrtrue(returnLabel);

        return;

        static bool ShouldDisableCollisionForCassetteFlyPlayer(Entity a, Entity b)
        {
            if (a is Player p1)
            {
                if (p1.StateMachine.State != Player.StCassetteFly)
                    return false;

                CassetteFlyOptions options = p1.GetComponentFromTracker<CassetteFlyOptionsComponent>()?.Options;
                if (options is null)
                    return false;

                if (options.CollisionMode == CassetteFlyOptions.CollisionModes.TriggersOnly)
                    return b is not Trigger;

                return options.CollisionMode == CassetteFlyOptions.CollisionModes.NoCollide;
            }

            if (b is Player p2)
            {
                if (p2.StateMachine.State != Player.StCassetteFly)
                    return false;

                CassetteFlyOptions options = p2.GetComponentFromTracker<CassetteFlyOptionsComponent>()?.Options;
                if (options is null)
                    return false;

                if (options.CollisionMode == CassetteFlyOptions.CollisionModes.TriggersOnly)
                    return a is not Trigger;

                return options.CollisionMode == CassetteFlyOptions.CollisionModes.NoCollide;
            }

            return false;
        }
    }

    #endregion
}
