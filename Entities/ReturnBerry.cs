using Celeste.Mod.Entities;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;

namespace Celeste.Mod.SorbetHelper {

    [CustomEntity("SorbetHelper/ReturnBerry")]
    [RegisterStrawberry(true, false)]
    [TrackedAs(typeof(Strawberry))]
    public class ReturnBerry : Strawberry, IStrawberry {
  //public bool Winged { get; private set; }
        private Sprite sprite;
        private bool collected;
        private bool isGhostBerry;
        private Wiggler wiggler;
		private Vector2 start;
		private Vector2[] nodes;
		public bool IsWinged { get; private set; }

		private static MethodInfo strawberryOnDash = typeof(Strawberry).GetMethod("OnDash", BindingFlags.Instance | BindingFlags.NonPublic);


        //private bool flyingAway;


        public ReturnBerry(EntityData data, Vector2 offset, EntityID gid) : base(data, offset, gid) {
            ID = gid;
			Position = (start = data.Position + offset);
			IsWinged = data.Bool("winged") || data.Name == "memorialTextController";
			base.Depth = -100;
            base.Collider = new Hitbox(14f, 14f, -7f, -7f);
			Add(new PlayerCollider(OnPlayer));
            if (IsWinged) {
				Add(new DashListener {
					OnDash = OnDash
				});
			}
        }
        
        //public ReturnBerry
		private void OnDash(Vector2 dir) {
			strawberryOnDash.Invoke(this, new object[] { dir} );
		}
		/*private void OnDash(Vector2 dir) {
			if (!flyingAway && Winged && !WaitingOnSeeds) {
				base.Depth = -1000000;
				Add(new Coroutine(FlyAwayRoutine()));
				flyingAway = true;
			}
		}*/
        public void IsOnPlayer(Player player) {
            if (Follower.Leader != null || collected)
			{
				return;
			}
			ReturnHomeWhenLost = true;
			if (IsWinged)
			{
				Level level = SceneAs<Level>();
				IsWinged = false;
				sprite.Rate = 0f;
				Alarm.Set(this, Follower.FollowDelay, delegate
				{
					sprite.Rate = 1f;
					sprite.Play("idle");
					level.Particles.Emit(P_WingsBurst, 8, Position + new Vector2(8f, 0f), new Vector2(4f, 2f));
					level.Particles.Emit(P_WingsBurst, 8, Position - new Vector2(8f, 0f), new Vector2(4f, 2f));
				});
			}
            Audio.Play(isGhostBerry ? "event:/game/general/strawberry_blue_touch" : "event:/game/general/strawberry_touch", Position);
			player.Leader.GainFollower(Follower);
			wiggler.Start();
			base.Depth = -1000000;
        }
    }




}