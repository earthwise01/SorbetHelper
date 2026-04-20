namespace Celeste.Mod.SorbetHelper.Entities;

[CustomEntity("SorbetHelper/CrumbleOnFlagBlock")]
public class CrumbleOnFlagBlock : Solid
{
    private readonly char tileType;
    private readonly bool blendIn;
    private readonly string flag;
    private readonly bool inverted;
    private readonly bool playAudio;
    private readonly bool showDebris;

    private readonly bool destroyAttached;
    private readonly float fadeInTime;

    private TileGrid tiles;
    private LightOcclude lightOcclude;

    public CrumbleOnFlagBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false)
    {
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

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        Level level = SceneAs<Level>();

        int widthInTiles = (int)Width / 8;
        int heightInTiles = (int)Height / 8;
        if (!blendIn)
        {
            tiles = GFX.FGAutotiler.GenerateBox(tileType, widthInTiles, heightInTiles).TileGrid;
        }
        else
        {
            Rectangle tileBounds = level.Session.MapData.TileBounds;
            VirtualMap<char> solidsData = level.SolidsData;
            int solidsDataX = (int)(X / 8f) - tileBounds.Left;
            int solidsDataY = (int)(Y / 8f) - tileBounds.Top;
            tiles = GFX.FGAutotiler.GenerateOverlay(tileType, solidsDataX, solidsDataY, widthInTiles, heightInTiles, solidsData).TileGrid;
        }

        Add(tiles);
        Add(new TileInterceptor(tiles, highPriority: true));
        Add(lightOcclude = new LightOcclude());

        if (CollideCheck<Player>() || level.Session.GetFlag(flag, inverted))
        {
            lightOcclude.Alpha = tiles.Alpha = 0f;
            Collidable = false;

            if (destroyAttached)
                DisableStaticMovers();
        }
    }

    public override void Update()
    {
        base.Update();

        if (!string.IsNullOrEmpty(flag) && SceneAs<Level>().Session.GetFlag(flag, inverted) == Collidable)
        {
            if (Collidable)
                Break();
            else if (!CollideCheck<Player>())
                Reform();
        }

        if (Collidable)
        {
            if (fadeInTime <= 0f)
                lightOcclude.Alpha = tiles.Alpha = 1f;
            else
                lightOcclude.Alpha = tiles.Alpha = Calc.Approach(tiles.Alpha, 1f, Engine.DeltaTime / fadeInTime);
        }
    }

    private void Break()
    {
        Collidable = false;

        if (destroyAttached)
            DisableStaticMovers();

        if (playAudio)
            Audio.Play(SFX.game_10_quake_rockbreak, Position);

        if (showDebris)
        {
            for (int x = 0; x < Width; x += 8)
            for (int y = 0; y < Height; y += 8)
            {
                if (!Scene.CollideCheck<Solid>(new Rectangle((int)X + x, (int)Y + y, 8, 8)))
                {
                    Scene.Add(Engine.Pooler.Create<Debris>()
                                           .Init(Position + new Vector2(x + 4, y + 4), tileType, playSound: true)
                                           .BlastFrom(TopCenter));
                }
            }
        }

        lightOcclude.Alpha = tiles.Alpha = 0f;
    }

    private void Reform()
    {
        Collidable = true;

        if (destroyAttached)
            EnableStaticMovers();

        if (playAudio)
            Audio.Play(SFX.game_gen_passageclosedbehind, Center);
    }
}
