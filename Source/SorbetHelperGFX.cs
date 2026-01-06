using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper;

public static class SorbetHelperGFX {
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(SorbetHelperGFX)}";

    public static int GameplayBufferWidth => GameplayBuffers.Gameplay?.Width ?? 320;
    public static int GameplayBufferHeight => GameplayBuffers.Gameplay?.Height ?? 180;

    public static bool ZoomOutActive => UsingExtendedCameraDynamics ||
                                        GameplayBufferWidth != 320 || GameplayBufferHeight != 180 ||
                                        (Engine.Scene is Level level && (level.Camera.Width != 320 || level.Camera.Height != 180));
    public static bool UsingExtendedCameraDynamics => ExtendedCameraDynamicsInterop.IsImported && ExtendedCameraDynamicsInterop.ExtendedCameraHooksEnabled();
    public static Vector2 GetZoomOutCameraCenterOffset(Camera camera) => new Vector2(camera.Width / 2f - 320f / 2f, camera.Height / 2f - 180f / 2f);

    public static Effect FxAlphaMask { get; private set; }

    internal static void LoadContent() {
        FxAlphaMask = LoadEffect("AlphaMask");
    }

    internal static void UnloadContent() {
        FxAlphaMask?.Dispose();
        FxAlphaMask = null;
    }

    private static Effect LoadEffect(string id) {
        string path = $"SorbetHelper:/Effects/SorbetHelper/{id}.cso";
        Logger.Info(LogID, $"Loading effect from {path}");

        if (!Everest.Content.TryGet(path, out ModAsset effect))
            Logger.Error(LogID, $"Failed to find effect at {path}!");

        return new Effect(Engine.Graphics.GraphicsDevice, effect.Data);
    }

    public static void EnsureBufferSize(VirtualRenderTarget target) {
        if (target is null || target.IsDisposed || (target.Width == GameplayBufferWidth && target.Height == GameplayBufferHeight))
            return;

        target.Width = GameplayBufferWidth;
        target.Height = GameplayBufferHeight;
        target.Reload();
    }

#if DEBUG

    [Command("sorbethelper_reloadgfx", "Reloads SorbetHelper GFX")]
    public static void ReloadContentCommand()
    {
        Logger.Info(LogID, "Reloading GFX...");

        UnloadContent();
        LoadContent();
    }

#endif
}
