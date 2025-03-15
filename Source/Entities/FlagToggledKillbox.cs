using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/FlagToggledKillbox")]
    [TrackedAs(typeof(Killbox))]
    public class FlagToggledKillbox : Killbox {
        private readonly string flag;
        private readonly bool inverted;
        private readonly bool flagOnly;

        public FlagToggledKillbox(EntityData data, Vector2 offset) : base(data, offset) {
            flag = data.Attr("flag", "");
            inverted = data.Bool("inverted", false);
            flagOnly = data.Bool("flagOnly", false);

            if (data.Bool("lenientHitbox", false))
                Get<PlayerCollider>().OnCollide = LenientOnPlayer;
        }

        public override void Update() {
            base.Update();

            if (string.IsNullOrEmpty(flag)) {
                if (flagOnly)
                    Collidable = inverted;

                return;
            }

            if (Collidable || flagOnly)
                Collidable = (Scene as Level).Session.GetFlag(flag, inverted);
        }

    // based on Level.EnforceBounds
    public void LenientOnPlayer(Player player) {
        if (player.Top > Top && SaveData.Instance.Assists.Invincible) {
            player.Play("event:/game/general/assist_screenbottom");
            player.Bounce(Top);
        } else if (player.Top > Top + 4f) {
            player.Die(Vector2.Zero);
        }
    }
    }
}
