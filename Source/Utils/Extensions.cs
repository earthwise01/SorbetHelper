using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.SorbetHelper.Utils;

internal static class Extensions {
    // session
    public static bool GetFlag(this Session session, string flag, bool inverted) =>
        session.GetFlag(flag) != inverted;

    // entitydata
    public static Ease.Easer Easer(this EntityData self, string key, Ease.Easer defaultValue) =>
        Util.Easers.GetValueOrDefault(self.Attr(key, ""), defaultValue);
    public static Ease.Easer Easer(this EntityData self, string key) =>
        Util.Easers.GetValueOrDefault(self.Attr(key, ""), Ease.Linear);

    public static IEnumerable<T> List<T>(this EntityData self, string key, Func<string, T> transform, string defaultValue = "") =>
        self.Attr(key, defaultValue).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(transform);
    public static IEnumerable<T> List<T>(this EntityData self, string key, Func<string, int, T> transform, string defaultValue = "") =>
        self.Attr(key, defaultValue).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(transform);

    public static Vector2 Vector(this EntityData self, string key, float defaultX = 0, float defaultY = 0) =>
        new(self.Float(key + "X", defaultX), self.Float(key + "Y", defaultY));
    public static Vector2 Vector(this EntityData self, string key, float defaultValue = 0) =>
        new(self.Float(key + "X", defaultValue), self.Float(key + "Y", defaultValue));

    // binarypacker.element (ie styleground entitydata)
    public static Ease.Easer AttrEaser(this BinaryPacker.Element self, string key, Ease.Easer defaultValue) =>
        Util.Easers.GetValueOrDefault(self.Attr(key, ""), defaultValue);
    public static Ease.Easer AttrEaser(this BinaryPacker.Element self, string key) =>
        Util.Easers.GetValueOrDefault(self.Attr(key, ""), Ease.Linear);

    public static IEnumerable<T> AttrList<T>(this BinaryPacker.Element self, string key, Func<string, T> transform, string defaultValue = "") =>
        self.Attr(key, defaultValue).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(transform);
    public static IEnumerable<T> AttrList<T>(this BinaryPacker.Element self, string key, Func<string, int, T> transform, string defaultValue = "") =>
        self.Attr(key, defaultValue).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(transform);

    public static Vector2 AttrVector(this BinaryPacker.Element self, string key, float defaultX = 0, float defaultY = 0) =>
        new(self.AttrFloat(key + "X", defaultX), self.AttrFloat(key + "Y", defaultY));
    public static Vector2 AttrVector(this BinaryPacker.Element self, string key, float defaultValue = 0) =>
        new(self.AttrFloat(key + "X", defaultValue), self.AttrFloat(key + "Y", defaultValue));

    // entity
    public static T GetComponentFromEnd<T>(this Entity entity) where T : Component {
        var components = entity.Components.components;
        for (int i = components.Count - 1; i >= 0; i--) {
            if (components[i] is T t) {
                return t;
            }
        }

        return null;
    }

    // level

    /// <summary>
    /// Adds a custom entity to a Level and copies a reference to all newly loaded entities into a list.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="entityData">The EntityData to load.</param>
    /// <param name="addTo">A list to add any loaded entities to.</param>
    /// <param name="addToLevel">Whether to automatically any the loaded entities to the level.</param>
    /// <returns>Whether an entity was loaded.</returns>
    public static bool LoadAndGetCustomEntity(this Level level, EntityData entityData, List<Entity> addTo, bool addToLevel = true) {
        var toAdd = level.Entities.ToAdd;
        var prevToAddCount = toAdd.Count;

        if (!Level.LoadCustomEntity(entityData, level))
            return false;

        for (int i = prevToAddCount; i < toAdd.Count; i++)
            addTo.Add(toAdd[i]);

        if (!addToLevel && prevToAddCount <= toAdd.Count)
            toAdd.RemoveRange(prevToAddCount, toAdd.Count - prevToAddCount);

        return true;
    }

    /// <summary>
    /// Adds a custom entity to a Level and returns a reference to it.<br/>
    /// If loading the entity results in muliple entities being created at the same time, this only returns the first one loaded.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="entityData">The EntityData to load.</param>
    /// <param name="addToLevel">Whether to automatically add the loaded entity to the level.</param>
    /// <returns>The loaded entity, or null if none was created.</returns>
    public static Entity LoadAndGetCustomEntity(this Level level, EntityData entityData, bool addToLevel = true) {
        var toAdd = level.Entities.ToAdd;
        var prevToAddCount = toAdd.Count;

        if (!Level.LoadCustomEntity(entityData, level) || prevToAddCount <= toAdd.Count)
            return null;

        var entity = toAdd[prevToAddCount];

        if (!addToLevel)
            toAdd.RemoveAt(prevToAddCount);

        return entity;
    }

    // misc
    public static bool IsInRange(this int value, int min, int max) => value >= min && value <= max;
    public static bool IsInRange(this float value, float min, float max) => value >= min && value <= max;
    public static Vector2 GetCenter(this Camera camera) => camera.Position + new Vector2(camera.Viewport.Width / 2f, camera.Viewport.Height / 2f);
}
