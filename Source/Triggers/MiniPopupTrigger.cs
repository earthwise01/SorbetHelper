using Celeste.Mod.SorbetHelper.Entities;

namespace Celeste.Mod.SorbetHelper.Triggers;

[CustomEntity("SorbetHelper/MiniPopupTrigger")]
public class MiniPopupTrigger(EntityData data, Vector2 offset, EntityID entityId) : Trigger(data, offset) {
    private enum Modes {
        OnPlayerEnter,
        OnFlagEnabled,
        OnFlagDisabled,
        WhilePlayerInside
    }

    private readonly Modes mode = data.Enum("mode", Modes.OnPlayerEnter);

    private readonly string flag = data.Attr("flag", "");
    private readonly bool onlyOnce = data.Bool("onlyOnce", false);
    private readonly bool removeOnLeave = data.Bool("removeOnLeave", true);

    private readonly float activeTime = data.Float("activeTime", 8f);
    private readonly string mainTextId = data.Attr("titleText", "AREA_7"), subTextId = data.Attr("subText", "CHECKPOINT_7_3");
    private readonly Color baseColor = data.HexColor("baseColor", Color.Black);
    private readonly Color accentColor = data.HexColor("accentColor", Color.LightCoral);
    private readonly Color titleColor = data.HexColor("titleColor", Color.White);
    private readonly string iconPath = data.Attr("iconTexture", "");
    private readonly string texturePath = data.Attr("texturePath", "");

    private bool currentFlagState;
    private bool triggered;

    private Action disablePopup;

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (!string.IsNullOrEmpty(flag))
            currentFlagState = SceneAs<Level>().Session.GetFlag(flag);
    }

    public override void Update() {
        base.Update();

        if (string.IsNullOrEmpty(flag) || SceneAs<Level>().Session.GetFlag(flag) == currentFlagState)
            return;

        currentFlagState = !currentFlagState;

        switch (mode) {
            case Modes.OnPlayerEnter or Modes.WhilePlayerInside:
                Collidable = currentFlagState;
                break;

            case Modes.OnFlagEnabled:
                if (currentFlagState)
                    Trigger();
                break;

            case Modes.OnFlagDisabled:
                if (!currentFlagState)
                    Trigger();
                break;
        }
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (mode is Modes.OnPlayerEnter or Modes.WhilePlayerInside)
            Trigger();
    }

    public override void OnLeave(Player player) {
        base.OnLeave(player);

        if (mode is Modes.WhilePlayerInside && disablePopup is not null)
            disablePopup();
    }

    private void Trigger() {
        if (triggered && removeOnLeave)
            return;

        triggered = true;
        disablePopup = MiniPopupDisplay.GetMiniPopupDisplay(Scene)
                                       .CreatePopup(mode is Modes.WhilePlayerInside ? -1 : activeTime, mainTextId,
                                           subTextId, baseColor, accentColor, titleColor, iconPath, texturePath);

        if (onlyOnce)
            SceneAs<Level>().Session.DoNotLoad.Add(entityId);
    }
}
