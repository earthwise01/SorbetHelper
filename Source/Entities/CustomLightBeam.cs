using System.Collections.Generic;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/CustomLightbeam")]
public class CustomLightBeam : Entity {
    private readonly string flag;
    private readonly bool invertFlag;
    private readonly float flagFadeTime;

    private readonly float rotation;

    private readonly bool fadeWhenNear;
    private readonly bool fadeOnTransition;
    private readonly bool noParticles;

    private Color color;

    private readonly bool rainbow;
    private const int RainbowSegmentSize = 4;

    private readonly bool useCustomRainbowColors;

    private readonly List<Color> rainbowColors = [];
    private readonly float rainbowGradientSize;
    private readonly float rainbowGradientSpeed;
    private readonly bool rainbowLoopColors;
    private readonly Vector2 rainbowCenter;
    private readonly bool rainbowSingleColor;

    private readonly float scroll;
    private readonly Vector2 scrollAnchor;
    private Vector2 RenderPosition {
        get {
            // don't need to do anything special for non-parallax lightbeams
            if (scroll == 1f)
                return Position;

            // hopefully i   mathed right and this actually fully does what i think it does
            Vector2 cam = SceneAs<Level>().Camera.Center;
            return cam + (Position - scrollAnchor * (1f - scroll) - cam * scroll);
        }
    }

    private readonly float baseAlpha;
    private float distanceAlpha = 1f;
    private float flagAlpha = 1f;
    private float alpha;

    private readonly int lightWidth;
    private readonly int lightLength;

    private float timer = Calc.Random.NextFloat(1000f);

    private readonly MTexture beamTexture;
    private readonly float offset;

    private readonly float cullRectTop, cullRectBottom, cullRectLeft, cullRectRight;
    private const int VisibilityPadding = 16;

