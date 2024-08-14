using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/KillZone")]
    public class KillZone : Entity {
        private readonly string flag;
        private readonly bool inverted;
        private readonly bool fastKill;

        public KillZone(EntityData data, Vector2 offset) : base(data.Position + offset) {
            base.Collider = new Hitbox(data.Width, data.Height);
            Add(new LedgeBlocker());
            Add(new PlayerCollider(OnCollide));
            flag = data.Attr("flag");
            inverted = data.Bool("inverted");
            fastKill = data.Bool("fastKill", false);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            if (!string.IsNullOrEmpty(flag) && !SceneAs<Level>().Session.GetFlag(flag, inverted))
                Collidable = false;
        }

        public void OnCollide(Player player) {
            if (fastKill) {
                player.Die(Vector2.Zero);
                return;
            }
            player.Die((player.Position - Position).SafeNormalize());
        }

        public override void Update() {
            if (!string.IsNullOrEmpty(flag)) {
                // If the associated flag is disabled (or enabled and the inverted toggle is set) disable the Kill Zone
                if (!SceneAs<Level>().Session.GetFlag(flag, inverted)) {
                    Collidable = false;
                } else if (!Collidable) {
                    Collidable = true;
                }
            }
        }
    }
}
