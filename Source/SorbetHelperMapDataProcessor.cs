namespace Celeste.Mod.SorbetHelper;

internal class SorbetHelperMapDataProcessor : EverestMapDataProcessor
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(SorbetHelperMapDataProcessor)}";

    private const string MapDataProcessedSID = "SorbetHelper/MapDataProcessed";

    public static Dictionary<(int, AreaMode), List<StylegroundDepthController.StylegroundDepthControllerData>> StylegroundDepthControllers { get; } = [];
    public static Dictionary<(int, AreaMode), HashSet<string>> MusicSyncEvents { get; } = [];

    public override Dictionary<string, Action<BinaryPacker.Element>> Init()
    {
        return new Dictionary<string, Action<BinaryPacker.Element>>() {
            ["entity:SorbetHelper/StylegroundDepthController"] = ProcessStylegroundDepthController,
            // backwards compat
            ["entity:SorbetHelper/StylegroundOverHudController"] = ProcessStylegroundOverHudController,
            ["entity:SorbetHelper/StylegroundEntityController"] = ProcessStylegroundDepthController,

            ["entity:SorbetHelper/MusicSyncControllerFMOD"] = ProcessMusicSyncController
        };

        #region Styleground Depth Controller

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

        void ProcessStylegroundOverHudController(BinaryPacker.Element data)
        {
            data.Attributes["depth"] = data.AttrInt("pauseBehavior", 0) < 2 ? "AbovePauseHud" : "AboveHud";
            data.Attributes["tag"] = "sorbetHelper_drawAboveHud";
            ProcessStylegroundDepthController(data);
        }

        #endregion

        #region Music Sync Controller

        void ProcessMusicSyncController(BinaryPacker.Element data)
        {
            HashSet<string> eventNames = data.AttrList("eventNames", str => str).ToHashSet();
            MusicSyncEvents[(AreaKey.ID, AreaKey.Mode)] = eventNames;

            Logger.Verbose(LogID, $"found a MusicSyncController in {AreaKey.SID} ({AreaKey.Mode})!");
        }

        #endregion
    }

    public override void Reset()
    {
        StylegroundDepthControllers.Remove((AreaKey.ID, AreaKey.Mode));
        MusicSyncEvents.Remove((AreaKey.ID, AreaKey.Mode));
    }

    public override void End()
    {

    }

    #region Hooks

    [OnLoad]
    internal static void Load()
    {
        Everest.Events.Level.OnLoadEntity += Event_OnLoadEntity;
    }

    [OnUnload]
    internal static void Unload()
    {
        Everest.Events.Level.OnLoadEntity -= Event_OnLoadEntity;
    }

    private static bool Event_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        // don't give any failed to load warnings for map data processed entities
        if (entityData.Name == MapDataProcessedSID)
            return true;

        return false;
    }

    #endregion
}
