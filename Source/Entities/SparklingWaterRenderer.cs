using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.SorbetHelper.Components;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
public class SparklingWaterRenderer : Entity {

    #region Settings

    public class Settings(Color outlineColor, Color edgeColor, Color fillColor,
        string detailTexture,
        float causticScale, float causticAlpha,
        float bubbleAlpha,
        float displacementSpeed) {

        private static readonly Color DefaultOutlineColor = Calc.HexToColorWithNonPremultipliedAlpha("9ce4f7de");
        private static readonly Color DefaultEdgeColor = Calc.HexToColorWithNonPremultipliedAlpha("8dceeb79");
        private static readonly Color DefaultFillColor = Calc.HexToColorWithNonPremultipliedAlpha("4289bd97");
        private const string DefaultDetailTexture = "objects/SorbetHelper/sparklingWater/detail";
        private const float DefaultCausticScale = 0.8f, DefaultCausticAlpha = 0.15f;
        private const float DefaultBubbleAlpha = 0.3f;
        private const float DefaultDisplacementSpeed = 0.25f;

        public static readonly Settings DefaultSettings = new Settings(DefaultOutlineColor, DefaultEdgeColor, DefaultFillColor,
            DefaultDetailTexture,
            DefaultCausticScale, DefaultCausticAlpha,
            DefaultBubbleAlpha,
            DefaultDisplacementSpeed);

        public readonly Color OutlineColor = outlineColor, EdgeColor = edgeColor, FillColor = fillColor;
        public readonly string DetailTexture = detailTexture;
        public readonly float CausticScale = causticScale, CausticAlpha = causticAlpha;
        public readonly float BubbleAlpha = bubbleAlpha;
        public readonly float DisplacementSpeed = displacementSpeed;

        public Settings(EntityData data)
            : this(data.HexColorWithNonPremultipliedAlpha("outlineColor", DefaultOutlineColor),
            data.HexColorWithNonPremultipliedAlpha("edgeColor", DefaultEdgeColor),
            data.HexColorWithNonPremultipliedAlpha("fillColor", DefaultFillColor),
            data.String("detailTexture", DefaultDetailTexture),
            data.Float("causticScale", DefaultCausticScale), data.Float("causticAlpha", DefaultCausticAlpha),
            data.Float("bubbleAlpha", DefaultBubbleAlpha),
            data.Float("displacementSpeed", DefaultDisplacementSpeed)) { }
    }

    public static Settings GetSettings(Scene scene, int depth)
        => SparklingWaterColorController.GetController(scene, depth)?.Settings ?? Settings.DefaultSettings;

    #endregion

    #region Renderer

    public static readonly Color OutlineMaskColor = new Color(0f, 0.5f, 1f);
    public static readonly Color TopEdgeMaskColor = new Color(1f, 1f, 0f);
    public static readonly Color BottomEdgeMaskColor = new Color(1f, 0f, 0f);
    public static readonly Color FillMaskColor = new Color(0f, 0.5f, 0f);

    private readonly List<SparklingWater> sparklingWater = [];

    private VirtualRenderTarget buffer, displacementBuffer;
    private readonly DepthAdheringDisplacementRenderHook displacementRenderHook;

    private float timer;

    private SparklingWaterRenderer(int depth) : base() {
        Tag = Tags.Global | Tags.TransitionUpdate;
        Depth = depth;

        Add(new BeforeRenderHook(BeforeRender));
        Add(displacementRenderHook = new DepthAdheringDisplacementRenderHook(Render, RenderDisplacement, true, true));
    }

    public void Track(SparklingWater water) => sparklingWater.Add(water);
    public void Untrack(SparklingWater water) => sparklingWater.Remove(water);

    public override void Update() {
        base.Update();
        timer += Engine.DeltaTime;
    }

