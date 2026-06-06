namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/FlagToggledKillbox")]
[TrackedAs(typeof(Killbox))]
public class FlagToggledKillbox : Killbox
{
    private readonly Condition enabledCondition;
    private readonly bool ignorePlayerPosition;

    private readonly float playerAboveThreshold;

    private readonly bool updateOnLoad;

    public FlagToggledKillbox(EntityData data, Vector2 offset) : base(data, offset)
    {
        ignorePlayerPosition = data.Bool("flagOnly", false);
        enabledCondition = data.Condition("flag", data.Bool("inverted", false));
        playerAboveThreshold = data.Float("playerAboveThreshold", 32f);
        updateOnLoad = data.Bool("updateOnLoad", false);

        if (data.Bool("lenientHitbox", false))
            Get<PlayerCollider>().OnCollide = LenientOnPlayer;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        if (updateOnLoad)
            Update();
    }

    public override void Update()
    {
        Level level = SceneAs<Level>();
        
        // condition only mode checks
        if (ignorePlayerPosition)
        {
            Collidable = enabledCondition.Check(level.Session);
            return;
        }

        // normal collidability checks
        Player player = level.Tracker.GetEntity<Player>();

        if (!Collidable && player is not null && player.Bottom < Top - playerAboveThreshold)
            Collidable = true;
        else if (player is not null && player.Top > Bottom + 32f)
            Collidable = false;

        // only become collidable if the condition is true
        Collidable = Collidable && enabledCondition.Check(level.Session);
    }

    // based on Level.EnforceBounds
    private void LenientOnPlayer(Player player)
    {
        if (SaveData.Instance.Assists.Invincible && player.Top > Top)
        {
            player.Play("event:/game/general/assist_screenbottom");
            player.Bounce(Top);
        }
        else if (player.Top > Top + 4f)
        {
            player.Die(Vector2.Zero);
        }
    }
}