    public CustomLightBeam(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Tag = Tags.TransitionUpdate;
        this.offset = Calc.Random.NextFloat();

        lightWidth = data.Width;
        lightLength = data.Height;
        Depth = data.Int("depth", -9998);

        flag = data.Attr("flag");
        invertFlag = data.Bool("inverted", false);
        flagFadeTime = Math.Max(data.Float("flagFadeTime", 0.25f), 0f);
        rotation = data.Float("rotation", 0f) * (MathF.PI / 180f);

        fadeWhenNear = data.Bool("fadeWhenNear", true);
        fadeOnTransition = data.Bool("fadeOnTransition", true);

        scroll = data.Float("scroll", 1f);
        if (data.Nodes?.Length >= 1)
            scrollAnchor = data.Nodes[0] + offset;
        else
            scrollAnchor = Position;

        baseAlpha = Math.Clamp(data.Float("alpha", 1f), 0f, 1f);

        beamTexture = GFX.Game[data.Attr("texture", "util/lightbeam")];
        noParticles = data.Bool("noParticles", false);
        color = data.HexColorWithNonPremultipliedAlpha("color", Calc.HexToColor("CCFFFF"));
        rainbow = data.Bool("rainbow", false);
        useCustomRainbowColors = data.Bool("useCustomRainbowColors", false);

        if (rainbow) {
            rainbowGradientSize = data.Float("gradientSize", 280f);
            rainbowGradientSpeed = data.Float("gradientSpeed", 50f);
            rainbowLoopColors = data.Bool("loopColors", false);
            rainbowCenter = new Vector2(data.Float("centerX", 0), data.Float("centerY", 0));
            rainbowSingleColor = data.Bool("singleColor", false);

            string[] colorsAsStrings = data.Attr("colors", "89E5AE,88E0E0,87A9DD,9887DB,D088E2").Split(',');
            foreach (string t in colorsAsStrings)
                rainbowColors.Add(Calc.HexToColorWithNonPremultipliedAlpha(t));

            if (rainbowLoopColors)
                rainbowColors.Add(rainbowColors[0]);
        }

        // offscreen culling bounds
        Vector2 baseCornerA = -Calc.AngleToVector(rotation, 1f) * (lightWidth / 2f); // would be top left with a rotation of 0f
        Vector2 baseCornerB = Calc.AngleToVector(rotation, 1f) * (lightWidth / 2f); // would be top right with a rotation of 0f
        Vector2 edgeCornerA = baseCornerA + Calc.AngleToVector(rotation + (float)Math.PI / 2f, 1f) * lightLength; // would be bottom left with a rotation of 0f
        Vector2 edgeCornerB = baseCornerB + Calc.AngleToVector(rotation + (float)Math.PI / 2f, 1f) * lightLength; // would be bottom right with a rotation of 0f

        cullRectTop = Math.Min(Math.Min(baseCornerA.Y, baseCornerB.Y), Math.Min(edgeCornerA.Y, edgeCornerB.Y));
        cullRectBottom = Math.Max(Math.Max(baseCornerA.Y, baseCornerB.Y), Math.Max(edgeCornerA.Y, edgeCornerB.Y));
        cullRectLeft = Math.Min(Math.Min(baseCornerA.X, baseCornerB.X), Math.Min(edgeCornerA.X, edgeCornerB.X));
        cullRectRight = Math.Max(Math.Max(baseCornerA.X, baseCornerB.X), Math.Max(edgeCornerA.X, edgeCornerB.X));
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        Level level = SceneAs<Level>();

        if (!string.IsNullOrEmpty(flag) && !level.Session.GetFlag(flag, invertFlag))
            flagAlpha = 0f;
        else
            flagAlpha = 1f;

        if (level.Transitioning && fadeOnTransition)
            distanceAlpha = 0f;

        alpha = baseAlpha * distanceAlpha * flagAlpha;
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        if (rainbow)
            RainbowHelper.SetGetHueScene(null);
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);
        if (rainbow)
            RainbowHelper.SetGetHueScene(null);
    }

    public override void Update() {
        base.Update();

        timer += Engine.DeltaTime;
        Level level = SceneAs<Level>();
        Player player = Scene.Tracker.GetEntity<Player>();

        Vector2 actualPosition = Position;
        Position = RenderPosition;

        Visible = InView(level.Camera);

        // vanilla proximity/transition fading
        if (player is not null) {
            float targetAlpha = 1f;

            if (fadeWhenNear) {
                Vector2 direction = Calc.AngleToVector(rotation + MathF.PI / 2f, 1f);
                Vector2 playerDistancePoint = Calc.ClosestPointOnLine(Position, Position + direction * 10000f, player.Center);

                if ((playerDistancePoint - player.Center).Length() <= lightWidth / 2f)
                    targetAlpha = Math.Min(1f, Math.Max(0f, (playerDistancePoint - Position).Length() - 8f) / lightLength);
            }

            if (level.Transitioning && fadeOnTransition)
                targetAlpha = 0f;

            distanceAlpha = Calc.Approach(distanceAlpha, targetAlpha, Engine.DeltaTime * 4f);
        }

        // flag fading
        if (!string.IsNullOrEmpty(flag)) {
            float targetAlpha = level.Session.GetFlag(flag, invertFlag) ? 1f : 0f;
            flagAlpha = Calc.Approach(flagAlpha, targetAlpha, Engine.DeltaTime / flagFadeTime);
        }

        // multiply baseAlpha, distanceAlpha, and flagAlpha together to get the actual alpha of the lightbeam.
        alpha = baseAlpha * distanceAlpha * flagAlpha;

        // emit particles
        if (Visible && !noParticles && alpha >= 0.5f && level.OnInterval(0.8f, offset)) {
            Vector2 direction = Calc.AngleToVector(rotation + MathF.PI / 2f, 1f);
            Vector2 particlePos = Position - direction * 4f;
            float emitX = Calc.Random.Next(lightWidth - 4) + 2 - lightWidth / 2f;
            particlePos += emitX * direction.Perpendicular();

            // if rainbow is enabled and rainbowSingleColor is disabled, call GetHue for the particle's color, otherwise use the color variable.
            Color particleColor;
            if (rainbow && !rainbowSingleColor) {
                RainbowHelper.SetGetHueScene(Scene);
                particleColor = GetHue(particlePos);
            } else {
                particleColor = color;
            }

            // todo: maybe transfer over the lightbeam's parallax somehow?
            level.Particles.Emit(LightBeam.P_Glow, particlePos, particleColor, rotation + MathF.PI / 2f);
        }

        Position = actualPosition;
    }

    /*
     public override void DebugRender(Camera camera) {
        base.DebugRender(camera);

        Vector2 baseCornerA = Position - Calc.AngleToVector(rotation, 1f) * (lightWidth / 2f); // would be top left with a rotation of 0f
        Vector2 baseCornerB = Position + Calc.AngleToVector(rotation, 1f) * (lightWidth / 2); // would be top right with a rotation of 0f
        Vector2 edgeCornerA = baseCornerA + Calc.AngleToVector(rotation + (float)Math.PI / 2f, 1f) * lightLength; // would be bottom left with a rotation of 0f
        Vector2 edgeCornerB = baseCornerB + Calc.AngleToVector(rotation + (float)Math.PI / 2f, 1f) * lightLength; // would be bottom right with a rotation of 0f
        Draw.Line(baseCornerA, baseCornerB, Color.GreenYellow * 0.5f);
        Draw.Line(baseCornerA, edgeCornerA, Color.GreenYellow * 0.5f);
        Draw.Line(baseCornerB, edgeCornerB, Color.GreenYellow * 0.5f);
        Draw.Line(edgeCornerA, edgeCornerB, Color.GreenYellow * 0.5f);

        Draw.HollowRect(rectangleLeft - visibilityPadding / 2, rectangleTop - visibilityPadding / 2, rectangleRight - rectangleLeft + visibilityPadding, rectangleBottom - rectangleTop + visibilityPadding, Color.Yellow * 0.5f);
    }
    */

    public override void Render() {
        base.Render();

        if (alpha <= 0f)
            return;

        Vector2 actualPosition = Position;
        Position = RenderPosition;

        if (rainbow)
            RainbowHelper.SetGetHueScene(Scene);

        if (rainbow && rainbowSingleColor)
            color = GetHue(Position);

        // render the base
        if (rainbow && !rainbowSingleColor) {
            // draw the base in 4px segments to make a gradient effect
            for (int i = 0; i < lightWidth; i += RainbowSegmentSize)
                DrawBeam(i - lightWidth / 2f, RainbowSegmentSize, lightLength - 4 + (float)Math.Sin(timer * 2f) * 4f, 0.4f);
        } else {
            DrawBeam(0f, lightWidth, lightLength - 4 + (float)Math.Sin(timer * 2f) * 4f, 0.4f);
        }

        // render the beams
        for (int i = 0; i < lightWidth; i += 4) {
            float num = timer + i * 0.6f;
            float beamWidth = 4f + (float)Math.Sin(num * 0.5f + 1.2f) * 4f;
            float beamPosition = (float)Math.Sin((num + i * 32) * 0.1f + Math.Sin(num * 0.05f + i * 0.1f) * 0.25) * (lightWidth / 2f - beamWidth / 2f);
            float beamLength = lightLength + (float)Math.Sin(num * 0.25f) * 8f;
            float beamAlpha = 0.6f + (float)Math.Sin(num + 0.8f) * 0.3f;
            DrawBeam(beamPosition, beamWidth, beamLength, beamAlpha);
        }

        Position = actualPosition;
    }

    private void DrawBeam(float position, float width, float length, float beamAlpha) {
        if (width < 1f)
            return;

        Vector2 beamPosition = Position + Calc.AngleToVector(rotation, 1f) * position;
        float beamRotation = rotation + (float)Math.PI / 2f;
        Color beamColor = ((rainbow && !rainbowSingleColor) ? GetHue(beamPosition) : color) * beamAlpha * alpha;
        beamTexture.Draw(beamPosition, new Vector2(0f, 0.5f), beamColor, new Vector2(1f / beamTexture.Width * length, width), beamRotation);
    }

    private Color GetHue(Vector2 position) {
        // use vanilla/rainbow spinner color controller colors by default
        if (!useCustomRainbowColors)
            return RainbowHelper.GetHue(position);

        // stolen from MaddieHelpingHand's RainbowSpinnerColorController
        // https://github.com/maddie480/MaddieHelpingHand/blob/master/Entities/RainbowSpinnerColorController.cs#L311
        if (rainbowColors.Count == 1)
            return rainbowColors[0];

        float progress = (position - rainbowCenter).Length() + Scene.TimeActive * rainbowGradientSpeed;
        while (progress < 0)
            progress += rainbowGradientSize;
        progress = progress % rainbowGradientSize / rainbowGradientSize;

        if (!rainbowLoopColors)
            progress = Calc.YoYo(progress);

        if (progress == 1f)
            return rainbowColors[rainbowColors.Count - 1];

        float globalProgress = (rainbowColors.Count - 1) * progress;
        int colorIndex = (int)globalProgress;
        float progressInIndex = globalProgress - colorIndex;
        return Color.Lerp(rainbowColors[colorIndex], rainbowColors[colorIndex + 1], progressInIndex);
    }

    private bool InView(Camera camera) => X + cullRectRight > camera.X - VisibilityPadding
                                          && X + cullRectLeft < camera.X + camera.Width + VisibilityPadding
                                          && Y + cullRectBottom > camera.Y - VisibilityPadding
                                          && Y + cullRectTop < camera.Y + camera.Height + VisibilityPadding;
}
