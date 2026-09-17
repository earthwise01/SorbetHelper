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

    private const string CommandPrefix = "sorbet";

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

            string commandSignature = $"${CommandPrefix}.{commandName}";

            ParameterInfo[] parameters = method.GetParameters();

            if (method.ReturnType == typeof(void)
                || parameters.Length < 1
                || parameters[0].ParameterType != typeof(Session))
            {
                Logger.Warn(LogID, $"Found Session Expression command '{commandSignature}' ({method.Name}) with invalid signature! Must not return 'void' and take a 'Session' as its first parameter.");
                return;
            }

            MethodInvoker methodInvoker = MethodInvoker.Create(method);
            if (parameters.Length == 1)
            {
                FrostHelper.RegisterSimpleSessionExpressionCommand(CommandPrefix, commandName, session => methodInvoker.Invoke(null, session));
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

                    Logger.Warn(LogID, $"Found parameter with invalid type on Session Expression function command '{commandSignature}' ({method.Name}): '{parameter.ParameterType.GetTypeName()} {parameter.Name}'. Only 'bool', 'int', 'float', 'string', 'Color', or 'object' parameters are supported.");
                    return;
                }

                string functionCommandSignature = GetFunctionCommandSignature(commandSignature, parameters);
                FrostHelper.RegisterFunctionSessionExpressionCommand(CommandPrefix, commandName, (session, args) =>
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

    #region Sorbet Helper Commands

    // color packing/unpacking

    [SessionExpressionCommand("packColor")]
    private static int PackColor(Session session, Color color)
        => color.ToPackedInt();

    [SessionExpressionCommand("unpackColor")]
    private static Color UnpackColor(Session session, int packedColor)
        => Color.FromPackedInt(packedColor);

    // misc color things

    [SessionExpressionCommand("rgba")]
    private static Color Rgba(Session session, float r, float g, float b, float a = 1f, float alpha = 1f)
        => new(r * alpha, g * alpha, b * alpha, a * alpha);

    [SessionExpressionCommand("hsl")]
    private static Color Hsl(Session session, float h, float s, float l, float alpha = 1f)
        => Calc.HslToColor(Calc.Mod(h, 1f), s, l) * alpha;

    [SessionExpressionCommand("nonPremultHex")]
    private static Color NonPremultHex(Session session, string hex)
        => Calc.HexToColorWithNonPremultipliedAlpha(hex);

    [SessionExpressionCommand("fromNonPremult")]
    private static Color FromNonPremult(Session session, Color color)
        => Color.FromNonPremultiplied(color.R, color.G, color.B, color.A);

    [SessionExpressionCommand("multAlpha")]
    private static Color MultAlpha(Session session, Color color, float alpha)
        => color * alpha;

    [SessionExpressionCommand("rgbLerp")]
    private static Color RgbLerp(Session session, Color color1, Color color2, float amount)
        => Color.Lerp(color1, color2, amount);

    // todo: hsv/hsl lerp?
    // todo: oklab/oklch colors/lerping?

    // ternary

    [SessionExpressionCommand("ternary")]
    private static object Ternary(Session session, bool condition, object trueResult, object falseResult)
        => condition ? trueResult : falseResult;

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
        if (parameterType == typeof(Color))
            return GetColor(arg);
        if (parameterType == typeof(object))
            return arg;

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
        Color color => color,
        int i       => Calc.HexToColor(i), // growls .   at least i give a pack color function
        float f     => Calc.HexToColor((int)f),
        string str  => FrostHelper.GetColor(str), // also growlss .   at least i give a multiply alpha function
        _           => Color.White
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
            return "any";
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
