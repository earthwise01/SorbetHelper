using System;
using Celeste;
using Monocle;

namespace Celeste.Mod.SorbetHelper {
    internal static class Extensions {
        public static bool GetFlag(this Session session, string flag, bool inverted) =>
            session.GetFlag(flag) != inverted;
    }
}
