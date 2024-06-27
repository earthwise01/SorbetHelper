using Celeste.Mod.Entities;
using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/CustomLightbeam")]
    public class CustomLightBeam : Entity {
        public string flag;
        public bool inverted;
        private readonly float flagFadeTime;

        public float rotation;

        private readonly bool fadeWhenNear = true;
        private readonly bool fadeOnTransition = true;

        private readonly bool rainbow;

        // used only when either rainbow == true && rainbowSingleColor == true or rainbow == false, otherwise ignored in favor of directly calling GetHue.
        // probably slightly messy but results in less unnecessary changes to the variable.
        public Color color = new Color(0.8f, 1f, 1f);

        private readonly bool useCustomRainbowColors;

        public List<Color> rainbowColors = new List<Color>();
        private readonly float rainbowGradientSize;
        private readonly float rainbowGradientSpeed;
        private readonly bool rainbowLoopColors;
        private Vector2 rainbowCenter;
        private readonly bool rainbowSingleColor;

        private const int rainbowSegmentSize = 4;

        private readonly bool noParticles;

        private float baseAlpha = 1;
        private float flagAlpha = 1;
        private float alpha;

        private readonly int lightWidth;
        private readonly int lightLength;

        private float timer = Calc.Random.NextFloat(1000f);

        private readonly MTexture beamTexture = GFX.Game["util/lightbeam"];

        private VirtualRenderTarget lightbeamTarget;
        private Matrix targetMatrix;
        private readonly float offset;

        private readonly float rectangleTop, rectangleBottom, rectangleLeft, rectangleRight;
        private bool visibleOnCamera, wasVisibleOnCamera;
        private const int visibilityPadding = 16;

        public CustomLightBeam(EntityData data, Vector2 offset) : base(data.Position + offset) {
            base.Tag = Tags.TransitionUpdate;
            this.offset = Calc.Random.NextFloat();

            lightWidth = data.Width;
            lightLength = data.Height;
            base.Depth = data.Int("depth", -9998);

            flag = data.Attr("flag");
            inverted = data.Bool("inverted", false);
            flagFadeTime = Math.Max(data.Float("flagFadeTime", 0.25f), 0f);
            rotation = data.Float("rotation", 0f) * ((float)Math.PI / 180f);

            fadeWhenNear = data.Bool("fadeWhenNear", true);
            fadeOnTransition = data.Bool("fadeOnTransition", true);

            float alpha = Math.Clamp(data.Float("alpha", 1f), 0f, 1f);

            noParticles = data.Bool("noParticles", false);
            color = Utils.HexToRGBAColor(data.Attr("color", "CCFFFF")) * alpha;
            rainbow = data.Bool("rainbow", false);
            useCustomRainbowColors = data.Bool("useCustomRainbowColors", false);

            if (rainbow) {
                rainbowGradientSize = data.Float("gradientSize", 280f);
                rainbowGradientSpeed = data.Float("gradientSpeed", 50f);
                rainbowLoopColors = data.Bool("loopColors", false);
                rainbowCenter = new Vector2(data.Float("centerX", 0), data.Float("centerY", 0));
                rainbowSingleColor = data.Bool("singleColor", false);

                string[] colorsAsStrings = data.Attr("colors", "89E5AE,88E0E0,87A9DD,9887DB,D088E2").Split(',');
                for (int i = 0; i < colorsAsStrings.Length; i++) {
                    rainbowColors.Add(Utils.HexToRGBAColor(colorsAsStrings[i]) * alpha);
                }
                if (rainbowLoopColors) {
                    rainbowColors.Add(rainbowColors[0]);
                }
            }

            // offscreen culling stuff
            // kinda janky i think but it works
            Vector2 baseCornerA = Position - Calc.AngleToVector(rotation, 1f) * (lightWidth / 2f); // would be top left with a rotation of 0f
            Vector2 baseCornerB = Position + Calc.AngleToVector(rotation, 1f) * (lightWidth / 2); // would be top right with a rotation of 0f
            Vector2 edgeCornerA = baseCornerA + Calc.AngleToVector(rotation + (float)Math.PI / 2f, 1f) * lightLength; // would be bottom left with a rotation of 0f
            Vector2 edgeCornerB = baseCornerB + Calc.AngleToVector(rotation + (float)Math.PI / 2f, 1f) * lightLength; // would be bottom right with a rotation of 0f

            rectangleTop = Math.Min(Math.Min(baseCornerA.Y, baseCornerB.Y), Math.Min(edgeCornerA.Y, edgeCornerB.Y));
            rectangleBottom = Math.Max(Math.Max(baseCornerA.Y, baseCornerB.Y), Math.Max(edgeCornerA.Y, edgeCornerB.Y));
            rectangleLeft = Math.Min(Math.Min(baseCornerA.X, baseCornerB.X), Math.Min(edgeCornerA.X, edgeCornerB.X));
            rectangleRight = Math.Max(Math.Max(baseCornerA.X, baseCornerB.X), Math.Max(edgeCornerA.X, edgeCornerB.X));

            Add(new BeforeRenderHook(BeforeRender));
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            Level level = base.Scene as Level;

            if (!string.IsNullOrEmpty(flag) && !level.Session.GetFlag(flag, inverted)) {
                flagAlpha = 0f;
            } else {
                flagAlpha = 1f;
            }

            if (level.Transitioning && fadeOnTransition) {
                baseAlpha = 0f;
            }

            // initialize render target and matrix
            lightbeamTarget ??= VirtualContent.CreateRenderTarget("sorbet-custom-lightbeam", (int)(rectangleRight - rectangleLeft + visibilityPadding), (int)(rectangleBottom - rectangleTop + visibilityPadding));
            Vector2 offset = Calc.AngleToVector(rotation - (float)Math.PI / 2f, lightLength) / 2f;
            targetMatrix = Matrix.CreateTranslation(-Position.X + lightbeamTarget.Width / 2f, -Position.Y + lightbeamTarget.Height / 2f, 0f) * Matrix.CreateTranslation(offset.X, offset.Y, 0f);
            RenderToTarget();
        }

        // clean up render target
        public override void Removed(Scene scene) {
            Dispose();
            base.Removed(scene);
        }

        public override void SceneEnd(Scene scene) {
            Dispose();
            base.SceneEnd(scene);
        }

        public void Dispose() {
            lightbeamTarget?.Dispose();
            lightbeamTarget = null;
        }

        public override void Update() {
            timer += Engine.DeltaTime;
            Level level = base.Scene as Level;
            Player entity = base.Scene.Tracker.GetEntity<Player>();

            wasVisibleOnCamera = visibleOnCamera;
            visibleOnCamera = InView(level.Camera);

            // vanilla lightbeam fading
            if (entity != null) {
                float target = 1f;
                if (fadeWhenNear) {
                    Vector2 vector = Calc.AngleToVector(rotation + (float)Math.PI / 2f, 1f);
                    Vector2 vector2 = Calc.ClosestPointOnLine(Position, Position + vector * 10000f, entity.Center);
                    target = Math.Min(1f, Math.Max(0f, (vector2 - Position).Length() - 8f) / lightLength);
                    if ((vector2 - entity.Center).Length() > lightWidth / 2f) {
                        target = 1f;
                    }
                }
                if (level.Transitioning && fadeOnTransition) {
                    target = 0f;
                }
                baseAlpha = Calc.Approach(baseAlpha, target, Engine.DeltaTime * 4f);
            }

            // fade flagAlpha towards either 0f or 1f depending on the flag state.
            if (!string.IsNullOrEmpty(flag)) {
                float flagTarget;
                if (!level.Session.GetFlag(flag, inverted)) {
                    flagTarget = 0f;
                } else {
                    flagTarget = 1f;
                }
                flagAlpha = Calc.Approach(flagAlpha, flagTarget, Engine.DeltaTime / flagFadeTime);
            }

            // multiply baseAlpha and flagAlpha together to get the actual alpha of the lightbeam.
            alpha = baseAlpha * flagAlpha;

            // emit particles
            if (visibleOnCamera && !noParticles && alpha >= 0.5f && level.OnInterval(0.8f, offset)) {
                Vector2 vector3 = Calc.AngleToVector(rotation + (float)Math.PI / 2f, 1f);
                Vector2 position = Position - vector3 * 4f;
                float num = Calc.Random.Next(lightWidth - 4) + 2 - lightWidth / 2;
                position += num * vector3.Perpendicular();
                // if rainbow is enabled and rainbowSingleColor is disabled, call GetHue for the particle's color, otherwise use the color variable.
                level.Particles.Emit(LightBeam.P_Glow, position, (rainbow && !rainbowSingleColor) ? GetHue(position) : color, rotation + (float)Math.PI / 2f);
            }

            base.Update();
        }

        /* public override void DebugRender(Camera camera) {
            base.DebugRender(camera);

            Vector2 baseCornerA = Position - Calc.AngleToVector(rotation, 1f) * (lightWidth / 2f); // would be top left with a rotation of 0f
            Vector2 baseCornerB = Position + Calc.AngleToVector(rotation, 1f) * (lightWidth / 2); // would be top right with a rotation of 0f
            Vector2 edgeCornerA = baseCornerA + Calc.AngleToVector(rotation + (float)Math.PI / 2f, 1f) * lightLength; // would be bottom left with a rotation of 0f
            Vector2 edgeCornerB = baseCornerB + Calc.AngleToVector(rotation + (float)Math.PI / 2f, 1f) * lightLength; // would be bottom right with a rotation of 0f
            Draw.Line(baseCornerA, baseCornerB, Color.GreenYellow * 0.5f);
            Draw.Line(baseCornerA, edgeCornerA, Color.GreenYellow * 0.5f);
            Draw.Line(baseCornerB, edgeCornerB, Color.GreenYellow * 0.5f);
            Draw.Line(edgeCornerA, edgeCornerB, Color.GreenYellow * 0.5f);

            Draw.HollowRect(rectangleLeft - visibilityPadding / 2, rectangleTop - visibilityPadding / 2, rectangleRight - rectangleLeft + visibilityPadding, rectangleBottom - rectangleTop + visibilityPadding, Color.Yellow * 0.5f);
        } */

        public void BeforeRender() {
            // only update graphics every 3 frames when on camera or when the lightbeam comes on screen
            if ((!visibleOnCamera || !Scene.OnRawInterval(0.05f, offset)) && visibleOnCamera == wasVisibleOnCamera)
                return;

            RenderToTarget();
        }

        public override void Render() {
            if (!visibleOnCamera)
                return;

            if (alpha > 0f) {
                Draw.SpriteBatch.Draw(lightbeamTarget, new Vector2(rectangleLeft - visibilityPadding / 2, rectangleTop - visibilityPadding / 2), null, Color.White * alpha, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }
        }

        private void RenderToTarget() {
            Engine.Graphics.GraphicsDevice.SetRenderTarget(lightbeamTarget);
            Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, targetMatrix);

            RenderLightbeam();

            Draw.SpriteBatch.End();
        }

        private void RenderLightbeam() {
            // update the hue of the rainbow lightbeam when rainbowSingleColor is enabled, otherwise GetHue is called directly whenever a color is needed.
            if (rainbow && rainbowSingleColor)
                color = GetHue(Position);

            // render the lightbeam
            if (rainbow && !rainbowSingleColor) {
                // draw the base in 4px segments to make a gradient effect
                for (int i = 0; i < lightWidth; i += rainbowSegmentSize) {
                    DrawBeam(i - lightWidth / 2f, rainbowSegmentSize, lightLength - 4 + (float)Math.Sin(timer * 2f) * 4f, 0.4f);
                }
            } else {
                DrawBeam(0f, lightWidth, lightLength - 4 + (float)Math.Sin(timer * 2f) * 4f, 0.4f);
            }
            for (int i = 0; i < lightWidth; i += 4) {
                float num = timer + i * 0.6f;
                float num2 = 4f + (float)Math.Sin(num * 0.5f + 1.2f) * 4f;
                float offset = (float)Math.Sin((double)((num + i * 32) * 0.1f) + Math.Sin(num * 0.05f + i * 0.1f) * 0.25) * (lightWidth / 2f - num2 / 2f);
                float length = lightLength + (float)Math.Sin(num * 0.25f) * 8f;
                float a = 0.6f + (float)Math.Sin(num + 0.8f) * 0.3f;
                DrawBeam(offset, num2, length, a);
            }
        }

        private void DrawBeam(float offset, float width, float length, float a) {
            float beamRotation = rotation + (float)Math.PI / 2f;
            // if rainbow is enabled and rainbowSingleColor is disabled, call GetHue for the beam's color, otherwise use the color variable.
            Color beamColor = ((rainbow && !rainbowSingleColor) ? GetHue(Position + Calc.AngleToVector(rotation, 1f) * offset) : color) * a;

            if (width >= 1f) {
                beamTexture.Draw(Position + Calc.AngleToVector(rotation, 1f) * offset, new Vector2(0f, 0.5f), beamColor, new Vector2(1f / beamTexture.Width * length, width), beamRotation);
            }
        }

        private Color GetHue(Vector2 position) {
            // use vanilla/rainbow spinner color controller colors by default
            if (!useCustomRainbowColors)
                return Utils.GetRainbowHue(Scene, position);

            // stolen from MaddieHelpingHand's RainbowSpinnerColorController
            // https://github.com/maddie480/MaddieHelpingHand/blob/master/Entities/RainbowSpinnerColorController.cs#L311
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
            int colorIndex = (int)globalProgress;
            float progressInIndex = globalProgress - colorIndex;
            return Color.Lerp(rainbowColors[colorIndex], rainbowColors[colorIndex + 1], progressInIndex);
        }

        private bool InView(Camera camera) =>
            rectangleLeft < camera.Right + visibilityPadding && rectangleRight > camera.Left - visibilityPadding && rectangleTop < camera.Bottom + visibilityPadding && rectangleBottom > camera.Top - visibilityPadding;
    }
}
