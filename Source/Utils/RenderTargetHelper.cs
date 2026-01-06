using System.Collections.Generic;

namespace Celeste.Mod.SorbetHelper.Utils;

internal static class RenderTargetHelper {
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(RenderTargetHelper)}";

    private static Queue<VirtualRenderTarget> RenderTargets { get; } = [];

    /// <summary>
    /// get a gameplay buffer<br/>
    /// make sure to either dispose it manually at some point or return it for later use
    /// </summary>
    /// <returns>a <see cref="VirtualRenderTarget"/> with dimensions matching the vanilla gameplay buffer <see cref="GameplayBuffers.Gameplay"/> (usually 320x180)</returns>
    public static VirtualRenderTarget GetGameplayBuffer() {
        if (RenderTargets.Count == 0)
            return VirtualContent.CreateRenderTarget("sorbetHelper-tempBuffer", SorbetHelperGFX.GameplayBufferWidth, SorbetHelperGFX.GameplayBufferHeight);

        VirtualRenderTarget cached = RenderTargets.Dequeue();
        if (cached.IsDisposed)
            cached = VirtualContent.CreateRenderTarget("sorbetHelper-tempBuffer", SorbetHelperGFX.GameplayBufferWidth, SorbetHelperGFX.GameplayBufferHeight);
        else
            SorbetHelperGFX.EnsureBufferSize(cached);

        return cached;
    }

    /// <summary>
    /// return a gameplay buffer to the queue for later use<br/>
    /// </summary>
    /// <param name="vrt">the gameplay buffer to return</param>
    public static void ReturnGameplayBuffer(VirtualRenderTarget vrt) => RenderTargets.Enqueue(vrt);

    /// <summary>
    /// get an array of gameplay buffers
    /// </summary>
    /// <param name="count">how many gameplay buffers</param>
    /// <returns>the gameplay buffers</returns>
    public static VirtualRenderTarget[] GetGameplayBuffers(int count) {
        VirtualRenderTarget[] buffers = new VirtualRenderTarget[count];

        for (int i = 0; i < count; i++)
            buffers[i] = GetGameplayBuffer();

        return buffers;
    }

    /// <summary>
    /// return an array of gameplay buffers to the queue for later use<br/>
    /// replaces all values in the array with null
    /// </summary>
    /// <param name="vrts">the gameplay buffers to return</param>
    public static void ReturnGameplayBuffers(VirtualRenderTarget[] vrts) {
        int count = vrts.Length;

        for (int i = 0; i < count; i++) {
            ReturnGameplayBuffer(vrts[i]);
            vrts[i] = null;
        }
    }

    /// <summary>
    /// dispose a <see cref="VirtualRenderTarget"/> and set it to null
    /// </summary>
    /// <param name="renderTarget">the <see cref="VirtualRenderTarget"/> to dispose</param>
    public static void DisposeAndSetNull(ref VirtualRenderTarget renderTarget) {
        renderTarget?.Dispose();
        renderTarget = null;
    }

    private static void DisposeQueue() {
        try {
            foreach (VirtualRenderTarget vrt in RenderTargets)
                vrt?.Dispose();

            RenderTargets.Clear();
        } catch (Exception e) {
            Logger.Error(LogID, $"???? literally how? {e}"); // this threw an error one time when reloading the mod and i cant replicate it anymore. fun!
        }
    }

    // private static void Log() {
    //     Engine.Commands.Log($"{RenderTargets.Count} render targets are currently queued");
    // }

    #region Hooks

    internal static void Load() {
        On.Celeste.GameplayBuffers.Unload += On_GameplayBuffers_Unload;
    }

    internal static void Unload() {
        On.Celeste.GameplayBuffers.Unload -= On_GameplayBuffers_Unload;
        DisposeQueue();
    }

    // unload any leftover queued buffers with the normal gameplay buffers
    private static void On_GameplayBuffers_Unload(On.Celeste.GameplayBuffers.orig_Unload orig) {
        orig();
        DisposeQueue();
    }

    #endregion

}
