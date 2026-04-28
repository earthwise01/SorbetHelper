namespace Celeste.Mod.SorbetHelper.Entities;

// grr evil backwards compat
[CustomEntity("SorbetHelper/BigWaterfall")]
public class ResizableWaterfall : Entity
{
    private enum SplashParticleDepths
    {
        ParticlesBG, Particles, ParticlesFG, None
    }

    private float width;
    private float height;

    private readonly bool ignoreSolids;
    private readonly bool ignoreWater;
    private readonly float wavePercent;
    private readonly bool rippleWater;

    private readonly Color baseColor;
    private readonly Color surfaceColor;
    private readonly Color fillColor;
    private readonly SplashParticleDepths splashParticleDepth;

    private Water water, topWater;
    private Solid solid;
    private readonly List<float> lines = [];
    private SoundSource loopingSfx;
    private SoundSource enteringSfx;

    private bool visibleOnCamera;

    public ResizableWaterfall(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Tag = Tags.TransitionUpdate;

        width = data.Width;

        float alpha = data.Float("alpha", 1f);
        baseColor = Calc.HexToColor(data.Attr("color", "87CEFA")) * alpha;
        surfaceColor = baseColor * 0.8f;
        fillColor = baseColor * 0.3f;

        Depth = data.Int("depth", -49900);
        splashParticleDepth = data.Enum("splashParticleDepth", SplashParticleDepths.ParticlesFG);

        ignoreSolids = data.Bool("ignoreSolids", false);
        ignoreWater = data.Bool("ignoreWater", false);
        wavePercent = data.Float("wavePercent", 1f);
        rippleWater = data.Bool("rippleWater", true);

        bool hasLines = data.Bool("lines", true);
        if (!hasLines)
            return;

        int edgeSize = width <= 8f ? 2 : 3;
        lines.Add(edgeSize);
        lines.Add(width - 1f - edgeSize);

        if (width > 16f)
        {
            int lineCount = Calc.Random.Next((int)(width / 16f));
            for (int i = 0; i < lineCount; i++)
                lines.Add(8f + Calc.Random.NextFloat(width - 16f));
        }
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        Level level = SceneAs<Level>();

        height = 8f;
        while (Y + height < level.Bounds.Bottom
               && (ignoreWater || (water = Scene.CollideFirst<Water>(new Rectangle((int)X, (int)(Y + height), 8, 8))) is null)
               && (ignoreSolids || (solid = Scene.CollideFirst<Solid>(new Rectangle((int)X, (int)(Y + height), 8, 8))) is null || !solid.BlockWaterfalls))
        {
            height += 8f;
            solid = null;
        }

        if (water is not null)
            height = Math.Max(water.Top - Y, 0f);

        if (!ignoreWater && Scene.CollideFirst<Water>(new Rectangle((int)X, (int)(Y - 1), 8, 1)) is { BottomSurface: not null } waterAbove)
            topWater = waterAbove;

        Add(loopingSfx = new SoundSource());
        loopingSfx.Play(width <= 24 ? "event:/env/local/waterfall_small_main" : "event:/env/local/waterfall_big_main");
        loopingSfx.Position.X = width / 2f;
        Add(enteringSfx = new SoundSource());
        enteringSfx.Play((water is not null && !Scene.CollideCheck<Solid>(new Rectangle((int)X, (int)(Y + height), 8, 16))) ? "event:/env/local/waterfall_small_in_deep" : "event:/env/local/waterfall_small_in_shallow");
        enteringSfx.Position.X = width / 2f;
        enteringSfx.Position.Y = height;

        Add(new DisplacementRenderHook(RenderDisplacement));
    }

    public override void Update()
    {
        base.Update();

        Level level = SceneAs<Level>();

        loopingSfx?.Position.Y = Calc.Clamp(level.Camera.GetCenter().Y, Y, height);

        Visible = visibleOnCamera = InView(level.Camera);
        if (!Visible)
            return;

        Water.Surface bottomWaterSurface = water is { Active: true } ? water.TopSurface : null;
        Water.Surface topWaterSurface = topWater is { Active: true } ? topWater.BottomSurface : null;
        if (rippleWater && Scene.OnInterval(0.3f) && (bottomWaterSurface is not null || topWaterSurface is not null))
        {
            int rippleCount = (int)MathF.Ceiling(width / 40f);
            float rippleDistance = width / rippleCount;
            float rippleStrengthMult = MathF.Pow(0.85f, MathF.Max(width / 40f - 1f, 0f)); // hmm
            for (int i = 0; i < rippleCount; i++)
            {
                float x = rippleDistance / 2f + rippleDistance * i;
                bottomWaterSurface?.DoRipple(new Vector2(X + x, water.Top), 0.75f * rippleStrengthMult);
                topWaterSurface?.DoRipple(new Vector2(X + x, topWater.Bottom), 0.5f * rippleStrengthMult);
            }
        }

        if (splashParticleDepth != SplashParticleDepths.None
            && !level.Transitioning
            && (solid is not null || water is not null || topWater is not null))
        {
            ParticleSystem particles = splashParticleDepth switch
            {
                SplashParticleDepths.ParticlesBG => level.ParticlesBG,
                SplashParticleDepths.Particles   => level.Particles,
                SplashParticleDepths.ParticlesFG => level.ParticlesFG,
                _                                => throw new ArgumentOutOfRangeException()
            };

            if (level.OnInterval(1f / 60f) && (solid is not null || water is not null))
                particles.Emit(Water.P_Splash, 1, new Vector2(X + width / 2f, Y + height + 2f), new Vector2(width / 2f + 4f, 2f), baseColor, -MathF.PI / 2f);

            if (level.OnInterval(0.15f) && topWater is not null)
                particles.Emit(Water.P_Splash, 1, new Vector2(X + width / 2f, Y), new Vector2(width / 2f + 4f, 2f), baseColor, Calc.Random.Range(0, MathF.PI));

        }
    }

