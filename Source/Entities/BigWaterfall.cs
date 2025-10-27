using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using MonoMod.Utils;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/BigWaterfall")]
public class BigWaterfall : Entity {
    private enum SplashParticleDepths {
        ParticlesBG, Particles, ParticlesFG, None
    }

    private float width;
    private float height;

    private readonly bool ignoreSolids;
    private readonly bool hasLines;
    private readonly float wavePercent;

    private readonly Color baseColor;
    private readonly Color surfaceColor;
    private readonly Color fillColor;
    private readonly SplashParticleDepths splashParticleDepth;

    private Water water;
    private Solid solid;
    private readonly List<float> lines = [];
    private SoundSource loopingSfx;
    private SoundSource enteringSfx;

    private bool visibleOnCamera;

    public BigWaterfall(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Tag = Tags.TransitionUpdate;

        width = data.Width;

        float alpha = data.Float("alpha", 1f);
        baseColor = Calc.HexToColor(data.Attr("color", "87CEFA")) * alpha;
        surfaceColor = baseColor * 0.8f;
        fillColor = baseColor * 0.3f;

        Depth = data.Int("depth", -49900);
        splashParticleDepth = data.Enum("splashParticleDepth", SplashParticleDepths.ParticlesFG);

        ignoreSolids = data.Bool("ignoreSolids", false);
        hasLines = data.Bool("lines", true);
        wavePercent = data.Float("wavePercent", 1f);

        if (hasLines) {
            if (width <= 8f) {
                lines.Add(2f);
                lines.Add(width - 3f);
            } else {
                lines.Add(3f);
                lines.Add(width - 4f);
            }
        }

        if (width > 16f && hasLines) {
            int lineCount = Calc.Random.Next((int)(width / 16f));

            for (int i = 0; i < lineCount; i++)
                lines.Add(8f + Calc.Random.NextFloat(width - 16f));
        }
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        Level level = Scene as Level;

        height = 8f;
        while (Y + height < level.Bounds.Bottom && (water = Scene.CollideFirst<Water>(new Rectangle((int)X, (int)(Y + height), 8, 8))) == null && ((solid = Scene.CollideFirst<Solid>(new Rectangle((int)X, (int)(Y + height), 8, 8))) == null || !solid.BlockWaterfalls || ignoreSolids)) {
            height += 8f;
            solid = null;
        }

        Add(loopingSfx = new SoundSource());
        loopingSfx.Play(width <= 24 ? "event:/env/local/waterfall_small_main" : "event:/env/local/waterfall_big_main");
        loopingSfx.Position.X = width / 2f;
        Add(enteringSfx = new SoundSource());
        enteringSfx.Play((water != null && !Scene.CollideCheck<Solid>(new Rectangle((int)X, (int)(Y + height), 8, 16))) ? "event:/env/local/waterfall_small_in_deep" : "event:/env/local/waterfall_small_in_shallow");
        enteringSfx.Position.X = width / 2f;
        enteringSfx.Position.Y = height;

        Add(new DisplacementRenderHook(RenderDisplacement));
    }

    public void RenderDisplacement() {
        if (!visibleOnCamera)
            return;

        Color waveColor = new Color(0.5f, 0.5f, wavePercent, 1f);

        if (water is not { TopSurface: not null }) {
            Draw.Rect(X, Y, width, height, waveColor);
            return;
        }

        Water.Surface waterSurface = water.TopSurface;
        float heightWithWater = height + water.TopSurface.Position.Y - water.Y;
        for (int i = 0; i < width; i++) {
            Draw.Rect(X + i, Y, 1f, heightWithWater - waterSurface.GetSurfaceHeight(new Vector2(X + 1f + i, water.Y)), waveColor);
        }
    }

    public override void Update() {
        Level level = Scene as Level;

        visibleOnCamera = InView(level.Camera);

        if (loopingSfx is not null) {
            Vector2 cameraPos = level.Camera.GetCenter();
            loopingSfx.Position.Y = Calc.Clamp(cameraPos.Y, Y, height);
        }

        if (visibleOnCamera) {
            if (water is { Active: true, TopSurface: not null } && Scene.OnInterval(0.3f)) {
                water.TopSurface.DoRipple(new Vector2(X + (width / 2f), water.Y), 0.75f);
                if (width >= 32) {
                    water.TopSurface.DoRipple(new Vector2(X + 8f, water.Y), 0.75f);
                    water.TopSurface.DoRipple(new Vector2(X + width - 8f, water.Y), 0.75f);
                }
            }

            if (splashParticleDepth != SplashParticleDepths.None && (water is not null || solid is not null) && !level.Transitioning) {
                Vector2 particlesPosition = new Vector2(X + (width / 2f), Y + height + 2f);

                ParticleSystem particles = splashParticleDepth switch {
                    SplashParticleDepths.ParticlesFG => level.ParticlesFG,
                    SplashParticleDepths.Particles   => level.Particles,
                    SplashParticleDepths.ParticlesBG => level.ParticlesBG,
                    _ => throw new ArgumentOutOfRangeException()
                };

                particles.Emit(Water.P_Splash, 1, particlesPosition, new Vector2((width / 2f) + 4f, 2f), baseColor, new Vector2(0f, -1f).Angle());
            }
        }

        base.Update();
    }

    public override void Render() {
        if (!visibleOnCamera)
            return;

        int edgeSize = width <= 8f ? 2 : 3;
        int innerEdgeSize = edgeSize - 1;
        int fillShrink = width <= 8f ? 1 : 0;

        if (water == null || water.TopSurface == null) {
            Draw.Rect(X + fillShrink, Y, width - fillShrink, height, fillColor);

            Draw.Rect(X - 1f, Y, edgeSize, height, surfaceColor);
            Draw.Rect(X + width - innerEdgeSize, Y, edgeSize, height, surfaceColor);
            if (hasLines) {
                foreach (float line in lines) {
                    Draw.Rect(X + line, Y, 1f, height, surfaceColor);
                }
            }

            return;
        }

        Water.Surface waterSurface = water.TopSurface;
        float heightWithWater = height + water.TopSurface.Position.Y - water.Y;
        for (int i = fillShrink; i < width - fillShrink; i++) {
            Draw.Rect(X + i, Y, 1f, heightWithWater - waterSurface.GetSurfaceHeight(new Vector2(X + 1f + i, water.Y)), fillColor);
        }

        Draw.Rect(X - 1f, Y, edgeSize, heightWithWater - waterSurface.GetSurfaceHeight(new Vector2(X, water.Y)), surfaceColor);
        Draw.Rect(X + width - innerEdgeSize, Y, edgeSize, heightWithWater - waterSurface.GetSurfaceHeight(new Vector2(X + width - 1f, water.Y)), surfaceColor);

        if (hasLines) {
            foreach (float line in lines) {
                Draw.Rect(X + line, Y, 1f, heightWithWater - waterSurface.GetSurfaceHeight(new Vector2(X + line + 1f, water.Y)), surfaceColor);
            }
        }
    }

    private bool InView(Camera camera) =>
        X < camera.Right + 24f && X + width > camera.Left - 24f && Y < camera.Bottom + 24f && Y + height > camera.Top - 24f;
}
