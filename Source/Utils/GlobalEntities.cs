using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Monocle;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using MonoMod.Utils;

namespace Celeste.Mod.SorbetHelper.Utils;

internal static class GlobalEntities {
    public const string ForceGlobalAttribute = "sorbetHelper_makeGlobal"; // could be useful idk

    // public static readonly Dictionary<string, Level.EntityLoader> EntityLoaders = [];
    public static readonly HashSet<string> GlobalEntityIDs = [];
    public static readonly HashSet<string> OnlyOneEntities = [];

    private static readonly HashSet<string> OnlyOneLoaded = [];

    private static bool LoadingGlobalEntities = false;

    private static void Event_OnLoadingThread(Level level) {
        OnlyOneLoaded.Clear();

        var origLevel = level.Session.Level;
        var mapData = level.Session.MapData;

        foreach (var levelData in mapData.Levels) {
            LoadingGlobalEntities = true; // bypass the check for global entities in LoadCustomEntity
            level.Session.Level = levelData.Name; // LoadCustomEntity doesn't take a LevelData argument and instead gets it through level.Session.LevelData,
                                                  // so to make sure global entities are loading with the correct offsets/entityids/etc level.Session.Level needs to be modified temporarily
            Calc.PushRandom(levelData.LoadSeed);

            try {
                var loaded = new List<Entity>();
                foreach (var entityData in levelData.Entities) {
                    var name = entityData.Name;
                    if ((!GlobalEntityIDs.Contains(name) && !entityData.Values.ContainsKey(ForceGlobalAttribute)) || OnlyOneLoaded.Contains(name))
                        continue;

                    level.LoadAndGetCustomEntity(entityData, loaded);

                    // wonder if i should make this result in only adding the entity with the highest id instead of the first found in the map,
                    // actually i wonder if this would be a good place for like    a mapdataprocessor or something idk
                    if (OnlyOneEntities.Contains(name))
                        OnlyOneLoaded.Add(name);
                }

                // apply the global tag
                foreach (var entity in loaded)
                    entity.Tag |= Tags.Global;
            } catch (Exception e) {
                Logger.Error(nameof(SorbetHelper), $"error while loading global entities for room {levelData.Name} in map {mapData.Area.SID}!\n {e}");
            }

            LoadingGlobalEntities = false;
            Calc.PopRandom();
        }

        level.Session.Level = origLevel;
    }

    // don't load global entities in Level.LoadCustomEntity
    private static bool Event_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
        if (!LoadingGlobalEntities && (GlobalEntityIDs.Contains(entityData.Name) || entityData.Values.ContainsKey(ForceGlobalAttribute)))
            return true;

        return false;
    }

    internal static void Load() {
        Everest.Events.LevelLoader.OnLoadingThread += Event_OnLoadingThread;
        Everest.Events.Level.OnLoadEntity += Event_OnLoadEntity;
    }
    internal static void Unload() {
        Everest.Events.LevelLoader.OnLoadingThread -= Event_OnLoadingThread;
        Everest.Events.Level.OnLoadEntity -= Event_OnLoadEntity;
    }

    public static void ProcessAttributes(Assembly assembly = null) {
        assembly ??= typeof(GlobalEntities).Assembly;
        var types = assembly.GetTypesSafe();

        foreach (var type in types) {
            var globalAttr = type.GetCustomAttribute<GlobalEntityAttribute>();
            if (globalAttr is null)
                continue;

            var ids = globalAttr.IDs;
            var onlyOne = globalAttr.OnlyOne;

            // if no ids specified try grabbing them from a custom entity attribute
            if (ids.Length == 0 && type.GetCustomAttribute<CustomEntityAttribute>() is { } customEntity) {
                ids = new string[customEntity.IDs.Length];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = customEntity.IDs[i].Split('=')[0].Trim();
            }

            foreach (var id in ids)
                RegisterGlobalEntity(id, onlyOne);
        }
    }

    public static void RegisterGlobalEntity(string id, bool onlyOne) {
        GlobalEntityIDs.Add(id);

        if (onlyOne)
            OnlyOneEntities.Add(id);
    }
}

/// <summary>
/// Mark a Monocle.Entity with a CustomEntityAttribute to be loaded during the Everest LevelLoader.OnLoadingThread event rather than when its room is loaded.<br/>
/// Automatically adds the Tags.Global BitTag.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class GlobalEntityAttribute : Attribute {
    public string[] IDs;
    public bool OnlyOne;

    public GlobalEntityAttribute(params string[] ids) : base() {
        this.IDs = ids;
    }
}
