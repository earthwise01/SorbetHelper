using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/FlagToggledKillbox")]
[TrackedAs(typeof(Killbox))]
public class FlagToggledKillbox : Killbox {
    private readonly string flag;
    private readonly bool inverted;
    private readonly bool flagOnly;

    private readonly float playerAboveThreshold;

    private readonly bool updateOnLoad;

    public FlagToggledKillbox(EntityData data, Vector2 offset) : base(data, offset) {
        flag = data.Attr("flag", "");
        inverted = data.Bool("inverted", false);
        flagOnly = data.Bool("flagOnly", false);

        playerAboveThreshold = data.Float("playerAboveThreshold", 32f);

        updateOnLoad = data.Bool("updateOnLoad", false);

        if (data.Bool("lenientHitbox", false))
            Get<PlayerCollider>().OnCollide = LenientOnPlayer;
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (updateOnLoad)
            Update();
    }

    public override void Update() {
        // flag only mode checks
        if (flagOnly) {
            if (string.IsNullOrEmpty(flag))
                Collidable = inverted;
            else
                Collidable = SceneAs<Level>().Session.GetFlag(flag, inverted);

            return;
        }

        // normal collidability checks
        Player player = Scene.Tracker.GetEntity<Player>();

        if (!Collidable && player is not null && player.Bottom < Top - playerAboveThreshold)
            Collidable = true;
        else if (player is not null && player.Top > Bottom + 32f)
            Collidable = false;

        // only keep collidable if the flag is set (or null/empty)
        bool canBeCollidable = string.IsNullOrEmpty(flag) || SceneAs<Level>().Session.GetFlag(flag, inverted);
        Collidable = Collidable && canBeCollidable;
    }

    // based on Level.EnforceBounds
    private void LenientOnPlayer(Player player) {
        if (player.Top > Top && SaveData.Instance.Assists.Invincible) {
            player.Play("event:/game/general/assist_screenbottom");
            player.Bounce(Top);
        } else if (player.Top > Top + 4f) {
            player.Die(Vector2.Zero);
        }
    }
}
