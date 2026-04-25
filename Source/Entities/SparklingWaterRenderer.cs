using System.Diagnostics.CodeAnalysis;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
public class SparklingWaterRenderer
    : DepthRenderer<SparklingWaterRenderer, SparklingWater, SparklingWaterRenderer.Options>
{
    #region Options

    public record Options(
        Color OutlineColor, Color EdgeColor, Color FillColor,
        string DetailTexture,
        float CausticScale, float CausticAlpha,
        float BubbleAlpha,
        float DisplacementSpeed)
    {
        private static readonly Color DefaultOutlineColor = Calc.HexToColorWithNonPremultipliedAlpha("87cefaf0");
        private static readonly Color DefaultEdgeColor = Calc.HexToColorWithNonPremultipliedAlpha("87cefa80");
        private static readonly Color DefaultFillColor = Calc.HexToColorWithNonPremultipliedAlpha("4480b890");
        private const string DefaultDetailTexture = "objects/SorbetHelper/sparklingWater/detail";
        private const float DefaultCausticScale = 0.8f, DefaultCausticAlpha = 0.15f;
        private const float DefaultBubbleAlpha = 0.3f;
        private const float DefaultDisplacementSpeed = 0.25f;

        public static readonly Options DefaultOptions = new Options(
            DefaultOutlineColor, DefaultEdgeColor, DefaultFillColor,
            DefaultDetailTexture,
            DefaultCausticScale, DefaultCausticAlpha,
            DefaultBubbleAlpha,
            DefaultDisplacementSpeed);

        public Options(EntityData data)
            : this(data.HexColorWithNonPremultipliedAlpha("outlineColor", DefaultOutlineColor),
                data.HexColorWithNonPremultipliedAlpha("edgeColor", DefaultEdgeColor),
                data.HexColorWithNonPremultipliedAlpha("fillColor", DefaultFillColor),
                data.String("detailTexture", DefaultDetailTexture),
                data.Float("causticScale", DefaultCausticScale), data.Float("causticAlpha", DefaultCausticAlpha),
                data.Float("bubbleAlpha", DefaultBubbleAlpha),
                data.Float("displacementSpeed", DefaultDisplacementSpeed))
        { }
    }

    #endregion

    #region Renderer

    public static readonly Color OutlineMaskColor = new Color(0f, 0.5f, 1f);
    public static readonly Color TopEdgeMaskColor = new Color(1f, 1f, 0f);
    public static readonly Color BottomEdgeMaskColor = new Color(1f, 0f, 0f);
    public static readonly Color FillMaskColor = new Color(0f, 0.5f, 0f);

    private VirtualRenderTarget buffer, displacementBuffer;

    private float timer;

    public SparklingWaterRenderer() : base()
    {
        Tag |= Tags.TransitionUpdate;
        Add(new DepthAdheringDisplacementRenderHook(Render, RenderDisplacement, true, true));
    }

    public override void Update()
    {
        base.Update();
        timer += Engine.DeltaTime;
    }

    protected override void BeforeRender()
    {
        if (VisibleGroups.Count == 0)
            return;

        using (new ResetGraphicsStateOnDispose(Engine.Instance.GraphicsDevice, 1, false))
        {
            RenderTargetHelper.CreateOrResizeGameplayBuffer(ref buffer);
            RenderTargetHelper.CreateOrResizeGameplayBuffer(ref displacementBuffer);
            Engine.Instance.GraphicsDevice.SetRenderTargets(buffer.Target, displacementBuffer.Target);
            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
            Engine.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Engine.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;

            base.BeforeRender();
        }
    }

    protected override void GroupBeforeRender(IGrouping<Options, SparklingWater> waterGroup)
    {
        Options options = waterGroup.Key;

        Camera camera = SceneAs<Level>().Camera;

        // prepare shader
        Texture2D detailTexture = GFX.Game[options.DetailTexture].Texture.Texture_Safe;
        Effect effect = SorbetHelperGFX.FxSparklingWater;
        effect.Parameters["outline_color"].SetValue(options.OutlineColor.ToVector4());
        effect.Parameters["edge_color"].SetValue(options.EdgeColor.ToVector4());
        effect.Parameters["fill_color"].SetValue(options.FillColor.ToVector4());
        effect.Parameters["texture_size"].SetValue(new Vector2(detailTexture.Width, detailTexture.Height));
        effect.Parameters["detail_config"].SetValue(new Vector4(options.CausticScale, options.CausticAlpha, options.BubbleAlpha, options.DisplacementSpeed));
        effect.Parameters["time"].SetValue(timer);
        effect.Parameters["camera_pos"].SetValue(camera.Position);

        // prepare detail texture
        Engine.Graphics.GraphicsDevice.Textures[0] = detailTexture;
        Engine.Graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

        // prepare matrix (based on GFX.DrawVertices)
        Vector2 viewport = new Vector2(Engine.Graphics.GraphicsDevice.Viewport.Width, Engine.Graphics.GraphicsDevice.Viewport.Height);
        Matrix matrix = camera.Matrix;
        matrix *= Matrix.CreateScale(1f / viewport.X * 2f, (0f - 1f / viewport.Y) * 2f, 1f);
        matrix *= Matrix.CreateTranslation(-1f, 1f, 0f);
        effect.Parameters["World"].SetValue(matrix);

        // draw water meshes
        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();

            foreach (SparklingWater water in waterGroup)
                water.DrawMesh();
        }
    }

    public override void Render()
    {
        if (VisibleGroups.Count == 0 || buffer is null || buffer.IsDisposed)
            return;

        Draw.SpriteBatch.Draw(buffer, SceneAs<Level>().Camera.Position, Color.White);
    }

    private void RenderDisplacement()
    {
        if (VisibleGroups.Count == 0 || displacementBuffer is null || displacementBuffer.IsDisposed)
            return;

        Draw.SpriteBatch.Draw(displacementBuffer, SceneAs<Level>().Camera.Position, Color.White);
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        Dispose();
    }

    public override void SceneEnd(Scene scene)
    {
        base.SceneEnd(scene);
        Dispose();
    }

    private void Dispose()
    {
        RenderTargetHelper.DisposeAndSetNull(ref buffer);
        RenderTargetHelper.DisposeAndSetNull(ref displacementBuffer);
    }

    #endregion
}
