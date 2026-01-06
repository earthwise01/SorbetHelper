using Celeste.Mod.Backdrops;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Backdrops;

[CustomBackdrop("SorbetHelper/ParallaxHiResSnow")]
public class ParallaxHiResSnow : HiResBackdrop {
    private struct Particle {
        public float Alpha;
        public float Scale;
        public float Speed;
        public Vector2 Scroll;
        public float Sin;
        public float Rotation;
        public Vector2 Position;

        public void Reset(ParallaxHiResSnow backdrop) {
            float scalePercent = Calc.Random.NextFloat();
            scalePercent *= scalePercent * scalePercent * scalePercent;

            Scale = Calc.Map(scalePercent, 0f, 1f, backdrop.minScale, backdrop.maxScale);
            Speed = Scale * Calc.Random.Range(backdrop.minSpeed, backdrop.maxSpeed);
            Scroll.X = Calc.Map(scalePercent, 0f, 1f, backdrop.minScroll.X, backdrop.maxScroll.X);
            Scroll.Y = Calc.Map(scalePercent, 0f, 1f, backdrop.minScroll.Y, backdrop.maxScroll.Y);

            Position.X = (-OffscreenPadding + Calc.Random.NextFloat(320f + OffscreenPadding * 2f)) * UpscaleAmount;
            Position.Y = (-OffscreenPadding + Calc.Random.NextFloat(180f + OffscreenPadding * 2f)) * UpscaleAmount;

            Sin = Calc.Random.NextFloat(MathF.PI * 2f);
            Rotation = backdrop.randomTextureRotation ? Calc.Random.NextFloat(MathF.PI * 2f) : 0f;
            Alpha = backdrop.fadeTowardsForeground ? MathHelper.Lerp(1f, 0f, scalePercent * 0.8f) : 1f;
        }
    }

    private const float UpscaleAmount = 6f;
    private const float OffscreenPadding = 128f / UpscaleAmount;

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

    private void Reset() {
        for (int i = 0; i < particles.Length; i++)
            particles[i].Reset(this);
    }

    public override void Added(Level level) {
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

    public override void RenderHiRes(Scene scene, Matrix upscaleMatrix) {
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
        Vector2 zoomCenterOffset = SorbetHelperGFX.GetZoomOutCameraCenterOffset(camera);
        Vector2 cameraPos = camera.Position.Floor() + zoomCenterOffset;

        for (int i = 0; i < particles.Length; i++) {
            ref Particle particle = ref particles[i];

            // need to divide by UpscaleAmount here since the snow particles are positioned at the screen resolution (1920x1080) as a leftover from vanilla HiResSnow,
            // while the matrix used for rendering HiResBackdrops expects positions in camera space (320x180)
            Vector2 renderPosition = new Vector2() {
                X = -OffscreenPadding + Mod(particle.Position.X / UpscaleAmount - cameraPos.X * particle.Scroll.X, 320f + OffscreenPadding * 2f),
                Y = -OffscreenPadding + Mod(particle.Position.Y / UpscaleAmount - cameraPos.Y * particle.Scroll.Y, 180f + OffscreenPadding * 2f)
            };
            Vector2 renderScale = stretchScale * particle.Scale / UpscaleAmount;

            // i dont remember why i did so much stuff again here and i dont feel like testing rn so   yay
            if (SorbetHelperGFX.ZoomOutActive) {
                renderPosition += zoomCenterOffset;
                renderPosition.X = -OffscreenPadding + Mod(OffscreenPadding + renderPosition.X, 320f + OffscreenPadding * 2f);
                renderPosition.Y = -OffscreenPadding + Mod(OffscreenPadding + renderPosition.Y, 180f + OffscreenPadding * 2f);
            }

            Color particleColor = color;
            if (particle.Alpha < 1f)
                particleColor *= particle.Alpha;

            // additive blending!! i love premultiplied alpha
            if (additiveMultiplier < 1f)
                particleColor = new Color(particleColor.R, particleColor.G, particleColor.B, (int)(particleColor.A * additiveMultiplier));

            if (!SorbetHelperGFX.ZoomOutActive) {
                particleTexture.DrawCentered(renderPosition, particleColor, renderScale, shouldStretch ? stretchRotate : particle.Rotation);
            } else {
                for (float x = 0f; x < camera.Width + OffscreenPadding; x += 320f + OffscreenPadding * 2f)
                for (float y = 0f; y < camera.Height + OffscreenPadding; y += 180f + OffscreenPadding * 2f)
                    particleTexture.DrawCentered(renderPosition + new Vector2(x, y), particleColor, renderScale, shouldStretch ? stretchRotate : particles[i].Rotation);
            }
        }
    }

    private static float Mod(float x, float m) => (x % m + m) % m;
}
