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
			// Creates a strawberry seed list with no strawberry seeds to effectively remove them.
			if (data.Nodes != null && data.Nodes.Length != 0) {
				Seeds = new List<StrawberrySeed>();
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
			yield return 0.3f;
			// If the player is still alive, put them in the CassetteFly state
			if (!player.Dead) {
				Audio.Play("event:/game/general/cassette_bubblereturn", SceneAs<Level>().Camera.Position + new Vector2(160f, 90f));
				player.StartCassetteFly(nodes[1], nodes[0]);
			}
		}
	}
}
