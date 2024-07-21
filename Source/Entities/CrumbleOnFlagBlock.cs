using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SorbetHelper.Utils;
using MonoMod.Utils;

namespace Celeste.Mod.SorbetHelper.Entities {

    [CustomEntity("SorbetHelper/CrumbleOnFlagBlock")]
    public class CrumbleOnFlagBlock : Solid {
        public TileGrid tiles;
        public EffectCutout cutout;

        private readonly char tileType;
        private readonly bool blendIn;
        private readonly string flag;
        private readonly bool inverted;
        private readonly bool playAudio;
        private readonly bool showDebris;

        public CrumbleOnFlagBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
            base.Depth = data.Int("depth", -10010);
            tileType = data.Char("tiletype", '3');
            flag = data.Attr("flag", "");
            inverted = data.Bool("inverted", false);
            playAudio = data.Bool("playAudio", true);
            showDebris = data.Bool("showDebris", true);
            blendIn = data.Bool("blendin", true);

            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tileType];
            Add(cutout = new EffectCutout());
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            Level level = scene as Level;

            if (!blendIn) {
                tiles = GFX.FGAutotiler.GenerateBox(tileType, (int)base.Width / 8, (int)base.Height / 8).TileGrid;
            } else {
                Rectangle tileBounds = level.Session.MapData.TileBounds;
                VirtualMap<char> solidsData = level.SolidsData;
                int x = (int)(base.X / 8f) - tileBounds.Left;
                int y = (int)(base.Y / 8f) - tileBounds.Top;
                int tilesX = (int)base.Width / 8;
                int tilesY = (int)base.Height / 8;
                tiles = GFX.FGAutotiler.GenerateOverlay(tileType, x, y, tilesX, tilesY, solidsData).TileGrid;
            }

            Add(tiles);
            Add(new TileInterceptor(tiles, highPriority: true));
            Add(new LightOcclude());

            if (CollideCheck<Player>() || level.Session.GetFlag(flag, inverted)) {
                cutout.Alpha = tiles.Alpha = 0f;
                Collidable = false;
            }
        }

        public override void Update() {
            base.Update();

            if (!string.IsNullOrEmpty(flag)) {
                if (!SceneAs<Level>().Session.GetFlag(flag, inverted)) {
                    if (!Collidable && !CollideCheck<Player>()) {
                        Collidable = true;
                        if (playAudio) {
                            Audio.Play("event:/game/general/passage_closed_behind", base.Center);
                        }
                    }
                } else {
                    Break();
                }
            }

            if (Collidable) {
                cutout.Alpha = tiles.Alpha = Calc.Approach(tiles.Alpha, 1f, Engine.DeltaTime);
            }
        }

        public virtual void Break() {
            if (!Collidable || base.Scene == null)
                return;

            if (playAudio)
                Audio.Play(SFX.game_10_quake_rockbreak, Position);

            Collidable = false;

            if (showDebris) {
                for (int i = 0; i < base.Width / 8f; i++) {
                    for (int j = 0; j < base.Height / 8f; j++) {
                        if (!base.Scene.CollideCheck<Solid>(new Rectangle((int)base.X + i * 8, (int)base.Y + j * 8, 8, 8))) {
                            base.Scene.Add(Engine.Pooler.Create<Debris>().Init(Position + new Vector2(4 + i * 8, 4 + j * 8), tileType, playSound: true).BlastFrom(base.TopCenter));
                        }
                    }
                }
            }

            cutout.Alpha = tiles.Alpha = 0f;
        }
    }
}
