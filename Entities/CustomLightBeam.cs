using Celeste.Mod.Entities;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/CustomLightbeam")]
    public class CustomLightBeam : Entity {

        private MTexture texture = GFX.Game["util/lightbeam"];

        private Color color = new Color(0.8f, 1f, 1f);

        private float baseAlpha;

        private float flagAlpha;

        private float alpha;

        public int LightWidth;

        public int LightLength;

        public float Rotation;

        public string Flag;

        public bool Rainbow;

        private float timer = Calc.Random.NextFloat(1000f);

        public CustomLightBeam(EntityData data, Vector2 offset) : base(data.Position + offset) {
            base.Tag = Tags.TransitionUpdate;
            base.Depth = -9998;
            LightWidth = data.Width;
            LightLength = data.Height;
            Flag = data.Attr("flag", "");
            Rainbow = data.Bool("rainbow", false);
            Rotation = data.Float("rotation", 0f) * ((float)Math.PI / 180f);
        }

        public override void Update() {
            timer += Engine.DeltaTime;
            Level level = base.Scene as Level;
            Player entity = base.Scene.Tracker.GetEntity<Player>();
            if (entity != null) {
                Vector2 vector = Calc.AngleToVector(Rotation + (float)Math.PI / 2f, 1f);
                Vector2 vector2 = Calc.ClosestPointOnLine(Position, Position + vector * 10000f, entity.Center);
                float target = Math.Min(1f, Math.Max(0f, (vector2 - Position).Length() - 8f) / (float)LightLength);
                if ((vector2 - entity.Center).Length() > (float)LightWidth / 2f) {
                    target = 1f;
                }
                if (level.Transitioning) {
                    target = 0f;
                }
                baseAlpha = Calc.Approach(baseAlpha, target, Engine.DeltaTime * 4f);
            }
            if (!string.IsNullOrEmpty(Flag)) {
                float flagTarget;
                if (level.Session.GetFlag(Flag)) {
                    flagTarget = 0f;
                } else {
                    flagTarget = 1f;
                }
                flagAlpha = Calc.Approach(flagAlpha, flagTarget, Engine.DeltaTime * 2f);
            }
            alpha = baseAlpha * flagAlpha;
            if (alpha >= 0.5f && level.OnInterval(0.8f)) {
                Vector2 vector3 = Calc.AngleToVector(Rotation + (float)Math.PI / 2f, 1f);
                Vector2 position = Position - vector3 * 4f;
                float num = Calc.Random.Next(LightWidth - 4) + 2 - LightWidth / 2;
                position += num * vector3.Perpendicular();
                Color particleColor = Rainbow ? GetHue(position) : Color.White;
                level.Particles.Emit(LightBeam.P_Glow, position, particleColor, Rotation + (float)Math.PI / 2f);                }
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
            if (Rainbow) {
                color = GetHue(Position + Calc.AngleToVector(Rotation, 1f) * offset);
            }
            if (width >= 1f) {
                texture.Draw(Position + Calc.AngleToVector(Rotation, 1f) * offset, new Vector2(0f, 0.5f), color * a * alpha, new Vector2(1f / (float)texture.Width * length, width), rotation);
            }
        }

        private Color GetHue(Vector2 position) {
            float value = (position.Length() + Scene.TimeActive * 50f) % 280f / 280f;
            float hue = 0.4f + Calc.YoYo(value) * 0.4f;
            return Calc.HsvToColor(hue, 0.4f, 0.9f);
        }
    }
}
