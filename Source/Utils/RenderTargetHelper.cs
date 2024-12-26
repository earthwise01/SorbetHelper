using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using Celeste.Mod.SorbetHelper.Entities;
using MonoMod.Utils;

namespace Celeste.Mod.SorbetHelper.Utils;

public static class RenderTargetHelper {
    private static Queue<VirtualRenderTarget> RenderTargets { get; set; } = [];

    /// <summary>
    /// get a gameplay buffer<br/>
    /// make sure to either dispose it manually at some point or return it for later use
    /// </summary>
    /// <returns>a virtual render target with dimensions matching the vanilla gameplay buffers (usually 320x180)</returns>
    public static VirtualRenderTarget GetGameplayBuffer() {
        if (RenderTargets.Count == 0)
            return VirtualContent.CreateRenderTarget("sorbetHelper-tempBuffer", Util.GameplayBufferWidth, Util.GameplayBufferHeight);

        var cached = RenderTargets.Dequeue();
        if (cached.IsDisposed)
            cached = VirtualContent.CreateRenderTarget("sorbetHelper-tempBuffer", Util.GameplayBufferWidth, Util.GameplayBufferHeight);
        else
            Util.CheckResizeBuffer(cached);

        return cached;
    }

    /// <summary>
    /// return a gameplay buffer to the queue for later use<br/>
    /// (can already be disposed as well but why would you do that what)
    /// </summary>
    /// <param name="vrt">the gameplay buffer to return</param>
    public static void ReturnGameplayBuffer(VirtualRenderTarget vrt) {
        RenderTargets.Enqueue(vrt);
    }

    /// <summary>
    /// get an array of gameplay buffers
    /// </summary>
    /// <param name="count">how many gameplay buffers</param>
    /// <returns>the gameplay buffers</returns>
    public static VirtualRenderTarget[] GetGameplayBuffers(int count) {
        var buffers = new VirtualRenderTarget[count];

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
        var count = vrts.Length;

        for (int i = 0; i < count; i++) {
            ReturnGameplayBuffer(vrts[i]);
            vrts[i] = null;
        }
    }

    /// <summary>
    /// dispose any gameplay buffers currently in the queue
    /// </summary>
    public static void DisposeQueue() {
        try {
            foreach (var vrt in RenderTargets) {
                vrt?.Dispose();
            }
            RenderTargets.Clear();
        } catch (Exception e) {
            Logger.Error(nameof(SorbetHelper), $"???? literally how? {e}"); // this threw an error one time when reloading the mod and i cant replicate it anymore. fun!
        }
    }
    // private static void Log() {
    //     Engine.Commands.Log($"{RenderTargets.Count} render targets are currently queued");
    // }
}

