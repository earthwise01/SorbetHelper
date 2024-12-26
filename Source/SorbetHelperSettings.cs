using Monocle;

namespace Celeste.Mod.SorbetHelper;

public class SorbetHelperSettings : EverestModuleSettings {
    [SettingSubText("modsettings_sorbethelper_minipopupsize_description")]
    public MiniPopupSizes MiniPopupSize { get; set; } = MiniPopupSizes.Normal;
    public enum MiniPopupSizes {
        Small, Normal, Large
    }

    [SettingSubText("modsettings_sorbethelper_minipopupcap_description")]
    [SettingRange(min: 1, max: 10)]
    public int MiniPopupVisibleCap { get; set; } = 8;
}
