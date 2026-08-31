namespace Celeste.Mod.SorbetHelper.Utils.ValueSources;

public abstract class ColorSource : IEquatable<ColorSource>
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(ColorSource)}";

    private sealed class LiteralColor(Color color) : ColorSource
    {
        protected override object EqualityIdentifier
            => color;

        public override Color GetValue(Session session)
            => color;
    }

    private sealed class SessionCounter(string counterName) : ColorSource
    {
        protected override object EqualityIdentifier
            => counterName;

        public override Color GetValue(Session session)
            => Color.FromPackedInt(session.GetCounter(counterName));
    }

    private sealed class SessionExpression(string expressionStr) : ColorSource
    {
        private readonly FrostHelper.SessionExpression expression = new FrostHelper.SessionExpression(expressionStr);

        protected override object EqualityIdentifier
            => expressionStr;

        public override Color GetValue(Session session)
            => Color.FromPackedInt(expression.GetInt(session));
    }

    protected abstract object EqualityIdentifier { get; }

    public abstract Color GetValue(Session session);

    public static ColorSource Create(string source, string defaultHex = "ffffff")
    {
        if (!string.IsNullOrWhiteSpace(source))
        {
            // todo: is this necessaryyy i feel like its just confusing when color hex codes Are known to start with #
            //       and its only rly useful for avoiding a frosthelper dependency while keeping a microlithmisc dependency (feels unlikely)
            if (source.StartsWith("#", out string counterName))
            {
                if (!counterName.IsWhiteSpace())
                    return new SessionCounter(counterName);

                Logger.Warn(LogID, $"Tried to create {nameof(ColorSource)} for session counter with empty name!");
            }
            else if (source.StartsWith("expr:", out string expression))
            {
                if (!expression.IsWhiteSpace())
                    return new SessionExpression(expression);

                Logger.Warn(LogID, $"Tried to create {nameof(ColorSource)} for empty session expression!");
            }
            else
                return new LiteralColor(Calc.HexToColorWithNonPremultipliedAlpha(source));
        }

        return new LiteralColor(Calc.HexToColorWithNonPremultipliedAlpha(defaultHex));
    }

    #region Equality Members

    public bool Equals(ColorSource other)
        => other is not null && (ReferenceEquals(this, other) || Equals(EqualityIdentifier, other.EqualityIdentifier));
    public override bool Equals(object obj)
        => obj is not null && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((ColorSource)obj)));
    public override int GetHashCode()
        => EqualityIdentifier?.GetHashCode() ?? 0;
    public static bool operator ==(ColorSource left, ColorSource right)
        => Equals(left, right);
    public static bool operator !=(ColorSource left, ColorSource right)
        => !Equals(left, right);

    #endregion
}
