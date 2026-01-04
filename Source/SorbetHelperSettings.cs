using Monocle;

namespace Celeste.Mod.SorbetHelper;

public class SorbetHelperSettings : EverestModuleSettings {
    public float MiniPopupScale { get; private set; } = 1f;
    public void CreateMiniPopupScaleEntry(TextMenu menu, bool inGame) {
        TextMenu.Option<float> option = new TextMenu.Option<float>(Dialog.Clean("modsettings_sorbethelper_minipopupscale_name"));
        for (float f = 0.75f; f <= 1.25f; f += 0.05f)
            option.Add((f * 100).ToString("n0") +  "%", f, f == MiniPopupScale);

        option.OnValueChange = (f) => MiniPopupScale = f;

        menu.Add(option);
    }

    [SettingSubText("modsettings_sorbethelper_minipopupcap_description")]
    [SettingRange(min: 1, max: 10)]
    public int MiniPopupVisibleCap { get; set; } = 8;
}