    public void RenderDisplacement()
    {
        if (!visibleOnCamera)
            return;

        Color waveColor = new Color(0.5f, 0.5f, wavePercent, 1f);

        Water.Surface bottomWaterSurface = water?.TopSurface;
        Water.Surface topWaterSurface = topWater?.BottomSurface;
        if (bottomWaterSurface is null && topWaterSurface is null)
        {
            Draw.Rect(X, Y, width, height, waveColor);
            return;
        }

        float renderY = Y;
        float renderHeight = height;

        if (bottomWaterSurface is not null)
            renderHeight += bottomWaterSurface.Position.Y - water.Top;
        if (topWaterSurface is not null)
        {
            renderY += topWaterSurface.Position.Y - topWater.Bottom;
            renderHeight -= topWaterSurface.Position.Y - topWater.Bottom;
        }

        for (int i = 0; i < width; i++)
        {
            float sliceX = X + i;
            float bottomSurfaceHeight = bottomWaterSurface is not null ? MathF.Round(bottomWaterSurface.GetSurfaceHeight(new Vector2(sliceX, water.Top))) : 0f;
            float topSurfaceHeight = topWaterSurface is not null ? MathF.Round(topWaterSurface.GetSurfaceHeight(new Vector2(sliceX, topWater.Bottom))) : 0f;
            Draw.Rect(sliceX, renderY + topSurfaceHeight, 1f, renderHeight - bottomSurfaceHeight - topSurfaceHeight, waveColor);
        }
    }

    public override void Render()
    {
        if (!visibleOnCamera)
            return;

        int reduceEdges = width <= 8f ? 1 : 0;
        int edgeSize = 3 - reduceEdges;

        Water.Surface bottomWaterSurface = water?.TopSurface;
        Water.Surface topWaterSurface = topWater?.BottomSurface;
        if (bottomWaterSurface is null && topWaterSurface is null)
        {
            Draw.Rect(X + reduceEdges, Y, width - reduceEdges, height, fillColor);
            Draw.Rect(X - 1f, Y, edgeSize, height, surfaceColor);
            Draw.Rect(X + width - (edgeSize - 1), Y, edgeSize, height, surfaceColor);

            foreach (float line in lines)
                Draw.Rect(X + line, Y, 1f, height, surfaceColor);

            return;
        }

        float renderY = Y;
        float renderHeight = height;

        if (bottomWaterSurface is not null)
            renderHeight += bottomWaterSurface.Position.Y - water.Top;
        if (topWaterSurface is not null)
        {
            renderY += topWaterSurface.Position.Y - topWater.Bottom;
            renderHeight -= topWaterSurface.Position.Y - topWater.Bottom;
        }

        for (int i = reduceEdges; i < width - reduceEdges; i++)
            DrawWaterfallSlice(X + i, fillColor);

        for (int i = 0; i < edgeSize; i++)
        {
            DrawWaterfallSlice(X - 1f + i, surfaceColor);
            DrawWaterfallSlice(X + width - i, surfaceColor);
        }

        foreach (float line in lines)
            DrawWaterfallSlice(X + line, surfaceColor);

        return;

        void DrawWaterfallSlice(float x, Color color)
        {
            float bottomSurfaceHeight = bottomWaterSurface is not null ? MathF.Round(bottomWaterSurface.GetSurfaceHeight(new Vector2(x, water!.Top))) : 0f;
            float topSurfaceHeight = topWaterSurface is not null ? MathF.Round(topWaterSurface.GetSurfaceHeight(new Vector2(x, topWater!.Bottom))) : 0f;
            Draw.Rect(x, renderY + topSurfaceHeight, 1f, renderHeight - bottomSurfaceHeight - topSurfaceHeight, color);
        }
    }

    private bool InView(Camera camera) => X < camera.Right + 24f && X + width > camera.Left - 24f
                                          && Y < camera.Bottom + 24f && Y + height > camera.Top - 24f;
}
