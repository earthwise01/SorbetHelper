using Celeste.Mod.Entities;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.SorbetHelper {

    [CustomEntity("SorbetHelper/KillZone")]
    public class KillZone : Entity {

        private PlayerCollider pc;

        public KillZone(Vector2 position) : base(position) {
            Add(new LedgeBlocker());
            Add(pc = new PlayerCollider(OnCollide));
        }

        public KillZone(EntityData e, Vector2 levelOffset)
            : this(e.Position + levelOffset) { base.Collider = new Hitbox(e.Width, e.Height); }

        public void OnCollide(Player player) {
            player.Die(new Vector2(0f,0f));
        }
    }
}
