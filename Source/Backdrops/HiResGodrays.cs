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

[CustomBackdrop("SorbetHelper/HiResGodrays")]
public class HiResGodrays : Backdrop {
    public struct Ray {
        public float X, Y;

        public float Percent;

        public float Duration;
        public float Scale;
        public Color Color;

        // godrays
        public float Width, Length;

        // image particles
        public float TexRotation;
        public float TexRotationSpeed;

        public Vector2 RenderPosition;
        public Color RenderColor;

        public void Reset(HiResGodrays backdrop) {
            Percent = 0f;
            X = -backdrop.OffscreenPadding + Calc.Random.NextFloat(320 + backdrop.OffscreenPadding * 2);
            Y = -backdrop.OffscreenPadding + Calc.Random.NextFloat(180 + backdrop.OffscreenPadding * 2);
            Duration = Calc.Random.Range(backdrop.MinDuration, backdrop.MaxDuration);
            Scale = Calc.Random.Range(backdrop.MinScale, backdrop.MaxScale);
            Color = backdrop.Colors[Calc.Random.Next(backdrop.Colors.Length)];

            // image particles
            if (backdrop.TexStartRotated)
                TexRotation = Calc.Random.NextFloat(MathF.PI * 2f);
            TexRotationSpeed = Calc.Random.Range(backdrop.TexMinRotate, backdrop.TexMaxRotate);

            // godrays
            Width = Calc.Random.Next(backdrop.MinWidth, backdrop.MaxWidth) * Scale;
            Length = Calc.Random.Next(backdrop.MinLength, backdrop.MaxLength) * Scale;
        }
    }

    private const float UpscaleAmount = Celeste.TargetWidth / Celeste.GameWidth;

    private readonly bool DoFadeInOut;
    private float visibleFade = 1f;
    private float cameraFade = 1f;

    private readonly int OffscreenPadding;
    private readonly float ScrollX, ScrollY;
    private readonly float SpeedX, SpeedY;
    private readonly float MinDuration, MaxDuration;
    private readonly float MinScale, MaxScale; // kinda redundant for normal godrays but unlike width/length also works with texture particles
    private readonly Color[] Colors;
    private readonly bool FadeNearPlayer;

    private readonly bool UsingTextureParticles;

    // godrays
    private readonly int MinWidth, MaxWidth;
    private readonly int MinLength, MaxLength;

    // image particles
    private readonly MTexture particleTexture;
    private readonly bool TexStartRotated;
    private readonly float TexMinRotate, TexMaxRotate;

    private readonly int rayCount;
    private readonly Ray[] rays;
    private VertexPositionColor[] vertices;
    private int vertexCount;

    private void Reset() {
        for (int i = 0; i < rays.Length; i++) {
            rays[i].Reset(this);
            rays[i].Percent = Calc.Random.NextFloat();
        }
    }

    public HiResGodrays(BinaryPacker.Element data) : base() {
        DoFadeInOut = data.AttrBool("fadeInOut", true);
        OffscreenPadding = data.AttrInt("offscreenPadding", 32);
        ScrollX = data.AttrFloat("scrollX", 1.1f);
        ScrollY = data.AttrFloat("scrollY", 1.1f);
        SpeedX = data.AttrFloat("speedX", 0f);
        SpeedY = data.AttrFloat("speedY", 8f);
        MinDuration = data.AttrFloat("minDuration", 4f);
        MaxDuration = data.AttrFloat("maxDuration", 12f);
        MinScale = data.AttrFloat("minScale", 1f);
        MaxScale = data.AttrFloat("maxScale", 1f);
        Colors = data.AttrList("colors", Util.HexToColorWithAlphaNonPremult, "f52b6380").ToArray();
        FadeNearPlayer = data.AttrBool("fadeNearPlayer", true);

        var texturePath = data.Attr("texturePath", "");
        UsingTextureParticles = !string.IsNullOrWhiteSpace(texturePath);

        // godrays
        MinWidth = data.AttrInt("minWidth", 8);
        MaxWidth = data.AttrInt("maxWidth", 16);
        MinLength = data.AttrInt("minLength", 20);
        MaxLength = data.AttrInt("maxLength", 40);

        // image particles
        particleTexture = OVR.Atlas.GetOrDefault(texturePath, OVR.Atlas["star"]);
        TexStartRotated = data.AttrBool("textureStartRotated", true);
        TexMinRotate = data.AttrFloat("textureMinRotate", -22.5f) * Calc.DegToRad;
        TexMaxRotate = data.AttrFloat("textureMaxRotate", 22.5f) * Calc.DegToRad;

        rayCount = data.AttrInt("rayCount", 6);
        rays = new Ray[rayCount];
        vertices = new VertexPositionColor[rayCount * 6];
        Reset();
    }

