using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.Backdrops;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Backdrops;

[CustomBackdrop("SorbetHelper/HiResGodrays")]
public class HiResGodrays : HiResBackdrop {
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
            X = -backdrop.offscreenPadding + Calc.Random.NextFloat(320f + backdrop.offscreenPadding * 2f);
            Y = -backdrop.offscreenPadding + Calc.Random.NextFloat(180f + backdrop.offscreenPadding * 2f);
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

    private readonly bool useTextureParticles;

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

    public override bool UseHiResSpritebatch => useTextureParticles;

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
        useTextureParticles = !string.IsNullOrWhiteSpace(texturePath);

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
        if (!useTextureParticles)
            vertices = new VertexPositionColor[rayCount * 6];

        Reset();
    }

    private void Reset() {
        for (int i = 0; i < rays.Length; i++) {
            rays[i].Reset(this);
            rays[i].Percent = Calc.Random.NextFloat();
        }
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

        float alpha = visibleFade * cameraFade * ExtendedVariantsCompat.ForegroundEffectOpacity;

        // resize vertex buffer for zoom out if needed,
        int visibleScreens = (int)Math.Ceiling((level.Camera.Width + offscreenPadding * 2f) / (320f + offscreenPadding * 2f));
        int expectedBufferLength = rayCount * 6 * visibleScreens * visibleScreens;
        if (!useTextureParticles && vertices.Length != expectedBufferLength)
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

            ray.RenderPosition.X = -offscreenPadding + Mod(ray.X - cameraPos.X * scrollX, 320f + offscreenPadding * 2f);
            ray.RenderPosition.Y = -offscreenPadding + Mod(ray.Y - cameraPos.Y * scrollY, 180f + offscreenPadding * 2f);

            float rayAlpha = Ease.CubeInOut(Calc.Clamp(((percent < 0.5f) ? percent : (1f - percent)) * 2f, 0f, 1f)) * alpha;
            ray.RenderColor = ray.Color * rayAlpha;

            if (useTextureParticles)
                continue;

            float rayWidth = ray.Width;
            float rayLength = ray.Length;

            // loop silly this is a zoom out momemnt,,,, ,,,,
            // this  shouldd work unless im stupid and this can rarely try n render more rays than expected and exceed the vertex buffer size
            for (int x = 0; x < 320 * visibleScreens + offscreenPadding; x += 320 + offscreenPadding * 2)
            for (int y = 0; y < 180 * visibleScreens + offscreenPadding; y += 180 + offscreenPadding * 2) {
                Vector2 renderPos = new Vector2(ray.RenderPosition.X + x, ray.RenderPosition.Y + y);
                Color renderColor = ray.RenderColor;

                //  need to do this here so that the correct godray fades etc etc,
                if (fadeNearPlayer && player is not null) {
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

        vertexCount = vertexIndex;
    }

    public override void RenderHiRes(Scene scene, Matrix upscaleMatrix) {
        if (useTextureParticles)
            DrawTextureParticles(scene);
        else
            DrawGodrays(upscaleMatrix);
    }

    private void DrawGodrays(Matrix matrix) {
        if (vertexCount > 0)
            GFX.DrawVertices(matrix, vertices, vertexCount);
    }

    private void DrawTextureParticles(Scene scene) {
        // im realising there mightt be some inconsistency between godrays/texture particles since one is setup in update and the other partly in render   fun
        Level level = (scene as Level)!;
        Vector2 cameraPos = level.Camera.Position.Floor();
        int cameraWidth = level.Camera.Width;
        int cameraHeight = level.Camera.Height;
        Player player = level.Tracker.GetEntity<Player>();

        for (int i = 0; i < rays.Length; i++) {
            ref Ray ray = ref rays[i];

            for (int x = 0; x < cameraWidth + offscreenPadding; x += 320 + offscreenPadding * 2)
            for (int y = 0; y < cameraHeight + offscreenPadding; y += 180 + offscreenPadding * 2) {
                Vector2 renderPos = new Vector2(ray.RenderPosition.X + x, ray.RenderPosition.Y + y);
                Color renderColor = ray.RenderColor;

                if (fadeNearPlayer && player is not null) {
                    float playerDistance = (renderPos + cameraPos - player.Position).Length();
                    if (playerDistance < 64f)
                        renderColor *= 0.25f + 0.75f * (playerDistance / 64f);
                }

                particleTexture.DrawCentered(renderPos, renderColor, ray.Scale * 1f / UpscaleAmount, ray.TexRotation);
            }
        }
    }

    private static float Mod(float x, float m) => (x % m + m) % m;
}
