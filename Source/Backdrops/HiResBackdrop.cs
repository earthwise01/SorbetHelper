using Celeste.Mod.UI;

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

            Matrix cameraToScreenMatrix = level.GetCameraToScreenMatrix();
            if (!SubHudRenderer.DrawToBuffer)
                cameraToScreenMatrix *= Engine.ScreenMatrix;

            SubHudRenderer.EndRender();

            bool spriteBatchActive = false;
            foreach (HiResBackdrop backdrop in backdrops)
            {
                if (!backdrop.Visible)
                    continue;

                switch (backdrop.UseHiResSpritebatch)
                {
                    case true when !spriteBatchActive:
                        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, cameraToScreenMatrix);
                        spriteBatchActive = true;
                        break;
                    case false when spriteBatchActive:
                        Draw.SpriteBatch.End();
                        spriteBatchActive = false;
                        break;
                }

                backdrop.RenderHiRes(Scene, cameraToScreenMatrix);
            }

            if (spriteBatchActive)
                Draw.SpriteBatch.End();

            SubHudRenderer.BeginRender();
        }
    }
}