    public override void Update(Scene scene) {
        Level level = scene as Level;

        base.Update(scene);
        // janky silly but makes fading out work with flags better
        if (!Visible && DoFadeInOut && visibleFade > 0f)
            Visible = true;

        // fading
        if (DoFadeInOut)
            visibleFade = Calc.Approach(visibleFade, IsVisible(level) ? 1 : 0, Engine.DeltaTime * 2f);

        cameraFade = 1f;
        if (FadeX != null)
            cameraFade *= FadeX.Value(level.Camera.X + Util.CameraWidth / 2f);
        if (FadeY != null)
            cameraFade *= FadeY.Value(level.Camera.Y + Util.CameraHeight / 2f);

        float alpha = visibleFade * cameraFade * ExtendedVariantsCompat.ForegroundEffectOpacity;

        // resize vertex buffer for zoom out if needed,
        int visibleScreens = (int)Math.Ceiling((Util.CameraWidth + OffscreenPadding * 2f) / (320f + OffscreenPadding * 2f));
        int expectedBufferLength = rayCount * 6 * visibleScreens * visibleScreens;
        if (!UsingTextureParticles && vertices.Length != expectedBufferLength)
            vertices = new VertexPositionColor[expectedBufferLength];

        var cameraPos = level.Camera.Position.Floor();
        var player = level.Tracker.GetEntity<Player>();

        var skew1 = Calc.AngleToVector(-1.6707964f, 1f);
        var skew2 = new Vector2(0f - skew1.Y, skew1.X);
        int num = 0;
        for (int i = 0; i < rays.Length; i++) {
            ref var ray = ref rays[i];

            if (ray.Percent >= 1f)
                ray.Reset(this);

            ray.Percent += Engine.DeltaTime / ray.Duration;
            ray.X += SpeedX * Engine.DeltaTime;
            ray.Y += SpeedY * Engine.DeltaTime;
            ray.TexRotation += ray.TexRotationSpeed * Engine.DeltaTime;

            float percent = ray.Percent;

            float renderX = -OffscreenPadding + Mod(ray.X - cameraPos.X * ScrollX, 320f + OffscreenPadding * 2f);
            float renderY = -OffscreenPadding + Mod(ray.Y - cameraPos.Y * ScrollY, 180f + OffscreenPadding * 2f);
            ray.RenderPosition = new Vector2(renderX, renderY);

            var rayAlpha = Ease.CubeInOut(Calc.Clamp(((percent < 0.5f) ? percent : (1f - percent)) * 2f, 0f, 1f)) * alpha;
            ray.RenderColor = ray.Color * rayAlpha;

            if (UsingTextureParticles)
                continue;

            float rayWidth = ray.Width;
            float rayLength = ray.Length;

            // loop silly this is a zoom out momemnt,,,, ,,,,
            // this  shouldd work unless im stupid and this can rarely try n render more rays than expected and exceed the vertex buffer size
            for (int x = 0; x < 320 * visibleScreens + OffscreenPadding; x += 320 + OffscreenPadding * 2) {
                for (int y = 0; y < 180 * visibleScreens + OffscreenPadding; y += 180 + OffscreenPadding * 2) {
                    var renderPos = new Vector2(ray.RenderPosition.X + x, ray.RenderPosition.Y + y);
                    var renderColor = ray.RenderColor;

                    //  need to do this here so that the correct godray fades etc etc,
                    if (FadeNearPlayer && player != null) {
                        float playerDistance = (renderPos + cameraPos - player.Position).Length();
                        if (playerDistance < 64f) {
                            renderColor *= 0.25f + 0.75f * (playerDistance / 64f);
                        }
                    }

                    var v1 = new VertexPositionColor(new Vector3(renderPos + skew2 * rayWidth + skew1 * rayLength, 0f), renderColor);
                    var v2 = new VertexPositionColor(new Vector3(renderPos - skew2 * rayWidth, 0f), renderColor);
                    var v3 = new VertexPositionColor(new Vector3(renderPos + skew2 * rayWidth, 0f), renderColor);
                    var v4 = new VertexPositionColor(new Vector3(renderPos - skew2 * rayWidth - skew1 * rayLength, 0f), renderColor);
                    vertices[num++] = v1;
                    vertices[num++] = v2;
                    vertices[num++] = v3;
                    vertices[num++] = v2;
                    vertices[num++] = v3;
                    vertices[num++] = v4;
                }
            }
        }

        vertexCount = num;
    }

