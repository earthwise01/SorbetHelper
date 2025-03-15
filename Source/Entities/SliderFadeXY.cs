using System.Globalization;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.SorbetHelper.Entities;

[GlobalEntity(                             "SorbetHelper/SliderFadeXYGlobal")]
[CustomEntity("SorbetHelper/SliderFadeXY", "SorbetHelper/SliderFadeXYGlobal")]
public class SliderFadeXY : Entity {
    private readonly string sliderName;

    public Backdrop.Fader FadeX, FadeY;

    public SliderFadeXY(EntityData data, Vector2 _) : base() {
        sliderName = data.Attr("slider", "");
        FadeX = CreateFader(data.Attr("fadeX", ""));
        FadeY = CreateFader(data.Attr("fadeY", ""));

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

        var level = Scene as Level;
        var camera = level.Camera.GetCenter();
        level.Session.SetSlider(sliderName, FadeX.Value(camera.X) * FadeY.Value(camera.Y));
    }

    private static Backdrop.Fader CreateFader(string raw) {
        var fader = new Backdrop.Fader();

        var zones = raw.Split(':');
        for (int i = 0; i < zones.Length; i++) {
            var zone = zones[i].Split(',');

            if (zone.Length == 2) {
                // values
                var values = zone[1].Split('-');
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
                var positions = zone[0].Split('-');
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
