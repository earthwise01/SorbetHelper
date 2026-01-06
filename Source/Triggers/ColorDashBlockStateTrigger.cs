using Celeste.Mod.SorbetHelper.Entities;

namespace Celeste.Mod.SorbetHelper.Triggers;

[CustomEntity("SorbetHelper/ColorDashBlockStateTrigger")]
[Tracked]
public class ColorDashBlockStateTrigger(EntityData data, Vector2 offset) : Trigger(data, offset) {
    public enum Modes { OnLevelLoad, OnPlayerSpawn, OnPlayerEnter }

    public readonly Modes Mode = data.Enum("mode", Modes.OnPlayerEnter);
    public readonly int Index = data.Int("index", 0);

    public override void Added(Scene scene) {
        base.Added(scene);

        if (Mode is Modes.OnLevelLoad)
            ColorDashBlock.SetColorDashBlockIndex(SceneAs<Level>().Session, Index);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (Mode is not Modes.OnPlayerEnter)
            return;

        ColorDashBlock.SetColorDashBlockIndex(SceneAs<Level>().Session, Index);
        foreach (ColorDashBlock colorDashBlock in Scene.Tracker.GetEntities<ColorDashBlock>())
            colorDashBlock.UpdateState(playEffects: false);
    }

    #region Hooks

    internal static void Load() {
        Everest.Events.Player.OnSpawn += Event_Player_Spawn;
    }
    internal static void Unload() {
        Everest.Events.Player.OnSpawn -= Event_Player_Spawn;
    }

    private static void Event_Player_Spawn(Player player) {
        ColorDashBlockStateTrigger trigger = player.CollideFirst<ColorDashBlockStateTrigger>();
        if (trigger is { Mode: Modes.OnPlayerSpawn })
            ColorDashBlock.SetColorDashBlockIndex(trigger.SceneAs<Level>().Session, trigger.Index);
    }

    #endregion

}
