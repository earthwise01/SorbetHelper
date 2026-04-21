namespace Celeste.Mod.SorbetHelper.Utils;

internal static class GlobalEntities
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(GlobalEntities)}";

    public const string ForceGlobalAttribute = "sorbetHelper_makeGlobal";

    private static readonly Dictionary<string, string> GlobalEntityIDs = [];

    private static bool loadingGlobalEntities = false;

    private static bool IsGlobalEntity(EntityData entityData)
        => (GlobalEntityIDs.TryGetValue(entityData.Name, out string onlyIfAttr)
            && (string.IsNullOrEmpty(onlyIfAttr) || entityData.Bool(onlyIfAttr)))
        || entityData.Bool(ForceGlobalAttribute);


    public static void ProcessAttributes(Assembly assembly)
    {
        Type[] types = assembly.GetTypesSafe();

        foreach (Type type in types)
        {
            GlobalEntityAttribute globalAttr = type.GetCustomAttribute<GlobalEntityAttribute>();
            if (globalAttr is null)
                continue;

            string[] ids = globalAttr.IDs;
            string onlyIfAttr = globalAttr.OnlyIfAttr;

            // if no ids specified try grabbing them from a custom entity attribute
            if (ids.Length == 0 && type.GetCustomAttribute<CustomEntityAttribute>() is { } customEntity)
            {
                ids = new string[customEntity.IDs.Length];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = customEntity.IDs[i].Split('=')[0].Trim();
            }

            foreach (string id in ids)
                RegisterGlobalEntity(id, onlyIfAttr);
        }
    }

    public static void RegisterGlobalEntity(string id, string onlyIfAttr)
        => GlobalEntityIDs.Add(id, onlyIfAttr);

    #region Hooks

    [OnLoad]
    internal static void ProcessSorbetHelperAssembly()
    {
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

    private static bool Event_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        // don't give any failed to load warnings for map data processed entities
        if (entityData.Name == SorbetHelperMapDataProcessor.MapDataProcessedSID)
            return true;

        // don't load global entities in Level.LoadCustomEntity
        if (!loadingGlobalEntities && IsGlobalEntity(entityData))
            return true;

        return false;
    }

    #endregion
}


/// <summary>
/// Registers an <see cref="Entity"/> that has a <see cref="CustomEntityAttribute"/> to be loaded during the <see cref="Everest.Events.LevelLoader.OnLoadingThread"/> event rather than when its room is loaded.<br/>
/// Automatically adds the <see cref="Tags.Global"/> tag.
/// </summary>
/// <param name="ids">A list of entity SIDs associated with the targetted entity to treat as global entities. If empty, defaults to all SIDs listed in its <see cref="CustomEntityAttribute"/>.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class GlobalEntityAttribute(params string[] ids) : Attribute
{
    public readonly string[] IDs = ids;

    /// <summary>
    /// If not null, the name of a <see langword="bool"/> value in the entity's <see cref="EntityData"/> that must be true for the entity to be treated as a global entity.
    /// </summary>
    public string OnlyIfAttr = null;
}
