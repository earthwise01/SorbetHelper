using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Backdrops;
using Celeste.Mod.UI;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Backdrops;

[CustomBackdrop("SorbetHelper/ParallaxHiResSnow")]
public class ParallaxHiResSnow : Backdrop {
    private struct Particle {
        public float Alpha;
        public float Scale;
        public Vector2 Scroll;
        public float Speed;
        public float Sin;
        public float Rotation;
        public Vector2 Position;

        public void Reset(ParallaxHiResSnow backdrop) {
            float scalePercent = Calc.Random.NextFloat();
            scalePercent *= scalePercent * scalePercent * scalePercent;

            Scale = Calc.Map(scalePercent, 0f, 1f, backdrop.minScale, backdrop.maxScale);
            Speed = Scale * Calc.Random.Range(backdrop.minSpeed, backdrop.maxSpeed);
            Scroll = new Vector2(Calc.Map(scalePercent, 0f, 1f, backdrop.minScroll.X, backdrop.maxScroll.X), Calc.Map(scalePercent, 0f, 1f, backdrop.minScroll.Y, backdrop.maxScroll.Y));

            Position.X = -OffscreenPaddingSize + Calc.Random.NextFloat(1920 + OffscreenPaddingSize * 2);
            Position.Y = -OffscreenPaddingSize + Calc.Random.NextFloat(1080 + OffscreenPaddingSize * 2);

            Sin = Calc.Random.NextFloat(MathF.PI * 2f);
            Rotation = backdrop.randomTextureRotation ? Calc.Random.NextFloat(MathF.PI * 2f) : 0f;
            Alpha = backdrop.fadeTowardsForeground ? MathHelper.Lerp(1f, 0f, scalePercent * 0.8f) : 1f;
        }
    }

    private const int OffscreenPaddingSize = 128;
    private const float UpscaleAmount = 6f;

    private readonly bool doVisibleFade;
    private float visibleFade = 1f;
    private float cameraFade = 1f;

    public Vector2 Direction;
    private readonly float additiveBlend;

    private readonly float minScale, maxScale;
    private readonly float minSpeed, maxSpeed;
    private readonly Vector2 minScroll, maxScroll;
    private readonly float sineAmplitude, sineFrequency;
    private readonly bool sineHorizontal;
    private readonly bool randomTextureRotation;
    private readonly bool speedStretching;
    private readonly bool fadeTowardsForeground;

    private readonly Particle[] particles;
    private readonly MTexture particleTexture;

    public void Reset() {
        for (int i = 0; i < particles.Length; i++)
            particles[i].Reset(this);
    }

    public ParallaxHiResSnow(BinaryPacker.Element data) : base() {
        doVisibleFade = data.AttrBool("fadeInOut", true);
        Direction.X = data.AttrFloat("directionX", -1f);
        Direction.Y = data.AttrFloat("directionY", 0f);
        minScale = data.AttrFloat("minScale", 0.05f);
        maxScale = data.AttrFloat("maxScale", 0.8f);
        minSpeed = data.AttrFloat("minSpeed", 2000f);
        maxSpeed = data.AttrFloat("maxSpeed", 4000f);
        minScroll.X = data.AttrFloat("minScrollX", 1f);
        minScroll.Y = data.AttrFloat("minScrollY", 1f);
        maxScroll.X = data.AttrFloat("maxScrollX", 1.25f);
        maxScroll.Y = data.AttrFloat("maxScrollY", 1.25f);
        sineAmplitude = data.AttrFloat("sineAmplitude", 100f);
        sineFrequency = data.AttrFloat("sineFrequency", 10f) / 10f;
        sineHorizontal = data.AttrBool("sineHorizontal", false);

        string texturePath = data.Attr("texturePath", "snow");
        particleTexture = OVR.Atlas.GetOrDefault(texturePath, OVR.Atlas["snow"]);
        additiveBlend = data.AttrFloat("additive", 0f);
        randomTextureRotation = data.AttrBool("randomRotation", true);
        speedStretching = data.AttrBool("speedStretching", false);
        fadeTowardsForeground = data.AttrBool("fadeTowardsForeground", true);

        int particleCount = data.AttrInt("particleCount", 50);
        particles = new Particle[particleCount];
        Reset();
    }

    private void Added(Level level) {
        level.Add(new ParallaxHiResSnowRenderer(this));
        visibleFade = IsVisible(level) ? 1f : 0f;
    }

    public override void Update(Scene scene) {
        Level level = (scene as Level)!;

        base.Update(scene);

        // fading
        if (doVisibleFade) {
            visibleFade = Calc.Approach(visibleFade, IsVisible(level) ? 1f : 0f, Engine.DeltaTime * 2f);

            // force visible while fading out
            if (!Visible && visibleFade > 0f)
                Visible = true;
        }

        cameraFade = 1f;
        if (FadeX != null)
            cameraFade *= FadeX.Value(level.Camera.X + level.Camera.Width / 2f);
        if (FadeY != null)
            cameraFade *= FadeY.Value(level.Camera.Y + level.Camera.Height / 2f);

        for (int i = 0; i < particles.Length; i++) {
            ref Particle particle = ref particles[i];

            particle.Position += Direction * particle.Speed * Engine.DeltaTime;
            if (!sineHorizontal)
                particle.Position.Y += (float)Math.Sin(particle.Sin) * sineAmplitude * Engine.DeltaTime;
            else
                particle.Position.X += (float)Math.Sin(particle.Sin) * sineAmplitude * Engine.DeltaTime;
            particle.Sin += Engine.DeltaTime * sineFrequency;
        }
    }

