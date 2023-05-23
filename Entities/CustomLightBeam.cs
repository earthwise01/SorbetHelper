using Celeste.Mod.Entities;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/CustomLightbeam")]
    public class CustomLightBeam : Entity {

        private MTexture texture = GFX.Game["util/lightbeam"];

        private Color color = new Color(0.8f, 1f, 1f);

        private List<Color> rainbowColors = new List<Color>();
        private float rainbowGradientSize;
        private float rainbowGradientSpeed;
        private bool rainbowLoopColors;
        private Vector2 rainbowCenter;
        private bool rainbowSingleColor;

        private bool showParticles;

        private float baseAlpha = 1;

        private float flagAlpha = 1;

        private float alpha;

        public int LightWidth;

        public int LightLength;

        public float Rotation;

        public string Flag;
        public bool Inverted;

        public bool Rainbow;

        public bool FadeWhenNear = true;
        public bool FadeOnTransition = true;

        private float timer = Calc.Random.NextFloat(1000f);

        public CustomLightBeam(EntityData data, Vector2 offset) : base(data.Position + offset) {
            base.Tag = Tags.TransitionUpdate;
            base.Depth = data.Int("depth", -9998);
            LightWidth = data.Width;
            LightLength = data.Height;
            Flag = data.Attr("flag", "");
            Inverted = data.Bool("inverted", false);
            Rotation = data.Float("rotation", 0f) * ((float)Math.PI / 180f);
            showParticles = data.Bool("particles", true);
            FadeWhenNear = data.Bool("fadeWhenNear", true);
            FadeOnTransition = data.Bool("fadeOnTransition", true);
            color = Calc.HexToColor(data.Attr("color", "CCFFFF"));
            Rainbow = data.Bool("rainbow", false);

            if (Rainbow) {
                rainbowGradientSize = data.Float("gradientSize", 280f);
                rainbowGradientSpeed = data.Float("gradientSpeed", 50f);
                rainbowLoopColors = data.Bool("loopColors", false);
                rainbowCenter = new Vector2(data.Float("centerX", 0), data.Float("centerY", 0));
                rainbowSingleColor = data.Bool("singleColor", false);

                string[] colorsAsStrings = data.Attr("colors", "89E5AE,88E0E0,87A9DD,9887DB,D088E2").Split(',');
                for (int i = 0; i < colorsAsStrings.Length; i++) {
                    rainbowColors.Add(Calc.HexToColor(colorsAsStrings[i]));
                }
                if (rainbowLoopColors) {
                    rainbowColors.Add(rainbowColors[0]);
                }
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            Level level = base.Scene as Level;
            if (!string.IsNullOrEmpty(Flag) && (!Inverted && !level.Session.GetFlag(Flag))
            || (Inverted && level.Session.GetFlag(Flag))) {
                flagAlpha = 0f;
            } else {
                flagAlpha = 1f;
            }
            if (level.Transitioning && FadeOnTransition) {
                baseAlpha = 0f;
            }
        }

        public override void Update() {
            timer += Engine.DeltaTime;
            Level level = base.Scene as Level;
            Player entity = base.Scene.Tracker.GetEntity<Player>();
            if (entity != null) {
                float target = 1f;
                if (FadeWhenNear) {
                    Vector2 vector = Calc.AngleToVector(Rotation + (float)Math.PI / 2f, 1f);
                    Vector2 vector2 = Calc.ClosestPointOnLine(Position, Position + vector * 10000f, entity.Center);
                    target = Math.Min(1f, Math.Max(0f, (vector2 - Position).Length() - 8f) / (float)LightLength);
                    if ((vector2 - entity.Center).Length() > (float)LightWidth / 2f) {
                        target = 1f;
                    }
                }
                if (level.Transitioning && FadeOnTransition) {
                    target = 0f;
                }
                baseAlpha = Calc.Approach(baseAlpha, target, Engine.DeltaTime * 4f);
            }
            if (!string.IsNullOrEmpty(Flag)) {
                float flagTarget;
                if ((!Inverted && !level.Session.GetFlag(Flag))
                || (Inverted && level.Session.GetFlag(Flag))) {
                    flagTarget = 0f;
                } else {
                    flagTarget = 1f;
                }
                flagAlpha = Calc.Approach(flagAlpha, flagTarget, Engine.DeltaTime * 2f);
            }
            alpha = baseAlpha * flagAlpha;
            if (Rainbow && rainbowSingleColor && level.OnInterval(0.08f)) {
                color = GetHue(Position);
            }
            if (showParticles && alpha >= 0.5f && level.OnInterval(0.8f)) {
                Vector2 vector3 = Calc.AngleToVector(Rotation + (float)Math.PI / 2f, 1f);
                Vector2 position = Position - vector3 * 4f;
                float num = Calc.Random.Next(LightWidth - 4) + 2 - LightWidth / 2;
                position += num * vector3.Perpendicular();
                level.Particles.Emit(LightBeam.P_Glow, position, (Rainbow && !rainbowSingleColor) ? GetHue(position) : color, Rotation + (float)Math.PI / 2f);
            }
            base.Update();
        }

        public override void Render() {
            if (alpha > 0f) {
                DrawTexture(0f, LightWidth, (float)(LightLength - 4) + (float)Math.Sin(timer * 2f) * 4f, 0.4f);
                for (int i = 0; i < LightWidth; i += 4) {
                    float num = timer + (float)i * 0.6f;
                    float num2 = 4f + (float)Math.Sin(num * 0.5f + 1.2f) * 4f;
                    float offset = (float)Math.Sin((double)((num + (float)(i * 32)) * 0.1f) + Math.Sin(num * 0.05f + (float)i * 0.1f) * 0.25) * ((float)LightWidth / 2f - num2 / 2f);
                    float length = (float)LightLength + (float)Math.Sin(num * 0.25f) * 8f;
                    float a = 0.6f + (float)Math.Sin(num + 0.8f) * 0.3f;
                    DrawTexture(offset, num2, length, a);
                }
            }
        }

        private void DrawTexture(float offset, float width, float length, float a) {
            float rotation = Rotation + (float)Math.PI / 2f;
            if (Rainbow && !rainbowSingleColor) {
                color = GetHue(Position + Calc.AngleToVector(Rotation, 1f) * offset);
            }
            if (width >= 1f) {
                texture.Draw(Position + Calc.AngleToVector(Rotation, 1f) * offset, new Vector2(0f, 0.5f), color * a * alpha, new Vector2(1f / (float)texture.Width * length, width), rotation);
            }
        }

        private Color GetHue(Vector2 position) {
            if (rainbowColors.Count == 1) {
                return rainbowColors[0];
            }

            float progress = (position - rainbowCenter).Length() + this.Scene.TimeActive * rainbowGradientSpeed;
            while (progress < 0) {
                progress += rainbowGradientSize;
            }
            progress = progress % rainbowGradientSize / rainbowGradientSize;
            if (!rainbowLoopColors) {
                progress = Calc.YoYo(progress);
            }

            if (progress == 1) {
                return rainbowColors[rainbowColors.Count - 1];
            }

            float globalProgress = (rainbowColors.Count - 1) * progress;
            int colorIndex = (int) globalProgress;
            float progressInIndex = globalProgress - colorIndex;
            return Color.Lerp(rainbowColors[colorIndex], rainbowColors[colorIndex + 1], progressInIndex);
        }
    }
}
