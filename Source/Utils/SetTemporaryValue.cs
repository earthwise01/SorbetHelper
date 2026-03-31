namespace Celeste.Mod.SorbetHelper.Utils;

/// <summary>
/// Sets a reference to a temporary value and restores the original value when disposed.
/// </summary>
public readonly ref struct SetTemporaryValue<T>
{
    private readonly ref T value;
    private readonly T originalValue;

    public SetTemporaryValue(ref T value, T temporaryValue)
    {
        originalValue = value;
        (this.value = ref value) = temporaryValue;
    }

    public void Dispose()
    {
        value = originalValue;
    }
}
