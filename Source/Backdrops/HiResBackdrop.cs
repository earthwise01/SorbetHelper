using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.UI;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Backdrops;

public abstract class HiResBackdrop : Backdrop
{
    /// <summary>
    /// Whether to automatically start a <see cref="SpriteBatch"/> when calling <see cref="RenderHiRes"/>.
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
    /// <param name="cameraToScreenMatrix">The matrix used to scale and position the backdrop into screen space (1920x1080) from camera space (320x180).</param>
    public virtual void RenderHiRes(Scene scene, Matrix cameraToScreenMatrix) { }

    #region Hooks

    internal static void Load()
    {
        Everest.Events.LevelLoader.OnLoadingThread += OnLoadingThread;
    }

    internal static void Unload()
    {
        Everest.Events.LevelLoader.OnLoadingThread -= OnLoadingThread;
    }

    private static void OnLoadingThread(Level level)
    {
        HiResBackdropRenderer renderer = null;
        foreach (Backdrop backdrop in level.Foreground.Backdrops)
        {
            if (backdrop is not HiResBackdrop hiResBackdrop)
                continue;

            if (renderer is null)
                level.Add(renderer = new HiResBackdropRenderer());

            renderer.AddBackdrop(hiResBackdrop);
            hiResBackdrop.Added(level);
        }
    }

    #endregion

    private class HiResBackdropRenderer : Entity
    {
        private readonly List<HiResBackdrop> backdrops = [];

        public HiResBackdropRenderer() : base()
        {
            Tag = global::Celeste.Tags.Global | TagsExt.SubHUD;
            Depth = 2000000;
        }

        public void AddBackdrop(HiResBackdrop backdrop)
        {
            backdrops.Add(backdrop);
        }

        public override void Render()
        {
            if (backdrops.All(backdrop => !backdrop.Visible))
                return;

            Level level = SceneAs<Level>();
            
            Matrix matrix = Matrix.Identity;

            // zoom & padding
            float zoom = level.Zoom;
            if (ExtendedVariantsCompat.IsLoaded)
                zoom *= ExtendedVariantsCompat.GetZoomLevel();
            float zoomTarget = ExtendedCameraDynamicsInterop.IsImported && ExtendedCameraDynamicsInterop.ExtendedCameraHooksEnabled()
                ? level.Zoom
                : level.ZoomTarget;
            Vector2 dimensions = new Vector2(320f, 180f);
            Vector2 scaledDimensions = dimensions / zoomTarget;
            Vector2 zoomOrigin = zoomTarget != 1f ? (level.ZoomFocusPoint - scaledDimensions / 2f) / (dimensions - scaledDimensions) * dimensions : Vector2.Zero;

            Vector2 paddingOffset = new Vector2(level.ScreenPadding, level.ScreenPadding * (9f / 16f));
            if (ExtendedVariantsCompat.IsLoaded)
                paddingOffset = ExtendedVariantsCompat.AddZoomPaddingOffset(paddingOffset);

            float scale = zoom * (320f - level.ScreenPadding * 2f) / 320f;

            matrix *= Matrix.CreateTranslation(-zoomOrigin.X, -zoomOrigin.Y, 0f)
                      * Matrix.CreateScale(scale)
                      * Matrix.CreateTranslation(zoomOrigin.X + paddingOffset.X, zoomOrigin.Y + paddingOffset.Y, 0f);

            // mirror mode & upside down
            if (SaveData.Instance.Assists.MirrorMode)
                matrix *= Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateTranslation(320f, 0f, 0f);
            if (ExtendedVariantsCompat.IsLoaded && ExtendedVariantsCompat.GetUpsideDown())
                matrix *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, 180f, 0f);

            // scale to screen size
            matrix *= Matrix.CreateScale(6f);
            if (!SubHudRenderer.DrawToBuffer)
                matrix *= Engine.ScreenMatrix;

            SubHudRenderer.EndRender();

            bool spriteBatchActive = false;
            foreach (HiResBackdrop backdrop in backdrops)
            {
                if (!backdrop.Visible)
                    continue;

                switch (backdrop.UseHiResSpritebatch)
                {
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
