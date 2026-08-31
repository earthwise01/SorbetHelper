namespace Celeste.Mod.SorbetHelper.Utils.ValueSources;

public abstract class FloatSource : IEquatable<FloatSource>
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(FloatSource)}";

    private sealed class LiteralFloat(float value) : FloatSource
    {
        protected override object EqualityIdentifier
            => value;

        public override float GetValue(Session session)
            => value;
    }

    private sealed class SessionSlider(string sliderName) : FloatSource
    {
        protected override object EqualityIdentifier
            => sliderName;

        public override float GetValue(Session session)
            => session.GetSlider(sliderName);
    }

    private sealed class SessionExpression(string expressionStr) : FloatSource
    {
        private readonly FrostHelper.SessionExpression expression = new FrostHelper.SessionExpression(expressionStr);

        protected override object EqualityIdentifier
            => expressionStr;

        public override float GetValue(Session session)
            => expression.GetFloat(session);
    }

    protected abstract object EqualityIdentifier { get; }

    public abstract float GetValue(Session session);

    public static FloatSource Create(object source, float defaultValue = 0f)
    {
        if (source is float value)
            return new LiteralFloat(value);

        string str = source?.ToString();
        if (!string.IsNullOrWhiteSpace(str))
        {
            if (str.StartsWith('@', out string sliderName))
            {
                if (!sliderName.IsWhiteSpace())
                    return new SessionSlider(sliderName);

                Logger.Warn(LogID, $"Tried to create {nameof(FloatSource)} for session slider with empty name!");
            }
            else if (str.StartsWith("expr:", out string expression))
            {
                if (!expression.IsWhiteSpace())
                    return new SessionExpression(expression);

                Logger.Warn(LogID, $"Tried to create {nameof(FloatSource)} for empty session expression!");
            }
            else if (float.TryParse(str, out float parsedValue))
                return new LiteralFloat(parsedValue);
            else
                Logger.Warn(LogID, $"Tried to create {nameof(FloatSource)} for invalid source: {str}!");
        }

        return new LiteralFloat(defaultValue);
    }

    #region Equality Members

    public bool Equals(FloatSource other)
        => other is not null && (ReferenceEquals(this, other) || Equals(EqualityIdentifier, other.EqualityIdentifier));
    public override bool Equals(object obj)
        => obj is not null && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((FloatSource)obj)));
    public override int GetHashCode()
        => EqualityIdentifier?.GetHashCode() ?? 0;
    public static bool operator ==(FloatSource left, FloatSource right)
        => Equals(left, right);
    public static bool operator !=(FloatSource left, FloatSource right)
        => !Equals(left, right);

    #endregion
}
