using System;
using Monocle;
using Celeste;

namespace Celeste.Mod.SorbetHelper.Utils {
    public static class Extensions {
        public static bool GetFlag(this Session session, string flag, bool inverted) =>
            session.GetFlag(flag) != inverted;
    }
}
