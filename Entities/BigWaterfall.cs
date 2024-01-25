using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using Celeste.Mod.Entities;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/BigWaterfall")]
    public class BigWaterfall : Entity {
        private float width;
        private float height;

        private readonly bool ignoreSolids;
        private readonly bool hasLines;

        private Color baseColor;
        private Color surfaceColor;
        private Color fillColor;

        private Water water;
        private Solid solid;
        private readonly List<float> lines = new List<float>();
        private SoundSource loopingSfx;
        private SoundSource enteringSfx;

        private bool visibleOnCamera;

        public BigWaterfall(EntityData data, Vector2 offset) : base(data.Position + offset) {
            base.Tag = Tags.TransitionUpdate;

            width = data.Width;
            base.Depth = data.Int("depth", -49900);

            ignoreSolids = data.Bool("ignoreSolids", false);
            hasLines = data.Bool("lines", true);

            baseColor = Calc.HexToColor(data.Attr("color", "87CEFA"));
            surfaceColor = baseColor * 0.8f;
            fillColor = baseColor * 0.3f;

            if (hasLines) {
                lines.Add(3f);
                lines.Add(width - 4f);
            }

            if (width > 16f && hasLines) {
                int num = Calc.Random.Next((int)(width / 16f));
                for (int i = 0; i < num; i++) {
                    lines.Add(8f + Calc.Random.NextFloat(width - 16f));
                }
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            Level level = base.Scene as Level;

            height = 8f;
            while (base.Y + height < level.Bounds.Bottom && (water = base.Scene.CollideFirst<Water>(new Rectangle((int)base.X, (int)(base.Y + height), 8, 8))) == null && ((solid = base.Scene.CollideFirst<Solid>(new Rectangle((int)base.X, (int)(base.Y + height), 8, 8))) == null || !solid.BlockWaterfalls || ignoreSolids)) {
                height += 8f;
                solid = null;
            }

            Add(loopingSfx = new SoundSource());
            loopingSfx.Play(width <= 24 ? "event:/env/local/waterfall_small_main" : "event:/env/local/waterfall_big_main");
            Add(enteringSfx = new SoundSource());
            enteringSfx.Play((water != null && !base.Scene.CollideCheck<Solid>(new Rectangle((int)base.X, (int)(base.Y + height), 8, 16))) ? "event:/env/local/waterfall_small_in_deep" : "event:/env/local/waterfall_small_in_shallow");
            enteringSfx.Position.Y = height;

            Add(new DisplacementRenderHook(RenderDisplacement));
        }

        public void RenderDisplacement() {
            Draw.Rect(base.X, base.Y, width, height, new Color(0.5f, 0.5f, 1f, 1f));
        }

        public override void Update() {
            visibleOnCamera = InView((base.Scene as Level).Camera);

            if (loopingSfx != null) {
                Vector2 position = (base.Scene as Level).Camera.Position;
                loopingSfx.Position.Y = Calc.Clamp(position.Y + 90f, base.Y, height);
            }

            if (visibleOnCamera) {
                if (water != null && water.Active && water.TopSurface != null && base.Scene.OnInterval(0.3f)) {
                    water.TopSurface.DoRipple(new Vector2(base.X + (width / 2f), water.Y), 0.75f);
                    if (width >= 32) {
                        water.TopSurface.DoRipple(new Vector2(base.X + 8f, water.Y), 0.75f);
                        water.TopSurface.DoRipple(new Vector2(base.X + width - 8f, water.Y), 0.75f);
                    }
                }

                if (water != null || solid != null) {
                    Vector2 position2 = new Vector2(base.X + (width / 2f), base.Y + height + 2f);
                    (base.Scene as Level).ParticlesFG.Emit(Water.P_Splash, 1, position2, new Vector2((width / 2f) + 4f, 2f), baseColor, new Vector2(0f, -1f).Angle());
                }
            }

            base.Update();
        }

        public override void Render() {
            if (!visibleOnCamera) return;

            if (water == null || water.TopSurface == null) {
                Draw.Rect(base.X, base.Y, width, height, fillColor);

                Draw.Rect(base.X - 1f, base.Y, 3f, height, surfaceColor);
                Draw.Rect(base.X + width - 2f, base.Y, 3f, height, surfaceColor);
                if (hasLines) {
                    foreach (float line in lines) {
                        Draw.Rect(base.X + line, base.Y, 1f, height, surfaceColor);
                    }
                }

                return;
            }

            Water.Surface topSurface = water.TopSurface;
            float num = height + water.TopSurface.Position.Y - water.Y;
            for (int i = 0; i <= width; i++) {
                Draw.Rect(base.X + i, base.Y, 1f, num - topSurface.GetSurfaceHeight(new Vector2(base.X + 1f + i, water.Y)), fillColor);
            }

            Draw.Rect(base.X - 1f, base.Y, 3f, num - topSurface.GetSurfaceHeight(new Vector2(base.X, water.Y)), surfaceColor);
            Draw.Rect(base.X + width - 2f, base.Y, 3f, num - topSurface.GetSurfaceHeight(new Vector2(base.X + width - 1f, water.Y)), surfaceColor);
            if (hasLines) {
                foreach (float line in lines) {
                    Draw.Rect(base.X + line, base.Y, 1f, num - topSurface.GetSurfaceHeight(new Vector2(base.X + line + 1f, water.Y)), surfaceColor);
                }
            }
        }

        private bool InView(Camera camera) =>
            base.X < camera.Right + 24f && base.X + width > camera.Left - 24f && base.Y < camera.Bottom + 24f && base.Y + height > camera.Top - 24f;
    }
}
