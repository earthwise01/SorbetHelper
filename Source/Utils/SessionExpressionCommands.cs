using System.Globalization;
using System.Numerics;

namespace Celeste.Mod.SorbetHelper.Utils;

internal static class SessionExpressionCommands
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    private class SessionExpressionCommand(string name) : Attribute
    {
        public string Name { get; } = name;
    }

    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(SessionExpressionCommands)}";

    private const string ModName = "sorbetHelper";

    internal static void RegisterCommands()
    {
        if (!FrostHelper.IsImported)
            return;

        // hmm
        // todo: support for expression contexts / userdata commands somehow maybe ? once i understand how those even work
        // also support for defining commands elsewhere if that seems like it would make sense
        foreach (MethodInfo method in typeof(SessionExpressionCommands).GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            ProcessMethod(method);

        return;

        static void ProcessMethod(MethodInfo method) {
            if (method.GetCustomAttribute<SessionExpressionCommand>() is not { Name: { } commandName })
                return;

            string commandSignature = $"${ModName}.{commandName}";

            ParameterInfo[] parameters = method.GetParameters();

            if (method.ReturnType != typeof(object)
                || parameters.Length < 1
                || parameters[0].ParameterType != typeof(Session))
            {
                Logger.Warn(LogID, $"Found Session Expression command '{commandSignature}' ({method.Name}) with invalid signature! Should return an 'object' and take a 'Session' as its first parameter.");
                return;
            }

            MethodInvoker methodInvoker = MethodInvoker.Create(method);
            if (parameters.Length == 1)
            {
                FrostHelper.RegisterSimpleSessionExpressionCommand(ModName, commandName, session => methodInvoker.Invoke(null, session));
                Logger.Info(LogID, $"Registered Session Expression simple command '{commandSignature}'");
            }
            else
            {
                for (int i = 1; i < parameters.Length; i++)
                {
                    ParameterInfo parameter = parameters[i];

                    if (parameter.ParameterType == typeof(bool)
                        || parameter.ParameterType == typeof(int)
                        || parameter.ParameterType == typeof(float)
                        || parameter.ParameterType == typeof(string)
                        || parameter.ParameterType == typeof(object)
                        || parameter.ParameterType == typeof(Color))
                        continue;

                    Logger.Warn(LogID, $"Found parameter with invalid type on Session Expression function command '{commandSignature}' ({method.Name}): '{parameter.ParameterType.GetTypeName()} {parameter.Name}'. Only 'bool', 'int', 'float', 'string', 'object', or 'Color' parameters are supported.");
                    return;
                }

                string functionCommandSignature = GetFunctionCommandSignature(commandSignature, parameters);
                FrostHelper.RegisterFunctionSessionExpressionCommand(ModName, commandName, (session, args) =>
                {
                    Span<object> arguments = new object[parameters.Length];

                    arguments[0] = session;
                    for (int i = 1; i < parameters.Length; i++)
                    {
                        ParameterInfo parameter = parameters[i];

                        if (i - 1 < args.Count)
                            arguments[i] = GetArgumentValue(args[i - 1], parameter.ParameterType);
                        else
                        {
                            if (!parameter.HasDefaultValue)
                                throw new Exception($"Not enough arguments provided to Session Expression function command '{functionCommandSignature}'! Got {args.Count}, expected {parameters.Length - 1}."); // hmm

                            arguments[i] = parameter.RawDefaultValue;
                        }
                    }

                    return methodInvoker.Invoke(null, arguments);
                });
                Logger.Info(LogID, $"Registered Session Expression function command '{functionCommandSignature}'");
            }
        }
    }

    #region Misc Commands

    // i should probably ask if this can go in base frosthelperr
    [SessionExpressionCommand("timeActive")]
    private static object GetTimeActive(Session session)
        => Engine.Scene.TimeActive;

    #endregion

    #region Color Packing Commands

    [SessionExpressionCommand("rgbColor")]
    private static object PackRgbColor(Session session, float r, float g, float b, float a = 1f, float alpha = 1f)
        => (new Color(r, g, b, a) * alpha).ToPackedInt();

    [SessionExpressionCommand("hslColor")]
    private static object PackHslColor(Session session, float h, float s, float l, float alpha = 1f)
        => (Calc.HslToColor(Calc.Mod(h, 1f), s, l) * alpha).ToPackedInt();

    [SessionExpressionCommand("hsvColor")]
    private static object PackHsvColor(Session session, float h, float s, float v, float alpha = 1f)
        => (Calc.HsvToColor(Calc.Mod(h, 1f), s, v) * alpha).ToPackedInt();

    [SessionExpressionCommand("hexColor")]
    private static object PackHexColor(Session session, string hexColor, float alpha = 1f)
        => (Calc.HexToColorWithNonPremultipliedAlpha(hexColor) * alpha).ToPackedInt();

    #endregion

    #region Color Lerp Commands

    [SessionExpressionCommand("lerpColorRgb")]
    private static object LerpColorRgb(Session session, Color color1, Color color2, float amount)
        => Color.Lerp(color1, color2, amount).ToPackedInt();

    // todo: hsl/hsv lerp?

    #endregion

    #region Misc Color Commands

    [SessionExpressionCommand("multiplyColorAlpha")]
    private static object MultiplyColorAlpha(Session session, Color color, float alpha)
        => (color * alpha).ToPackedInt();

    #endregion

    #region Argument Parsing Utils
    
    // see https://github.com/JaThePlayer/FrostHelper/blob/e4a80d31b3a5526f8bb941e4d349353d629ee847/Code/FrostHelper/Helpers/ConditionHelper.cs#L748
    private static object GetArgumentValue(object arg, Type parameterType)
    {
        // the world if switch expressions supported typeof
        if (parameterType == typeof(bool))
            return GetBool(arg);
        if (parameterType == typeof(int))
            return GetNumber<int>(arg);
        if (parameterType == typeof(float))
            return GetNumber<float>(arg);
        if (parameterType == typeof(string))
            return GetString(arg);
        if (parameterType == typeof(object))
            return arg;
        if (parameterType == typeof(Color))
            return GetColor(arg);
        throw new ArgumentException($"Unsupported parameter type for Session Expression function commands: {parameterType.FullName}");
    }

    private static bool GetBool(object obj) => obj switch
    {
        bool b  => b,
        int n   => n != 0,
        float n => n != 0f,
        null    => false,
        _       => true,
    };

    private static T GetNumber<T>(object obj) where T : struct, INumber<T> => obj switch
    {
        T t      => t,
        float n  => T.CreateTruncating(n),
        double n => T.CreateTruncating(n),
        int n    => T.CreateTruncating(n),
        short n  => T.CreateTruncating(n),
        byte n   => T.CreateTruncating(n),
        _        => T.Zero
    };

    private static string GetString(object obj) => obj switch
    {
        string str     => str,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _              => obj.ToString() ?? ""
    };

    private static Color GetColor(object obj) => obj switch
    {
        Color color     => color,
        int packedColor => Color.FromPackedInt(packedColor),
        string str      => Calc.HexToColorWithNonPremultipliedAlpha(str),
        IFormattable f  => Calc.HexToColorWithNonPremultipliedAlpha(f.ToString(null, CultureInfo.InvariantCulture)),
        _               => Color.White
    };

    #endregion
    
    #region Logging Utils
    
    private static string GetTypeName(this Type type)
    {
        if (type == typeof(bool))
            return "bool";
        if (type == typeof(int))
            return "int";
        if (type == typeof(float))
            return "float";
        if (type == typeof(string))
            return "string";
        if (type == typeof(object))
            return "object";
        return type.Name;
    }

    // hmm
    private static string GetFunctionCommandSignature(string prefix, ParameterInfo[] parameters)
        => $"{prefix}({string.Join(", ", parameters.Skip(1).Select(parameter => {
            string parameterSignature = $"{parameter.ParameterType.GetTypeName()} {parameter.Name}";

            if (!parameter.HasDefaultValue)
                return parameterSignature;

            string defaultValue = parameter.RawDefaultValue switch
            {
                bool boolValue => boolValue ? "1" : "0", // session expressions don't seem to support true/false literals?
                _              => parameter.RawDefaultValue?.ToString()
            };
            return $"{parameterSignature} = {defaultValue}";
        }))})";
    
    #endregion
}
