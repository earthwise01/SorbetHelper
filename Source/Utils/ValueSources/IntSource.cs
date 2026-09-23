namespace Celeste.Mod.SorbetHelper.Utils.ValueSources;

public abstract class IntSource : IEquatable<IntSource>
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(IntSource)}";

    private sealed class LiteralInt(int value) : IntSource
    {
        protected override object EqualityIdentifier => value;
        public override int GetValue(Session session) => value;
    }
    
    private sealed class SessionCounter(string counterName) : IntSource
    {
        protected override object EqualityIdentifier => counterName;
        public override int GetValue(Session session) => session.GetCounter(counterName);
    }

    private sealed class SessionExpression(FrostHelper.SessionExpression<int> expression) : IntSource
    {
        protected override object EqualityIdentifier => expression.SourceText;
        public override int GetValue(Session session) => expression.Get(session);
    }

    protected abstract object EqualityIdentifier { get; }
    public abstract int GetValue(Session session);

    public static IntSource Create(object source, int defaultValue = 0)
    {
        if (source is int value)
            return new LiteralInt(value);

        string str = source?.ToString();
        if (!string.IsNullOrWhiteSpace(str))
        {
            if (str.StartsWith('#', out string counterName))
            {
                if (!counterName.IsWhiteSpace())
                    return new SessionCounter(counterName);

                Logger.Warn(LogID, $"Tried to create {nameof(IntSource)} for session counter with empty name!");
            }
            else if (str.StartsWith("expr:", out string expressionStr))
            {
                if (FrostHelper.SessionExpression<int>.CreateOrNull(expressionStr) is { } expression)
                    return new SessionExpression(expression);

                Logger.Warn(LogID, $"Tried to create {nameof(IntSource)} for {(FrostHelper.IsImported ? $"invalid session expression `{expressionStr}`" : $"session expression `{expressionStr}`, but Frost Helper is not loaded")}!");
            }
            else if (int.TryParse(str, out int parsedValue))
                return new LiteralInt(parsedValue);
            else
                Logger.Warn(LogID, $"Tried to create {nameof(IntSource)} for invalid source: `{str}`!");
        }

        return new LiteralInt(defaultValue);
    }

    #region Equality Members

    public bool Equals(IntSource other)
        => other is not null && (ReferenceEquals(this, other) || Equals(EqualityIdentifier, other.EqualityIdentifier));
    public override bool Equals(object obj)
        => obj is not null && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((IntSource)obj)));
    public override int GetHashCode()
        => EqualityIdentifier?.GetHashCode() ?? 0;
    public static bool operator ==(IntSource left, IntSource right)
        => Equals(left, right);
    public static bool operator !=(IntSource left, IntSource right)
        => !Equals(left, right);

    #endregion
}
