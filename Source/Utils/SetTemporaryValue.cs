namespace Celeste.Mod.SorbetHelper.Utils;

/// <summary>
/// Sets a reference to a temporary value and restores the original value when disposed.
/// </summary>
public readonly ref struct SetTemporaryValue<T>
{
    private readonly ref T value;
    private readonly T origValue;

    public SetTemporaryValue(ref T value, T newValue)
    {
        origValue = value;
        (this.value = ref value) = newValue;
    }

    public void Dispose()
    {
        value = origValue;
    }
}
