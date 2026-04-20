using System.Collections.ObjectModel;
using System.Globalization;

namespace Celeste.Mod.SorbetHelper.Utils;

internal static class Extensions
{
    #region Misc Extensions

    public static bool IsInRange(this int value, int min, int max) => value >= min && value <= max;
    public static bool IsInRange(this float value, float min, float max) => value >= min && value <= max;

    extension(StringSplitOptions)
    {
        public static StringSplitOptions TrimAndRemoveEmpty => StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
    }

    #endregion

    #region Calc Extensions

    extension(Calc)
    {
        // Modified from HexToColorWithAlpha in https://github.com/EverestAPI/Everest/blob/dev/Celeste.Mod.mm/Patches/Monocle/Calc.cs
        /// <summary>
        /// Convert a hex color, possibly including a non-premultiplied alpha value, into an XNA <see cref="Color"/>.
        /// </summary>
        /// <param name="hex">a hex color, in either <c>RRGGBB</c>, <c>RRGGBBAA</c>, or <c>AA</c> form.</param>
        /// <returns>an XNA <see cref="Color"/>, defaulting to <see cref="Color.White"/>.</returns>
        public static Color HexToColorWithNonPremultipliedAlpha(string hex)
        {
            int consumed = 0;

            if (hex.Length >= 1 && hex[0] == '#')
                consumed = 1;

            int r, g, b, a;

            switch (hex.Length - consumed)
            {
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

    #endregion

    #region Level Extensions

    extension(Level self)
    {
        /// <summary>
        /// Loads an <see cref="Entity"/> from <see cref="EntityData"/> into a <see cref="Level"/>, and copies a reference to it into a list.<br/>
        /// If multiplie entities are loaded (e.g. due to an event on <see cref="Everest.Events.Level.OnLoadEntity"/>), all newly loaded entities will be added to the list.
        /// </summary>
        /// <param name="entityData">The <see cref="EntityData"/> to load.</param>
        /// <param name="level">The <see cref="Level"/> to use for loading the <see cref="EntityData"/>, using <see cref="Level.LoadCustomEntity"/>.</param>
        /// <param name="addTo">The list to add any loaded entities to.</param>
        /// <param name="addToLevel">Whether the loaded entities should be added to the level.</param>
        /// <returns>Whether an entity was loaded.</returns>
        public static bool LoadAndGetCustomEntity(EntityData entityData, Level level, List<Entity> addTo, bool addToLevel = true)
        {
            List<Entity> toAdd = level.Entities.ToAdd;
            int prevToAddCount = toAdd.Count;

            if (!Level.LoadCustomEntity(entityData, level))
                return false;

            for (int i = prevToAddCount; i < toAdd.Count; i++)
                addTo.Add(toAdd[i]);

            if (!addToLevel && toAdd.Count > prevToAddCount)
                toAdd.RemoveRange(prevToAddCount, toAdd.Count - prevToAddCount);

            return true;
        }

        /// <summary>
        /// Loads an <see cref="Entity"/> from <see cref="EntityData"/> into a <see cref="Level"/>, and returns a reference to it<br/>
        /// If multiplie entities are loaded (e.g. due to an event on <see cref="Everest.Events.Level.OnLoadEntity"/>), this only returns the first one loaded.
        /// </summary>
        /// <param name="entityData">The <see cref="EntityData"/> to load.</param>
        /// <param name="level">The <see cref="Level"/> to use for loading the <see cref="EntityData"/>, using <see cref="Level.LoadCustomEntity"/>.</param>
        /// <param name="addToLevel">Whether the loaded entity should be added to the level.</param>
        /// <returns>The loaded entity, or null if none was created.</returns>
        public static Entity LoadAndGetCustomEntity(EntityData entityData, Level level, bool addToLevel = true)
        {
            List<Entity> toAdd = level.Entities.ToAdd;
            int prevToAddCount = toAdd.Count;

            if (!Level.LoadCustomEntity(entityData, level) || toAdd.Count <= prevToAddCount)
                return null;

            Entity entity = toAdd[prevToAddCount];

            if (!addToLevel)
                toAdd.RemoveAt(prevToAddCount);

            return entity;
        }

        /// <summary>
        /// Get a matrix that can be used to transform a vector from camera space to screen space. Accounts for compatibility with ExtendedVariants and ExtendedCameraDynamics.
        /// </summary>
        /// <returns>A <see cref="Matrix"/> that can be used to transform a vector from camera space to screen space.</returns>
        public Matrix GetCameraToScreenMatrix()
        {
            Matrix matrix = Matrix.Identity;

            // zoom & padding
            float zoom = self.Zoom;
            if (ExtendedVariantsCompat.IsLoaded)
                zoom *= ExtendedVariantsCompat.GetZoomLevel();
            float zoomTarget = ExtendedCameraDynamics.IsImported && ExtendedCameraDynamics.ExtendedCameraHooksEnabled()
                ? self.Zoom
                : self.ZoomTarget;
            Vector2 dimensions = new Vector2(320f, 180f);
            Vector2 scaledDimensions = dimensions / zoomTarget;
            Vector2 zoomOrigin = zoomTarget != 1f ? (self.ZoomFocusPoint - scaledDimensions / 2f) / (dimensions - scaledDimensions) * dimensions : Vector2.Zero;

            Vector2 paddingOffset = new Vector2(self.ScreenPadding, self.ScreenPadding * (9f / 16f));
            if (ExtendedVariantsCompat.IsLoaded)
                paddingOffset = ExtendedVariantsCompat.AddZoomPaddingOffset(paddingOffset);

            float scale = zoom * (320f - self.ScreenPadding * 2f) / 320f;

            matrix *= Matrix.CreateTranslation(-zoomOrigin.X, -zoomOrigin.Y, 0f)
                      * Matrix.CreateScale(scale)
                      * Matrix.CreateTranslation(zoomOrigin.X + paddingOffset.X, zoomOrigin.Y + paddingOffset.Y, 0f);

            // mirror mode & upside down
            if (SaveData.Instance.Assists.MirrorMode)
                matrix *= Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateTranslation(320f, 0f, 0f);
            if (ExtendedVariantsCompat.IsLoaded && ExtendedVariantsCompat.GetUpsideDown())
                matrix *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, 180f, 0f);

            // scale to screen size
            matrix *= Matrix.CreateScale(6f);

            return matrix;
        }
    }

    #endregion

    #region Entity Extensions

    extension(Entity self)
    {
        /// <summary>
        /// Shortcut function for getting a Component from the Entity's Components list.<br/>
        /// Searches from end to start, which may be more efficient if you can guarantee the Component was added later.
        /// </summary>
        public T GetComponentFromEnd<T>() where T : Component
        {
            List<Component> components = self.Components.components;

            for (int i = components.Count - 1; i >= 0; i--)
            {
                if (components[i] is T t)
                    return t;
            }

            return null;
        }

        /// <summary>
        /// Shortcut function for getting a Component from the Entity's Components list.<br/>
        /// If the number of components in the tracker is less than the size of the entity's components list, searches using the Tracker instead.<br/>
        /// </summary>
        public T GetComponentFromTracker<T>() where T : Component
            => self.Scene is not null
               && self.Scene.Tracker.Components.TryGetValue(typeof(T), out List<Component> trackedComponents)
               && trackedComponents.Count < self.Components.Count
                ? trackedComponents.FirstOrDefault(c => c.Entity == self) as T
                : self.Components.Get<T>();

        public bool CheckTypeName(params HashSet<string> typeNames)
        {
            Type type = self.GetType();
            return typeNames.Overlaps(EntityRegistry.GetKnownSidsFromType(type)) || typeNames.Contains(type.Name) || typeNames.Contains(type.FullName);
        }
    }

    #endregion

    #region Session Extensions

    extension(Session self)
    {
        public bool GetFlag(string flag, bool inverted)
            => self.GetFlag(flag) != inverted;
    }

    #endregion

    #region Camera Extensions

    extension(Camera self)
    {
        public int Width => self.Viewport.Width;
        public int Height => self.Viewport.Height;

        public Vector2 GetCenter()
            => self.Position + new Vector2(self.Viewport.Width / 2f, self.Viewport.Height / 2f);
        public Vector2 GetZoomOutCenterOffset()
            => new Vector2(self.Viewport.Width / 2f - 320f / 2f, self.Viewport.Height / 2f - 180f / 2f);
    }

    #endregion

    #region EntityData Extensions

    extension(EntityData self)
    {
        public Color HexColorWithAlpha(string key, Color defaultValue = default)
        {
            if (self.Values is null || !self.Values.TryGetValue(key, out object value))
                return defaultValue;

            string hexColor = value.ToString();
            if (hexColor?.Length is 2 or 6 or 8)
                return Calc.HexToColorWithAlpha(hexColor);

            return defaultValue;
        }

        public Color HexColorWithNonPremultipliedAlpha(string key, Color defaultValue = default)
        {
            if (self.Values is null || !self.Values.TryGetValue(key, out object value))
                return defaultValue;

            string hexColor = value.ToString();
            if (hexColor?.Length is 2 or 6 or 8)
                return Calc.HexToColorWithNonPremultipliedAlpha(hexColor);

            return defaultValue;
        }

        public T? Nullable<T>(string key) where T : struct, IParsable<T>
        {
            if (self.Values is null || !self.Values.TryGetValue(key, out object value))
                return null;

            if (value is T tResult)
                return tResult;

            if (T.TryParse(value.ToString(), CultureInfo.InvariantCulture, out T parsedResult))
                return parsedResult;

            return null;
        }

        public Ease.Easer Easer(string key, Ease.Easer defaultValue)
            => Ease.StringToEaser.GetValueOrDefault(self.Attr(key, ""), defaultValue);
        public Ease.Easer Easer(string key)
            => Ease.StringToEaser.GetValueOrDefault(self.Attr(key, ""), Ease.Linear);

        public IEnumerable<string> List(string key, string defaultValue = "")
            => self.Attr(key, defaultValue).Split(',', StringSplitOptions.TrimAndRemoveEmpty);
        public IEnumerable<T> List<T>(string key, Func<string, T> transform, string defaultValue = "")
            => self.Attr(key, defaultValue).Split(',', StringSplitOptions.TrimAndRemoveEmpty).Select(transform);
        public IEnumerable<T> List<T>(string key, Func<string, int, T> transform, string defaultValue = "")
            => self.Attr(key, defaultValue).Split(',', StringSplitOptions.TrimAndRemoveEmpty).Select(transform);
    }

    #endregion

    #region BinaryPacker.Element Extensions

    extension(BinaryPacker.Element self)
    {
        public Color AttrHexColorWithAlpha(string key, Color defaultValue = default)
        {
            if (!self.Attributes.TryGetValue(key, out object value))
                return defaultValue;

            string hexColor = value.ToString();
            if (hexColor?.Length is 2 or 6 or 8)
                return Calc.HexToColorWithAlpha(hexColor);

            return defaultValue;
        }

        public Color AttrHexColorWithNonPremultipliedAlpha(string key, Color defaultValue = default)
        {
            if (!self.Attributes.TryGetValue(key, out object value))
                return defaultValue;

            string hexColor = value.ToString();
            if (hexColor?.Length is 2 or 6 or 8)
                return Calc.HexToColorWithNonPremultipliedAlpha(hexColor);

            return defaultValue;
        }

        public Ease.Easer AttrEaser(string key, Ease.Easer defaultValue)
            => Ease.StringToEaser.GetValueOrDefault(self.Attr(key, ""), defaultValue);
        public Ease.Easer AttrEaser(string key)
            => Ease.StringToEaser.GetValueOrDefault(self.Attr(key, ""), Ease.Linear);

        public IEnumerable<T> AttrList<T>(string key, Func<string, T> transform, string defaultValue = "")
            => self.Attr(key, defaultValue).Split(',', StringSplitOptions.TrimAndRemoveEmpty).Select(transform);
        public IEnumerable<T> AttrList<T>(string key, Func<string, int, T> transform, string defaultValue = "")
            => self.Attr(key, defaultValue).Split(',', StringSplitOptions.TrimAndRemoveEmpty).Select(transform);
    }

    #endregion

    #region Ease Extensions

    // grrr this kind of sucks but oh well
    private static readonly ReadOnlyDictionary<string, Ease.Easer> EaseExtensions_StringToEaser
        = new ReadOnlyDictionary<string, Ease.Easer>(new Dictionary<string, Ease.Easer>
        {
            { "Linear", Ease.Linear },
            { "SineIn", Ease.SineIn }, { "SineOut", Ease.SineOut }, { "SineInOut", Ease.SineInOut },
            { "QuadIn", Ease.QuadIn }, { "QuadOut", Ease.QuadOut }, { "QuadInOut", Ease.QuadInOut },
            { "CubeIn", Ease.CubeIn }, { "CubeOut", Ease.CubeOut }, { "CubeInOut", Ease.CubeInOut },
            { "QuintIn", Ease.QuintIn }, { "QuintOut", Ease.QuintOut }, { "QuintInOut", Ease.QuintInOut },
            { "ExpoIn", Ease.ExpoIn }, { "ExpoOut", Ease.ExpoOut }, { "ExpoInOut", Ease.ExpoInOut },
            { "BackIn", Ease.BackIn }, { "BackOut", Ease.BackOut }, { "BackInOut", Ease.BackInOut },
            { "BigBackIn", Ease.BigBackIn }, { "BigBackOut", Ease.BigBackOut }, { "BigBackInOut", Ease.BigBackInOut },
            { "ElasticIn", Ease.ElasticIn }, { "ElasticOut", Ease.ElasticOut }, { "ElasticInOut", Ease.ElasticInOut },
            { "BounceIn", Ease.BounceIn }, { "BounceOut", Ease.BounceOut }, { "BounceInOut", Ease.BounceInOut },
        });

    extension(Ease)
    {
        /// <summary>
        /// maps all Monocle <see cref="Ease.Easer"/>s to their names (i.e. <c>"SineInOut"</c> => <see cref="Ease.SineInOut"/>)
        /// </summary>
        public static ReadOnlyDictionary<string, Ease.Easer> StringToEaser => EaseExtensions_StringToEaser;
    }

    #endregion

    #region ILCursor Extensions

    extension(ILCursor self)
    {
        public VariableDefinition AddVariable(Type type)
        {
            VariableDefinition variableDefinition = new VariableDefinition(self.Context.Import(type));
            self.Body.Variables.Add(variableDefinition);
            return variableDefinition;
        }

        public VariableDefinition AddVariable<T>()
            => AddVariable(self, typeof(T));
    }

    #endregion
}
