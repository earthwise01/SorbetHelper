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
        public LightOcclude lightOcclude;

        private readonly char tileType;
        private readonly bool blendIn;
        private readonly string flag;
        private readonly bool inverted;
        private readonly bool playAudio;
        private readonly bool showDebris;

        private readonly bool destroyAttached;
        private readonly float fadeInTime;

        public CrumbleOnFlagBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
            Depth = data.Int("depth", -10010);
            tileType = data.Char("tiletype", '3');
            flag = data.Attr("flag", "");
            inverted = data.Bool("inverted", false);
            playAudio = data.Bool("playAudio", true);
            showDebris = data.Bool("showDebris", true);
            blendIn = data.Bool("blendin", true);
            destroyAttached = data.Bool("destroyAttached", false);
            fadeInTime = data.Float("fadeInTime", 1f);

            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tileType];
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            var level = scene as Level;

            if (!blendIn) {
                tiles = GFX.FGAutotiler.GenerateBox(tileType, (int)Width / 8, (int)Height / 8).TileGrid;
            } else {
                var tileBounds = level.Session.MapData.TileBounds;
                var solidsData = level.SolidsData;
                int x = (int)(X / 8f) - tileBounds.Left;
                int y = (int)(Y / 8f) - tileBounds.Top;
                int tilesX = (int)Width / 8;
                int tilesY = (int)Height / 8;
                tiles = GFX.FGAutotiler.GenerateOverlay(tileType, x, y, tilesX, tilesY, solidsData).TileGrid;
            }

            Add(tiles);
            Add(new TileInterceptor(tiles, highPriority: true));
            Add(lightOcclude = new LightOcclude());

            if (CollideCheck<Player>() || level.Session.GetFlag(flag, inverted)) {
                lightOcclude.Alpha = tiles.Alpha = 0f;
                Collidable = false;

                if (destroyAttached)
                    DisableStaticMovers();
            }
        }

        public override void Update() {
            base.Update();

            if (!string.IsNullOrEmpty(flag)) {
                if (!(Scene as Level).Session.GetFlag(flag, inverted)) {
                    if (!Collidable && !CollideCheck<Player>()) {
                        Collidable = true;

                        if (destroyAttached)
                            EnableStaticMovers();
                        if (playAudio)
                            Audio.Play(SFX.game_gen_passageclosedbehind, base.Center);
                    }
                } else {
                    Break();
                }
            }

            if (Collidable) {
                if (fadeInTime <= 0f)
                    lightOcclude.Alpha = tiles.Alpha = 1f;
                else
                    lightOcclude.Alpha = tiles.Alpha = Calc.Approach(tiles.Alpha, 1f, Engine.DeltaTime / fadeInTime);
            }
        }

        public void Break() {
            if (!Collidable || Scene is null)
                return;

            Collidable = false;

            if (destroyAttached)
                DisableStaticMovers();
            if (playAudio)
                Audio.Play(SFX.game_10_quake_rockbreak, Position);
            if (showDebris) {
                for (int i = 0; i < Width / 8f; i++) {
                    for (int j = 0; j < Height / 8f; j++) {
                        if (Scene.CollideCheck<Solid>(new Rectangle((int)X + i * 8, (int)Y + j * 8, 8, 8))) {
                            Scene.Add(Engine.Pooler.Create<Debris>().Init(Position + new Vector2(4 + i * 8, 4 + j * 8), tileType, playSound: true).BlastFrom(TopCenter));
                        }
                    }
                }
            }

            lightOcclude.Alpha = tiles.Alpha = 0f;
        }
    }
}
