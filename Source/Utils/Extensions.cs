using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Celeste.Mod.SorbetHelper.Utils;

internal static class Extensions
{
    extension(string self)
    {
        public bool TryRemovePrefix(string prefix, out string result)
        {
            if (self.StartsWith(prefix, StringComparison.Ordinal))
            {
                result = self[prefix.Length..];
                return true;
            }

            result = self;
            return false;
        }

        public bool TryRemovePrefix(char prefix, out string result)
        {
            if (self.StartsWith(prefix))
            {
                result = self[1..];
                return true;
            }

            result = self;
            return false;
        }
    }

    extension(StringSplitOptions)
    {
        public static StringSplitOptions TrimAndRemoveEmpty => StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
    }

    extension(Calc)
    {
        public static int Mod(int x, int m)
            => (x % m + m) % m;
        public static float Mod(float x, float m)
            => (x % m + m) % m;

        public static bool IsInRange(int value, int min, int max)
            => value >= min && value <= max;
        public static bool IsInRange(float value, float min, float max)
            => value >= min && value <= max;

        #region Color Conversions

        /// <summary>
        /// Convert a hex color, possibly including a non-premultiplied alpha value, into an XNA <see cref="Color"/>.
        /// </summary>
        /// <param name="hex">a hex color, in either <c>RRGGBB</c>, <c>RRGGBBAA</c>, or <c>AA</c> form.</param>
        /// <returns>an XNA <see cref="Color"/>, defaulting to <see cref="Color.White"/>.</returns>
        public static Color HexToColorWithNonPremultipliedAlpha(string hex)
        {
            // modified from https://github.com/EverestAPI/Everest/blob/b24dd1e6ed4efc912e341de44b74497335393dce/Celeste.Mod.mm/Patches/Monocle/Calc.cs#L83

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

        public static Color HslToColor(float hue, float s, float l)
        {
            if (s == 0f)
                return new Color(l, l, l);

            float hueSegment = hue * 6f;
            float c = (1f - MathF.Abs(2f * l - 1f)) * s;
            float x = c * (1f - MathF.Abs(hueSegment % 2f - 1f));
            float m = l - c / 2f;

            return (hueSegment) switch
            {
                < 1f => new Color(c + m, x + m, m),
                < 2f => new Color(x + m, c + m, m),
                < 3f => new Color(m, c + m, x + m),
                < 4f => new Color(m, x + m, c + m),
                < 5f => new Color(x + m, m, c + m),
                _    => new Color(c + m, m, x + m),
            };
        }

        #endregion
    }
    
    extension(Color self)
    {
        public int ToPackedInt()
            => unchecked((int)self.PackedValue);

        public static Color FromPackedInt(int packedValue)
            => new Color() { PackedValue = unchecked((uint)packedValue) };
    }

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

    extension(Session self)
    {
        public bool GetFlag(string flag, bool inverted)
            => self.GetFlag(flag) ^ inverted;
    }

    extension(Camera self)
    {
        public int Width => self.Viewport.Width;
        public int Height => self.Viewport.Height;

        public Vector2 GetCenter()
            => self.Position + new Vector2(self.Viewport.Width / 2f, self.Viewport.Height / 2f);
        public Vector2 GetZoomOutCenterOffset()
            => new Vector2(self.Viewport.Width / 2f - 320f / 2f, self.Viewport.Height / 2f - 180f / 2f);
    }

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

        public Condition Condition(string key, bool inverted = false, bool defaultValue = true)
            => ValueSources.Condition.Create(self.Attr(key), inverted, defaultValue);

        public FloatSource FloatSource(string key, float defaultValue = 0f)
            => ValueSources.FloatSource.Create(self.Values?.GetValueOrDefault(key), defaultValue);

        public IntSource IntSource(string key, int defaultValue = 0)
            => ValueSources.IntSource.Create(self.Values?.GetValueOrDefault(key), defaultValue);

        public ColorSource ColorSource(string key, string defaultHex = "ffffff")
            => ValueSources.ColorSource.Create(self.Attr(key), defaultHex);

        public Ease.Easer Easer(string key, Ease.Easer defaultValue)
            => Ease.FromNameOrDefault(self.Attr(key, ""), defaultValue);
        public Ease.Easer Easer(string key)
            => Ease.FromNameOrDefault(self.Attr(key, ""), Ease.Linear);
    }

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
            => Ease.FromNameOrDefault(self.Attr(key, ""), defaultValue);
        public Ease.Easer AttrEaser(string key)
            => Ease.FromNameOrDefault(self.Attr(key, ""), Ease.Linear);
    }

    extension(Ease)
    {
        public static Ease.Easer FromNameOrDefault(string name, Ease.Easer defaultValue = null)
            => name.ToLowerInvariant() switch
            {
                "linear"    => Ease.Linear,
                "sinein"    => Ease.SineIn,    "sineout"    => Ease.SineOut,    "sineinout"    => Ease.SineInOut,
                "quadin"    => Ease.QuadIn,    "quadout"    => Ease.QuadOut,    "quadinout"    => Ease.QuadInOut,
                "cubein"    => Ease.CubeIn,    "cubeout"    => Ease.CubeOut,    "cubeinout"    => Ease.CubeInOut,
                "quintin"   => Ease.QuintIn,   "quintout"   => Ease.QuintOut,   "quintinout"   => Ease.QuintInOut,
                "expoin"    => Ease.ExpoIn,    "expoout"    => Ease.ExpoOut,    "expoinout"    => Ease.ExpoInOut,
                "backin"    => Ease.BackIn,    "backout"    => Ease.BackOut,    "backinout"    => Ease.BackInOut,
                "bigbackin" => Ease.BigBackIn, "bigbackout" => Ease.BigBackOut, "bigbackinout" => Ease.BigBackInOut,
                "elasticin" => Ease.ElasticIn, "elasticout" => Ease.ElasticOut, "elasticinout" => Ease.ElasticInOut,
                "bouncein"  => Ease.BounceIn,  "bounceout"  => Ease.BounceOut,  "bounceinout"  => Ease.BounceInOut,
                _           => defaultValue
            };
    }

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
}
