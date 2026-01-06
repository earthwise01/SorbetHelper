using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
[CustomEntity("SorbetHelper/KillZone")]
public class KillZone : Entity {
    private readonly string flag;
    private readonly bool inverted;
    private readonly bool fastKill;

    public KillZone(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Collider = new Hitbox(data.Width, data.Height);

        flag = data.Attr("flag");
        inverted = data.Bool("inverted");
        fastKill = data.Bool("fastKill", false);
        bool collideHoldables = data.Bool("collideHoldables", false);

        Add(new LedgeBlocker());
        Add(new PlayerCollider(OnPlayer));

        if (collideHoldables)
            Add(new HoldableCollider(OnHoldable));
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (!string.IsNullOrEmpty(flag) && !SceneAs<Level>().Session.GetFlag(flag, inverted))
            Collidable = false;
    }

    public override void Update() {
        base.Update();

        if (!string.IsNullOrEmpty(flag))
            Collidable = SceneAs<Level>().Session.GetFlag(flag, inverted);
    }

    private void OnPlayer(Player player) {
        if (fastKill)
            player.Die(Vector2.Zero);
        else
            player.Die((player.Position - Center).SafeNormalize());
    }

    private void OnHoldable(Holdable holdable) {
        // special casing !!!!!
        switch (holdable.Entity) {
            case TheoCrystal theo:
                theo.Die();
                break;
            case Glider glider:
                // (see lower il hook) do nothing here weh
                // or never mind im giving up
                FakeGliderDieIGiveUp(glider);
                break;
            default:
                holdable.OnHitSpinner(this);
                break;
        } // why did i get a crash here and why was it only once and never again what
    }

    // i dont think this works with mhh respawning jellies but   oh my godwhatever ill do this better later if i feel like it
    private static void FakeGliderDieIGiveUp(Glider glider) {
        if (glider.destroyed)
            return;

        glider.destroyed = true;
        glider.Collidable = false;
        if (glider.Hold.IsHeld) {
            Vector2 holderSpeed = glider.Hold.Holder.Speed;
            glider.Hold.Holder.Drop();
            glider.Speed = holderSpeed * 0.333f;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        }

        glider.Add(new Coroutine(glider.DestroyAnimationRoutine()));
    }
    // whyyy do jellies not have a normal Die method </3 this sucks
    //     internal static void Load() {
    //         IL.Celeste.Glider.Update += IL_Glider_Update;
    //     }
    //     internal static void Unload() {
    //         IL.Celeste.Glider.Update -= IL_Glider_Update;
    //     }

            // oka y nevermind this crashes yayyy
    //     private static void IL_Glider_Update(ILContext il) {
    // /*         var cursor = new ILCursor(il);

    //         var shouldDestroyVariable = new VariableDefinition(il.Import(typeof(bool)));
    //         il.Body.Variables.Add(shouldDestroyVariable);

    //         var destroyLabel = cursor.DefineLabel();

    //         cursor.GotoNext(MoveType.After, i => i.MatchLdfld<Glider>(nameof(Glider.destroyed)), i => i.MatchBrtrue(out _));
    //         cursor.EmitLdarg0();
    //         cursor.EmitDelegate(checkHoldableKillZones);
    //         cursor.EmitBrtrue(destroyLabel);

    //         // make the brtrue jump to where the glider is destroyed
    //         cursor.GotoNext(MoveType.Before, i => i.MatchStfld<Glider>(nameof(Glider.destroyed)));
    //         cursor.GotoPrev(MoveType.After, i => i.MatchBrfalse(out _));
    //         cursor.MarkLabel(destroyLabel);

    //         Console.WriteLine(il);

    //         static bool checkHoldableKillZones(Glider self) {
    //             var killZones = self.Scene.Tracker.GetEntities<KillZone>();

    //             foreach (var killZone in killZones) {
    //                 if (((KillZone)killZone).collideHoldables && self.CollideCheck(killZone))
    //                     return true;
    //             }

    //             return false;
    //         } */
    //     }
}
