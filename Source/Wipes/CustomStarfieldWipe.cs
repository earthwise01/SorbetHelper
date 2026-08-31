namespace Celeste.Mod.SorbetHelper.Wipes;

[CustomWipe("SorbetHelper/CustomStarfieldWipe = Load", "SorbetHelper/FourPointStarfieldWipe = LoadFourPoint")]
public class CustomStarfieldWipe : ScreenWipe
{
    private const string LogID = $"{nameof(SorbetHelper)}/{nameof(CustomStarfieldWipe)}";

    private struct Star(float scale)
    {
        public readonly float Scale = scale;
        public readonly float Speed = (0.5f + (1f - scale) * 0.5f) * Celeste.TargetWidth * 0.05f;
        public readonly float SineDistance = scale * Celeste.TargetHeight * 0.05f;

        public float X = Calc.Random.Range(0, HorizontalRange);
        public float Y = Celeste.TargetHeight * (0.5f + Calc.Random.Choose(-1, 1) * (1f - scale) * Calc.Random.Range(0.25f, 0.5f));
        public float Sine = Calc.Random.NextFloat(MathF.PI * 2f);
        public float Rotation = MathF.PI / 4f - MathF.PI / 12f + Calc.Random.NextFloat(MathF.PI / 6f);

        public void Update()
        {
            X += Speed * Engine.DeltaTime;
            Sine += (1f - Scale) * 8f * Engine.DeltaTime;
            Rotation += (1f - Scale) * 0.25f * Engine.DeltaTime;
        }
    }

    private const int HorizontalPadding = 500;
    private const int HorizontalRange = Celeste.TargetWidth + HorizontalPadding * 2;
    
    private const int StarCount = 64;

    public const int DefaultPoints = 5;
    public const float DefaultInnerRadius = 1f, DefaultOuterRadius = 2f;
    public static readonly SorbetHelperMetadata.CustomStarfieldWipeSettingsData DefaultSettings = new()
    {
        StarPoints = DefaultPoints,
        StarInnerRadius = DefaultInnerRadius, StarOuterRadius = DefaultOuterRadius
    };

    public static readonly SorbetHelperMetadata.CustomStarfieldWipeSettingsData FourPointSettings = new()
    {
        StarPoints = 4,
        StarInnerRadius = 1f, StarOuterRadius = 2.5f
    };

    private readonly SorbetHelperMetadata.CustomStarfieldWipeSettingsData settings;

    private readonly Vector2[] starShape;
    private readonly int starVertexCount;
    private readonly Star[] stars;
    private readonly VertexPositionColor[] vertices;

    private bool hasDrawn;

    private static SorbetHelperMetadata.CustomStarfieldWipeSettingsData GetSettingsFromMetadata(Scene scene)
    {
        AreaKey? areaKey = scene switch
        {
            Level level                                  => level.Session.Area,
            Overworld { Current: OuiChapterPanel panel } => panel.Area,
            Overworld { Current: OuiFileSelect }         => SaveData.Instance.LastArea_Safe,
            _                                            => null
        };

        if (areaKey is not { } key)
        {
            Logger.Warn(LogID, "Couldn't find current AreaKey to get custom starfield settings from metadata with!");
            return DefaultSettings;
        }

        if (!SorbetHelperMetadata.TryGetMetadata(key, out SorbetHelperMetadata metadata))
            return DefaultSettings;

        return metadata.CustomStarfieldWipeSettings ?? DefaultSettings;
    }

    public static ScreenWipe Load(Scene scene, bool wipeIn, Action onComplete)
        => new CustomStarfieldWipe(scene, wipeIn, onComplete, GetSettingsFromMetadata(scene));

    public static ScreenWipe LoadFourPoint(Scene scene, bool wipeIn, Action onComplete)
        => new CustomStarfieldWipe(scene, wipeIn, onComplete, FourPointSettings);

