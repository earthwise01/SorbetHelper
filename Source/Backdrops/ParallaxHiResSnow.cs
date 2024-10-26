using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Backdrops;
using Celeste.Mod.Entities;
using Celeste.Mod.UI;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Backdrops;

[CustomBackdrop("SorbetHelper/ParallaxHiResSnow")]
public class ParallaxHiResSnow : Backdrop {
    public struct Particle {
        public float Alpha;
        public float Scale;
        public Vector2 Scroll;
        public float Speed;
        public float Sin;
        public float Rotation;
        public Vector2 Position;
        public Vector2 RenderPosition;

        public void Reset(Vector2 direction, ParallaxHiResSnow backdrop) {
            float num = Calc.Random.NextFloat();
            num *= num * num * num;

            Scale = Calc.Map(num, 0f, 1f, backdrop.MinScale, backdrop.MaxScale);
            Speed = Scale * Calc.Random.Range(backdrop.MinSpeed, backdrop.MaxSpeed);
            Scroll = new(Calc.Map(num, 0f, 1f, backdrop.MinScroll.X, backdrop.MaxScroll.X), Calc.Map(num, 0f, 1f, backdrop.MinScroll.Y, backdrop.MaxScroll.Y));

            if (direction.X < 0f) {
                Position = new Vector2(Engine.Width + OffscreenPaddingSize, Calc.Random.NextFloat(Engine.Height));
            } else if (direction.X > 0f) {
                Position = new Vector2(-OffscreenPaddingSize, Calc.Random.NextFloat(Engine.Height));
            } else if (direction.Y > 0f) {
                Position = new Vector2(Calc.Random.NextFloat(Engine.Width), -OffscreenPaddingSize);
            } else if (direction.Y < 0f) {
                Position = new Vector2(Calc.Random.NextFloat(Engine.Width), Engine.Height + OffscreenPaddingSize);
            }

            Sin = Calc.Random.NextFloat(MathF.PI * 2f);
            Rotation = backdrop.RandomTextureRotation ? Calc.Random.NextFloat(MathF.PI * 2f) : 0f;
            Alpha = backdrop.FadeTowardsForeground ? MathHelper.Lerp(1f, 0f, num * 0.8f) : 1f;
        }
    }

    private const int OffscreenPaddingSize = 128;
    private const float UpscaleAmount = Celeste.TargetWidth / Celeste.GameWidth;

    private readonly bool DoFadeInOut;
    private float visibleFade = 1f;
    private float cameraFade = 1f;

    public Vector2 Direction;
    private readonly float AdditiveBlend;

    private readonly float MinScale, MaxScale;
    private readonly float MinSpeed, MaxSpeed;
    private readonly Vector2 MinScroll, MaxScroll;
    private readonly float SineAmplitude, SineFrequency;
    private readonly bool RandomTextureRotation;
    private readonly bool FadeTowardsForeground;

    private readonly Particle[] particles;
    private readonly MTexture particleTexture;

    public void Reset() {
        for (int i = 0; i < particles.Length; i++) {
            particles[i].Reset(Direction, this);
            particles[i].Position.X = -OffscreenPaddingSize + Calc.Random.NextFloat(Engine.Width + OffscreenPaddingSize * 2);
            particles[i].Position.Y = -OffscreenPaddingSize + Calc.Random.NextFloat(Engine.Height + OffscreenPaddingSize * 2);
        }
    }

    public ParallaxHiResSnow(BinaryPacker.Element data) : base() {
        DoFadeInOut = data.AttrBool("fadeInOut", true);
        Direction.X = data.AttrFloat("directionX", -1f);
        Direction.Y = data.AttrFloat("directionY", 0f);
        MinScale = data.AttrFloat("minScale", 0.05f);
        MaxScale = data.AttrFloat("maxScale", 0.8f);
        MinSpeed = data.AttrFloat("minSpeed", 2000f);
        MaxSpeed = data.AttrFloat("maxSpeed", 4000f);
        MinScroll.X = data.AttrFloat("minScrollX", 1f);
        MinScroll.Y = data.AttrFloat("minScrollY", 1f);
        MaxScroll.X = data.AttrFloat("maxScrollX", 1.25f);
        MaxScroll.Y = data.AttrFloat("maxScrollY", 1.25f);
        SineAmplitude = data.AttrFloat("sineAmplitude", 100f);
        SineFrequency = data.AttrFloat("sineFrequency", 10f) / 10f;

        var texturePath = data.Attr("texturePath", "snow");
        particleTexture = OVR.Atlas.GetOrDefault(texturePath, OVR.Atlas["snow"]);
        AdditiveBlend = data.AttrFloat("additive", 0f);
        RandomTextureRotation = data.AttrBool("randomRotation", true);
        FadeTowardsForeground = data.AttrBool("fadeTowardsForeground", true);

        var particleCount = data.AttrInt("particleCount", 50);
        particles = new Particle[particleCount];
        Reset();
    }

