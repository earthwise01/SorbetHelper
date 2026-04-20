namespace Celeste.Mod.SorbetHelper.Utils;

internal static class RenderTargetHelper
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(RenderTargetHelper)}";

    public static int GameplayWidth => GameplayBuffers.Gameplay?.Width ?? 320;
    public static int GameplayHeight => GameplayBuffers.Gameplay?.Height ?? 180;

    public static void CreateOrResizeGameplayBuffer(ref VirtualRenderTarget target)
    {
        if (target is not { IsDisposed: false })
        {
            target = VirtualContent.CreateRenderTarget("sorbet-helper-gameplay-buffer", GameplayWidth, GameplayHeight);
            return;
        }

        if (target.Width == GameplayWidth && target.Height == GameplayHeight)
            return;

        target.Width = GameplayWidth;
        target.Height = GameplayHeight;
        target.Reload();
    }

    public static void DisposeAndSetNull(ref VirtualRenderTarget renderTarget)
    {
        renderTarget?.Dispose();
        renderTarget = null;
    }

    #region Temporary Gameplay Buffers

    private static readonly Queue<VirtualRenderTarget> TempBuffers = [];

    public static VirtualRenderTarget GetTempBuffer()
    {
        if (!TempBuffers.TryDequeue(out VirtualRenderTarget tempBuffer))
            tempBuffer = null;

        CreateOrResizeGameplayBuffer(ref tempBuffer);

        return tempBuffer;
    }

    public static void ReturnTempBuffer(VirtualRenderTarget gameplayBuffer)
    {
        if (gameplayBuffer is not null && !gameplayBuffer.IsDisposed)
            TempBuffers.Enqueue(gameplayBuffer);
    }

    public static VirtualRenderTarget[] GetTempBuffers(int count)
    {
        VirtualRenderTarget[] tempBuffers = new VirtualRenderTarget[count];

        for (int i = 0; i < count; i++)
            tempBuffers[i] = GetTempBuffer();

        return tempBuffers;
    }

    public static void ReturnTempBuffers(ref VirtualRenderTarget[] gameplayBuffers)
    {
        foreach (VirtualRenderTarget gameplayBuffer in gameplayBuffers)
            ReturnTempBuffer(gameplayBuffer);

        gameplayBuffers = null;
    }

    private static void DisposeQueue()
    {
        try
        {
            foreach (VirtualRenderTarget vrt in TempBuffers)
                vrt.Dispose();

            TempBuffers.Clear();
        }
        catch (Exception e)
        {
            Logger.Error(LogID, $"Error while trying to dispose temporary gameplay buffer queue! {e}");
        }
    }

    #region Hooks

    [OnLoad]
    internal static void Load()
    {
        On.Celeste.GameplayBuffers.Unload += On_GameplayBuffers_Unload;
    }

    [OnUnload]
    internal static void Unload()
    {
        On.Celeste.GameplayBuffers.Unload -= On_GameplayBuffers_Unload;

        DisposeQueue();
    }

    private static void On_GameplayBuffers_Unload(On.Celeste.GameplayBuffers.orig_Unload orig)
    {
        orig();

        DisposeQueue();
    }

    #endregion

    #endregion
}
