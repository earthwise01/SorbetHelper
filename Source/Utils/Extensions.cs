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

    // misc
    public static bool IsInRange(this int value, int min, int max) => value >= min && value <= max;
    public static bool IsInRange(this float value, float min, float max) => value >= min && value <= max;
    public static Vector2 GetCenter(this Camera camera) => camera.Position + new Vector2(camera.Viewport.Width / 2f, camera.Viewport.Height / 2f);
}
