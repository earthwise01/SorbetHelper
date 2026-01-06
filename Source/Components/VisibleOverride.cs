using System.Collections.Generic;

namespace Celeste.Mod.SorbetHelper.Components;

/// <summary>
/// Prevents an Entity from rendering while preserving the value of its Visible field.<br/>
/// This is done by setting its Visible field to false for the duration of Level.Render and restoring it afterwards.
/// </summary>
[Tracked]
public class VisibleOverride() : Component(false, false) {
    private bool? wasVisible;

    /// <summary>
    /// Whether this component's Entity is currently supposed to be visible.<br/>
    /// Prefer this over Entity.Visible if checking from inside Level.Render.
    /// </summary>
    public bool EntityVisible => wasVisible ?? Entity.Visible;

    #region Hooks

    internal static void Load() {
        On.Celeste.Level.Render += On_Level_Render;
    }

    internal static void Unload() {
        On.Celeste.Level.Render -= On_Level_Render;
    }

    private static void On_Level_Render(On.Celeste.Level.orig_Render orig, Level self) {
        List<Component> visibleOverrides = self.Tracker.GetComponents<VisibleOverride>();
        foreach (VisibleOverride component in visibleOverrides) {
            component.wasVisible = component.Entity.Visible;
            component.Entity.Visible = false;
        }

        orig(self);

        foreach (VisibleOverride component in visibleOverrides) {
            component.Entity.Visible = (bool)component.wasVisible;
            component.wasVisible = null;
        }
    }

    #endregion
}
