namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/CustomLightbeam")]
public class CustomLightBeam : Entity
{
    private const int BaseGradientResolution = 4;
    private const int VisibilityPadding = 16;

    private readonly int lightWidth;
    private readonly int lightLength;
    
    private readonly FloatSource rotationSource;

    private float rotation;
    private Color color;

    private readonly Condition condition;
    private readonly float flagFadeTime;
    private readonly bool fadeWhenNear;
    private readonly bool fadeOnTransition;
    private readonly bool noParticles;

    private readonly float scroll;
    private readonly Vector2 scrollAnchor;

    private readonly MTexture beamTexture;

    private readonly ColorSource colorSource;

    private readonly bool rainbow;
    private readonly bool rainbowSingleColor;

    private readonly bool useCustomRainbowColors;
    private readonly Color[] rainbowColors;
    private readonly float rainbowGradientSize;
    private readonly float rainbowGradientSpeed;
    private readonly bool rainbowLoopColors;
    private readonly Vector2 rainbowCenter;

    private readonly float baseAlpha;
    private readonly bool additive;

    private float distanceAlpha = 1f;
    private float flagAlpha = 1f;
    private float alpha = 1f;

    private float timer = Calc.Random.NextFloat(1000f);
    private readonly float particleTimerOffset = Calc.Random.NextFloat();

    private float boundsTop, boundsBottom, boundsLeft, boundsRight;

    private Vector2 RenderPosition
    {
        get
        {
            if (scroll == 1f)
                return Position;

            Vector2 cameraCenter = SceneAs<Level>().Camera.GetCenter();
            return cameraCenter + (Position - scrollAnchor * (1f - scroll) - cameraCenter * scroll);
        }
    }

    public CustomLightBeam(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Tag = Tags.TransitionUpdate;

        lightWidth = data.Width;
        lightLength = data.Height;
        rotationSource = data.FloatSource("rotation", 0f);
        Depth = data.Int("depth", -9998);
        condition = data.Condition("flag", data.Bool("inverted"));
        flagFadeTime = Math.Max(data.Float("flagFadeTime", 0.25f), 0f);
        fadeWhenNear = data.Bool("fadeWhenNear", true);
        fadeOnTransition = data.Bool("fadeOnTransition", true);
        noParticles = data.Bool("noParticles", false);
        scroll = data.Float("scroll", 1f);
        scrollAnchor = data.Nodes?.Length >= 1 ? data.Nodes[0] + offset : Position;
        beamTexture = GFX.Game[data.Attr("texture", "util/lightbeam")];
        baseAlpha = data.Float("alpha", 1f);
        additive = data.Bool("additive", false);
        colorSource = data.ColorSource("color", "ccffff");

        rainbow = data.Bool("rainbow", false);
        useCustomRainbowColors = data.Bool("useCustomRainbowColors", false);
        if (rainbow && useCustomRainbowColors)
        {
            rainbowGradientSize = data.Float("gradientSize", 280f);
            rainbowGradientSpeed = data.Float("gradientSpeed", 50f);
            rainbowLoopColors = data.Bool("loopColors", false);
            rainbowCenter = new Vector2(data.Float("centerX", 0f), data.Float("centerY", 0f));
            rainbowSingleColor = data.Bool("singleColor", false);

            rainbowColors = data.Attr("colors", "89e5ae,88e0e0,87a9dd,9887db,d088e2")
                                .Split(',')
                                .Select(Calc.HexToColorWithNonPremultipliedAlpha)
                                .ToArray();
        }
    }

    private void UpdateCullBounds()
    {
        Vector2 baseCornerA = -Calc.AngleToVector(rotation, 1f) * (lightWidth / 2f);
        Vector2 baseCornerB = Calc.AngleToVector(rotation, 1f) * (lightWidth / 2f);
        Vector2 edgeCornerA = baseCornerA + Calc.AngleToVector(rotation + (float)Math.PI / 2f, 1f) * lightLength;
        Vector2 edgeCornerB = baseCornerB + Calc.AngleToVector(rotation + (float)Math.PI / 2f, 1f) * lightLength;
        boundsTop = Math.Min(Math.Min(baseCornerA.Y, baseCornerB.Y), Math.Min(edgeCornerA.Y, edgeCornerB.Y));
        boundsBottom = Math.Max(Math.Max(baseCornerA.Y, baseCornerB.Y), Math.Max(edgeCornerA.Y, edgeCornerB.Y));
        boundsLeft = Math.Min(Math.Min(baseCornerA.X, baseCornerB.X), Math.Min(edgeCornerA.X, edgeCornerB.X));
        boundsRight = Math.Max(Math.Max(baseCornerA.X, baseCornerB.X), Math.Max(edgeCornerA.X, edgeCornerB.X));
    }
    
    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        Level level = SceneAs<Level>();

        rotation = rotationSource.GetValue(level.Session) * Calc.DegToRad;
        UpdateCullBounds();

        if (level.Transitioning && fadeOnTransition)
            distanceAlpha = 0f;

