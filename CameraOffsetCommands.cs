using Monocle;

namespace Celeste.Mod.SorbetHelper {

    public class CameraOffsetCommands {

        [Command("set_camera_offset_x", "Sets the camera X offset to the specifed value. (Sorbet Helper)")]
        public static void SetXOffset(float offset = 0f) {
            (Engine.Scene as Level).CameraOffset.X = offset * 48f;
            Engine.Commands.Log("set camera x offset to " + offset.ToString());
        }

        [Command("set_camera_offset_y", "Sets the camera X offset to the specifed value. (Sorbet Helper)")]
        public static void SetYOffset(float offset = 0f) {
            (Engine.Scene as Level).CameraOffset.Y = offset * 32f;
            Engine.Commands.Log("set camera y offset to " + offset.ToString());
        }

        [Command("get_camera_offset", "Shows the current camera X/Y offsets. (Sorbet Helper)")]
        public static void GetOffset() {
            float x = (Engine.Scene as Level).CameraOffset.X / 48f;
            float y = (Engine.Scene as Level).CameraOffset.Y / 32f;
            Engine.Commands.Log("X offset: " + x.ToString());
            Engine.Commands.Log("Y offset: " + y.ToString());
        }

    }

}
