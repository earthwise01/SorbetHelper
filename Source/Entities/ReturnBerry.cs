using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/ReturnBerry")]
    [RegisterStrawberry(true, false)]
    public class ReturnBerry : Strawberry {
        private readonly Vector2[] nodes;
        public float delay;

        public ReturnBerry(EntityData data, Vector2 offset, EntityID gid) : base(data, offset, gid) {
            nodes = data.NodesOffset(offset);
            delay = data.Float("delay", 0.3f);
            isGhostBerry = SaveData.Instance.CheckStrawberry(ID);
            Add(new PlayerCollider(OnPlayer));
            // Creates a strawberry seed list with no strawberry seeds to effectively remove them.
            if (data.Nodes != null && data.Nodes.Length != 0) {
                Seeds = new List<StrawberrySeed>();
                if (data.Nodes.Length > 2) {
                    for (int i = 2; i < data.Nodes.Length; i++) {
                        Seeds.Add(new StrawberrySeed(this, offset + data.Nodes[i], i, isGhostBerry));
                    }
                }
            }
        }

        public new void OnPlayer(Player player) {
            // Calls the OnPlayer function from Strawberry, then starts NodeRoutine if there are 2 or more nodes.
            base.OnPlayer(player);
            if (nodes != null && nodes.Length >= 2) {
                Add(new Coroutine(NodeRoutine(player)));
                Collidable = false;
            }
        }

        private IEnumerator NodeRoutine(Player player) {
            if (delay > 0f)
                yield return delay;

            // If the player is still alive, put them in the CassetteFly state
            if (!player.Dead) {
                Audio.Play("event:/game/general/cassette_bubblereturn", SceneAs<Level>().Camera.Position + new Vector2(160f, 90f));
                player.StartCassetteFly(nodes[1], nodes[0]);
            }
        }
    }
}
