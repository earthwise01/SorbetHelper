namespace Celeste.Mod.SorbetHelper;

internal class SorbetHelperMapDataProcessor : EverestMapDataProcessor
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(SorbetHelperMapDataProcessor)}";

    public const string MapDataProcessedSID = "SorbetHelper/MapDataProcessed";

    public static Dictionary<(int, AreaMode), List<StylegroundDepthController.StylegroundDepthControllerData>> StylegroundDepthControllers { get; private set; } = [];
    public static Dictionary<(int, AreaMode), HashSet<string>> MusicSyncEvents { get; private set; } = [];

    public override Dictionary<string, Action<BinaryPacker.Element>> Init()
    {
        void ProcessStylegroundDepthController(BinaryPacker.Element data)
        {
            if (!StylegroundDepthControllers.TryGetValue((AreaKey.ID, AreaKey.Mode), out List<StylegroundDepthController.StylegroundDepthControllerData> depthControllers))
                StylegroundDepthControllers[(AreaKey.ID, AreaKey.Mode)] = depthControllers = [];

            string stylegroundTag = data.Attr("tag");
            if (string.IsNullOrEmpty(stylegroundTag))
                return;

            string depthAttr = data.Attr("depth", "-8500");
            int depth = 0;
            StylegroundDepthController.Modes mode = StylegroundDepthController.Modes.Normal;

            if (!int.TryParse(depthAttr, out depth) && !Enum.TryParse(depthAttr, true, out mode))
                Logger.Warn(LogID, "invalid depth for Styleground Depth Controller! " + depthAttr);

            StylegroundDepthController.StylegroundDepthControllerData depthController = new StylegroundDepthController.StylegroundDepthControllerData(stylegroundTag, depth, mode);
            depthControllers.Add(depthController);

            Logger.Verbose(LogID, $"found a StylegroundDepthController in {AreaKey.SID} {AreaKey.ID} ({AreaKey.Mode}), with depth {depthAttr}!");

            data.Name = MapDataProcessedSID;
        }

        // convert to a styleground depth controller
        void ProcessStylegroundOverHudController(BinaryPacker.Element entityData)
        {
            entityData.Attributes["depth"] = entityData.AttrInt("pauseBehavior", 0) < 2 ? "AbovePauseHud" : "AboveHud";
            entityData.Attributes["tag"] = "sorbetHelper_drawAboveHud";
            ProcessStylegroundDepthController(entityData);
        }

        void ProcessMusicSyncController(BinaryPacker.Element entityData)
        {
            HashSet<string> eventNames = entityData.AttrList("eventNames", str => str).ToHashSet();
            MusicSyncEvents[(AreaKey.ID, AreaKey.Mode)] = eventNames;

            Logger.Verbose(LogID, $"found a MusicSyncController in {AreaKey.SID} ({AreaKey.Mode})!");
        }

        // swap to the global versions based on a "global" attribute
        static void ProcessGlobalOptionController(BinaryPacker.Element entityData)
        {
            if (entityData.AttrBool("global", false))
                entityData.Name += "Global";
        }

        return new Dictionary<string, Action<BinaryPacker.Element>>
        {
            // styleground depth controller
            { "entity:SorbetHelper/StylegroundDepthController", ProcessStylegroundDepthController },
            // styleground over hud
            { "entity:SorbetHelper/StylegroundOverHudController", ProcessStylegroundOverHudController },
            // styleground entity controller
            { "entity:SorbetHelper/StylegroundEntityController", ProcessStylegroundDepthController },

            // music sync
            { "entity:SorbetHelper/MusicSyncControllerFMOD", ProcessMusicSyncController },

            // global controllers
            { $"entity:{EntityStylegroundController.EntitySID}", ProcessGlobalOptionController },
            { $"entity:{LightCoverController.EntitySID}", ProcessGlobalOptionController },
            { $"entity:{SliderFadeXY.EntitySID}", ProcessGlobalOptionController },
            { $"entity:{DarknessTransparencyFixController.EntitySID}", ProcessGlobalOptionController },
            { $"entity:{SparklingWaterColorController.EntitySID}", ProcessGlobalOptionController }
        };
    }

    public override void Reset()
    {
        StylegroundDepthControllers.Remove((AreaKey.ID, AreaKey.Mode));
        MusicSyncEvents.Remove((AreaKey.ID, AreaKey.Mode));
    }

    public override void End()
    {

    }
}
