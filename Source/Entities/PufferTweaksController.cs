using MonoMod.Cil;

namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/PufferTweaksController")]
[Tracked]
public class PufferTweaksController(EntityData data, Vector2 offset) : Entity(data.Position + offset) {
    private readonly bool fixSquishExplode = data.Bool("fixSquishExplode", false);
    private readonly bool snapToSpring = data.Bool("snapToSpring", false);
    private readonly float springXSpeedThreshold = data.Float("springXSpeedThreshold", 60f);
    private readonly float springYSpeedThreshold = data.Float("springYSpeedThreshold", 0f);
    private readonly bool canBePushedWhileExploded = data.Bool("canBePushedWhileExploded", true);
    private readonly bool canRespawnWhenHomeBlocked = data.Bool("canRespawnWhenHomeBlocked", true);
    private readonly bool moreExplodeParticles = data.Bool("moreExplodeParticles", false);

    #region Hooks

    // this is so many hooks help
    internal static void Load() {
        On.Celeste.Puffer.Update += On_Update;
        IL.Celeste.Puffer.Update += IL_Update;
        On.Celeste.Puffer.OnSquish += On_OnSquish;
        On.Celeste.Puffer.HitSpring += On_HitSpring;
        IL.Celeste.Puffer.HitSpring += IL_HitSpring;
        On.Celeste.Puffer.Explode += On_Explode;
    }

    internal static void Unload() {
        On.Celeste.Puffer.Update -= On_Update;
        IL.Celeste.Puffer.Update -= IL_Update;
        On.Celeste.Puffer.OnSquish -= On_OnSquish;
        On.Celeste.Puffer.HitSpring -= On_HitSpring;
        IL.Celeste.Puffer.HitSpring -= IL_HitSpring;
        On.Celeste.Puffer.Explode -= On_Explode;
    }

    private static void On_Update(On.Celeste.Puffer.orig_Update orig, Puffer self) {
        orig(self);

        if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { canBePushedWhileExploded: false })
            self.TreatNaive = self.state == Puffer.States.Gone;
    }

    private static void IL_Update(ILContext il) {
        ILCursor cursor = new ILCursor(il) {
            Index = -1
        };

        ILLabel stayInStGoneLabel = null;
        cursor.GotoPrev(MoveType.After, i => i.MatchBgtUn(out stayInStGoneLabel));
        cursor.EmitLdarg0();
        cursor.EmitDelegate(CheckForSolid);
        cursor.EmitBrtrue(stayInStGoneLabel);

        return;

        static bool CheckForSolid(Puffer self) {
            if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is not { canRespawnWhenHomeBlocked: false })
                return false;

            return self.CollideCheck<Solid>();
        }
    }

    private static void On_OnSquish(On.Celeste.Puffer.orig_OnSquish orig, Puffer self, CollisionData data) {
        if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { fixSquishExplode: true } && (self.state == Puffer.States.Gone || self.cantExplodeTimer > 0f))
            return;

        orig(self, data);
    }

    private static bool On_HitSpring(On.Celeste.Puffer.orig_HitSpring orig, Puffer self, Spring spring) {
        bool result = orig(self, spring);

        if (result && self.Scene.Tracker.GetEntity<PufferTweaksController>() is { snapToSpring: true }) {
            self.MoveToX(spring.CenterX);
            self.MoveToY(spring.CenterY);
        }

        return result;
    }

    private static void IL_HitSpring(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        // upwards springs
        cursor.GotoNext(MoveType.After, i => i.MatchLdcR4(0f));
        cursor.EmitLdarg0();
        cursor.EmitDelegate(ModifyUpwardsThreshold);

        // right facing springs
        cursor.GotoNext(MoveType.After, i => i.MatchLdcR4(60f));
        cursor.EmitLdarg0();
        cursor.EmitDelegate(ModifyRightThreshold);

        // left facing spring
        cursor.GotoNext(MoveType.After, i => i.MatchLdcR4(-60f));
        cursor.EmitLdarg0();
        cursor.EmitDelegate(ModifyLeftThreshold);

        return;

        static float ModifyUpwardsThreshold(float orig, Puffer self) {
            if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { } controller)
                return -controller.springYSpeedThreshold;

            return orig;
        }

        static float ModifyRightThreshold(float orig, Puffer self) {
            if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { } controller)
                return controller.springXSpeedThreshold;

            return orig;
        }

        static float ModifyLeftThreshold(float orig, Puffer self) {
            if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { } controller)
                return -controller.springXSpeedThreshold;

            return orig;
        }
    }

    private static void On_Explode(On.Celeste.Puffer.orig_Explode orig, Puffer self) {
        orig(self);

        if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is not { moreExplodeParticles: true })
            return;

        // might feel like tweaking these later but works alright i guess for now

        Level level = self.SceneAs<Level>();
        for (float angle = 0f; angle < MathF.PI * 2f; angle += MathF.PI / 18f) {
            Vector2 position = self.Center + Calc.AngleToVector(angle + Calc.Random.Range(-MathF.PI / 90f, MathF.PI / 90f), Calc.Random.Range(12, 18));
            level.ParticlesFG.Emit(Seeker.P_Regen, position, angle);
        }

        for (float angle = 0f; angle < MathF.PI * 2f; angle += MathF.PI / 6f) {
            Vector2 position = self.Center + Calc.AngleToVector(angle + Calc.Random.Range(-MathF.PI / 90f, MathF.PI / 90f), Calc.Random.Range(12, 18));
            level.ParticlesFG.Emit(new ParticleType(Player.P_SummitLandB) {
                SpeedMin = 60,
                SpeedMax = 90,
                SpeedMultiplier = 0.25f,
                Acceleration = Vector2.UnitY * -30f,
            }, position, angle);
        }
    }

    #endregion

}
