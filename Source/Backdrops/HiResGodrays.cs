using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Backdrops;
using Celeste.Mod.UI;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Backdrops;

[CustomBackdrop("SorbetHelper/HiResGodrays")]
public class HiResGodrays : Backdrop {
    private struct Ray {
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
            X = -backdrop.offscreenPadding + Calc.Random.NextFloat(320 + backdrop.offscreenPadding * 2);
            Y = -backdrop.offscreenPadding + Calc.Random.NextFloat(180 + backdrop.offscreenPadding * 2);
            Duration = Calc.Random.Range(backdrop.minDuration, backdrop.maxDuration);
            Scale = Calc.Random.Range(backdrop.minScale, backdrop.maxScale);
            Color = backdrop.colors[Calc.Random.Next(backdrop.colors.Length)];

            // image particles
            if (backdrop.texStartRotated)
                TexRotation = Calc.Random.NextFloat(MathF.PI * 2f);
            TexRotationSpeed = Calc.Random.Range(backdrop.texMinRotate, backdrop.texMaxRotate);

            // godrays
            Width = Calc.Random.Next(backdrop.minWidth, backdrop.maxWidth) * Scale;
            Length = Calc.Random.Next(backdrop.minLength, backdrop.maxLength) * Scale;
        }
    }

    private const float UpscaleAmount = 6f;

    private readonly bool doVisibleFade;
    private float visibleFade = 1f;
    private float cameraFade = 1f;

    private readonly int offscreenPadding;
    private readonly float scrollX, scrollY;
    private readonly float speedX, speedY;
    private readonly float minDuration, maxDuration;
    private readonly float minScale, maxScale; // kinda redundant for normal godrays but unlike width/length also works with texture particles
    private readonly Color[] colors;
    private readonly bool fadeNearPlayer;

    private readonly bool usingTextureParticles;

    // godrays
    private readonly int minWidth, maxWidth;
    private readonly int minLength, maxLength;

    // image particles
    private readonly MTexture particleTexture;
    private readonly bool texStartRotated;
    private readonly float texMinRotate, texMaxRotate;

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
        doVisibleFade = data.AttrBool("fadeInOut", true);
        offscreenPadding = data.AttrInt("offscreenPadding", 32);
        scrollX = data.AttrFloat("scrollX", 1.1f);
        scrollY = data.AttrFloat("scrollY", 1.1f);
        speedX = data.AttrFloat("speedX", 0f);
        speedY = data.AttrFloat("speedY", 8f);
        minDuration = data.AttrFloat("minDuration", 4f);
        maxDuration = data.AttrFloat("maxDuration", 12f);
        minScale = data.AttrFloat("minScale", 1f);
        maxScale = data.AttrFloat("maxScale", 1f);
        colors = data.AttrList("colors", Calc.HexToColorWithNonPremultipliedAlpha, "f52b6380").ToArray();
        fadeNearPlayer = data.AttrBool("fadeNearPlayer", true);

        string texturePath = data.Attr("texturePath", "");
        usingTextureParticles = !string.IsNullOrWhiteSpace(texturePath);

        // godrays
        minWidth = data.AttrInt("minWidth", 8);
        maxWidth = data.AttrInt("maxWidth", 16);
        minLength = data.AttrInt("minLength", 20);
        maxLength = data.AttrInt("maxLength", 40);

        // image particles
        particleTexture = OVR.Atlas.GetOrDefault(texturePath, OVR.Atlas["star"]);
        texStartRotated = data.AttrBool("textureStartRotated", true);
        texMinRotate = data.AttrFloat("textureMinRotate", -22.5f) * Calc.DegToRad;
        texMaxRotate = data.AttrFloat("textureMaxRotate", 22.5f) * Calc.DegToRad;

        rayCount = data.AttrInt("rayCount", 6);
        rays = new Ray[rayCount];
        vertices = new VertexPositionColor[rayCount * 6];
        Reset();
    }

    private void Added(Level level) {
        level.Add(new HiResGodraysRenderer(this));
        visibleFade = IsVisible(level) ? 1f : 0f;
    }

    public override void Update(Scene scene) {
        Level level = scene as Level;

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
            cameraFade *= FadeX.Value(level.Camera.X + Util.CameraWidth / 2f);
        if (FadeY != null)
            cameraFade *= FadeY.Value(level.Camera.Y + Util.CameraHeight / 2f);

        float alpha = visibleFade * cameraFade * ExtendedVariantsCompat.ForegroundEffectOpacity;

        // resize vertex buffer for zoom out if needed,
        int visibleScreens = (int)Math.Ceiling((Util.CameraWidth + offscreenPadding * 2f) / (320f + offscreenPadding * 2f));
        int expectedBufferLength = rayCount * 6 * visibleScreens * visibleScreens;
        if (!usingTextureParticles && vertices.Length != expectedBufferLength)
            vertices = new VertexPositionColor[expectedBufferLength];

        Vector2 cameraPos = level.Camera.Position.Floor();
        Player player = level.Tracker.GetEntity<Player>();

        Vector2 skew1 = Calc.AngleToVector(-1.6707964f, 1f);
        Vector2 skew2 = new Vector2(0f - skew1.Y, skew1.X);
        int vertexIndex = 0;
        for (int i = 0; i < rays.Length; i++) {
            ref Ray ray = ref rays[i];

            if (ray.Percent >= 1f)
                ray.Reset(this);

            ray.Percent += Engine.DeltaTime / ray.Duration;
            ray.X += speedX * Engine.DeltaTime;
            ray.Y += speedY * Engine.DeltaTime;
            ray.TexRotation += ray.TexRotationSpeed * Engine.DeltaTime;

            float percent = ray.Percent;

            float renderX = -offscreenPadding + Mod(ray.X - cameraPos.X * scrollX, 320f + offscreenPadding * 2f);
            float renderY = -offscreenPadding + Mod(ray.Y - cameraPos.Y * scrollY, 180f + offscreenPadding * 2f);
            ray.RenderPosition = new Vector2(renderX, renderY);

            float rayAlpha = Ease.CubeInOut(Calc.Clamp(((percent < 0.5f) ? percent : (1f - percent)) * 2f, 0f, 1f)) * alpha;
            ray.RenderColor = ray.Color * rayAlpha;

            if (usingTextureParticles)
                continue;

            float rayWidth = ray.Width;
            float rayLength = ray.Length;

            // loop silly this is a zoom out momemnt,,,, ,,,,
            // this  shouldd work unless im stupid and this can rarely try n render more rays than expected and exceed the vertex buffer size
            for (int x = 0; x < 320 * visibleScreens + offscreenPadding; x += 320 + offscreenPadding * 2) {
                for (int y = 0; y < 180 * visibleScreens + offscreenPadding; y += 180 + offscreenPadding * 2) {
                    Vector2 renderPos = new Vector2(ray.RenderPosition.X + x, ray.RenderPosition.Y + y);
                    Color renderColor = ray.RenderColor;

                    //  need to do this here so that the correct godray fades etc etc,
                    if (fadeNearPlayer && player != null) {
                        float playerDistance = (renderPos + cameraPos - player.Position).Length();
                        if (playerDistance < 64f)
                            renderColor *= 0.25f + 0.75f * (playerDistance / 64f);
                    }

                    VertexPositionColor v1 = new VertexPositionColor(new Vector3(renderPos + skew2 * rayWidth + skew1 * rayLength, 0f), renderColor);
                    VertexPositionColor v2 = new VertexPositionColor(new Vector3(renderPos - skew2 * rayWidth, 0f), renderColor);
                    VertexPositionColor v3 = new VertexPositionColor(new Vector3(renderPos + skew2 * rayWidth, 0f), renderColor);
                    VertexPositionColor v4 = new VertexPositionColor(new Vector3(renderPos - skew2 * rayWidth - skew1 * rayLength, 0f), renderColor);
                    vertices[vertexIndex++] = v1;
                    vertices[vertexIndex++] = v2;
                    vertices[vertexIndex++] = v3;
                    vertices[vertexIndex++] = v2;
                    vertices[vertexIndex++] = v3;
                    vertices[vertexIndex++] = v4;
                }
            }
        }

        vertexCount = vertexIndex;
    }

    public void DrawSelf(Scene scene, Matrix matrix) {
        if (usingTextureParticles)
            DrawTextureParticles(scene, matrix);
        else
            DrawGodrays(matrix);
    }

    private void DrawGodrays(Matrix matrix) {
        if (vertexCount > 0)
            GFX.DrawVertices(matrix, vertices, vertexCount);
    }

    private void DrawTextureParticles(Scene scene, Matrix matrix) {
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, matrix);

        // im realising there mightt be some inconsistency between godrays/texture particles since one is setup in update and the other partly in render   fun
        int cameraWidth = Util.CameraWidth;
        int cameraHeight = Util.CameraHeight;
        Level level = scene as Level;
        Player player = level.Tracker.GetEntity<Player>();
        Vector2 cameraPos = level.Camera.Position.Floor();

        for (int i = 0; i < rays.Length; i++) {
            ref Ray ray = ref rays[i];

            for (int x = 0; x < cameraWidth + offscreenPadding; x += 320 + offscreenPadding * 2) {
                for (int y = 0; y < cameraHeight + offscreenPadding; y += 180 + offscreenPadding * 2) {
                    Vector2 renderPos = new Vector2(ray.RenderPosition.X + x, ray.RenderPosition.Y + y);
                    Color renderColor = ray.RenderColor;

                    //  need to do this here so that the correct godray fades etc etc,
                    if (fadeNearPlayer && player != null) {
                        float playerDistance = (renderPos + cameraPos - player.Position).Length();
                        if (playerDistance < 64f)
                            renderColor *= 0.25f + 0.75f * (playerDistance / 64f);
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
        foreach (Backdrop backdrop in level.Foreground.Backdrops) {
            if (backdrop is HiResGodrays godrays)
                godrays.Added(level);
        }
    }

    private class HiResGodraysRenderer : Entity {
        private readonly HiResGodrays backdrop;

        public HiResGodraysRenderer(HiResGodrays backdrop) : base() {
            this.backdrop = backdrop;

            Tag = global::Celeste.Tags.Global | TagsExt.SubHUD;
            Depth = 2000000;
        }

        public override void Render() {
            if (!backdrop.Visible)
                return;

            Level level = SceneAs<Level>();
            Matrix matrix = Matrix.CreateScale(UpscaleAmount);

            // mirror mode
            if (SaveData.Instance.Assists.MirrorMode)
                matrix *= Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateTranslation(1920, 0f, 0f);
            if (ExtendedVariantsCompat.UpsideDown)
                matrix *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, 1080, 0f);

            // zoom out support
            if (Util.ZoomOutActive)
                matrix *= Matrix.CreateScale(320f / Util.CameraWidth);

            // watchtower/etc edge padding
            if (level.ScreenPadding != 0f) {
                float paddingScale = (320f - level.ScreenPadding * 2f) / 320f;
                Vector2 paddingOffset = new Vector2(level.ScreenPadding, level.ScreenPadding * 0.5625f);
                matrix *= Matrix.CreateTranslation(1920 * -0.5f, 1080 * -0.5f, 0f) * Matrix.CreateScale(paddingScale) * Matrix.CreateTranslation(1920 * 0.5f + paddingOffset.X, 1080 * 0.5f + paddingOffset.Y, 0f);
            }

            if (!SubHudRenderer.DrawToBuffer)
                matrix *= Engine.ScreenMatrix;

            SubHudRenderer.EndRender();

            backdrop.DrawSelf(Scene, matrix);

            SubHudRenderer.BeginRender();
        }
    }
}