    public void DrawSelf(Scene scene) {
        Color color = Color * visibleFade * cameraFade * ExtendedVariantsCompat.ForegroundEffectOpacity;
        float additiveMultiplier = 1f - additiveBlend;

        float stretchSpeed = Calc.Clamp(Direction.Length(), 0f, 20f);
        float stretchRotate = 0f;
        Vector2 stretchScale = Vector2.One;
        bool shouldStretch = stretchSpeed > 1f && speedStretching;

        if (shouldStretch) {
            stretchRotate = Direction.Angle();
            stretchScale = new Vector2(stretchSpeed, 0.2f + (1f - stretchSpeed / 20f) * 0.8f);
        }

        // zoom (out) support is kinda based on https://github.com/Ikersfletch/ExCameraDynamics/blob/main/Code/Backdrops/ZoomParticleParallax.cs
        // could've maybe gone for a depth based approach where the "distance" of the particles determines how affected they are by the "zoom" but eh idk this works

        // bwehh
        Camera camera = (scene as Level)!.Camera;
        Vector2 zoomCenterOffset = SorbetHelperGFX.GetZoomOutCameraCenterOffset(camera) * UpscaleAmount;
        Vector2 cameraPosLarge = camera.Position.Floor() * UpscaleAmount + zoomCenterOffset;

        for (int i = 0; i < particles.Length; i++) {
            ref Particle particle = ref particles[i];

            Vector2 renderPosition = new Vector2() {
                X = -OffscreenPaddingSize + Mod(particle.Position.X - cameraPosLarge.X * particle.Scroll.X, 1920 + OffscreenPaddingSize * 2),
                Y = -OffscreenPaddingSize + Mod(particle.Position.Y - cameraPosLarge.Y * particle.Scroll.Y, 1080 + OffscreenPaddingSize * 2)
            };

            // i dont remember why i did so much stuff again here and i dont feel like testing rn so   yay
            if (SorbetHelperGFX.ZoomOutActive) {
                renderPosition += zoomCenterOffset;
                renderPosition.X = -OffscreenPaddingSize + Mod(OffscreenPaddingSize + renderPosition.X, 1920 + OffscreenPaddingSize * 2);
                renderPosition.Y = -OffscreenPaddingSize + Mod(OffscreenPaddingSize + renderPosition.Y, 1080 + OffscreenPaddingSize * 2);
            }

            Color particleColor = color;
            if (particle.Alpha < 1f)
                particleColor *= particle.Alpha;

            // additive blending!! i love premultiplied alpha
            if (additiveMultiplier < 1f)
                particleColor = new Color(particleColor.R, particleColor.G, particleColor.B, (int)(particleColor.A * additiveMultiplier));

            if (!SorbetHelperGFX.ZoomOutActive) {
                particleTexture.DrawCentered(renderPosition, particleColor, stretchScale * particle.Scale, shouldStretch ? stretchRotate : particle.Rotation);
            } else {
                for (int x = 0; x < camera.Width * UpscaleAmount + OffscreenPaddingSize; x += 1920 + OffscreenPaddingSize * 2)
                for (int y = 0; y < camera.Height * UpscaleAmount + OffscreenPaddingSize; y += 1080 + OffscreenPaddingSize * 2)
                    particleTexture.DrawCentered(renderPosition + new Vector2(x, y), particleColor, stretchScale * particles[i].Scale, shouldStretch ? stretchRotate : particles[i].Rotation);
            }
        }
    }

    private static float Mod(float x, float m) => (x % m + m) % m;

    internal static void Load() {
        Everest.Events.LevelLoader.OnLoadingThread += OnLoadingThread;
    }

    internal static void Unload() {
        Everest.Events.LevelLoader.OnLoadingThread -= OnLoadingThread;
    }

    private static void OnLoadingThread(Level level) {
        foreach (Backdrop backdrop in level.Foreground.Backdrops) {
            if (backdrop is ParallaxHiResSnow hiResSnow)
                hiResSnow.Added(level);
        }
    }

    private class ParallaxHiResSnowRenderer : Entity {
        private readonly ParallaxHiResSnow backdrop;

        public ParallaxHiResSnowRenderer(ParallaxHiResSnow backdrop) : base() {
            this.backdrop = backdrop;

            Tag = global::Celeste.Tags.Global | TagsExt.SubHUD;
            Depth = 2000000;
        }

        public override void Render() {
            if (!backdrop.Visible)
                return;

            Level level = SceneAs<Level>();
            Matrix matrix = Matrix.Identity;

            // mirror mode
            if (SaveData.Instance.Assists.MirrorMode)
                matrix *= Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateTranslation(1920, 0f, 0f);
            if (ExtendedVariantsCompat.UpsideDown)
                matrix *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, 1080, 0f);

            // zoom out support
            if (SorbetHelperGFX.ZoomOutActive)
                matrix *= Matrix.CreateScale(level.Zoom);

            // watchtower/etc edge padding
            if (level.ScreenPadding != 0f) {
                float paddingScale = (320f - level.ScreenPadding * 2f) / 320f;
                Vector2 paddingOffset = new Vector2(level.ScreenPadding, level.ScreenPadding * 0.5625f);
                matrix *= Matrix.CreateTranslation(1920 * -0.5f, 1080 * -0.5f, 0f) * Matrix.CreateScale(paddingScale) * Matrix.CreateTranslation(1920 * 0.5f + paddingOffset.X, 1080 * 0.5f + paddingOffset.Y, 0f);
            }

            SubHudRenderer.EndRender();
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, matrix * (SubHudRenderer.DrawToBuffer ? Matrix.Identity : Engine.ScreenMatrix));

            backdrop.DrawSelf(Scene);

            Draw.SpriteBatch.End();
            SubHudRenderer.BeginRender();
        }
    }
}

