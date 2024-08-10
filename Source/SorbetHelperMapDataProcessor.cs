using System;
using Monocle;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Entities;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Backdrops;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Celeste.Mod.SorbetHelper {
    public class SorbetHelperMapDataProcessor : EverestMapDataProcessor {

        // might modify this at some point to allow using settings from the controller but just checking if one exists works fine for now
        public static Dictionary<int, List<AreaMode>> LevelsWithStylegroundOverHudControllers { get; private set; } = [];
        public static bool LevelContainsStylegroundOverHudController(int areaKey, AreaMode areaMode) {
            if (LevelsWithStylegroundOverHudControllers.TryGetValue(areaKey, out var modesWithController)) {
                return modesWithController.Contains(areaMode);
            }

            return false;
        }

        public override Dictionary<string, Action<BinaryPacker.Element>> Init() {
            void stylegroundOverHudControllerHandler(BinaryPacker.Element entityData) {
                if (!LevelsWithStylegroundOverHudControllers[AreaKey.ID].Contains(AreaKey.Mode)) {
                    LevelsWithStylegroundOverHudControllers[AreaKey.ID].Add(AreaKey.Mode);
                    Logger.Log(LogLevel.Verbose, "SorbetHelper", $"[MapDataProcessor] found a StylegroundOverHudController in {AreaKey.SID} ({AreaKey.Mode})!");
                }
            }

            return new Dictionary<string, Action<BinaryPacker.Element>> {
                {
                    "entity:SorbetHelper/StylegroundOverHudController", stylegroundOverHudControllerHandler
                }
            };
        }

        public override void Reset() {
            // add entry for level if it doesn't exist yet
            if (!LevelsWithStylegroundOverHudControllers.ContainsKey(AreaKey.ID))
                LevelsWithStylegroundOverHudControllers[AreaKey.ID] = [];

            // reset whether or not the level contains a controller
            LevelsWithStylegroundOverHudControllers[AreaKey.ID].Remove(AreaKey.Mode);
        }

        public override void End() {

        }
    }
}
