using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Monocle;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using MonoMod.Utils;

namespace Celeste.Mod.SorbetHelper.Utils;

public static class GlobalEntities {
    public static readonly Dictionary<string, Level.EntityLoader> EntityLoaders = [];
    public static readonly HashSet<string> OnlyOneEntities = [];

    private static readonly HashSet<string> OnlyOneLoaded = [];

    private static void Event_OnLoadingThread(Level level) {
        OnlyOneLoaded.Clear();

        var mapData = level.Session.MapData;

        foreach (var levelData in mapData.Levels) {
            var offset = new Vector2(levelData.Bounds.Left, levelData.Bounds.Top);
            Calc.PushRandom(levelData.LoadSeed);

            foreach (var entityData in levelData.Entities) {
                var name = entityData.Name;
                if (!EntityLoaders.TryGetValue(name, out var loader) || OnlyOneLoaded.Contains(name))
                    continue;

                var loaded = loader(level, levelData, offset, entityData);
                if (loaded is null)
                    continue;

                loaded.AddTag(Tags.Global);
                level.Add(loaded);
                // wonder if i should make this result in only adding the entity with the highest id instead of the first found in the map,
                if (OnlyOneEntities.Contains(name))
                    OnlyOneLoaded.Add(name);
            }

            Calc.PopRandom();
        }
    }

    // don't load global entities in Level.LoadLevel
    private static bool Event_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
        if (EntityLoaders.ContainsKey(entityData.Name))
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

        EntityLoaders.Clear();
        OnlyOneEntities.Clear();

        foreach (var type in types) {
            var attribute = type.GetCustomAttribute<GlobalEntityAttribute>();
            if (attribute is null)
                continue;

            // stolen from Everest.Loader.ProcessAssembly :3
            var id = attribute.ID;

            Level.EntityLoader loader = null;

            ConstructorInfo ctor = null;
            MethodInfo gen = null;

            gen = type.GetMethod("Load", [typeof(Level), typeof(LevelData), typeof(Vector2), typeof(EntityData)]);
            if (gen is not null && gen.IsStatic && gen.ReturnType.IsCompatible(typeof(Entity))) {
                loader = (level, levelData, offset, entityData) => (Entity)gen.Invoke(null, [level, levelData, offset, entityData]);
                goto RegisterEntityLoader;
            }

            ctor = type.GetConstructor([typeof(EntityData), typeof(Vector2), typeof(EntityID)]);
            if (ctor != null) {
                loader = (level, levelData, offset, entityData) => (Entity)ctor.Invoke([entityData, offset, new EntityID(levelData.Name, entityData.ID)]);
                goto RegisterEntityLoader;
            }

            ctor = type.GetConstructor([typeof(EntityData), typeof(Vector2)]);
            if (ctor is not null) {
                loader = (level, levelData, offset, entityData) => (Entity)ctor.Invoke([entityData, offset]);
                goto RegisterEntityLoader;
            }

            ctor = type.GetConstructor([typeof(Vector2)]);
            if (ctor is not null) {
                loader = (level, levelData, offset, entityData) => (Entity)ctor.Invoke([offset]);
                goto RegisterEntityLoader;
            }

            ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor is not null) {
                loader = (level, levelData, offset, entityData) => (Entity)ctor.Invoke(null);
                goto RegisterEntityLoader;
            }

        RegisterEntityLoader:
            if (loader is null) {
                Logger.Warn(nameof(SorbetHelper), $"Found global entity without suitable constructor / Load(Level, LevelData, Vector2, EntityData): {id} ({type.FullName})");
                continue;
            }

            EntityLoaders[id] = loader;
            if (attribute.OnlyOne)
                OnlyOneEntities.Add(id);
        }
    }
}

/// <summary>
/// Mark a Monocle.Entity to be loaded from map data during the Everest LevelLoader.OnLoadingThread event.<br/>
/// Automatically adds the Tags.Global BitTag.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class GlobalEntityAttribute : Attribute {
    public string ID;
    public bool OnlyOne;

    public GlobalEntityAttribute(string id, bool onlyOne = false) : base() {
        ID = id;
        OnlyOne = onlyOne;
    }
}