    public void DrawSelf(Scene scene, Matrix matrix) {
        if (UsingTextureParticles)
            DrawTextureParticles(scene, matrix);
        else
            DrawGodrays(matrix);
    }

    private void DrawGodrays(Matrix matrix) {
        if (vertexCount > 0) {
            GFX.DrawVertices(matrix, vertices, vertexCount);
        }
    }

    private void DrawTextureParticles(Scene scene, Matrix matrix) {
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, matrix);

        // im realising there mightt be some inconsistency between godrays/texture particles since one is setup in update and the other partly in render   fun
        var cameraWidth = Util.CameraWidth;
        var cameraHeight = Util.CameraHeight;
        var level = scene as Level;
        var player = level.Tracker.GetEntity<Player>();
        var cameraPos = level.Camera.Position.Floor();

        for (int i = 0; i < rays.Length; i++) {
            ref var ray = ref rays[i];

            for (int x = 0; x < cameraWidth + OffscreenPadding; x += 320 + OffscreenPadding * 2) {
                for (int y = 0; y < cameraHeight + OffscreenPadding; y += 180 + OffscreenPadding * 2) {
                    var renderPos = new Vector2(ray.RenderPosition.X + x, ray.RenderPosition.Y + y);
                    var renderColor = ray.RenderColor;

                    //  need to do this here so that the correct godray fades etc etc,
                    if (FadeNearPlayer && player != null) {
                        float playerDistance = (renderPos + cameraPos - player.Position).Length();
                        if (playerDistance < 64f) {
                            renderColor *= 0.25f + 0.75f * (playerDistance / 64f);
                        }
                    }

                    particleTexture.DrawCentered(renderPos, renderColor, ray.Scale * 1f / UpscaleAmount, ray.TexRotation);
                }
            }
        }

        Draw.SpriteBatch.End();

    }

    private static float Mod(float x, float m) => (x % m + m) % m;

    internal static void Load() {
        Everest.Events.LevelLoader.OnLoadingThread += OnLoadingThread;
    }

    internal static void Unload() {
        Everest.Events.LevelLoader.OnLoadingThread -= OnLoadingThread;
    }

    private static void OnLoadingThread(Level level) {
        var hiResGodrayBackdrops = level.Foreground.Backdrops.Where(backdrop => backdrop is HiResGodrays);

        foreach (var godrays in hiResGodrayBackdrops) {
            var entity = new HiResGodraysRenderer(godrays as HiResGodrays);
            level.Add(entity);
        }
    }

    public class HiResGodraysRenderer : Entity {
        private readonly HiResGodrays Backdrop;

        public HiResGodraysRenderer(HiResGodrays backdrop) : base() {
            Backdrop = backdrop;

            Tag = global::Celeste.Tags.Global | TagsExt.SubHUD;
            Depth = 2000000; // doesn't really matter but just in case it gets used with other subhud stuff :p
        }

        public override void Render() {
            if (!Backdrop.Visible)
                return;

            var level = Scene as Level;
            var matrix = Matrix.CreateScale(UpscaleAmount);

            // mirror mode
            if (SaveData.Instance.Assists.MirrorMode)
                matrix *= Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateTranslation(1920, 0f, 0f);
            if (ExtendedVariantsCompat.UpsideDown)
                matrix *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, 1080, 0f);

            // zoom out support
            if (Util.ZoomOutActive)
                //matrix *= Matrix.CreateTranslation(1920 * -0.5f, 1080 * -0.5f, 0f) * Matrix.CreateScale(Math.Min(level.Zoom, 1f)) * Matrix.CreateTranslation(1920 * 0.5f, 1080 * 0.5f, 0f);
                matrix *= Matrix.CreateScale(320f / Util.CameraWidth);

            // watchtower/etc edge padding
            if (level.ScreenPadding != 0f) {
                float paddingScale = (320f - level.ScreenPadding * 2f) / 320f;
                Vector2 paddingOffset = new(level.ScreenPadding, level.ScreenPadding * 0.5625f);
                matrix *= Matrix.CreateTranslation(1920 * -0.5f, 1080 * -0.5f, 0f) * Matrix.CreateScale(paddingScale) * Matrix.CreateTranslation(1920 * 0.5f + paddingOffset.X, 1080 * 0.5f + paddingOffset.Y, 0f);
            }

            if (!SubHudRenderer.DrawToBuffer)
                matrix *= Engine.ScreenMatrix;

            SubHudRenderer.EndRender();

            Backdrop.DrawSelf(Scene, matrix);

            SubHudRenderer.BeginRender();
        }
    }
}

