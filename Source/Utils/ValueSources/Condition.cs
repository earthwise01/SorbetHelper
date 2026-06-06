namespace Celeste.Mod.SorbetHelper.Utils.ValueSources;

public abstract class Condition(bool inverted = false)
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(Condition)}";

    public sealed class Empty(bool defaultValue) : Condition(false)
    {
        public bool DefaultValue { get; } = defaultValue;

        protected override bool RawCheck(Session session)
            => DefaultValue;
    }

    public sealed class Flag(string flagName, bool inverted = false) : Condition(inverted)
    {
        public string FlagName { get; } = flagName;

        protected override bool RawCheck(Session session)
            => session is not null && session.GetFlag(FlagName);
    }

    public sealed class SessionExpression(string expression, bool inverted = false) : Condition(inverted)
    {
        public FrostHelper.SessionExpression Expression { get; } = new FrostHelper.SessionExpression(expression);

        protected override bool RawCheck(Session session)
            => session is not null && Expression.GetBool(session);
    }

    public bool Inverted { get; } = inverted;

    public bool Check(Session session)
        => RawCheck(session) ^ Inverted;

    protected abstract bool RawCheck(Session session);

    public static Condition Create(string condition, bool inverted = false, bool defaultValue = true)
    {
        if (!string.IsNullOrWhiteSpace(condition))
        {
            // kinda awkward maybee
            if (condition.TryRemovePrefix('!', out condition))
                inverted = !inverted;

            if (condition.TryRemovePrefix("expr:", out string expression))
            {
                if (!expression.IsWhiteSpace())
                    return new SessionExpression(expression, inverted);

                Logger.Warn(LogID, $"Tried to create {nameof(Condition)} for empty session expression!");
            }
            else if (!condition.IsWhiteSpace())
                return new Flag(condition, inverted);
            else
                Logger.Warn(LogID, $"Tried to create {nameof(Condition)} for flag with empty name!");
        }

        return new Empty(defaultValue);
    }
}
