using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.SorbetHelper.Utils;

internal static class Extensions {
    extension(Calc) {
        public static Color HexToColorWithNonPremultipliedAlpha(string hex) {
            int consumed = 0;

            if (hex.Length >= 1 && hex[0] == '#')
                consumed = 1;

            int r, g, b, a;

            switch (hex.Length - consumed) {
                case 2:
                    // one byte of data, for the alpha channel
                    a = Calc.HexToByte(hex[consumed++]) * 16 + Calc.HexToByte(hex[consumed++]);
                    // the other channels are fixed at white
                    return new Color(a, a, a, a);

                case 6:
                    // three bytes, for RGB and no alpha
                    r = Calc.HexToByte(hex[consumed++]) * 16 + Calc.HexToByte(hex[consumed++]);
                    g = Calc.HexToByte(hex[consumed++]) * 16 + Calc.HexToByte(hex[consumed++]);
                    b = Calc.HexToByte(hex[consumed++]) * 16 + Calc.HexToByte(hex[consumed++]);
                    return new Color(r, g, b);

                case 8:
                    // four bytes, filling all four channels
                    r = Calc.HexToByte(hex[consumed++]) * 16 + Calc.HexToByte(hex[consumed++]);
                    g = Calc.HexToByte(hex[consumed++]) * 16 + Calc.HexToByte(hex[consumed++]);
                    b = Calc.HexToByte(hex[consumed++]) * 16 + Calc.HexToByte(hex[consumed++]);
                    a = Calc.HexToByte(hex[consumed++]) * 16 + Calc.HexToByte(hex[consumed++]);
                    return Color.FromNonPremultiplied(r, g, b, a);

                default:
                    // some invalid data, so return a sensible default
                    return Color.White;
            }
        }
    }

    extension(Session self) {
        public bool GetFlag(string flag, bool inverted)
            => self.GetFlag(flag) != inverted;
    }

    extension(EntityData self) {
        public Color HexColorWithAlpha(string key, Color defaultValue = default) {
            if (!self.Values.TryGetValue(key, out object value))
                return defaultValue;

            string hexColor = value.ToString();
            if (hexColor?.Length is 2 or 6 or 8)
                return Calc.HexToColorWithAlpha(hexColor);

            return defaultValue;
        }

        public Color HexColorWithNonPremultipliedAlpha(string key, Color defaultValue = default) {
            if (!self.Values.TryGetValue(key, out object value))
                return defaultValue;

            string hexColor = value.ToString();
            if (hexColor?.Length is 2 or 6 or 8)
                return Calc.HexToColorWithNonPremultipliedAlpha(hexColor);

            return defaultValue;
        }

        public Ease.Easer Easer(string key, Ease.Easer defaultValue)
            => Util.Easers.GetValueOrDefault(self.Attr(key, ""), defaultValue);
        public Ease.Easer Easer(string key)
            => Util.Easers.GetValueOrDefault(self.Attr(key, ""), Ease.Linear);

        public IEnumerable<T> List<T>(string key, Func<string, T> transform, string defaultValue = "")
            => self.Attr(key, defaultValue).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(transform);
        public IEnumerable<T> List<T>(string key, Func<string, int, T> transform, string defaultValue = "")
            => self.Attr(key, defaultValue).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(transform);
    }

    extension(BinaryPacker.Element self) {
        public Color AttrHexColorWithAlpha(string key, Color defaultValue = default) {
            if (!self.Attributes.TryGetValue(key, out object value))
                return defaultValue;

            string hexColor = value.ToString();
            if (hexColor?.Length is 2 or 6 or 8)
                return Calc.HexToColorWithAlpha(hexColor);

            return defaultValue;
        }

        public Color AttrHexColorWithNonPremultipliedAlpha(string key, Color defaultValue = default) {
            if (!self.Attributes.TryGetValue(key, out object value))
                return defaultValue;

            string hexColor = value.ToString();
            if (hexColor?.Length is 2 or 6 or 8)
                return Calc.HexToColorWithNonPremultipliedAlpha(hexColor);

            return defaultValue;
        }

        public Ease.Easer AttrEaser(string key, Ease.Easer defaultValue)
            => Util.Easers.GetValueOrDefault(self.Attr(key, ""), defaultValue);

        public Ease.Easer AttrEaser(string key)
            => Util.Easers.GetValueOrDefault(self.Attr(key, ""), Ease.Linear);

        public IEnumerable<T> AttrList<T>(string key, Func<string, T> transform, string defaultValue = "")
            => self.Attr(key, defaultValue).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(transform);

        public IEnumerable<T> AttrList<T>(string key, Func<string, int, T> transform, string defaultValue = "")
            => self.Attr(key, defaultValue).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(transform);
    }

    extension(Entity entity) {
        public T GetComponentFromEnd<T>() where T : Component {
            List<Component> components = entity.Components.components;

            for (int i = components.Count - 1; i >= 0; i--) {
                if (components[i] is T t)
                    return t;
            }

            return null;
        }
    }

    extension(Level level)
    {
        /// <summary>
        /// Adds a custom entity to a Level and copies a reference to all newly loaded entities into a list.
        /// </summary>
        /// <param name="entityData">The EntityData to load.</param>
        /// <param name="addTo">A list to add any loaded entities to.</param>
        /// <param name="addToLevel">Whether to automatically any the loaded entities to the level.</param>
        /// <returns>Whether an entity was loaded.</returns>
        public bool LoadAndGetCustomEntity(EntityData entityData, List<Entity> addTo, bool addToLevel = true) {
            List<Entity> toAdd = level.Entities.ToAdd;
            int prevToAddCount = toAdd.Count;

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
        /// <param name="entityData">The EntityData to load.</param>
        /// <param name="addToLevel">Whether to automatically add the loaded entity to the level.</param>
        /// <returns>The loaded entity, or null if none was created.</returns>
        public Entity LoadAndGetCustomEntity(EntityData entityData, bool addToLevel = true) {
            List<Entity> toAdd = level.Entities.ToAdd;
            int prevToAddCount = toAdd.Count;

            if (!Level.LoadCustomEntity(entityData, level) || prevToAddCount <= toAdd.Count)
                return null;

            Entity entity = toAdd[prevToAddCount];

            if (!addToLevel)
                toAdd.RemoveAt(prevToAddCount);

            return entity;
        }
    }

    // misc
    public static bool IsInRange(this int value, int min, int max)=> value >= min && value <= max;
    public static bool IsInRange(this float value, float min, float max) => value >= min && value <= max;
    public static Vector2 GetCenter(this Camera camera)
        => camera.Position + new Vector2(camera.Viewport.Width / 2f, camera.Viewport.Height / 2f);
}
