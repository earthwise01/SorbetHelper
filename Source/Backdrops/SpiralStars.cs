using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Backdrops;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Backdrops;

[CustomBackdrop("SorbetHelper/SpiralStars")]
public class SpiralStars : Backdrop {
    private class Star {
        public int TextureSet;
        public float AnimationTimer;
        public int FrameIndex;
        public Color Color;

        public float Angle;
        public float Distance;
        public float Scale;
        public Vector2 Position;

        public List<Vector2> TrailPositions = [];
        public List<float> TrailScales = [];
        public List<int> TrailFrames = [];
    }

    private readonly List<List<MTexture>> textures;
    private Vector2 textureCenter;
    private readonly float[] trailAlphas;

    private readonly Color backgroundColor;
    private readonly Star[] stars;

    private readonly Vector2 center;
    private readonly float speed;
    private readonly float rotationSpeed;
    private readonly float eventHorizonDistance;
    private readonly float spawningDistance;
    private readonly int trailLength;

    public SpiralStars(BinaryPacker.Element data) : base() {
        center = new(data.AttrFloat("centerX", 160f), data.AttrFloat("centerY", 90f));

        speed = data.AttrFloat("speed", 70f);
        rotationSpeed = Calc.DegToRad * data.AttrFloat("rotationSpeed", -40f);
        eventHorizonDistance = data.AttrFloat("centerRadius", 70f);
        spawningDistance = data.AttrFloat("spawnRadius", 190f);
        trailLength = data.AttrInt("trailLength", 8);

        string spritePath = data.Attr("spritePath", "bgs/02/stars");
        textures =
        [
            GFX.Game.GetAtlasSubtextures(spritePath + "/a"),
            GFX.Game.GetAtlasSubtextures(spritePath + "/b"),
            GFX.Game.GetAtlasSubtextures(spritePath + "/c"),
        ];
        textureCenter = new Vector2(textures[0][0].Width, textures[0][0].Height) / 2f;

        trailAlphas = new float[trailLength];
        for (int j = 0; j < trailAlphas.Length; j++) {
            trailAlphas[j] = 0.7f * (1f - (float)j / (float)trailAlphas.Length);
        }

        backgroundColor = Util.HexToRGBAColor(data.Attr("backgroundColor", "00000000"));
        var colors = data.Attr("colors", "ffffff").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Calc.HexToColor).ToArray();

        int starCount = data.AttrInt("starCount", 100);
        stars = new Star[starCount];
        for (int i = 0; i < stars.Length; i++) {
            var star = new Star {
                AnimationTimer = Calc.Random.NextFloat(MathF.PI * 2f),
                TextureSet = Calc.Random.Next(textures.Count),
                Color = colors[Calc.Random.Next(colors.Length)],
                Angle = Calc.Random.NextAngle(),
                Distance = Calc.Random.NextFloat(190f),
            };

            UpdateStar(star, false);

            stars[i] = star;
        }
    }

    public override void Update(Scene scene) {
        base.Update(scene);
        // bit sillyy but makes the trails explode *slightly* less on different game speeds (keyword slightly, fast game speeds still look awful but its better than before at least)
        // might look into seeing if there's a better way to do the trails in general later idk
        bool updateTrails = scene.OnInterval(Engine.RawDeltaTime);

        foreach (var star in stars) {
            star.Distance = Mod(star.Distance - Engine.DeltaTime * speed, spawningDistance + 1);
            star.Angle += Engine.DeltaTime * rotationSpeed;
            star.AnimationTimer += Engine.DeltaTime;

            UpdateStar(star, updateTrails);
        }
    }

    public override void Render(Scene scene) {
        if (backgroundColor.A > 0)
            Draw.Rect(-1f, -1f, Celeste.GameWidth + 2f, Celeste.GameHeight + 2f, backgroundColor);

        foreach (var star in stars) {
            var textureSet = textures[star.TextureSet];

            for (int j = 0; j < star.TrailPositions.Count; j++) {
                float trailScale = star.TrailScales[j];

                textureSet[star.TrailFrames[j]].Draw(star.TrailPositions[j], textureCenter, star.Color * trailScale * trailAlphas[j], trailScale);
            }

            textureSet[star.FrameIndex].Draw(star.Position, textureCenter, star.Color * star.Scale, star.Scale);
        }
    }

    private void UpdateStar(Star star, bool createTrail) {
        Vector2 position = center + Calc.AngleToVector(star.Angle, star.Distance);
        float scale = Math.Clamp(star.Distance / eventHorizonDistance, 0f, 1f);

        List<MTexture> list = textures[star.TextureSet];
        int frameIndex = (int)((Math.Sin(star.AnimationTimer) + 1.0) / 2.0 * list.Count);
        frameIndex %= list.Count;

        if (createTrail && trailLength > 0) {
            if (star.TrailPositions.Count >= trailLength)
                star.TrailPositions.RemoveAt(star.TrailPositions.Count - 1);
            star.TrailPositions.Insert(0, star.Position);

            if (star.TrailScales.Count >= trailLength)
                star.TrailScales.RemoveAt(star.TrailScales.Count - 1);
            star.TrailScales.Insert(0, star.Scale);

            if (star.TrailFrames.Count >= trailLength)
                star.TrailFrames.RemoveAt(star.TrailFrames.Count - 1);
            star.TrailFrames.Insert(0, star.FrameIndex);
        }

        star.Position = position;
        star.Scale = scale;
        star.FrameIndex = frameIndex;
    }

    private static float Mod(float x, float m) {
        return (x % m + m) % m;
    }
}
