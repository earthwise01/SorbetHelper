using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;

namespace Celeste.Mod.SorbetHelper.Utils;

internal static class GlobalEntities {
    public const string ForceGlobalAttribute = "sorbetHelper_makeGlobal"; // could be useful idk

    private static readonly HashSet<string> GlobalEntityIDs = [];
    private static readonly HashSet<string> OnlyOneEntities = [];

    private static bool loadingGlobalEntities = false;

    private static bool IsGlobalEntity(EntityData entityData)
        => GlobalEntityIDs.Contains(entityData.Name) || entityData.Bool(ForceGlobalAttribute);


    public static void ProcessAttributes(Assembly assembly) {
        Type[] types = assembly.GetTypesSafe();

        foreach (Type type in types) {
            GlobalEntityAttribute globalAttr = type.GetCustomAttribute<GlobalEntityAttribute>();
            if (globalAttr is null)
                continue;

            string[] ids = globalAttr.IDs;
            bool onlyOne = globalAttr.OnlyOne;

            // if no ids specified try grabbing them from a custom entity attribute
            if (ids.Length == 0 && type.GetCustomAttribute<CustomEntityAttribute>() is { } customEntity) {
                ids = new string[customEntity.IDs.Length];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = customEntity.IDs[i].Split('=')[0].Trim();
            }

            foreach (string id in ids)
                RegisterGlobalEntity(id, onlyOne);
        }
    }

    public static void RegisterGlobalEntity(string id, bool onlyOne) {
        GlobalEntityIDs.Add(id);

        if (onlyOne)
            OnlyOneEntities.Add(id);
    }

    #region Hooks

    internal static void Load() {
        Everest.Events.LevelLoader.OnLoadingThread += Event_OnLoadingThread;
        Everest.Events.Level.OnLoadEntity += Event_OnLoadEntity;
    }
    internal static void Unload() {
        Everest.Events.LevelLoader.OnLoadingThread -= Event_OnLoadingThread;
        Everest.Events.Level.OnLoadEntity -= Event_OnLoadEntity;
    }

    private static void Event_OnLoadingThread(Level level) {
        HashSet<string> onlyOneLoaded = [];

        string origLevel = level.Session.Level;
        MapData mapData = level.Session.MapData;

        foreach (LevelData levelData in mapData.Levels) {
            loadingGlobalEntities = true;
            // LoadCustomEntity doesn't take a LevelData argument and instead gets it through level.Session.LevelData,
            // so to make sure global entities are loading with the correct offsets/EntityIDs/etc, Session.Level needs to be modified temporarily
            level.Session.Level = levelData.Name;
            Calc.PushRandom(levelData.LoadSeed);

            try {
                List<Entity> loaded = [];

                foreach (EntityData entityData in levelData.Entities) {
                    string name = entityData.Name;
                    if (!IsGlobalEntity(entityData) || onlyOneLoaded.Contains(name))
                        continue;

                    Level.LoadAndGetCustomEntity(entityData, level, loaded);

                    // wonder if i should make this result in only adding the entity with the highest id instead of the first found in the map,
                    // actually i wonder if this would be a good place for like    a mapdataprocessor or something idk
                    if (OnlyOneEntities.Contains(name))
                        onlyOneLoaded.Add(name);
                }

                // apply the global tag
                foreach (Entity entity in loaded)
                    entity.Tag |= Tags.Global;
            } catch (Exception e) {
                Logger.Error(nameof(SorbetHelper), $"error while loading global entities for room {levelData.Name} in map {mapData.Area.SID}!\n{e}");
            }

            loadingGlobalEntities = false;
            Calc.PopRandom();
        }

        level.Session.Level = origLevel;
    }

    private static bool Event_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
        // don't give any failed to load warnings for map data processed entities
        if (entityData.Name == "SorbetHelper/MapDataProcessed")
            return true;

        // don't load global entities in Level.LoadCustomEntity
        if (!loadingGlobalEntities && IsGlobalEntity(entityData))
            return true;

        return false;
    }

    #endregion

}

/// <summary>
/// Mark a Monocle.Entity with a CustomEntityAttribute to be loaded during the Everest LevelLoader.OnLoadingThread event rather than when its room is loaded.<br/>
/// Automatically adds the Tags.Global BitTag.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class GlobalEntityAttribute(params string[] ids) : Attribute {
    public string[] IDs = ids;
    public bool OnlyOne;
}
