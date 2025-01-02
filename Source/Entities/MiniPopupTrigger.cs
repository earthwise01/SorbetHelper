using Microsoft.Xna.Framework;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.Entities;
using Monocle;

namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/MiniPopupTrigger")]
public class MiniPopupTrigger : Trigger {
    public enum Modes {
        OnPlayerEnter,
        OnFlagEnabled,
        OnFlagDisabled
    }
    private readonly Modes Mode;

    private readonly string flag;

    private readonly bool onlyOnce;
    private readonly EntityID id;

    private readonly float activeTime;
    private readonly string mainTextId, subTextId;
    private readonly Color baseColor, accentColor, titleColor;
    private readonly string iconPath;
    private readonly string texturePath;

    private bool currentFlagState;
    private bool triggered;

    public MiniPopupTrigger(EntityData data, Vector2 offset, EntityID entityId) : base(data, offset) {
        Mode = data.Enum("mode", Modes.OnPlayerEnter);

        flag = data.Attr("flag", "");

        onlyOnce = data.Bool("onlyOnce", false);
        id = entityId;

        activeTime = data.Float("activeTime", 8f);
        mainTextId = data.Attr("titleText", "AREA_7");
        subTextId = data.Attr("subText", "CHECKPOINT_7_3");
        baseColor = data.HexColor("baseColor", Color.Black);
        accentColor = data.HexColor("accentColor", Color.LightCoral);
        titleColor = data.HexColor("titleColor", Color.White);
        iconPath = data.Attr("iconTexture", "");
        texturePath = data.Attr("texturePath", "");
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        if (!string.IsNullOrEmpty(flag))
            currentFlagState = (scene as Level).Session.GetFlag(flag);
    }

    public override void Update() {
        base.Update();

        if (!string.IsNullOrEmpty(flag) && (Scene as Level).Session.GetFlag(flag) != currentFlagState) {
            currentFlagState = !currentFlagState;

            switch (Mode) {
                case Modes.OnPlayerEnter:
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
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (Mode is Modes.OnPlayerEnter)
            Trigger();
    }

    private void Trigger() {
        if (triggered)
            return;

        triggered = true;
        Scene.Tracker.GetEntity<MiniPopupDisplay>()?.CreatePopup(activeTime, mainTextId, subTextId, baseColor, accentColor, titleColor, iconPath, texturePath);

        if (onlyOnce)
            (Scene as Level).Session.DoNotLoad.Add(id);
    }
}
