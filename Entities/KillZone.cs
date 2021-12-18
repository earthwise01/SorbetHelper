using Celeste.Mod.Entities;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.SorbetHelper {

    [CustomEntity("SorbetHelper/KillZone")]
    public class KillZone : Entity {

        private PlayerCollider pc;
        private string flag;
        private bool inverted;

        public KillZone(EntityData data, Vector2 offset) : base(data.Position + offset) {
            base.Collider = new Hitbox(data.Width, data.Height);
            Add(new LedgeBlocker());
            Add(pc = new PlayerCollider(OnCollide));
            flag = data.Attr("flag");
            inverted = data.Bool("inverted");
        }

        public void OnCollide(Player player) {
			player.Die((player.Position - Position).SafeNormalize());
        }
        
        public override void Update() {
            if (!string.IsNullOrEmpty(flag)) {
                // If the associated flag is disabled (or enabled and the inverted toggle is set) disable the Kill Zone
                if ((!inverted && !SceneAs<Level>().Session.GetFlag(flag)) 
                || (inverted && SceneAs<Level>().Session.GetFlag(flag))) {
                    Collidable = false;
                } else if (!Collidable) {
                    Collidable = true;
                } 
            }
        }
    }
}
