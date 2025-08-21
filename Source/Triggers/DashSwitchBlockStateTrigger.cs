using System.Linq;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.SorbetHelper.Triggers;

[CustomEntity("SorbetHelper/DashSwitchBlockStateTrigger")]
[Tracked]
public class DashSwitchBlockStateTrigger : Trigger {
    public enum Modes { OnLevelLoad, OnPlayerSpawn, OnPlayerEnter }

    public readonly Modes Mode;
    public readonly int Index;

    public DashSwitchBlockStateTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Mode = data.Enum("mode", Modes.OnPlayerEnter);
        Index = data.Int("index", 0);
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        if (Mode is Modes.OnLevelLoad)
            DashSwitchBlock.SetDashSwitchBlockIndex((Scene as Level).Session, Index);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (Mode is not Modes.OnPlayerEnter)
            return;

        DashSwitchBlock.SetDashSwitchBlockIndex((Scene as Level).Session, Index);
        foreach (var dashSwitchBlock in Scene.Tracker.GetEntities<DashSwitchBlock>().Cast<DashSwitchBlock>())
            dashSwitchBlock.UpdateState(playEffects: false);
    }

    private static void Event_Player_Spawn(Player player) {
        var trigger = player.CollideFirst<DashSwitchBlockStateTrigger>();
        if (trigger is { Mode: Modes.OnPlayerSpawn })
            DashSwitchBlock.SetDashSwitchBlockIndex((trigger.Scene as Level).Session, trigger.Index);
    }

    internal static void Load() {
        Everest.Events.Player.OnSpawn += Event_Player_Spawn;
    }
    internal static void Unload() {
        Everest.Events.Player.OnSpawn -= Event_Player_Spawn;
    }
}