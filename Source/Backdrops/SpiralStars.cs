using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Backdrops;
using Celeste.Mod.SorbetHelper.Utils;

namespace Celeste.Mod.SorbetHelper.Backdrops;

[CustomBackdrop("SorbetHelper/SpiralStars")]
public class SpiralStars : Backdrop
{
    private class Star(int textureSet, Color color, int trailLength)
    {
        public struct StarSprite
        {
            public Vector2 Position;
            public float Scale;
            public int FrameIndex;
        }

        public readonly int TextureSet = textureSet;
        public readonly Color Color = color;

        public float Distance;
        public float Angle;
        public float AnimationTimer;

        public StarSprite Sprite;
        public readonly StarSprite[] Trail = new StarSprite[trailLength];
    }

    private readonly Color backgroundColor;

    private readonly Vector2 center;
    private readonly float speed;
    private readonly float rotationSpeed;
    private readonly float innerRadius;
    private readonly float outerRadius;
    private readonly float trailDelay;

    private readonly List<List<MTexture>> textureSets = [];
    private readonly Vector2 textureCenter;
    private readonly float[] trailAlphas;

    private readonly Star[] stars;

    public SpiralStars(BinaryPacker.Element data) : base()
    {
        backgroundColor = Calc.HexToColorWithNonPremultipliedAlpha(data.Attr("backgroundColor", "00000000"));
        Color[] colors = data.AttrList("colors", Calc.HexToColorWithNonPremultipliedAlpha, "ffffff").ToArray();
        center = new Vector2(data.AttrFloat("centerX", 160f), data.AttrFloat("centerY", 90f));
        speed = data.AttrFloat("speed", 70f);
        rotationSpeed = data.AttrFloat("rotationSpeed", -40f).ToRad();
        innerRadius = data.AttrFloat("centerRadius", 70f);
        outerRadius = data.AttrFloat("spawnRadius", 190f);
        trailDelay = data.AttrFloat("trailDelay", 1f / 60f);
        int trailLength = data.AttrInt("trailLength", 8);

        string spriteDir = data.Attr("spritePath", "bgs/02/stars");
        textureSets.Add(GFX.Game.GetAtlasSubtextures(spriteDir + "/a"));
        for (char c = 'b'; c <= 'z'; c++)
        {
            string spritePath = spriteDir + "/" + c;
            if (GFX.Game.HasAtlasSubtextures(spritePath))
                textureSets.Add(GFX.Game.GetAtlasSubtextures(spritePath));
            else
                break;
        }
        textureCenter = new Vector2(textureSets[0][0].Width, textureSets[0][0].Height) / 2f;

        trailAlphas = new float[trailLength];
        for (int i = 0; i < trailAlphas.Length; i++)
            trailAlphas[i] = 0.7f * (1f - (float)i / (float)trailAlphas.Length);

        stars = new Star[data.AttrInt("starCount", 100)];
        for (int i = 0; i < stars.Length; i++)
        {
            Star star = new Star(Calc.Random.Next(textureSets.Count), colors[Calc.Random.Next(colors.Length)], trailLength)
            {
                Distance = Calc.Random.NextFloat(outerRadius),
                Angle = Calc.Random.NextAngle(),
                AnimationTimer = Calc.Random.NextFloat(MathF.PI * 2f)
            };
            UpdateStarSprites(star);

            stars[i] = star;
        }
    }

    public override void Update(Scene scene)
    {
        base.Update(scene);

        foreach (Star star in stars)
        {
            star.Distance = Mod(star.Distance - Engine.DeltaTime * speed, outerRadius + 1);
            star.Angle += Engine.DeltaTime * rotationSpeed;
            star.AnimationTimer += Engine.DeltaTime;

            UpdateStarSprites(star);
        }
    }

    public override void Render(Scene scene)
    {
        if (backgroundColor.A > 0)
            Draw.Rect(-1f, -1f, SorbetHelperGFX.GameplayBufferWidth + 2f, SorbetHelperGFX.GameplayBufferHeight + 2f, backgroundColor);

        Vector2 zoomCenterOffset = SorbetHelperGFX.GetZoomOutCameraCenterOffset((scene as Level)!.Camera);

        foreach (Star star in stars)
        {
            List<MTexture> textureSet = textureSets[star.TextureSet];

            for (int i = 0; i < star.Trail.Length; i++)
                textureSet[star.Trail[i].FrameIndex].Draw(star.Trail[i].Position + zoomCenterOffset, textureCenter, star.Color * star.Trail[i].Scale * trailAlphas[i], star.Trail[i].Scale);

            textureSet[star.Sprite.FrameIndex].Draw(star.Sprite.Position + zoomCenterOffset, textureCenter, star.Color * star.Sprite.Scale, star.Sprite.Scale);
        }
    }

    private void UpdateStarSprites(Star star)
    {
        for (int i = 0; i < star.Trail.Length; i++)
            UpdateStarSprite(star, ref star.Trail[i], -trailDelay * (i + 1));

        UpdateStarSprite(star, ref star.Sprite, 0f);
    }

    private void UpdateStarSprite(Star star, ref Star.StarSprite starSprite, float timeOffset)
    {
        float distance = Mod(star.Distance - timeOffset * speed, outerRadius + 1);
        float angle = star.Angle + timeOffset * rotationSpeed;

        Vector2 position = center + Calc.AngleToVector(angle, distance);
        float scale = Math.Clamp(distance / innerRadius, 0f, 1f);

        List<MTexture> textureSet = textureSets[star.TextureSet];
        int frameIndex = (int)((Math.Sin(star.AnimationTimer + timeOffset) + 1.0) / 2.0 * textureSet.Count);
        frameIndex %= textureSet.Count;

        starSprite.Position = position;
        starSprite.Scale = scale;
        starSprite.FrameIndex = frameIndex;
    }

    private static float Mod(float x, float m) => (x % m + m) % m;
}
