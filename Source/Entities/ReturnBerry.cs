using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/ReturnBerry")]
[RegisterStrawberry(true, false)]
public class ReturnBerry : Strawberry {
    private readonly Vector2[] nodes;
    private readonly float bubbleDelay;
    private readonly bool bubbleParticles;

    public ReturnBerry(EntityData data, Vector2 offset, EntityID id) : base(data, offset, id) {
        nodes = data.NodesOffset(offset);
        bubbleDelay = data.Float("delay", 0.3f);
        bubbleParticles = data.Bool("bubbleParticles", false);

        Remove(Get<PlayerCollider>());
        Add(new PlayerCollider(OnPlayer));

        // recreate the strawberry seed list but accounting for the first 2 nodes being for the bubble
        if (data.Nodes is { Length: > 0 }) {
            Seeds.Clear();

            if (data.Nodes.Length > 2) {
                for (int i = 0; i < data.Nodes.Length - 2; i++) {
                    Seeds.Add(new StrawberrySeed(this, offset + data.Nodes[i], i, isGhostBerry));
                }
            }
        }
    }

    public override void Update() {
        base.Update();

        // bubbles !!
        // i think ive seen like a map or two do this before with a seperate emitter entity but honestly i feel like itd be cool to have it built in
        if (!bubbleParticles || Follower.HasLeader || collected || WaitingOnSeeds || !Visible || CollideCheck<FakeWall>() || CollideCheck<Solid>())
            return;

        if (Scene.OnInterval(0.55f, ID.ID * 64f)) // not sure abt speed still or whether it shd be fast or slow
            (Scene as Level).Particles.Emit(Player.P_CassetteFly, 2, Center + new Vector2(0f, 2f), new Vector2(5f));
    }

    public new void OnPlayer(Player player) {
        // not an override method but still need to call "base" because the original playercollider get removed
        base.OnPlayer(player);

        if (nodes is { Length: >= 2 }) {
            Add(new Coroutine(NodeRoutine(player)));
            Collidable = false;
        }
    }

    private IEnumerator NodeRoutine(Player player) {
        if (bubbleDelay > 0f)
            yield return bubbleDelay;

        // if maddy is still alive put her in a bubble
        if (!player.Dead) {
            Audio.Play("event:/game/general/cassette_bubblereturn", (Scene as Level).Camera.GetCenter());
            player.StartCassetteFly(nodes[1], nodes[0]);
        }
    }
}
