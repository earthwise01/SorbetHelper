using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/DashGateBlock")]
[Tracked]
public class DashGateBlock : GateBlock {
    private enum Axes {
        Both,
        Horizontal,
        Vertical
    }

    private readonly MTexture mainTexture, lightsTexture;

    private readonly bool canMoveVertically;
    private readonly bool canMoveHorizontally;
    private readonly bool allowWavedash;
    private readonly bool dashCornerCorrection;
    private readonly bool refillDash;

    private float activationFlash;

    private readonly ParticleType P_RefillDash;
    private readonly ParticleType P_RefillDashReturn;

    public DashGateBlock(EntityData data, Vector2 offset) : base(data, offset) {
        allowWavedash = data.Bool("allowWavedash", false);
        dashCornerCorrection = data.Bool("dashCornerCorrection", false);
        refillDash = data.Bool("refillDash", false);

        Axes axes = data.Enum("axes", Axes.Both);
        string blockSprite = data.Attr("blockSprite", "SorbetHelper/gateblock/dash/block");
        switch (axes) {
            default:
                canMoveHorizontally = canMoveVertically = true;
                break;
            case Axes.Horizontal:
                blockSprite += "_h";
                canMoveHorizontally = true;
                canMoveVertically = false;
                break;
            case Axes.Vertical:
                blockSprite += "_v";
                canMoveHorizontally = false;
                canMoveVertically = true;
                break;
        }
        mainTexture = GFX.Game[$"objects/{blockSprite}"];
        lightsTexture = GFX.Game[$"objects/{blockSprite}_lights"];

        OnDashCollide = OnDashed;

        P_RefillDash = new ParticleType(Refill.P_Shatter) {
            Color = P_Activate.Color2,
            Color2 = P_Activate.Color,
            SpeedMin = 120f,
            SpeedMax = 190f
        };
        P_RefillDashReturn = new ParticleType(P_RefillDash) {
            Color = P_ActivateReturn.Color2,
            Color2 = P_ActivateReturn.Color
        };
    }

    public DashCollisionResults OnDashed(Player player, Vector2 dir) {
        if (Triggered || !CanActivate(dir))
            return DashCollisionResults.NormalCollision;

        bool gravityInverted = GravityHelperInterop.IsImported && GravityHelperInterop.IsPlayerInverted();

        // make wallbouncing easier
        if (dashCornerCorrection && (player.Left >= Right - 4f || player.Right < Left + 4f) && dir.Y == (gravityInverted ? 1f : -1f))
            return DashCollisionResults.NormalCollision;

        Level level = SceneAs<Level>();

        // trigger the gate
        Activate();

        if (refillDash && player.Dashes < player.MaxDashes) {
            player.RefillDash();
            Audio.Play(SFX.game_gen_diamond_return, player.Center);

            ParticleType particle = atNode ? P_RefillDashReturn : P_RefillDash;

            float angle = dir.Angle();
            level.ParticlesFG.Emit(particle, 4, player.Center, Vector2.One * 4f, angle - MathF.PI / 2f);
            level.ParticlesFG.Emit(particle, 4, player.Center, Vector2.One * 4f, angle + MathF.PI / 2f);
        }

        // hit effects
        if (smoke)
            ActivateParticles();
        activationFlash = 1f;
        level.DirectionalShake(dir);
        scale = new Vector2(
            1f + Math.Abs(dir.Y) * 0.28f - Math.Abs(dir.X) * 0.28f,
            1f + Math.Abs(dir.X) * 0.28f - Math.Abs(dir.Y) * 0.28f
        );
        offset = dir * 4.15f;
        Audio.Play("event:/game/04_cliffside/arrowblock_activate", Center);
        Audio.Play("event:/sorbethelper/sfx/gateblock_dash_hit", Center);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

        if (allowWavedash && dir.Y == (gravityInverted ? -1f : 1f))
            return DashCollisionResults.NormalCollision;

        return DashCollisionResults.Rebound;
    }

    private bool CanActivate(Vector2 direction)
        => (direction.X != 0f && canMoveHorizontally) || (direction.Y != 0f && canMoveVertically);

    public override void Update() {
        base.Update();

        if (activationFlash > 0f)
            activationFlash -= Engine.DeltaTime * 5f;
    }

    public override void Render() {
        if (!VisibleOnCamera)
            return;

        // main block
        DrawNineSlice(mainTexture, Color.White);
        DrawNineSlice(lightsTexture, Color.Lerp(fillColor, Color.White, activationFlash * 0.4f));

        // render icon
        base.Render();
    }

    protected override void RenderOutline() {
        Vector2 scaledTopLeft = Center + Offset - (new Vector2(Collider.Width / 2f, Collider.Height / 2f) * Scale);
        float scaledWidth = Collider.Width * Scale.X;
        float scaledHeight = Collider.Height * Scale.Y;

        Draw.Rect(scaledTopLeft - Vector2.UnitY, scaledWidth, scaledHeight + 2, Color.Black);
        Draw.Rect(scaledTopLeft - Vector2.UnitX, scaledWidth + 2, scaledHeight, Color.Black);
    }
}
