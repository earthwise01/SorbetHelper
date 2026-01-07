using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[Tracked]
public class SparklingWaterRenderer : Entity {

    #region Settings

    public class Settings {
        [CustomEntity("SorbetHelper/SparklingWaterColorController")]
        [Tracked]
        private class SparklingWaterColorController(EntityData data, Vector2 offset) : Entity(data.Position + offset) {
            public readonly Settings Settings = new Settings(data);
        }

        private static readonly Color outlineDefault = Calc.HexToColorWithNonPremultipliedAlpha("9ce4f7de");
        private static readonly Color edgeDefault = Calc.HexToColorWithNonPremultipliedAlpha("8dceeb79");
        private static readonly Color fillDefault = Calc.HexToColorWithNonPremultipliedAlpha("4289bd97");

        public readonly Color OutlineColor = outlineDefault;
        public readonly Color EdgeColor = edgeDefault;
        public readonly Color FillColor = fillDefault;
        public readonly string DetailTexture = "objects/SorbetHelper/sparklingWater/detail";
        public readonly float CausticScale = 0.8f;
        public readonly float CausticAlpha = 0.15f;
        public readonly float BubbleAlpha = 0.3f;
        public readonly float DisplacementSpeed = 0.25f;

        private static readonly Settings Default = new Settings();

        private Settings() { }
        private Settings(EntityData data) {
            OutlineColor = data.HexColorWithNonPremultipliedAlpha("outlineColor", OutlineColor);
            EdgeColor = data.HexColorWithNonPremultipliedAlpha("edgeColor", EdgeColor);
            FillColor = data.HexColorWithNonPremultipliedAlpha("fillColor", FillColor);
            DetailTexture = data.String("detailTexture", DetailTexture);
            CausticScale = data.Float("causticScale", CausticScale);
            CausticAlpha = data.Float("causticAlpha", CausticAlpha);
            BubbleAlpha = data.Float("bubbleAlpha", BubbleAlpha);
            DisplacementSpeed = data.Float("displacementSpeed", DisplacementSpeed);
        }

        public static Settings GetSettings(Scene scene) {
            SparklingWaterColorController controller = scene.Tracker.GetEntity<SparklingWaterColorController>();
            if (controller is null)
                return Default;

            return controller.Settings;
        }
    }

    #endregion

    #region Renderer

    public static readonly Color BorderColor = new Color(0f, 0.5f, 1f);
    public static readonly Color TopEdgeColor = new Color(1f, 1f, 0f);
    public static readonly Color BottomEdgeColor = new Color(1f, 0f, 0f);
    public static readonly Color FillColor = new Color(0f, 0.5f, 0f);

    private readonly List<SparklingWater> sparklingWater = [];

    private VirtualRenderTarget buffer, displacementBuffer;

    private float timer;

    public SparklingWaterRenderer(int depth) : base() {
        Tag = Tags.Global | Tags.TransitionUpdate;
        Depth = depth;

        Add(new BeforeRenderHook(BeforeRender));
    }

    public void Track(SparklingWater water) => sparklingWater.Add(water);
    public void Untrack(SparklingWater water) => sparklingWater.Remove(water);

    public override void Update() {
        base.Update();
        timer += Engine.DeltaTime;
    }

    public void BeforeRender() {
        if (sparklingWater.Count == 0)
            return;

        Camera camera = SceneAs<Level>().Camera;

        // set buffers
        buffer ??= VirtualContent.CreateRenderTarget($"SorbetHelper_SparklingWaterBuffer_{Depth}", 320, 180); // no zoom out!
        displacementBuffer ??= VirtualContent.CreateRenderTarget($"SorbetHelper_SparklingWaterDisplacementBuffer_{Depth}", 320, 180);
        Engine.Instance.GraphicsDevice.SetRenderTargets(new(buffer), new(displacementBuffer));
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        Engine.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        Engine.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        // prepare shader
        Effect effect = SorbetHelperGFX.FxSparklingWater;
        Settings settings = Settings.GetSettings(Scene);
        effect.Parameters["outline_color"].SetValue(settings.OutlineColor.ToVector4());
        effect.Parameters["edge_color"].SetValue(settings.EdgeColor.ToVector4());
        effect.Parameters["fill_color"].SetValue(settings.FillColor.ToVector4());
        effect.Parameters["detail_config"].SetValue(new Vector4(settings.CausticScale, settings.CausticAlpha, settings.BubbleAlpha, settings.DisplacementSpeed));
        effect.Parameters["time"].SetValue(timer);
        effect.Parameters["camera_pos"].SetValue(camera.Position);

        // prepare detail texture
        Texture prevTexture0 = Engine.Graphics.GraphicsDevice.Textures[0];
        SamplerState prevSamplerState0 = Engine.Graphics.GraphicsDevice.SamplerStates[0];
        Engine.Graphics.GraphicsDevice.Textures[0] = GFX.Game[settings.DetailTexture].Texture.Texture_Safe;
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

    public override void Render() {
        if (sparklingWater.Count == 0 || buffer is null || buffer.IsDisposed)
            return;

        Camera camera = SceneAs<Level>().Camera;

        Draw.SpriteBatch.Draw(buffer, camera.Position, Color.White);

        Settings settings = Settings.GetSettings(Scene);
        if (settings.DisplacementSpeed <= 0f)
            return; // no need to distort

        GameplayRenderer.End();

        RenderTargetBinding[] prevRenderTargets = Engine.Instance.GraphicsDevice.GetRenderTargets();
        RenderTarget2D gameplayBuffer = GameplayBuffers.Gameplay;
        if (prevRenderTargets.Length > 0)
            gameplayBuffer = prevRenderTargets[0].RenderTarget as RenderTarget2D ?? gameplayBuffer;

        // copy the gameplay buffer into a seperate render target temporarily
        Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempA);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
        Draw.SpriteBatch.Draw(gameplayBuffer, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();

        // distort the gameplay buffer (based on Distort.Apply)
        // vanilla distortion applies a huge offset down and to the right when the background is Color.Transparent since r and g are both 0 (positive displacement), and not 0.5 (no displacement).
        // unfortunately though, when drawing to multiple render targets you can't clear them at the same time with different colors!
        // so we use a simplified custom distort effect that only does the water displacement instead.
        Engine.Instance.GraphicsDevice.SetRenderTargets(prevRenderTargets);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        Effect distortEffect = SorbetHelperGFX.FxSparklingWaterDistort;
        distortEffect.Parameters["water_sine"].SetValue(timer * 16f);
        distortEffect.Parameters["water_camera_y"].SetValue((int)Math.Floor(camera.Y));

        Texture prevTexture1 = Engine.Graphics.GraphicsDevice.Textures[1];
        Engine.Graphics.GraphicsDevice.Textures[1] = displacementBuffer;

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, distortEffect);
        Draw.SpriteBatch.Draw(GameplayBuffers.TempA, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();

        Engine.Graphics.GraphicsDevice.Textures[1] = prevTexture1;

        GameplayRenderer.Begin();
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

    public static SparklingWaterRenderer GetSparklingWaterRenderer(Scene scene, int depth) {
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
