using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.SorbetHelper.Utils;

/// <summary>
/// Resets the <see cref="Texture"/> slots, <see cref="SamplerState"/> slots, <see cref="BlendState"/>, <see cref="DepthStencilState"/>, <see cref="RasterizerState"/>, and optionally the render targets of a given <see cref="GraphicsDevice"/> when disposed.
/// </summary>
public readonly ref struct ResetGraphicsStateOnDispose
{
    private readonly GraphicsDevice graphicsDevice;

    private readonly int resetSamplerSlots;
    private readonly Texture[] previousTextures;
    private readonly SamplerState[] previousSamplerStates;

    private readonly BlendState previousBlendState;
    private readonly DepthStencilState previousDepthStencilState;
    private readonly RasterizerState previousRasterizerState;

    private readonly RenderTargetBinding[] previousRenderTargets;

    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> to reset the state of.</param>
    /// <param name="resetSamplerSlots">The number of sampler slots (<see cref="Texture"/> slots + <see cref="SamplerState"/> slots) to reset.</param>
    /// <param name="resetRenderTargets">Whether to also reset the GraphicsDevice's render targets.</param>
    public ResetGraphicsStateOnDispose(GraphicsDevice graphicsDevice, int resetSamplerSlots, bool resetRenderTargets)
    {
        this.graphicsDevice = graphicsDevice;
        this.resetSamplerSlots = resetSamplerSlots;

        previousTextures = new Texture[resetSamplerSlots];
        previousSamplerStates = new SamplerState[resetSamplerSlots];
        for (int i = 0; i < resetSamplerSlots; i++)
        {
            previousTextures[i] = graphicsDevice.Textures[i];
            previousSamplerStates[i] = graphicsDevice.SamplerStates[i];
        }

        previousBlendState = graphicsDevice.BlendState;
        previousDepthStencilState = graphicsDevice.DepthStencilState;
        previousRasterizerState = graphicsDevice.RasterizerState;

        if (resetRenderTargets)
            previousRenderTargets = graphicsDevice.GetRenderTargets();
    }

    public void Dispose()
    {
        for (int i = 0; i < resetSamplerSlots; i++)
        {
            graphicsDevice.Textures[i] = previousTextures[i];
            graphicsDevice.SamplerStates[i] = previousSamplerStates[i];
        }

        graphicsDevice.BlendState = previousBlendState;
        graphicsDevice.DepthStencilState = previousDepthStencilState;
        graphicsDevice.RasterizerState = previousRasterizerState;

        if (previousRenderTargets is not null)
            graphicsDevice.SetRenderTargets(previousRenderTargets);
    }
}
