namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/KillZone")]
[Tracked]
public class KillZone : Entity
{
    private readonly string flag;
    private readonly bool inverted;
    private readonly bool fastKill;

    public KillZone(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
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

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        if (!string.IsNullOrEmpty(flag) && !SceneAs<Level>().Session.GetFlag(flag, inverted))
            Collidable = false;
    }

    public override void Update()
    {
        base.Update();

        if (!string.IsNullOrEmpty(flag))
            Collidable = SceneAs<Level>().Session.GetFlag(flag, inverted);
    }

    private void OnPlayer(Player player)
    {
        player.Die(fastKill ? Vector2.Zero : (player.Position - Center).SafeNormalize());
    }

    private void OnHoldable(Holdable holdable)
    {
        switch (holdable.Entity)
        {
            case TheoCrystal theo:
                theo.Die();
                break;
            case Glider glider:
                DestroyGlider(glider);
                break;
            default:
                holdable.OnHitSpinner(this);
                break;
        }

        return;

        // we need to do this manually since the code that usually destroys the jelly is buried deep in its update method in a foreach loop over seeker barriers ..
        // not compatible with mhh respawning jellies but  oh well
        static void DestroyGlider(Glider glider)
        {
            if (glider.destroyed)
                return;

            glider.destroyed = true;
            glider.Collidable = false;
            if (glider.Hold.IsHeld)
            {
                Vector2 holderSpeed = glider.Hold.Holder.Speed;
                glider.Hold.Holder.Drop();
                glider.Speed = holderSpeed * 0.333f;
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            }

            glider.Add(new Coroutine(glider.DestroyAnimationRoutine()));
        }
    }
}