    public CustomStarfieldWipe(Scene scene, bool wipeIn, Action onComplete = null, SorbetHelperMetadata.CustomStarfieldWipeSettingsData settings = null)
        : base(scene, wipeIn, onComplete)
    {
        this.settings = settings ?? DefaultSettings;

        starShape = new Vector2[this.settings.StarPoints];
        starVertexCount = (this.settings.StarPoints - 2) * 3 + this.settings.StarPoints * 3;
        stars = new Star[StarCount];
        vertices = new VertexPositionColor[starVertexCount * StarCount];

        for (int i = 0; i < starShape.Length; i++)
            starShape[i] = Calc.AngleToVector(i / (float)starShape.Length * (MathF.PI * 2f), this.settings.StarInnerRadius);

        for (int i = 0; i < stars.Length; i++)
            stars[i] = new Star(MathF.Pow(i / (float)stars.Length, 5f));

        for (int i = 0; i < vertices.Length; i++)
            vertices[i].Color = WipeIn ? Color.Black : Color.White;
    }

    public override void Update(Scene scene)
    {
        base.Update(scene);

        for (int i = 0; i < stars.Length; i++)
            stars[i].Update();
    }

    public override void BeforeRender(Scene scene)
    {
        hasDrawn = true;

        Engine.Graphics.GraphicsDevice.SetRenderTarget(Celeste.WipeTarget);
        Engine.Graphics.GraphicsDevice.Clear(WipeIn ? Color.White : Color.Black);

        if (Percent > 0.8f)
        {
            float rectHeight = Calc.Map(Percent, 0.8f, 1f) * (Celeste.TargetHeight + 2f);
            Draw.SpriteBatch.Begin();
            Draw.Rect(-1f, (Celeste.TargetHeight - rectHeight) * 0.5f, Celeste.TargetWidth + 2f, rectHeight, WipeIn ? Color.Black : Color.White);
            Draw.SpriteBatch.End();
        }

        int index = 0;
        for (int i = 0; i < stars.Length; i++)
        {
            float starX = -HorizontalPadding + stars[i].X % HorizontalRange;
            float starY = stars[i].Y + MathF.Sin(stars[i].Sine) * stars[i].SineDistance;
            float starScale = (0.1f + stars[i].Scale * 0.9f) * Celeste.TargetHeight * 0.8f * Ease.CubeIn(Percent);
            DrawStar(ref index, Matrix.CreateRotationZ(stars[i].Rotation) * Matrix.CreateScale(starScale) * Matrix.CreateTranslation(starX, starY, 0f));
        }

        GFX.DrawVertices(Matrix.Identity, vertices, vertices.Length);
    }

    private void DrawStar(ref int index, Matrix matrix)
    {
        int startIndex = index;

        for (int i = 1; i < starShape.Length - 1; i++)
        {
            vertices[index++].Position = new Vector3(starShape[0], 0f);
            vertices[index++].Position = new Vector3(starShape[i], 0f);
            vertices[index++].Position = new Vector3(starShape[i + 1], 0f);
        }

        for (int i = 0; i < starShape.Length; i++)
        {
            Vector2 pointStart = starShape[i];
            Vector2 pointEnd = starShape[(i + 1) % starShape.Length];
            Vector2 pointTip = (pointStart + pointEnd) * 0.5f + (pointStart - pointEnd).SafeNormalize(settings.StarOuterRadius - settings.StarInnerRadius).TurnRight();
            vertices[index++].Position = new Vector3(pointStart, 0f);
            vertices[index++].Position = new Vector3(pointTip, 0f);
            vertices[index++].Position = new Vector3(pointEnd, 0f);
        }

        for (int i = startIndex; i < startIndex + starVertexCount; i++)
            vertices[i].Position = Vector3.Transform(vertices[i].Position, matrix);
    }

    public override void Render(Scene scene)
    {
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, StarfieldWipe.SubtractBlendmode, SamplerState.LinearClamp, null, null, null, Engine.ScreenMatrix);

        if (WipeIn ? Percent <= 0.01f : Percent >= 0.99f)
            Draw.Rect(-1f, -1f, Celeste.TargetWidth + 2f, Celeste.TargetHeight + 2f, Color.White);
        else if (hasDrawn)
            Draw.SpriteBatch.Draw(Celeste.WipeTarget, new Vector2(-1f, -1f), Color.White);

        Draw.SpriteBatch.End();
    }
}
