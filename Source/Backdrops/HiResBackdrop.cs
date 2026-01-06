using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.UI;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Backdrops;

public abstract class HiResBackdrop : Backdrop {
    private const float UpscaleAmount = 6f;

    /// <summary>
    /// Whether to automatically start a <see cref="SpriteBatch"/> when calling <see cref="RenderHiRes"/>, or let the backdrop handle everything itself.
    /// </summary>
    public virtual bool UseHiResSpritebatch => true;

    /// <summary>
    /// Called at the end of the <see cref="LevelLoader.LoadingThread"/> after the backdrop is added to the <see cref="Level"/>.
    /// </summary>
    /// <param name="level">The <see cref="Level"/> instance the backdrop was added to.</param>
    public virtual void Added(Level level) { }

    /// <summary>
    /// Renders the backdrop.
    /// </summary>
    /// <param name="scene">The scene the backdrop is being rendered in.</param>
    /// <param name="upscaleMatrix">The matrix used to upscale and position the backdrop into screen space (1920x1080) from camera space (320x180).</param>
    public virtual void RenderHiRes(Scene scene, Matrix upscaleMatrix) { }

    #region Hooks

    internal static void Load() {
        Everest.Events.LevelLoader.OnLoadingThread += OnLoadingThread;
    }

    internal static void Unload() {
        Everest.Events.LevelLoader.OnLoadingThread -= OnLoadingThread;
    }

    private static void OnLoadingThread(Level level) {
        HiResBackdropRenderer renderer = null;
        foreach (Backdrop backdrop in level.Foreground.Backdrops) {
            if (backdrop is not HiResBackdrop hiResBackdrop)
                continue;

            if (renderer is null)
                level.Add(renderer = new HiResBackdropRenderer());

            renderer.AddBackdrop(hiResBackdrop);
            hiResBackdrop.Added(level);
        }
    }

    #endregion

    private class HiResBackdropRenderer : Entity {
        private readonly List<HiResBackdrop> backdrops = [];

        public HiResBackdropRenderer() : base() {
            Tag = global::Celeste.Tags.Global | TagsExt.SubHUD;
            Depth = 2000000;
        }

        public void AddBackdrop(HiResBackdrop backdrop) {
            backdrops.Add(backdrop);
        }

        public override void Render() {
            // todo: not sure what the threshold for this check should be    if any
            if (backdrops.Count < 10 && backdrops.All(backdrop => !backdrop.Visible))
                return;

            Level level = SceneAs<Level>();
            Matrix matrix = Matrix.CreateScale(UpscaleAmount);

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

            if (!SubHudRenderer.DrawToBuffer)
                matrix *= Engine.ScreenMatrix;

            SubHudRenderer.EndRender();

            bool spriteBatchActive = false;
            foreach (HiResBackdrop backdrop in backdrops) {
                if (!backdrop.Visible)
                    continue;

                switch (backdrop.UseHiResSpritebatch) {
                    case true when !spriteBatchActive:
                        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, matrix);
                        spriteBatchActive = true;
                        break;
                    case false when spriteBatchActive:
                        Draw.SpriteBatch.End();
                        spriteBatchActive = false;
                        break;
                }

                backdrop.RenderHiRes(Scene, matrix);
            }

            if (spriteBatchActive)
                Draw.SpriteBatch.End();

            SubHudRenderer.BeginRender();
        }
    }
}
