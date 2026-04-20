namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/PufferTweaksController")]
[Tracked]
public class PufferTweaksController(EntityData data, Vector2 offset) : Entity(data.Position + offset)
{
    private readonly bool fixSquishExplode = data.Bool("fixSquishExplode", false);
    private readonly bool snapToSpring = data.Bool("snapToSpring", false);
    private readonly float springXSpeedThreshold = data.Float("springXSpeedThreshold", 60f);
    private readonly float springYSpeedThreshold = data.Float("springYSpeedThreshold", 0f);
    private readonly bool canBePushedWhileExploded = data.Bool("canBePushedWhileExploded", true);
    private readonly bool canRespawnWhenHomeBlocked = data.Bool("canRespawnWhenHomeBlocked", true);
    private readonly bool moreExplodeParticles = data.Bool("moreExplodeParticles", false);

    private static ParticleType P_ExplodeSmoke;

    [OnLoadContent]
    internal static void LoadParticles(bool firstLoad)
    {
        P_ExplodeSmoke = new ParticleType(Player.P_SummitLandB)
        {
            SpeedMin = 60,
            SpeedMax = 90,
            SpeedMultiplier = 0.25f,
            Acceleration = Vector2.UnitY * -30f,
        };
    }

    #region Hooks

    [OnLoad]
    internal static void Load()
    {
        On.Celeste.Puffer.Update += On_Puffer_Update;
        IL.Celeste.Puffer.Update += IL_Puffer_Update;
        On.Celeste.Puffer.OnSquish += On_Puffer_OnSquish;
        On.Celeste.Puffer.HitSpring += On_Puffer_HitSpring;
        IL.Celeste.Puffer.HitSpring += IL_Puffer_HitSpring;
        On.Celeste.Puffer.Explode += On_Puffer_Explode;
    }

    [OnUnload]
    internal static void Unload()
    {
        On.Celeste.Puffer.Update -= On_Puffer_Update;
        IL.Celeste.Puffer.Update -= IL_Puffer_Update;
        On.Celeste.Puffer.OnSquish -= On_Puffer_OnSquish;
        On.Celeste.Puffer.HitSpring -= On_Puffer_HitSpring;
        IL.Celeste.Puffer.HitSpring -= IL_Puffer_HitSpring;
        On.Celeste.Puffer.Explode -= On_Puffer_Explode;
    }

    private static void On_Puffer_Update(On.Celeste.Puffer.orig_Update orig, Puffer self)
    {
        orig(self);

        if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { canBePushedWhileExploded: false })
            self.TreatNaive = self.state == Puffer.States.Gone;
    }

    private static void IL_Puffer_Update(ILContext il)
    {
        ILCursor cursor = new ILCursor(il)
        {
            Index = -1
        };

        ILLabel stayInStGoneLabel = null;
        if (!cursor.TryGotoPrev(MoveType.After, instr => instr.MatchBgtUn(out stayInStGoneLabel)))
            throw new HookHelper.HookException(il, "Failed to find check for whether to stay in `States.Gone` to modify.");

        cursor.EmitLdarg0();
        cursor.EmitDelegate(CheckForSolid);
        cursor.EmitBrtrue(stayInStGoneLabel);

        return;

        static bool CheckForSolid(Puffer self)
        {
            if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is not { canRespawnWhenHomeBlocked: false })
                return false;

            return self.CollideCheck<Solid>();
        }
    }

    private static void On_Puffer_OnSquish(On.Celeste.Puffer.orig_OnSquish orig, Puffer self, CollisionData data)
    {
        if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { fixSquishExplode: true } && (self.state == Puffer.States.Gone || self.cantExplodeTimer > 0f))
            return;

        orig(self, data);
    }

    private static bool On_Puffer_HitSpring(On.Celeste.Puffer.orig_HitSpring orig, Puffer self, Spring spring)
    {
        bool result = orig(self, spring);

        if (result && self.Scene.Tracker.GetEntity<PufferTweaksController>() is { snapToSpring: true })
        {
            self.MoveToX(spring.CenterX);
            self.MoveToY(spring.CenterY);
        }

        return result;
    }

    private static void IL_Puffer_HitSpring(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0f)))
            throw new HookHelper.HookException(il, "Unable to find upwards speed threshold `0f` to modify.");

        cursor.EmitLdarg0();
        cursor.EmitDelegate(ModifyUpwardsThreshold);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(60f)))
            throw new HookHelper.HookException(il, "Unable to find rightwards speed threshold `60f` to modify.");

        cursor.EmitLdarg0();
        cursor.EmitDelegate(ModifyRightThreshold);

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-60f)))
            throw new HookHelper.HookException(il, "Unable to find leftwards speed threshold `-60f` to modify.");

        cursor.EmitLdarg0();
        cursor.EmitDelegate(ModifyLeftThreshold);

        return;

        static float ModifyUpwardsThreshold(float orig, Puffer self)
        {
            if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { } controller)
                return -controller.springYSpeedThreshold;

            return orig;
        }

        static float ModifyRightThreshold(float orig, Puffer self)
        {
            if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { } controller)
                return controller.springXSpeedThreshold;

            return orig;
        }

        static float ModifyLeftThreshold(float orig, Puffer self)
        {
            if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { } controller)
                return -controller.springXSpeedThreshold;

            return orig;
        }
    }

    private static void On_Puffer_Explode(On.Celeste.Puffer.orig_Explode orig, Puffer self)
    {
        orig(self);

        if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is not { moreExplodeParticles: true })
            return;

        // might feel like tweaking these later but works alright i guess for now

        Level level = self.SceneAs<Level>();
        for (float angle = 0f; angle < MathF.PI * 2f; angle += MathF.PI / 18f)
        {
            Vector2 position = self.Center + Calc.AngleToVector(angle + Calc.Random.Range(-MathF.PI / 90f, MathF.PI / 90f), Calc.Random.Range(12, 18));
            level.ParticlesFG.Emit(Seeker.P_Regen, position, angle);
        }

        for (float angle = 0f; angle < MathF.PI * 2f; angle += MathF.PI / 6f)
        {
            Vector2 position = self.Center + Calc.AngleToVector(angle + Calc.Random.Range(-MathF.PI / 90f, MathF.PI / 90f), Calc.Random.Range(12, 18));
            level.ParticlesFG.Emit(P_ExplodeSmoke, position, angle);
        }
    }

    #endregion
}
