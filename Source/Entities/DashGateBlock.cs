namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/DashGateBlock")]
[Tracked]
public class DashGateBlock : GateBlock
{
    [Flags]
    private enum HitAxes
    {
        Horizontal = 0b01,
        Vertical = 0b10,
        Both = Horizontal | Vertical
    }

    private readonly MTexture mainTexture, lightsTexture;

    private readonly HitAxes hitAxes;
    private readonly bool allowWavedash;
    private readonly bool dashCornerCorrection;
    private readonly bool refillDash;

    private readonly ParticleType P_RefillDash;
    private readonly ParticleType P_RefillDashReturn;

    public DashGateBlock(EntityData data, Vector2 offset, EntityID id) : base(data, offset, id)
    {
        allowWavedash = data.Bool("allowWavedash", false);
        dashCornerCorrection = data.Bool("dashCornerCorrection", false);
        refillDash = data.Bool("refillDash", false);

        hitAxes = data.Enum("axes", HitAxes.Both);

        string blockSprite = data.Attr("blockSprite", "SorbetHelper/gateblock/dash/block");
        if (hitAxes == HitAxes.Horizontal)
            blockSprite += "_h";
        else if (hitAxes == HitAxes.Vertical)
            blockSprite += "_v";
        mainTexture = GFX.Game[$"objects/{blockSprite}"];
        lightsTexture = GFX.Game[$"objects/{blockSprite}_lights"];

        OnDashCollide = OnDashed;

        P_RefillDash = new ParticleType(Refill.P_Shatter)
        {
            Color = Color.Lerp(startColor, Color.White, 0.75f),
            Color2 = startColor,
            SpeedMin = 120f,
            SpeedMax = 190f
        };
        P_RefillDashReturn = new ParticleType(P_RefillDash)
        {
            Color = Color.Lerp(nodeColor, Color.White, 0.75f),
            Color2 = nodeColor,
        };
    }

    public DashCollisionResults OnDashed(Player player, Vector2 dir)
    {
        if (Triggered || !CanActivate(dir))
            return DashCollisionResults.NormalCollision;

        bool gravityInverted = GravityHelperInterop.IsImported && GravityHelperInterop.IsPlayerInverted();

        // make wallbouncing easier
        if (dashCornerCorrection && (player.Left >= Right - 4f || player.Right < Left + 4f) && dir.Y == (gravityInverted ? 1f : -1f))
            return DashCollisionResults.NormalCollision;

        Level level = SceneAs<Level>();

        // trigger the gate
        Activate(dir);

        if (refillDash && player.RefillDash())
        {
            Audio.Play(SFX.game_gen_diamond_return, player.Center);

            ParticleType particle = AtNode ? P_RefillDashReturn : P_RefillDash;

            float angle = dir.Angle();
            level.ParticlesFG.Emit(particle, 4, player.Center, Vector2.One * 4f, angle - MathF.PI / 2f);
            level.ParticlesFG.Emit(particle, 4, player.Center, Vector2.One * 4f, angle + MathF.PI / 2f);
        }

        level.DirectionalShake(dir);
        Audio.Play("event:/game/04_cliffside/arrowblock_activate", Center);
        Audio.Play("event:/sorbethelper/sfx/gateblock_dash_hit", Center);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

        if (allowWavedash && dir.Y == (gravityInverted ? -1f : 1f))
            return DashCollisionResults.NormalCollision;

        return DashCollisionResults.Rebound;
    }

    private bool CanActivate(Vector2 direction)
        => (direction.X != 0f && (hitAxes & HitAxes.Horizontal) != 0)
           || (direction.Y != 0f && (hitAxes & HitAxes.Vertical) != 0);

    protected override void RenderBlock()
    {
        DrawBlockNiceSlice(mainTexture, Color.White);
        DrawBlockNiceSlice(lightsTexture, FillColor);
    }

    protected override void RenderOutline()
    {
        Rectangle blockRect = GetBlockRectangle();
        Draw.Rect(blockRect.X - 1, blockRect.Y, blockRect.Width + 2, blockRect.Height, Color.Black);
        Draw.Rect(blockRect.X, blockRect.Y - 1, blockRect.Width, blockRect.Height + 2, Color.Black);
    }
}
