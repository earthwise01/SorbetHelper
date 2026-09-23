namespace Celeste.Mod.SorbetHelper.Utils.ValueSources;

public abstract class Condition(bool inverted = false) : IEquatable<Condition>
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(Condition)}";

    public sealed class None(bool defaultValue) : Condition(false)
    {
        public bool DefaultValue { get; } = defaultValue;

        protected override object EqualityIdentifier => DefaultValue;
        protected override bool RawCheck(Session session) => DefaultValue;
    }

    public sealed class Flag(string flagName, bool inverted = false) : Condition(inverted)
    {
        public string FlagName { get; } = flagName;

        protected override object EqualityIdentifier => FlagName;
        protected override bool RawCheck(Session session) => session is not null && session.GetFlag(FlagName);
    }

    public sealed class SessionExpression(FrostHelper.SessionExpression<bool> expression, bool inverted = false) : Condition(inverted)
    {
        public FrostHelper.SessionExpression<bool> Expression { get; } = expression;

        protected override object EqualityIdentifier => Expression.SourceText;
        protected override bool RawCheck(Session session) => session is not null && Expression.Get(session);
    }

    protected abstract object EqualityIdentifier { get; }
    protected abstract bool RawCheck(Session session);

    public bool Inverted { get; } = inverted;
    public bool Check(Session session) => RawCheck(session) ^ Inverted;

    public static Condition Create(string condition, bool inverted = false, bool defaultValue = true)
    {
        if (!string.IsNullOrWhiteSpace(condition))
        {
            if (condition.StartsWith('!'))
            {
                inverted = !inverted;
                condition = condition[1..];
            }

            if (condition.StartsWith("expr:", out string expressionStr))
            {
                if (FrostHelper.SessionExpression<bool>.CreateOrNull(expressionStr) is { } expression)
                    return new SessionExpression(expression, inverted);

                Logger.Warn(LogID, $"Tried to create {nameof(Condition)} for {(FrostHelper.IsImported ? $"invalid session expression `{expressionStr}`" : $"session expression `{expressionStr}`, but Frost Helper is not loaded")}!");
            }
            else
            {
                if (!condition.IsWhiteSpace())
                    return new Flag(condition, inverted);

                Logger.Warn(LogID, $"Tried to create {nameof(Condition)} for flag with empty name!");
            }
        }

        return new None(defaultValue);
    }

    #region Equality Members

    public bool Equals(Condition other) => other is not null && (ReferenceEquals(this, other) || (Equals(EqualityIdentifier, other.EqualityIdentifier) && Inverted == other.Inverted));
    public override bool Equals(object obj) => obj is not null && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((Condition)obj)));
    public override int GetHashCode() => HashCode.Combine(EqualityIdentifier, Inverted);
    public static bool operator ==(Condition left, Condition right) => Equals(left, right);
    public static bool operator !=(Condition left, Condition right) => !Equals(left, right);

    #endregion

}
