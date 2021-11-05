using Celeste.Mod.Entities;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using MonoMod.Utils;

namespace Celeste.Mod.SorbetHelper {
	

    [CustomEntity("SorbetHelper/ReturnBerry")]
    [RegisterStrawberry(true, false)]
    public class ReturnBerry : Strawberry {

		private Vector2[] nodes;

        public ReturnBerry(EntityData data, Vector2 offset, EntityID gid) : base(data, offset, gid) {
			nodes = data.NodesOffset(offset);
			Add(new PlayerCollider(OnPlayer));
			if (data.Nodes != null && data.Nodes.Length != 0) {
				Seeds = new List<StrawberrySeed>();
			}
        }

        public new void OnPlayer(Player player) {
			base.OnPlayer(player);
			if (nodes != null && nodes.Length >= 2) {
				Logger.Log("SorbetHelper", "Adding NodeRoutine!");
				Add(new Coroutine(NodeRoutine(player)));
				Collidable = false;
			}
			
        }
		private IEnumerator NodeRoutine(Player player) {
			yield return 0.3f;
			if (!player.Dead) {
				Audio.Play("event:/game/general/cassette_bubblereturn", SceneAs<Level>().Camera.Position + new Vector2(160f, 90f));
				Logger.Log("SorbetHelper", "Starting Cassete Fly!");
				player.StartCassetteFly(nodes[1], nodes[0]);
			}
		}
    }




}