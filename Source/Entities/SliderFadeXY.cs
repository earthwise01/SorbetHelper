using System.Globalization;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity(           EntitySID + "Global")] // global version is swapped to in mapdataprocessor based on data.Bool("global")
[CustomEntity(EntitySID, EntitySID + "Global")]
public class SliderFadeXY : Entity
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(SliderFadeXY)}";

    public const string EntitySID = "SorbetHelper/SliderFadeXY";

    private readonly string sliderName;
    private readonly Backdrop.Fader fadeX, fadeY;

    public SliderFadeXY(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        sliderName = data.Attr("slider", "");
        fadeX = CreateFader(data.Attr("fadeX", ""));
        fadeY = CreateFader(data.Attr("fadeY", ""));

        Tag |= Tags.TransitionUpdate;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (string.IsNullOrEmpty(sliderName))
            RemoveSelf();
    }

    public override void Update()
    {
        base.Update();

        Level level = SceneAs<Level>();
        Vector2 camera = level.Camera.Center;
        level.Session.SetSlider(sliderName, fadeX.Value(camera.X) * fadeY.Value(camera.Y));
    }

    private static Backdrop.Fader CreateFader(string raw)
    {
        Backdrop.Fader fader = new Backdrop.Fader();

        string[] zones = raw.Split(':');
        for (int i = 0; i < zones.Length; i++)
        {
            string[] zone = zones[i].Split(',');

            // todo: hmm i still feel like this could b tidier
            if (zone.Length == 2)
            {
                // values
                string[] values = zone[1].Split('-');

                if (values.Length != 2)
                {
                    Logger.Warn(LogID, $"Fader formatting error! Wrong number of values specified for zone {i + 1} ({values.Length} instead of 2).");
                    continue;
                }

                if (!TryParseWarnOnFailure(values[0], "Could not parse value", out float fromValue)
                    || !TryParseWarnOnFailure(values[1], "Could not parse value", out float toValue))
                    continue;

                // positions
                string[] positions = zone[0].Split('-');

                if (positions.Length != 2)
                {
                    Logger.Warn(LogID, $"Fader formatting error! Wrong number of positions specified for zone {i + 1} ({positions.Length} instead of 2). Remember that negative numbers are prefixed with `n` and not `-`!");
                    continue;
                }

                int fromPositionSign = 1;
                if (positions[0][0] == 'n')
                {
                    fromPositionSign = -1;
                    positions[0] = positions[0].Substring(1);
                }

                if (!TryParseWarnOnFailure(positions[0], "Could not parse position", out int fromPosition))
                    continue;

                int toPositionSign = 1;
                if (positions[1][0] == 'n')
                {
                    toPositionSign = -1;
                    positions[1] = positions[1].Substring(1);
                }

                if (!TryParseWarnOnFailure(positions[1], "Could not parse position", out int toPosition))
                    continue;

                fader.Add(fromPositionSign * fromPosition, toPositionSign * toPosition, fromValue, toValue);

                static bool TryParseWarnOnFailure<T>(string s, string error, out T result) where T : struct, IParsable<T>
                {
                    if (T.TryParse(s, CultureInfo.InvariantCulture, out result))
                        return true;

                    Logger.Warn(LogID, $"Fader formatting error! {error} {s}.");
                    return false;
                }
            }
            else if (zone.Length != 0)
            {
                Logger.Warn(LogID, $"Fader formatting error! Zone {i + 1} has wrong number of arguments ({zone.Length} instead of 2). Each zone needs 1 pair of positions followed by 1 pair of values!");
            }
        }

        return fader;
    }
}
