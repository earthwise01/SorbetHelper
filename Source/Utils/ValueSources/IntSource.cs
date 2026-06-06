namespace Celeste.Mod.SorbetHelper.Utils.ValueSources;

public abstract class IntSource : IEquatable<IntSource>
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(IntSource)}";

    private sealed class LiteralInt(int value) : IntSource
    {
        protected override object EqualityIdentifier
            => value;

        public override int GetValue(Session session)
            => value;
    }
    
    private sealed class SessionCounter(string counterName) : IntSource
    {
        protected override object EqualityIdentifier
            => counterName;

        public override int GetValue(Session session)
            => session.GetCounter(counterName);
    }

    private sealed class SessionExpression(string expressionStr) : IntSource
    {
        private readonly FrostHelper.SessionExpression expression = new FrostHelper.SessionExpression(expressionStr);

        protected override object EqualityIdentifier
            => expressionStr;

        public override int GetValue(Session session)
            => expression.GetInt(session);
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
            if (str.TryRemovePrefix('#', out string counterName))
            {
                if (!counterName.IsWhiteSpace())
                    return new SessionCounter(counterName);

                Logger.Warn(LogID, $"Tried to create {nameof(IntSource)} for session counter with empty name!");
            }
            else if (str.TryRemovePrefix("expr:", out string expression))
            {
                if (!expression.IsWhiteSpace())
                    return new SessionExpression(expression);

                Logger.Warn(LogID, $"Tried to create {nameof(IntSource)} for empty session expression!");
            }
            else if (int.TryParse(str, out int parsedValue))
                return new LiteralInt(parsedValue);
            else
                Logger.Warn(LogID, $"Tried to create {nameof(IntSource)} for invalid source: {str}!");
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