        flagAlpha = condition.Check(level.Session) ? 1f : 0f;
        alpha = distanceAlpha * flagAlpha;
    }

    public override void Update()
    {
        base.Update();

        using var applyScroll = new SetTemporaryValue<Vector2>(ref Position, RenderPosition);

        Level level = SceneAs<Level>();

        timer += Engine.DeltaTime;

        float prevRotation = rotation;
        rotation = rotationSource.GetValue(level.Session) * Calc.DegToRad;
        if (rotation != prevRotation)
            UpdateCullBounds();

        if (!rainbow)
            color = colorSource.GetValue(level.Session);
        else if (rainbowSingleColor)
            color = GetRainbowHue(Position);

        Visible = InView(level.Camera);

        if (Scene.Tracker.GetEntity<Player>() is { } player)
        {
            float targetAlpha = 1f;
            if (fadeOnTransition && level.Transitioning)
                targetAlpha = 0f;
            else if (fadeWhenNear)
            {
                Vector2 direction = Calc.AngleToVector(rotation + MathF.PI / 2f, 1f);
                Vector2 playerDistancePoint = Calc.ClosestPointOnLine(Position, Position + direction * 10000f, player.Center);

                if ((playerDistancePoint - player.Center).Length() <= lightWidth / 2f)
                    targetAlpha = Math.Min(1f, Math.Max(0f, (playerDistancePoint - Position).Length() - 8f) / lightLength);
            }

            distanceAlpha = Calc.Approach(distanceAlpha, targetAlpha, Engine.DeltaTime * 4f);
        }
        flagAlpha = Calc.Approach(flagAlpha, condition.Check(level.Session) ? 1f : 0f, Engine.DeltaTime / flagFadeTime);
        alpha = distanceAlpha * flagAlpha;

        if (!noParticles && Visible && alpha >= 0.5f && level.OnInterval(0.8f, particleTimerOffset))
        {
            Vector2 direction = Calc.AngleToVector(rotation + MathF.PI / 2f, 1f);
            Vector2 particlePos = Position + -4f * direction
                                           + (Calc.Random.Next(lightWidth - 4) + 2 - lightWidth / 2f) * direction.Perpendicular();
            Color particleColor = (!rainbow || rainbowSingleColor ? color : GetRainbowHue(particlePos)) * baseAlpha;
            if (additive)
                particleColor.A = 0;

            level.Particles.Emit(LightBeam.P_Glow, particlePos, particleColor, rotation + MathF.PI / 2f);
        }
    }

    public override void Render()
    {
        base.Render();

        if (alpha <= 0f)
            return;

        using var applyScroll = new SetTemporaryValue<Vector2>(ref Position, RenderPosition);

        float baseLength = lightLength - 4 + MathF.Sin(timer * 2f) * 4f;
        if (!rainbow || rainbowSingleColor)
            DrawBeam(0f, lightWidth, baseLength, 0.4f);
        else
        {
            for (int i = 0; i < lightWidth; i += BaseGradientResolution)
                DrawBeam(i - lightWidth / 2f, BaseGradientResolution, baseLength, 0.4f);
        }

        for (int i = 0; i < lightWidth; i += 4)
        {
            float num = timer + i * 0.6f;
            float beamWidth = 4f + MathF.Sin(num * 0.5f + 1.2f) * 4f;
            float beamPosition = MathF.Sin((num + i * 32) * 0.1f + MathF.Sin(num * 0.05f + i * 0.1f) * 0.25f) * (lightWidth / 2f - beamWidth / 2f);
            float beamLength = lightLength + MathF.Sin(num * 0.25f) * 8f;
            float beamAlpha = 0.6f + MathF.Sin(num + 0.8f) * 0.3f;

            DrawBeam(beamPosition, beamWidth, beamLength, beamAlpha);
        }
    }

    private void DrawBeam(float position, float width, float length, float beamAlpha)
    {
        if (width < 1f)
            return;

        Vector2 beamPosition = Position + Calc.AngleToVector(rotation, 1f) * position;
        float beamRotation = rotation + MathF.PI / 2f;
        Color beamColor = (!rainbow || rainbowSingleColor ? color : GetRainbowHue(beamPosition)) * (baseAlpha * alpha * beamAlpha);
        if (additive)
            beamColor.A = 0;

        beamTexture.Draw(beamPosition, new Vector2(0f, 0.5f), beamColor, new Vector2(1f / beamTexture.Width * length, width), beamRotation);
    }

    private Color GetRainbowHue(Vector2 position)
    {
        if (!useCustomRainbowColors)
            return RainbowHelper.GetHue(Scene, position);

        // stolen + modified from MaddieHelpingHand's RainbowSpinnerColorController
        // https://github.com/maddie480/MaddieHelpingHand/blob/master/Entities/RainbowSpinnerColorController.cs#L311
        if (rainbowColors.Length == 1)
            return rainbowColors[0];

        float progress = (position - rainbowCenter).Length() + Scene.TimeActive * rainbowGradientSpeed;
        while (progress < 0)
            progress += rainbowGradientSize;
        progress = progress % rainbowGradientSize / rainbowGradientSize;

        // hmm kind of dont like thiss but i also kind of dont like needing to copy the first color to the end for loopcolors
        float progressInColors = !rainbowLoopColors
            ? (rainbowColors.Length - 1) * Calc.YoYo(progress)
            : rainbowColors.Length * progress;
        int colorIndex = (int)progressInColors;
        int nextColorIndex = (colorIndex + 1);
        float progressInIndex = progressInColors - colorIndex;
        return Color.Lerp(rainbowColors[colorIndex % rainbowColors.Length], rainbowColors[nextColorIndex % rainbowColors.Length], progressInIndex);
    }

    private bool InView(Camera camera) => X + boundsRight > camera.X - VisibilityPadding
                                          && X + boundsLeft < camera.X + camera.Width + VisibilityPadding
                                          && Y + boundsBottom > camera.Y - VisibilityPadding
                                          && Y + boundsTop < camera.Y + camera.Height + VisibilityPadding;
}
