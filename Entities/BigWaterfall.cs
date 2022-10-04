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

        /*private enum Layers {
			FG,
			BG
		}*/

        private float width;

        private bool ignoreSolids;
        private float height;

        private Water water;
		private Solid solid;


        private List<float> lines = new List<float>();

		private Color baseColor;

		private Color surfaceColor;

		private Color fillColor;

		//private float sine;

		private SoundSource loopingSfx;

        public BigWaterfall(EntityData data, Vector2 offset) : base (data.Position + offset) {
            base.Tag = Tags.TransitionUpdate;

			width = data.Width;
            ignoreSolids = data.Bool("ignoreSolids");
			base.Depth = data.Int("depth", -49900);
			baseColor = Calc.HexToColor(data.Attr("color", "87CEFA"));
			surfaceColor = baseColor * 0.8f;
			fillColor = baseColor * 0.3f;
			lines.Add(3f);
			lines.Add(width - 4f);
			Add(loopingSfx = new SoundSource());
			loopingSfx.Play("event:/env/local/waterfall_big_main");
			//}
			/*else
			{
				base.Depth = 10010;
				parallax = 0f - (0.7f + Calc.Random.NextFloat() * 0.2f);
				surfaceColor = Calc.HexToColor("89dbf0") * 0.5f;
				fillColor = Calc.HexToColor("29a7ea") * 0.3f;
				lines.Add(6f);
				lines.Add(width - 7f);
			}
			fade = 1f;
			Add(new TransitionListener
			{
				OnIn = delegate(float f)
				{
					fade = f;
				},
				OnOut = delegate(float f)
				{
					fade = 1f - f;
				}
			});*/
			if (width > 16f) {
				int num = Calc.Random.Next((int)(width / 16f));
				for (int i = 0; i < num; i++) {
					lines.Add(8f + Calc.Random.NextFloat(width - 16f));
				}
			}

        }

        public override void Awake(Scene scene) {
			base.Awake(scene);
			Level level = base.Scene as Level;
			//bool flag = false;
			height = 8f;
			while (base.Y + height < (float)level.Bounds.Bottom && (water = base.Scene.CollideFirst<Water>(new Rectangle((int)base.X, (int)(base.Y + height), 8, 8))) == null && ((solid = base.Scene.CollideFirst<Solid>(new Rectangle((int)base.X, (int)(base.Y + height), 8, 8))) == null || (!solid.BlockWaterfalls || ignoreSolids))) {
				height += 8f;
				solid = null;
			}
			/*if (water != null && !base.Scene.CollideCheck<Solid>(new Rectangle((int)base.X, (int)(base.Y + height), 8, 16)))
			{
				flag = true;
			}*/
			/*Add(loopingSfx = new SoundSource());
			loopingSfx.Play("event:/env/local/waterfall_small_main");
			Add(enteringSfx = new SoundSource());
			enteringSfx.Play(flag ? "event:/env/local/waterfall_small_in_deep" : "event:/env/local/waterfall_small_in_shallow");
			enteringSfx.Position.Y = height;*/
			Add(new DisplacementRenderHook(RenderDisplacement));
		}


        public void RenderDisplacement() {
			Draw.Rect(base.X, base.Y, width, height, new Color(0.5f, 0.5f, 1f, 1f));
		}

        public override void Update() {
			if (loopingSfx != null) {
				Vector2 position = (base.Scene as Level).Camera.Position;
                loopingSfx.Position.Y = Calc.Clamp(position.Y + 90f, base.Y, height);
			}
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
			base.Update();
		}

        public override void Render() {
			//float x = RenderPosition.X;
			//Color color = fillColor * fade;
			//Color color2 = surfaceColor * fade;
            if (water == null || water.TopSurface == null)
			{
			    Draw.Rect(base.X, base.Y, width, height, fillColor);
		    //if (layer == Layers.FG)
			//{
                Draw.Rect(base.X - 1f, base.Y, 3f, height, surfaceColor);
				Draw.Rect(base.X + width - 2f, base.Y, 3f, height, surfaceColor);
				foreach (float line in lines) {
					Draw.Rect(base.X + line, base.Y, 1f, height, surfaceColor);
				}
                return;
            }
        	Water.Surface topSurface = water.TopSurface;
			float num = height + water.TopSurface.Position.Y - water.Y;
			for (int i = 0; i <= width; i++) {
				Draw.Rect(base.X + (float)i, base.Y, 1f, num - topSurface.GetSurfaceHeight(new Vector2(base.X + 1f + (float)i, water.Y)), fillColor);
			}
			Draw.Rect(base.X - 1f, base.Y, 3f, num - topSurface.GetSurfaceHeight(new Vector2(base.X, water.Y)), surfaceColor);
			Draw.Rect(base.X + width - 2f, base.Y, 3f, num - topSurface.GetSurfaceHeight(new Vector2(base.X + width - 1f, water.Y)), surfaceColor);
			foreach (float line in lines) {
					Draw.Rect(base.X + line, base.Y, 1f, num - topSurface.GetSurfaceHeight(new Vector2(base.X + line + 1f, water.Y)), surfaceColor);
				}
			/*}
			Vector2 position = (base.Scene as Level).Camera.Position;
			int num = 3;
			float num2 = Math.Max(base.Y, (float)Math.Floor(position.Y / (float)num) * (float)num);
			float num3 = Math.Min(base.Y + height, position.Y + 180f);
			for (float num4 = num2; num4 < num3; num4 += (float)num)
			{
				int num5 = (int)(Math.Sin(num4 / 6f - sine * 8f) * 2.0);
				Draw.Rect(X, num4, 4 + num5, num, surfaceColor);
				Draw.Rect(X + width - 4f + (float)num5, num4, 4 - num5, num, surfaceColor);
				foreach (float line2 in lines)
				{
					Draw.Rect(X + (float)num5 + line2, num4, 1f, num, surfaceColor);
				}
			}*/
		}


    }
}