    public override void Update(Scene scene) {
        Level level = scene as Level;

        base.Update(scene);
        // janky silly but makes fading out work with flags better
        if (!Visible && DoFadeInOut && visibleFade > 0f)
            // if ((ExcludeFrom is null || !ExcludeFrom.Contains(level.Session.Level)) && (OnlyIn is null || OnlyIn.Contains(level.Session.Level)))
            Visible = true;

        // fading
        if (DoFadeInOut)
            visibleFade = Calc.Approach(visibleFade, IsVisible(level) ? 1 : 0, Engine.DeltaTime * 2f);

        cameraFade = 1f;
        if (FadeX != null)
            cameraFade *= FadeX.Value(level.Camera.X + Celeste.GameWidth / 2f);
        if (FadeY != null)
            cameraFade *= FadeY.Value(level.Camera.Y + Celeste.GameHeight / 2f);

        // bwehh
        var cameraPosLarge = level.Camera.Position * UpscaleAmount;

        for (int i = 0; i < particles.Length; i++) {
            ref var particle = ref particles[i];

            particle.Position += Direction * particle.Speed * Engine.DeltaTime;
            particle.Position.Y += (float)Math.Sin(particle.Sin) * SineAmplitude * Engine.DeltaTime;
            particle.Sin += Engine.DeltaTime * SineFrequency;

            // if (particle.RenderPosition.X < -EdgePaddingAmount || particle.RenderPosition.X > (Engine.Width + EdgePaddingAmount) || particle.RenderPosition.Y < -EdgePaddingAmount || particle.RenderPosition.Y > (Engine.Height + EdgePaddingAmount)) {
            //     particle.Reset(Direction, this);
            // }

            particle.RenderPosition.X = -OffscreenPaddingSize + Mod(particle.Position.X - cameraPosLarge.X * particle.Scroll.X, Engine.Width + OffscreenPaddingSize * 2);
            particle.RenderPosition.Y = -OffscreenPaddingSize + Mod(particle.Position.Y - cameraPosLarge.Y * particle.Scroll.Y, Engine.Height + OffscreenPaddingSize * 2);
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
        var hiResSnowBackdrops = level.Foreground.Backdrops.Where(backdrop => backdrop is ParallaxHiResSnow).Cast<ParallaxHiResSnow>();

        foreach (var hiResSnow in hiResSnowBackdrops) {
            var entity = new ParallaxHiResSnowRenderer(hiResSnow);
            level.Add(entity);
        }
    }

    public class ParallaxHiResSnowRenderer : Entity {
        private readonly ParallaxHiResSnow Backdrop;

        public ParallaxHiResSnowRenderer(ParallaxHiResSnow backdrop) : base() {
            Backdrop = backdrop;

            Tag = global::Celeste.Tags.Global | TagsExt.SubHUD;
            Depth = 2000000; // doesn't really matter but just in case it gets used with other subhud stuff :p
        }

        public override void Render() {
            if (!Backdrop.Visible)
                return;

            // float num = Calc.Clamp(Backdrop.Direction.Length(), 0f, 20f);
            // float num2 = 0f;
            // var vector = Vector2.One;
            // bool flag = num > 1f;
            // if (flag) {
            //     num2 = Direction.Angle();
            //     vector = new Vector2(num, 0.2f + (1f - num / 20f) * 0.8f);
            // }

            var particles = Backdrop.particles;
            var texture = Backdrop.particleTexture;
            var color = Backdrop.Color * Backdrop.visibleFade * Backdrop.cameraFade * ExtendedVariantsCompat.ForegroundEffectOpacity;
            var additiveMultiplier = 1f - Backdrop.AdditiveBlend;

            var level = Scene as Level;
            var matrix = Matrix.Identity;

            // mirror mode
            if (SaveData.Instance.Assists.MirrorMode)
                matrix *= Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateTranslation(Engine.Width, 0f, 0f);
            if (ExtendedVariantsCompat.UpsideDown)
                matrix *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, Engine.Height, 0f);

            // watchtower/etc edge padding
            if (level.ScreenPadding != 0f) {
                float paddingScale = (320f - level.ScreenPadding * 2f) / 320f;
                Vector2 paddingOffset = new(level.ScreenPadding, level.ScreenPadding * 0.5625f);
                matrix = Matrix.CreateTranslation(Engine.Width * -0.5f, Engine.Height * -0.5f, 0f) * Matrix.CreateScale(paddingScale) * Matrix.CreateTranslation(Engine.Width * 0.5f + paddingOffset.X, Engine.Height * 0.5f + paddingOffset.Y, 0f);
            }

            SubHudRenderer.EndRender();
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, matrix * (SubHudRenderer.DrawToBuffer ? Matrix.Identity : Engine.ScreenMatrix));

            for (int i = 0; i < particles.Length; i++) {
                var particleColor = color;
                if (particles[i].Alpha < 1f)
                    particleColor *= particles[i].Alpha;

                // additive blending!! i love premultiplied alpha
                if (additiveMultiplier < 1f)
                    particleColor = new(particleColor.R, particleColor.G, particleColor.B, (int)(particleColor.A * additiveMultiplier));

                texture.DrawCentered(particles[i].RenderPosition, particleColor, particles[i].Scale, particles[i].Rotation);
            }

            SubHudRenderer.EndRender();
            SubHudRenderer.BeginRender();
        }
    }
}

