namespace Celeste.Mod.SorbetHelper.Utils;

internal static class EffectHelper
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(EffectHelper)}";

    public static Effect LoadEffect(string id)
    {
        string path = $"SorbetHelper:/Effects/SorbetHelper/{id}.cso";

        if (!Everest.Content.TryGet(path, out ModAsset effectAsset))
            throw new Exception($"Failed to find effect at {path}!");

        Logger.Info(LogID, $"Loaded effect from {path}.");
        return new Effect(Engine.Graphics.GraphicsDevice, effectAsset.Data);
    }

    public static void DisposeAndSetNull(ref Effect effect)
    {
        effect?.Dispose();
        effect = null;
    }
}
