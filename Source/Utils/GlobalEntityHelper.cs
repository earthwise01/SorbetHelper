namespace Celeste.Mod.SorbetHelper.Utils;

/// <summary>
/// Registers an <see cref="Entity"/> that has a <see cref="CustomEntityAttribute"/> to be loaded during the <see cref="Everest.Events.LevelLoader.OnLoadingThread"/> event rather than when its room is loaded.<br/>
/// Automatically adds the <see cref="Tags.Global"/> tag.
/// </summary>
/// <param name="onlyGlobalIf">If not <see langword="null"/>, the name of a <see langword="bool"/> value in the entity's <see cref="EntityData"/> that must be true for the entity to be treated as a global entity.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal class GlobalEntityAttribute(string onlyGlobalIf = null) : Attribute
{
    public readonly string OnlyGlobalIf = onlyGlobalIf;
}

internal static class GlobalEntityHelper
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(GlobalEntityHelper)}";

    public const string ForceGlobalAttribute = "sorbetHelper_makeGlobal";

    private static readonly Dictionary<string, string> GlobalEntitySIDs = [];

    private static bool loadingGlobalEntities = false;

    private static bool IsGlobalEntity(EntityData entityData)
        => (GlobalEntitySIDs.TryGetValue(entityData.Name, out string onlyGlobalIf)
            && (string.IsNullOrEmpty(onlyGlobalIf) || entityData.Bool(onlyGlobalIf)))
        || entityData.Bool(ForceGlobalAttribute);


    private static void ProcessAttributes(Assembly assembly)
    {
        Type[] types = assembly.GetTypesSafe();

        foreach (Type type in types)
        {
            GlobalEntityAttribute globalEntityAttr = type.GetCustomAttribute<GlobalEntityAttribute>();
            if (globalEntityAttr is null)
                continue;

            string onlyGlobalIf = globalEntityAttr.OnlyGlobalIf;

            if (type.GetCustomAttribute<CustomEntityAttribute>() is not { } customEntityAttr)
            {
                Logger.Warn(LogID, $"{type.FullName} has a {nameof(GlobalEntityAttribute)} without a corrosponding {nameof(CustomEntityAttribute)}!");
                continue;
            }

            foreach (string entitySid in customEntityAttr.IDs)
                RegisterGlobalEntity(entitySid.Split('=')[0].Trim(), onlyGlobalIf);
        }
    }

    public static void RegisterGlobalEntity(string entitySid, string onlyGlobalIf)
        => GlobalEntitySIDs.Add(entitySid, onlyGlobalIf);

    #region Hooks

    [OnLoad]
    internal static void ProcessSorbetHelperAssembly()
    {
        // hmm
        ProcessAttributes(typeof(SorbetHelperModule).Assembly);
    }

    [OnLoad]
    internal static void Load()
    {
        Everest.Events.LevelLoader.OnLoadingThread += Event_OnLoadingThread;
        Everest.Events.Level.OnLoadEntity += Event_OnLoadEntity;
    }

    [OnUnload]
    internal static void Unload()
    {
        Everest.Events.LevelLoader.OnLoadingThread -= Event_OnLoadingThread;
        Everest.Events.Level.OnLoadEntity -= Event_OnLoadEntity;
    }

    private static void Event_OnLoadingThread(Level level)
    {
        string origLevel = level.Session.Level;
        loadingGlobalEntities = true;

        List<Entity> loadedEntities = [];

        MapData mapData = level.Session.MapData;
        foreach (LevelData levelData in mapData.Levels)
        {
            // LoadCustomEntity doesn't take a LevelData argument and instead uses level.Session.LevelData, so we need to change the current level temporarily
            level.Session.Level = levelData.Name;
            Calc.PushRandom(levelData.LoadSeed);

            try
            {
                foreach (EntityData entityData in levelData.Entities.Where(IsGlobalEntity))
                    Level.LoadAndGetCustomEntity(entityData, level, loadedEntities);
            }
            catch (Exception e)
            {
                Logger.Error(LogID, $"error while loading global entities for room {levelData.Name} in map {mapData.Area.SID}!\n{e}");
            }

            Calc.PopRandom();
        }

        foreach (Entity entity in loadedEntities)
            entity.Tag |= Tags.Global;

        loadingGlobalEntities = false;
        level.Session.Level = origLevel;
    }

    // prevent global entities from loading normally
    private static bool Event_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        if (!loadingGlobalEntities && IsGlobalEntity(entityData))
            return true;

        return false;
    }

    #endregion
}
