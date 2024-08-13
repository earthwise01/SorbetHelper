using System;
using System.Collections.Generic;
using Monocle;
using Celeste.Mod.SorbetHelper.Utils;
using Celeste.Mod.SorbetHelper.Entities;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Backdrops;

namespace Celeste.Mod.SorbetHelper {
    public class SorbetHelperMapDataProcessor : EverestMapDataProcessor {

        // honestly idk why im doing it this way and probably shouldve just. made this a Regular Entity but having it map wide is kinda neat and its also just kinda an excuse to mess with map data processors
        public static Dictionary<(int, AreaMode), StylegroundOverHudRenderer.StylegroundOverHudControllerData> StylegroundOverHudControllers { get; private set; } = [];

        public override Dictionary<string, Action<BinaryPacker.Element>> Init() {
            void stylegroundOverHudControllerHandler(BinaryPacker.Element entityData) {
                var controller = new StylegroundOverHudRenderer.StylegroundOverHudControllerData {
                    PauseUpdate = entityData.AttrInt("pauseBehavior", 0) == 1,
                    DisableWhenPaused = entityData.AttrInt("pauseBehavior", 0) == 2
                };

                StylegroundOverHudControllers[(AreaKey.ID, AreaKey.Mode)] = controller;

                Logger.Log(LogLevel.Verbose, "SorbetHelper", $"[MapDataProcessor] found a StylegroundOverHudController in {AreaKey.SID} ({AreaKey.Mode})!");
            }

            return new Dictionary<string, Action<BinaryPacker.Element>> {
                {
                    "entity:SorbetHelper/StylegroundOverHudController", stylegroundOverHudControllerHandler
                }
            };
        }

        public override void Reset() {
            // reset whether or not the level contains a controller
            StylegroundOverHudControllers.Remove((AreaKey.ID, AreaKey.Mode));
        }

        public override void End() {

        }
    }
}
