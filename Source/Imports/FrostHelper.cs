using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ModInteropImportGenerator;

namespace Celeste.Mod.SorbetHelper.Imports;

[GenerateImports("FrostHelper", RequiredDependency = false)]
public static partial class FrostHelper
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(Imports)}/{nameof(FrostHelper)}";

    #region Session Expressions

    #region Session Expression Wrapper Class

    public sealed class SessionExpression
    {
        private static object EmptyExpression
        {
            get
            {
                if (field is null)
                    TryCreateSessionExpression("", out field);

                return field;
            }
        }

        private readonly object expressionObject;

        public Type ReturnType
            => GetSessionExpressionReturnedType(expressionObject);

        public SessionExpression(string expression)
        {
            if (!IsImported)
                throw new InvalidOperationException($"Attempted to parse session expression '{expression}', but Frost Helper is not imported!");

            if (!TryCreateSessionExpression(expression, out expressionObject))
            {
                Logger.Warn(LogID, $"Failed to parse session expression '{expression}'");
                expressionObject = EmptyExpression;
            }
        }

        public object Get(Session session, object userdata = null)
            => GetSessionExpressionValue(expressionObject, session, userdata);

        public bool GetBool(Session session, object userdata = null)
            => GetBoolSessionExpressionValue(expressionObject, session, userdata);
        public int GetInt(Session session, object userdata = null)
            => GetIntSessionExpressionValue(expressionObject, session, userdata);
        public float GetFloat(Session session, object userdata = null)
            => GetFloatSessionExpressionValue(expressionObject, session, userdata);
        public string GetString(Session session, object userdata = null)
            => GetStringSessionExpressionValue(expressionObject, session, userdata);
    }

    #endregion

    public static partial bool TryCreateSessionExpression(string str, [NotNullWhen(true)] out object expression);
    public static partial bool TryCreateSessionExpression(string str, object context, [NotNullWhen(true)] out object expression);

    public static partial object GetSessionExpressionValue(object expression, Session session);
    public static partial object GetSessionExpressionValue(object expression, Session session, object userdata);

    public static partial Type GetSessionExpressionReturnedType(object expression);

    public static partial int GetIntSessionExpressionValue(object expression, Session session);
    public static partial int GetIntSessionExpressionValue(object expression, Session session, object userdata);
    public static partial float GetFloatSessionExpressionValue(object expression, Session session);
    public static partial float GetFloatSessionExpressionValue(object expression, Session session, object userdata);
    public static partial bool GetBoolSessionExpressionValue(object expression, Session session);
    public static partial bool GetBoolSessionExpressionValue(object expression, Session session, object userdata);

    // not sure why frosthelper doesn't have an export for this
    public static string GetStringSessionExpressionValue(object expression, Session session, object userdata = null)
        => GetSessionExpressionValue(expression, session, userdata) switch
        {
            string str     => str,
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            { } obj        => obj.ToString() ?? ""
        };

    public static partial void RegisterSimpleSessionExpressionCommand(string modName, string cmdName, Func<Session, object> func);
    public static partial void RegisterFunctionSessionExpressionCommand(string modName, string cmdName, Func<Session, IReadOnlyList<object>, object> func);

    public static partial object CreateSessionExpressionContext(
        Dictionary<string, Func<Session, object, object>> simpleCommands,
        Dictionary<string, Func<Session, object, IReadOnlyList<object>, object>> functionCommands);

    #endregion
}