    public void BeforeRender() {
        List<SparklingWater> visibleWater = sparklingWater.Where(water => water.Visible && water.VisibleOnCamera).ToList();

        // setting Visible to false when no water is visible will prevent the depth adhering displacement from still interrupting the spritebatch unnecessarily
        Visible = visibleWater.Count > 0;
        if (!Visible)
            return;

        Camera camera = SceneAs<Level>().Camera;
        Settings settings = GetSettings(Scene, Depth);

        // only distort if necessary
        displacementRenderHook.Visible = settings.DisplacementSpeed > 0f;

        // set buffers
        buffer ??= VirtualContent.CreateRenderTarget($"SorbetHelper_SparklingWaterBuffer_{Depth}", SorbetHelperGFX.GameplayBufferWidth, SorbetHelperGFX.GameplayBufferHeight);
        displacementBuffer ??= VirtualContent.CreateRenderTarget($"SorbetHelper_SparklingWaterDisplacementBuffer_{Depth}", SorbetHelperGFX.GameplayBufferWidth, SorbetHelperGFX.GameplayBufferHeight);
        SorbetHelperGFX.EnsureBufferSize(buffer);
        SorbetHelperGFX.EnsureBufferSize(displacementBuffer);
        Engine.Instance.GraphicsDevice.SetRenderTargets(new(buffer), new(displacementBuffer));
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        Engine.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        Engine.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        // prepare shader
        Texture2D detailTexture = GFX.Game[settings.DetailTexture].Texture.Texture_Safe;
        Effect effect = SorbetHelperGFX.FxSparklingWater;
        effect.Parameters["outline_color"].SetValue(settings.OutlineColor.ToVector4());
        effect.Parameters["edge_color"].SetValue(settings.EdgeColor.ToVector4());
        effect.Parameters["fill_color"].SetValue(settings.FillColor.ToVector4());
        effect.Parameters["texture_size"].SetValue(new Vector2(detailTexture.Width, detailTexture.Height));
        effect.Parameters["detail_config"].SetValue(new Vector4(settings.CausticScale, settings.CausticAlpha, settings.BubbleAlpha, settings.DisplacementSpeed));
        effect.Parameters["time"].SetValue(timer);
        effect.Parameters["camera_pos"].SetValue(camera.Position);

        // prepare detail texture
        Texture prevTexture0 = Engine.Graphics.GraphicsDevice.Textures[0];
        SamplerState prevSamplerState0 = Engine.Graphics.GraphicsDevice.SamplerStates[0];
        Engine.Graphics.GraphicsDevice.Textures[0] = detailTexture;
        Engine.Graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

        // prepare matrix (based on GFX.DrawVertices)
        Vector2 viewport = new Vector2(Engine.Graphics.GraphicsDevice.Viewport.Width, Engine.Graphics.GraphicsDevice.Viewport.Height);
        Matrix matrix = camera.Matrix;
        matrix *= Matrix.CreateScale(1f / viewport.X * 2f, (0f - 1f / viewport.Y) * 2f, 1f);
        matrix *= Matrix.CreateTranslation(-1f, 1f, 0f);
        effect.Parameters["World"].SetValue(matrix);

        // draw water meshes
        foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
            pass.Apply();

            foreach (SparklingWater water in sparklingWater) {
                if (water.Visible && water.VisibleOnCamera)
                    water.DrawMesh();
            }
        }

        Engine.Graphics.GraphicsDevice.Textures[0] = prevTexture0;
        Engine.Graphics.GraphicsDevice.SamplerStates[0] = prevSamplerState0;
    }

    public void RenderDisplacement() {
        if (displacementBuffer is null || displacementBuffer.IsDisposed)
            return;

        Draw.SpriteBatch.Draw(displacementBuffer, SceneAs<Level>().Camera.Position, Color.White);
    }

    public override void Render() {
        if (buffer is null || buffer.IsDisposed)
            return;

        Draw.SpriteBatch.Draw(buffer, SceneAs<Level>().Camera.Position, Color.White);
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        Dispose();
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);
        Dispose();
    }

    public void Dispose() {
        RenderTargetHelper.DisposeAndSetNull(ref buffer);
        RenderTargetHelper.DisposeAndSetNull(ref displacementBuffer);
    }

    public static SparklingWaterRenderer GetRenderer(Scene scene, int depth) {
        if (scene.Tracker.GetEntities<SparklingWaterRenderer>()
                         .Concat(scene.Entities.ToAdd)
                         .FirstOrDefault(r => r is SparklingWaterRenderer && r.Depth == depth)
            is not SparklingWaterRenderer renderer) {
            scene.Add(renderer = new SparklingWaterRenderer(depth));
            Logger.Info($"{nameof(SorbetHelper)}/{nameof(SparklingWaterRenderer)}", $"creating new SparklingWaterRenderer at depth {depth}.");
        }

        return renderer;
    }

    #endregion

}
