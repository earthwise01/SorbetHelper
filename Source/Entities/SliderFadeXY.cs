using System.Globalization;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity(              EntityDataID + "Global")] // global version is swapped to in mapdataprocessor based on data.Bool("global")
[CustomEntity(EntityDataID, EntityDataID + "Global")]
public class SliderFadeXY : Entity {
    public const string EntityDataID = "SorbetHelper/SliderFadeXY";

    private readonly string sliderName;
    private readonly Backdrop.Fader fadeX, fadeY;

    public SliderFadeXY(EntityData data, Vector2 offset) : base(data.Position + offset) {
        sliderName = data.Attr("slider", "");
        fadeX = CreateFader(data.Attr("fadeX", ""));
        fadeY = CreateFader(data.Attr("fadeY", ""));

        Tag |= Tags.TransitionUpdate;
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        // no empty slider attribute
        if (string.IsNullOrEmpty(sliderName))
            RemoveSelf();
    }

    public override void Update() {
        base.Update();

        Level level = SceneAs<Level>();
        Vector2 camera = level.Camera.Center;
        level.Session.SetSlider(sliderName, fadeX.Value(camera.X) * fadeY.Value(camera.Y));
    }

    private static Backdrop.Fader CreateFader(string raw) {
        Backdrop.Fader fader = new Backdrop.Fader();

        string[] zones = raw.Split(':');
        for (int i = 0; i < zones.Length; i++) {
            string[] zone = zones[i].Split(',');

            if (zone.Length == 2) {
                // values
                string[] values = zone[1].Split('-');
                if (values.Length != 2) {
                    if (values.Length < 2)
                        Logger.Warn(nameof(SorbetHelper), $"Fader formatting error! Less than 2 values specified for zone {i + 1}.");
                    if (values.Length > 2)
                        Logger.Warn(nameof(SorbetHelper), $"Fader formatting error! More than 2 values specified for zone {i + 1}.");

                    continue;
                }

                float fromValue = float.Parse(values[0], CultureInfo.InvariantCulture);
                float toValue = float.Parse(values[1], CultureInfo.InvariantCulture);

                // positions
                string[] positions = zone[0].Split('-');
                if (positions.Length != 2) {
                    if (positions.Length < 2)
                        Logger.Warn(nameof(SorbetHelper), $"Fader formatting error! Less than 2 positions specified for zone {i + 1}.");
                    if (positions.Length > 2)
                        Logger.Warn(nameof(SorbetHelper), $"Fader formatting error! More than 2 positions specified for zone {i + 1}. (Remember that negative numbers are prefixed with 'n' and not '-'!)");

                    continue;
                }

                int posFrom = 1;
                int posTo = 1;
                if (positions[0][0] == 'n') {
                    posFrom = -1;
                    positions[0] = positions[0].Substring(1);
                }

                if (positions[1][0] == 'n') {
                    posTo = -1;
                    positions[1] = positions[1].Substring(1);
                }

                fader.Add(posFrom * int.Parse(positions[0]), posTo * int.Parse(positions[1]), fromValue, toValue);
            } else if (zone.Length != 0) {
                Logger.Warn(nameof(SorbetHelper), $"Fader formatting error! Zone {i + 1} has wrong number of arguments. (Remember that each zone needs 1 pair of positions followed by 1 pair of values!)");
            }
        }

        return fader;
    }
}
