using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Entities;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Backdrops;

namespace Celeste.Mod.SorbetHelper;
public class SorbetHelperMapDataProcessor : EverestMapDataProcessor {

    public static Dictionary<(int, AreaMode), List<StylegroundDepthController.StylegroundDepthControllerData>> StylegroundDepthControllers { get; private set; } = [];
    public static Dictionary<(int, AreaMode), HashSet<string>> MusicSyncEvents { get; private set; } = [];

    public override Dictionary<string, Action<BinaryPacker.Element>> Init() {
        void stylegroundDepthController(BinaryPacker.Element data) {
            if (!StylegroundDepthControllers.TryGetValue((AreaKey.ID, AreaKey.Mode), out var depthControllers))
                StylegroundDepthControllers[(AreaKey.ID, AreaKey.Mode)] = depthControllers = [];

            var stylegroundTag = data.Attr("tag");
            if (string.IsNullOrEmpty(stylegroundTag))
                return;

            var depthAttr = data.Attr("depth", "-8500");
            var depth = 0;
            var mode = StylegroundDepthController.Modes.Normal;

            if (!int.TryParse(depthAttr, out depth) && !Enum.TryParse(depthAttr, true, out mode))
                Logger.Warn("SorbetHelper", "Invalid depth for Styleground Depth Controller! " + depthAttr);

            var depthController = new StylegroundDepthController.StylegroundDepthControllerData(stylegroundTag, depth, mode);
            depthControllers.Add(depthController);

            Logger.Verbose("SorbetHelper", $"[MapDataProcessor] found a StylegroundDepthController in {AreaKey.SID} {AreaKey.ID} ({AreaKey.Mode}), with depth {depthAttr}!");

            data.Name = "SorbetHelper/MapDataProcessed"; // don't get any annoying "failed to load" warnings (see global entities loading event)
        }

        // convert to a styleground depth controller
        void stylegroundOverHudController(BinaryPacker.Element entityData) {
            entityData.Attributes["depth"] = entityData.AttrInt("pauseBehavior", 0) < 2 ? "AbovePauseHud" : "AboveHud";
            entityData.Attributes["tag"] = "sorbetHelper_drawAboveHud";
            stylegroundDepthController(entityData);
        }

        void musicSyncController(BinaryPacker.Element entityData) {
            var eventNames = entityData.AttrList("eventNames", str => str).ToHashSet();
            MusicSyncEvents[(AreaKey.ID, AreaKey.Mode)] = eventNames;

            Logger.Verbose("SorbetHelper", $"[MapDataProcessor] found a MusicSyncController in {AreaKey.SID} ({AreaKey.Mode})!");
        }

        // swap to the global versions based on a "global" attribute
        static void globalControllerSwap(BinaryPacker.Element entityData) {
            if (entityData.AttrBool("global", false))
                entityData.Name += "Global";
        }

        return new Dictionary<string, Action<BinaryPacker.Element>> {
            // styleground depth controller
            {
                "entity:SorbetHelper/StylegroundDepthController", stylegroundDepthController
            },
            // styleground over hud
            {
                "entity:SorbetHelper/StylegroundOverHudController", stylegroundOverHudController
            },
            // styleground entity controller
            {
                "entity:SorbetHelper/StylegroundEntityController", stylegroundDepthController
            },

            // music sync
            {
                "entity:SorbetHelper/MusicSyncControllerFMOD", musicSyncController
            },

            // global controllers
            {
                "entity:SorbetHelper/EntityStylegroundController", globalControllerSwap
            },
            {
                "entity:SorbetHelper/LightCoverController", globalControllerSwap
            },
            {
                "entity:SorbetHelper/SliderFadeXY", globalControllerSwap
            },
            {
                $"entity:{DarknessTransparencyFixController.EntityDataID}", globalControllerSwap
            }
        };
    }

    public override void Reset() {
        StylegroundDepthControllers.Remove((AreaKey.ID, AreaKey.Mode));
        MusicSyncEvents.Remove((AreaKey.ID, AreaKey.Mode));
    }

    public override void End() {

    }
}
