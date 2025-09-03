using System.Linq;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.SorbetHelper.Triggers;

[CustomEntity("SorbetHelper/ColorDashBlockStateTrigger")]
[Tracked]
public class ColorDashBlockStateTrigger : Trigger {
    public enum Modes { OnLevelLoad, OnPlayerSpawn, OnPlayerEnter }

    public readonly Modes Mode;
    public readonly int Index;

    public ColorDashBlockStateTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Mode = data.Enum("mode", Modes.OnPlayerEnter);
        Index = data.Int("index", 0);
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        if (Mode is Modes.OnLevelLoad)
            ColorDashBlock.SetColorDashBlockIndex((Scene as Level).Session, Index);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (Mode is not Modes.OnPlayerEnter)
            return;

        ColorDashBlock.SetColorDashBlockIndex((Scene as Level).Session, Index);
        foreach (ColorDashBlock colorDashBlock in Scene.Tracker.GetEntities<ColorDashBlock>())
            colorDashBlock.UpdateState(playEffects: false);
    }

    private static void Event_Player_Spawn(Player player) {
        var trigger = player.CollideFirst<ColorDashBlockStateTrigger>();
        if (trigger is { Mode: Modes.OnPlayerSpawn })
            ColorDashBlock.SetColorDashBlockIndex((trigger.Scene as Level).Session, trigger.Index);
    }

    internal static void Load() {
        Everest.Events.Player.OnSpawn += Event_Player_Spawn;
    }
    internal static void Unload() {
        Everest.Events.Player.OnSpawn -= Event_Player_Spawn;
    }
}
