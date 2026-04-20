namespace Celeste.Mod.SorbetHelper.Utils;

// ty aon
internal static class HookHelper
{
    public static void DisposeAndSetNull(ref Hook hook)
    {
        hook?.Dispose();
        hook = null;
    }

    public static void DisposeAndSetNull(ref ILHook ilHook)
    {
        ilHook?.Dispose();
        ilHook = null;
    }

    /// <summary>
    /// Contains commonly used <see cref="BindingFlags"/>.
    /// </summary>
    public static class Bind
    {
        /// <summary>
        /// Shorthand for <see cref="BindingFlags.Public"/> and <see cref="BindingFlags.Static"/>.
        /// </summary>
        public const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

        /// <summary>
        /// Shorthand for <see cref="BindingFlags.NonPublic"/> and <see cref="BindingFlags.Static"/>.
        /// </summary>
        public const BindingFlags NonPublicStatic = BindingFlags.NonPublic | BindingFlags.Static;

        /// <summary>
        /// Shorthand for <see cref="BindingFlags.Public"/> and <see cref="BindingFlags.Instance"/>.
        /// </summary>
        public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        /// <summary>
        /// Shorthand for <see cref="BindingFlags.NonPublic"/> and <see cref="BindingFlags.Instance"/>.
        /// </summary>
        public const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    }

    /// <summary>
    /// Custom <see cref="Exception"/> to be thrown when hook application fails.
    /// </summary>
    public class HookException : Exception
    {
        public HookException(string message, Exception inner = null) : base($"Hook application failed: {message}", inner) { }

        public HookException(ILContext il, string message, Exception inner = null) : base($"IL hook application on method {il.Method.FullName} failed: {message}", inner) { }
    }
}
