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

        public readonly record struct Trail(Vector2 Position, float Scale, int FrameIndex);
        public List<Trail> Trails = [];
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
    private readonly float trailDelay;

    public SpiralStars(BinaryPacker.Element data) : base() {
        center = new(data.AttrFloat("centerX", 160f), data.AttrFloat("centerY", 90f));

        speed = data.AttrFloat("speed", 70f);
        rotationSpeed = Calc.DegToRad * data.AttrFloat("rotationSpeed", -40f);
        eventHorizonDistance = data.AttrFloat("centerRadius", 70f);
        spawningDistance = data.AttrFloat("spawnRadius", 190f);
        trailLength = data.AttrInt("trailLength", 8);
        trailDelay = data.AttrFloat("trailDelay", 1f / 60f);

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

            UpdateStar(star);

            stars[i] = star;
        }
    }

    public override void Update(Scene scene) {
        base.Update(scene);

        foreach (var star in stars) {
            star.Distance = Mod(star.Distance - Engine.DeltaTime * speed, spawningDistance + 1);
            star.Angle += Engine.DeltaTime * rotationSpeed;
            star.AnimationTimer += Engine.DeltaTime;

            UpdateStar(star);
        }
    }

    public override void Render(Scene scene) {
        if (backgroundColor.A > 0)
            Draw.Rect(-1f, -1f, Celeste.GameWidth + 2f, Celeste.GameHeight + 2f, backgroundColor);

        foreach (var star in stars) {
            var textureSet = textures[star.TextureSet];

            for (int j = 0; j < star.Trails.Count; j++) {
                var trail = star.Trails[j];
                float trailScale = trail.Scale;

                textureSet[trail.FrameIndex].Draw(trail.Position, textureCenter, star.Color * trailScale * trailAlphas[j], trailScale);
            }

            textureSet[star.FrameIndex].Draw(star.Position, textureCenter, star.Color * star.Scale, star.Scale);
        }
    }

    private void UpdateStar(Star star) {
        Vector2 position = center + Calc.AngleToVector(star.Angle, star.Distance);
        float scale = Math.Clamp(star.Distance / eventHorizonDistance, 0f, 1f);

        List<MTexture> list = textures[star.TextureSet];
        int frameIndex = (int)((Math.Sin(star.AnimationTimer) + 1.0) / 2.0 * list.Count);
        frameIndex %= list.Count;

        if (trailLength > 0) {
            star.Trails.Clear();
            for (int i = 1; i <= trailLength; i++) {
                star.Trails.Add(GetTrailWithTimeOffset(star, i * -trailDelay));
            }
        }

        star.Position = position;
        star.Scale = scale;
        star.FrameIndex = frameIndex;
    }

    private Star.Trail GetTrailWithTimeOffset(Star star, float timeOffset) {
        float distance = Mod(star.Distance - timeOffset * speed, spawningDistance + 1);
        float angle = star.Angle + timeOffset * rotationSpeed;

        Vector2 position = center + Calc.AngleToVector(angle, distance);
        float scale = Math.Clamp(distance / eventHorizonDistance, 0f, 1f);

        List<MTexture> list = textures[star.TextureSet];
        int frameIndex = (int)((Math.Sin(star.AnimationTimer + timeOffset) + 1.0) / 2.0 * list.Count);
        frameIndex %= list.Count;

        return new(position, scale, frameIndex);
    }

    private static float Mod(float x, float m) {
        return (x % m + m) % m;
    }
}
