namespace Celeste.Mod.SorbetHelper.Utils;

internal static class SessionExpressionCommands
{
    private const string DefaultColorId = "default",
                         WhitespaceColorId = "whitespace",
                         OperatorColorId = "operator",
                         LiteralColorId = "literal",
                         StringContentColorId = "string",
                         FlagColorId = "flag",
                         CounterColorId = "counter",
                         SliderColorId = "slider",
                         FieldColorId = "field",
                         CommandColorId = "command",
                         TypeColorId = "type";

    private const string ModPrefix = "sorbet";

    internal static void RegisterCommands()
    {
        if (!FrostHelper.IsImported)
            return;

        FrostHelper.RegisterFunctionSessionExpressionCommandV2(ModPrefix, "hsl", null, [
            ("Creates a ", DefaultColorId, null),
            (nameof(Color), TypeColorId, null),
            (" using h, s, l values, assumed to be in range 0-1.", DefaultColorId, null)
        ], Hsl);

        FrostHelper.RegisterFunctionSessionExpressionCommandV2(ModPrefix, "hsla", null, [
            ("Creates a ", DefaultColorId, null),
            (nameof(Color), TypeColorId, null),
            (" using h, s, l values, assumed to be in range 0-1. Alpha is in range 0-255.", DefaultColorId, null)
        ], Hsla);

        FrostHelper.RegisterFunctionSessionExpressionCommandV2(ModPrefix, "oklab", null, [
            ("Creates a ", DefaultColorId, null),
            (nameof(Color), TypeColorId, null),
            (" using Oklab L, a, b values, assumed to be in ranges 0-1, -0.4-0.4, -0.4-0.4.", DefaultColorId, null),
            // "\nNot properly gamut clipped, colors outside the sRGB gamut may come out weirdly.", DefaultColorId, null)
        ], Oklab);

        FrostHelper.RegisterFunctionSessionExpressionCommandV2(ModPrefix, "oklch", null, [
            ("Creates a ", DefaultColorId, null),
            (nameof(Color), TypeColorId, null),
            (" using Oklch L, C, h values, assumed to be in ranges 0-1, 0-~0.4, 0-360.", DefaultColorId, null),
            // ("\nNot properly gamut clipped, colors outside the sRGB gamut may come out weirdly.", DefaultColorId, null)
        ], Oklch);

        FrostHelper.RegisterFunctionSessionExpressionCommandV2(ModPrefix, "lerpOklab", null, [
            ("Performs a linear interpolation between two colors based on the given weight, within the Oklab color space.", DefaultColorId, null)
        ], LerpOklab);

        FrostHelper.RegisterFunctionSessionExpressionCommandV2(ModPrefix, "fromNonPremult", null, [
            ("Translate a non-premultipled alpha ", DefaultColorId, null),
            (nameof(Color), TypeColorId, null),
            (" to a ", DefaultColorId, null),
            (nameof(Color), TypeColorId, null),
            (" that contains premultiplied alpha.", DefaultColorId, null)
        ], FromNonPremult);
    }

    // hmm
    // probably removeable if/when microlithmisc gets built in support for rgba colours instead of just abgr (aka packedvalues)
    // private static int PackHexAbgr(Session session, object userdata, Color color)
    //     => Calc.ColorToHexAbgr(color);
    // private static Color UnpackHexAbgr(Session session, object userdata, int packedColor)
    //     => Calc.HexToColorAbgr(packedColor);

    // mayb these shouldve also been frosthelper requests but i dont wanna b annoyingg weh
    private static Color Hsl(Session session, object userdata, float h, float s, float l)
        => Calc.HslToColor(Calc.Mod(h, 1f), s, l);
    private static Color Hsla(Session session, object userdata, float h, float s, float l, int a)
        => Calc.HslToColor(Calc.Mod(h, 1f), s, l) with { A = (byte)a };

    private static Color Oklab(Session session, object userdata, float L, float a, float b)
        => Calc.OklabToColor(L, a, b);

    private static Color Oklch(Session session, object userdata, float L, float C, float h)
    {
        (float a, float b) = (C * MathF.Cos(h), C * MathF.Sin(h));
        return Calc.OklabToColor(L, a, b);
    }

    private static Color LerpOklab(Session session, object userdata, Color color1, Color color2, float amount)
        => Color.LerpOklab(color1, color2, amount);

    private static Color FromNonPremult(Session session, object userdata, Color color)
        => Color.FromNonPremultiplied(color.R, color.G, color.B, color.A);

}
