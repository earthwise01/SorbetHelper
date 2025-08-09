using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/PufferTweaksController")]
[Tracked]
public class PufferTweaksController(EntityData data, Vector2 offset) : Entity(data.Position + offset) {
    private readonly bool FixSquishExplode = data.Bool("fixSquishExplode", false);
    private readonly bool SnapToSpring = data.Bool("snapToSpring", false);
    private readonly float SpringXSpeedThreshold = data.Float("springXSpeedThreshold", 60f);
    private readonly float SpringYSpeedThreshold = data.Float("springYSpeedThreshold", 0f);
    private readonly bool CanBePushedWhileExploded = data.Bool("canBePushedWhileExploded", true);
    private readonly bool CanRespawnWhenHomeBlocked = data.Bool("canRespawnWhenHomeBlocked", true);
    private readonly bool MoreExplodeParticles = data.Bool("moreExplodeParticles", false);

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

        if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { CanBePushedWhileExploded: false })
            self.TreatNaive = self.state == Puffer.States.Gone;
    }

    private static void IL_Update(ILContext il) {
        var cursor = new ILCursor(il) {
            Index = -1
        };

        ILLabel stayInStGoneLabel = null;
        cursor.GotoPrev(MoveType.After, i => i.MatchBgtUn(out stayInStGoneLabel));
        cursor.EmitLdarg0();
        cursor.EmitDelegate(checkForSolid);
        cursor.EmitBrtrue(stayInStGoneLabel);

        static bool checkForSolid(Puffer self) {
            if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is not { CanRespawnWhenHomeBlocked: false })
                return false;

            return self.CollideCheck<Solid>();
        }
    }

    private static void On_OnSquish(On.Celeste.Puffer.orig_OnSquish orig, Puffer self, CollisionData data) {
        if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { FixSquishExplode: true } && (self.state == Puffer.States.Gone || self.cantExplodeTimer > 0f))
            return;

        orig(self, data);
    }

    private static bool On_HitSpring(On.Celeste.Puffer.orig_HitSpring orig, Puffer self, Spring spring) {
        var result = orig(self, spring);

        if (result && self.Scene.Tracker.GetEntity<PufferTweaksController>() is { SnapToSpring: true }) {
            self.MoveToX(spring.CenterX);
            self.MoveToY(spring.CenterY);
        }

        return result;
    }

    private static void IL_HitSpring(ILContext il) {
        var cursor = new ILCursor(il);

        // upwards springs
        cursor.GotoNext(MoveType.After, i => i.MatchLdcR4(0f));
        cursor.EmitLdarg0();
        cursor.EmitDelegate(modifyUpwardsThreshold);

        // right facing springs
        cursor.GotoNext(MoveType.After, i => i.MatchLdcR4(60f));
        cursor.EmitLdarg0();
        cursor.EmitDelegate(modifyRightThreshold);

        // left facing spring
        cursor.GotoNext(MoveType.After, i => i.MatchLdcR4(-60f));
        cursor.EmitLdarg0();
        cursor.EmitDelegate(modifyLeftThreshold);

        static float modifyUpwardsThreshold(float orig, Puffer self) {
            if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { } controller)
                return -controller.SpringYSpeedThreshold;

            return orig;
        }

        static float modifyRightThreshold(float orig, Puffer self) {
            if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { } controller)
                return controller.SpringXSpeedThreshold;

            return orig;
        }

        static float modifyLeftThreshold(float orig, Puffer self) {
            if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is { } controller)
                return -controller.SpringXSpeedThreshold;

            return orig;
        }
    }

    private static void On_Explode(On.Celeste.Puffer.orig_Explode orig, Puffer self) {
        orig(self);

        if (self.Scene.Tracker.GetEntity<PufferTweaksController>() is not { MoreExplodeParticles: true })
            return;

        // might feel like tweaking these later but works alright i guess for now

        var level = self.Scene as Level;
        for (float angle = 0f; angle < MathF.PI * 2f; angle += MathF.PI / 18f) {
            var position = self.Center + Calc.AngleToVector(angle + Calc.Random.Range(-MathF.PI / 90f, MathF.PI / 90f), Calc.Random.Range(12, 18));
            level.ParticlesFG.Emit(Seeker.P_Regen, position, angle);
        }

        for (float angle = 0f; angle < MathF.PI * 2f; angle += MathF.PI / 6f) {
            var position = self.Center + Calc.AngleToVector(angle + Calc.Random.Range(-MathF.PI / 90f, MathF.PI / 90f), Calc.Random.Range(12, 18));
            level.ParticlesFG.Emit(new ParticleType(Player.P_SummitLandB) {
                SpeedMin = 60,
                SpeedMax = 90,
                SpeedMultiplier = 0.25f,
                Acceleration = Vector2.UnitY * -30f,
            }, position, angle);
        }
    }
}