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
    public static Dictionary<(int, AreaMode), HashSet<string>> MusicSyncEvents { get; private set; } = [];

    public override Dictionary<string, Action<BinaryPacker.Element>> Init() {
        static void stylegroundOverHudController(BinaryPacker.Element entityData) {
            entityData.Name = "SorbetHelper/StylegroundDepthController";
            entityData.Attributes["depth"] = entityData.AttrInt("pauseBehavior", 0) < 2 ? "AbovePauseHud" : "AboveHud";
            entityData.Attributes["tag"] = "sorbetHelper_drawAboveHud";
        }

        static void stylegroundEntityController(BinaryPacker.Element entityData) {
            entityData.Name = "SorbetHelper/StylegroundDepthController";
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
            // styleground over hud
            {
                "entity:SorbetHelper/StylegroundOverHudController", stylegroundOverHudController
            },
            // styleground entity controller
            {
                "entity:SorbetHelper/StylegroundEntityController", stylegroundEntityController
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
            }
        };
    }

    public override void Reset() {
        MusicSyncEvents.Remove((AreaKey.ID, AreaKey.Mode));
    }

    public override void End() {

    }
}
