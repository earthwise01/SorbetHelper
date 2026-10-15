namespace Celeste.Mod.SorbetHelper.Triggers;

[CustomEntity("SorbetHelper/ColorDashBlockStateTrigger")]
[Tracked]
public class ColorDashBlockStateTrigger(EntityData data, Vector2 offset) : Trigger(data, offset)
{
    private enum Modes
    {
        OnLevelLoad,
        OnPlayerSpawn,
        OnPlayerEnter
    }

    private readonly Modes mode = data.Enum("mode", Modes.OnPlayerEnter);
    private readonly int index = data.Int("index", 0);

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (mode is Modes.OnLevelLoad)
            ColorDashBlock.SetColorDashBlockIndex(SceneAs<Level>().Session, index);
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);

        if (mode is Modes.OnPlayerEnter)
        {
            ColorDashBlock.SetColorDashBlockIndex(SceneAs<Level>().Session, index);
            foreach (ColorDashBlock colorDashBlock in Scene.Tracker.GetEntities<ColorDashBlock>())
                colorDashBlock.UpdateState(playEffects: false);
        }
    }

    #region Hooks

    [OnLoad]
    internal static void Load()
    {
        Everest.Events.Player.OnSpawn += Event_Player_Spawn;
    }

    [OnUnload]
    internal static void Unload()
    {
        Everest.Events.Player.OnSpawn -= Event_Player_Spawn;
    }

    private static void Event_Player_Spawn(Player player)
    {
        if (player.CollideFirst<ColorDashBlockStateTrigger>() is { mode: Modes.OnPlayerSpawn } trigger)
            ColorDashBlock.SetColorDashBlockIndex(trigger.SceneAs<Level>().Session, trigger.index);
    }

    #endregion
}
